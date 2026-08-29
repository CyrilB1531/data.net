# 0232 — Reference pages for Lodestar.Embeddings.Persistence

**Issue:** [#0232](https://github.com/CyrilB1531/lodestar/issues/0232) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

**The index is organised around the split that actually decides a call: what the file carries against what stays a parameter.** `vocab.txt` records nothing but its tokens, so lowercase is a parameter and **getting it wrong is silent**; `spiece.model` and `tokenizer.json` carry their settings, so those loaders read them.

**Refusing a model is documented as a feature rather than a gap** — stock BERT by `LoadWordPiece`, Llama-2 and Mistral v0.1 by `LoadBpe`, each named. A reader who meets a refusal should find it in the page, not in an exception.

## What shipped

An index, five type pages and twelve member pages, with the `covered` entry in the same commit. 5 types, 28 members.
