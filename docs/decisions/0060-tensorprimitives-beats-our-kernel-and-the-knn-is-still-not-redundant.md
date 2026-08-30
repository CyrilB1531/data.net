# 0060 — `TensorPrimitives` beats our kernel, and the kNN is still not redundant

**Status:** accepted · **Date:** 2026-08-30 · **Completes:** [`0059`](0059-phase-0-verifications-two-confirmed-voids-do-not-survive-nuget.md)

## Context

V6 of [#437](https://github.com/CyrilB1531/lodestar/issues/437) is the last of Phase 0, and the only
one that wanted a measurement rather than a reading. [#427](https://github.com/CyrilB1531/lodestar/issues/427)'s
open-risks table carries it as *"`TensorPrimitives` makes the kNN redundant"*, with *"V6 + P3; fall
back on many-vs-many top-k"* as the action.

**The availability half needs no run.** `System.Numerics.Tensors` **10.0.11, 2026-08-11**, targets
`net8.0`, `netstandard2.0` and `net462`, at 48.3 million downloads
([nuget.org](https://www.nuget.org/packages/System.Numerics.Tensors)). It reaches both of our
frameworks, so the question was never whether it is available.

## What was measured

`tensor-primitives` on `f007341`, a hosted runner, .NET 10.0.11, 4 cores, `Vector<float>` 8 wide
and hardware-accelerated, three rounds of nine interleaved runs, load average 3.15 / 3.14 / 3.14.
Medians in ms over 10 000 × 384 floats:

| row | round 1 | round 2 | round 3 |
| --- | ---: | ---: | ---: |
| `ours_dot_knn` | 1.126 | 0.820 | 0.832 |
| `tp_dot_knn` | 1.028 | 0.713 | 0.679 |
| `ours_cosine_knn` | 1.812 | 1.260 | 1.248 |
| `tp_cosine_knn` | 0.991 | 0.869 | 0.832 |
| `ours_one_sweep` | 0.841 | 0.749 | 0.710 |
| `tp_one_sweep` | 0.722 | 0.672 | 0.615 |
| `index_search` | 2.862 | 1.680 | 1.687 |

| ratio, above 1 means `TensorPrimitives` is faster | r1 | r2 | r3 |
| --- | ---: | ---: | ---: |
| dot, per row of 384 | 1.09× | 1.15× | 1.23× |
| cosine, per row of 384 | 1.83× | 1.45× | 1.50× |
| one sweep of 3.84 M floats | 1.16× | 1.11× | 1.15× |

**The two agree before any of this is read as a speed.** Worst absolute difference 1.192e-7 on the
dot and 3.725e-8 on the cosine, identical in all three rounds — the corpus is deterministic.

## Decision

**`TensorPrimitives` is faster than our kernel on our own access pattern, and the kNN is still not
redundant.** Both halves are the finding, and the second does not soften the first.

**Our kernel is behind.** 1.09–1.23× on the dot, which is the shape
[`EmbeddingIndex.Search`](../reference/embeddings/search/embeddingindex-search.md)
actually runs, and 1.11–1.16× on a single long sweep, which is what `TensorPrimitives` is designed
for. There is no access pattern here on which we win.

**But the kernel is about half of a query.** `index_search` is 1.680–2.862 ms against
`ours_dot_knn`'s 0.820–1.126 — the dot is roughly 49% of `Search`, and top-k selection is most of
the rest. Replacing the kernel outright with `TensorPrimitives` therefore buys **6–9% of a query**:
0.107 ms of 1.680 in round 2, 0.153 ms of 1.687 in round 3.

So the risk as the roadmap phrased it does not hold — a 6–9% margin is not redundancy — **and the
reason it does not hold is not that our kernel is good.** It is that the kernel is not where a query
spends its time. That is a different finding from the one the risk anticipated, and it points
somewhere else: **top-k selection, not the dot, is what a kNN lot should measure next.**

**The cosine row prices a route we do not take.**
[`EmbeddingIndex`](../reference/embeddings/search/embeddingindex.md) normalizes on insertion, so
`Search` is a dot product and never calls a cosine. The 1.45–1.83× there is what per-call cosine
would cost us if we ever computed one, and is an argument for delegating that shape if it is ever
needed — not a statement about the index as it stands.

## What was refused

**Reading the container.** The same diagnostic in this session's container reported the dot at
**0.27×** — our kernel apparently 3.7× *faster* — and an earlier throwaway probe reported 3.9×.
Both are inverted by the runner. The container reading was reported here as *"the roadmap's risk is
not borne out"*, and it was wrong; the risk is borne out on the numbers, and only fails on what
follows from them. This is the repository's own rule (`bench/README.md` §10) earning its keep for
the third time in one session.

**Delegating the kernel now.** A 6–9% margin on a query is real and it is a candidate, but it costs
a dependency in `Lodestar.Embeddings` — and on `netstandard2.0` `TensorPrimitives` has no
intrinsics to reach for, which this run did not measure. Taking it is a lot with its own gate, not
a consequence of this one.

**Rewriting [`VectorMath.Dot`](../reference/embeddings/search/vectormath-dot.md) to close the
gap.** Nothing here says where the 9–23% goes, and a kernel rewritten against an unexplained
margin is how a session produces a change it cannot defend.

## Consequences

- **Phase 0 is complete.** V1–V7 are answered; 0059 carries V3, V4, V5 and V7, this carries V6.
- **The open risk changes shape rather than closing.** *"`TensorPrimitives` makes the kNN
  redundant"* is answered no, and replaced by a smaller, measured one: our kernel is 9–23% behind
  the BCL on the shape we run, and delegating it is worth 6–9% of a query.
- **A kNN performance lot should start at top-k**, which this table puts at about half of `Search`
  and which nothing in this repository has measured.
- **What would change this decision** is the `netstandard2.0` half. Both sides fall back there —
  ours to a scalar loop, `TensorPrimitives` to whatever it does without intrinsics — and if the
  margin inverts on that target, a delegation would have to be conditional rather than a
  replacement, which changes what it is worth.
