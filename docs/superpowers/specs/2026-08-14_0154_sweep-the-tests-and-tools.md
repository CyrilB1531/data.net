# 0154 — Sweep the tests and tools, where a comment asserts what the corpus proves

**Issue:** [#154](https://github.com/CyrilB1531/data.net/issues/154) · **Date:** 2026-08-14 ·
**Branch:** `docs/154-sweep-tests-and-tools` · **Part of:** [#134](https://github.com/CyrilB1531/data.net/issues/134)

## Context

Fourth and largest zone of #134's sweep, after `DataNet.Metrics` (#151), `DataNet.Embeddings` (#152) and
`DataNet.Text` (#153), all merged. The issue argues this is the *worse* half rather than the lesser one, and
the argument holds: a comment in `src/` asserts what the reference does, while a comment in `tests/` asserts
**what the corpus proves** — and this repository's whole conformance argument is that the corpora prove
things. A false claim there misdescribes the evidence a reviewer reaches for.

### Measured on `main` at `0871da8`

**316 blocks**, not the issue's 249: it counted before three merges and left `bench/` out.

| zone | blocks | | zone | blocks |
| --- | ---: | --- | --- | ---: |
| `tools/` scripts | **83** — 57 in `generate_oracles.py` | | `tools/tests/` | 22 |
| `tests/DataNet.Metrics.Tests` | 78 | | `samples/` | 20 |
| `tests/DataNet.Embeddings.Tests` | 67 | | `tests/DataNet.Text.Tests` | 17 |
| `bench/` | 28 | | `tests/DataNet.Fuzzy.NetStandard.Tests` | 1 |

`tools/count_cited_claims.py` over the whole zone: **112 blocks name a reference library and 33 cite
something a reader can open — 29%.** That is three to five times `src/`'s rate before its sweeps, and it has
a cause: a test that replays a corpus cites it by nature.

### The escape route the issue proposes does not exist

The issue says a Python docstring is not a comment block, and that this "is the escape route for most of
`generate_oracles.py`'s 82". Measured: **none of its 57 blocks sits above a `def` or a `class`.** They sit
elsewhere:

| where | blocks | what they explain |
| --- | ---: | --- |
| above a module constant | 23 | why a fixture holds what it holds |
| above a statement | 11 | why that line is there |
| **inside a literal** | 9 | what a single corpus case is for |
| section banners | 5 | — |
| above an assertion | 5 | what the assertion protects |

So the route has to be found rather than assumed, which is D2.

## Decisions

### D1 — one task per zone, largest first

Seven zones, and the two biggest are half the work. `tools/generate_oracles.py` alone holds 57 blocks and is
its own task: it is the file every corpus in the repository comes from, and the file whose comments explain
why each corpus contains what it contains.

### D2 — the routes are the consuming docstring and `tools/README.md`, not the corpus

For `generate_oracles.py`, measured against where its blocks actually are:

- **the docstring of the function that consumes the constant.** Each of the 23 constants feeds one or two
  generators, and this file already explains its corpora in exactly those docstrings.
- **`tools/README.md`**, which already carries a `## generate_oracles.py` section. Conventions and traps
  belong there — the map from #156 applied to this file.
- **cutting**, for what restates what the code shows.

**Not the corpus.** Explaining a case inside its own fixture would mean adding a field, which means
regenerating, which means bytes move — a behaviour change wearing a sweep's clothes. The nine per-case
comments shorten or move to the generator function's docstring instead.

### D3 — in `tests/`, a comment that names what a corpus proves keeps its citation

A test comment saying "measured against `tokenizers` 0.23.1, case 17" is the cheap tier already done. What
this lot removes is the block that **restates the assertion below it**, which is the common shape in
`DataNet.Metrics.Tests`' 78 and `DataNet.Embeddings.Tests`' 67.

Where a comment explains **why a case exists** — the shape the test was written to catch — that is not a
restatement and it survives, shortened, with the corpus case named. A test whose reason for existing is
deleted is a test the next person deletes as redundant.

### D4 — `samples/` and `bench/` answer to a different reader

`samples/` is read by someone learning the API and is compiled against the published packages (ADR 0009);
`bench/` is read by someone reproducing a measurement. Their comments explain *the example* and *the
protocol*, not the library's behaviour, so the triage's tier 3 — "nothing checks it, cut it" — applies more
often there. `bench/`'s claims about methodology point at `docs/guides/performance.md`, which #156 has just
made the single home for measurements.

### D5 — the budgets, and what is already exempt

Two lines for an inline comment, eight lines of prose for XML documentation. **The reason above a `#pragma`
is exempt** since #151, and a Python docstring is not a comment block at all. Neither is to be "tidied" into
scope.

### D6 — no behaviour changes, and the corpora do not move

`tests/oracles/` stays byte-identical, and the suite stays at **3 185 passing, 0 failed**. This is the
constraint that makes a sweep of the *test* zone safe to review: if a corpus byte moves, the lot changed
evidence rather than prose.

## Documentation

`tools/README.md`'s `generate_oracles.py` section takes what leaves that file. No ADR, and no ADR number.

## Out of scope

The three merged zones. Wiring the counter into CI (#155), which waits on this one. `docs/superpowers/`.

## Risks

- **Deleting the reason a test exists.** D3 is the mitigation, and the failure mode is delayed: the test
  survives this lot and is deleted as redundant six months later.
- **Moving a case's explanation into the corpus.** Forbidden by D2 for a reason that is easy to forget under
  time pressure — it regenerates the corpus, and a sweep must not move evidence.
- **`generate_oracles.py` is edited by every lot that adds a corpus.** Two sessions work this repository;
  fetch before pushing, and expect to rebase.
