# 0235 — Reference pages for Lodestar.Embeddings.Onnx

**Issue:** [#0235](https://github.com/CyrilB1531/lodestar/issues/0235) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

The same discovery on the third package: **`Lodestar.Fuzzy` also ran no reference gate**, so this lot wired one for it as well as documenting ONNX inference. A `covered` entry without a gate is prose nothing reads.

## What shipped

Five pages for `OnnxTextEmbedder` — 1 type, 7 members.
