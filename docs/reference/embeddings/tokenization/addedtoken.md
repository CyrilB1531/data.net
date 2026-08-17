# AddedToken

A token matched **literally**, before the model's vocabulary sees the text.

<!-- docs-declaration -->

```csharp
public sealed record AddedToken
```

**Properties** — `Content` and `Id` are **constructor parameters**: the exact string to match, and the id it maps to. `Special`
marks it as a control token rather than content. `SingleWord` requires the match to stand alone
rather than fall inside a word. `Lstrip` and `Rstrip` absorb whitespace to the left or right into
the match. `Normalized` says whether the normalizer runs over it first.

**Example** — a mask token, matched whole where the model would otherwise split it.

```csharp
using Lodestar.Embeddings.Tokenization;

var token = new AddedToken("[MASK]", 103)
{
    Special = true,
    Lstrip = true,
};

string content = token.Content;  // => [MASK]
```

**Remarks** — added tokens exist because a vocabulary cannot express "this exact string is one
token, whatever my merge rules say". `[MASK]` would otherwise become `[`, `MA`, `##SK`, `]` and
mean nothing. They are matched **before** the sub-word algorithm runs, so they win over it.

`Lstrip` is the one that surprises: with it on, the space before `[MASK]` is absorbed **into the
token string** and disappears from the ids. That is BERT's own behaviour and it changes the token
text you see without changing the id count.

`SingleWord` is what keeps a token like `<s>` from matching inside `a<s>b`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`WordPieceVocabulary`](wordpiecevocabulary.md),
[`BpeVocabulary`](bpevocabulary.md), the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
| [`AddedToken.Equals`](addedtoken-equals.md) | Value equality over every flag. |
| [`AddedToken.GetHashCode`](addedtoken-gethashcode.md) | A hash consistent with it. |
