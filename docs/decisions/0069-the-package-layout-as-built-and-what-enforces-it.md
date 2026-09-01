# 0069 — The package layout as built, and what enforces it

**Status:** accepted · **Date:** 2026-09-01

## Context

[#427](https://github.com/CyrilB1531/lodestar/issues/427) states the split criteria and the
tiering, and CI enforces them. Nothing writes them down.
[Decision 0012](0012-per-package-versioning.md) records that each package versions on its own and
[decision 0016](0016-metrics-package-placement.md) that metrics ship separately, but neither says
what may become a package, what edges are allowed between them, or what a reviewer may cite when a
pull request adds one. A rule enforced by a script and argued from an issue is a rule nobody can
point at.

[#439](https://github.com/CyrilB1531/lodestar/issues/439)'s P5 asks for that record, and notes
that it must describe **what exists** rather than what the roadmap proposed — the two differ.

## The layout, as built

Four packages, every one `net10.0;netstandard2.0`, one public API on both:

| package | holds | ships with |
| --- | --- | --- |
| `Lodestar.Text` | distances, phonetics, set similarity, stemmers, tokenizers, sparse vectorizers, persistence | nothing on `net10.0`; `System.Text.Json` and the polyfills on `netstandard2.0` |
| `Lodestar.Embeddings` | sub-word tokenizers, batch encoding, pooling, SIMD kNN, ONNX inference | `Microsoft.ML.OnnxRuntime` |
| `Lodestar.Fuzzy` | `fuzz.*`, `process.*`, blocking deduplication | `Lodestar.Text` |
| `Lodestar.Metrics` | classification, regression, clustering and ranking metrics | nothing on `net10.0`; the polyfills on `netstandard2.0` |

**`Lodestar.Fuzzy → Lodestar.Text` is the only inter-package edge**, and it exists because
[`Fuzz.Ratio`](../reference/fuzzy/matching/fuzz-ratio.md) is built on `Indel`. `src/` references
packages, never projects, so a clean clone builds with no pack step (0012).

## Three rules, and what enforces each

**1. Split only for a distinct dependency profile, audience or release cadence — never for
tidiness.** Every package costs CI, documentation, symbols and a release checklist, forever, for
one maintainer. `Lodestar.Embeddings` exists because ONNX Runtime must not reach a caller who only
wants `Levenshtein`; `Lodestar.Metrics` because 0016 measured that its audience does not overlap
`Text`'s. Nothing enforces this one — it is a judgement, and this ADR is what a reviewer cites.

**2. The shipped dependency graph is exactly the written one.**
`tools/check_nuspec_dependencies.py --require-all` asserts it per package and per target framework,
**ranges included**: an unexpected edge fails as loudly as a missing one, and an edge whose floor
moved is a different edge. `dotnet pack` derives `<dependencies>` from what restore resolved, so
without this the graph consumers see is a build output nobody wrote down.

**3. `src/` reference packages, never projects.** A text grep cannot tell the shipped path from the
opt-in developer loop (`LodestarUseProjectRefs` puts a `ProjectReference` back), so CI asks
evaluated MSBuild instead and never sets the property. `tools/check_version_floor.py` holds the
three places a `Lodestar.Text` version number lives to each other, and `--check-feed` proves the
floor is actually resolvable.

## Where this diverges from the roadmap, and why

**`Lodestar.Abstractions` was never built.** #427 made its case conditionally — `CsrMatrix`, dense
views, `IDistance` and the tokenizer interfaces must live in one dependency-free package *"or
splitting produces duplicated types and circular dependencies"*. Neither happened: there is no
duplicated public type and no cycle, one deliberate edge, asserted. The predicted failure did not
occur because the split was done a different way, and a package built against a failure that did
not happen is a package nobody needs.

**The naming is flatter.** `Lodestar.Embeddings` and `Lodestar.Fuzzy` sit at the top level rather
than under `Text.`, and `Text.Distance` never became its own package. Still within the two-level
`Lodestar.<Domain>[.<Sub>]` rule, and the `Lodestar.` prefix is reserved on nuget.org, so a new
package is automatically ours and cannot be squatted.

**The satellite tier is empty.** #427 tiers core (netstandard2.0 + net10, no dependencies) against
satellite (net8.0+, dependencies allowed). As built, `Embeddings` carries ONNX Runtime and still
ships both target frameworks, so it is not a satellite by that definition — nothing has yet needed
a net8-only floor. The tier is a rule waiting for its first member, not a description.

## What this does not settle

Whether a second, third and fourth edge into `Lodestar.Text` stays acceptable.
[#438](https://github.com/CyrilB1531/lodestar/issues/438) put the question precisely: every Phase 2
lot in [#440](https://github.com/CyrilB1531/lodestar/issues/440) reaches for the same things —
`Text.Similarity` verifies candidates with our distances, `Text.Index` is "the missing link between
Distance and Fuzzy", `Text.Search` builds on `CountVectorizer` and so on `CsrMatrix`. Whether that
is the point at which `Abstractions` (or `Text.Distance` as its own package) earns itself is for
whoever opens the first of those lots. It is not a defect now, and this ADR does not pretend to
decide it.

## Consequences

- The rules CI enforces are citable in review, which is what they were missing.
- `1.0.0` is not gated on a package that was never built. #427 ties the tag to `Abstractions` being
  settled; settled here means *decided against, for the reason above*.
- A pull request adding a package or an edge has something to argue with, and a maintainer
  disagreeing with it amends this record rather than editing it.

## Relationship to 0012 and 0016

Neither is amended: this contradicts nothing either says. 0012 records that each package versions
and releases on its own; 0016 records why metrics are not inside `Text`. This states the layout
those two decisions produced and the criteria that would admit a fifth package — it extends them,
and the index's relationships column says so in both directions.
