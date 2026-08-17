# WordPieceVocabulary

The vocabulary and the settings that decide how it is read.

<!-- docs-declaration -->

```csharp
public sealed record WordPieceVocabulary
```

**Properties** — `Vocab` maps token to id. `UnkToken` is the token a word that cannot be covered
becomes. `ContinuationPrefix` marks a piece that continues a word, `##` in BERT. `Lowercase` says
whether text is folded before matching. `AddedTokens` are the literal matches applied first.
`Count` is how many entries the vocabulary holds.

**Example** — loading one from the `vocab.txt` a model ships.

```csharp
using System.Text;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

var bounds = new ArtifactLoadOptions();
byte[] file = Encoding.UTF8.GetBytes("[UNK]\ntoken\n##ize\ntext");

WordPieceVocabulary vocabulary = VocabTxtLoader.Load(
    new MemoryStream(file), bounds, unkToken: "[UNK]", continuationPrefix: "##", lowercase: true);

int count = vocabulary.Count;  // => 4
```

**Remarks** — a `vocab.txt` is one token per line and the id is the **line number**, which is why
loading it needs no ids and why editing such a file by inserting a line renumbers everything after
it. That is a real way to break a model quietly.

The settings travel with the vocabulary rather than with the tokenizer because they are properties
of the file: a vocabulary trained lowercase cannot be read case-sensitively, and pairing it with
the wrong `ContinuationPrefix` produces tokens that exist nowhere in it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`WordPieceTokenizer`](wordpiecetokenizer.md), [`AddedToken`](addedtoken.md).

## Members

| Member | What it does |
| --- | --- |
| [`WordPieceVocabulary.Equals`](wordpiecevocabulary-equals.md) | Value equality over the entries and settings. |
| [`WordPieceVocabulary.GetHashCode`](wordpiecevocabulary-gethashcode.md) | A hash consistent with it. |
