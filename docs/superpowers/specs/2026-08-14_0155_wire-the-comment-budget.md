# 0155 — Wire the comment budget into CI, once the tree can pass it

**Issue:** [#155](https://github.com/CyrilB1531/data.net/issues/155) · **Date:** 2026-08-14 ·
**Branch:** `chore/155-wire-the-comment-budget`, stacked on `docs/154-sweep-tests-and-tools` ·
**Part of:** [#134](https://github.com/CyrilB1531/data.net/issues/134)

## Context

Last of #134's sweep issues. `tools/check_comment_length.py` shipped in #150 **deliberately unwired**,
because a guard that is red on arrival gets switched off rather than obeyed. #151, #152 and #153 are merged;
Issue #154 is in review, and this branch sits on it.

### The tree still does not pass, and the reason is not an unfinished sweep

With #154 applied, the counter reports **24 blocks past their budget, 9 of which carry a `long-comment:`
marker** — so **15 findings**. They are not leftovers from a zone somebody swept badly. They are in areas
**no zone issue ever covered**:

| where | findings | what it is |
| --- | ---: | --- |
| `src/Shared/Persistence/` | 11 | `JsonArtifact`, `Base64Numbers`, `ArtifactHeader`, `ArtifactIo` — compiled into all four packages |
| `src/DataNet.Fuzzy/` | 2 | `Fuzz.cs`, `Deduplicator.cs` |
| `src/Shared/` | 1 | `GlobalUsings.cs`, `RegexDefaults.cs` |
| `src/DataNet.Embeddings/Tokenization/BpeTokenizer.cs` | 1 | **a regression** |

Issue #134's four zones — `DataNet.Metrics`, `DataNet.Embeddings`, `DataNet.Text`, and tests/tools — did not
partition the tree. `src/Shared` and `DataNet.Fuzzy` were nobody's.

### The regression is this issue's own argument, one day old

`BpeTokenizer.cs:132` acquired a three-line comment in `708982f`, the fix for
[#160](https://github.com/CyrilB1531/data.net/issues/160), **after** #152 had swept that file. It is a good
comment — it names the corpus and the case — and it is one line over the inline budget.

Without the gate wired, a merged lot puts blocks back. It took less than a day to demonstrate.

## Decisions

### D1 — making the tree pass belongs to this lot

Fourteen findings in two uncovered areas plus one regression is not worth a fifth zone issue, and this issue
cannot land without them. It clears them, then wires the guard.

`src/Shared/Persistence/`'s eleven are the substance: they document the artifact format's own reader and
writer, and they are the same class of prose #152 and #153 handled — a claim about a format, checkable
against the round-trip tests, and long because it explains why a decision was taken rather than what the
code does.

### D2 — the guard goes where its siblings already are, in both jobs

`ci.yml` invokes `check_machine_paths.py` and `check_version_floor.py` in **two** places: the `Lint` job and
the Windows job. Both run with no dependency install, which is why those two guards live there and why this
one can join them:

```yaml
      - name: Comment budgets
        run: python tools/check_comment_length.py
```

`python` on the Windows job and `python3` on Lint, matching the sibling lines exactly rather than inventing
a third convention — the platform split `CONTRIBUTING.md` documents.

### D3 — CI reports the marker count and does not fail on its growth

`--report` prints how many blocks carry `long-comment:`. **Measured on this branch: 9, across the 806 blocks
the five sweeps touched.** That is the baseline a later reader needs.

The issue asks whether CI should fail when that number climbs. **It should not.** A marker is a judgment —
`CONTRIBUTING.md` holds it to a `#pragma warning disable`'s bar, a reason a reviewer can disagree with — and
failing a build on the count would convert a judgment into a quota. A legitimate marker must be addable
without turning a branch red, and the guard against rubber-stamping is the review reading the reason, which
is what `.github/instructions/comment_claims.instructions.md` already asks for.

What CI does gain is the number in its log, from the same `--report` call, so a reader can see it move.

### D4 — `.github/instructions/` stays outside the markdownlint glob, and this says so

Established while writing #150 and confirmed here: the glob in `ci.yml` covers `README.md`,
`CONTRIBUTING.md`, `docs/**/*.md`, `tools/README.md` and `bench/README.md`. It does **not** cover
`.github/instructions/`, whose `sonarqube_mcp.instructions.md` carries 17 unresolved violations in a merged
state — which is how the gap was found.

Widening the glob means fixing those 17 first, which is a decision about a file this lot does not otherwise
touch. **Not done here**, and recorded so the next reader finds an answer rather than the gap.

### D5 — the branch is stacked and cannot merge before #154

Issue #154 clears 316 of the 331 blocks. Wiring the guard on `main` today would fail the job it adds on its own
first run, which is exactly the outcome #150 avoided by shipping the tool unwired.

## Documentation

`CONTRIBUTING.md` and `tools/README.md` already describe the guard. What changes is that it is enforced, so
each gains one sentence saying CI runs it — no new section.

## Out of scope

Widening the markdownlint glob (D4). Failing on marker growth (D3). The 9 existing markers, each of which
carries a reason a review accepted.

## Risks

- **The tree passes today and fails tomorrow**, because a lot in flight adds a block — precisely what the
  `BpeTokenizer` regression shows. That is the gate working: the branch that adds the block fixes it.
- **A contributor meets the guard for the first time in CI** rather than locally. `CONTRIBUTING.md` lists it
  among the commands to run before pushing, and this lot's sentence says CI enforces it.
