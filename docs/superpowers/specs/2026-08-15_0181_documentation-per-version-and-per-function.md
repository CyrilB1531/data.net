# 0181 — Documentation published per version, and a reference entry per function

**Issue:** [#181](https://github.com/CyrilB1531/data.net/issues/181) · **Date:** 2026-08-15 ·
**Branch:** `docs/181-documentation-per-version-and-per-function`

## Context

Two defects, and they compound. The documentation describes the working tree rather than what is on
the feed, and it never says what a given function is *for*.

### What was measured

- The prose is `README.md` and five pages under `docs/guides/` — 1 723 lines together. **No page names
  a version.**
- `src/DataNet.Text/Version.props` declares `0.4.0`; `0.3.0` is what shipped on 2026-08-14. Every guide
  on `main` already describes a version nobody can install.
- `GenerateDocumentationFile` is on for every project, and the packed XML files carry **557 methods,
  165 types and 170 properties** across the four packages. The `<summary>` elements are one-liners
  written for a reader who already knows the algorithm.
- `grep -c "<example>"` over `src/**/*.cs` returns **one** match, in
  `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs` — an internal loader. The public surface
  has none.
- The CI markdownlint step already globs `docs/**/*.md`, so pages added under `docs/` are linted with
  no workflow change.
- `tools/extract_doc_snippets.py` carries its sources in the module body:
  `SOURCES = ["README.md", "docs/guides/*.md"]`.
- `.github/workflows/release.yml` already triggers on `tags: ['DataNet.*/v*']`, splits the tag into
  package and version, and refuses a tag that disagrees with the declared version.
- The repository has its wiki enabled (`has_wiki: true`), and the wiki is a separate git repository,
  `data.net.wiki.git`.

## Decisions

### D1 — the reference is written by hand, never generated from the XML comments

The XML comments are one line each and carry no example. Generating a reference from them would
publish that thinness at scale and give a reader nothing they cannot already get from IntelliSense.
The prose that answers "what is this for, and why this one rather than its neighbour" does not exist
anywhere yet and has to be written.

This does not stop the XML comments improving later. It says they are not the source of this
documentation, so nothing here is blocked on them.

DocFX over enriched XML comments was weighed, because it is how Microsoft actually produces the
layout D3 adopts, and rejected on three counts. The prose would leave `docs/`, so markdownlint and
the doc-snippets compilation would stop seeing it. The eight-line budget on XML prose that
`tools/check_comment_length.py` enforces would need its `long-comment:` escape on essentially the
whole public surface, which does not break the rule so much as hollow it out. And the reference
would then be pinned to the source tree rather than to a released tag, which is the defect this
issue exists to fix.

### D2 — the pages live in this repository; the wiki is an output

A page written straight into the wiki escapes every gate the project has: markdownlint, the
doc-snippets compilation, and review in a pull request. On a repository whose culture is that the
barrier catches things before the reader does, that is not an acceptable trade.

So the pages are authored under `docs/`, reviewed in a pull request like any change, and a workflow
copies them into the wiki. The reader sees a wiki; the author keeps the guard rails.

### D3 — one page per area, entries laid out like the .NET API reference

Layout added to the repository:

```text
docs/
  guides/                 unchanged
  reference/
    text/                 distances.md, phonetics.md, set-similarity.md, stemmers.md,
                          tokenizers.md, vectorizers.md, persistence.md
    embeddings/
    fuzzy/
    metrics/
  wiki-map.json
```

The shape of a page and of an entry follows the .NET API reference on `learn.microsoft.com`, because
that is the layout a .NET reader already knows how to read. Measured on `String.Compare` and
`StringBuilder`: a type page opens on *Definition* (namespace, assembly, declaration, inheritance),
then *Examples*, then *Remarks* broken into thematic subheadings, then member tables of two columns,
name and description. A member page opens on *Definition*, then an overload table, then, per
overload, the declaration, *Parameters*, *Returns*, *Exceptions*, *Examples*, *Remarks*, *See also*
and *Applies to*.

A page here carries one area rather than one type, so the structure collapses by one level:

- the page opens on a sentence saying what the area is for, and a member table — name and
  description, one row per type and per method;
- a `###` entry per exported type, carrying its declaration, its properties and fields described in
  place, and its *Remarks*;
- a `####` entry per public method, all overloads of one name sharing the entry.

A method entry carries, in this order: a one-sentence summary, the declaration, *Parameters*,
*Returns*, *Exceptions*, *Example*, *Remarks*, *Applies to*, *See also*. Empty rubrics are omitted
rather than filled with "none".

**The beginner-facing prose lives in *Remarks*.** That is where "what is this for, when would I
prefer it to its neighbour, and what is the trap" is written, in plain language, and it is the half
of the entry that matters most for the reader this issue is about. The rubrics around it are what
make the page navigable; *Remarks* is what makes it useful.

***Applies to* is not decoration here.** The packages ship `net10.0` and `netstandard2.0` from one
assembly, and `VectorMath.Dot` is a deliberate behavioural split between them. The rubric states
which targets export the member, and the note where behaviour differs.

The Python counterpart is **not** repeated in the entry. `docs/equivalence.md` already carries that
mapping, and a second copy would be a second truth; the entry links to it under *See also*.

**Declaration fences carry a marker.** A declaration is a signature, not a statement, so compiling it
as a snippet would fail. It is written as a `csharp` fence preceded by `<!-- docs-declaration -->`,
which has two readers: `tools/extract_doc_snippets.py` excludes it from compilation, and the gate of
D7 takes it as the signature to compare against reflection. One marker, two consumers, and no way to
write a declaration the gate cannot find.

Worked example, which the distances page will match:

```markdown
#### Levenshtein.Distance

Counts the fewest insertions, deletions and substitutions that turn one string into the other.

<!-- docs-declaration -->

    public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement unit)

**Parameters** — `a`, `b`: the two strings to compare. `unit`: what counts as one
character, `Utf16` by default or `CodePoint` to match Python outside the Basic
Multilingual Plane.

**Returns** — `int`, the number of edits. Zero when the strings are equal.

**Example**

    int d = Levenshtein.Distance("kitten", "sitting");   // => 3

**Remarks** — this is the ordinary answer to "how different are these two texts",
and the right tool for typing mistakes and mis-keyed names; to compare sets of
words rather than characters, Jaccard is better. The trap is that the result is
not bounded: 3 is enormous between two short words and negligible between two
paragraphs, so `NormalizedSimilarity` is what you want for a score in [0, 1].

**Applies to** — net10.0, netstandard2.0.

**See also** — `NormalizedSimilarity`, `Indel.Distance`, the equivalence table.
```

### D3b — Mermaid diagrams where they show a mechanism

A page may carry `mermaid` fences. The GitHub wiki renders them natively, markdownlint does not look
inside a fence, and the snippet extractor takes only `csharp` fences, so a diagram passes every gate
without changing one.

A diagram earns its place by showing a mechanism the prose cannot hand a reader in one glance — the
vectorization pipeline from text to `CsrMatrix`, the encoding pipeline in `DataNet.Embeddings`, or a
decision tree answering "which distance do I pick". It is not decoration, and a diagram that only
restates the sentence above it is removed in review.

### D4 — the wiki follows `main`, and a tag archives a copy

`Text/` always mirrors `main`. Pushing `DataNet.Text/v0.4.0` freezes a copy of that tree into
`Text/0.4.0/`, which is never rewritten afterwards. Every archived version stays.

**The cost, recorded because it was chosen with it known:** a reader who lands on `Text/` reads the
development tree, which is defect 1 with an archive added. It was preferred to publishing from the
tag alone, because that would have left the wiki silent between releases — the requirement was that
the wiki be current before the next release, not only at it.

What reduces the harm without changing the model: every page in a live channel carries a generated
banner naming the latest released version of that package and linking to its archived directory. The
banner is written by the publisher, never by hand, so it cannot go stale.

### D5 — `wiki-map.json` says which pages belong to which package

One file declares, per package, the pages that ship with it and the reference areas that are
**covered**. The cross-cutting guides are attached to the package they demonstrate — `quickstart.md`
and `vectorization.md` to `DataNet.Text`, `embeddings.md` to `DataNet.Embeddings`,
`migrating-from-rapidfuzz.md` to `DataNet.Fuzzy`. A guide is therefore archived by the tag of the
package whose API it shows, which is the only tag that can make its code true.

The pages whose subject is the project rather than an API — `docs/decisions/`, `docs/migration/`,
`docs/guides/performance.md` — sit at the wiki root and belong to no package. They follow `main` and
are never archived: `performance.md`'s numbers are already dated by machine and window inside the
page, and versioning it per package would attach one page to four numbers.

The same file's `covered` list drives the coverage gate of D7. Using one declaration for both means
the split and the gate cannot disagree.

### D6 — the snippets are executed, not merely compiled

Today `samples/DataNet.DocSnippets` is built and never run, so a result written in a page is checked
by nobody. That result is precisely what the issue asks for, so it becomes a gate.

The expected value is written as a trailing `// =>` marker on the statement that produces it. The
extractor turns that marker into an assertion, so there is no second copy of the value to drift —
the same principle that already makes the fences the single source of truth:

```csharp
int d = Levenshtein.Distance("kitten", "sitting");           // => 3
double s = Levenshtein.NormalizedSimilarity("kitten", "sitting");  // => 0.5714…
```

Three rules make that unambiguous:

- Only `// =>` is an assertion. A plain `//` stays an explanatory comment, so the fences already in
  the guides keep their present meaning and this change does not turn them into assertions.
- The comparison is on the value's invariant-culture string form.
- A trailing `…` in the expected text means prefix match, which is how the guides already write
  irrational results such as `0.5714…`.

A fence with no value to assert — one that writes a file, loads an ONNX model, or prints — opts out
with a marker on the line before it, in the shape of the `<!-- docs-compile: skip -->` marker the
extractor already understands.

### D7 — the gate: coverage, and the mechanical half checked against the assemblies

Every exported type gets an entry, and every public method gets an entry, with all overloads of one
name sharing a single entry. Properties, fields and constants are described inside their type's
entry rather than each getting one: an entry per property would be some 900 entries, most of them a
hollow sentence, and four lots that never finish.

Microsoft derives the declaration, the parameter list and *Applies to* from the assemblies. Here
they are written by hand, so the gate is what replaces that derivation. An xunit test in each
package's existing test project reflects over `GetExportedTypes()` and asserts four things:

1. every required type and method has its entry, restricted to the areas `wiki-map.json` declares
   covered;
2. the `<!-- docs-declaration -->` block lists exactly the overloads reflection reports, compared as
   normalised text against a signature rendered from the `MethodInfo` — same parameter types, same
   order, same optional parameters;
3. every parameter named in reflection appears in the entry's *Parameters* rubric;
4. *Applies to* names the targets that actually export the member, which is the one check that can
   catch a `net10.0`-only path being documented as if it existed on `netstandard2.0`.

Rendering a signature from a `MethodInfo` so that it reads as a reader would write it —
`ReadOnlySpan<char>` rather than the reflection spelling — is the real implementation cost of this
decision, and it is paid once.

The reference Markdown reaches the test through `<Content Include>` copied to the output directory,
read from `AppContext.BaseDirectory` — the pattern the oracle fixtures already use. The `Content`
item is declared in `tests/Directory.Build.props` so the `*.NetStandard.Tests` projects, which link
sources rather than share project settings, get it too. Running on both target frameworks is also
what makes check 4 possible at all: each assembly reports its own exported surface.

Restricting to declared areas is what lets this land before the four lots exist. Without it the
foundation pull request is red until all four are finished.

### D8 — the publisher is a tool, not YAML

`tools/build_wiki.py` reads a checkout plus `wiki-map.json` and produces the wiki tree: the package
channels, the archived directories, the generated `_Sidebar.md` and `Home.md`, and the banner of D4.
The workflow clones the wiki, runs the tool, commits and pushes. Logic in a tool is testable and
reviewable; logic in YAML is neither, and this repository already keeps its checks in `tools/`.

Two triggers, one tool:

- `push` on `main` refreshes each package's live channel and the root pages.
- `push` of a tag matching `DataNet.*/v*` writes the archived directory for that package and version.

A `workflow_dispatch` taking a ref lets a failed publication be repeated by hand, and lets the
workflow be proved before anything depends on it.

## Scope of this issue

The foundation, and one area as proof:

1. `docs/wiki-map.json`, `tools/build_wiki.py`, and the publishing workflow.
2. The entry convention, written in `CONTRIBUTING.md` — the process document, per the table in
   `CLAUDE.md`.
3. `tools/extract_doc_snippets.py` extended to `docs/reference/**/*.md`, taught the `// =>` assertion
   marker, and taught to skip a `<!-- docs-declaration -->` block.
4. `samples/DataNet.DocSnippets` run in CI rather than only built.
5. The gate of D7, in the four test projects, active only on declared areas, with the signature
   renderer it needs.
6. `docs/reference/text/distances.md`, written against the convention — including at least one
   Mermaid diagram, the "which distance do I pick" decision tree — and declared covered.

Four lots follow, one issue each: the rest of `DataNet.Text`, then `DataNet.Embeddings`,
`DataNet.Fuzzy`, `DataNet.Metrics` — each on a template that will already have survived a release.

## Risks and accepted costs

- **The default landing page shows unreleased prose.** Accepted in D4, mitigated by the generated
  banner. It is the price of a wiki that is current between releases.
- **Pushing to the wiki repository needs a credential.** `GITHUB_TOKEN` with `contents: write` is
  expected to be enough for `data.net.wiki.git`, and the plan proves it on a throwaway page before
  anything is built on top. If it is not, the fallback is a fine-grained token held as a secret,
  which widens what a compromised workflow could do and would be recorded as its own decision.
- **Executing the snippets lengthens CI and widens what breaks the build.** An example that now
  throws fails the build. That is the intent, and it will be felt most on the packages with I/O.
- **The wiki carries no archived version until the next release.** The tag trees predate the
  reference pages, so `Text/0.3.0/` cannot be reconstructed. The archive starts at the first tag
  pushed after this lands.
- **The .NET layout costs more per entry than prose would.** Declaration, *Parameters*, *Returns*,
  *Exceptions* and *Applies to* are five rubrics a writer fills before reaching the sentence a reader
  came for. D7 is what makes them worth writing rather than a form to fill in — a rubric no gate
  reads would rot. If the signature renderer turns out to cost more than the checking is worth, the
  honest retreat is to drop check 2 and keep checks 1, 3 and 4, not to keep an unchecked declaration.

## Out of scope

Generating an API reference from the XML comments, improving the XML comments themselves, a hosted
documentation site, and the reference pages for the other three packages.
