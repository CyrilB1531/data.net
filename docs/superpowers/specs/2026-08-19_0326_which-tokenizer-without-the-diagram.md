# 0326 — embeddings/tokenization asks 'Which tokenizer?' without the diagram

**Issue:** [#0326](https://github.com/CyrilB1531/lodestar/issues/0326) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

The index carried the heading every sibling draws under and answered it with **a table keyed on the file on disk**. That table is right, and incomplete in the one place a reader gets stuck.

## The branch the prose was not drawing

**A `tokenizer.json` is a single filename covering three models.** The three `Load` methods each assert `model.type` and refuse a file declaring another — so **choosing by filename reaches a refusal rather than a tokenizer.**

## What was decided

The flowchart branches on **what the reader can see**: the file first, then what it declares. That is the sequence a reader actually performs, and it is why the diagram is not a restatement of the table.
