# 0118 — The BPE values the loader refuses for nothing, and the one that crashes `Decode`

**Issue:** [#118](https://github.com/CyrilB1531/data.net/issues/118) · **Umbrella:** [#105](https://github.com/CyrilB1531/data.net/issues/105) · **Date:** 2026-08-10

## Context

`TokenizerJsonLoader.LoadBpe` refuses five things a real `tokenizer.json` can declare. #105 was split into
six lots on 2026-08-10; this is the first, and the only one that designs no new behaviour. Every item here
is a value that is representable in a real file and that this library either rejects for no reason or
accepts and then crashes on.

All four were verified against `main` at 38813b0 rather than carried over from #105's text, which a previous
session wrote and which `main` has moved under since.

| Claim | Where it was checked |
| --- | --- |
| `end_of_word_suffix: ""` crashes `Decode` | `BpeTokenizer.cs:718-720` guards only `is null`; `TokenizerJsonLoader.cs:551` assigns the empty string as-is |
| `continuing_subword_prefix: ""` is refused | `TokenizerJsonLoader.cs:585` matches `is { } prefix`, which an empty string satisfies |
| `dropout: 0.0` is refused | `TokenizerJsonLoader.cs:597` refuses any non-null value without reading it |
| `add_prefix_space` defaults disagree | `?? true` at `TokenizerJsonLoader.cs:756`, `?? false` at `:797` |

## Decisions

### D1 — an empty end-of-word suffix is no suffix, and the rule lives on the type

`BpeVocabulary.EndOfWordSuffix`'s `init` accessor maps `""` to `null`. `Equals` and `GetHashCode` are
untouched: they already compare the stored value, which becomes canonical.

The rule goes on the type rather than in the loader because `BpeVocabulary` is public and constructible.
`new BpeVocabulary { EndOfWordSuffix = "" }` reaches `StringBuilder.Replace("", " ")` without the loader
ever running, so a loader-side fix would leave half the defect in place — and would make a loaded
vocabulary and a hand-built one with the same meaning compare unequal. That is the failure ADR 0022 §4
records for `AddedToken.Normalized`, where the default was first written in the loader and had to be moved
onto the type. This is the same shape, and it gets the same answer without paying for the discovery twice.

### D2 — the two refusals become conditional, and only the condition changes

`continuing_subword_prefix: ""` and `dropout: 0.0` stop being refused. The readers are untouched:
`OptionalString` still returns `""` for an empty JSON string, and the refusal messages for a non-empty
prefix and a non-zero dropout stay word for word, because they are still right.

A non-empty prefix stays refused — it is [#120](https://github.com/CyrilB1531/data.net/issues/120)'s
subject. Whether dropout above zero is ever reproduced stays open — it is
[#123](https://github.com/CyrilB1531/data.net/issues/123)'s decision. This lot only removes the two values
that provably change nothing.

### D3 — a `Sequence`'s `ByteLevel` step defaults `add_prefix_space` to `true`

`?? false` at `TokenizerJsonLoader.cs:797` becomes `?? true`, matching both HuggingFace's `ByteLevel`
default and the sibling reader at `:756`. Empty today because Llama-3 and Qwen2 both declare the flag
explicitly; live for the first file that does not.

### D4 — the public `ContinuingSubwordPrefix` property is not touched here

It is public, participates in `Equals` and `GetHashCode`, and `TokenizerJsonLoader.cs:552` deliberately
does not carry it, with a comment saying so. Giving it D1's treatment would be symmetric, but
[#120](https://github.com/CyrilB1531/data.net/issues/120) may delete the property outright, and churning it
twice is worse than leaving it consistent with itself for one release cycle. Recorded here so a reviewer
asking "why one and not the other" finds the answer instead of the omission.

## The measurement this plan must take before implementing

Whether `tokenizers` 0.23.1 accepts `end_of_word_suffix: ""`, and what it does with it, is **not known**.
Three outcomes, and they are not the same change:

1. **It accepts and ignores it.** D1 is parity. A corpus case proves it and no ADR is needed.
2. **It accepts and does something with it.** D1 becomes a deliberate divergence, and a divergence is
   written in `docs/decisions/`, not in a commit message.
3. **It refuses the file.** No corpus can exist, and the unit tests on the type carry the proof alone —
   the situation #104 hit when `tokenizers` turned out to refuse a `tokenizer.json` omitting `normalized`.

Probe first, implement second. The same applies, less sharply, to `continuing_subword_prefix: ""` and
`dropout: 0.0`: each is asserted to be a no-op, and an oracle is what turns that assertion into a fact.

## Evidence

Two of the four items — the empty prefix and the zero dropout — are the *absence* of a refusal, and a test
that loads a file without throwing proves only that nothing was thrown. It does not prove the value is a
no-op.

- **The crash:** a unit test that `EndOfWordSuffix = ""` reads back as `null` and compares equal to a
  vocabulary built with `null`, plus a `Decode` on the classic lineage that raised `ArgumentException`
  before. Neither depends on an oracle.
- **The two no-op values:** an oracle case showing `tokenizers` produces the same tokens with the value
  declared as with the setting absent. That is the assertion that carries the claim; a load test is a
  by-product.
- **The `add_prefix_space` default:** the proof runs through `Encode`, on text where `true` and `false`
  produce different tokens. A test asserting the parsed flag would pass even if the flag were never used.

New corpora go in `tools/generate_oracles.py`, following `generate_bpe_added_token_flags`'s shape — it
records `tokenizer.to_str()` in its metadata, so the C# side parses the exact bytes HuggingFace was handed.
Not `tools/build_tiny_models.py`, which holds frozen fixtures that carry no reference values.

Generation runs from a neutral working directory, or `nltk` refuses to import its dependencies; and the
generator's own exit code is read, never a pipeline's.

## Documentation

- `docs/equivalence.md`'s `LoadBpe` row enumerates eleven refusals in prose, each verified against the code
  during #104. Two become conditional: a non-empty prefix, a non-zero dropout.
- `CHANGELOG.md`: the plan must first establish **whether the crash exists in a published version**. 0.2.0
  shipped; if the classic lineage and `end_of_word_suffix` were in it, the entry goes under *Fixed* and
  concerns real users, which is a different statement from a defect that never shipped. That is a
  `git tag` and `Version.props` check, not a recollection.
- `docs/guides/embeddings.md` enumerates refusals too. Check it by `grep` rather than from memory — counts
  and enumerations going stale silently is the failure mode #104 hit twice.
- No ADR, unless the probe returns outcome 2 above.

## Out of scope

`fuse_unk` ([#119](https://github.com/CyrilB1531/data.net/issues/119)), a non-empty
`continuing_subword_prefix` and the public property ([#120](https://github.com/CyrilB1531/data.net/issues/120)),
the BPE normalizer ([#121](https://github.com/CyrilB1531/data.net/issues/121)), `use_regex: false` and the
per-piece prefix-space rule ([#122](https://github.com/CyrilB1531/data.net/issues/122)), and the dropout
decision ([#123](https://github.com/CyrilB1531/data.net/issues/123)). `byte_fallback` stays refused for
ADR 0017 §3's reason and belongs to no lot.

## Risks

- **The probe changes the shape of the work.** Outcome 2 turns a fix into a divergence with an ADR; outcome
  3 removes the corpus this spec assumes. The plan carries all three branches rather than one.
- **`Equals` is public behaviour.** D1 makes two vocabularies that compare unequal today compare equal
  tomorrow. Nothing in the repository depends on the old answer, but the CHANGELOG should say so.
- **None of the four appears in any shipped model or committed corpus**, so no existing corpus can catch a
  mistake in them by regressing. The fixtures built for the purpose are the only guard, which is why they
  must assert token streams rather than load success.
