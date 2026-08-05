# From string to vector — bag of words, TF-IDF, hashing

`DataNet.Text.Vectorization` reproduces `sklearn.feature_extraction.text`: it turns
a corpus of documents into a **sparse matrix** of features.

```bash
dotnet add package DataNet.Text
```

## Bag of words — `CountVectorizer`

```csharp
using DataNet.Text.Vectorization;

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
([decision 0010](../decisions/0010-stop-word-list-provenance.md)). The lists are
close — Italian is identical word for word, the others differ by a handful — but
if you need exactly what nltk removes, load the corpus yourself and pass it in.

Removal is an ordinal match against the analyzer's output, so a list only removes
what preprocessing leaves behind: with `StripAccents = true`, `même` becomes
`meme` and no longer matches. Single-letter entries (`c`, `d`, `l`, `à` in the
French list) never match under the default token pattern either, which drops
one-character tokens. scikit-learn behaves the same way in both cases.

## TF-IDF — `TfidfVectorizer`

The formula is scikit-learn's, **to the character** (a classic pitfall):
`idf(t) = ln((1 + n) / (1 + df(t))) + 1`, then L2 normalization of each row.

```csharp
var tv = new TfidfVectorizer();
CsrMatrix tfidf = tv.FitTransform(docs);
IReadOnlyList<double> idf = tv.Idf;   // the learned idf vector
```

Options (`TfidfOptions`): `UseIdf`, `SmoothIdf`, `SublinearTf`, `Norm` (L1/L2/none).

## Hashing — `HashingVectorizer`

No vocabulary (hence stateless, ideal for streaming). Uses MurmurHash3-32,
identical to sklearn.

```csharp
var hv = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 1 << 18 });
CsrMatrix hashed = hv.Transform(docs);   // no Fit needed
```

## Cosine similarity between two documents

```csharp
CsrMatrix m = new TfidfVectorizer().FitTransform(["the cat eats", "the dog eats"]);
double[,] d = m.ToDense();
// dot product of the two rows (already L2-normalized) = cosine
double cos = 0;
for (int j = 0; j < m.ColumnCount; j++) cos += d[0, j] * d[1, j];
Console.WriteLine(cos); // ~0.51
```

See the [equivalence table](../equivalence.md) for the exact correspondence with
each scikit-learn call.
