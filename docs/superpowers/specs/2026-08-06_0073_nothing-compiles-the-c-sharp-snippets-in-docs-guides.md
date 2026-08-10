# Design — #73: compile the guides' C#, from the guides themselves

**Date:** 2026-08-06 · **Issue:** #73 · **Branch:** `docs/73-compile-guide-snippets` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

markdownlint validates the Markdown and does not know C# exists. So until now **a
renamed method could not fail anything the guides are in** — the quickstart, the
vectorization guide, the embeddings guide, the README.

Documentation that no longer compiles is worse than no documentation: a reader
copies it, it fails, and they conclude the library is broken.

## The shape decision the issue left open

Three options, and the difference matters more than it looks.

- **A `docs-samples` project holding a second copy of each snippet.** This converts
  "documentation that lies" into "documentation that lies while a project compiles
  nearby" — **nothing forces the two to agree**.
- **Marker-based inclusion (MarkdownSnippets).** Needs a sync tool *and* a drift
  check, and moves the text a reader edits out of the file they are reading.
- **Extraction from the Markdown itself.** The Markdown stays the single copy.

## Decisions

### D1 — Extraction, so drift is impossible rather than detected

`tools/extract_doc_snippets.py` reads every ` ```csharp ` fence in `README.md` and
`docs/guides/`, and emits code into `samples/DataNet.DocSnippets/Generated/`,
git-ignored and rebuilt on every run.

**There is no second copy**, so a snippet cannot drift. It also adds no syntax to
files that are plain Markdown today.

### D2 — One method per fence

Which is what lets `vectorization.md` declare `cv` twice on the same page without
colliding. A guide is written for a reader, not for a compiler, and a page that
reintroduces a variable is normal prose.

### D3 — `using` lines are hoisted to the compilation unit

So a fence inherits the usings a reader would already have in scope from earlier
on the page.

### D4 — Compiled against the **packed** packages

Not the projects. **A snippet that only compiles through a `ProjectReference` is
not one a reader can run.** Same `NUGET_PACKAGES` separation as the sample job,
and for the same reason (ADR 0009).

### D5 — An opt-out that a reviewer can disagree with

A fence that genuinely cannot compile carries
`<!-- docs-compile: skip - reason -->` on the line above it, **and the reason has
to be one a reviewer can disagree with**. Same bar as an analyzer suppression.

### D6 — `SnippetContext.cs` supplies what the prose introduces without showing

Guides legitimately say "given a corpus…" without a declaration. That context is
written once, by hand, rather than forcing every page to become a compilable
program.

### D7 — It joins the definition of done

A line in `CONTRIBUTING.md`, and a `Guide snippets compile` CI job. A gate not
listed where contributors read is a gate they discover by failing.

## Out of scope

- Executing the snippets. Compiling proves the API exists and the signatures
  resolve; running would need fixtures per page.
- Non-C# fences.

## What "done" means

Every ` ```csharp ` fence in `README.md` and `docs/guides/` extracted and compiled
against the packed packages in CI; the generated tree git-ignored; the opt-out
documented; the four SonarQube findings on the extractor answered.
