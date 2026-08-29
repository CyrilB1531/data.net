# 0236 — Reference pages for Lodestar.Text

**Issue:** [#0236](https://github.com/CyrilB1531/lodestar/issues/0236) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

**Parent:** [#204](https://github.com/CyrilB1531/lodestar/issues/204), one lot of twelve.

## The shape every lot of #204 follows

An index, a page per type, a page per public method, and **the `covered` entry in the same commit** — the entry turns the gate on for the namespace, so it cannot land before the pages it would fail on. Declarations are replayed against both target frameworks' assemblies and examples are executed, so a signature that drifts fails CI rather than a reader.

## What this lot found

The last of the twelve and the smallest: **one type and no members**, the root namespace's single exported type. It matters only for what it completes — with its `covered` entry the twelve are done and no namespace #204 named is outside the gate.

## What shipped

The index, one type page, and the entry that closes #204.
