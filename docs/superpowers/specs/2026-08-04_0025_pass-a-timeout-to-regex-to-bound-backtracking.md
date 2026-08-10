# Design — #25: bound Regex backtracking with a match timeout

**Date:** 2026-08-04 · **Issue:** #25 · **Branch:** `fix/25-regex-timeouts` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

`csharpsquid:S6444` (Minor) — `WordPieceTokenizer.cs:24` and
`TextAnalyzer.cs:57`. Both construct a `Regex` with no `matchTimeout`, so
backtracking is unbounded and a pathological input hangs the thread rather than
failing.

**Sonar rates this Minor, which understates it here.** Both regexes run over
caller-supplied text, and `TextAnalyzer` additionally accepts a caller-supplied
**pattern**. An arbitrary pattern applied to arbitrary text is the textbook ReDoS
pair — and it is reachable straight from the public API of a published
text-processing package.

For a library whose job is processing untrusted documents, "a crafted input stalls
the process" is a defect, not a hardening opportunity.

## Decisions

### D1 — One policy, in `src/Shared`, not a literal per call site

`RegexDefaults` joins `Guard` and `StringCompat` in `src/Shared`, compiled into
each library. Two call sites today, more later; a timeout duplicated at each of
them drifts, and the drift is invisible.

### D2 — One second

Generous enough that no realistic document approaches it, small enough that a
catastrophic pattern fails fast. The number is a judgement, so it is written down
once with its reasoning rather than repeated as a magic constant.

### D3 — The exception surfaces; it is not swallowed as a no-match

`RegexMatchTimeoutException` propagates to the caller.

Catching it and returning "no tokens" would make a timed-out tokenization
**indistinguishable from a legitimately empty document** — the worse failure of
the two, because it is silent and produces a plausible result. A caller who gets
an exception knows something happened.

This is a contract change and belongs on the public API documentation and in the
changelog, under *Changed* rather than *Fixed*.

### D4 — Proven by a test that could not pass any other way

```csharp
[Fact]
public void Pathological_pattern_times_out_instead_of_hanging()
{
    string input = new string('a', 40) + "!";
    Assert.Throws<RegexMatchTimeoutException>(() => Analyzer(@"(a+)+$").Analyze(input));
}
```

**Reaching the assertion at all is the proof**: unbounded backtracking on that
input does not finish in any reasonable time, so a test that completes has
demonstrated the timeout fired. A second test pins that ordinary documents
tokenize unchanged.

### D5 — `[GeneratedRegex]` is deliberately not adopted

`SYSLIB1045` suggests it for the same call site in `WordPieceTokenizer`. The
attribute is **net-only**, so it cannot be applied unconditionally now that the
libraries also target `netstandard2.0`. Considering the two together is what the
issue asked for; the answer is no, and the reason is recorded so the suggestion is
not re-raised.

## Out of scope

- Any change to tokenization results. The corpora must not move.
- A configurable timeout on the public API. One policy until a caller asks.

## What "done" means

Both regexes constructed with an explicit `matchTimeout` from `RegexDefaults`; the
exception documented as surfacing; the ReDoS test passing; corpora unchanged; both
frameworks green.
