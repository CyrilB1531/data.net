# Python → C# equivalence table

Filled in **as we go**: a row is added at the same time as each function is
implemented, never retrofitted at the end (§6.1 of the brief).

## DataNet.Text — distances & similarity

| Python | Library | C# | Differences |
|---|---|---|---|
| `Levenshtein.distance(a, b)` | rapidfuzz | `Levenshtein.Distance(a, b)` | Compares **UTF-16 units** by default; pass `TextElement.CodePoint` for exact parity with Python on non-BMP characters (emoji…). Weights `(1,1,1)`. |
| `Levenshtein.normalized_distance(a, b)` | rapidfuzz | `Levenshtein.NormalizedDistance(a, b)` | `distance / max(len(a), len(b))`, `0` if both empty. Identical. |
| `Levenshtein.normalized_similarity(a, b)` | rapidfuzz | `Levenshtein.NormalizedSimilarity(a, b)` | `1 - normalized_distance`. Two empty strings ⇒ `1`. Identical. |
| `OSA.distance(a, b)` | rapidfuzz | `Osa.Distance(a, b)` | Optimal String Alignment (restricted Damerau): adjacent transposition allowed, no substring re-edited. Differs from full Damerau (`"CA"/"ABC"` ⇒ 3 vs 2). |
| `OSA.normalized_similarity(a, b)` | rapidfuzz | `Osa.NormalizedSimilarity(a, b)` | `1 - dist/max(len)`. Identical. |
| `DamerauLevenshtein.distance(a, b)` | rapidfuzz | `DamerauLevenshtein.Distance(a, b)` | Unrestricted Damerau (Lowrance-Wagner). `"CA"/"ABC"` ⇒ 2. Not a metric. |
| `DamerauLevenshtein.normalized_similarity(a, b)` | rapidfuzz | `DamerauLevenshtein.NormalizedSimilarity(a, b)` | `1 - dist/max(len)`. Identical. |
| `hamming_distance(a, b)` | jellyfish | `Hamming.Distance(a, b)` | Differing positions + length difference. Matches jellyfish on normal inputs; documented divergence on combining marks ([decision 0005](decisions/0005-hamming-jellyfish-divergence.md)). |
| `Indel.distance(a, b)` | rapidfuzz | `Indel.Distance(a, b)` | Insertions/deletions only = `len(a)+len(b)-2·LCS`. Basis of `fuzz.ratio`. |
| `Indel.normalized_similarity(a, b)` | rapidfuzz | `Indel.NormalizedSimilarity(a, b)` | `1 - dist/(len(a)+len(b))`. **×100 = `fuzz.ratio`.** |
| `jaro_similarity(a, b)` | jellyfish | `Jaro.Similarity(a, b)` | Empty ⇒ `0`. Matches jellyfish except combining-mark quirks ([decision 0005](decisions/0005-hamming-jellyfish-divergence.md)). |
| `jaro_winkler_similarity(a, b)` | jellyfish | `JaroWinkler.Similarity(a, b)` | Prefix boost only when Jaro > 0.7 (Winkler threshold), weight `0.1`, prefix ≤ 4. |
| `SequenceMatcher(None,a,b).find_longest_match(...).size` | difflib | `Lcs.SubstringLength(a, b)` | Longest common (contiguous) substring. Same tie-break as difflib. |
| — (classic LCS) | — | `Lcs.SubsequenceLength(a, b)` | Longest common subsequence (order-preserving, non-contiguous). Basis of `Indel`. |
| `SequenceMatcher(None,a,b).ratio()` | difflib | `RatcliffObershelp.Similarity(a, b)` | Gestalt `2·M/T`. `autojunk` **not** replicated (identical for ≤ 200 elements; [decision 0006](decisions/0006-ratcliff-autojunk.md)). |

## DataNet.Text — set similarity (q-gram multisets)

| Python | Library | C# | Differences |
|---|---|---|---|
| `Jaccard(qval=1).normalized_similarity(a, b)` | textdistance | `Jaccard.Similarity(a, b)` | Multisets (bags) of q-grams, `qval=1` by default. `\|A∩B\|/\|A∪B\|`. |
| `Sorensen(qval=1).normalized_similarity(a, b)` | textdistance | `SorensenDice.Similarity(a, b)` | `2·\|A∩B\|/(\|A\|+\|B\|)`. |
| `Overlap(qval=1).normalized_similarity(a, b)` | textdistance | `Overlap.Similarity(a, b)` | `\|A∩B\|/min(\|A\|,\|B\|)`. |
| `Tversky(qval=1).normalized_similarity(a, b)` | textdistance | `Tversky.Similarity(a, b)` | `α=β=1` by default (⇒ Jaccard). |
| `Cosine(qval=1).normalized_similarity(a, b)` | textdistance | `Cosine.Similarity(a, b)` | `\|A∩B\|/√(\|A\|·\|B\|)`. Pass `qval:2` for character bigrams. |

> textdistance raises on some empty inputs; DataNet defines them cleanly: both
> empty ⇒ `1`, one empty ⇒ `0`. The oracle covers non-empty pairs (`qval=1`);
> edges are covered by unit tests.

## DataNet.Text — phonetic encoding

| Python | Library | C# | Differences |
|---|---|---|---|
| `soundex(s)` | jellyfish | `Soundex.Encode(s)` | Initial letter + 3 digits. Exact parity (402 words). |
| `metaphone(s)` | jellyfish | `Metaphone.Encode(s)` | Parity on real words; jellyfish non-word quirks not reproduced ([decision 0007](decisions/0007-metaphone-scope.md)). |
| `nysiis(s)` | jellyfish | `Nysiis.Encode(s)` | Non-truncated variant. Exact parity (402 words). |

## DataNet.Text — sparse vectorization

| Python | Library | C# | Differences |
|---|---|---|---|
| `CountVectorizer()` | scikit-learn | `new CountVectorizer()` | Sorted vocabulary, `token_pattern` `\b\w\w+\b` (single characters dropped), `lowercase` by default. Parity across 10 configs. |
| `CountVectorizer(ngram_range=(1,2))` | scikit-learn | `new CountVectorizer(new(){ NgramRange=(1,2) })` | Word n-grams joined by a space. |
| `CountVectorizer(analyzer="char"/"char_wb")` | scikit-learn | `Analyzer = AnalyzerKind.Char / CharWordBoundary` | Character n-grams (with/without crossing word boundaries). |
| `CountVectorizer(min_df=…, max_df=…)` | scikit-learn | `MinDf`, `MaxDf` | `<1` = proportion, `≥1` = absolute count (sklearn `_limit_features` semantics). |
| `CountVectorizer(strip_accents="unicode")` | scikit-learn | `StripAccents = true` | NFKD decomposition + removal of combining marks. |
| `CountVectorizer(stop_words="english")` | scikit-learn | `StopWords = StopWords.English` | sklearn's 318-word list (identical). Any custom collection accepted. |
| `scipy.sparse` (CSR) | scipy | `CsrMatrix` | Home-grown CSR: `ToDense`, L1/L2 norms, `NormalizeRows`, matrix-vector product. |
| `TfidfVectorizer()` | scikit-learn | `new TfidfVectorizer()` | `smooth_idf` + L2 normalization on by default. `idf = ln((1+n)/(1+df)) + 1`. Parity across 7 configs. |
| `TfidfTransformer()` | scikit-learn | `new TfidfTransformer()` | `use_idf`, `smooth_idf`, `sublinear_tf`, `norm` (L1/L2/none). |
| `HashingVectorizer()` | scikit-learn | `new HashingVectorizer()` | Hashing trick, no vocabulary. MurmurHash3-32 (seed 0) reproduced; alternate sign + L2 normalization by default. |

## DataNet.Text — stemming

| Python | Library | C# | Differences |
|---|---|---|---|
| `PorterStemmer(mode=ORIGINAL_ALGORITHM).stem(w)` | nltk | `PorterStemmer.Stem(w)` | Porter (1980) algorithm, 5 steps. Exact parity (86 words). |
| `SnowballStemmer("english").stem(w)` | nltk | `EnglishSnowballStemmer.Stem(w)` | Porter2: R1/R2 regions, exceptions. Exact parity (190 words). |
| `SnowballStemmer("french").stem(w)` | nltk | `FrenchSnowballStemmer.Stem(w)` | French Snowball: RV region, 6 steps, NFC-normalized input. Exact parity (152 words). |

## DataNet.Embeddings — sub-word tokenization & pooling

| Python | Library | C# | Differences |
|---|---|---|---|
| `Tokenizer(WordPiece(vocab)).encode(t)` | tokenizers (HF) | `new WordPieceTokenizer(vocab).Encode(t)` | Greedy longest match, `##` continuation, `[UNK]`. Pre-tokenization `\w+\|[^\w\s]+`. Exact parity. |
| `Tokenizer(Unigram(...)).encode(t)` / `sp.encode(t)` | tokenizers / sentencepiece | `new SentencePieceTokenizer(vocab).Encode(t)` | Unigram via Viterbi (max log-probability). `▁` prefix, `identity` normalizer. Exact parity. |
| mean pooling + `F.normalize` | sentence-transformers | `Pooler.MeanPoolAndNormalize(...)` | Masked mean (padding excluded) + L2 normalization. |
| `util.semantic_search` / `corpus @ query` | sentence-transformers / numpy | `new EmbeddingIndex(dim).Search(q, k)` | Exhaustive SIMD-vectorized cosine. Top-k, index-ascending tie-break. |
| `onnxruntime.InferenceSession(...).run(...)` + pooling | onnxruntime | `new OnnxTextEmbedder(path).Embed(ids, mask)` | Loads an ONNX model (weights not redistributed), runs it, mean-pool + L2. Feeds `token_type_ids` only if the model declares it. |

## DataNet.Fuzzy — applied fuzzy matching

| Python | Library | C# | Differences |
|---|---|---|---|
| `fuzz.ratio(a, b)` | rapidfuzz | `Fuzz.Ratio(a, b)` | Indel similarity ×100. Case-sensitive (no preprocessing, like rapidfuzz). |
| `fuzz.partial_ratio(a, b)` | rapidfuzz | `Fuzz.PartialRatio(a, b)` | Best sliding window (shorter over longer; both directions when lengths are equal). |
| `fuzz.token_sort_ratio(a, b)` | rapidfuzz | `Fuzz.TokenSortRatio(a, b)` | Sort tokens then `ratio`. |
| `fuzz.token_set_ratio(a, b)` | rapidfuzz | `Fuzz.TokenSetRatio(a, b)` | Shared tokens vs differences. |
| `fuzz.WRatio(a, b)` | rapidfuzz | `Fuzz.WRatio(a, b)` | Weighted combination based on the length ratio. |
| `process.extract(q, choices, limit=…, score_cutoff=…)` | rapidfuzz | `Process.Extract(q, choices, limit:…, scoreCutoff:…)` | Default scorer `WRatio`, score-descending order (index tie-break), cutoff, short-circuit. |
| `process.extractOne(q, choices)` | rapidfuzz | `Process.ExtractOne(q, choices)` | Best candidate or `null`. |
| blocking deduplication | — (application pattern) | `Deduplicator.FindClusters(...)` | Partition by blocking key + transitive closure (union-find). Avoids O(n²). |

## Conventions

- **Comparison unit.** Unless stated otherwise, string distances compare `char`
  values (UTF-16 units), which is the native .NET choice and fastest. Python
  libraries (rapidfuzz, jellyfish) iterate over code points: to reproduce their
  values *exactly* on supplementary text (emoji, rare ideographs), pass
  `TextElement.CodePoint`. See [`decisions/0002-unicode-comparison-unit.md`](decisions/0002-unicode-comparison-unit.md).
- **`ReadOnlySpan<char>`.** All computation signatures accept spans; `string`
  literals convert implicitly, so `Levenshtein.Distance("a", "b")` works with no
  allocation.
- **Culture.** No operation is culture-sensitive by default. Overloads accepting a
  `CultureInfo` are added where case/accents matter (tokenization).
