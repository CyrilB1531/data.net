# Metaphone

Metaphone (Lawrence Philips, 1990): English spelling reduced to the sounds it stands for.

<!-- docs-declaration -->

```csharp
public static class Metaphone
```

**Example** — two silent letters, gone.

```csharp
using Lodestar.Text.Phonetics;

string knight = Metaphone.Encode("Knight");  // => NT
string wright = Metaphone.Encode("Wright");  // => RT
```

**Remarks** — this is the one of the three that **knows how English is spelled**. `Knight` starts
with an `n` sound and `Wright` with an `r`, and Metaphone says so; [`Soundex`](soundex.md) and
[`Nysiis`](nysiis.md) both key on the written first letter and file them under `K` and `W`.

The output alphabet is `B X S K J T F H L M N P R 0 W Y`, where `X` is "sh" and `0` is "th" —
`Thomas` encodes to `0MS` and `Christina` to `XRSTN`. Vowels appear only at the start of a word.
The code is variable length, and on the 123 real words it is pinned to it runs from 1 to 6
characters.

Reference behaviour is `jellyfish.metaphone` **on real words**.
[Decision 0007](../../../decisions/0007-metaphone-scope.md) records why the corpus is real words
rather than the shared random one: on degenerate letter sequences, jellyfish exhibits quirks of
its C implementation that are not worth reproducing, so this implementation does not claim parity
there.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Soundex`](soundex.md), [`Nysiis`](nysiis.md),
[the phonetics index](../phonetics.md).

## Members

| Member | What it does |
| --- | --- |
| [`Metaphone.Encode`](metaphone-encode.md) | The Metaphone code of one word. |
