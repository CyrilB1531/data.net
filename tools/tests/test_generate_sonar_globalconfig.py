"""The generator is the one thing here a drift check cannot judge: it proves the file
stable, never right. These run offline, against fixtures trimmed from real responses."""
import json
import subprocess
import sys
from pathlib import Path

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
    assert active == {"S107", "S1192", "S2245", "S3776"}


def test_the_delta_is_active_and_disabled_only():
    active = gen.active_rules(json.loads((FIXTURES / "rules_search.json").read_text(encoding="utf-8")))
    disabled = gen.disabled_rules(FIXTURES / "error_log.sarif")

    assert gen.delta(active, disabled) == ["S107", "S1192", "S3776"]


def test_the_delta_is_ordered_by_rule_number_not_alphabetically():
    # S1192 sorts before S107 as text, which would make the file churn for no reason.
    assert gen.delta({"S107", "S1192"}, {"S107", "S1192"}) == ["S107", "S1192"]


def test_render_declares_a_global_config_and_carries_no_timestamp():
    text = gen.render(["S107"], profile_key="P", analyzer_version="1.2.3")

    assert text.splitlines()[0] == "is_global = true"
    assert "dotnet_diagnostic.S107.severity = warning" in text
    assert "P" in text and "1.2.3" in text
    assert gen.render(["S107"], profile_key="P", analyzer_version="1.2.3") == text


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
