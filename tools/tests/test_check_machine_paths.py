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


# The two CI runner paths, as they appeared in issue #70's spec and plan.
RUNNER_PATH = "Base dir: " + "/home/" + "runner/work/data.net/data.net"

# A home-directory path in the shape a contributor's terminal produces.
POSIX_HOME = "/home/" + "someone/Documents/devs/data.net"
MAC_HOME = "/Users/" + "someone/src/data.net"
WINDOWS_HOME = "C:\\\\Users\\\\someone\\\\src"

# A generic Windows profile folder named in prose, with nothing after it: the
# same "mention vs. path" distinction the POSIX patterns draw with their
# trailing separator, checked here for the Windows one too.
WINDOWS_PROSE = "C:" + "\\Users\\" + "Public"

# The same folder as an actual path, with a trailing separator and a
# component after it -- the shape that must still be flagged.
WINDOWS_PATH_WITH_FILE = WINDOWS_PROSE + "\\file"

# Load-bearing paths that must never be flagged. The oracle generator has to
# run from a neutral directory -- nltk refuses to import its dependencies when
# they appear to live under the current one -- so /tmp appears in CLAUDE.md,
# in CONTRIBUTING.md and in several plans.
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
    # "Public" here is a generic Windows profile folder, not someone's home --
    # the same class of false positive the trailing separator rules out for
    # the POSIX patterns above.
    assert not scan("See " + WINDOWS_PROSE + " for shared files")


def test_a_windows_path_with_a_trailing_component_is_flagged():
    assert scan(WINDOWS_PATH_WITH_FILE)


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


def test_the_guard_exempts_only_itself_and_its_tests():
    assert guard.EXEMPT == frozenset({
        "tools/check_machine_paths.py",
        "tools/tests/test_check_machine_paths.py",
    })


# The scratchpad path as it appeared in four plans, with the name redacted the
# way the spec redacts it -- the shape is what matters, and a whole one here
# would put a home directory back into a tracked file.
SCRATCH = "/tmp/claude-" + "49201103/" + "-home-" + "someone-Documents-devs-data-net2/x/scratchpad"


def test_the_named_shapes_alone_miss_the_dashed_form():
    # The finding that shaped this guard: the scratchpad encodes the home
    # directory with dashes, so nothing searching for a slash-separated one
    # sees it. Only the /tmp/claude- prefix catches this string by shape.
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
    # A username that appears inside an unrelated word is not a path, and a
    # guard that said otherwise would fire on prose for any contributor
    # unlucky enough to be called something ordinary.
    probes = guard.environment_probes("/home/" + "ed")

    assert not guard.scan_text("the edited plan", probes)
    assert guard.scan_text("/home/" + "ed/src", probes)


def test_no_home_means_no_environment_probes():
    assert guard.environment_probes(None) == ()
