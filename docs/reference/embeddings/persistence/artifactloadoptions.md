# ArtifactLoadOptions

The five bounds a vocabulary load is held to.

<!-- docs-declaration -->

```csharp
public sealed record ArtifactLoadOptions
```

**Example** — tightening the ceiling for a file from somewhere you do not control.

```csharp
using Lodestar.Embeddings.Persistence;

var strict = new ArtifactLoadOptions { MaxVocabularySize = 60_000, MaxTotalBytes = 8L * 1024 * 1024 };
int cap = strict.MaxVocabularySize;  // => 60000
long defaultDepth = new ArtifactLoadOptions().MaxJsonDepth;  // => 32
```

**Remarks** — a downloaded vocabulary is a file, and a file can declare anything. Deserializing
one that claims a hundred million entries would allocate them before anything noticed, which is a
denial of service written as a model. Every loader in this namespace takes these bounds, and the
defaults are generous enough that a real model never meets one.

| Property | What it bounds | Default |
| --- | --- | --- |
| `MaxVocabularySize` | Vocabulary entries accepted. | 1 000 000 |
| `MaxTokenLength` | Characters in a single token. | 1024 |
| `MaxJsonDepth` | JSON nesting depth. | 32 |
| `MaxTotalBytes` | Bytes read from the source. | 256 MiB |
| `MaxArrayLength` | Elements in one array. | 1 000 000 |

**Exceeding a bound is a refusal, not a truncation.** A model that silently loaded smaller than it
was saved would tokenize differently and say nothing, which is worse than failing. The
`InvalidDataException` names both the limit and the value that broke it, so the fix is either a
different file or a deliberately raised ceiling.

Being a `record` with `init` accessors, an instance is built once and shared: `with` gives you a
variant without disturbing the original, and passing `null` to a loader means these defaults.

**This is not `Lodestar.Text.Persistence.ArtifactLoadOptions`.** The two share a name and nothing
else — that one bounds a saved vectorizer, this one a downloaded vocabulary, and their defaults
differ because what they bound differs.
[Decision 0011](../../../decisions/0011-persistence-format.md) records why they are declared per
package rather than shared.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`VocabTxtLoader`](vocabtxtloader.md),
[`TokenizerJsonLoader`](tokenizerjsonloader.md), [the persistence index](../persistence.md).
