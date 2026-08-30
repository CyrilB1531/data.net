# 0341 — Byte-level BPE split diverges from HuggingFace above the BMP

**Issue:** [#0341](https://github.com/CyrilB1531/lodestar/issues/0341) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

**Neither half of a surrogate pair is ever `\p{L}` or `\p{N}` on its own**, so a rune above the Basic Multilingual Plane fell to whichever alternative a vendored pattern uses for punctuation-or-other, not the one HuggingFace takes.

## Confirmed against the reference before anything moved

Against the real `tokenizers` library **and** the current C# regex directly, for a string of bold mathematical letters and digits:

- `llama3` produced **4 pieces where HuggingFace produces 5**;
- `qwen2` produced **6 where HuggingFace produces 9**.

## What was decided

**Classify by rune, not by UTF-16 half.** `Apply()` matches against a shadow of the input in which each surrogate pair is represented by a character of the same category as the rune it encodes, so the vendored pattern sees what it was written to see.

Reading a reference implementation to diagnose one failing case is diagnosis and is fine; the pattern itself stays vendored under its own licence, per [ADR 0003](../../decisions/0003-licensing-and-provenance.md).
