# 0041 — One sample file per public class, named after it

**Status:** accepted · **Date:** 2026-08-19

## Context

`samples/Lodestar.Sample/` groups its demonstrations by lot, and the lot numbers say nothing about
what is inside. `Lot5Metrics.cs` is 591 lines, `Lot3Embeddings.cs` is 417, and between them they
carry dozens of examples under names that help nobody find one.

Someone meeting `MutualInformation` for the first time cannot guess that its example lives in
`Lot5Metrics.cs`. Neither a file-name search nor an IDE's *go to file* reaches it — only a full-text
search does, and only if they think to try. The opacity compounds with every lot: issues #262, #263
and #264 each added a demonstration into a file whose name mentions none of them.

The samples are the first thing a reader reaches for, because they are the only place that shows a
type being *used* from outside its assembly. Being the hardest thing to navigate is the wrong
property for that.

## Decision

**One file per public class, named `<ClassName>Sample.cs`, holding a `Run()` the package's
aggregator calls.** Finding the example for a type becomes the same gesture as finding the type.

Three rules fix the edges:

1. **An enum gets no file of its own.** It is demonstrated through the class whose parameter it is —
   `TextElement` by `Levenshtein`, `ZeroDivision` by `Precision` — and a file exercising an enum
   alone would have to invent a use for it. The reference pages already document enums separately;
   the samples are about calls, and an enum is not one.
2. **An internal type gets no sample, and this is not a convention.** A sample shows what a consumer
   of the package can write, and a consumer cannot name an internal type. If one deserved an
   example, the finding would be that it should be public.
3. **A per-package aggregator survives**, `<Package>Samples.cs`, calling each class's `Run()` in a
   readable order. `Program.cs` keeps calling one method per package rather than growing to 140
   lines, and the order in the aggregator is where a reader learns which types belong together —
   which is the one thing the lot numbering did well.

Nested public types were weighed and are not a case: measured across `src/`, there is no indented
public type declaration. If one appears it arrives with its own decision.

## Consequences

- **140 files, and therefore not one pull request.** The convention lands with `Lodestar.Text` as
  its worked example, then one lot per package — `Fuzzy` (4 types), `Embeddings` (31),
  `Metrics` (67). Each is reviewable on its own, and none of them collides with the whole of
  `samples/` for a week.
- **The packaging gate is what proves nothing was lost.** ADR 0009 requires every public type to be
  reachable from the sample by a member reference; splitting the files moves where those references
  live. `PackagingGate.cs` fails if one goes missing, which is why the split is safe to do
  mechanically and unsafe to do without running it.
- **A new public type now has an obvious home**, which is the point: a contributor adding
  `FooBar` writes `FooBarSample.cs` rather than choosing a lot, and a reviewer notices its absence
  by the file not being there.
- **The `Lot*` files shrink rather than disappear.** The last package to leave them decides whether
  anything is left worth keeping; asking that question now, with four packages still inside, would
  be guessing.

## The alternative, and why it lost

**Keeping the lots and adding an index** — a table in `samples/README.md` mapping each type to the
lot that demonstrates it — was the cheap option, and it was seriously in play: no refactor, no
packaging-gate risk, one file to maintain.

It lost because it is a second copy of a fact the file system can hold directly. An index goes stale
the first time someone adds a type without updating it, and nothing fails when it does — the same
silence this repository already fights in `bench/bench-map.json` and the exception-parity gate, both
of which needed a guard to stay honest. Naming the file after the class needs no guard: the file is
either there or it is not, and the packaging gate already asks that question for the type.
