# 0042 — Phonetic encoders refuse a `null` word

**Status:** accepted · **Date:** 2026-08-20

## Context

[#342](https://github.com/CyrilB1531/lodestar/issues/342) is what this records.
[`Soundex.Encode`](../reference/text/phonetics/soundex-encode.md),
[`Nysiis.Encode`](../reference/text/phonetics/nysiis-encode.md) and
[`Metaphone.Encode`](../reference/text/phonetics/metaphone-encode.md) each have a `string` overload
that forwarded straight to `value.AsSpan()`. `string.AsSpan()` on a `null` string returns an empty
span rather than throwing, so a `null` word silently encoded to `""` — the same answer an empty
string gets. [`PorterStemmer.Stem`](../reference/text/stemming/porterstemmer-stem.md) and every
Snowball stemmer in `Lodestar.Text.Stemming`, by contrast, call `Guard.NotNull` first and throw
`ArgumentNullException`.

Both families take one string and live in the same package. A caller who pipelines a stemmer into a
phonetic encoder — or the reverse — gets an exception from one half and a silent empty key from the
other, for the same missing input. The silent half is the worse of the two: every `null` in a
dataset collides into the same code, and nothing says so, where the stemmer would have stopped the
pipeline at the first `null` instead of encoding one further and burying it in an index.

## Decision

**The phonetic encoders refuse a `null` word, matching the stemmers.** Each `string` overload above
now calls `Guard.NotNull` and throws `ArgumentNullException`, the same exception type and the same
message shape the stemmers already use. The `ReadOnlySpan<char>` overload each encoder also exposes
is unaffected — a span cannot be `null` the same way a reference can, so there is nothing there to
refuse.

No opt-in flag. The two families disagreeing was the defect; a flag would keep the disagreement and
just move the question of which behavior applies from "which type is this" to "which argument did
the caller pass," which is not simpler for a caller pipelining both. `Lodestar.Text` is pre-1.0
(`0.3.2` at the time of this decision), so this is a minor-version change under SemVer rather than
a major one — recorded here for whoever next bumps `src/Lodestar.Text/Version.props` and cuts a
release (CONTRIBUTING.md's *Releasing*), not performed by this change itself.

An empty string is unaffected by any of this: it is not `null`, and every encoder already accepts it
and returns `""`, which is a real answer (no letters found) rather than a stand-in for "unknown."

## Consequences

- All three `string` overloads above gain an `ArgumentNullException` tag and Exceptions-block
  entry; their reference pages and [the phonetics index](../reference/text/phonetics.md) state the
  one rule instead of the split.
- The next `Lodestar.Text` release names this as a breaking change to a public method's exception
  contract, in a package still pre-1.0.
- The next phonetic encoder added to this package inherits the rule from the stemmers' own
  precedent, rather than repeating this question.

## Alternatives rejected

- **Accept `null` everywhere, relax the stemmers instead.** Loses the diagnostic the stemmers
  already give for exactly this input, and does not fix the inconsistency — it moves which half of
  the package is silent. The issue's own framing already argues the silent answer is the worse one to
  standardize on.
- **Gate the refusal behind an explicit option.** Keeps both behaviors alive at once, which is what
  a caller pipelining a stemmer into an encoder cannot use — the failure they are trying to catch
  would only be caught on the half of the call graph whose flag happened to be set. Revisit only if a
  caller surfaces a real use for the empty-string answer on a `null` input.
