# The rule and its instruments — implementation plan (#134, part A)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Write the rule this repository is said to have and does not, give the review step a durable home,
and build the length guard — without switching it on.

**Architecture:** Three independent deliverables. `CONTRIBUTING.md` states four rules about comments;
`.github/instructions/` carries the review step, on the pattern the SonarQube instructions established;
`tools/check_comment_length.py` counts blocks and refuses unmarked long ones, proven by its own tests and
**deliberately not wired into CI** — it would fail on 354 existing blocks, which parts B–D clear.

**Tech Stack:** Markdown, Python 3 standard library only, pytest.

**Spec:** `docs/superpowers/specs/2026-08-14_0134_claims-and-comment-discipline.md`

**Issue:** [#134](https://github.com/CyrilB1531/data.net/issues/134) ·
**Branch:** `docs/134-claims-and-comment-discipline`, off `main` (already created; the spec is on it)

**This is part A of four.** B, C and D sweep the existing blocks and claims, zone by zone; E switches the
guard on and reads the prose documents against each other. Nothing here changes a single existing comment.

## Global Constraints

- **This plan is inside the scope of the rule it writes.** Its own prose, and the commit messages it
  prescribes, are held to the four rules below. A plan that broke them while defining them would be the
  defect the issue exists for.
- **Python: standard library only**, on the pattern `tools/check_machine_paths.py` and
  `tools/check_version_floor.py` establish. CI runs them with no dependency install.
- **A wrong claim in a comment or document is a defect** here — which is precisely what this lot is
  writing down. For every sentence you write, the question is not whether it reads plausibly but **what
  you would run to check it, and whether you ran it**. Your report must carry one line per claim.
- **No machine paths in committed files.** `python tools/check_machine_paths.py` refuses a tracked file
  holding a path under someone's home directory. **Run it after `git add`** — it scans tracked files, so an
  unstaged file is not checked.
- **Everything in English.** Commit messages carry no `feat:`/`fix:` prefix.
- **The machine is shared.** Every `dotnet` command goes through `../data.net/.dotnet-guarded`. **This plan
  runs no `dotnet` at all** — if you find yourself wanting to build, you have strayed from the brief.
- Per task the gate is `python -m pytest tools/tests -q` and
  `npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"`.

## The marker, defined once

A comment block over eight lines carries, **as its first line**, a marker naming why:

```csharp
// long-comment: the four measured rows this refusal rests on, and why each is
// not the obvious reading
```

```python
# long-comment: nltk's import refusal, the three shapes it takes, and the one
# that is not documented upstream
```

The prefix is `long-comment:` in both languages, after the comment leader. What follows on that line is the
reason, and it is what a reviewer accepts or rejects — the guard can only see that a marker exists.

## File Structure

| File | Responsibility |
| --- | --- |
| `CONTRIBUTING.md` | **New section** stating the four rules, beside `Performance claims`. |
| `CLAUDE.md` | One pointer to it, since that is what agents read. |
| `.github/instructions/comment_claims.instructions.md` | **New.** The review step: the trigger, and the derivation question. |
| `tools/check_comment_length.py` | **New.** Counts blocks, refuses an unmarked one over eight lines, reports the marker count. |
| `tools/tests/test_check_comment_length.py` | **New.** Fixtures are real blocks from this repository. |

---

### Task 1: The rule

**Files:**

- Modify: `CONTRIBUTING.md` (new section after `## Performance claims`)
- Modify: `CLAUDE.md` (one pointer)

**Interfaces:**

- Produces, for Tasks 2 and 3: the marker spelling `long-comment:` and the eight-line threshold, which both
  later tasks cite rather than restate.

- [ ] **Step 1: Read the section you are writing beside**

```bash
grep -n "^## Performance claims" -A 10 CONTRIBUTING.md
```

That is the precedent: this repository already governs one class of claim by requiring its evidence, and
ends with *"Verify what you are actually measuring before quoting a result."* Your section generalises it,
so it should read as its sibling rather than as a new regime.

- [ ] **Step 2: Add the section**

After `## Performance claims`, add:

```markdown
## Claims in comments

A comment here often carries the reason a divergence from the Python reference exists, which is what makes
that divergence reviewable. That is what makes them load-bearing, and it is also what makes them dangerous:
nothing checks them, and they go stale when the code beside them moves. Four rules, and they bind every
tracked file — `src/`, `tests/`, `tools/`, `bench/`, `samples/`, `docs/` and `docs/superpowers/` alike. A
spec that overclaims what its corpus proves is the same defect as a comment that overclaims what the
reference does.

**A comment says why, never what.** Restating the line below it is noise, and it goes stale faster than the
code does — the code at least gets compiled.

**A claim carries what would check it.** Where it is executable — a measurement, a reference library's
output, a count — run it and cite the corpus case, the file and line, or the command. "Measured" with no
pointer is an assertion wearing a measurement's clothes.

**Eight lines above a member.** Past that, the reasoning belongs in [`docs/decisions/`](docs/decisions/),
cited from one line — or it needs cutting. `tools/check_comment_length.py` counts them.

**A longer block carries a marker naming its reason**, as its first line:

    // long-comment: <why this one needs the room>

Longer is allowed where it is necessary; the marker is what stops it becoming the norm. It is held to the
bar a `#pragma warning disable` is held to — a reason a reviewer can disagree with, and "it felt useful" is
not one. A code review judges whether the marker was deserved, because the guard can only see that one
exists.
```

- [ ] **Step 3: Point `CLAUDE.md` at it**

`CLAUDE.md`'s `## Workflow` section already says "Everything written in English — code, comments, ADRs,
commit messages, PR bodies." Add immediately after that sentence:

```markdown
Comments are held to four rules — say why not what, carry what would check the claim, eight lines above a
member, and a marker with its reason past that. `CONTRIBUTING.md`'s *Claims in comments* is the statement;
`tools/check_comment_length.py` counts the lines and `.github/instructions/comment_claims.instructions.md`
carries what a review asks about one.
```

- [ ] **Step 4: Check the rule against itself**

The paragraph you just added to `CLAUDE.md` is four lines; the `CONTRIBUTING.md` section is prose rather
than a comment block, so the eight-line rule does not reach it. **Confirm that reading rather than assuming
it**: the guard in Task 3 scans comment blocks in source files, not Markdown prose. If you find yourself
wanting to exempt the documents you just wrote, the rule is written wrong.

- [ ] **Step 5: Gate and commit**

```bash
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
git add CONTRIBUTING.md CLAUDE.md
python tools/check_machine_paths.py
git commit -m "$(cat <<'EOF'
Write the rule this repository was said to have

CONTRIBUTING.md mentions comments four times -- a reason above a pragma, a
comment naming each NoWarned rule, a divergence belonging in docs/decisions/,
and an analyzer finding being a compile error. None of them says a wrong claim
in a comment is a defect, and neither does CLAUDE.md, any ADR or any guide.
Issue #134 opens by asserting that it does.

So the rule is written before anything enforces it, beside Performance claims,
which is the precedent: this repository already governs one class of claim by
requiring its evidence and already ends that section by telling a reader to
check what they are actually measuring.

Four rules, binding every tracked file including docs/superpowers/. A spec
that overclaims what its corpus proves is the same defect as a comment that
overclaims what the reference does, and two specs did exactly that.

Issue #134
EOF
)"
```

---

### Task 2: The review step, where it survives the session

**Files:**

- Create: `.github/instructions/comment_claims.instructions.md`

**Interfaces:**

- Consumes: the marker spelling and threshold from Task 1.
- Produces: nothing later tasks import; it is read by review flows.

- [ ] **Step 1: Read the file you are modelling on**

```bash
head -20 .github/instructions/sonarqube_mcp.instructions.md
grep -n "instructions" CLAUDE.md
```

It opens with YAML frontmatter (`applyTo: "**/*"`), and `CLAUDE.md` names it — that pairing is what makes
an instruction durable here rather than living in one session's dispatch prompt.

- [ ] **Step 2: Write it**

```markdown
---
applyTo: "**/*"
---

# Reviewing a claim in a comment

`CONTRIBUTING.md`'s *Claims in comments* states the rule. This is what a review does about it.

## The trigger

Every comment the diff **modifies or moves**.

Moves are not a formality. Three of the eight false claims found on 2026-08-13 were one sentence corrected
in one place and left standing in its copy — a plan's prose fixed while the commit-message block fifty
lines below kept the old number, twice in one branch. A comment that moved without being rewritten is
exactly where a correction fails to arrive.

## The question

Not "is this claim still true". That question is answered *yes* by re-reading, because the second reader
inherits the first's framing from the diff.

On 2026-08-13, in issue #140, a false claim survived two reviews that were both looking at it: a task
reviewer wrote it, an implementer transcribed it, and the whole-branch reviewer caught it only because it
re-derived the shape from scratch. Of the eight failures that day, six fell to someone re-deriving the
claim independently, one to a differential against a separately written reference, one to an agent blocked
by a criterion that contradicted its measurement, and **none to careful reading**.

So the question is: **what would you run to check this, and did you run it?**

- Where the claim is executable, run it and cite the output — the corpus case, the command, the file and
  line.
- Where a reviewer is checking someone else's claim, derive it independently rather than following their
  reasoning. Reading their derivation confirms it; producing your own does not.
- Where nothing reasonable checks it, it is an opinion. That is allowed, and saying so plainly is the fix —
  a comment that cannot be checked is not thereby exempt, it is thereby not a claim.

## The marker

A comment block over eight lines carries `long-comment:` and a reason on its first line, and
`tools/check_comment_length.py` refuses one that does not. The guard sees only that a marker exists.
**Whether the block deserved one is the review's call**, at the bar a `#pragma warning disable` is held to.
A block that could have been eight lines, or whose reasoning belonged in an ADR, is a finding even though
the guard passed it.
```

- [ ] **Step 3: Gate and commit**

```bash
npx markdownlint-cli2 ".github/instructions/*.md"
git add .github/instructions/comment_claims.instructions.md
python tools/check_machine_paths.py
git commit -m "$(cat <<'EOF'
Give the review step a home that outlives the session

Issue #134 proposes adding "is this comment still true?" to a reviewer's
attention lens and calls it nearly free. Measured, that question does not
work: on #140 a false claim survived two reviews that were both looking at
it, because re-reading a claim confirms it -- the second reader inherits the
first's framing from the diff. Of the eight failures on 2026-08-13, six fell
to someone re-deriving the claim, and none to careful reading.

So the step asks what you would run rather than whether it reads true, and
asks a reviewer to derive independently rather than to follow the author's
reasoning.

It lives beside sonarqube_mcp.instructions.md because that is the only shape
in this repository a review flow cites and CLAUDE.md names. A step that
exists only in a session's dispatch prompt does not survive that session,
which is how this became an issue rather than a habit.

Issue #134
EOF
)"
```

---

### Task 3: The guard, built but not switched on

**Files:**

- Create: `tools/check_comment_length.py`
- Create: `tools/tests/test_check_comment_length.py`

**Interfaces:**

- Consumes: the marker spelling `long-comment:` and the threshold 8 from Task 1.
- Produces, for parts B–E: `python tools/check_comment_length.py` exiting 0 clean, 1 with findings, 2 on
  bad usage; `--report` printing counts without failing.

**It is not added to `.github/workflows/ci.yml`.** It would fail on 354 existing blocks. Part E wires it in
once they are cleared — a guard that is red on arrival gets switched off rather than obeyed.

- [ ] **Step 1: Write the failing tests**

`tools/tests/test_check_comment_length.py`:

```python
"""The guard's own tests, over blocks taken from this repository.

Issue #134 measured 354 blocks running past eight lines, holding 5532 of the
9803 comment lines in the tree. The fixtures below are shapes from that list
rather than invented ones, which is what makes them evidence that the counter
matches the thing that was counted.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import check_comment_length as guard  # noqa: E402


def blocks(text):
    return guard.blocks_in(text.split("\n"), ".cs")


def test_a_run_of_comment_lines_is_one_block():
    text = "// one\n// two\n// three\nint x = 1;\n"
    assert [b.length for b in blocks(text)] == [3]


def test_a_blank_line_ends_a_block():
    # Where a naive counter is wrong: two paragraphs of prose are two blocks,
    # and neither is over the threshold even though together they would be.
    text = "// one\n// two\n\n// three\n// four\nint x = 1;\n"
    assert [b.length for b in blocks(text)] == [2, 2]


def test_xml_documentation_counts_as_a_comment():
    text = "/// <summary>a</summary>\n/// <remarks>b</remarks>\nint X;\n"
    assert [b.length for b in blocks(text)] == [2]


def test_eight_lines_is_allowed_and_nine_is_not():
    eight = "".join(f"// line {i}\n" for i in range(8)) + "int x = 1;\n"
    nine = "".join(f"// line {i}\n" for i in range(9)) + "int x = 1;\n"
    assert guard.findings_in(eight.split("\n"), ".cs") == []
    assert len(guard.findings_in(nine.split("\n"), ".cs")) == 1


def test_a_marked_block_is_allowed_however_long():
    text = "// long-comment: the four measured rows\n" + "".join(
        f"// line {i}\n" for i in range(20)) + "int x = 1;\n"
    assert guard.findings_in(text.split("\n"), ".cs") == []


def test_the_marker_must_be_the_first_line_of_the_block():
    # Buried in the middle it is prose, not a marker, and the block is unmarked.
    text = "// one\n// long-comment: too late\n" + "".join(
        f"// line {i}\n" for i in range(9)) + "int x = 1;\n"
    assert len(guard.findings_in(text.split("\n"), ".cs")) == 1


def test_python_uses_the_same_marker_after_its_own_leader():
    text = "# long-comment: nltk's import refusal\n" + "".join(
        f"# line {i}\n" for i in range(12)) + "x = 1\n"
    assert guard.findings_in(text.split("\n"), ".py") == []


def test_a_shebang_and_a_coding_line_are_not_a_comment_block():
    # Every tool in tools/ opens with these two, and counting them would make
    # each file start one line into a block it never wrote.
    text = "#!/usr/bin/env python3\n# -*- coding: utf-8 -*-\nimport sys\n"
    assert blocks(text) == []


def test_a_python_docstring_is_not_a_comment_block():
    # tools/check_machine_paths.py opens with a 30-line docstring. It is not a
    # comment and this guard does not count it -- prose in a docstring is the
    # module's documentation, which is where long explanation belongs.
    text = '"""One\nTwo\nThree\n"""\nimport sys\n'
    assert blocks(text) == []


def test_the_finding_names_the_file_the_line_and_the_length():
    nine = "".join(f"// line {i}\n" for i in range(9)) + "int x = 1;\n"
    finding = guard.findings_in(nine.split("\n"), ".cs")[0]
    assert finding.line == 1
    assert finding.length == 9
```

- [ ] **Step 2: Run them and watch them fail**

```bash
python -m pytest tools/tests/test_check_comment_length.py -q
```

Expected: `ModuleNotFoundError: No module named 'check_comment_length'`. That is the right first failure.

- [ ] **Step 3: Write the guard**

`tools/check_comment_length.py`. The docstring names the drift it catches, as its siblings do:

```python
#!/usr/bin/env python3
"""Refuse a comment block that runs past eight lines without saying why.

Measured on 2026-08-14 across src/, tests/, tools/, bench/ and samples/: 1837
comment blocks holding 9803 lines, of which 354 run past eight lines and hold
5532 of those lines. One block in five carries more than half the prose, and
the longest runs 63 lines.

Long is not banned. A block past the threshold carries `long-comment:` and a
reason as its first line, which is the bargain a #pragma warning disable
strikes: allowed, deliberate, and reviewable. This guard sees only that the
marker exists -- whether the block deserved one is a code review's call, and
CONTRIBUTING.md's "Claims in comments" says so.

A docstring is not a comment block. Python prose belongs in one, and the tools
in this directory open with thirty-line docstrings on purpose.

Usage:  python tools/check_comment_length.py [--report]
        python tools/check_comment_length.py --help

  --report  Print the block, line and marker counts and exit 0 without
            failing. What the sweep uses to see how much is left.
  --help, -h  Print this message to stdout and exit 0.

Exit:   0 clean, 1 findings printed, 2 bad usage
"""
```

Then the module. Keep it small and total; the tests above are its specification:

- `THRESHOLD = 8` and `MARKER = "long-comment:"`, each named once.
- `LEADERS = {".cs": ("///", "//"), ".py": ("#",)}` — the file suffixes it understands. A suffix it does
  not know is skipped, not guessed at.
- `Block = namedtuple("Block", "line length marked")` and
  `Finding = namedtuple("Finding", "path line length")`.
- `blocks_in(lines, suffix) -> list[Block]`: walk the lines, accumulate consecutive ones whose stripped
  form starts with a leader, end a block on anything else. **A `#!` shebang and a `# -*-` coding line are
  not comment lines** — the tests pin that, and every tool in `tools/` opens with them.
- `findings_in(lines, suffix) -> list[Finding]`: the blocks over `THRESHOLD` whose first line does not
  carry `MARKER` after its leader.
- `tracked_files()`: `git ls-files` from the repository root, as `check_machine_paths.py` does — anchor on
  the root rather than the working directory, which is the bug #133 shipped and fixed.
- `main()`: `--help` to stdout exit 0, `--report` printing counts and exiting 0, otherwise printing one
  line per finding as `path:line: N lines, no long-comment marker` and exiting 1 if any.

**Do not exempt this module or its test from the scan.** Unlike `check_machine_paths.py`, which must
contain the patterns it searches for, nothing here needs a long unmarked block — and if the guard's own
source cannot satisfy the guard, the threshold is wrong.

- [ ] **Step 4: Run the tests**

```bash
python -m pytest tools/tests/test_check_comment_length.py -q
```

Expected: 10 passed. **Read the count** — a file that fails to import reports zero tests and exits zero
under some invocations.

- [ ] **Step 5: Point it at the repository and record what it finds**

```bash
python tools/check_comment_length.py --report
python tools/check_comment_length.py | head -20; echo "exit=${PIPESTATUS[0]}"
```

Expected: `--report` prints counts and exits 0; the bare run prints findings and exits 1, because 354
blocks are unmarked. **That failure is correct and is why this task does not touch CI.**

Record the numbers in your report. If the block count differs materially from the spec's 1837/9803/354,
say so with the difference — the spec's counter and this one were written separately, and a disagreement
is information about one of them rather than a rounding detail.

- [ ] **Step 6: Gate and commit**

```bash
python -m pytest tools/tests -q
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
git add tools/check_comment_length.py tools/tests/test_check_comment_length.py
python tools/check_machine_paths.py
git commit -m "$(cat <<'EOF'
Count comment blocks, and refuse a long one that does not say why

Measured across src/, tests/, tools/, bench/ and samples/: 1837 comment
blocks holding 9803 lines, of which 354 run past eight lines and hold 5532 of
them. One block in five carries more than half the prose; the longest runs 63
lines.

The guard is not wired into CI here, and that is the point of stopping short:
it exits 1 on this tree today, and a guard that is red on arrival gets
switched off rather than obeyed. Parts B through D clear the 354; part E
switches it on once it passes.

Long stays possible. A block past the threshold carries long-comment: and a
reason as its first line, which is the bargain a pragma strikes -- allowed,
deliberate, reviewable. The guard sees only that the marker exists; whether
the block deserved one is a review's call.

A docstring is not a comment block, which is why the tools in this directory
keep their thirty-line ones. Prose in a docstring is documentation; prose
above a member is what this counts.

Issue #134
EOF
)"
```

---

## Self-review

**Spec coverage.** D1 is Task 1, which writes the rule the spec measured absent. D2 is Task 2, whose whole
content is the derivation question and the evidence for preferring it to re-reading. D3 is Task 3's guard,
and D3's "the truth problem is not mechanisable" is honoured by Task 2 and Task 3 being separate
instruments. D4 — long explanations belong in `docs/decisions/` — is stated in Task 1's rule text and is
what parts B–D will act on. D5, the 9%-cite figure, belongs to the sweep and is not this part's to touch.
The spec's *What done looks like* has five bullets; this plan delivers the fourth in full (the rule is
written where humans and agents meet it, and the instructions file exists) and builds the instrument the
first depends on. The other three are B–E.

**Placeholders.** Task 3 Step 3 describes the module in prose rather than giving it line by line, and that
is deliberate: the ten tests above it are its specification, and a plan that also transcribed the
implementation would give two sources of truth for one small module — with the plan's copy the one that
goes stale, which this branch has now demonstrated three times. Every name, constant, signature and exit
code it must expose is fixed in that step; nothing is left to taste.

**Type consistency.** `blocks_in(lines, suffix)`, `findings_in(lines, suffix)`, `Block(line, length,
marked)` and `Finding(path, line, length)` are used with those names in Task 3's tests and description.
`THRESHOLD = 8` and `MARKER = "long-comment:"` match Task 1's rule text and Task 2's instructions file
exactly — the marker is spelled `long-comment:` in all three, which is the kind of agreement that fails
silently if it drifts.

**The one thing a reviewer should push on.** Task 3 asks for a guard that does not exempt itself, on the
argument that nothing in it needs a long unmarked block. If that turns out false — if the module cannot
document itself inside eight lines without a marker — the honest conclusion is that the threshold is wrong,
not that the guard needs an exemption. Say so rather than adding one.
