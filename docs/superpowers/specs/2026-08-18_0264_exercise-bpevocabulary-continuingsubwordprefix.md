# 0264 — Exercise BpeVocabulary.ContinuingSubwordPrefix

**Issue:** [#0264](https://github.com/CyrilB1531/lodestar/issues/0264) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-18

## Problem

The third of the three sample gaps, and the same cause: a property on a type the gate already counted as reached.

## What shipped

The property exercised, and with it the case for [#265](https://github.com/CyrilB1531/lodestar/issues/265): **383 members, 383 referenced, 44 documented exclusions — up from 2.** The exclusion count rising is the honest part; a gate that goes from type to member granularity surfaces members that genuinely cannot be reached from a sample, and each now carries a reason rather than being silently covered by a sibling.
