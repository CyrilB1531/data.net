# RakeMetric

Which per-word score `Rake` sums into a phrase score.

<!-- docs-declaration -->

```csharp
public enum RakeMetric { DegreeToFrequencyRatio, WordDegree, WordFrequency }
```

**Members** — `DegreeToFrequencyRatio` sums `deg(w) / freq(w)`: the paper's own metric, and
rake-nltk's default. `WordDegree` sums `deg(w)` alone — how many words `w` shares a candidate with,
itself included, counted once per occurrence. `WordFrequency` sums `freq(w)` alone — how often `w`
occurs at all, ignoring how long the candidates it appears in are.

**Example** — the same document, three metrics, three different top scores.

```csharp
using Lodestar.Text.Keywords;

string doc = "linear constraints and linear constraints";
double byDegree = new Rake(new RakeOptions { Metric = RakeMetric.WordDegree }).Extract(doc)[0].Score;
double byFrequency = new Rake(new RakeOptions { Metric = RakeMetric.WordFrequency }).Extract(doc)[0].Score;

double degreeScore = byDegree;      // => 8
double frequencyScore = byFrequency;  // => 4
```

**Remarks** — `WordDegree` rewards a word for standing in a long candidate — its degree grows with
the run's length every time the run appears — where `WordFrequency` counts only how often the word
itself shows up, regardless of what it stood next to. `DegreeToFrequencyRatio`, the default, is a
compromise between the two: a word that is rare but always found in long company scores high, and
one that is common but always alone does not.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Rake`](rake.md), [`RakeOptions`](rakeoptions.md), [`KeywordMatch`](keywordmatch.md).
