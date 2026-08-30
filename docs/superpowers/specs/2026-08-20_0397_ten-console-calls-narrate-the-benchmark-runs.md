# 0397 — Ten Console calls narrate the benchmark runs

**Issue:** [#0397](https://github.com/CyrilB1531/lodestar/issues/0397) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

**A library that writes to a console its caller did not open decides for an application it cannot see**, and `src/` has never done it. Under `bench/` ten calls narrating a run had accumulated, each individually harmless, with nothing stopping an eleventh.

## What was decided

A guard rather than a ban. `tools/check_no_console_writeline.py` refuses an unexplained `Console` call under `src/` or `bench/`; a call that earns its place carries `// console-print: <reason>` **on the call itself or the line directly above it**.

**Six of the ten went. The four that remain carry what no file does and say so on the line**, where a reviewer can disagree with the reason — which is the bargain a `#pragma warning disable` strikes, and the same one [#187](https://github.com/CyrilB1531/lodestar/issues/187) made for `long-comment:`.

## The trap the marker's placement carries

**Directly above** means directly: a two-line comment puts the marker one line too far up and the guard fails. This session met that exact failure on a later branch.
