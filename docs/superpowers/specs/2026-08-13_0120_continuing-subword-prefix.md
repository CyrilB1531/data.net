# 0120 — `continuing_subword_prefix`, and the property that carries its name

**Issue:** [#120](https://github.com/CyrilB1531/data.net/issues/120) · **Umbrella:** [#105](https://github.com/CyrilB1531/data.net/issues/105) · **Date:** 2026-08-13

## Context

`TokenizerJsonLoader.LoadBpe` refuses a model declaring a non-empty `continuing_subword_prefix`.
`BpeVocabulary.ContinuingSubwordPrefix` is public, participates in `Equals` and `GetHashCode`, and no
loader has ever set it — a documented choice, not an oversight, which is why the issue framed this lot as
having to settle it in one direction or the other.

This is lot 3 of #105, and the first that reaches into the merge loop rather than around it.

Its dependency is satisfied: #130 landed first, so merge resolution now reads the model's vocabulary alone
rather than the map with added tokens folded in. Building this lot's decoration on that would have meant
doing it twice.

## What the reference does

All measured against `tokenizers` 0.23.1 before this spec was written, with `##` as the prefix throughout.

### D1 — the prefix applies per pre-tokenized piece, not per text

Vocabulary `{a, b, ##a, ##b}`, `Whitespace` pre-tokenizer:

| text | tokens |
| --- | --- |
| `ab` | `['a', '##b']` |
| `ab ab` | `['a', '##b', 'a', '##b']` |
| `a b` | `['a', 'b']` |

The second row is the one that decides it: the first symbol of the *second* word is bare. "Subword" means
subword of a piece.

DataNet gets the boundary for free — `InitialSymbols` is already called once per piece — but not the
decoration, which it does not do at all today.

### D2 — a non-initial symbol is looked up **only** in its prefixed form

Vocabulary `{a, b}` with no `##b`:

| declared | `ab` |
| --- | --- |
| no unknown token | `['a']` |
| unknown token `[UNK]` | `['a', '[UNK]']` |

`b` exists bare and is **not** used. There is no fallback: for a non-initial symbol the prefixed form is
the only lookup, and a miss follows the ordinary uncovered path — substituted where an unknown token
exists, dropped where none does.

**DataNet reproduces this exactly, including the silent loss**, and says so in `docs/equivalence.md`
rather than inventing a refusal. A real model declaring a prefix carries the prefixed forms; the loss
happens only on a file that is already broken, and the reference loses the same characters.

### D3 — a merge's result is the prefix-stripped concatenation, and it is required

Merge `("a", "##b")`:

| vocabulary | outcome |
| --- | --- |
| `ab` and `a##b` both present | `['ab']` — the **stripped** result wins |
| `ab` alone | `['ab']`, builds |
| `a##b` alone | **build refused**: ``Token `ab` out of vocabulary`` |

The third row is the strongest form this answer could take: the reference does not merely prefer the
stripped result, it *requires* it in the vocabulary and refuses the file otherwise.

This collides with what #130 shipped. Its merge loop computes the result as `pair.Left + pair.Right`,
which is correct only while no prefix is ever reproduced. With a prefix live it computes `a##b`, and:

- on the second row it throws on a file the reference loads;
- on the first row it resolves to the id of `a##b` where the reference resolves `ab` — no exception, a
  different token stream, silent.

The refusal #130 added keeps its reason. What changes is the string it is asked about.

### D3b — only the right side loses a prefix, and the suffix is not involved

`(a, ##b)` is the shape #105 recorded, and it does not say what happens when both sides carry the prefix,
or when a suffix is in play. Measured:

| merge | result | vocabulary holds |
| --- | --- | --- |
| `("a", "##b")` | `ab` | `ab` |
| `("##b", "##c")` | `##bc` | `##bc` |
| `("##b", "##c")` | **build refused**, ``Token `##bc` out of vocabulary`` | `##b##c` instead |
| `("a", "##b</w>")` | `ab</w>` | `ab</w>` |

One rule covers all four: **the result is the left side plus the right side with its continuing prefix
removed.** The left keeps whatever decoration it has — the second row would be `bc` if both sides were
stripped, and the third shows the reference requires `##bc` specifically. An end-of-word suffix is simply
part of the string and rides along; it never participates.

The fourth row also shows merges name the *fully* decorated right side. A merge written `("a", "##b")`
does not fire on a symbol that is `##b</w>`, which is what a non-initial final character becomes — measured,
that model returns `['a', '##b</w>']` unmerged.

This matters beyond correctness: "both sides lose their prefix" is the plausible reading, it gives `bc`
where the reference gives `##bc`, and no case in #105 or in the issue would have caught it.

### D4 — the two decorations compose, prefix then suffix

Vocabulary `{a, b, ##b, b</w>, ##b</w>, a</w>}`, `end_of_word_suffix="</w>"`:

| text | tokens |
| --- | --- |
| `ab` | `['a', '##b</w>']` |
| `a` | `['a</w>']` |
| `b` | `['b</w>']` |

A symbol that is both non-initial and last carries both decorations, in the order prefix, character,
suffix. Neither #118 nor #130 looked at this combination.

### D5 — an empty prefix is a no-op

`continuing_subword_prefix=""` produces the same stream as declaring none: `['a', 'b']` over the vocabulary
above. This matches the rule #118 established for an empty `end_of_word_suffix`, and it means the loader
can normalise an empty prefix to "no prefix" rather than carrying two spellings of one meaning.

### D6 — merges name the prefixed form

The serialized file records `continuing_subword_prefix: "##"` and the merge as `["a", "##b"]`. Merge
entries therefore name vocabulary entries as they appear, decoration included, which is why the rank table
needs no new keying — the ids it maps are the decorated symbols' own.

## Design

| Where | What |
| --- | --- |
| `BpeTokenizer.InitialSymbols` | Decorates: bare at the start of a piece, prefixed after, suffixed at the end, both where both apply. No fallback to the undecorated form. |
| `BpeTokenizer`'s merge loop | The result is `Left` plus `Right` with its continuing prefix removed, not the plain concatenation — the left side keeps its own (D3b). #130's refusal stays, asked about the right string. |
| `TokenizerJsonLoader.LoadBpe` | Stops refusing a non-empty prefix and sets `ContinuingSubwordPrefix`; an empty one reads as absent. |
| `BpeVocabulary` | Nothing structural. The property stops being a name with nothing behind it. |
| `BpeFilesLoader` | Untouched: `merges.txt` has no field to declare a prefix in. |

The rank table keeps its shape (D6), which is the part of the issue's framing that the measurement
changed: what moves is which symbols enter the loop, not what the loop is keyed on.

## Evidence

A corpus `bpe_continuing_prefix.json`, generated against `tokenizers` 0.23.1, with the models built inside
the generator and carried in `metadata.models` — the shape #118, #119 and #130 established.

Eight distinctions across ten models — distinction 3 needs two, and 7 needs a no-prefix baseline to be
compared against — recorded as 25 encoded cases. Each exists for something no other distinguishes:

1. **the base case**, `ab` → `['a','##b']`;
2. **two words**, `ab ab`, the only case that tells per-piece from per-text (D1);
3. **a missing prefixed form**, the only case that proves there is no fallback (D2), carried twice — with
   an unknown token and without, since the two lose the character differently;
4. **a merge whose stripped result alone exists**, the only case that catches #130's concatenation (D3);
5. **a merge with both sides prefixed**, the only case that tells "the right side loses its prefix" from
   "both sides do" — the second reading gives `bc` where the reference gives `##bc` (D3b);
6. **prefix and suffix together** (D4);
7. **an empty prefix**, whose stream must equal the no-prefix model's — its own regression proof (D5);
8. **a merge whose right side carries the prefix and the suffix at once**, the only case that tells "the
   strip takes the prefix off and leaves the suffix on" from a strip that takes both, or neither — D3b's
   fourth row, `("a", "##b</w>")` → `ab</w>`, which was measured during design and had no model holding
   it until the final review put one there.

Plus the build refusal of D3's third row, recorded the way #118 records its own: the exact document handed
to the reference and the error it answered with.

## Out of scope

Applying the prefix in `ByteLevelSymbols`. Nothing here measured what a byte-level model declaring one
does in the reference, and the two halves of `BpeTokenizer` would answer differently if it were left
alone — the symbols unprefixed, a merge's right side still stripped, and the disagreement silent because
the byte-level alphabet spells `0x23` as `#`. So the pairing is **refused** by name instead, by
`TokenizerJsonLoader.LoadBpe` for a file and by `BpeTokenizer`'s constructor for a hand-built
`BpeVocabulary`; what the reference makes of such a file stays out of scope, and the refusal does not rest
on it. `WordPieceTokenizer`, which carries its
own implementation of the same setting — `ReadWordPiece` already reads `continuing_subword_prefix` and
defaults it to `##`, and whether the two implementations should share anything is a question this lot does
not open. The rest of umbrella #105: `dropout` (#123), the normalizer (#121), and the no-split mode
(#122), which touches the pre-tokenizer rather than the merge loop.

## Risks

- **The silent loss in D2 is now DataNet's behaviour too.** It is parity, and the equivalence row says so,
  but a caller handed a broken vocabulary gets a short token stream and no signal. The alternative —
  refusing at load — was considered and rejected: the reference accepts those files, and verifying that
  every character has a prefixed form would cost a pass over a 50 000-entry vocabulary to catch a file that
  is already wrong.
- **D3's collision is the kind that a corpus catches and reading does not.** Where both result forms exist
  the current code returns a valid id, just the wrong one. Evidence case 4 exists for exactly that, and it
  is the one case in this lot whose absence would leave a silent defect rather than a loud one.
