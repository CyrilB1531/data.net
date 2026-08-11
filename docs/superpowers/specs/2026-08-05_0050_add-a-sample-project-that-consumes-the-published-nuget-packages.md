# Design — #50: a sample that consumes the published packages

**Date:** 2026-08-05 · **Issue:** #50 · **Branch:** `feat/50-package-sample` ·
**Checkout:** `<repo>`

## Problem

Nothing in this repository consumes the published packages. Everything builds
against `ProjectReference`, so the packaging itself is only ever exercised by
`dotnet pack` — **never by installing and using the result**.

That leaves a whole class of defect invisible. A package can pack cleanly and
still be broken for a consumer:

- a type `public` in the source but not reachable from outside the assembly
- a missing dependency in the `netstandard2.0` group, so `System.Memory` is absent
  at run time
- an XML documentation file that fails to ship, so IntelliSense is empty
- an assembly that resolves for `net10.0` but not on .NET Framework — which is the
  whole point of #1

**None of these would fail CI today.**

## Decisions

### D1 — A local feed, not nuget.org

Restoring the last *published* version would be more honest as documentation and
useless as a gate: it can only fail once a broken package is already public. A
local folder fed by `dotnet pack` inverts that — the gate runs on what is **about
to ship**.

The fact that 0.2.0 is not published yet is the case in favour, not against: the
sample already runs green against it.

Recorded as ADR 0009.

### D2 — The referenced version binds to `$(Version)` from the root props

Which applies to `samples/` too, so the reference tracks what `pack` just produced
instead of pinning a number that goes stale at the next release.

### D3 — Outside `DataNet.slnx`, deliberately

Inside the solution, `ProjectReference` resolution would quietly satisfy the
references and **the sample would prove nothing while appearing to work**. This is
the single most important decision in the change and the easiest to undo by
accident, so it is verified rather than intended.

### D4 — It runs in CI, packing first

A sample that is never built rots into documentation that lies. Building resolves
the packages as a consumer would, so a missing dependency group or an unreachable
public type fails the build; running proves the code works once resolved.

### D5 — One thing per lot, so it doubles as a runnable quickstart

- distances — `Levenshtein`, `JaroWinkler`
- vectorization — `TfidfVectorizer` over a few documents
- stemming — the six Snowball languages now that #2–#5 are merged
- fuzzy — `Process.ExtractOne`
- embeddings — **tokenizer only**

Embeddings need care: the ONNX path requires a model that is deliberately not
committed (ADR 0003). The sample uses the tokenizer and **says so**, rather than
failing or silently skipping.

### D6 — It prints the target framework it resolved

The first line of output. That is what makes a resolution failure visible rather
than something to infer from a stack trace.

## Out of scope

- Covering every public type. That is a real gap and becomes #72.
- Publishing anything.

## What "done" means

`samples/DataNet.Sample` consuming the three packages by `PackageReference`;
running end to end and printing something meaningful per lot; the feed decision in
ADR 0009; the README linking it; **verified absent from `DataNet.slnx`**.
