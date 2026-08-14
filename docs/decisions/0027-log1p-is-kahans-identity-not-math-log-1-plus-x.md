# 0027 — `Log1P` is Kahan's identity, not `Math.Log(1 + x)`

**Status:** accepted · **Date:** 2026-08-14

## Context

`MeanSquaredLogError` needs `log(1 + x)` — numpy's `log1p`, which is what
`sklearn.metrics.mean_squared_log_error` calls — and neither target
framework has it: `netstandard2.0` has no `log1p` of any name, so one
hand-written implementation has to serve both `net10.0` and
`netstandard2.0` or the two would disagree in the last place.

The obvious spelling, `Math.Log(1.0 + value)`, loses the low-order bits of a
small `value` in the addition itself, before the logarithm ever sees them.
Measured against scikit-learn 1.9.0 on targets around `1e-9`
(`mean_squared_log_error([1e-9, 2e-9, 3e-9], [2e-9, 4e-9, 1e-9])`, expected
`2.9999999856666664e-18`), that spelling returns `3.000000038019698e-18` —
out by `1.4e-8` relative, 7000 times the tolerance the corpus otherwise
holds to. The frozen oracle corpus does not catch this itself: its
comparison rule scales by `max(1, |expected|)`, so at `3e-18` it reduces to
an absolute `1e-9` and every implementation passes, including one that
returns zero.

## Decision

`Log1P` uses Kahan's identity instead:

```csharp
double shifted = 1.0 + value;
return shifted == 1.0
    ? value
    : Math.Log(shifted) * value / (shifted - 1.0);
```

`u = 1 + v` rounds, but `u - 1` recovers exactly what the addition actually
added — not `v` itself, but the value the rounded `u` represents. Scaling
the correctly-rounded `log(u)` by the ratio `v / (u - 1)` corrects for
exactly the bits the addition lost. The `shifted == 1.0` branch is where `v`
vanished entirely into the addition; there, `log(1 + v) ≈ v` to full
precision, and `u - 1` would be zero, so the division is guarded rather than
computed.

## Consequences

- `MeanSquaredLogError.LogResidual.Log1P` carries a short pointer to this
  record instead of restating the derivation; the numbers above stay here
  where a later change to either can make them stale in only one place.
- One hand-written `Log1P` serves both target frameworks, which is why it is
  written out at all rather than delegated to a BCL method that only one of
  them has.
- Verified by
  `LogErrorTests.A_tiny_target_keeps_the_bits_that_one_plus_x_would_round_away`,
  which asserts the relative error against the hand-measured scikit-learn
  value directly, because the frozen corpus's own tolerance cannot.
