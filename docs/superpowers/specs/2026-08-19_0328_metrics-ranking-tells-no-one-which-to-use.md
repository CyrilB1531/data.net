# 0328 — metrics/ranking has six sections and none tells a reader which to use

**Issue:** [#0328](https://github.com/CyrilB1531/lodestar/issues/0328) · **Status:** accepted — **retrospective**, written 2026-08-29 from the commits that closed it · **Date:** 2026-08-19

## Problem

Six sections, each explaining a property — how the gains are shaped, what happens to ties, why `Dcg` takes a `logBase` and `Ndcg` does not. **Read together they contain the routing. Nothing assembled it**, so a reader who did not already know which metric they wanted had to read all six to find out.

## The branch the page had already named

**The input shape**, and the page's own headings had said it: *"One ordered list, four types"* against *"A label matrix, and the rank as a count"*. It is load-bearing rather than descriptive — the two families share no input and cannot be substituted for one another — so it is the first branch.

## What shipped

The diagram, routing the eight by the input shape they refuse to share.
