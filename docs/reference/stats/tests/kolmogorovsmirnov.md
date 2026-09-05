# KolmogorovSmirnov

The two-sample Kolmogorov-Smirnov test.

**Two-sample only.** The one-sample test compares a sample against a named distribution, which
means passing a cumulative distribution function; this package has no distributions namespace to
pass one from, and inventing one to serve a single test is a second package's worth of surface.

## Members

| Member | What it does |
| --- | --- |
| [`KolmogorovSmirnov.TwoSample`](kolmogorovsmirnov-twosample.md) | Compares two samples by the largest gap between their empirical distributions. |
