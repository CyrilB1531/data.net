# #109 — The build enforces only the default Sonar rules Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Make `dotnet build` refuse what the SonarCloud quality gate refuses, by generating a
`.globalconfig` from the server's own quality profile; and document the one local run that covers what no
.NET build can — Python, duplication and coverage.

**Architecture:** A Python generator intersects two facts: which C# rules the project's server profile
activates (one anonymous HTTP call) and which rules `SonarAnalyzer.CSharp` ships disabled (the
`tool.driver.rules[]` table of a SARIF v2 error log the compiler already knows how to write). The
intersection — **nine rules, measured** — becomes severity entries in a root `.globalconfig` wired once in
`Directory.Build.props`. The same script in `--check` mode guards the file against profile drift on every
pull request. The second net is a pinned SonarQube Community container plus the honest list of what it does
not reproduce.

**Tech Stack:** Python 3.12 (`urllib`, no new runtime dependency), pytest (new, dev-only), MSBuild
`GlobalAnalyzerConfigFiles`, SonarAnalyzer.CSharp 10.20.0.135146, SonarQube Community in Docker or Podman.

**Spec:** `docs/superpowers/specs/2026-08-11_0109_the-build-enforces-only-the-default-sonar-rules.md`

## Global Constraints

- Everything in English — code, comments, commit messages, PR body. Commit messages carry no
  `feat:`/`fix:` prefix and no process prefix such as `Fix round 1:`.
- Branch `chore/109-sonar-rule-parity` in `/home/cyril/Documents/devs/data.net`, based on `main` at
  `92f9f4d`. Never commit to `main`. Do not push or open a pull request without asking.
- Warnings are errors repository-wide. Every task's build must end `0 Avertissement(s) 0 Erreur(s)`, and
  **`dotnet build` is incremental — without `--no-incremental` no analyzer diagnostic is produced at all**.
- `dotnet format DataNet.slnx --verify-no-changes` must exit 0. Run it **bare**, no `env -u DOTNET_ROOT`.
- Read the pass/fail **counts** of every test run. A `--filter` that matches nothing exits zero and reports
  success. Baseline on `main` at `92f9f4d`: **2269 passing, 0 failed**, across eight assemblies.
- **Never write `echo "exit=$?"` after a pipeline** — it reports the last command's status. Redirect to a
  file and check separately.
- `docs/superpowers/` and `tools/README.md` are inside CI's markdownlint glob; `CHANGELOG.md` is not.
- Python style: this repository runs SonarCloud's Python analysis over `tools/`. Keep functions small,
  and give any `except` that is itself a measurement a comment saying so.
- No secret is required by anything this plan adds. If a step seems to need `SONAR_TOKEN`, it is the
  container task, and the token is the *local* container's, never SonarCloud's.

## What was already measured, and must not be re-derived from scratch

The spec records these against `SonarAnalyzer.CSharp` **10.20.0.135146**. Tasks below cite them; Task 1
re-verifies only the one that is still open.

| Fact | Value |
| --- | --- |
| C# profile for `CyrilB1531_data.net` | `Sonar way`, key `AZF_RqJ__mc37gztrQ3P`, **377 active rules**, 343 of them `csharpsquid` |
| Rules the package declares / ships disabled | **450** `Sxxxx` declared, **138** `enabled: false` |
| **The delta** (active on server **and** disabled in package) | **9**: `S107 S110 S1192 S1479 S2342 S2436 S3776 S6664 S6669` |
| Findings those nine produce today | **0** across `src`, `tests`, `bench`, `samples/DataNet.Sample` and `samples/DataNet.DocSnippets` — verified non-vacuous by a canary 8-parameter method, which produced `warning S107` twice (once per target framework) |
| `S2245` | **not** in the delta: the package declares it *enabled*, yet four unsuppressed `new Random` sites build green. Task 1 settles why |

## File Structure

| File | Responsibility |
| --- | --- |
| `tools/generate_sonar_globalconfig.py` *(new)* | Reads the profile and the SARIF rule table, writes or checks `.globalconfig`. |
| `tools/tests/test_generate_sonar_globalconfig.py` *(new)* | Proves the parsing, the intersection and the rendering — offline, on frozen fixtures. |
| `tools/tests/fixtures/rules_search.json`, `error_log.sarif` *(new)* | Trimmed real responses, committed. |
| `tools/requirements.txt`, `tools/requirements.lock.txt` | `pytest` added, lock regenerated with hashes. |
| `.globalconfig` *(new, generated, committed)* | The nine severity entries. |
| `Directory.Build.props` | One `GlobalAnalyzerConfigFiles` item, with the comment saying why it is explicit. |
| `.github/workflows/ci.yml` | The `Lint` job gains the pytest run and the drift check. |
| `tests/Directory.Build.props`, `bench/Directory.Build.props` | `CA5394` leaves the area-wide `NoWarn` (Task 1). |
| `tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs`, `tests/DataNet.Text.Tests/Distances/LevenshteinPropertyTests.cs` | The four seeded `new Random` sites gain a suppression carrying its reason. |
| `tools/sonarqube-local/compose.yaml` *(new)* | The pinned local server. |
| `CONTRIBUTING.md` | The pre-push section, and the `.editorconfig`-versus-build correction. |
| `tools/README.md` | The new script and the container, in the file's existing table. |

---

### Task 1: Settle what a build can enforce about `new Random`

**Files:**

- Modify: `tests/Directory.Build.props:22`
- Modify: `bench/Directory.Build.props:16`
- Modify: `tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs` (around `:436` and `:499`)
- Modify: `tests/DataNet.Text.Tests/Distances/LevenshteinPropertyTests.cs` (around `:32` and `:61`)

**Depends on:** nothing.

**Produces:** the answer Task 3's acceptance assertion needs — whether the issue's second criterion
("re-introducing an unsuppressed `new Random` fails `dotnet build`") is met by `S2245`, by `CA5394`, or by
neither.

The issue rests on `S2245` being invisible to the build. The measurement says the package declares it
**enabled**, and four unsuppressed sites still build green. One of those two statements is misleading, and
a plan that guessed which would bake the wrong criterion into a test.

- [ ] **Step 1: Force the rule and see whether it can fire at all**

```bash
cd /home/cyril/Documents/devs/data.net
printf '[*.cs]\ndotnet_diagnostic.S2245.severity = warning\n' > .editorconfig
dotnet build tests/DataNet.Text.Tests -c Release --no-incremental -p:TreatWarningsAsErrors=false > /tmp/109-t1-s2245.log 2>&1
echo "build=$?"
grep -c "warning S2245" /tmp/109-t1-s2245.log
rm -f .editorconfig
```

`LevenshteinPropertyTests.cs:32,61` are unsuppressed `new Random(Seed)` sites in that project. If the count
is 0, the rule cannot be reached from a plain build: SonarAnalyzer gates its security-hotspot rules on
scanner context, so no severity entry will ever make it fail a compilation. Record the count either way.

- [ ] **Step 2: Measure the .NET rule that covers the same code**

`CA5394` ("do not use insecure randomness") is the build-visible counterpart, and both `tests/` and
`bench/` carry it in an area-wide `NoWarn`. Take it out temporarily and count:

```bash
sed -i 's/;CA5394//' tests/Directory.Build.props bench/Directory.Build.props
dotnet build DataNet.slnx -c Release --no-incremental -p:TreatWarningsAsErrors=false > /tmp/109-t1-ca5394.log 2>&1
echo "build=$?"
grep -oE "^[^(]+\([0-9]+,[0-9]+\): warning CA5394" /tmp/109-t1-ca5394.log | sed 's|.*/data.net/||' | sort -u
git checkout tests/Directory.Build.props bench/Directory.Build.props
```

Expect the four sites the issue names — line numbers have moved since it was written, they are now
`BpeTokenizerTests.cs:436,499` and `LevenshteinPropertyTests.cs:32,61` — plus every `bench/` site, which
already carries an `S2245` pragma and relies on the `NoWarn` for the CA half.

- [ ] **Step 3: Make the criterion true through the rule that can carry it**

Remove `CA5394` from both area-wide `NoWarn` lists, and suppress it where it is deliberate, at the site,
with the reason — which is `CONTRIBUTING.md`'s own rule: an area-wide `NoWarn` is for a rule an area trips
*by being that area*, and "a seeded generator makes this test reproducible" is a statement about four call
sites, not about the whole test tree.

In each of the two test files, above the class, matching the idiom already in
`tests/DataNet.Metrics.Tests/RocAucParallelTests.cs:6-8`:

```csharp
// SonarLint S2245 / CA5394: a seeded Random builds a reproducible corpus for this
// test; the sequence is fixed by the seed and nothing here is a security decision.
#pragma warning disable S2245, CA5394
```

In the five `bench/` files that already open a `#pragma warning disable S2245`, extend the same line to
`S2245, CA5394` rather than adding a second pragma, and leave their existing comment as it is.

- [ ] **Step 4: Green, with the counts read**

```bash
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/109-t1-b.log 2>&1; echo "build=$?"; tail -3 /tmp/109-t1-b.log
dotnet test DataNet.slnx -c Release > /tmp/109-t1-t.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/109-t1-t.log
```

Expected: 0 warnings, **2269 passing**. This task changes no behaviour, so any change in the count is a
mistake to explain rather than to accept.

- [ ] **Step 5: Prove the guard is real**

Add a temporary unsuppressed site and watch the build fail, then delete it. A suppression that would also
hide a *new* misuse is worth nothing:

```bash
cat > tests/DataNet.Text.Tests/ZzProbe109.cs <<'EOF'
namespace DataNet.Text.Tests;

internal static class ZzProbe109
{
    public static int Next() => new System.Random().Next();
}
EOF
dotnet build tests/DataNet.Text.Tests -c Release --no-incremental > /tmp/109-t1-canary.log 2>&1
echo "build=$?"
grep -oE "error (CA5394|S2245)" /tmp/109-t1-canary.log | sort | uniq -c
rm -f tests/DataNet.Text.Tests/ZzProbe109.cs
```

Expected: non-zero exit and `error CA5394`. Quote the line in your report.

- [ ] **Step 6: Commit**

```bash
git add tests/Directory.Build.props bench/Directory.Build.props \
        tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs \
        tests/DataNet.Text.Tests/Distances/LevenshteinPropertyTests.cs \
        bench/DataNet.Text.Benchmarks
git commit -m "Suppress the seeded generators one by one, not the whole test tree"
```

---

### Task 2: The generator, and the Python test suite it needs

**Files:**

- Create: `tools/generate_sonar_globalconfig.py`
- Create: `tools/tests/test_generate_sonar_globalconfig.py`
- Create: `tools/tests/fixtures/rules_search.json`, `tools/tests/fixtures/error_log.sarif`
- Modify: `tools/requirements.txt`, `tools/requirements.lock.txt`

**Depends on:** nothing.

**Interfaces:**

- Produces, for Tasks 3 and 4:
  - `python3 tools/generate_sonar_globalconfig.py --error-log <path>` writes `.globalconfig` at the
    repository root and exits 0.
  - `--check` compares instead of writing: **exit 0** identical, **exit 1** drift (prints a unified diff),
    **exit 2** the API could not be reached (prints the URL and the error).
  - `--rules <path>` substitutes a saved API response for the HTTP call, which is what the tests use.

- [ ] **Step 1: Add pytest and regenerate the lock**

Append to `tools/requirements.txt`, under a comment that says why a test dependency lives beside the
oracle-generation ones:

```text
# pytest — the only test runner in tools/. tools/generate_sonar_globalconfig.py
# parses two foreign formats and writes a file the build reads; a drift check
# proves that generator stable, never that it is right.
pytest==8.4.2
```

Then regenerate the hashed lock exactly as `CONTRIBUTING.md:195` prescribes:

```bash
pip-compile --generate-hashes --strip-extras --output-file tools/requirements.lock.txt tools/requirements.txt
git diff --stat tools/requirements.lock.txt
```

If `pip-compile` is not installed, `pipx run pip-tools pip-compile …` produces the same file. Do **not**
hand-edit the lock.

- [ ] **Step 2: Freeze the two fixtures from real responses**

```bash
mkdir -p tools/tests/fixtures
curl -s "https://sonarcloud.io/api/rules/search?organization=cyrilb1531&qprofile=AZF_RqJ__mc37gztrQ3P&activation=true&languages=cs&ps=500" \
  -o /tmp/109-rules-full.json
python3 - <<'EOF'
import json, pathlib
full = json.load(open("/tmp/109-rules-full.json"))
keep = {"csharpsquid:S107", "csharpsquid:S1192", "csharpsquid:S3776", "csharpsquid:S2245", "csharpsquid:S100"}
trimmed = {"total": len(keep), "p": 1, "ps": 500,
           "rules": [{"key": r["key"]} for r in full["rules"] if r["key"] in keep]}
pathlib.Path("tools/tests/fixtures/rules_search.json").write_text(
    json.dumps(trimmed, indent=1, sort_keys=True) + "\n", encoding="utf-8")
EOF
```

For the SARIF fixture, produce a real error log and trim it to the same five rules:

```bash
dotnet build src/DataNet.Fuzzy -c Release --no-incremental -f net10.0 \
  -p:ErrorLog=/tmp/109-fixture.sarif%2Cversion=2 > /tmp/109-fixture-build.log 2>&1
echo "build=$?"
python3 - <<'EOF'
import json, pathlib
d = json.load(open("/tmp/109-fixture.sarif", encoding="utf-8-sig"))
rules = d["runs"][0]["tool"]["driver"]["rules"]
keep = {"S107", "S1192", "S3776", "S2245", "S100"}
trimmed = {"version": "2.1.0", "runs": [{"tool": {"driver": {"name": "trimmed for tests", "rules": [
    {"id": r["id"], "defaultConfiguration": r.get("defaultConfiguration", {})}
    for r in rules if r["id"] in keep]}}}]}
pathlib.Path("tools/tests/fixtures/error_log.sarif").write_text(
    json.dumps(trimmed, indent=1, sort_keys=True) + "\n", encoding="utf-8")
EOF
```

Note the `%2C`: MSBuild eats a bare comma in a property value, and without the escape the compiler writes
**SARIF v1**, whose `rules` table lists only rules that produced a result — no `enabled` flag anywhere.
This cost a probe when the spec was written; do not "simplify" it back.

The five chosen rules make the fixture carry every case the code must distinguish: `S107`, `S1192` and
`S3776` are active-and-disabled (they belong in the output), `S2245` is active-and-enabled (it does not),
and `S100` is disabled-but-not-active (it does not either).

- [ ] **Step 3: Write the failing tests**

`tools/tests/test_generate_sonar_globalconfig.py`:

```python
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

    assert active == {"S100", "S107", "S1192", "S2245", "S3776"}


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
```

- [ ] **Step 4: Run them and watch them fail**

```bash
python3 -m pytest tools/tests -q > /tmp/109-t2-red.log 2>&1
echo "pytest=$?"
tail -5 /tmp/109-t2-red.log
```

Expected: a collection error, `ModuleNotFoundError: generate_sonar_globalconfig`. **Read the count** — a
run that collected 0 tests is not a red run.

- [ ] **Step 5: Write the generator**

`tools/generate_sonar_globalconfig.py`:

```python
#!/usr/bin/env python3
"""Generate the .globalconfig that raises the Sonar rules the analyzer package ships disabled.

SonarAnalyzer.CSharp enables a subset of its own rules by default; the SonarCloud
quality profile enables a different set. Rules in the second and not the first are
invisible to `dotnet build` and blocking at the quality gate, which is a three-minute
round trip per finding (issue #109).

Two inputs, both cheap:

  * the profile's active rules, from one anonymous SonarCloud call -- the project is
    public, so no token is involved and anyone who clones this repository can run it;
  * `runs[].tool.driver.rules[]` of a SARIF v2 error log, where the compiler itself
    declares which rules are enabled by default.

The intersection is written as severity entries. `warning`, not `error`:
TreatWarningsAsErrors already decides whether a finding stops the build, and one
lever for that decision is enough.
"""
from __future__ import annotations

import argparse
import difflib
import json
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path

DEFAULT_API = "https://sonarcloud.io/api"
ORGANIZATION = "cyrilb1531"
PROJECT = "CyrilB1531_data.net"
LANGUAGE = "cs"
REPOSITORY = "csharpsquid"

RULE_ID = re.compile(r"^S\d+$")
EXIT_DRIFT = 1
EXIT_UNREACHABLE = 2

ROOT = Path(__file__).resolve().parent.parent


def fetch(url: str) -> dict:
    """One GET, or a clean failure naming the URL."""
    with urllib.request.urlopen(url, timeout=30) as response:  # noqa: S310 - fixed https host
        return json.loads(response.read().decode("utf-8"))


def profile_key(api: str) -> str:
    """The C# profile associated with *this project*, not the organization's default."""
    url = f"{api}/qualityprofiles/search?organization={ORGANIZATION}&project={PROJECT}"
    for profile in fetch(url)["profiles"]:
        if profile["language"] == LANGUAGE:
            return profile["key"]
    raise LookupError(f"no {LANGUAGE} quality profile is associated with {PROJECT}")


def active_rules(payload: dict) -> set[str]:
    """The rule ids the profile activates, without the repository prefix."""
    return {
        key.split(":", 1)[1]
        for key in (rule["key"] for rule in payload["rules"])
        if key.startswith(f"{REPOSITORY}:")
    }


def disabled_rules(error_log: Path) -> set[str]:
    """The rule ids the analyzer package declares and ships disabled."""
    # utf-8-sig: the compiler writes a byte-order mark.
    document = json.loads(error_log.read_text(encoding="utf-8-sig"))
    disabled: set[str] = set()
    for run in document["runs"]:
        for rule in run["tool"]["driver"].get("rules", []):
            enabled = rule.get("defaultConfiguration", {}).get("enabled")
            if enabled is False and RULE_ID.match(rule["id"]):
                disabled.add(rule["id"])
    return disabled


def delta(active: set[str], disabled: set[str]) -> list[str]:
    """Active on the server and disabled in the package, in rule-number order."""
    return sorted(active & disabled, key=lambda rule: int(rule[1:]))


def render(rules: list[str], profile_key: str, analyzer_version: str) -> str:
    """The file, deterministic and timestamp-free so it can be drift-checked."""
    lines = [
        "is_global = true",
        "",
        "# Generated by tools/generate_sonar_globalconfig.py -- do not edit by hand.",
        "#",
        f"# The rules SonarCloud's profile {profile_key} activates for {PROJECT} and that",
        f"# SonarAnalyzer.CSharp {analyzer_version} ships disabled, so that a finding fails the",
        "# build on the machine that wrote the code rather than the quality gate three",
        "# minutes after the push. See docs/decisions/0015-sonar-rules-in-the-build.md and",
        "# issue #109.",
        "#",
        "# warning, not error: TreatWarningsAsErrors in the root Directory.Build.props is",
        "# what turns a finding into a failure, and two levers for one decision is one too",
        "# many.",
        "",
    ]
    lines += [f"dotnet_diagnostic.{rule}.severity = warning" for rule in rules]
    return "\n".join(lines) + "\n"


def analyzer_version() -> str:
    """The pin the root Directory.Build.props carries, quoted in the header."""
    text = (ROOT / "Directory.Build.props").read_text(encoding="utf-8")
    match = re.search(r"<DataNetSonarAnalyzerVersion>([^<]+)</", text)
    if match is None:
        raise LookupError("Directory.Build.props declares no DataNetSonarAnalyzerVersion")
    return match.group(1)


def build(args: argparse.Namespace) -> tuple[str, str]:
    if args.rules is not None:
        payload = json.loads(Path(args.rules).read_text(encoding="utf-8"))
        key = "(fixture)"
    else:
        key = profile_key(args.api)
        payload = fetch(
            f"{args.api}/rules/search?organization={ORGANIZATION}&qprofile={key}"
            f"&activation=true&languages={LANGUAGE}&ps=500")
    rules = delta(active_rules(payload), disabled_rules(Path(args.error_log)))
    return render(rules, key, analyzer_version()), key


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--error-log", required=True, help="SARIF v2 error log from a build")
    parser.add_argument("--output", default=str(ROOT / ".globalconfig"))
    parser.add_argument("--rules", help="a saved rules/search response, instead of calling the API")
    parser.add_argument("--api", default=DEFAULT_API)
    parser.add_argument("--check", action="store_true", help="compare instead of writing")
    args = parser.parse_args(argv)

    try:
        expected, key = build(args)
    except (urllib.error.URLError, TimeoutError, OSError) as error:
        # The failure that must not look like drift: the file is fine, the network is not.
        print(f"could not reach {args.api}: {error}", file=sys.stderr)
        return EXIT_UNREACHABLE

    target = Path(args.output)
    if not args.check:
        target.write_text(expected, encoding="utf-8")
        print(f"{target}: {expected.count('dotnet_diagnostic.')} rules from profile {key}")
        return 0

    actual = target.read_text(encoding="utf-8") if target.exists() else ""
    if actual == expected:
        print(f"{target} matches profile {key}")
        return 0
    print("".join(difflib.unified_diff(
        actual.splitlines(keepends=True), expected.splitlines(keepends=True),
        fromfile=str(target), tofile="regenerated")))
    return EXIT_DRIFT


if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 6: Run them and watch them pass**

```bash
python3 -m pytest tools/tests -q > /tmp/109-t2-green.log 2>&1
echo "pytest=$?"
tail -3 /tmp/109-t2-green.log
```

Expected: **7 passed**. State the number.

- [ ] **Step 7: Run the Python analysis before committing**

`.github/instructions/sonarqube_mcp.instructions.md` applies. Analyse the new script and the test file
through the SonarQube MCP server's snippet analysis, since its C# analyser does not exist but its Python
one does, and clear anything it reports.

- [ ] **Step 8: Commit**

```bash
git add tools/generate_sonar_globalconfig.py tools/tests tools/requirements.txt tools/requirements.lock.txt
git commit -m "Derive the rules the build is missing from the profile that gates the merge"
```

---

### Task 3: Generate the file, wire it into the build, and prove it bites

**Files:**

- Create: `.globalconfig`
- Modify: `Directory.Build.props`

**Depends on:** Task 2.

**Interfaces:**

- Consumes: `tools/generate_sonar_globalconfig.py --error-log <path>`.
- Produces: a build in which the nine rules are enforced.

- [ ] **Step 1: Produce an error log and generate the file**

```bash
cd /home/cyril/Documents/devs/data.net
dotnet build src/DataNet.Fuzzy -c Release --no-incremental -f net10.0 \
  -p:ErrorLog=/tmp/109-t3.sarif%2Cversion=2 > /tmp/109-t3-sarif.log 2>&1
echo "build=$?"
python3 tools/generate_sonar_globalconfig.py --error-log /tmp/109-t3.sarif
cat .globalconfig
```

Expected, from the spec's measurement: **nine** entries — `S107 S110 S1192 S1479 S2342 S2436 S3776 S6664
S6669`. A different set is not a reason to edit the file by hand; it is a reason to say so in your report,
because it means the profile or the analyzer moved since 2026-08-11.

One project's error log is enough: every project in the repository resolves the same analyzer version
through `$(DataNetSonarAnalyzerVersion)`, and the rule table describes the analyzers, not the code.

- [ ] **Step 2: Wire it, once, where a reader will find it**

In the root `Directory.Build.props`, after the `PropertyGroup` that pins
`DataNetSonarAnalyzerVersion`:

```xml
  <!-- The rules the server's quality profile enables and SonarAnalyzer.CSharp ships
       disabled. Generated by tools/generate_sonar_globalconfig.py; the Lint job
       regenerates and diffs it on every pull request, so a profile change is caught
       here rather than three minutes after a push.

       Named explicitly because a .globalconfig is picked up on its own only from the
       project's own directory, never from an ancestor — and an .editorconfig, which
       would be found by the upward search, also carries the formatting conventions
       dotnet format enforces. See issue #109. -->
  <ItemGroup>
    <GlobalAnalyzerConfigFiles Include="$(MSBuildThisFileDirectory).globalconfig" />
  </ItemGroup>
```

`samples/Directory.Build.props` imports this file, so the samples are covered without a second entry.

- [ ] **Step 3: Prove the wiring reaches the compiler**

This is the issue's first acceptance criterion, demonstrated rather than left to a canary project that
must never compile:

```bash
cat > tests/DataNet.Text.Tests/ZzProbe109.cs <<'EOF'
namespace DataNet.Text.Tests;

internal static class ZzProbe109
{
    public static int TooManyParameters(int a, int b, int c, int d, int e, int f, int g, int h)
        => a + b + c + d + e + f + g + h;
}
EOF
dotnet build tests/DataNet.Text.Tests -c Release --no-incremental > /tmp/109-t3-canary.log 2>&1
echo "build=$?"
grep -oE "error S107" /tmp/109-t3-canary.log | sort | uniq -c
rm -f tests/DataNet.Text.Tests/ZzProbe109.cs
```

Expected: non-zero exit, `error S107` (twice — once per target framework). Quote the line in your report;
this is the sentence the issue asks for.

- [ ] **Step 4: Green everywhere, including outside the solution**

```bash
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/109-t3-b.log 2>&1; echo "build=$?"; tail -3 /tmp/109-t3-b.log
dotnet format DataNet.slnx --verify-no-changes > /tmp/109-t3-f.log 2>&1; echo "format=$?"
dotnet test DataNet.slnx -c Release > /tmp/109-t3-t.log 2>&1; echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/109-t3-t.log
```

Expected: 0 warnings, format 0, **2269 passing**. The nine rules were measured to produce zero findings on
this tree; if one fires, it is a real finding on code that changed since, and it is fixed here rather than
exempted.

The samples are outside the solution and are built by Task 6's final verification. If a finding appears
there, the same rule applies.

- [ ] **Step 5: Commit**

```bash
git add .globalconfig Directory.Build.props
git commit -m "Make the build enforce the nine rules only the quality gate could see"
```

---

### Task 4: The drift check, in the job that already lints

**Files:**

- Modify: `.github/workflows/ci.yml:11-26` (the `Lint (markdown + C# format)` job)

**Depends on:** Tasks 2 and 3.

**Interfaces:**

- Consumes: `--check`, and its three exit codes from Task 2.

- [ ] **Step 1: Add the two steps**

In the `Lint` job, after the C# format check. Python setup mirrors the `Oracles are reproducible` job's
install line (`ci.yml:207`), including `--require-hashes`:

```yaml
      - name: Setup Python
        uses: actions/setup-python@<same pinned sha as the oracles job>
        with:
          python-version: '3.12'
      - name: Install tool dependencies
        run: pip install --only-binary :all: --require-hashes -r tools/requirements.lock.txt
      - name: Tool tests
        run: python -m pytest tools/tests -q
      # The .globalconfig mirrors a profile that lives on a server, so it can go stale
      # without anything in this repository changing. One call to sonarcloud.io per run
      # buys learning about that on the pull request that first meets it. A profile that
      # moved exits 1 and prints the diff; an API that cannot be reached exits 2 and says
      # so, because a check that goes green on silence proves nothing.
      - name: Sonar globalconfig is current
        run: |
          dotnet build src/DataNet.Fuzzy -c Release --no-incremental -f net10.0 \
            -p:ErrorLog=$RUNNER_TEMP/rules.sarif%2Cversion=2
          python tools/generate_sonar_globalconfig.py --check --error-log $RUNNER_TEMP/rules.sarif
```

Copy the `actions/setup-python` SHA from the job that already uses it — actions are pinned to full commit
SHAs here (#24), and a tag would fail review.

- [ ] **Step 2: Prove the check discriminates, both ways**

```bash
cp .globalconfig /tmp/109-globalconfig.bak
sed -i '/S3776/d' .globalconfig
python3 tools/generate_sonar_globalconfig.py --check --error-log /tmp/109-t3.sarif > /tmp/109-t4-drift.log 2>&1
echo "drift=$?"
head -12 /tmp/109-t4-drift.log
cp /tmp/109-globalconfig.bak .globalconfig
python3 tools/generate_sonar_globalconfig.py --check --error-log /tmp/109-t3.sarif > /tmp/109-t4-ok.log 2>&1
echo "restored=$?"
python3 tools/generate_sonar_globalconfig.py --check --error-log /tmp/109-t3.sarif \
  --api http://127.0.0.1:9/api > /tmp/109-t4-down.log 2>&1
echo "unreachable=$?"
```

Expected: `drift=1` with `S3776` in the diff, `restored=0`, `unreachable=2`. Report all three.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "Catch a moved quality profile on the pull request that meets it"
```

---

### Task 5: The local scanner, and the two paragraphs that describe it honestly

**Files:**

- Create: `tools/sonarqube-local/compose.yaml`
- Modify: `CONTRIBUTING.md` (the definition-of-done area around `:70`, and the `.editorconfig` paragraph
  at `:252`)
- Modify: `tools/README.md`

**Depends on:** Task 3 (so the documented loop describes the build as it now behaves).

- [ ] **Step 1: Write the compose file, pinned by digest**

```yaml
# A local SonarQube Community server, for the half of the quality gate no .NET build
# can reach: the Python rules over tools/, duplication, and coverage. Issue #109.
#
# Pinned by digest rather than by tag: `community` moves, and a scanner run whose
# analyser versions changed underneath it explains a finding that was not there
# yesterday as a code change.
services:
  sonarqube:
    image: sonarqube:community@sha256:<digest resolved in step 2>
    ports:
      - "9000:9000"
    environment:
      # Single-node Elasticsearch in a container that is thrown away after the run.
      SONAR_ES_BOOTSTRAP_CHECKS_DISABLE: "true"
    volumes:
      - sonarqube_data:/opt/sonarqube/data
      - sonarqube_extensions:/opt/sonarqube/extensions
      - sonarqube_logs:/opt/sonarqube/logs

volumes:
  sonarqube_data:
  sonarqube_extensions:
  sonarqube_logs:
```

- [ ] **Step 2: Resolve the digest and record what it cost**

```bash
docker pull sonarqube:community > /tmp/109-t5-pull.log 2>&1; echo "pull=$?"
docker image inspect sonarqube:community --format '{{index .RepoDigests 0}}'
docker image inspect sonarqube:community --format '{{.Size}}'
```

Put the digest in the compose file. Note the size — `CONTRIBUTING.md` states the cost, and a number
measured on this machine is what it states.

If the daemon is not running, `podman` is installed too and `podman compose` accepts the same file; say in
your report which one you used, because the documented command must be the one that was actually run.

- [ ] **Step 3: Run one analysis end to end, and time it**

```bash
cd /home/cyril/Documents/devs/data.net/tools/sonarqube-local && docker compose up -d
# Wait for the server rather than sleeping blind:
until curl -s http://localhost:9000/api/system/status | grep -q '"status":"UP"'; do sleep 5; done
```

Then, from the repository root, with a token created in the local server's UI
(*My Account → Security*, `local` is a fine name) exported as `SONAR_TOKEN`:

```bash
dotnet tool install --global dotnet-sonarscanner   # once, if absent
dotnet sonarscanner begin /k:"datanet-local" \
  /d:sonar.host.url="http://localhost:9000" /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.python.version="3.12" \
  /d:sonar.exclusions="tests/oracles/**,samples/DataNet.DocSnippets/Generated/**"
dotnet build DataNet.slnx -c Release --no-incremental
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
```

Record: wall-clock of the three commands, and the finding count the server reports for C# and for Python.
Those numbers are what the documentation quotes.

- [ ] **Step 4: Write the `CONTRIBUTING.md` section**

Beside the definition of done, a section named for what it is — **Before pushing: the half the build
cannot see**. It must contain, in this order: what it covers (`tools/` Python rules, duplication,
coverage); the commands from Step 3 verbatim; the measured cost from Steps 2 and 3; and, plainly, the three
ways it is **not** SonarCloud:

- the Community edition has no branch or pull-request analysis, so the verdict is over the whole project
  and never over the diff — which is the axis the real gate judges on;
- the custom `No new issue` gate and its seven conditions are not there;
- its analyser versions move independently of the server's.

End on the sentence that keeps it honest: a finding it reports is real, a clean run promises nothing.

- [ ] **Step 5: Correct the `.editorconfig` paragraph**

At `CONTRIBUTING.md:252`, the text reads "Do not reach for `.editorconfig` or `.vscode/settings.json` for
SonarLint rules." It is right about SonarLint and wrong read as a blanket ban. Make it name the tool it is
about, and point at the lever that does work for the build — the generated `.globalconfig` — without
inviting hand edits to it:

> Do not reach for `.editorconfig` or `.vscode/settings.json` **to change what SonarLint reports**.
> SonarLint reads neither […]. The **build** is a different tool: its Roslyn pass does read analyzer
> configuration files, which is how `.globalconfig` raises the rules SonarAnalyzer ships disabled. That
> file is generated from the server's profile — change the profile, or the generator, never the file.

- [ ] **Step 6: `tools/README.md`**

Add `generate_sonar_globalconfig.py` and `sonarqube-local/` to the file's existing list, in its existing
voice, with one line each. Read the file first: it enumerates the scripts, and an enumeration that grows
without its count being checked is how documentation goes stale here.

- [ ] **Step 7: Lint and commit**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" \
  "tools/README.md" "bench/README.md" > /tmp/109-t5-md.log 2>&1
echo "markdownlint=$?"
tail -3 /tmp/109-t5-md.log
git add tools/sonarqube-local CONTRIBUTING.md tools/README.md
git commit -m "Document the local run that covers Python, duplication and coverage"
```

---

### Task 6: Final verification

**Depends on:** Tasks 1-5. Nothing is committed here unless a gate fails and is fixed.

- [ ] **Step 1: Every gate, with real exit codes**

```bash
cd /home/cyril/Documents/devs/data.net
git status --porcelain                                                    # empty
dotnet build DataNet.slnx -c Release --no-incremental > /tmp/109-fv-b.log 2>&1; echo "build=$?"; tail -3 /tmp/109-fv-b.log
dotnet format DataNet.slnx --verify-no-changes > /tmp/109-fv-f.log 2>&1;   echo "format=$?"
dotnet test DataNet.slnx -c Release > /tmp/109-fv-t.log 2>&1;              echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/109-fv-t.log
python3 -m pytest tools/tests -q > /tmp/109-fv-p.log 2>&1;                 echo "pytest=$?"; tail -2 /tmp/109-fv-p.log
python3 tools/check_version_floor.py > /tmp/109-fv-v.log 2>&1;             echo "floor=$?"
```

All 0, 0 warnings, **2269 passing** across eight assemblies, and the pytest count stated.

- [ ] **Step 2: The two gates outside the solution — where the new file has never been exercised**

```bash
SCRATCH=<this session's scratchpad>
rm -rf ./artifacts "$SCRATCH/pack-packages"
NUGET_PACKAGES="$SCRATCH/pack-packages" bash -c 'for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do dotnet pack "$p" -c Release -o ./artifacts || exit 1; done'
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
rm -rf "$SCRATCH/sample-packages"
NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet run --project samples/DataNet.Sample -c Release
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES="$SCRATCH/sample-packages" dotnet build samples/DataNet.DocSnippets -c Release --no-incremental
```

`samples/` is where the new `GlobalAnalyzerConfigFiles` item travels through an import rather than being
declared, so this is the step that proves it arrived. The nine rules were measured to produce nothing there
either.

- [ ] **Step 3: The oracle drift gate, which this branch must not have moved**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python <repo>/tools/generate_oracles.py > /tmp/109-fv-gen.log 2>&1
echo "generate=$?"
cd <repo> && git status --porcelain tests/oracles/
```

Expected: empty. This branch touches no corpus, so anything here is the known flakiness — regenerate once
more before reporting it.

- [ ] **Step 4: Stop and report**

Do not push and do not open a pull request. Report the state, the three numbers Task 1 measured, the nine
rules, the canary output from Task 3 Step 3, the three exit codes from Task 4 Step 2, and the container's
measured cost, and let the user decide.

---

## Self-Review

**Spec coverage.** D1 → Task 3 Step 2. D2 → Task 2 Step 5 (`render`) and Task 3 Step 1. D3 → Task 2 Step 5
(`profile_key`, resolved per project). D4 → Task 4. D5 → Task 2 Steps 1-6. D6 → Task 2 Step 3's delta tests
(the file assertion) and Task 3 Step 3 (the demonstration); the S2245 half of the criterion is answered by
Task 1, which is where the spec sends it. D7 → Task 5. D8 → Task 1, and the "zero findings" measurement is
carried into Tasks 3 and 6 as the expectation to check rather than to assume. D9 → Task 5 Step 5.
Documentation section → Task 5 Steps 4-6; the ADR the spec makes conditional is not written, because no
task disables a rule the server enables — if Task 1 ends with `CA5394` staying in a `NoWarn`, that
condition is met and the ADR is due.

**Placeholders.** One deliberate: the image digest in Task 5 Step 1, resolved by Step 2 which immediately
follows, and the `actions/setup-python` SHA in Task 4 Step 1, which must be copied from the pinned one
already in `ci.yml` rather than guessed — writing a SHA here that CI would reject is worse than pointing at
the line to copy. The `<repo>` and `SCRATCH` placeholders in Task 6 are paths only the executing session
knows.

**Type consistency.** `active_rules`, `disabled_rules`, `delta`, `render`, `profile_key`, `analyzer_version`,
`build`, `main` are the names used in both the test file and the implementation. Exit codes `0/1/2` are
consistent across Task 2's tests, Task 4's CI step and Task 4's proof. The corpus of nine rule ids is
identical in the constraints table, Task 3 Step 1 and the self-review.
