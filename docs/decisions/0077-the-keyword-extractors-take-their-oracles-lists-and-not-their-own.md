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
its own does.**
`scipy.linalg.eig`'s `pagerank_weighted_scipy` path returns eigenvalues and eigenvectors in no
particular order, and `summa` reads column 0 unconditionally (`vecs[:, 0]`), trusting it to be the
dominant one — true for a strongly connected graph, and not guaranteed for one that is not. Two
documents drafted for the oracle corpus measure the failure directly: the abstract from Rose et
al. — the RAKE paper's own worked example, already the source of `Rake`'s doc-page examples —
reads column 0 at eigenvalue **−0.85** against a dominant **1.0**, and
`"Matrix matrix theory over natural numbers and linear systems"` reads **−0.6024** against a
dominant **0.9663**. A port that copied `eig`'s first column would have reproduced both failures
exactly, keyword for keyword, rather than TextRank's actual ranking. Both documents were **removed
from the candidate list by hand** once the check found them, not filtered out automatically by
anything running at generation time — `generate_keywords_textrank`'s guard, added afterwards,
recomputes each candidate document's ranking by power iteration and, if a *published, single-word*
score disagrees with it by more than `1e-9`, raises `SystemExit`, halting the whole run rather than
skipping the offending document and continuing. So it is not a filter over "every document": it is
the net that would catch a *future* document shaped like these two, and it never fires on the
committed corpus because the two that would have tripped it are no longer in the candidate list. The
three documents that are (`two_sentences`, `natural_language`, `domestic_cat`; 7, 22 and 16 graph
nodes) agree with the power iteration under `4.48e-13`, well inside the `1e-9` the guard actually
checks. Power iteration always converges to the dominant eigenvector by construction, so the corpus
is a parity claim over documents where the two algorithms provably agree rather than one that
happened never to exercise the disagreement — and
[`TextRank`](../reference/text/keywords/textrank.md)'s own Remarks say so for a reader who never
opens this file. The guard itself only walks `sk.keywords`'s **published, single-word** entries —
it skips every glued multi-word phrase — so what it proves is agreement on the top-N that was
actually returned, not that the same top-N would come out of the true dominant vector for a document
whose published set happens to differ.

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
`MmrOracleTests`. The dominance screen for divergence 3 lives in `generate_keywords_textrank`
itself, not in a comment, and raises rather than silently drops a bad case — but it walks only the
published, single-word entries, so it is the net that would catch a *future* candidate document
shaped like the two this session excluded by hand, not a proof that covers every entry summa could
publish.
