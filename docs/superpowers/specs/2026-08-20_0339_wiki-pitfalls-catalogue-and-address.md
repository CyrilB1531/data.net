# 0339 — Wiki pitfalls: catalogue and address the ones this project can handle

**Issue:** [#0339](https://github.com/CyrilB1531/lodestar/issues/0339) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits and decisions that closed it · **Date:** 2026-08-20

## Problem

An epic. The wiki accumulated claims and gaps that a reader arriving from Python would fall into, and nothing had gone through them as a set to sort those this project can act on from those it cannot.

## What it produced

Sub-issues, each a pitfall with an owner or a reason it has none. The ones that shipped are the useful record: [#340](https://github.com/CyrilB1531/lodestar/issues/340) a promised bridge that should never be built, [#341](https://github.com/CyrilB1531/lodestar/issues/341) a real divergence above the BMP, [#342](https://github.com/CyrilB1531/lodestar/issues/342) an API asymmetry, [#343](https://github.com/CyrilB1531/lodestar/issues/343) a refusal given for the wrong reason.

**The pattern across them is worth naming**: three of the four were *documentation making a promise the code did not keep*, and only one was a defect in the code. A pitfall catalogue is mostly an audit of claims.
