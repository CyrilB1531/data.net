# Python → C# equivalence table

Filled in **as we go**: a row is added at the same time as each function is
implemented, never retrofitted at the end (§6.1 of the brief).

## DataNet.Text — distances & similarity

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
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
| --- | --- | --- | --- |
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
| --- | --- | --- | --- |
| `soundex(s)` | jellyfish | `Soundex.Encode(s)` | Initial letter + 3 digits. Exact parity (402 words). |
| `metaphone(s)` | jellyfish | `Metaphone.Encode(s)` | Parity on real words; jellyfish non-word quirks not reproduced ([decision 0007](decisions/0007-metaphone-scope.md)). |
| `nysiis(s)` | jellyfish | `Nysiis.Encode(s)` | Non-truncated variant. Exact parity (402 words). |

## DataNet.Text — sparse vectorization

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `CountVectorizer()` | scikit-learn | `new CountVectorizer()` | Sorted vocabulary, `token_pattern` `\b\w\w+\b` (single characters dropped), `lowercase` by default. Parity across 10 configs. |
| `CountVectorizer(ngram_range=(1,2))` | scikit-learn | `new CountVectorizer(new(){ NgramRange=(1,2) })` | Word n-grams joined by a space. |
| `CountVectorizer(analyzer="char"/"char_wb")` | scikit-learn | `Analyzer = AnalyzerKind.Char / CharWordBoundary` | Character n-grams (with/without crossing word boundaries). |
| `CountVectorizer(min_df=…, max_df=…)` | scikit-learn | `MinDf`, `MaxDf` | `<1` = proportion, `≥1` = absolute count (sklearn `_limit_features` semantics). |
| `CountVectorizer(strip_accents="unicode")` | scikit-learn | `StripAccents = true` | NFKD decomposition + removal of combining marks. |
| `CountVectorizer(stop_words="english")` | scikit-learn | `StopWords = StopWords.English` | sklearn's 318-word list (identical). Any custom collection accepted. |
| `nltk.corpus.stopwords.words("french")` | nltk | `StopWords.French` | **Not identical.** The shipped lists are Snowball's, not nltk's, for licensing reasons ([decision 0010](decisions/0010-stop-word-list-provenance.md)). Same for `German`, `Portuguese`, `Spanish`; `Italian` matches nltk word for word. |
| `scipy.sparse` (CSR) | scipy | `CsrMatrix` | Home-grown CSR: `ToDense`, L1/L2 norms, `NormalizeRows`, matrix-vector product. |
| `TfidfVectorizer()` | scikit-learn | `new TfidfVectorizer()` | `smooth_idf` + L2 normalization on by default. `idf = ln((1+n)/(1+df)) + 1`. Parity across 7 configs. |
| `TfidfTransformer()` | scikit-learn | `new TfidfTransformer()` | `use_idf`, `smooth_idf`, `sublinear_tf`, `norm` (L1/L2/none). |
| `HashingVectorizer()` | scikit-learn | `new HashingVectorizer()` | Hashing trick, no vocabulary. MurmurHash3-32 (seed 0) reproduced; alternate sign + L2 normalization by default. |

## DataNet.Text — model persistence

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `joblib.dump(vec, path)` / `pickle.dump(vec, f)` | joblib / pickle | `vec.Save(path)` / `vec.Save(stream)` | Versioned JSON, not a pickle: data only, never code. Applies to `CountVectorizer`, `TfidfVectorizer` and `HashingVectorizer`. UTF-8 without BOM; the idf vector is base64-encoded raw IEEE-754 bits, the rest is readable JSON. |
| `joblib.load(path)` / `pickle.load(f)` | joblib / pickle | `TfidfVectorizer.Load(path, options?)` | Static, not a constructor. Bounded by `ArtifactLoadOptions` — `pickle.load` has no equivalent, since it trusts the file by design ([decision 0011](decisions/0011-persistence-format.md)). |
| — (no equivalent) | — | `ArtifactLoadOptions` | Deliberate addition, not a port: caps vocabulary size, token length, JSON depth, total bytes and array length. Over a limit ⇒ `InvalidDataException` naming limit and value. |

## DataNet.Text — stemming

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `PorterStemmer(mode=ORIGINAL_ALGORITHM).stem(w)` | nltk | `PorterStemmer.Stem(w)` | Porter (1980) algorithm, 5 steps. Exact parity (86 words). |
| `SnowballStemmer("english").stem(w)` | nltk | `EnglishSnowballStemmer.Stem(w)` | Porter2: R1/R2 regions, exceptions. Exact parity (190 words). |
| `SnowballStemmer("french").stem(w)` | nltk | `FrenchSnowballStemmer.Stem(w)` | French Snowball: RV region, 6 steps, NFC-normalized input. Exact parity (152 words). |
| `SnowballStemmer("spanish").stem(w)` | nltk | `SpanishSnowballStemmer.Stem(w)` | Spanish Snowball: attached-pronoun step 0, accents stripped last. Exact parity (127 words). |
| `SnowballStemmer("portuguese").stem(w)` | nltk | `PortugueseSnowballStemmer.Stem(w)` | Portuguese Snowball: nasal `a~`/`o~` expansion, accents kept. Exact parity (105 words). |
| `SnowballStemmer("italian").stem(w)` | nltk | `ItalianSnowballStemmer.Stem(w)` | Italian Snowball: acute→grave folding, `u`/`i` marking. Exact parity (96 words); `enza`→`te` follows nltk over the published text, see [0008](decisions/0008-italian-enza-nltk-divergence.md). |
| `SnowballStemmer("german").stem(w)` | nltk | `GermanSnowballStemmer.Stem(w)` | German Snowball: `ß`→`ss`, `u`/`y` marking, R1 floored at 3, no RV region. Exact parity (88 words). |

## DataNet.Embeddings — sub-word tokenization & pooling

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `Tokenizer(WordPiece(vocab)).encode(t)` | tokenizers (HF) | `new WordPieceTokenizer(vocab).Encode(t)` | Greedy longest match, `##` continuation, `[UNK]`. Pre-tokenization `\w+\|[^\w\s]+`. Exact parity. |
| `Tokenizer(Unigram(...)).encode(t)` / `sp.encode(t)` | tokenizers / sentencepiece | `new SentencePieceTokenizer(vocab).Encode(t)` | Unigram via Viterbi (max log-probability). `▁` prefix, `identity` normalizer. Exact parity on the vocabularies the oracle replays: the self-trained `tiny_sp.model`, and XLM-R's own 250 002 pieces in fairseq layout (next row). Stock XLM-R, T5, ALBERT and camemBERT ship the `nmt_nfkc` normalizer, which the loaders **refuse** — parity is claimed over the vocabulary, not over that pipeline. See [decision 0013](decisions/0013-sentencepiece-parity-scope.md). |
| `sp.encode(t)` over the XLM-R vocabulary | sentencepiece | `new SentencePieceTokenizer(SentencePieceModelLoader.Load("xlmr_fairseq.model")).Encode(t)` | 250 002 pieces with `<s>`=0, `<pad>`=1, `</s>`=2, `<unk>`=3, `<mask>`=250001 — the layout HuggingFace gives XLM-R, and the one an id-based control filter gets wrong. Identical segmentation over Latin, Cyrillic and Japanese input, including text naming all five markers literally: none is ever matched as text. Fixture built by `tools/fetch_xlmr_vocab.py`. |
| `sp.encode(t)` with an all-positive-score vocabulary | sentencepiece | idem | The unknown piece is scored `min(0, min_score) - 10` where Python uses `min_score - 10`. Identical for every real model (scores are log-probabilities, so the floor never binds); DataNet penalises the unknown piece more where it does. [Decision 0013](decisions/0013-sentencepiece-parity-scope.md). |
| mean pooling + `F.normalize` | sentence-transformers | `Pooler.MeanPoolAndNormalize(...)` | Masked mean (padding excluded) + L2 normalization. |
| `util.semantic_search` / `corpus @ query` | sentence-transformers / numpy | `new EmbeddingIndex(dim).Search(q, k)` | Exhaustive SIMD-vectorized cosine. Top-k, index-ascending tie-break. |
| `onnxruntime.InferenceSession(...).run(...)` + pooling | onnxruntime | `new OnnxTextEmbedder(path).Embed(ids, mask)` | Loads an ONNX model (weights not redistributed), runs it, mean-pool + L2. Feeds `token_type_ids` only if the model declares it. |

## DataNet.Embeddings — vocabulary loaders

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `BertTokenizer(vocab_file=…)` vocabulary loading | transformers | `VocabTxtLoader.Load(path, …)` | One token per line, id = line number. Reproduces two quirks of the Python loop: a blank line is a token whose string is empty, and a repeated token keeps the **last** id. A UTF-8 BOM is stripped rather than absorbed into the first token. |
| `Tokenizer.from_file("tokenizer.json")` (WordPiece) | tokenizers (HF) | `TokenizerJsonLoader.LoadWordPiece(path)` | Reads `model.vocab`, `unk_token`, `continuing_subword_prefix`, and derives `lowercase` from the normalizer. **Refuses** a pipeline it does not reproduce — `NFKC`/`Precompiled` normalizers, a non-`Whitespace` pre-tokenizer, any `post_processor`, `truncation` or `padding` — rather than ignoring it. |
| `Tokenizer.from_file("tokenizer.json")` (Unigram) | tokenizers (HF) | `TokenizerJsonLoader.LoadUnigram(path)` | Reads the `[piece, score]` pairs and `unk_id`. `tokenizer.json` records no piece types, so they are derived: the `special` entries of `added_tokens` become `Control`, the piece at `unk_id` becomes `Unknown`. Pre-tokenizer must be `Metaspace` with `▁`. |
| `sentencepiece_model_pb2.ModelProto().ParseFromString(…)` | sentencepiece | `SentencePieceModelLoader.Load(path)` | Hand-written minimal protobuf reader (varint, length-delimited, fixed32). Pieces, scores, **types**, and `unk`/`bos`/`eos`/`pad` ids from `trainer_spec`. Scores are 32-bit floats widened to `double`, exactly as the Python binding does. **Refuses** any normalizer other than `identity`, since only that one is reproduced. |
| `sp.id_to_piece(i)` / `sp.get_score(i)` | sentencepiece | `vocab.Pieces[i].Piece` / `.Score` | Identical; scores compared at `1e-9` in the oracle. |
| `sp.IsControl(i)` / `sp.IsUnknown(i)` | sentencepiece | `vocab.Types[i]`, `vocab.IsMatchable(i)` | The type comes from the file. The previous constructor inferred it from ids 0/1/2, which is wrong for any model laying out differently — that constructor is now `[Obsolete]`. |

## DataNet.Fuzzy — applied fuzzy matching

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
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
- **Stop words.** `StopWords.English` is scikit-learn's list; the other five are
  Snowball's, because the nltk corpus carries no usable licence
  ([`decisions/0010`](decisions/0010-stop-word-list-provenance.md)). This is the
  one place where the library knowingly does not match nltk, so the gap is
  measured rather than described: French 154 words vs nltk's 157 (13 / 16 words
  apart), German 231 vs 232 (4 / 5), Portuguese 203 vs 207 (0 / 4), Spanish 308
  vs 313 (2 / 7), Italian identical. Matching is ordinal against the analyzer's
  output, so `StripAccents = true` also stops accented entries from matching —
  as it does in scikit-learn.
