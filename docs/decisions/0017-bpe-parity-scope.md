# 0017 — What `BpeTokenizer` promises, and where it knowingly differs

**Status:** accepted · **Date:** 2026-08-09

## Context

`BpeTokenizer` reproduces `tokenizers.models.BPE`, in both the classic
(character-level) lineage and the byte-level one GPT-2 introduced. Four model
families are named in the guides: GPT-2, Llama-3, Qwen2, and — by absence —
Llama-2 and Mistral v0.1. As with `SentencePieceTokenizer` ([0013](0013-sentencepiece-parity-scope.md)),
naming a family is a claim, and the claim has to say exactly how far it reaches.

Two more things surfaced while building the oracle and are recorded here rather
than left for a reader to rediscover: the split disagrees with HuggingFace above
the Basic Multilingual Plane, and one of the three shipped split patterns could
not be read from its primary source.

## Decision

### 1. End-to-end parity: GPT-2 and the classic lineage

`BpeTokenizer` is proven token-for-token against `tokenizers` over GPT-2's real
50 257-entry vocabulary and merge table — vendored, not hand-built — covering the
byte-level pipeline (`ByteLevel` pre-tokenizer, the 256-entry byte alphabet,
`Encode` and `Decode`, added tokens, `ignore_merges`) end to end: vocabulary,
merge loop and split pattern together. The classic, non-byte-level lineage is
proven the same way over a small BPE model trained for the purpose, exercising
the merge loop over Unicode code points rather than bytes. Both directions —
`Encode` and the round trip through `Decode` — are covered, including CJK text
and emoji.

### 2. Llama-3 and Qwen2: split-level parity only

`BpePatterns.Llama3` and `BpePatterns.Qwen2` are proven against HuggingFace: the
oracle replays the exact `Split` regex each model's `tokenizer.json` declares
over text neither GPT-2's pattern nor the other model's pattern would split the
same way, and a test pins the shipped constants to the same strings the corpus
was generated from, so the two cannot drift apart.

What is **not** proven end-to-end is a Llama-3 or Qwen2 *encoding* — there is no
vendored 128 000- or 150 000-entry Llama-3/Qwen2 vocabulary and merge table in
this repository, and the caller supplies both through [`TokenizerJsonLoader.LoadBpe`](../reference/embeddings/persistence/tokenizerjsonloader-loadbpe.md).
That is a deliberate scope limit, not an oversight: the merge loop itself is
already proven by the GPT-2 oracle, byte-identically to what a Llama-3 or Qwen2
vocabulary would exercise. Vendoring a vocabulary two orders of magnitude larger
than GPT-2's would re-prove a loop that does not change with vocabulary size, to
gain confidence the split-level oracle already provides where it actually
differs — the pattern. `BpeTokenizer(vocabulary)` with a caller-supplied Llama-3
or Qwen2 vocabulary is therefore parity **at the split level**, not at the model
level.

### 3. `byte_fallback` is refused; Llama-2 and Mistral v0.1 have no path here

[`TokenizerJsonLoader.LoadBpe`](../reference/embeddings/persistence/tokenizerjsonloader-loadbpe.md) refuses a model declaring `byte_fallback`, naming
it in the exception. `BpeFilesLoader` has nothing to check here: `vocab.json` +
`merges.txt` carries no pipeline flags at all, `byte_fallback` included — the
only route by which a `byte_fallback` model reaches `BpeVocabulary` is
`tokenizer.json`, which `LoadBpe` reads and refuses. Llama-2 and Mistral v0.1
are trained as **SentencePiece BPE with a `Metaspace` pre-tokenizer**, not
HuggingFace byte-level BPE — a third pipeline, distinct from both the classic
and byte-level lineages `BpeTokenizer` implements and from the `Unigram` +
`Metaspace` pipeline `SentencePieceTokenizer` implements. Neither class
reproduces it. This is named explicitly, in the guide and here, so a reader
looking for "which class do I use for Llama-2" stops looking rather than
picking the nearest-sounding one and getting silently wrong embeddings.

### 4. The split diverges above the Basic Multilingual Plane

.NET's `\p{L}` and `\p{N}` character classes test the `UnicodeCategory` of one
UTF-16 code unit at a time. For an astral character — anything above U+FFFF,
encoded as a surrogate pair — both halves individually report category
`Surrogate`, which is neither `Letter` nor `Number`. Rust's `regex` crate, which
`tokenizers` runs on, matches by code point and sees the actual category of the
astral character. Verified directly: `Regex.Matches("A\U0001D400B", @" ?\p{L}+")`
on .NET 10 returns `["A", "B"]`, dropping U+1D400 (MATHEMATICAL BOLD CAPITAL A,
category `Lu`) entirely, where HuggingFace's split keeps the whole three-character
run as one letter piece. The same holds for U+1D7CE (MATHEMATICAL BOLD DIGIT
ZERO, category `Nd`) against `\p{N}`.

None of the fixtures this library ships can show the gap: their only astral
characters are emoji, category `So`, which neither engine's split treats as a
letter or a digit, so the two sides agree on emoji by coincidence rather than by
the split logic matching. Text containing mathematical alphanumeric symbols (the
U+1D400–U+1D7FF block) or other astral letters/digits will split differently
here than in HuggingFace for `Gpt2`, `Llama3` and `Qwen2` — all three patterns
use `\p{L}` and/or `\p{N}`. This is recorded as a known boundary
rather than a defect a user discovers on their own input.

### 5. Where the Llama-3 split pattern came from

`meta-llama/Meta-Llama-3-8B` is a gated HuggingFace repository; fetching its
`tokenizer.json` without credentials this project does not have returns HTTP
401. Writing `BpePatterns.Llama3` from memory was refused — an unverifiable
literal in a public API is exactly what this project's provenance rule
([0003](0003-provenance-and-licensing.md)) exists to prevent. The pattern was
instead read from two independent, ungated mirrors:

- `NousResearch/Meta-Llama-3-8B`
- `unsloth/llama-3-8b`

Their `pre_tokenizer` blocks are byte-identical to each other, including
`ignore_merges: true` and the 128 000-entry vocabulary size. Two independent
sources agreeing is what stands in for reading the gated original, and both URLs
are recorded in `tools/generate_oracles.py`, beside the literal they justify —
so a reader auditing where a public regex constant came from does not have to
reconstruct this chain from a git blame.

## Consequences

- `docs/equivalence.md` states the corpus each `BpeTokenizer` row is proven
  over, rather than an unqualified "exact parity" — end-to-end for GPT-2 and the
  classic lineage, split-level only for Llama-3 and Qwen2.
- `docs/guides/embeddings.md` names Llama-2 and Mistral v0.1 as a model family
  with **no** class in this library, pointing here.
- Mathematical alphanumeric symbols and other letters/digits above the BMP are a
  known split divergence from HuggingFace, not a bug tracked for a fix — fixing
  it means porting Rust's per-code-point category matching into a hand-rolled
  scanner, which is out of scope for this decision.

### The merge loop: measured, and rewritten once the measurement asked for it

The BPE plan deferred a priority-queue rewrite of the merge loop until a
benchmark showed it was needed rather than assumed. All figures below are one
machine's: Intel Core i7-4770S (Haswell), Ubuntu 24.04.4, .NET SDK 10.0.110,
BenchmarkDotNet 0.14.0.

The first measurement, over the same 5000-document corpus and vocabulary size as
`SentencePieceTokenizer`'s own benchmark, found `Bpe` at **1.08×** `Unigram`'s
mean time and 0.216× its allocation — comfortably inside the 2× acceptance bar,
so ordinary text gave no reason to rewrite anything. The same run added a probe
on a single 2048-character token with no split point in it — the shape a linear
rescan-and-shift merge loop loses on — and it cost roughly 764× an average
corpus document, disproportionate enough to flag for the repository owner rather
than decide on unilaterally.

A follow-up benchmark measured that shape at four lengths instead of one, so
scaling could be read from a table instead of inferred from a single point:

| Length | Mean | Ratio vs previous |
| --- | --- | --- |
| 512 | 7.166 ms | — |
| 1024 | 27.203 ms | 3.80× |
| 2048 | 106.406 ms | 3.91× |
| 4096 | 443.203 ms | 4.17× |

Cost roughly quadruples per doubling of length while allocation merely doubles —
the rescan in `Merge`, not allocation, was what scaled quadratically. That
confirmed the loop was genuinely quadratic rather than merely slow on one input,
and the priority-queue arm **was needed**: the owner released it.

The shipped `Merge` threads symbols on a doubly-linked list (a merge unlinks one
node; nothing moves) and keeps candidate merges in a hand-rolled binary heap of
packed `(rank, leftmost position)` entries, validated when they come off the
queue and dropped in silence when stale rather than hunted down and removed at
merge time. The same four lengths after the rewrite:

| Length | Before | After | Speedup |
| --- | --- | --- | --- |
| 512 | 7.166 ms | 164.8 µs | 43× |
| 1024 | 27.203 ms | 332.3 µs | 82× |
| 2048 | 106.406 ms | 690.7 µs | 154× |
| 4096 | 443.203 ms | 1383.4 µs | 320× |

The doublings now cost 2.02×, 2.08× and 2.00× — linear per symbol, the shape
allocation already had. Allocation itself is unchanged at every length: the two
scratch buffers are rented from `ArrayPool` and returned in a `finally`, the same
discipline `EncodePiece` already used for the symbol buffer itself. On ordinary
corpus text the ratio against `Unigram` moved from 1.08× to 1.10× with 0.22× its
allocation — both inside the run-to-run noise of the `Unigram` baseline, so nothing
observable changed for the text this library is actually measured against; the
rewrite paid for the pathological case without costing the common one.

The rewrite produces byte-identical tokens to the loop it replaced: the full
oracle corpus passes unchanged on both `net10.0` and `netstandard2.0`, with no
assertion touched. Where two merges tie on rank, the leftmost occurrence wins.
That is HuggingFace's own rule, not an inference from DataNet's data structure:
`tokenizers` keeps its candidate merges in a heap ordered by `(rank, position)`
ascending, so the leftmost of two equally-ranked pairs is the one it pops first.
The queue's packed `(rank, leftmost position)` ordering reproduces that
comparison rather than defining it, which is the only reason the packing is
allowed to be convenient. Three tests pin the tie-break directly, since the
corpora happen not to exercise it.
