# 0077 — The keyword extractors take their oracles' lists, and not their own

**Status:** accepted · **Date:** 2026-09-04

## Context

[#525](https://github.com/CyrilB1531/lodestar/issues/525) ships `Rake` and `TextRank`
(`Lodestar.Text.Keywords`) against `rake-nltk` and `summa`, and `Mmr` (`Lodestar.Embeddings.Search`)
against `keybert`'s selection step. All three are unsupervised — no model weights, no training —
which is what makes them reachable from a `netstandard2.0` core package at all. Measuring them
against their references surfaced five divergences worth recording rather than silently matching or
silently working around.

## Decision

Five divergences, each accepted rather than reproduced or hidden:

**1. The stop-word list is the oracle's own, never downloaded.** `rake_nltk.Rake()` with no
arguments downloads `nltk`'s `stopwords` corpus on first use; `RakeOptions.StopWords` and
`TextRankOptions.StopWords` default to `StopWords.English`, already compiled into the assembly —
the same list [decision 0010](0010-stop-word-list-provenance.md) already gives the vectorizers, for
the same reason: no Python at runtime, and no network call a test can flake on. A caller who wants
byte-for-byte parity with a specific `rake-nltk` run supplies that run's own list through
`RakeOptions.StopWords`; the oracle generator does exactly that (`tools/generate_oracles.py`,
`generate_keywords_rake`), which is what makes the frozen corpus a parity claim at all rather than a
comparison between two different stop-word lists wearing the same name.

**2. TextRank's parity is numerical, not exact, and is checked at the same `1e-9` every oracle
corpus in this repository is.** summa solves its co-occurrence graph's eigenproblem outright,
through `scipy.linalg.eig`; the internal graph
[`TextRank.Extract`](../reference/text/keywords/textrank-extract.md) builds reaches the same ranking
by power iteration, because a from-scratch general eigensolver is a large, easy-to-get-subtly-wrong
undertaking for one call site, and the algorithm's own literature already describes it as an
iterative method. `TextRankOracleTests` asserts `1e-9`, not tighter — a looser bound would have let a
genuine dominance divergence (below) hide inside the tolerance during development, rounding a
wrong eigenvector's coordinates to look like the right one's. Replaying `WordGraph.Rank`'s loop by
hand against the frozen corpus measures the actual agreement well past that: under `4.48e-13` on
the loosest case (`two_sentences`, phrase `numbers`) — descriptively true of this build, but a
measurement, not a gate. `TextRankOptions.Tolerance`'s `1e-12` default is a different quantity again:
it is the power iteration's own convergence delta, the change between successive iterates that stops
the loop, not the distance from the fixed point summa's direct solve reaches — at `Damping = 0.85`
the latter runs about `6.7`× the former, so reading `Tolerance` as the parity figure conflates the
two.

**3. TextRank's internal graph ranks by the dominant left eigenvector; summa does not check that
its own does — and which column summa reads is not reproducible.**
`scipy.linalg.eig`'s `pagerank_weighted_scipy` path returns eigenvalues and eigenvectors in no
particular order, and `summa` reads column 0 unconditionally (`vecs[:, 0]`), trusting it to be the
dominant one — true for a strongly connected graph, and not guaranteed for one that is not. Two
documents drafted for the oracle corpus measured that failure directly: the abstract from Rose et
al. — the RAKE paper's own worked example, already the source of `Rake`'s doc-page examples —
reads column 0 at eigenvalue **−0.85** against a dominant **1.0**, and
`"Matrix matrix theory over natural numbers and linear systems"` reads **−0.6024** against a
dominant **0.9663**. A port that copied `eig`'s first column would have reproduced both failures
exactly, keyword for keyword, rather than TextRank's actual ranking. Both documents were **removed
from the candidate list by hand** once the check found them; neither is in
`KEYWORDS_TEXTRANK_DOCUMENTS`.

That removal alone turned out not to be enough. The three documents that remained still measure
eigenvalue **0.85 with multiplicity 3** — `two_sentences` at `|vals| = 1, 0.85, 0.85, 0.85, 0.425`,
`domestic_cat` at `1, 0.85, 0.85, 0.85, 0.774` — and a repeated eigenvalue means the eigenvector
basis is not unique: LAPACK is free to return the degenerate columns in whatever order its BLAS
build produces. `generate_keywords_textrank` used to trust `summa.keywords.keywords`'s raw published
score and only *screen* it, by recomputing the stationary distribution through power iteration and
raising `SystemExit` when the two disagreed by more than `1e-9`. That screen only ever proved the
column the *generating machine's* LAPACK happened to load was dominant on that machine — it never
made the generator pick the same column a different machine's BLAS would. Measured directly: the
GitHub Actions runner and a developer machine disagreed about which of the three degenerate columns
loads first for `two_sentences`, so the *Oracles are reproducible* job — which regenerates the
corpus on the runner and diffs it against the one regenerated locally, [decision
0073](0073-the-oracle-gate-compares-numbers-not-bytes.md) — failed with `summa`'s published score
for `'diophantine'` not matching the frozen one, both sides having passed the very screen meant to
catch exactly this.

So the generator no longer lets `summa` pick a column at all. For the span of each
`summa.keywords.keywords` call, `generate_keywords_textrank` replaces
`summa.keywords._pagerank` — the name `summa.keywords` imports `pagerank_weighted_scipy` under,
so that is the alias that has to be patched, not `summa.pagerank_weighted` itself — with a version
that builds summa's own matrix bit for bit (`damping * build_adjacency_matrix(graph).todense() +
(1 - damping) * build_probability_matrix(graph)`, and `1 - 0.85` is `0.15000000000000002`, not
`0.15`, which changes the matrix LAPACK diagonalises), then selects the left eigenvector belonging
to the eigenvalue of the largest modulus **by index**, not by column position. Before returning it,
the replacement asserts the two things it promises — that the selected eigenvalue is actually the
largest in modulus, and that the selected vector satisfies `vᵀM ≈ λvᵀ` to a tight tolerance —
raising `SystemExit` naming the document if either fails, which is the guard's successor: the old
screen is meaningless once the pick is forced rather than trusted, so it was replaced rather than
kept alongside a mechanism it no longer needs to catch. The original `_pagerank` is restored once
every document is done, so no other generator step is affected. This is forced by reproducibility,
not chosen for its own sake: with column 0 dominant on the machine that originally froze the corpus,
`summa`'s live output already equalled the committed numbers exactly for all five documents, so the
fix buys a generation that agrees with itself across machines, not new values — confirmed by
regenerating twice and diffing both runs with `tools/compare_oracles.py`.

**4. `words` past the graph's size returns what there is; summa raises.** `summa.keywords.keywords`
computes `int(len(nodes) * ratio)` or takes `words` directly, then indexes the sorted node list with
it — `IndexError` when it exceeds the list's length. `TextRankOptions.Words` bounded by
`Math.Min(take, nodes.Count)` returns every node the graph has rather than throwing on a caller who
asked for more keywords than a short document contains. An `IndexError` here would be a range check
copied from an implementation detail of the reference's own indexing, not a property of the
algorithm; refusing to reproduce it is deliberate, not an oversight the oracle corpus happens not to
exercise.

**5. `keybert` parameterises `diversity = 1 − λ`, rounds to four decimals, and re-sorts by
relevance.** `keybert._mmr.mmr(doc_embedding, word_embeddings, words, top_n, diversity)` takes a
`diversity` in `[0, 1]` where `1` is maximally diverse;
[`Mmr.Select`](../reference/embeddings/search/mmr-select.md)'s `lambda` runs the other way,
`1` is maximally relevant, because that is the sign the MMR literature itself uses in the score
`λ · relevance − (1 − λ) · redundancy`, and inverting it inside `Select` would make the parameter
disagree with the formula computing it. `keybert` also rounds every similarity to four decimal
places before comparing, which changes which of two near-tied candidates wins on the fifth digit —
not reproduced, since a documented rounding step is a numerical-stability choice specific to one
implementation, not a property of the algorithm to match bit for bit. Both are why the oracle
corpus carries no scores at all: `tests/oracles/mmr.json`'s cases store `id`, `name`, `query`,
`candidates`, `count`, `lambda` and `selected`, and `MmrOracleTests.Matches_keybert` compares only
the selected index set — there is no score column to hold to a tolerance, loose or otherwise.

The ordering divergence is a different kind, and costs more to work around than to document:
`keybert` returns its final picks **sorted by relevance to the query**, discarding the order MMR
selected them in; [`Mmr.Select`](../reference/embeddings/search/mmr-select.md)'s own contract is
selection order, stated in its XML docs before this
decision existed (`<returns>The chosen indices, <b>in selection order</b>.</returns>`), because a
caller doing MMR for its diversity property — not for keybert's specific output shape — usually
wants to know which pick came from redundancy pressure and which came first on pure relevance. Re-
sorting inside `Select` to match `keybert` would serve one caller's convention at the cost of that
information for every other one. `tests/oracles/mmr.json`'s cases are compared as a **set** against
[`Mmr.Select`](../reference/embeddings/search/mmr-select.md)'s output for exactly this reason — see
`MmrOracleTests.Matches_keybert`, which orders
both sides before asserting rather than comparing sequences.

## What this does not decide

[`VectorMath.Dot`](../reference/embeddings/search/vectormath-dot.md)'s two target frameworks summing
in different orders, and the near-tie that can follow from it, is not a divergence from Python — it
is a divergence between `net10.0` and `netstandard2.0`
[`Mmr.Select`](../reference/embeddings/search/mmr-select.md) can itself produce, already documented
in that same method's own XML remarks and inherited from
[`VectorMath`](../reference/embeddings/search/vectormath.md)'s existing one. Recording it a second
time here would be the same fact under two names.

## What enforces it

`tests/oracles/keywords_rake.json`, `keywords_textrank.json` and `mmr.json` are frozen replays of
rake-nltk 1.0.6, summa 1.2.0 and keybert 0.9.0 respectively, generated by
`tools/generate_oracles.py` and compared in `RakeOracleTests`, `TextRankOracleTests` and
`MmrOracleTests`. Divergence 3's deterministic eigenvector pick lives in
`generate_keywords_textrank` itself, not in a comment, patching `summa.keywords._pagerank` for the
span of each `summa.keywords.keywords` call and restoring it afterwards; its own dominance and
left-eigenvector assertions raise `SystemExit` naming the document rather than silently freezing a
column that fails either check.
