# 0356 — The nightly generates a benchmark_latest.md its own Lint job refuses

**Issue:** [#0356](https://github.com/CyrilB1531/lodestar/issues/0356) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-27

## Problem

The lint job failed on a generated page: **two byte-identical `### …BlockedTableBenchmarks-report-github` headings**, which markdownlint's MD024 refuses. Both carried the same preamble — same job, same host, same commit — and different numbers, so **the class was measured twice in one run.**

## What was decided

`included_reports` emitted one `### {stem}` per path with **nothing checking that a stem appears once**. Later occurrences are now numbered `(run N)`.

**Numbering rather than dropping is the deliberate half.** The second measurement is real data, it is almost certainly not wanted, and a reader who can see it is the one who can go and remove whatever produced it. Dropping it would hide the cause along with the symptom.

## The edge the tests pin

**An empty report must not consume an occurrence** — a report skipped for being empty must not push the next one to `(run 2)`, because the numbering counts what the page shows, not what the directory held.
