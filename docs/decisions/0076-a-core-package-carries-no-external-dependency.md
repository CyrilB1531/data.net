# 0076 — A core package carries no external dependency, so ONNX Runtime gets its own

**Status:** accepted · **Date:** 2026-09-03 · **Supersedes:** [0069](0069-the-package-layout-as-built-and-what-enforces-it.md)

## Context

[#427](https://github.com/CyrilB1531/lodestar/issues/427) tiers the packages: core is
`netstandard2.0` + `net10.0` with no external dependency, satellite is `net8.0+` with
dependencies allowed. [Decision 0069](0069-the-package-layout-as-built-and-what-enforces-it.md)
wrote the layout down a year into it and had to record that the tier was aspirational:

> **The satellite tier is empty.** As built, `Embeddings` carries ONNX Runtime and still ships
> both target frameworks, so it is not a satellite by that definition — nothing has yet needed a
> net8-only floor. The tier is a rule waiting for its first member, not a description.

0069's own rule 1 — *split only for a distinct dependency profile, audience or release cadence,
never for tidiness* — gave `Lodestar.Embeddings` its reason for existing: *"ONNX Runtime must not
reach a caller who only wants `Levenshtein`."* That reason was one level short of where it led.

Measured on 2026-09-03, at `main`:

- `Microsoft.ML.OnnxRuntime` 1.28.0 was **the only external dependency in the repository**.
  `Abstractions`, `Text`, `Fuzzy`, `Metrics`, `Decomposition` and `Conformal` declared none.
- It was reached by **one file**, `src/Lodestar.Embeddings/Onnx/OnnxTextEmbedder.cs`, 407 lines.
  Nothing else in the package referenced the type; the only other mention was a `<see cref>`.
- Everything else in the package — the four sub-word tokenizers, the batch encoder, the pooling,
  the `.npy` reader and the 869-line SIMD kNN index — needed no runtime and could not be had
  without one.

`Lodestar.Embeddings.csproj` said as much in a comment and stopped there: *"ONNX Runtime is the
one external dependency, isolated to this package so Lodestar.Text stays dependency-free. Only
the OnnxTextEmbedder type uses it."* Isolation to a package that also holds the tokenizers is
isolation from `Lodestar.Text`, not from a caller.

0069 is also stale on two counts unrelated to this one, which is why this record supersedes it
rather than amending it — ADRs are not edited ([`README`](README.md),
`tools/check_adr_immutable.py`). It describes four packages where eight now ship, and states that
*"`Lodestar.Abstractions` was never built"*, which
[decision 0071](0071-csrmatrix-moves-to-an-abstractions-package.md) had already overtaken.

## Decision

**A core package carries no external dependency. An external dependency earns its own satellite
package, named for it.**

`OnnxTextEmbedder` moves to a new `Lodestar.Onnx`, in namespace `Lodestar.Onnx` — every package
sets `RootNamespace` equal to its `PackageId`, and a `Lodestar.Onnx` holding
`Lodestar.Embeddings.Onnx` would have been the only exception. `Lodestar.Embeddings` 0.6.0 drops
the type and the `PackageReference`, and becomes core tier like everything else.

This restates #427's tiering as a rule rather than a table, and the rule is mechanical where the
table was a judgement. It does **not** replace 0069's rule 1: *distinct dependency profile,
audience or release cadence, never tidiness* still governs every other split, and this decision
only removes the one case where "distinct dependency profile" was being judged by eye.

### The layout, as it now ships

| tier | package | external dependency | inter-package edge |
| --- | --- | --- | --- |
| core | `Lodestar.Abstractions` | — | — |
| core | `Lodestar.Text` | `System.Text.Json` on `netstandard2.0` only | → `Abstractions` |
| core | `Lodestar.Embeddings` | `System.Text.Json` on `netstandard2.0` only | — |
| core | `Lodestar.Fuzzy` | — | → `Text` |
| core | `Lodestar.Metrics` | — | — |
| core | `Lodestar.Decomposition` | — | → `Abstractions` |
| core | `Lodestar.Conformal` | — | — |
| satellite | `Lodestar.Onnx` | `Microsoft.ML.OnnxRuntime` | → `Embeddings` |

`Lodestar.Onnx` ships `net10.0;netstandard2.0` like the rest: ONNX Runtime 1.28.0 supports both,
which the previous arrangement proved, so #427's `net8.0+` satellite floor is not taken here. The
floor is a permission, not an obligation — a satellite takes it when its dependency forces it.

The polyfills and `System.Text.Json` are not external dependencies for this rule's purpose. They
are `netstandard2.0` shims for what is in-box on `net10.0`, so a package carrying them offers one
API at two implementations rather than a second thing to install
([decision 0011](0011-persistence-format.md)).

## What enforces it

`tools/check_nuspec_dependencies.py` asserts the shipped graph per package and per target
framework, ranges included. `Microsoft.ML.OnnxRuntime` appears in exactly one row of `EXPECTED`,
under `Lodestar.Onnx`, so the rule fails a build rather than a review the moment an external
dependency reappears in a core package.

`tools/check_version_floor.py` gains the fourth edge, `Lodestar.Onnx` → `Lodestar.Embeddings`,
floored at 0.5.0 — the release in which
[`BatchEncoder.EncodeAll`](../reference/embeddings/tokenization/batchencoder-encodeall.md) and
[`BatchEncoder.Pad`](../reference/embeddings/tokenization/batchencoder-pad.md) became public. A
floor names what the dependent needs, and that is what it needs.

0069's rules 2 and 3 are unchanged and still enforced by the same two scripts: the shipped graph
is exactly the written one, and `src/` references packages rather than projects.

## The cost, and what it bought

Two merges rather than one, and a release between them. `OnnxTextEmbedder` used four `internal`
members of `Lodestar.Embeddings` — [`EncodeAll`](../reference/embeddings/tokenization/batchencoder-encodeall.md),
[`Pad`](../reference/embeddings/tokenization/batchencoder-pad.md), and `EncodedBatch.Ids`/`.Mask`
— so no namespace arrangement could have made the split a single green pull request against the
published 0.4.0. The first two shipped public in 0.5.0
([#534](https://github.com/CyrilB1531/lodestar/pull/534)); the two fields stayed internal,
because `InputIds` and `AttentionMask` already expose the same buffers as spans.

That last substitution costs a copy: ONNX Runtime wraps a `Memory<T>`, which a span cannot
supply, so a padded batch is copied into a rented array before the session sees it. The
single-sequence path already paid exactly that, for exactly that reason, and the alternative was
a second padding implementation in `Lodestar.Onnx` that could drift from the one the frozen
corpus asserts.

What it bought: `dotnet add package Lodestar.Embeddings` no longer restores a native runtime. The
sub-word tokenizers, the batch encoder, the pooling, the `.npy` reader and the kNN index reach a
caller who does not run models at all.

## Options considered

**Extract a `Lodestar.Vectors` instead** — move `VectorMath` and `EmbeddingIndex` out, on the
grounds that a caller holding its own vectors should not need the tokenizers. It answers a
different question: the tokenizers and the pooling stay behind ONNX Runtime, so the weight is
unchanged for everyone but the kNN user. And once `Lodestar.Onnx` exists, `Lodestar.Embeddings`
has no external dependency at all, which leaves cohesion as the only argument for the split —
which 0069 rule 1 refuses by name.

**`InternalsVisibleTo("Lodestar.Onnx")`** — one line, no public API added, and the split needs no
0.5.0 release first. Refused: a NuGet floor of `>= 0.5.0` resolves any later
`Lodestar.Embeddings`, and internals are not a contract, so the two packages would be welded
together by a guarantee neither of them states. A public API that says what the neighbour needs
is the honest version of the same coupling.

**Leave it** — the isolation is nearly true, and one file is a small prize. Refused because the
tier was never going to acquire a first member on its own, and #427 is explicit that pre-1.0 is
when the layout is free: *"Do the full split in one go, decide the final layout on paper first."*
Phase 4's own constraint — anything needing a numerical dependency has to be argued as satellite
before it is written — becomes a mechanical check instead of an argument nobody remembers to have.
