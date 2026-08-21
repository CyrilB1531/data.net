# DutchSnowballStemmer.Stem

The Snowball stem of one Dutch word.

<!-- docs-declaration -->

```csharp
public static string Stem(string word)
```

**Parameters** — `word` is a single Dutch word. It is lowercased and NFC-normalised before the
rules run, so an `ë` typed as a combining sequence and one typed as a single character behave
alike.

**Returns** — `string`, the stem, always lowercase and free of accents: the fold happens first and
is never undone.

**Exceptions** — `ArgumentNullException` when `word` is `null`. A word of one or two characters is
returned folded and otherwise untouched, having no room for a region.

**Example** — a suffix rewritten before it is removed, and one that survives.

```csharp
using Lodestar.Text.Stemming;

string rewritten = DutchSnowballStemmer.Stem("waarheden");  // => waarheid
string removed = DutchSnowballStemmer.Stem("overheid");  // => over
string kept = DutchSnowballStemmer.Stem("vrijheid");  // => vrijheid
```

**Remarks** — those three lines are one rule seen from three sides. `heden` becomes `heid` rather
than being deleted, and `heid` is then deleted only when it lies inside R2. In `overheid` it does,
so the stem is `over`; in `vrijheid` it does not, and the suffix stays. `waarheden` shows the
rewrite without the deletion.

A stem is a key, not a word: `houdbaar` comes back as `houdbar`, which is not Dutch. What matters
is that every form sharing a root produces the same key, and that no two roots collide.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`DutchSnowballStemmer`](dutchsnowballstemmer.md),
[the stemming index](../stemming.md).
