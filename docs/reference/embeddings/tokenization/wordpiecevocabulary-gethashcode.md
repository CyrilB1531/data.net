# WordPieceVocabulary.GetHashCode

A hash consistent with that equality.

<!-- docs-declaration -->

```csharp
public int GetHashCode()
```

**Returns** — `int`, over the settings and the vocabulary's size rather than its every entry.

**Example** — equal vocabularies hash alike.

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

bool equal = first.Equals(second);  // => True
bool hashesAlike = first.GetHashCode() == second.GetHashCode();  // => True
```

**Remarks** — hashing every entry of a thirty-thousand-token vocabulary on every call is a cost
nobody wants, so the hash is over the settings and the count. Two different vocabularies of the
same size and settings collide, which is permitted: [`Equals`](wordpiecevocabulary-equals.md) is
what decides, and it does read every entry.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`WordPieceVocabulary.Equals`](wordpiecevocabulary-equals.md).
