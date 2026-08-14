# 0167 — A `Split` step may spell its pattern as a literal

**Issue:** [#167](https://github.com/CyrilB1531/data.net/issues/167) · **Date:** 2026-08-14

## Context

`TokenizerJsonLoader` reads a `Sequence`'s `Split` step pattern from `pattern.Regex` and nothing else, so
a `tokenizer.json` spelling it `{"String": …}` is refused by name — with a message naming a field the
author never wrote.

That spelling is not exotic: it is what the **default Python call produces**.
`pre_tokenizers.Split("|", "isolated")` passes a literal and serialises `{"String": "|"}`; only
`Split(Regex(r"\|"), …)` gives `{"Regex": "\\|"}`. The gap was found on 2026-08-14 while generating
`bpe_prefix_space.json` for #122: three of that corpus's five models were unloadable for exactly this
reason, and the corpus had to be regenerated with `Regex(...)` to be replayable at all.

## What is measured

All against `tokenizers` 0.23.1 on 2026-08-14.

### D1 — `String` is a literal, `Regex` is a pattern, and they differ observably

`Split(pattern, Isolated, invert=false)` over `"abc a.c"`:

| pattern | pieces |
| --- | --- |
| `String "a.c"` | `['abc ', 'a.c']` |
| `Regex "a.c"` | `['abc', ' ', 'a.c']` |

The literal matched only the real `a.c`; the pattern's `.` matched `b` as well. This is a second
semantics, not a spelling quirk.

### D2 — the mapping is exactly an escape, including the case that proves it

| literal | over | pieces |
| --- | --- | --- |
| `"\d"` | `"a\db 7"` | `['a', '\d', 'b 7']` |
| `"ab"` | `"xabyab"` | `['x', 'ab', 'y', 'ab']` |
| `"é"` | `"aéb"` | `['a', 'é', 'b']` |
| `"😀"` | `"a😀b"` | `['a', '😀', 'b']` |

`"\d"` matches the two characters and **not a digit** — the `7` stays in its gap. That is the case that
distinguishes "we escaped the literal" from "we passed it through", and nothing else in this list does.
Astral literals match as themselves, so the escape carries no Basic-Multilingual-Plane caveat of the kind
[ADR 0017 §4](../../decisions/0017-bpe-parity-scope.md) records for `\p{L}`.

### D3 — an empty literal splits every character, and agrees with an empty regex

`Split("", Isolated)` over `"abc"` gives `['a', 'b', 'c']`, and `Split(Regex(""), Isolated)` gives the
same. So in the reference the empty case needs no special handling.

**Whether .NET agrees is not measured here and must be**, before the design below is implemented: an
empty .NET `Regex` matches the empty string at every position, and how `BpePreTokenizer.Apply`'s
gap-and-match walk arranges four empty matches over `"abc"` is a property of that code, not of the
reference. See *Risks*.

### D4 — no published model uses the spelling

Across the 23 `tokenizer.json` files read for [#123](https://github.com/CyrilB1531/data.net/issues/123)
on 2026-08-14 — the fifteen #121 surveyed plus eight more — there are **10 `Split` patterns and every one
is `Regex`**. Llama-3 (both ungated mirrors), Qwen2, deepseek-coder, bloom-560m, falcon-7b and
stablelm-2 carry them.

**So this fixes nothing for a published file and everything for a hand-written one.** It is worth doing
because the refusal is reachable from the shortest correct Python call, not because a model needs it.

## Design

### The rule

`ReadBpeSequencePreTokenizer` reads the `Split` step's pattern as:

| document | read as |
| --- | --- |
| `{"Regex": s}` | `s`, unchanged — today's behaviour |
| `{"String": s}` | `Regex.Escape(s)` |
| both present | **refused**, naming both |
| neither present | **refused**, naming both |

`tokenizers` writes exactly one of the two, so a document carrying both is not something the reference
produces; picking a winner would be inventing behaviour rather than reproducing it. A document carrying
neither is refused today and stays refused — only its message changes, to name the spelling the author
actually used rather than the one they did not.

### Why an escape is a total reproduction, where a foreign regex would not be

`docs/equivalence.md` records that a `Replace` normalizer is refused because its pattern may be a Rust
regex whose flavour .NET does not share. **A literal has no flavour.** Every literal has an exact .NET
equivalent under `Regex.Escape`, so the reproduction is total rather than best-effort — there is no
residual class of literals this handles wrongly, which is precisely what the `\d` case in D2 exists to
demonstrate.

### One site

A `Split` step is reachable only inside a `Sequence` here — `ReadBpeSequencePreTokenizer` checks the
first step's type and no other path constructs one. So the change is six lines in one method, and the
`BpeSplitStep` it produces is unchanged: downstream, an escaped literal *is* a regex.

## Evidence

A corpus `bpe_split_literal.json`, models carried in `metadata.models`, on the shape #118 through #145
established. Each literal is paired with its escaped-regex twin as two models over the same texts, so the
equality is measured **per case** rather than argued once for all literals:

| literal | why it is carried |
| --- | --- |
| `"\d"` | the discriminator — an unescaped pattern would match digits, and no other case shows it |
| `"a.c"` | D1's pair, where the metacharacter changes what matches |
| `"\|"` | the shape #122's corpus hit, and the commonest hand-written separator |
| `"ab"` | a multi-character literal |
| `"😀"` | an astral literal, against ADR 0017 §4's plane caveat |
| `""` | D3's empty case — carried whether or not it ends up refused, because the decision has to rest on a measurement either way |

Plus the two refusals — both spellings present, and neither — recorded the way #145's corpus records the
`Split` shapes `tokenizers` itself rejects.

## Out of scope

**A `pattern` field on anything but a `Split` step**, and the `Regex`-flavour question for genuine regex
patterns, which is `Replace`'s problem and stays refused.

**`dropout`** ([#123](https://github.com/CyrilB1531/data.net/issues/123)), whose refusal rests on
unprovability rather than on effort, and which this lot's argument does not touch.

## Risks

- **The empty literal is the one unmeasured case.** D3 shows the reference treats it as splitting every
  character. What `BpePreTokenizer.Apply` does with an empty .NET `Regex` — which matches at every
  position — is a property of DataNet's own gap-and-match walk and must be measured before the corpus is
  frozen. If the two disagree, the empty literal is refused by name and the corpus records the refusal
  instead. **Deciding this by reasoning rather than by running it is the way this lot goes wrong.**
- **`Regex.Escape` is not symmetric with `Regex.Unescape`** and escapes whitespace, so the escaped form
  will not look like the literal in a debugger. That is cosmetic, but the corpus's `declares` strings
  should carry the literal rather than the escaped form so a reader can see what was written.
- **No published model exercises this**, so the corpus is the only thing standing behind it — which
  raises the bar on the pairs discriminating. Each literal and its escaped twin must agree on every text,
  and the `\d` model must differ from a hypothetical unescaped one. A pair that agrees for the wrong
  reason measures nothing, which #122's first half hit twice.
