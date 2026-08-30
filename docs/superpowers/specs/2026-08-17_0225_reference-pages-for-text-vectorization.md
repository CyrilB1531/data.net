# 0225 — Reference pages for Lodestar.Text.Vectorization

**Issue:** [#0225](https://github.com/CyrilB1531/lodestar/issues/0225) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-17

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

**The wiki is flat, and a stem collides across channels.** A page publishes as `{channel}-{stem}`, so `docs/guides/vectorization.md` and `docs/reference/text/vectorization.md` would both publish as `Text-vectorization`, and `build_wiki` refuses rather than overwriting one with the other. This lot gave the new index the same stem the guide had had for months, and broke the Wiki workflow on `main`.

**Renaming the directory is the whole fix**, and costs nothing else: the gate ties an index's *name* to its directory (it looks for `<directory>.md`), but the directory name itself is free. [#238](https://github.com/CyrilB1531/lodestar/issues/238) is the guard that stops the class of fault rather than this instance of it.

## What shipped

The index, 12 type pages, 32 member pages, and the `covered` entry. 12 types, 90 members.
