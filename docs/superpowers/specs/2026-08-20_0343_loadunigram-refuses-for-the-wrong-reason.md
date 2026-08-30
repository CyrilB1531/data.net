# 0343 — LoadUnigram refuses a Llama-2 tokenizer.json for the wrong reason

**Issue:** [#0343](https://github.com/CyrilB1531/lodestar/issues/0343) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

The refusal was correct and its **reason** was not. A Llama-2 `tokenizer.json` declares a BPE model, so `LoadUnigram` should refuse it for declaring the wrong `model.type` — and instead it failed further in, on a detail of the file, giving a message that sent the reader looking in the wrong place.

## Why the reason matters as much as the refusal

A `tokenizer.json` is **one filename covering three models** — the same fact [#326](https://github.com/CyrilB1531/lodestar/issues/326) built its diagram around. A caller choosing a loader by filename will meet a refusal, and **the message is the whole of what tells them which loader they wanted.**

## What shipped

The assertion on `model.type` moved ahead of the details, so each loader refuses by name and says which one to reach for instead. [#315](https://github.com/CyrilB1531/lodestar/issues/315) later gave the lineage its actual path.
