# 0156 — Read the prose documents against each other

**Issue:** [#156](https://github.com/CyrilB1531/data.net/issues/156) · **Date:** 2026-08-14 ·
**Branch:** `docs/156-read-the-documents-against-each-other` · **Part of:** [#134](https://github.com/CyrilB1531/data.net/issues/134)

## Context

The half of #134 that is not about comments. The three code zones are swept and merged (#151, #152, #153);
this one applies the same standard to the documents, and it is the only sibling with **no counter** — the
issue says so, and measuring confirmed it.

### What a mechanical pass actually finds

The corpus is **every tracked Markdown file except `docs/superpowers/`**: 56 files, 8 704 lines, 6 486 of
them prose outside code fences. The first count for this spec said 43 files and left out `docs/migration/`'s
eight, `tools/README.md`, `bench/README.md`, `THIRD-PARTY-NOTICES.md` and `.github/instructions/`.

A candidate finder over all 56, looking for windows of consecutive normalised lines occurring in more than
one file, found **two** pairings and nothing else:

- the four-line `python` / `python3` platform passage, verbatim in `CONTRIBUTING.md` and `CLAUDE.md`;
- **31 identical table rows** — the whole classification-metrics measurement table — in `bench/README.md`
  and `docs/guides/performance.md`.

Nothing else repeats word for word. The issue's other four examples are **paraphrases**:

| instruction | files |
| --- | ---: |
| the `python`/`python3` split | 2 — **verbatim** |
| the oracle generator's neutral directory | 2 — paraphrased |
| `Blocked import of regex from current working directory` | 2 — paraphrased |
| `DataNetUseProjectRefs` | 3, including ADR 0012 — paraphrased |
| "warnings are errors" | **5**, including ADRs 0015 and 0019 — paraphrased |
| *(controls)* "read the test count", "the oracle drift gate is flaky" | 1 each |

Writing that finder cost two false results before it was right — a sentence splitter that read `24.04` as a
sentence end, then a regex fence-stripper that mispaired on `CONTRIBUTING.md`'s 35 fence markers and ate the
prose it was meant to skip. Both made it report **zero duplicates**, confidently. The finder is a throwaway
in the scratch directory, not a tool this repository ships; its output is a candidate list, and the reading
is the work.

**No unclosed code fence exists** in any of the 56 files — checked while diagnosing the second bug.

## Decisions

### D1 — the target is contradiction, not duplication

Two files saying the same thing is not a defect. **Two files saying it differently is**, because the next
correction lands in one wording and leaves the other standing — which is how three of the eight false claims
found on 2026-08-13 happened.

So the rule this lot enforces is: **duplicate verbatim, or point. Never paraphrase.** A verbatim copy is
visibly the same text and a reviewer can diff it; two paraphrases of one rule drift apart silently and
become a contradiction nobody wrote on purpose.

The four-line `python`/`python3` passage therefore **stays in both files as it is**.

### D1b — misplacement is what causes contradiction, so it is fixed first

A paragraph in the wrong document is not untidiness. It is where nobody looks, so the next person needing
that fact **writes it again somewhere else, in their own words** — and now the corpus holds two paraphrases
that will disagree the first time one is corrected. D1 forbids the paraphrase; this decision removes the
condition that produces it.

It also has the plainer benefit: one place to look, and one place to correct.

The 31 shared rows are the case in point, and the evidence is one night old. #140 re-measured `median_ae`,
wrote the new figure into `docs/guides/performance.md`, and never touched `bench/README.md`. It escaped
contradiction only because `median_ae` is not among the README's rows — the mechanism was in place and the
subject happened to be absent.

Each document has a subject, and content whose subject is another document's moves there and leaves a link:

| document | its subject |
| --- | --- |
| `bench/README.md` | **how to measure** — the harness, the corpus, the commands |
| `docs/guides/performance.md` | **what was measured** — every number, with its machine and its window |
| `tools/README.md` | what each tool does and how to run it |
| `CONTRIBUTING.md` | the process a contributor follows |
| `CLAUDE.md` | what a session needs to be productive, and the traps that cost time |
| `docs/equivalence.md` | the Python call to C# counterpart mapping, with each divergence |
| `docs/migration/` | what is delegated to another .NET library, and why |
| `CHANGELOG.md` | what changed, per release |
| `docs/decisions/` | a decision, with its options and its loser |
| root `README.md` | what the project is, and where to go next |

The benchmark numbers therefore live in `docs/guides/performance.md`, and `bench/README.md` links to it.

**The map itself is published**, in the root `README.md` and in `CLAUDE.md` — a reader deciding where to
write, and a session deciding where to look, both need it, and neither will consult a spec. It carries three
columns: the document, **where its content comes from**, and what it is for. Where the content has a
generator or a measurement behind it — `performance.md` from a benchmark run on a named machine,
`docs/decisions/README.md` from the ADRs' own status lines, `THIRD-PARTY-NOTICES.md` from the packages'
licences — the map says so, because that is what tells a reader whether to edit the document or its source.

It appears **verbatim in both files**, not paraphrased, per D1. That is the rule applied to this lot's own
output rather than an exception to it.

### D2 — `CLAUDE.md` and `CONTRIBUTING.md` keep their present division

They have different audiences — a session and a contributor — and the split as it stands is not this lot's
to redraw. Where both must state a rule, they state it in the same words or one points at the other.

### D3 — `docs/decisions/` gains the README it does not have

Thirty-three ADRs, every one carrying a `**Status:**` line, and **no index**. A reader looking for "is this
still the decision?" has to open thirty-three files, and the relationships between them are invisible:

- 31 read `accepted`;
- `0013` reads `accepted, superseded in part by 0014` — the only stated relationship;
- `0004` reads `single-word and blocked shipped`, which is a sentence rather than a status.

`docs/decisions/README.md` lists every ADR with its number, title, status, date and what it supersedes,
updates or is updated by. It is generated by reading the files, not by hand-maintaining a second copy of
their titles — but it is **committed prose**, not a generated artifact, because the relationships it records
are a reading.

**No ADR's body is rewritten.** An ADR is a dated record, and the convention already in the tree is ADR
0022's `> **#119 and #120 update:**` block, which names what has gone stale and leaves the original
paragraph standing. Where this lot finds an ADR contradicted by a later one, it adds such a block. It does
not edit history.

`0004`'s status becomes a status — the sentence it carries moves into the body.

### D4 — the third defect is the one with no signal, and it spans every document

A statement that was true and no longer is. There is nothing mechanical to find it; it is looked for where
behaviour moved. **In every prose file, not only `docs/equivalence.md`**: the root `README.md`, `tools/README.md`,
`bench/README.md`, `docs/migration/README.md`, `CONTRIBUTING.md`, `CLAUDE.md`, `CHANGELOG.md` and the guides
all make claims that a lot could have falsified.

The recent lots are the map of where to look: #121 and #149 changed what `LoadBpe` accepts and what `Decode`
does, #140 changed the median's cost, #143 and #145 changed the pre-tokenizer, #150 added a rule every
document now has to agree with, and #151-#153 moved prose into these very files.

### D5 — the documents are made readable, and readability is given criteria rather than taste

Four checks about content:

- **no contradiction** between any two statements in the corpus;
- **no paraphrase** of a rule stated elsewhere — verbatim or a pointer;
- **nothing in a document whose subject belongs to another**, per D1b's table;
- **every pointer resolves**: a named file exists, a named ADR says what it is cited for, a named command
  runs.

And five about form, because a document nobody can read is not saved by being correct. Each is observable,
so a reviewer can disagree with a specific edit rather than with a taste:

- **a document opens by saying what it is and who it is for**, in one or two sentences;
- **a heading answers a question a reader arrives with**, so they can skip to it;
- **a paragraph makes one point** — one carrying three becomes three;
- **a fact a reader must act on is not buried mid-paragraph**: it becomes a list item, a table row, or the
  first sentence;
- **no sentence needs re-reading to parse** — in practice, more than two subordinate clauses or more than
  one aside between dashes.

**A readability edit never changes a claim.** It splits, promotes, re-orders or deletes a repetition; it does
not restate. That is what keeps the diff reviewable, and it is enforced by making those edits their own
commits, separate from the ones that change meaning — a reviewer reads them knowing no fact moved.

## Documentation

`docs/decisions/README.md` is created. Everything else is edited in place.

## Out of scope

The comment zones (#151, #152, #153 — all merged) and the tests/tools/samples zone (#154). Wiring the
comment counter into CI (#155). `docs/superpowers/`, which is process scratch rather than published prose.
Style rewriting, per D5.

## Risks

- **Rewriting an ADR to agree with a newer one.** That destroys the reasoning the record exists to preserve.
  D3 forbids it; the update-block convention is the answer.
- **A "contradiction" that is two subjects sharing a phrase.** Each candidate is read in context before it is
  called one, and the finder's output is a candidate list rather than a defect list.
- **The corpus moves under the lot.** Six pull requests merged into these files in the last day. Fetch before
  pushing, and re-run the finder at the end rather than trusting the list from the start.
