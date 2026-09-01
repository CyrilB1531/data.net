# 0073 — The oracle gate compares numbers at the suites' tolerance, not bytes

**Status:** accepted · **Date:** 2026-09-01

## Context

`Oracles are reproducible` regenerated every corpus on the runner and ran `git diff --quiet --
tests/oracles`. That asserts **byte-identity**, which is a strictly stronger property than
anything the test suites check: they replay the same corpora at **`1e-9` absolute** for floats and
exactly for strings, as `CLAUDE.md` states. The gate therefore failed on floating-point noise no
assertion in the repository can see.

The noise is real and its cause is known. numpy and scikit-learn reduce through whichever SIMD
kernel scipy-openblas selects for the host CPU, so the last bits of any BLAS-reduced value
describe the machine that ran the generator ([`0065`](0065-the-oracle-generators-floor-is-the-ci-interpreter.md)'s
neighbourhood: issues [#95](https://github.com/CyrilB1531/lodestar/issues/95) and
[#97](https://github.com/CyrilB1531/lodestar/issues/97) are the two failures that established it,
the second reproduced on demand with `OPENBLAS_CORETYPE=Nehalem`).

`tools/generate_oracles.py` already answers that with rounding. `stable()` writes 12 significant
digits, and `settled()` — added for the decomposition corpora — flushes anything under `1e-12` to
zero on top. Both reduce the problem. Neither closes it, and the arithmetic says why:

- The suites compare at **`1e-9` absolute**, and `decomposition_svd.json`'s singular values run
  from **8.776 to 22.606**. Staying inside an absolute tolerance at that magnitude takes about
  **12 significant digits**.
- The host-dependent spread on those same values **reaches the 12th significant digit**. Measured
  on a runner against this host: `-0.0026268786319` against `-0.00262687863191`, and
  `0.039091039312` against `0.0390910393121`.

There is no digit count that satisfies both. Rounding coarser than 12 digits breaks the tests —
measured at 10 digits, **13 tests fail**, one of them on `1.13e-8` of error against a `1e-9`
tolerance on a singular value of 22.606. Rounding at 12 digits leaves the disagreement in the last
digit kept. Rounding lowers the *probability* of drift; it cannot remove it, because the digit the
hosts disagree on is a digit the tolerance still needs.

## Decision

**The gate asserts what the tests assert.** `tools/compare_oracles.py` compares the committed
corpora against a fresh generation semantically: floats within `1e-9` absolute, and everything
else — integers, strings, booleans, nulls, the set *and order* of an object's keys, an array's
length and order, and the set of files — exactly. Non-finite values are compared exactly too: a
tolerance around an infinity asserts nothing, and two NaNs agree where `==` would not.

The workflow copies `tests/oracles` aside before the generator overwrites it, regenerates, and
runs the comparator over the two directories. The `if: failure()` artefact upload stays: a
difference that survives a `1e-9` comparison is a real change to a corpus, and the runner is still
thrown away with everything needed to see the whole of it.

`1e-9` is named once, as `TOLERANCE`, with a comment saying it is the suites' tolerance — the two
must not drift apart silently, which is the failure this decision is about ending.

Nothing about the corpora or the generator changes. `stable()` and `settled()` stay exactly as
they are: rounding is still worth doing, because it keeps most host noise out of the committed
bytes and keeps the diffs readable. It simply stops being load-bearing for the gate.

## Options refused

**Round coarser, in `stable()`.** Measured and refused: at 10 significant digits, 13 tests fail,
with `1.13e-8` of error on a 22.606 singular value against a `1e-9` tolerance. The two constraints
point in opposite directions — the tests need about 12 significant digits at that magnitude, and
absorbing the observed spread would need fewer than 12. No value satisfies both.

**Rescale the decomposition fixtures so the quantities are O(1).** This works, and it is the most
tempting option: a singular value near 1 has four more orders of margin under a `1e-9` absolute
tolerance, so 12 digits would absorb the spread comfortably. It is refused for two reasons. It
protects only the corpora someone remembers to rescale — the next fixture with a large quantity
reintroduces the failure, and nothing warns that it has. And it leaves the gate asserting the
wrong thing: byte-identity would still be the claim, held together by fixture values chosen to
make an over-strict comparison survivable. Fixing the fixture to satisfy the gate is the tail
wagging the dog; the gate is what was wrong.

**Delete the gate, or make it advisory.** Not seriously considered, and recorded so nobody
proposes it as the simple option. The property the gate defends — a corpus is reproducible from
the generator, so a reviewer can rebuild what a pull request commits — is worth more than the
false failures cost.

## Consequences

The gate now fails only on a difference the tests would fail on, so a red here means a corpus
genuinely moved. `CLAUDE.md`'s "occasionally flaky — re-run before believing it" advice goes with
that, and CONTRIBUTING.md no longer tells a contributor that a corpus must be *byte*-reproducible.

**What is lost is byte-level formatting.** Indentation, separators, the spelling of a float
(`0.5` against `5e-1`), a trailing newline: a corpus rewritten in any of those ways and in no
other now passes a gate that would previously have caught it. That is the whole of the loss, and
it is narrow — the comparator deliberately keeps ordering: a reordered array, a reordered key set,
a lost or gained key or file all still fail, and the generator is deterministic in its ordering
anyway, so nothing but a hand edit produces one.

`tools/compare_oracles.py` is a comparator, not a guard over the working tree, so it is not in the
pre-commit hook and not in `test_pre_commit_hook.py`'s `OFFLINE_EXCLUSIONS`: it takes two
directories and neither of them is "the repository". A contributor who wants the answer locally
regenerates into a copy and runs it over the two.

This does not weaken [`0065`](0065-the-oracle-generators-floor-is-the-ci-interpreter.md)'s floor
or `0037`'s hook, and it does not license widening a test's tolerance. `1e-9` is the number the
suites already hold; the gate stops holding a stricter one nobody can meet.
