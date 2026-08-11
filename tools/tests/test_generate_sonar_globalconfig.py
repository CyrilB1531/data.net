"""The generator is the one thing here a drift check cannot judge: it proves the file
stable, never right. These run offline, against fixtures trimmed from real responses."""
import http.server
import json
import os
import re
import subprocess
import sys
import threading
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import generate_sonar_globalconfig as gen  # noqa: E402

FIXTURES = Path(__file__).parent / "fixtures"
SCRIPT = Path(__file__).resolve().parents[1] / "generate_sonar_globalconfig.py"


def test_disabled_rules_reads_the_sarif_rule_table():
    disabled = gen.disabled_rules(FIXTURES / "error_log.sarif")

    assert "S107" in disabled
    assert "S1192" in disabled
    # Declared with an empty defaultConfiguration, which means enabled.
    assert "S2245" not in disabled


def test_active_rules_keeps_only_csharpsquid_ids():
    active = gen.active_rules(json.loads((FIXTURES / "rules_search.json").read_text(encoding="utf-8")))

    # S100 is deliberately absent: it is disabled-but-not-active (see the fixture
    # generation note in the implementation plan), so it never reaches the profile's
    # activation=true response and must not appear here either.
    # csharpsecurity:S2076 is present in the fixture and deliberately absent here:
    # it exercises the REPOSITORY filter, which the other four rules -- all
    # csharpsquid: -- never did on their own.
    assert active == {"S107", "S1192", "S2245", "S3776"}


def test_the_delta_is_active_and_disabled_only():
    active = gen.active_rules(json.loads((FIXTURES / "rules_search.json").read_text(encoding="utf-8")))
    disabled = gen.disabled_rules(FIXTURES / "error_log.sarif")

    assert gen.delta(active, disabled) == ["S107", "S1192", "S3776"]


def test_the_delta_is_ordered_by_rule_number_not_alphabetically():
    # S1192 sorts before S107 as text, which would make the file churn for no reason.
    assert gen.delta({"S107", "S1192"}, {"S107", "S1192"}) == ["S107", "S1192"]


def test_render_declares_a_global_config():
    text = gen.render(["S107"], profile_key="P", analyzer_version="1.2.3")

    assert text.splitlines()[0] == "is_global = true"
    assert "dotnet_diagnostic.S107.severity = warning" in text
    assert "P" in text
    assert "1.2.3" in text


def test_render_carries_no_timestamp():
    # Asserted directly, on the text itself: calling render() twice in the same
    # process and comparing would pass even if a second-granularity timestamp were
    # embedded, since both calls would land in the same second. A file that changes
    # every day cannot be drift-checked (D3 of the 0109 spec), so what matters is
    # that no date or time ever appears, not that two nearly-simultaneous calls agree.
    text = gen.render(["S107"], profile_key="P", analyzer_version="1.2.3")

    assert not re.search(r"\d{4}-\d{2}-\d{2}", text)
    assert not re.search(r"\d{1,2}:\d{2}:\d{2}", text)


def test_render_is_a_pure_function_of_its_arguments():
    first = gen.render(["S107"], profile_key="P", analyzer_version="1.2.3")
    second = gen.render(["S107"], profile_key="P", analyzer_version="1.2.3")

    assert first == second


def test_active_rules_raises_when_rules_search_is_truncated():
    # ps=500 against a profile with more than 500 active rules would silently drop
    # the overflow from the intersection: --check would then regenerate the same
    # truncated file and report a match, and the build would enforce less than the
    # quality gate with every gate staying green. total > len(rules) is the one
    # signal available to catch that from the response alone.
    payload = {"p": 1, "ps": 4, "total": 500, "rules": [{"key": "csharpsquid:S107"}]}

    with pytest.raises(gen.TruncatedResponse, match="total=500"):
        gen.active_rules(payload)


def test_check_exits_with_a_dedicated_code_when_rules_search_is_truncated(tmp_path):
    truncated = tmp_path / "rules_search_truncated.json"
    truncated.write_text(
        json.dumps({"p": 1, "ps": 4, "total": 500, "rules": [{"key": "csharpsquid:S107"}]}),
        encoding="utf-8")

    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--check", "--error-log", str(FIXTURES / "error_log.sarif"),
         "--rules", str(truncated), "--output", str(tmp_path / ".globalconfig")],
        capture_output=True, text=True, check=False)

    # Neither the drift code nor the unreachable-API code: a truncated response is
    # its own failure and must not be mistaken for either.
    assert result.returncode == gen.EXIT_TRUNCATED
    assert result.returncode not in (gen.EXIT_DRIFT, gen.EXIT_UNREACHABLE)
    assert "total=500" in result.stderr


def test_check_exits_one_and_prints_a_diff_when_the_file_drifted(tmp_path):
    target = tmp_path / ".globalconfig"
    target.write_text("is_global = true\n", encoding="utf-8")

    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--check", "--error-log", str(FIXTURES / "error_log.sarif"),
         "--rules", str(FIXTURES / "rules_search.json"), "--output", str(target)],
        capture_output=True, text=True, check=False)

    assert result.returncode == 1
    assert "dotnet_diagnostic.S107.severity" in result.stdout


def test_check_exits_two_when_the_api_cannot_be_reached(tmp_path):
    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--check", "--error-log", str(FIXTURES / "error_log.sarif"),
         "--api", "http://127.0.0.1:9/api", "--output", str(tmp_path / ".globalconfig")],
        capture_output=True, text=True, check=False)

    # Not 1: a check that reports drift when the network is down would send someone
    # editing a file that never changed.
    assert result.returncode == 2
    assert "127.0.0.1:9" in result.stderr


class _ServiceUnavailableHandler(http.server.BaseHTTPRequestHandler):
    """Answers every request with 503, so fetch() raises urllib.error.HTTPError."""

    def do_GET(self):  # noqa: N802 - BaseHTTPRequestHandler's own naming
        self.send_response(503)
        self.end_headers()

    def log_message(self, format_, *args):  # noqa: A002 - keep pytest output quiet
        pass


def test_check_exits_two_not_three_on_an_http_status_error(tmp_path):
    # urllib.error.HTTPError -- raised here by a real 503 response, not a stub --
    # is a urllib.error.URLError subclass, and it sets .filename to the request URL
    # with .strerror left None. Before the fix, checking .filename alone routed it
    # into the local-file branch and printed "cannot read <url>: None" at exit 3: a
    # SonarCloud outage reported as an unreadable file. It must stay a network
    # failure, exit 2, same as a refused connection.
    server = http.server.HTTPServer(("127.0.0.1", 0), _ServiceUnavailableHandler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        api = f"http://127.0.0.1:{server.server_address[1]}"
        result = subprocess.run(
            [sys.executable, str(SCRIPT), "--check", "--error-log", str(FIXTURES / "error_log.sarif"),
             "--api", api, "--output", str(tmp_path / ".globalconfig")],
            capture_output=True, text=True, check=False)
    finally:
        server.shutdown()
        thread.join()

    assert result.returncode == gen.EXIT_UNREACHABLE
    assert result.returncode != gen.EXIT_INPUT_MISSING
    assert "cannot read" not in result.stderr
    assert "could not reach" in result.stderr


def test_check_exits_zero_when_the_file_on_disk_already_matches(tmp_path):
    target = tmp_path / ".globalconfig"
    rules = gen.delta(
        gen.active_rules(json.loads((FIXTURES / "rules_search.json").read_text(encoding="utf-8"))),
        gen.disabled_rules(FIXTURES / "error_log.sarif"))
    target.write_text(gen.render(rules, profile_key="(fixture)", analyzer_version=gen.analyzer_version()),
                       encoding="utf-8")

    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--check", "--error-log", str(FIXTURES / "error_log.sarif"),
         "--rules", str(FIXTURES / "rules_search.json"), "--output", str(target)],
        capture_output=True, text=True, check=False)

    assert result.returncode == 0
    assert "matches profile" in result.stdout


def test_check_names_the_path_and_does_not_report_the_api_as_unreachable_when_a_local_file_is_missing(tmp_path):
    missing = tmp_path / "does-not-exist.sarif"

    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--check", "--error-log", str(missing),
         "--rules", str(FIXTURES / "rules_search.json"), "--output", str(tmp_path / ".globalconfig")],
        capture_output=True, text=True, check=False)

    # A local path typo is not a network failure: it must not be reported as
    # one, and it must not reuse the "API unreachable" exit code.
    assert result.returncode == gen.EXIT_INPUT_MISSING
    assert result.returncode != gen.EXIT_UNREACHABLE
    assert "could not reach" not in result.stderr
    assert str(missing) in result.stderr


def test_check_reports_an_unreadable_local_file_as_a_local_failure_not_the_network(tmp_path):
    if hasattr(os, "geteuid") and os.geteuid() == 0:
        pytest.skip("root ignores POSIX permission bits, so chmod 000 would not fail")

    unreadable = tmp_path / "unreadable.sarif"
    unreadable.write_text("{}", encoding="utf-8")
    unreadable.chmod(0o000)
    try:
        result = subprocess.run(
            [sys.executable, str(SCRIPT), "--check", "--error-log", str(unreadable),
             "--rules", str(FIXTURES / "rules_search.json"), "--output", str(tmp_path / ".globalconfig")],
            capture_output=True, text=True, check=False)
    finally:
        unreadable.chmod(0o644)

    # No network call happens at all here -- --rules bypasses the API entirely --
    # yet a PermissionError on a local file used to be reported as "could not reach
    # https://sonarcloud.io/api", because the OSError branch treated everything that
    # was not a bare FileNotFoundError as the network. It must be reported as the
    # local failure it is.
    assert result.returncode == gen.EXIT_INPUT_MISSING
    assert result.returncode != gen.EXIT_UNREACHABLE
    assert "could not reach" not in result.stderr
    assert str(unreadable) in result.stderr


def test_committed_globalconfig_raises_s107():
    # The permanent half of D6 in the 0109 spec: every other test here runs against
    # trimmed fixtures, so none of them would notice a regenerated file that lost a
    # rule the issue's acceptance criteria named. This one reads the file the build
    # actually uses.
    text = (Path(__file__).resolve().parents[2] / ".globalconfig").read_text(encoding="utf-8")

    assert "dotnet_diagnostic.S107.severity = warning" in text
