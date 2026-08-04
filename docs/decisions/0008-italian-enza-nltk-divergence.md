# 0008 — Italian `enza`/`enze`: follow nltk, not the published description

**Status:** accepted · **Date:** 2026-08-05

## Context

The published description of the Italian Snowball algorithm gives step 1 as:

> `enza enze` → replace with `ente` if in R2

Implemented that way, `esistenza` becomes `esistente`, which step 3a trims to
`esistent`.

`nltk` does something else:

```python
elif suffix in ("enza", "enze"):
    word = suffix_replace(word, suffix, "te")
```

It substitutes `te`, not `ente`. So `esistenza` becomes `esist` + `te` =
`esistte`, which step 3a trims to `esistt`. The same applies to `differenza` →
`differt`.

The divergence only shows when the suffix is inside R2. `potenza`, `pazienza`,
`partenza` and `presenza` fail that condition, fall through to step 3a, and stem
to `potenz`, `pazienz`, `partenz`, `presenz` under either reading — which is why
a small corpus can easily miss this.

## Decision

**Match `nltk`.** `ItalianSnowballStemmer` replaces `enza`/`enze` with `te`.

The project's contract is behavioural parity with the Python libraries a user is
migrating from, and for the stemmers that library is `nltk` — it is what the
oracle corpora are frozen from and what `docs/equivalence.md` names as the
reference. A stemmer that matched the prose but disagreed with the tool people
actually use would be wrong in the only sense that matters here.

This is the same call as [`0005`](0005-hamming-jellyfish-divergence.md), where
`jellyfish` diverges from the textbook definition and we follow `jellyfish`.

## Consequences

- `esistenza` and `differenza` stem to `esistt` and `differt`, which look wrong
  read as Italian. They are correct as *parity*, and that is the guarantee this
  library offers.
- If `nltk` ever aligns itself with the published description, the corpus will
  drift and the `Oracles are reproducible` job will catch it. At that point this
  decision should be revisited rather than the corpus quietly regenerated.
- Found because two of 96 corpus cases failed. The implementation had been
  written from the published description, and the reading looked right — the
  oracle is what turned a plausible misreading into a specific, locatable
  difference. Neither reasoning nor review would have caught it.
