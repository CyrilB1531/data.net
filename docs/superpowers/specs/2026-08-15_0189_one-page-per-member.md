# 0189 — One page per member, with a type page and a namespace index above it

**Issue:** [#189](https://github.com/CyrilB1531/data.net/issues/189) ·
**Status:** implemented · **Date:** 2026-08-15

## Problem

`Text-distances` is 1034 lines and 22 members; `Metrics-classification` is 1646 lines and
`Metrics-regression` 1333. A reader after one method opens all of it and scrolls.

Splitting `DataNet.Metrics` over two pages during #181 was the wrong repair: it halved a page that
should not have existed at that size. The .NET API reference every C# reader already knows has three
levels — namespace, type, member — and the current pages imitate all three inside one file, with
`##`, `###` and `####` standing in for navigation.

## Decisions

### D1 — the unit is the member, and two levels sit above it

A member page documents one member: its overloads, its declaration block, its parameters, its
return, one worked example, its remarks, its *Applies to*. Overloads stay together — they share a
name, a purpose and an example, and the gate already groups them by name.

A type page carries what is true of the type rather than of one member: what it is for, the
invariants its members share, and a table linking every member page. A namespace index carries the
prose that orients — the conventions, the "which one do I want?" diagram — and a table linking every
type page.

### D2 — the layout, and why the member sits beside its type rather than under it

```text
docs/reference/text/distances.md                        namespace index
docs/reference/text/distances/levenshtein.md            type
docs/reference/text/distances/levenshtein-distance.md   member
```

Two levels below `docs/reference/`, never three. A wiki page name is flat
(`Text-levenshtein-distance`), so directory depth buys nothing there, and one glob —
`docs/reference/*/*/*.md` — reaches every page a namespace holds. A member under its own type
directory would need a third level in every glob and in `build_wiki`'s naming for no reader-visible
gain.

The stem is `<type>-<member>`, lower-cased with the dot dropped, which is the slug rule
`ReferenceDocumentation.Anchor` already computes for the in-page anchors these links replace.

### D3 — `covered` maps a namespace to its directory

`wiki-map.json` today maps a namespace to a page or to a list of pages — the relaxation
`DataNet.Metrics` forced in #181. With one page per member a list would be 42 entries maintained by
hand, so it becomes a directory:

```json
"covered": { "DataNet.Text.Distances": "docs/reference/text/distances" }
```

The index page is `<directory>.md` beside it, by construction rather than by declaration.

### D4 — the gate keeps its four checks and gains two

Unchanged: every exported type is documented, every public method is documented, a declaration block
lists exactly the overloads reflection reports, *Applies to* names the frameworks that really export
the member. What moves is where each is found — a member page holds one entry, so its `H1` is the
title the parser used to read from `####`.

Two checks the split makes possible, and which replace what the single page got for free:

- a type page's member table links every member page of that type, and nothing else;
- the namespace index links every type page.

Neither is decoration: with the entries in separate files, a member page can now exist and be
reachable from nothing.

### D5 — a link into the reference is a link to a page

Roughly 90 links across the ADRs, `equivalence.md`, the guides and the migration inventory read
`reference/text/distances.md#levenshteindistance`. They become
`reference/text/distances/levenshtein-distance.md`. `CheckLinks`' two obligations survive with their
targets changed: a backticked member in prose is a link, and a page that names a member links its
page at least once.

### D6 — the split is mechanical, and thrown away

The 64 existing entries are moved by a one-shot script, not by hand: the parser that the gate
already has knows where each entry starts and ends. The script is deleted in the same pull request
that lands the pages, like #181's wiki probe — its output is reviewed, not its code.

What the script cannot do is write the type pages' prose, which does not exist yet: today a type's
role is a row in the opening table. That is written by hand, per type, and it is the one part of
this that is not mechanical.

### D7 — lots, and the gate stays green between them

`DataNet.Text.Distances` first — 9 types, 22 members, the pilot that proves the machinery.
`DataNet.Metrics` second, where the pain is worst — 31 types, 42 members. `covered` is per
namespace, so a namespace that has not moved yet keeps its single page and its gate, and the two
shapes coexist for exactly as long as the lots take.

## What this costs

- **About 100 new files for the two lots**, and roughly as many wiki pages. Flat names keep them
  addressable; `_Sidebar.md` does not list them, the index and type pages do.
- **The doc-snippets gate is unaffected in kind** — the same fences, in more files. Its class names
  are already path-derived, so a third path segment only has to reach `SOURCES`.
- **`git blame` on a reference entry breaks** at the move. Accepted: the entries are two days old.

## Risks

- **A member page is short enough to be worth nothing.** A page holding a declaration and three
  lines of prose is worse than a section. Mitigation: the type page carries what is common, so the
  member page is what is specific — and where that leaves nothing to say, the reference is telling
  us the member needs no entry, which the gate will refuse. Watch for it in the pilot before
  committing to the second lot.
- **Two shapes at once.** Between lots, `ReferenceDocumentation` supports both a single-page
  namespace and a directory, which is a branch in the gate with a limited life. It is deleted when
  the last namespace moves, and the spec says so here so that the deletion is not forgotten.

## What the implementation added to this spec

- **A wrap must know where a fence starts.** Re-flowing the generated pages' prose split lines
  inside declaration blocks, and the gate refuses a declaration that no longer matches the signature
  reflection renders — measured, 22 complaints. The throwaway wrapper skips fenced lines.
- **A member summary may contain a pipe.** `MeanAbsolutePercentageError`'s reads
  `|yTrue - yPred| / |yTrue|`, which the generated member table read as cell boundaries. Escaped.
- **`CheckLinks` accepts both shapes.** A member is reachable by the anchor of a combined page or by
  a link to its own page, because the two coexist while namespaces move one lot at a time. When the
  last namespace has moved, the anchor half goes.
