# 0433 — A benchmark process that saves before it loads is measuring a warmed heap

**Issue:** [#433](https://github.com/CyrilB1531/lodestar/issues/433) ·
**Status:** proposed · **Date:** 2026-08-29

## Problem

`PersistenceCrossLang` measures `embedding_index_save` and then `embedding_index_load` **in one
process**, and the save allocates tens of megabytes. That leaves the large-object heap grown and
its pages already committed for the load that follows, so the load does not pay the page commits
[#324](https://github.com/CyrilB1531/lodestar/issues/324) identified as the irreducible part of its
allocation phase.

[#430](https://github.com/CyrilB1531/lodestar/issues/430) made it visible by removing the subsidy.
It did not create it, and it did not measure its size.

## What is already known, and what it is not

Three estimates, none of them the number this lot needs.

| source | figure | what it compares |
| --- | ---: | --- |
| container, 8 runs, both orders | 1.22× | before/after #430, same process |
| nightly `EmbeddingIndexLoad` mean | 1.10× | same, netting out a 1.21× slower runner |
| nightly `embedding_index_load` row | 1.17× | same, netting out the same runner |

They agree, and the allocation is **35.35 MB on both sides, identical to three digits**, which is
what says the load does the same work and pays for more of it. But every one of them is a *change
in the subsidy between two builds*, not the subsidy's size. The "roughly 20%" now published in
`bench/README.md` §7 is an inference from that band, and this lot is what replaces it with a
measurement.

## What is settled and out of scope

- **Stating the condition** — #433's item 2, first half. Done in
  [#451](https://github.com/CyrilB1531/lodestar/pull/451): `bench/README.md` §7 says every
  `embedding_index_load*` row is flattered, and that a load row is not a control for a save change.
- **An allocation-free control** — item 3. Done: `save-phases` carries `block_copy_floor`, a
  `memcpy` that allocates nothing and so cannot be subsidised.
- **Changing the load path.** Nothing here optimises anything. #434, #435 and #436 are the load-side
  lots and this lot must not pre-empt them.

## The three questions

**1. How large is the subsidy?** `embedding_index_load` in a process that has saved, against the
same load in a process that has not. One number, one machine, one window — the C# side alone
answers it, and it needs no Python.

**2. Does the Python side have it too?** `np.load` after `np.save` in one process, against `np.load`
cold. numpy allocates its output array per call the same way we do, so the asymmetry may well be
symmetric — but if it is *not*, the published cross-language ratio is biased **in our favour**,
and #324's "furthest behind" framing is understated rather than overstated. That is the finding that
would matter most, and nothing has looked.

**3. Given 1 and 2, does the harness split?** Two processes cost a launch per row and break the
back-to-back pairing `bench/README.md`'s measurement conditions depend on — the pairing that
section 7 argues is what makes its Python and C# rows comparable at all. Stating the condition may
be the right answer. **This question is not to be answered before 1 and 2 have numbers**, which is
the whole reason #432's refusal is the model here: measure, then decide, and be willing to decide
against the change.

## Acceptance

- A figure for question 1, taken with both states interleaved rather than as two campaigns, with
  the spread published and not only the median.
- A figure for question 2 on the same corpus and the same machine, or an explicit statement that
  numpy's allocator makes the comparison meaningless and why.
- A decision on question 3 recorded where the next person looks — an ADR if the harness changes,
  a paragraph in `bench/README.md` if it does not.
- No change to any published row's meaning without the guide and the harness page moving with it.
