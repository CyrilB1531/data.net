# From string to vector — bag of words, TF-IDF, hashing

`Lodestar.Text.Vectorization` reproduces `sklearn.feature_extraction.text`: it turns
a corpus of documents into a **sparse matrix** of features.

```bash
dotnet add package Lodestar.Text
```

## Bag of words — `CountVectorizer`

```csharp
using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

string[] docs =
[
    "the cat eats",
    "the dog eats",
    "the cat and the dog",
];

var cv = new CountVectorizer();
CsrMatrix counts = cv.FitTransform(docs);

// Sorted vocabulary (like sklearn): ["and", "cat", "dog", "eats", "the"]
foreach (string f in cv.GetFeatureNames()) Console.Write($"{f} ");

double[,] dense = counts.ToDense(); // for inspection
```

> By default: lowercasing, token pattern `\b\w\w+\b` (**single-letter** words are
> dropped — like sklearn). Configure via `CountVectorizerOptions`.

Common settings (identical to sklearn):

```csharp
var cv = new CountVectorizer(new CountVectorizerOptions
{
    NgramRange = (1, 2),                 // unigrams + word bigrams
    MinDf = 2,                           // drop rare terms (df < 2)
    MaxDf = 0.9,                         // drop over-frequent terms (df > 90%)
    StopWords = StopWords.English,       // or any collection
    Analyzer = AnalyzerKind.Char,        // character n-grams
});
```

### Stop words

Six lists ship: `StopWords.English`, `.French`, `.German`, `.Italian`,
`.Portuguese`, `.Spanish` — and any `IReadOnlyCollection<string>` works just as
well.

English is scikit-learn's 318-word list, for `stop_words="english"` parity. The
other five come from Snowball, **not** from `nltk.corpus.stopwords`: that corpus
has no stated licence, so it cannot be redistributed here
([decision 0010](../decisions/0010-stop-word-list-provenance.md)). Per-language
word counts against nltk's are in
[`docs/equivalence.md`](../equivalence.md#conventions) — but if you need exactly
what nltk removes, load the corpus yourself and pass it in.

Each list is built the first time it is read and never again, one language at a
time: a program that only ever asks for French does not pay for the other five.
On `net10.0` a vectorizer handed one of the shipped lists reuses it as it is,
rather than copying it — a collection of your own is still copied, so that a
`HashSet<string>` you keep adding to cannot change what a vectorizer already
built removes.

Removal is an ordinal match against the analyzer's output, so a list only removes
what preprocessing leaves behind: with `StripAccents = true`, `même` becomes
`meme` and no longer matches. Single-letter entries (`c`, `d`, `l`, `à` in the
French list) never match under the default token pattern either, which drops
one-character tokens. scikit-learn behaves the same way in both cases.

## TF-IDF — `TfidfVectorizer`

The formula is scikit-learn's, **to the character** (a classic pitfall):
`idf(t) = ln((1 + n) / (1 + df(t))) + 1`, then L2 normalization of each row.

```csharp
using Lodestar.Abstractions;

var tv = new TfidfVectorizer();
CsrMatrix tfidf = tv.FitTransform(docs);
IReadOnlyList<double> idf = tv.Idf;   // the learned idf vector
```

Options (`TfidfOptions`): `UseIdf`, `SmoothIdf`, `SublinearTf`, `Norm` (L1/L2/none).

## Hashing — `HashingVectorizer`

No vocabulary (hence stateless, ideal for streaming). Uses MurmurHash3-32,
identical to sklearn.

```csharp
using Lodestar.Abstractions;

var hv = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 1 << 18 });
CsrMatrix hashed = hv.Transform(docs);   // no Fit needed
```

## Cosine similarity between two documents

```csharp
using Lodestar.Abstractions;

CsrMatrix m = new TfidfVectorizer().FitTransform(["the cat eats", "the dog eats"]);
double[,] d = m.ToDense();
// dot product of the two rows (already L2-normalized) = cosine
double cos = 0;
for (int j = 0; j < m.ColumnCount; j++) cos += d[0, j] * d[1, j];
Console.WriteLine(cos); // ~0.51
```

## Saving a fitted model

Fitting learns a vocabulary and, for TF-IDF, an idf vector. Both die with the
process unless you write them down — so training on a corpus and scoring later,
the normal split in any real pipeline, needs persistence.

```csharp
using Lodestar.Abstractions;

var tfidf = new TfidfVectorizer().Fit(trainingDocuments);
tfidf.Save("model.json");

// …later, in another process
TfidfVectorizer reloaded = TfidfVectorizer.Load("model.json");
CsrMatrix scored = reloaded.Transform(newDocuments);   // no refit
```

Entry by entry: [`TfidfVectorizer.Save`](../reference/text/vectorizers/tfidfvectorizer-save.md)
writes the vocabulary and the document frequencies together, and
[`TfidfVectorizer.Load`](../reference/text/vectorizers/tfidfvectorizer-load.md) reads both back,
so a reloaded model weights exactly as the original did.

The equivalent of `joblib.dump` / `joblib.load`, with two differences that
matter. The artifact is **versioned JSON, not a pickle** — it is data, never
code, so it can be diffed, reviewed and read from a source you do not control.
And the round trip is **bit-exact**: `scored` is identical to what the original
vectorizer would have produced, element by element, not within a tolerance.

One part is not meant to be read: the **idf vector is a base64 string** of raw
IEEE-754 bits, because it is thirty thousand floats nobody inspects by eye and
writing it as JSON numbers was measurably the most expensive thing in the file.
The vocabulary, the options and the header stay plain text, which is where
diffing and review actually happen — [`docs/decisions/0011`](../decisions/0011-persistence-format.md)
has the measurements.

`Save`/`Load` also accept a `Stream`, and both have async counterparts. A stream
you pass in is never disposed for you; the `path` overloads own the file handle
they open.

`CountVectorizer` and `HashingVectorizer` persist the same way. Hashing is
stateless, but its **options** still round-trip — a pipeline reloaded with a
different `NumFeatures` or `AlternateSign` produces different columns for the
same document, and nothing downstream would notice.

### Reading a file you did not write

Every count in an artifact sizes a buffer, so loading is bounded:

```csharp
using Lodestar.Text.Persistence;

var strict = new ArtifactLoadOptions { MaxVocabularySize = 50_000, MaxTotalBytes = 8L * 1024 * 1024 };
TfidfVectorizer model = TfidfVectorizer.Load("model.json", strict);
```

Anything the file gets wrong — a truncated document, an unknown property, an
unsupported version, a vocabulary that is not sorted, a limit exceeded — raises
`InvalidDataException` with a message naming the problem. The reasoning behind
the format is in
[decision 0011](../decisions/0011-persistence-format.md).

See the [equivalence table](../equivalence.md) for the exact correspondence with
each scikit-learn call.
