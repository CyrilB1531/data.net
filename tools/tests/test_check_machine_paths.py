"""The guard's own tests, holding the strings that actually reached this repository.

Issue #133 exists because ten absolute paths were committed and nothing caught
them. Six of the strings below are those, recovered from the commits that
removed them, rather than examples someone invented -- which is the only way
these prove the guard catches what happened instead of what was imagined.

The module under test contains the patterns it searches for, so it exempts
itself and this file. Everything else in the repository is scanned.
"""
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_machine_paths as guard  # noqa: E402

# argv[0], unused by main() (only argv[1:] matters) -- a shared constant
# keeps python:S1192 quiet past three literal repeats.
PROG = "check_machine_paths.py"


# The two CI runner paths, as they appeared in issue #70's spec and plan.
RUNNER_PATH = "Base dir: " + "/home/" + "runner/work/data.net/data.net"

# A home-directory path in the shape a contributor's terminal produces.
POSIX_HOME = "/home/" + "someone/Documents/devs/data.net"
MAC_HOME = "/Users/" + "someone/src/data.net"
WINDOWS_HOME = "C:\\\\Users\\\\someone\\\\src"

# A generic Windows profile mentioned in prose, nothing after it -- the same
# mention-vs-path distinction the POSIX patterns draw, checked for Windows too.
WINDOWS_PROSE = "C:" + "\\Users\\" + "Public"

# The same folder as an actual path, with a trailing separator and a
# component after it -- the shape that must still be flagged.
WINDOWS_PATH_WITH_FILE = WINDOWS_PROSE + "\\file"

# The same home directory, written with forward slashes -- which Windows and
# PowerShell both accept -- rather than backslashes.
WINDOWS_HOME_FORWARD_SLASH = "C:/Users/" + "someone/src/thing.cs"
WINDOWS_PROSE_FORWARD_SLASH = "C:/Users/" + "Public"

# A UNC profile path, assembled from fragments for the same reason
# POSIX_HOME/WINDOWS_HOME are: this file must not contain the literal shape it tests.
UNC_HOME = chr(92) * 2 + "server" + chr(92) + "Users" + chr(92) + "someone"
UNC_HOME_WITH_FILE = UNC_HOME + chr(92) + "file"

# A share that is not called "Users" -- not a home directory, however deep.
UNC_SHARE_BARE = chr(92) * 2 + "server" + chr(92) + "share"

# The "Users" share itself, named in prose with nobody's profile after it --
# the same "mention vs. path" distinction WINDOWS_PROSE draws above.
UNC_USERS_SHARE_BARE = chr(92) * 2 + "server" + chr(92) + "Users"

# Load-bearing: /tmp is required by the neutral-directory rule CLAUDE.md and
# CONTRIBUTING.md give the oracle generator (nltk's cwd-import refusal).
NEUTRAL = "cd /tmp && python tools/generate_oracles.py"
SYSTEM = "/usr/bin/env python3"
TILDE = "~/.nuget/packages"


def scan(text):
    return guard.scan_text(text, guard.NAMED_SHAPES)


def test_a_runner_checkout_path_is_flagged():
    assert scan(RUNNER_PATH)


def test_a_posix_home_directory_is_flagged():
    assert scan(POSIX_HOME)


def test_a_mac_home_directory_is_flagged():
    assert scan(MAC_HOME)


def test_a_windows_home_directory_is_flagged():
    assert scan(WINDOWS_HOME)


def test_a_bare_windows_profile_mention_is_not_flagged():
    # "Public" is a generic Windows profile, not someone's home -- same
    # false-positive class the trailing separator rules out for POSIX above.
    assert not scan("See " + WINDOWS_PROSE + " for shared files")


def test_a_windows_path_with_a_trailing_component_is_flagged():
    assert scan(WINDOWS_PATH_WITH_FILE)


def test_a_forward_slash_windows_home_directory_is_flagged():
    # Caught deliberately by the Windows pattern now, not by luck through the
    # /Users/ pattern meant for macOS.
    assert scan(WINDOWS_HOME_FORWARD_SLASH)


def test_a_bare_forward_slash_windows_profile_mention_is_not_flagged():
    assert not scan("See " + WINDOWS_PROSE_FORWARD_SLASH + " for shared files")


def test_a_unc_home_directory_is_flagged():
    assert scan(UNC_HOME_WITH_FILE)


def test_a_bare_unc_share_is_not_flagged():
    # A network share that isn't named "Users" isn't a home directory.
    assert not scan(UNC_SHARE_BARE)


def test_a_bare_unc_users_share_is_not_flagged():
    assert not scan(UNC_USERS_SHARE_BARE)


def test_the_neutral_working_directory_is_not_flagged():
    # /tmp is load-bearing here; a guard that refused it would break the
    # documented way to run the oracle generator.
    assert not scan(NEUTRAL)


def test_system_paths_and_tilde_are_not_flagged():
    assert not scan(SYSTEM)
    assert not scan(TILDE)


def test_the_report_names_the_line():
    text = "clean line\n" + POSIX_HOME + "\n"
    findings = scan(text)

    assert findings[0][0] == 2


def test_the_report_names_which_probe_matched():
    findings = scan(POSIX_HOME)

    assert findings[0][1] == "a home directory under /home"


def test_the_guard_exempts_only_itself_and_its_tests():
    assert guard.EXEMPT == frozenset({
        "tools/check_machine_paths.py",
        "tools/tests/test_check_machine_paths.py",
    })


# The shape #133's spec found in four plans, name redacted the same way the
# spec redacts it -- id below is a fake stand-in, not history's real one.
SCRATCH = "/tmp/claude-" + "12345678/" + "-home-" + "someone-Documents-devs-data-net2/x/scratchpad"


def test_the_named_shapes_alone_miss_the_dashed_form():
    # The finding that shaped this guard (#133's spec): dashes, not slashes,
    # so only the /tmp/claude- prefix catches this string by shape.
    dashed_only = "-home-" + "someone-Documents-devs-data-net2"

    assert not guard.scan_text(dashed_only, guard.NAMED_SHAPES)


def test_an_environment_probe_catches_the_dashed_form():
    probes = guard.environment_probes("/home/" + "someone")
    dashed_only = "-home-" + "someone-Documents-devs-data-net2"

    assert guard.scan_text(dashed_only, probes)


def test_an_environment_probe_catches_the_home_path_itself():
    probes = guard.environment_probes("/home/" + "someone")

    assert guard.scan_text(POSIX_HOME, probes)


def test_an_environment_probe_needs_a_boundary_around_the_name():
    # A username inside an unrelated word isn't a path -- a guard that said
    # otherwise would fire on prose for any ordinarily-named contributor.
    probes = guard.environment_probes("/home/" + "ed")

    assert not guard.scan_text("the edited plan", probes)
    assert guard.scan_text("/home/" + "ed/src", probes)


def test_no_home_means_no_environment_probes():
    assert guard.environment_probes(None) == ()


def test_a_trailing_separator_on_home_does_not_disable_the_probes():
    # Regression test: environment_probes("/home/name/") used to split on the
    # trailing "/" and get account == "", silently dropping all three probes.
    with_slash = guard.environment_probes("/home/" + "someone" + "/")
    without_slash = guard.environment_probes("/home/" + "someone")

    assert with_slash == without_slash
    assert with_slash != ()


def test_home_of_only_a_separator_still_yields_no_probes():
    # Stripping "/" from "/" leaves "", caught by the existing empty-account
    # clause -- must keep returning no probes, not crash.
    assert guard.environment_probes("/") == ()


def test_a_windows_home_directory_yields_the_same_three_probes():
    home = "C:" + chr(92) + "Users" + chr(92) + "someone"

    probes = guard.environment_probes(home)

    assert len(probes) == 3


def test_a_windows_path_is_caught_by_the_derived_probes():
    home = "C:" + chr(92) + "Users" + chr(92) + "someone"
    text = "the file lives at " + home + chr(92) + "src" + chr(92) + "thing.cs"

    assert any(pattern.search(text) for _, pattern in guard.environment_probes(home))


def test_a_trailing_backslash_does_not_swallow_the_account_name():
    # The POSIX branch strips a trailing "/" for this reason: without it the
    # account name comes out empty and all three probes are silently dropped.
    bare = "C:" + chr(92) + "Users" + chr(92) + "someone"

    assert guard.environment_probes(bare + chr(92)) == guard.environment_probes(bare)


def test_tracked_files_is_independent_of_the_process_cwd(monkeypatch):
    # Regression test for tracked_files()'s own docstring: cwd used to leak
    # from the process, silently scanning a fraction of the repository.
    baseline = guard.tracked_files()

    monkeypatch.chdir(guard.ROOT / "tools")

    assert guard.tracked_files() == baseline


def test_help_flag_prints_to_stdout_and_exits_zero(capsys):
    exit_code = guard.main([PROG, "--help"])
    captured = capsys.readouterr()

    assert exit_code == 0
    assert captured.out
    assert not captured.err


def test_short_help_flag_behaves_the_same(capsys):
    exit_code = guard.main([PROG, "-h"])
    captured = capsys.readouterr()

    assert exit_code == 0
    assert captured.out
    assert not captured.err


def test_an_unrecognised_argument_is_bad_usage_on_stderr(capsys):
    exit_code = guard.main([PROG, "--nonsense"])
    captured = capsys.readouterr()

    assert exit_code == 2
    assert not captured.out
    assert captured.err


def test_the_failure_message_points_at_no_environment_for_a_derived_hit(tmp_path, monkeypatch, capsys):
    # A derived probe (often the account name) is the one likeliest to fire
    # on ordinary prose -- the escape hatch is undiscoverable unless named here.
    monkeypatch.setenv("HOME", "/home/" + "someone")
    monkeypatch.setattr(guard, "ROOT", tmp_path)
    monkeypatch.setattr(guard, "tracked_files", lambda: ["finding.txt"])
    (tmp_path / "finding.txt").write_text(
        "-home-" + "someone-Documents-devs-data-net2\n", encoding="utf-8")

    exit_code = guard.main([PROG])
    captured = capsys.readouterr()

    assert exit_code == 1
    assert "--no-environment" in captured.err


def test_the_failure_message_omits_no_environment_for_a_named_shape_hit(tmp_path, monkeypatch, capsys):
    # By design (#133 spec D3): a named-shape hit is never a false positive,
    # so the message should not point at a flag that would not help.
    monkeypatch.delenv("HOME", raising=False)
    monkeypatch.setattr(guard, "ROOT", tmp_path)
    monkeypatch.setattr(guard, "tracked_files", lambda: ["finding.txt"])
    (tmp_path / "finding.txt").write_text(RUNNER_PATH + "\n", encoding="utf-8")

    exit_code = guard.main([PROG])
    captured = capsys.readouterr()

    assert exit_code == 1
    assert "--no-environment" not in captured.err
