# 0258 — Should the gate confront an XML exception tag with the page that documents it

**Issue:** [#0258](https://github.com/CyrilB1531/lodestar/issues/0258) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

A member's `<exception cref>` tags and its reference page's **Exceptions** rubric could disagree with nothing noticing. The question was whether a gate should hold them to the same set — and the answer had to come with evidence about how often they actually drift.

## What the evidence was

Emptying `Lodestar.Embeddings`' `exceptionsUnchecked` turned the parity gate on for Onnx, Persistence, Pooling, Search and Tokenization, and it found **thirty disagreements**. Five were pages silent about an exception the source declares; **the rest were sources silent about one the page names**.

**Each was read as a claim about behaviour rather than copied across the gap.** Thirteen were reproduced against running code — the pooling shape refusals, the index's dimension and `k` bounds, `VectorMath.Dot`'s length check, the loaders' null argument, `BatchEncoder.Encode` over `MaxLength` with truncation off.

## What was decided

The gate compares the **set** of types, in either order, and does not compare the sentence around each — *when* it is thrown stays a review question. Namespaces still owing parity are named in `exceptionsUnchecked`, **a list that only ever shrinks**. [ADR 0038](../../decisions/0038-the-gate-confronts-an-exception-tag-with-the-page-that-documents-it.md) is the decision.
