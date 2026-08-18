# 0009 — The sample restores from a local feed, not nuget.org

**Status:** accepted · **Date:** 2026-08-05

## Context

Nothing in the repository consumed the published packages. Everything built
through `ProjectReference`, so packaging was only ever exercised by
`dotnet pack` — never by installing and using the result.

That hides a whole class of defect. A package can pack cleanly and still be
broken for a consumer: a missing dependency in the `netstandard2.0` group so
`System.Memory` is absent at run time; an XML documentation file that fails to
ship; a type that is `public` in source but unreachable from outside the
assembly. None of those would have failed CI.

## Decision

`samples/DataNet.Sample` consumes the three packages by **`PackageReference`**,
restoring from a **local folder fed by `dotnet pack`** rather than from nuget.org.

The version is bound to `$(Version)` from the repository's root
`Directory.Build.props`, which applies to the sample too, so it tracks whatever
`dotnet pack` just produced instead of pinning a number that goes stale.

> **Amended by [0012](0012-per-package-versioning.md).** The three packages now
> version independently, so there is no repository-wide `$(Version)` left to bind
> to: the sample imports each project's `Version.props` and uses one property per
> package. It still tracks what `dotnet pack` just produced — but preserving that
> took more than rebinding the property, and the rest of this document should be
> read with the next paragraph in mind.
>
> Listing the local feed first does not make it the feed that answers. Restore
> consults the global packages folder before any source, so a DataNet package
> declared at a version that is *also* on nuget.org resolves to the published
> assembly — the exact inversion "Why not nuget.org" below exists to prevent, now
> arriving silently and without editing this file. Under per-package versioning
> that is the normal case, not an accident: a package that did not change keeps
> its published version indefinitely. Three things hold the line now — a
> `packageSourceMapping` confining `DataNet.*` to the local feed, a separate
> `NUGET_PACKAGES` for the sample's restore in CI, and `check_version_floor.py`
> keeping a declared version off the feed in the first place.

## Why not nuget.org

Restoring from nuget.org would test the **last published** version. That is more
honest as documentation — it is what a reader would actually get — but useless as
a gate: it can only fail after a broken package is already public, and it cannot
run at all before the first publish of a new version. At the time this landed,
`0.2.0` was not yet on nuget.org, and the sample already worked against it.

A local feed inverts that: the gate runs on what is *about* to ship.

## Consequences

- **The packages must be packed before the sample restores.** The CI job does
  this; running it by hand requires the same:

  ```bash
  for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy src/DataNet.Metrics; do
    dotnet pack "$p" -c Release -o ./artifacts
  done
  dotnet run --project samples/DataNet.Sample -c Release
  ```

- **The sample is deliberately outside `DataNet.slnx`.** Inside the solution,
  `ProjectReference` resolution would quietly satisfy the references and the
  sample would prove nothing while appearing to work.
- **It runs in CI**, because a sample that is never built rots into documentation
  that lies.

> **Amended by #265: the gate is member-granular.** The bullet below states the
> contract in terms of **types**, and that was the contract until 2026-08-18. It
> is now stated in terms of **members**: every public method, constructor and
> property of an exported type needs its own member reference, not one reference
> standing in for the whole type. The paragraph below is kept as written, because
> the reasoning for the gate is unchanged and only its granularity moved; what
> changed is recorded under *Member granularity* at the end of this document.

- **It covers every exported public type, and a gate keeps it that way.** The
  "unreachable public type" guarantee above is worth exactly the set of types the
  sample references, and when this was measured it referenced 14 of 58 — so for
  the other 44 the job was green by construction. `PackagingGate` reads the exported
  surface of the three assemblies *as NuGet resolved them for the sample* and
  fails the run when one of them has no member referenced. Adding a public type
  without adding a call is now a red build rather than a silent hole; #65 and #66
  added seven such types before anyone noticed. The criterion is a member
  reference in the compiled metadata, so `typeof(T)` alone does not satisfy it,
  and enums — whose members are compile-time constants — are the one documented
  exception, satisfied by being named.
- **It covers `lib/net10.0` only.** The `netstandard2.0` package assets are not
  consumed by anything: the netstandard2.0 *assemblies* are covered by the mirror
  test projects, but the *package* dependency group for that target is not.
  Adding a `net8.0` target to the sample would close this, since net8.0 resolves
  `lib/netstandard2.0`; it needs the 8.0 runtime in CI.
- ONNX inference is not exercised. Model weights are deliberately not committed,
  so the sample uses the tokenizer and says so, rather than failing on a missing
  file.

## Member granularity (amendment, 2026-08-18)

Type granularity had a blind spot that shipped three defects green. Once **any**
member of a type was referenced, every other method, overload and property on it
was invisible to the gate — so `Ndcg` reached by its unweighted call hid the
`sampleWeight` parameter #223 had added, `Silhouette` reached by `Score` hid two
of its four public methods, and `BpeVocabulary` constructed several times hid
every one of its properties. Those are #262, #263 and #264. A samples audit over
nine merged pull requests found that type-level coverage was in fact **intact** —
all 14 new public types were reached — and that every gap it found was
member-level. That is the argument for moving the gate down a level rather than
widening it sideways.

**What the gate now demands.** One member reference per public method,
constructor and property of every exported type. A property counts as reached by
either accessor, since an object initializer emits the setter and a `Console`
line the getter. Enums keep the exception they already had, for the reason
already given: their members are compile-time constants that leave a type
reference and never a member one, so naming the type is all a consumer can do.

**What it cost.** The exclusion list went from **2 entries to 44**, and the sample
gained roughly 56 calls. The list did not become forty paragraphs nobody reads,
because the growth is three shapes rather than forty cases, and each shape carries
one reason:

| shape | entries | why no sample can reach it |
| --- | --- | --- |
| a record's synthesised `Equals`/`GetHashCode` | 17 | a consumer compares *with* it, never calls it, so no line emits a reference |
| the `…Async` twin of a loader called synchronously | 14 | a console sample reading a committed fixture has no honest reason to await, and calling both demonstrates the API twice rather than the package once |
| a result record the library constructs | 9 | its properties are exercised; constructing one by hand is what a consumer never does |
| [`PrecompiledNormalizer.FromCharsMap`](../reference/embeddings/tokenization/precompilednormalizer-fromcharsmap.md) / [`.Normalize`](../reference/embeddings/tokenization/precompilednormalizer-normalize.md) | 2 | a charsmap is a binary trie inside a `spiece.model`, and model artifacts are never committed; measured, it refuses an empty blob and a four-zero-byte header with different sentences, so there is no input to pass |

The existing discipline survives the growth: every entry carries a reason a
reviewer can disagree with, and a key naming a member that no longer exists still
fails the gate, so the list cannot rot into a silent omission. Type-level keys
remain valid and exclude a whole type, which is what the two original entries are.

**What this granularity does not catch, measured rather than assumed.** An
**optional parameter** emits the same member reference whether it is passed or
omitted — `Score(a, b, c)` and `Score(a, b, c, sampleWeight)` are one name in
metadata — so reverting #262 does **not** fail this gate. Keying on arity as well
as name does catch it, and was tried: it takes the judged surface from 383 members
to 679 with 339 uncovered, which is a different decision about how much the sample
must demonstrate rather than a refinement of this one. It is left open deliberately,
and the narrower form the issue suggests — detecting a defaulted parameter no
sample ever passes, on its own — is the cheaper route to the same catch.

Reverting #263 **is** caught, checked by doing it: the gate reports
[`Silhouette.ScoreFromDistances`](../reference/metrics/clustering/silhouette-scorefromdistances.md) and
[`Silhouette.PerSampleFromDistances`](../reference/metrics/clustering/silhouette-persamplefromdistances.md) by name.
