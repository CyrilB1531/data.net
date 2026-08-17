# BpePatterns

The four pre-tokenizer regexes real BPE models use.

<!-- docs-declaration -->

```csharp
public static class BpePatterns
```

**Properties** — `Whitespace` splits on runs of whitespace. `Gpt2` is GPT-2's own pattern, which
keeps a leading space with the word that follows it. `Llama3` and `Qwen2` are those models'
patterns, which differ from GPT-2's in how they treat digits and contractions.

**Example** — choosing the pattern the model was trained with.

```csharp
using Lodestar.Embeddings.Tokenization;

string gpt2 = BpePatterns.Gpt2;
bool declared = gpt2.Length > 0;  // => True
```

**Remarks** — the pre-tokenizer runs **before** any merge and decides what a "word" is; the merges
then apply within each piece and never across them. So the pattern is part of the model, not a
preference: encoding with GPT-2's merges under Llama-3's pattern gives ids neither model would
produce.

`Llama3` and `Qwen2` differ from `Gpt2` mainly on digit grouping — a change made because
tokenizing numbers three digits at a time helps arithmetic — which is exactly the kind of
difference that produces plausible, wrong ids when the pattern is mismatched.

A model whose file declares no pre-tokenizer hands the whole text to the merge loop; that is
[`BpeVocabulary.NoPreTokenizer`](bpevocabulary.md), and it is not the same as choosing
`Whitespace`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BpeVocabulary`](bpevocabulary.md), [`BpeSplitStep`](bpesplitstep.md).
