# Distances — `DataNet.Text.Distances`

How different are two pieces of text? Every type on this page answers that, and they disagree on
what "different" means: some count the edits that turn one string into the other, some check how
many characters line up in roughly the same place, and one looks for the longest stretches the two
have in common. Picking the wrong one is the usual cause of a similarity score that looks nothing
like what a reader expects.

Two conventions run through the whole namespace, and knowing them saves reading every entry.

- A **distance** counts how far apart two inputs are: `0` means identical, and a bigger number is a
  worse match. A **similarity** runs the other way, `1` meaning identical. What decides whether a
  number can be compared across pairs of different lengths is its type, not its name: a `Distance`
  returning `int` counts edits and has no upper bound, while every member returning `double` on this
  page — the `Normalized…` ones and also `Jaro`, `JaroWinkler` and `RatcliffObershelp` — is already
  scaled to `[0, 1]` and is comparable.
- Every member that takes a `string` also takes a `TextElement` saying what counts as one character.
  (The generic overloads — `Distance<T>`, `SubsequenceLength<T>`, `SubstringLength<T>` — do not: they
  compare whatever elements you hand them.) The default,
  `TextElement.Utf16Unit`, is .NET's own unit and gives the same answer as Python for every
  character in the Basic Multilingual Plane. Outside it — emoji, rare ideographs — one character is
  two UTF-16 units, and the two disagree on purpose; pass `TextElement.CodePoint` for Python's
  answer. The reasoning is in [decision 0002](../../decisions/0002-unicode-comparison-unit.md).

Comparing two **bags** of words or characters, where position does not matter at all, is a
different question. It is answered by the `DataNet.Text.Similarity` namespace — `Jaccard`,
`SorensenDice`, `Overlap`, `Tversky` and `Cosine` — not by anything here; its members are listed
under [set similarity in the equivalence table](../../equivalence.md).

## Which one do I want?

```mermaid
flowchart TD
    A["What are you comparing?"] --> B["Two short strings:<br/>names, codes, typos"]
    A --> C["Two longer texts"]
    A --> D["Two bags of words<br/>or characters"]
    B --> E{"Do the two line up<br/>position by position?"}
    E -->|yes| F["Hamming"]
    E -->|no| G{"Is agreement on the first<br/>few letters strong evidence?"}
    G -->|yes| H["JaroWinkler"]
    G -->|no| I{"Are swapped neighbours<br/>a common mistake?"}
    I -->|yes| J["DamerauLevenshtein,<br/>or Osa when speed matters more"]
    I -->|no| K{"Do you want a count of edits,<br/>or a forgiving score?"}
    K -->|a count| L["Levenshtein"]
    K -->|a score| M["Jaro"]
    C --> N{"Do you want a score,<br/>or the shared text itself?"}
    N -->|the text itself| O["Lcs"]
    N -->|a score| P{"Does the shared material come in a<br/>few long passages, or scattered?"}
    P -->|long passages| Q["RatcliffObershelp"]
    P -->|scattered| R["Indel"]
    D --> S["Not here — see<br/>DataNet.Text.Similarity"]
```

| Type | What it measures |
| --- | --- |
| [`DamerauLevenshtein`](distances/dameraulevenshtein.md) | Insertions, deletions, substitutions and swaps of neighbouring characters, with no limit on re-editing a stretch. |
| [`Hamming`](distances/hamming.md) | How many positions hold a different character, plus the difference in length. |
| [`Indel`](distances/indel.md) | Insertions and deletions only, never substitutions — the basis of rapidfuzz's `fuzz.ratio`. |
| [`Jaro`](distances/jaro.md) | How many characters the two share near the same position, and how many of those arrive out of order. |
| [`JaroWinkler`](distances/jarowinkler.md) | `Jaro`, raised for pairs that already agree on their first few characters. |
| [`Lcs`](distances/lcs.md) | The length of the longest run the two have in common, contiguous or not. |
| [`Levenshtein`](distances/levenshtein.md) | Insertions, deletions and substitutions. |
| [`Osa`](distances/osa.md) | The same as `DamerauLevenshtein`, except that no stretch of text may be edited twice. |
| [`RatcliffObershelp`](distances/ratcliffobershelp.md) | How much of the two texts their matching blocks cover, taken longest first. |
