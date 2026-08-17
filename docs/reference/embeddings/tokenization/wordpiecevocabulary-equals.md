# WordPieceVocabulary.Equals

Value equality over the entries and the settings.

<!-- docs-declaration -->

```csharp
public bool Equals(WordPieceVocabulary other)
```

**Parameters** — `other` is the vocabulary to compare against.

**Returns** — `bool`, true when both hold the same entries with the same ids and the same settings.

**Example** — the same file loaded twice.

```csharp
using System.Text;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;

var bounds = new ArtifactLoadOptions();
byte[] file = Encoding.UTF8.GetBytes("[UNK]\ntoken\n##ize\ntext");

WordPieceVocabulary first = VocabTxtLoader.Load(
    new MemoryStream(file), bounds, unkToken: "[UNK]", continuationPrefix: "##", lowercase: true);
WordPieceVocabulary second = VocabTxtLoader.Load(
    new MemoryStream(file), bounds, unkToken: "[UNK]", continuationPrefix: "##", lowercase: true);

bool same = first.Equals(second);  // => True
```

**Remarks** — the dictionary is compared **by content**, which a `record`'s synthesised equality
would not do: it would compare the two dictionaries by reference and report two loads of the same
file as different vocabularies.

Comparing vocabularies is how to check that a saved tokenizer still matches the model beside it,
which is worth doing once at start-up rather than inferring from bad output.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`WordPieceVocabulary`](wordpiecevocabulary.md).
