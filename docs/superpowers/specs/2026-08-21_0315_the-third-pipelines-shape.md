# 0315 — The third pipeline's shape: one tokenizer or two, and where Metaspace lives

**Issue:** [#0315](https://github.com/CyrilB1531/lodestar/issues/0315) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-21

## Problem

The SentencePiece-BPE lineage — Llama-2 and Mistral v0.1 — needed a loading path, and the issue asked whether that is one tokenizer or two, and where Metaspace belongs.

## What reading the files answered, which the issue had not asked

**The two named files do not declare the same thing.**

- **Llama-2** spells metaspace as a **normalizer** `Sequence` of `Prepend` and `Replace`, with a null pre-tokenizer.
- **Mistral** declares a **Metaspace pre-tokenizer**, with a null normalizer.

A design teaching `BpePreTokenizer` about Metaspace **would have loaded one and refused the other** — the half-a-view failure this lot exists to prevent. Deciding the shape before the code existed is what caught it.

## What was decided

**The lineage stays a BPE model**, because that is what `model.type` says and a class name contradicting the file it loads is a lie a reader pays for. Metaspace becomes **one transform** reachable from either declaration site. [ADR 0050](../../decisions/0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md) records it, amending [0017](../../decisions/0017-bpe-parity-scope.md) §3 — whose `byte_fallback` refusal and "no path here" for Llama-2 and Mistral v0.1 both fall.
