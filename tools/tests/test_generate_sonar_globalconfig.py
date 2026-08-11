"""The generator is the one thing here a drift check cannot judge: it proves the file
stable, never right. These run offline, against fixtures trimmed from real responses.

The CLI lost --error-log/--rules/--api/--output (issue #131 -- a SonarCloud taint
scan flagged all four as reaching a path or URL sink), so these drive the module
directly: `gen.main([...])` in-process, with `monkeypatch.setattr` swapping the
module constants (`ERROR_LOG`, `OUTPUT`, `DEFAULT_API`) instead of passing flags to
a subprocess. `DEFAULT_API` points at a throwaway localhost HTTP server for the
tests that used to hand the script a saved rules/search response with --rules; the
server never leaves the machine, so this stays as offline as the fixture file was.
"""
import contextlib
import http.server
import json
import os
import re
import threading
from pathlib import Path

import pytest

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import generate_sonar_globalconfig as gen  # noqa: E402

FIXTURES = Path(__file__).parent / "fixtures"
RULES_SEARCH = json.loads((FIXTURES / "rules_search.json").read_text(encoding="utf-8"))


@contextlib.contextmanager
def api_server(rules_payload):
    """A localhost stand-in for sonarcloud.io/api that answers the two calls
    build() makes -- qualityprofiles/search and rules/search -- from *rules_payload*,
    so a test can point DEFAULT_API at it instead of reaching the real network."""

    class Handler(http.server.BaseHTTPRequestHandler):
        def do_GET(self):  # noqa: N802 - BaseHTTPRequestHandler's own naming
            if self.path.startswith("/qualityprofiles/search"):
                body = json.dumps({"profiles": [{"language": "cs", "key": "(fixture)"}]}).encode("utf-8")
            elif self.path.startswith("/rules/search"):
                body = json.dumps(rules_payload).encode("utf-8")
            else:
                self.send_response(404)
                self.end_headers()
                return
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(body)

        def log_message(self, format_, *args):  # noqa: A002 - keep pytest output quiet
            pass

    server = http.server.HTTPServer(("127.0.0.1", 0), Handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        yield f"http://127.0.0.1:{server.server_address[1]}"
    finally:
        server.shutdown()
        thread.join()


def test_disabled_rules_reads_the_sarif_rule_table():
    disabled = gen.disabled_rules(FIXTURES / "error_log.sarif")

    assert "S107" in disabled
    assert "S1192" in disabled
    # Declared with an empty defaultConfiguration, which means enabled.
    assert "S2245" not in disabled


def test_active_rules_keeps_only_csharpsquid_ids():
    active = gen.active_rules(RULES_SEARCH)

    # S100 is deliberately absent: it is disabled-but-not-active (see the fixture
    # generation note in the implementation plan), so it never reaches the profile's
    # activation=true response and must not appear here either.
    # csharpsecurity:S2076 is present in the fixture and deliberately absent here:
    # it exercises the REPOSITORY filter, which the other four rules -- all
    # csharpsquid: -- never did on their own.
    assert active == {"S107", "S1192", "S2245", "S3776"}


def test_the_delta_is_active_and_disabled_only():
    active = gen.active_rules(RULES_SEARCH)
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


def test_check_exits_with_a_dedicated_code_when_rules_search_is_truncated(tmp_path, monkeypatch, capsys):
    truncated = {"p": 1, "ps": 4, "total": 500, "rules": [{"key": "csharpsquid:S107"}]}
    monkeypatch.setattr(gen, "ERROR_LOG", FIXTURES / "error_log.sarif")
    monkeypatch.setattr(gen, "OUTPUT", tmp_path / ".globalconfig")

    with api_server(truncated) as api:
        monkeypatch.setattr(gen, "DEFAULT_API", api)
        code = gen.main(["--check"])

    # Neither the drift code nor the unreachable-API code: a truncated response is
    # its own failure and must not be mistaken for either.
    assert code == gen.EXIT_TRUNCATED
    assert code not in (gen.EXIT_DRIFT, gen.EXIT_UNREACHABLE)
    assert "total=500" in capsys.readouterr().err


def test_check_exits_one_and_prints_a_diff_when_the_file_drifted(tmp_path, monkeypatch, capsys):
    target = tmp_path / ".globalconfig"
    target.write_text("is_global = true\n", encoding="utf-8")
    monkeypatch.setattr(gen, "ERROR_LOG", FIXTURES / "error_log.sarif")
    monkeypatch.setattr(gen, "OUTPUT", target)

    with api_server(RULES_SEARCH) as api:
        monkeypatch.setattr(gen, "DEFAULT_API", api)
        code = gen.main(["--check"])

    assert code == 1
    assert "dotnet_diagnostic.S107.severity" in capsys.readouterr().out


def test_check_exits_two_when_the_api_cannot_be_reached(tmp_path, monkeypatch, capsys):
    monkeypatch.setattr(gen, "ERROR_LOG", FIXTURES / "error_log.sarif")
    monkeypatch.setattr(gen, "OUTPUT", tmp_path / ".globalconfig")
    monkeypatch.setattr(gen, "DEFAULT_API", "http://127.0.0.1:9/api")

    code = gen.main(["--check"])

    # Not 1: a check that reports drift when the network is down would send someone
    # editing a file that never changed.
    assert code == 2
    assert "127.0.0.1:9" in capsys.readouterr().err


class _ServiceUnavailableHandler(http.server.BaseHTTPRequestHandler):
    """Answers every request with 503, so fetch() raises urllib.error.HTTPError."""

    def do_GET(self):  # noqa: N802 - BaseHTTPRequestHandler's own naming
        self.send_response(503)
        self.end_headers()

    def log_message(self, format_, *args):  # noqa: A002 - keep pytest output quiet
        pass


def test_check_exits_two_not_three_on_an_http_status_error(tmp_path, monkeypatch, capsys):
    # urllib.error.HTTPError -- raised here by a real 503 response, not a stub --
    # is a urllib.error.URLError subclass, and it sets .filename to the request URL
    # with .strerror left None. Before the fix, checking .filename alone routed it
    # into the local-file branch and printed "cannot read <url>: None" at exit 3: a
    # SonarCloud outage reported as an unreadable file. It must stay a network
    # failure, exit 2, same as a refused connection.
    monkeypatch.setattr(gen, "ERROR_LOG", FIXTURES / "error_log.sarif")
    monkeypatch.setattr(gen, "OUTPUT", tmp_path / ".globalconfig")

    server = http.server.HTTPServer(("127.0.0.1", 0), _ServiceUnavailableHandler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        monkeypatch.setattr(gen, "DEFAULT_API", f"http://127.0.0.1:{server.server_address[1]}")
        code = gen.main(["--check"])
    finally:
        server.shutdown()
        thread.join()

    captured = capsys.readouterr()
    assert code == gen.EXIT_UNREACHABLE
    assert code != gen.EXIT_INPUT_MISSING
    assert "cannot read" not in captured.err
    assert "could not reach" in captured.err


def test_check_exits_zero_when_the_file_on_disk_already_matches(tmp_path, monkeypatch, capsys):
    target = tmp_path / ".globalconfig"
    rules = gen.delta(gen.active_rules(RULES_SEARCH), gen.disabled_rules(FIXTURES / "error_log.sarif"))
    target.write_text(gen.render(rules, profile_key="(fixture)", analyzer_version=gen.analyzer_version()),
                       encoding="utf-8")
    monkeypatch.setattr(gen, "ERROR_LOG", FIXTURES / "error_log.sarif")
    monkeypatch.setattr(gen, "OUTPUT", target)

    with api_server(RULES_SEARCH) as api:
        monkeypatch.setattr(gen, "DEFAULT_API", api)
        code = gen.main(["--check"])

    assert code == 0
    assert "matches profile" in capsys.readouterr().out


def test_check_names_the_path_and_does_not_report_the_api_as_unreachable_when_a_local_file_is_missing(
        tmp_path, monkeypatch, capsys):
    missing = tmp_path / "does-not-exist.sarif"
    monkeypatch.setattr(gen, "ERROR_LOG", missing)
    monkeypatch.setattr(gen, "OUTPUT", tmp_path / ".globalconfig")

    with api_server(RULES_SEARCH) as api:
        monkeypatch.setattr(gen, "DEFAULT_API", api)
        code = gen.main(["--check"])

    captured = capsys.readouterr()
    # A local path typo is not a network failure: it must not be reported as
    # one, and it must not reuse the "API unreachable" exit code.
    assert code == gen.EXIT_INPUT_MISSING
    assert code != gen.EXIT_UNREACHABLE
    assert "could not reach" not in captured.err
    assert str(missing) in captured.err


def test_check_reports_an_unreadable_local_file_as_a_local_failure_not_the_network(tmp_path, monkeypatch, capsys):
    if hasattr(os, "geteuid") and os.geteuid() == 0:
        pytest.skip("root ignores POSIX permission bits, so chmod 000 would not fail")

    unreadable = tmp_path / "unreadable.sarif"
    unreadable.write_text("{}", encoding="utf-8")
    unreadable.chmod(0o000)
    monkeypatch.setattr(gen, "ERROR_LOG", unreadable)
    monkeypatch.setattr(gen, "OUTPUT", tmp_path / ".globalconfig")

    try:
        with api_server(RULES_SEARCH) as api:
            monkeypatch.setattr(gen, "DEFAULT_API", api)
            code = gen.main(["--check"])
    finally:
        unreadable.chmod(0o644)

    captured = capsys.readouterr()
    # The local server above answers every call, so this exercises exactly what it
    # is meant to: a PermissionError on a local file must not be reported as "could
    # not reach" the API, because the network is not what failed.
    assert code == gen.EXIT_INPUT_MISSING
    assert code != gen.EXIT_UNREACHABLE
    assert "could not reach" not in captured.err
    assert str(unreadable) in captured.err


def test_committed_globalconfig_raises_s107():
    # The permanent half of D6 in the 0109 spec: every other test here runs against
    # trimmed fixtures, so none of them would notice a regenerated file that lost a
    # rule the issue's acceptance criteria named. This one reads the file the build
    # actually uses.
    text = (Path(__file__).resolve().parents[2] / ".globalconfig").read_text(encoding="utf-8")

    assert "dotnet_diagnostic.S107.severity = warning" in text
