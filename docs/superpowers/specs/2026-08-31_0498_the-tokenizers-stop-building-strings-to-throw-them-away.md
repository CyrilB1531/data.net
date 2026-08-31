# 0498 — The tokenizers stop building strings to throw them away

**Issue:** [#498](https://github.com/CyrilB1531/lodestar/issues/498) ·
**Status:** accepted · **Date:** 2026-08-31

## Problem

[#438](https://github.com/CyrilB1531/lodestar/issues/438)'s Embeddings box measured
`Microsoft.ML.Tokenizers` 2.0.0 encoding the same 5 000 documents to the same ids while
allocating **3.55 MB and 3.09 MB against our 118.84 MB and 519.51 MB**.
[Decision 0068](../../decisions/0068-the-tokenizer-gap-is-the-loader-not-the-encode-kernel.md)
ruled that the gap worth keeping is the `tokenizer.json` loader, not the encode kernel, and that
this is a defect to fix rather than an argument to delegate. This is that fix.

## Where the bytes were

Two loops, the same mistake, and neither is subtle once named: **a string built only so that a
dictionary could hash it.**

```csharp
string sub = s.Substring(i, l);                  // SentencePiece, per position × per length
if (_pieces.TryGetValue(sub, out SentencePiece p))

string sub = word[start..end];                   // WordPiece, per candidate
if (start > 0) { sub = _continuationPrefix + sub; }   // and a second one
if (_vocab.TryGetValue(sub, out int id))
```

The unigram lattice probes every substring up to the longest piece at every position — sixteen
lengths per character on this vocabulary — and the WordPiece greedy match shortens a candidate one
character at a time. Every miss cost a string, and misses are almost all of it.

`Dictionary<TKey, TValue>` cannot be asked about a slice. .NET 9 answers that with
`GetAlternateLookup`; netstandard2.0 has nothing, and this package ships one public behaviour on
both targets.

## What changed

**1. `CharSpanMap<TValue>`** — a read-only, span-probed table. Open addressing over a power-of-two
table never filled past half, built once and never mutated, so no tombstones and no resize path,
and every probe loop is bounded by the table's own length. FNV-1a over UTF-16 code units, which is
never persisted and is no security boundary: the only requirement is that a key and the span of the
same characters agree.

It carries a second overload, `TryGetValue(prefix, key, out value)`, because WordPiece's candidate
is `##` plus a slice and concatenating to ask was the second allocation.

It lives in `Lodestar.Embeddings.Tokenization`, not `src/Shared/`: Shared is compiled into every
library, and a helper only Embeddings uses would raise CA1812 in the other three. If a second
package ever needs it, that is when it moves.

**2. The lattice rents its arrays.** Three arrays the length of the text on every call were the
rest of the bytes once the substrings were gone. `ArrayPool<T>.Shared` is thread-safe, which this
type promises to be. A rental may be longer than asked, so every loop is bounded by `n` rather than
by `Length`, and only the prefix in use is initialized.

**3. WordPiece appends and rolls back** instead of staging each word in two lists. The model is
all-or-nothing per word — an unmatchable tail makes the whole word unknown — so the rollback *is*
the staging, and it costs a `Count` each rather than two allocations per word.

The greedy scan moved into `LongestPieceAt`, which is also what keeps `TokenizeWord` under the
cognitive-complexity limit the analyzer enforces.

## What it bought

Container run, so the times wait on a named machine
([ADR 0051](../../decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)); the
allocation is a property of the code path.

| model | allocated before | after | time before | after |
| --- | ---: | ---: | ---: | ---: |
| WordPiece | 118.84 MB | **68.25 MB** | 112.36 ms | 70.74 ms |
| SentencePiece | 519.51 MB | **30.33 MB** | 682.64 ms | 378.52 ms |

Seventeen times less allocation on the unigram path, and the ratio against the incumbent moves from
0.08 to 0.15; WordPiece from 0.48 to 0.77.

## What is left, and why it stops here

The incumbent is still ahead, and the remaining distance is structural rather than wasteful:

- **The token strings themselves.** `TokenizationResult` always materializes the pieces, and at
  5 000 documents that is most of what is left. `Microsoft.ML.Tokenizers` returns ids without
  building them. Closing that means an ids-only path through the encoders — a public-surface
  decision, not an optimization, and so not this lot's to take.
- **`Regex.Matches` and `Match.Value`** in WordPiece's pre-tokenizer, one object and one string per
  word. `Regex.EnumerateMatches` avoids both and does not exist on netstandard2.0, so it would be a
  conditional path in a package whose rule is one behaviour on both targets — its own decision.

Neither is a string built to be thrown away, which is what #498 was about.

## Testing

- `tests/Lodestar.Embeddings.Tests/CharSpanMapTests.cs` — eleven tests: found and absent keys, a
  slice of a longer string, the prefix overload and its empty-prefix case, the empty key, an empty
  map, a repeated key keeping the last value as an indexer assignment would, five thousand keys
  each found and each absent neighbour rejected, and the two refusals.
- The whole suite passes on both target frameworks, unchanged: **689 and 690**. That is the real
  gate here — every oracle in `tests/oracles/` replays exactly, which is what makes an encode-path
  rewrite safe at all. Not one expectation was touched.
