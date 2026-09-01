# 0071 — `CsrMatrix` moves to a `Lodestar.Abstractions` package

**Status:** accepted · **Amends:** [0069](0069-the-package-layout-as-built-and-what-enforces-it.md) · **Date:** 2026-09-01

## Context

[Decision 0069](0069-the-package-layout-as-built-and-what-enforces-it.md) recorded
`Lodestar.Abstractions` as **decided against**, and the reasoning was sound at the time:
[#427](https://github.com/CyrilB1531/lodestar/issues/427) argued for it conditionally — the shared
primitives must live in one dependency-free package *"or splitting produces duplicated types and
circular dependencies"* — and neither happened. A package built against a failure that did not occur
is a package nobody needs.

0069 also left one thing open, by name: *"Whether a second, third and fourth edge into
`Lodestar.Text` stays acceptable… is for whoever opens the first of those lots."*
[#440](https://github.com/CyrilB1531/lodestar/issues/440)'s lot 3 is that lot — `TruncatedSvd` reads
a `CsrMatrix` and nothing else of `Lodestar.Text` — and this record is that answer.

## Decision

`CsrMatrix` and `SparseNorm` move to a new `Lodestar.Abstractions` package, in a
`Lodestar.Abstractions` namespace. `Lodestar.Decomposition` will depend on `Abstractions` alone, and
`Lodestar.Text` becomes the type's second consumer rather than its owner.

## Why not the edge into `Lodestar.Text`

Because of what it makes every later consumer carry. A package that wants a sparse matrix and two
products would take the distances, the phonetics, the stemmers, the tokenizers, the vectorizers and
the persistence layer with it — and `System.Text.Json` on `netstandard2.0`, which is the one
dependency `Lodestar.Text` deliberately has and `Abstractions` deliberately does not.

0069's own first rule reads the other way here: *split only for a distinct dependency profile,
audience or release cadence.* The dependency profile genuinely differs, and that is the rule's first
clause rather than a reading of its last.

## What it costs, stated rather than softened

- **A breaking source change.** `Lodestar.Text` 0.5.0 no longer declares the type; every
  `using Lodestar.Text.Vectorization;` that touches `CsrMatrix` changes. About two dozen source
  files, the sample, the executed snippets, seven reference pages, three decision records and the
  README.
- **Three pull requests separated by two releases nobody can automate.** `src/` references published
  packages and never projects (0069 rule 3), so `Lodestar.Abstractions` 0.1.0 must be on nuget.org
  before `Lodestar.Text` can reference it, and `Lodestar.Text` 0.5.0 before `Lodestar.Decomposition`
  can be built against the pair. `LodestarUseProjectRefs` is a local loop, not a merge strategy.
- **An `InternalsVisibleTo` from `Abstractions` to `Text`.** `CreateUnchecked` skips the structural
  validation and is documented *"never call it with data that came from outside"*; `CountVectorizer`,
  `TfidfTransformer` and `HashingVectorizer` call it four times between them. The grant names a
  package in the opposite direction to the dependency, which reads wrong and is inert at run time. It
  ships in 0.1.0 rather than later, because adding it afterwards would cost another release of the
  package whose whole purpose is being published first.

## Options refused

**A type-forward keeping `Lodestar.Text.Vectorization` as the namespace.** It buys full source *and*
binary compatibility: no `using` changes, no version bump beyond a patch. Refused because it would
leave `Lodestar.Abstractions` declaring, for the life of the package, a type in a namespace that
names a different package — a permanent lie about where the type lives, bought to spare a
find-and-replace in a pre-1.0 library where semver already allows the break.

**A namespace inside `Lodestar.Text`.** By far the cheapest: no package, no edge, no release
sequence, and this record would not exist. Refused for the reason above — and because deferring
0069's open question a second time does not make it easier, it makes the answer more expensive.

**Making `CreateUnchecked` public.** It would remove the `InternalsVisibleTo`, at the price of
exporting a factory whose documentation says not to call it. A footgun is worse than an attribute
that reads backwards.

## Consequences

- 0069's *"`Lodestar.Abstractions` was never built"* paragraph and its *"What this does not settle"*
  section are what this amends. Its rules 1, 2 and 3 stand unchanged and now cover six packages.
- The layout gains one edge and loses none: `Text → Abstractions`, `Decomposition → Abstractions`,
  and `Fuzzy → Text` as before. There is still no cycle and no duplicated public type once step B
  lands.
- `1.0.0` is no longer un-gated by this question, which 0069 had settled by deciding against the
  package. It is settled the other way instead, and the release sequence above is what it now waits
  on.
