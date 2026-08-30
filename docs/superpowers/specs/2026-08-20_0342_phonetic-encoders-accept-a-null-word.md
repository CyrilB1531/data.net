# 0342 — Phonetic encoders accept a null word where the stemmers throw

**Issue:** [#0342](https://github.com/CyrilB1531/lodestar/issues/0342) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

`Soundex.Encode(string)`, `Nysiis.Encode(string)` and `Metaphone.Encode(string)` returned `""` on a `null` word. **The seven stemmers next door throw `ArgumentNullException` on the same input.** Two neighbouring families in one package, disagreeing on the most basic contract there is.

## Why silence is the worse answer

An empty code is a **valid** phonetic code. A caller passing `null` by accident gets a value that flows on and matches other empty codes, rather than an exception at the point of the mistake.

## What was decided

**Breaking**: all three throw. [ADR 0042](../../decisions/0042-phonetic-encoders-refuse-a-null-word.md) records it, and the changelog carries it as a breaking change rather than as a fix — the distinction a consumer needs.
