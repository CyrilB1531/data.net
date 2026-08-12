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
| `Tokenizer(WordPiece(vocab)).encode(t)` | tokenizers (HF) | `new WordPieceTokenizer(vocab).Encode(t)` | Greedy longest match, `##` continuation, `[UNK]`. Pre-tokenization `\w+\|[^\w\s]+`. Exact parity. The `added_tokens` table is matched as **text**, ahead of the pre-tokenizer, rather than folded into the vocabulary as whole-word entries — with `lstrip`, `rstrip` and `single_word` honoured, and the same `AddedTokenScanner` `BpeTokenizer` uses, so a flag cannot mean two things. Which text an entry is matched against is decided by its `normalized` field and not by `special`: non-normalized entries run in an outer pass over the raw input and emit the raw slice, normalized ones have their own content normalized and run over the lowercased gaps the first pass left. A `special`-but-normalized entry is lowercased like any other — [decision 0022](decisions/0022-added-token-matching-flags.md). `WordPieceVocabulary.Count` counts `Vocab` alone and therefore under-counts what `Encode` can emit, as `BpeVocabulary` already did. |
| `Tokenizer(Unigram(...)).encode(t)` / `sp.encode(t)` | tokenizers / sentencepiece | `new SentencePieceTokenizer(vocab).Encode(t)` | Unigram via Viterbi (max log-probability), preceded by the model's own `precompiled_charsmap` and its whitespace flags. Exact parity over four vocabularies and four different character maps — stock XLM-R's `nmt_nfkc` (the map every stock T5, ALBERT and camemBERT also carries, byte for byte), a `nmt_nfkc_cf` model, a hand-written three-rule map, and `tiny_sp.model`, which has none. `remove_extra_whitespaces` collapses runs of U+0020 only; a run of uncovered characters comes back as **one** unknown piece, as in Python. |
| `sp.normalize(t)` | sentencepiece | `vocab.Normalizer.Normalize(t)` | The `precompiled_charsmap` alone: a longest-match walk over a darts-clone trie. Covers every built-in rule and any `--normalization_rule_tsv`, because they all compile to that one blob. Not reimplemented on `string.Normalize(FormKC)`, which would drift: the map is frozen at the Unicode version that compiled it and the two already differ on 181 code points — [decision 0014](decisions/0014-precompiled-normalizer.md). |
| `sp.encode(t)` over the XLM-R vocabulary | sentencepiece | `new SentencePieceTokenizer(SentencePieceModelLoader.Load("xlmr_fairseq.model")).Encode(t)` | 250 002 pieces with `<s>`=0, `<pad>`=1, `</s>`=2, `<unk>`=3, `<mask>`=250001 — the layout HuggingFace gives XLM-R, and the one an id-based control filter gets wrong. Identical segmentation over Latin, Cyrillic and Japanese input, including text naming all five markers literally: none is ever matched as text. Fixture built by `tools/fetch_xlmr_vocab.py`. |
| `sp.encode(t)` with an all-positive-score vocabulary | sentencepiece | idem | The unknown piece is scored `min(0, min_score) - 10` where Python uses `min_score - 10`. Identical for every real model (scores are log-probabilities, so the floor never binds); DataNet penalises the unknown piece more where it does. [Decision 0013](decisions/0013-sentencepiece-parity-scope.md). |
| `Tokenizer(BPE(vocab, merges)).encode(t)` with a `ByteLevel` pre-tokenizer | tokenizers (HF) | `new BpeTokenizer(vocab).Encode(t)` | Lowest-ranked-merge-first, over a doubly-linked list of symbols and a priority queue rather than a rescan-and-shift loop — see the scaling figures in [decision 0017](decisions/0017-bpe-parity-scope.md). Added-token matching before merging — the whole `added_tokens` table, including the special tokens `model.vocab` also declares, with each entry's `lstrip`, `rstrip` and `single_word` flags honoured as measured ([decision 0022](decisions/0022-added-token-matching-flags.md)); the raw-versus-normalized pass that flag table also carries is moot here, since `LoadBpe` refuses any normalizer at all — `ignore_merges`, and `add_prefix_space`, which is applied per added-token-delimited segment and only where the segment does not already begin with a space, as `ByteLevel` does in Python. **End-to-end** parity over GPT-2's vendored 50 257-entry vocabulary and merge table (byte-level) and a self-trained model (the classic, non-byte-level lineage); `BpePatterns.Llama3` and `BpePatterns.Qwen2` are proven **at the split level only**, against the vocabulary the caller supplies — decision 0017 again. The split itself diverges from HuggingFace on letters and digits above the Basic Multilingual Plane (mathematical alphanumeric symbols, e.g.), because .NET's `\p{L}`/`\p{N}` test one UTF-16 surrogate half at a time. A byte-level model missing one of the 256 alphabet characters from `model.vocab` while declaring it as an added token now throws `ArgumentException` from `ByteLevelSymbols` rather than silently folding the added token's id in, as it did before issue #130; the reference does neither, dropping the uncovered byte instead since there is no `unk_token` to substitute — measured, `aQa` with byte `Q` missing from `model.vocab` and present only as an added token is `['a', 'a']` there — so this swaps one divergence for another, throwing rather than returning a wrong token stream. |
| `BPE(..., fuse_unk=True)` | tokenizers | `new BpeVocabulary(vocab, merges) { FuseUnk = true }` | A run of consecutive uncovered characters is one unknown token, not one each. The run stops at a pre-tokenizer boundary, so `"aZ Za"` under `Whitespace` keeps two. Fusing happens before merging, so a fused symbol can take part in a merge. With no `UnkToken` the flag does nothing, because an uncovered character is dropped rather than substituted; on a byte-level model it does nothing either, because all 256 characters are covered. |
| `tokenizer.decode(ids)` | tokenizers (HF) | `BpeTokenizer.Decode(ids)` | Byte-level: every UTF-8 byte round-trips exactly, including malformed-looking sequences that came from `Encode`. **`skipSpecialTokens` defaults to `false`** — the opposite of Python's `skip_special_tokens=True` — so `Decode(Encode(x)) == x` holds without passing an extra argument; pass `true` to drop added tokens. It drops exactly the added tokens whose `added_tokens` entry is `special`, carried on `AddedToken.Special`, matching Python's `skip_special_tokens`. Proven over the byte-level and classic corpora above, decode direction, including a GPT-2 corpus whose text names `<\|endoftext\|>` at the id `model.vocab` gives it. **An added token carrying `lstrip` (or `rstrip`) breaks the byte-exact round trip**, here and in Python alike: the absorbed whitespace is consumed into the match and is not restored, so `'a <mask> b'` decodes to `'a<mask> b'`. Following HuggingFace is the parity; restoring the space would be the divergence — [decision 0022](decisions/0022-added-token-matching-flags.md). |
| `tokenizer.add_tokens([...])` | tokenizers | `BpeVocabulary.AddedTokens` | An added token is matched as literal text and carries an id, but it is **not** a model vocabulary entry: a character it spells that `model.vocab` does not declare is still substituted with the unknown token. Measured, `aQa` with `Q` an added token absent from `model.vocab` and `single_word` on is `['a', '[UNK]', 'a']`. `TryGetId` and `Decode` still see it, matching `token_to_id` and `decode`. |
| — (refused) | tokenizers | `new BpeTokenizer(…)` throws `ArgumentException` | Two shapes the reference also refuses while reading the document: a merge naming a token `model.vocab` does not declare — measured, ``Token `Q` out of vocabulary`` — and a merge whose result is absent, refused here too but with a message of DataNet's own, since the reference panics there instead of raising: ``range end index 2 out of range for slice of length 1``. A third shape, an `unk_token` present only in `added_tokens`, the reference does **not** refuse to build: it loads the file, answers `token_to_id`, and encodes text the model already covers, raising only from `encode` and only on text needing a substitution the vocabulary cannot supply. DataNet refuses it here, at construction — earlier than the reference, a divergence in timing rather than outcome. |
| mean pooling + `F.normalize` | sentence-transformers | `Pooler.MeanPoolAndNormalize(...)` | Masked mean (padding excluded) + L2 normalization. |
| `util.semantic_search` / `corpus @ query` | sentence-transformers / numpy | `new EmbeddingIndex(dim).Search(q, k)` | Exhaustive SIMD-vectorized cosine. Top-k, index-ascending tie-break. |
| mean pooling over a `[batch, seq, dim]` tensor | sentence-transformers | `Pooler.MeanPoolBatch(...)` / `MeanPoolAndNormalizeBatch(...)` | Each row pooled against its own slice of the mask. Vectorized with `Vector<float>` on `net10.0`, scalar on `netstandard2.0`, and the two are bit-identical — asserted with `float` equality, not a tolerance, because one frozen corpus serves both builds. |
| `tokenizer(texts, padding=True, truncation=True, max_length=n)` | tokenizers (HF) | `new BatchEncoder(tokenizer, options).EncodeBatch(texts)` | Inserts the template's special tokens, truncates inside a budget that counts them as HuggingFace does, pads each batch to **its own** longest row (`padding="longest"`, never `"max_length"`) and builds the attention mask. Ids and mask replayed against `encode_batch` for equality, not within a tolerance. `TruncationStrategy.None` **refuses** an over-long text; HuggingFace's `truncation=False` returns it untruncated. |
| `TemplateProcessing(single="[CLS] $A [SEP]")` | tokenizers (HF) | `SpecialTokenTemplate.Bert` / `.Roberta` / `.T5` / `.None` | The wrapping as data. Tokens are named, never numbered — the id comes from the model's vocabulary through `ISubwordTokenizer.TryGetId`, so a vocabulary placing `[CLS]` anywhere works and one lacking it throws at construction. A pair template (`$A`/`$B`) is not supported: DataNet encodes one sequence at a time. |
| `onnxruntime.InferenceSession(...).run(...)` + pooling | onnxruntime | `new OnnxTextEmbedder(path).Embed(ids, mask)` | Loads an ONNX model (weights not redistributed), runs it, mean-pool + L2. Feeds `token_type_ids` only if the model declares it. Takes `ReadOnlySpan<long>` since 0.3.0, where it took `IReadOnlyList<long>`. Refuses an output whose rank is neither 3 nor 2, and any input or output name the model does not declare. |
| `SentenceTransformer.encode(texts, batch_size=n, normalize_embeddings=True)` | sentence-transformers | `new OnnxTextEmbedder(path, tokenizer).EmbedBatch(texts, options)` | The whole chain in one call: encode, sub-batch, pad, run, mean-pool, normalize, restore the caller's order. `SortByLength` buckets by length between sub-batches and changes nothing observable. Agreement with a float64 reference is bounded near 1e-7, not 1e-9: ONNX Runtime returns float32 and the vector is normalized in float32. `convert_to_tensor`, `show_progress_bar` and the pooling modes other than mean have no equivalent. |

## DataNet.Embeddings — vocabulary loaders

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `BertTokenizer(vocab_file=…)` vocabulary loading | transformers | `VocabTxtLoader.Load(path, …)` | One token per line, id = line number. Reproduces two quirks of the Python loop: a blank line is a token whose string is empty, and a repeated token keeps the **last** id. A UTF-8 BOM is stripped rather than absorbed into the first token. |
| `Tokenizer.from_file("tokenizer.json")` (WordPiece) | tokenizers (HF) | `TokenizerJsonLoader.LoadWordPiece(path)` | Reads `model.vocab`, `unk_token`, `continuing_subword_prefix`, and derives `lowercase` from the normalizer. The whole `added_tokens` table lands in `WordPieceVocabulary.AddedTokens` with all five flags — `lstrip`, `rstrip`, `single_word`, `special`, `normalized` — instead of being folded into `model.vocab`: a folded entry is an ordinary whole-word vocabulary member and cannot honour a flag. `normalized` absent falls to `!special`, which is Rust's `AddedToken::from` default rather than a measured behaviour — `tokenizers` refuses a file omitting the field, so no corpus can reach that path. **Refuses** a pipeline it does not reproduce — `NFKC`/`Precompiled` normalizers, a non-`Whitespace` pre-tokenizer, any `post_processor`, `truncation` or `padding` — rather than ignoring it, and refuses an `unk_token` that `model.vocab` does not define even when `added_tokens` does: the table is matched as text ahead of the model, so an unknown token declared only there is one the model can never fall back to. |
| `Tokenizer.from_file("tokenizer.json")` (Unigram) | tokenizers (HF) | `TokenizerJsonLoader.LoadUnigram(path)` | Reads the `[piece, score]` pairs and `unk_id`. `tokenizer.json` records no piece types, so they are derived: the `special` entries of `added_tokens` become `Control`, the piece at `unk_id` becomes `Unknown`. A `Precompiled` normalizer is read — it is the same blob a `spiece.model` carries, base64-encoded — through the same interpreter, so the two formats describe the same model identically. `NFKC` is still **refused**: it asks for the runtime's Unicode tables where the model asked for a frozen map. Pre-tokenizer must be `Metaspace` with `▁`. |
| `models.BPE.from_file(vocab, merges)` | tokenizers (HF) | `BpeFilesLoader.Load(vocabPath, mergesPath)` | Reads the pre-`tokenizer.json` `vocab.json` + `merges.txt` pair GPT-2 (and Llama-3, Qwen2) ship. Neither file carries a pipeline, so `byteLevel` (default `true`, GPT-2's own default) and the split pattern (`BpePatterns.Gpt2` when byte-level) are parameters, not read from the files. Proven over GPT-2's vendored `vocab.json`/`merges.txt` — [decision 0017](decisions/0017-bpe-parity-scope.md). |
| `Tokenizer.from_file("tokenizer.json")` (BPE) | tokenizers (HF) | `TokenizerJsonLoader.LoadBpe(path)` | Reads `model.vocab`/`model.merges`, the whole `added_tokens` table (the entries `model.vocab` also declares included — that is where every special token is — together with all five of each entry's flags — `lstrip`, `rstrip`, `single_word`, `special` and `normalized`), `ignore_merges`, `end_of_word_suffix`, `unk_token`, `fuse_unk`, and derives byte-level-ness and the split pattern from `pre_tokenizer` — a bare `ByteLevel` (stock GPT-2), `Whitespace` (classic lineage), or a `Sequence` of `Split` then `ByteLevel` (the Llama-3/Qwen2 shape). **Refuses**, naming what it found: `byte_fallback`, a **non-empty** `continuing_subword_prefix`, a **non-zero** `dropout`, any `normalizer`, a `ByteLevel` with `use_regex` off, a `ByteLevel` block declaring no `add_prefix_space` in any of the three positions one can appear in — top-level `pre_tokenizer`, a `Sequence` step, the `decoder` — since `tokenizers` has no default for that field and refuses the file itself, `truncation`, `padding`, a `post_processor`, any other pre-tokenizer shape, and a `decoder` whose byte-level-ness disagrees with the model's own — which would not decode what it encodes, in Python either. **Accepts** the values that provably change nothing: an empty `continuing_subword_prefix`, a `dropout` of `0.0`, and an `end_of_word_suffix` of `""`, which reads back as absent since an empty marker marks nothing — `bpe_no_op_settings.json` replays `tokenizers` producing the same tokens with each of the three as without it. An omitted `use_regex` and an omitted `trim_offsets` are accepted too: the first has a default in the reference, the second is never read here. Proven over all three pipeline shapes: byte-level GPT-2, the classic lineage, and a `Sequence` pattern shaped like Qwen2's — split-level only for Llama-3/Qwen2 themselves, decision 0017. |
| `sentencepiece_model_pb2.ModelProto().ParseFromString(…)` | sentencepiece | `SentencePieceModelLoader.Load(path)` | Hand-written minimal protobuf reader (varint, length-delimited, fixed32). Pieces, scores, **types**, and `unk`/`bos`/`eos`/`pad` ids from `trainer_spec`. Scores are 32-bit floats widened to `double`, exactly as the Python binding does. The `normalizer_spec` is read, not merely inspected: its `precompiled_charsmap` becomes a `PrecompiledNormalizer`. **Refuses** a normalizer named without a map to apply, or a map that will not parse — nothing is decided from `normalizer_spec.name`. |
| `sp.id_to_piece(i)` / `sp.get_score(i)` | sentencepiece | `vocab.Pieces[i].Piece` / `.Score` | Identical; scores compared at `1e-9` in the oracle. |
| `sp.IsControl(i)` / `sp.IsUnknown(i)` | sentencepiece | `vocab.Types[i]`, `vocab.IsMatchable(i)` | The type comes from the file. The previous constructor inferred it from ids 0/1/2, which is wrong for any model laying out differently — that constructor is now `[Obsolete]`. |

## DataNet.Embeddings — index persistence

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `numpy.save(path, matrix)` | numpy | `index.Save(path)` / `index.Save(stream)` | Versioned JSON whose vector block is base64-encoded raw little-endian IEEE-754 bits, not a `.npy` memory dump. Carries the normalization flag and an optional id per vector — a `.npy` header already carries `shape`/`dtype`, so the per-vector dimension is recoverable as `shape[1]`, but the flag and the ids have nowhere to live in it. |
| `numpy.load(path)` | numpy | `EmbeddingIndex.Load(path, options?)` | Static, not a constructor. Returns a queryable index rather than an array, and bounds every count against `ArtifactLoadOptions` before it sizes a buffer — the vector block by `MaxTotalBytes` in bytes before parsing, the rest by `MaxArrayLength` in elements. |
| `faiss.write_index(idx, path)` / `faiss.read_index(path)` | faiss | `index.Save(path)` / `EmbeddingIndex.Load(path)` | Comparable in purpose, not in structure: DataNet's index is exhaustive (`IndexFlatIP`-shaped), so there is no graph or quantizer to serialize. An approximate index is a separate decision, not made. |
| — (a parallel `list[str]` the caller keeps) | — | `index.Add(vector, id)` / `index.GetId(i)` | Deliberate addition: without ids in the file, a reloaded index is a wall of anonymous integers. |

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

## DataNet.Metrics — classification metrics

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `accuracy_score(y_true, y_pred)` | scikit-learn | `Accuracy.Score(yTrue, yPred)` | Identical, `normalize` included. The overload taking a `ConfusionMatrix` scores only the samples that matrix kept. |
| `confusion_matrix(y_true, y_pred, labels=…)` | scikit-learn | `ConfusionMatrix.Compute(…)` | Rows are true labels. Label order is the sorted union, or the caller's order left unsorted. Counts are `double` because `sampleWeight` is supported ([`decisions/0016`](decisions/0016-metrics-package-placement.md)). |
| `precision_score(…, average=…)` | scikit-learn | `Precision.Score(…, Averaging…)` | All four modes. `average=None` is `Precision.PerClass`, a method rather than an enum member: it returns one value per class, not a scalar. |
| `recall_score(…, average=…)` | scikit-learn | `Recall.Score(…, Averaging…)` | As above. |
| `f1_score(…, average=…)` | scikit-learn | `F1.Score(…, Averaging…)` | As above. |
| `fbeta_score(…, beta=…)` | scikit-learn | `FBeta.Score(…, beta, …)` | Finite `beta ≥ 0`; scikit-learn also accepts `inf`, which throws here. |
| `classification_report(…)` | scikit-learn | `ClassificationReport.Compute(…)`, `.ToText(digits)` | Structured *and* character-exact text. `ZeroDivision.NaN` renders `NaN` where Python writes `nan`; the numbers still match. |
| `zero_division=0/1/np.nan` | scikit-learn | `ZeroDivision.Zero/One/NaN` | Values identical. The `UndefinedMetricWarning` has no equivalent; `ZeroDivision.Throw` is the opt-in replacement. |
| `roc_auc_score(y_true, y_score)` | scikit-learn | `RocAuc.Score(…)` | Binary. `posLabel` is explicit here (default 1) where scikit-learn infers it. |
| `roc_auc_score(…, multi_class=…)` | scikit-learn | `RocAuc.MultiClass(…, MultiClassRocOptions)` | `ovr` and `ovo`. Separate method: the overloads would be ambiguous. Strategy, averaging, labels and weights travel in `MultiClassRocOptions`, which also carries `MaxDegreeOfParallelism` — no scikit-learn equivalent, opt-in, sequential by default. `sampleWeight` refused for `ovo`, as in scikit-learn. |
| `balanced_accuracy_score(…, adjusted=…)` | scikit-learn | `BalancedAccuracy.Score(…)` | Averages over the classes with a true sample, as scikit-learn does; `adjusted` divides by that same kept count, and returns `NaN` or `-∞` when only one class is kept — the same two values scikit-learn returns. The overload taking a `ConfusionMatrix` scores only the classes that matrix holds: with an explicit `labels` subset, a dropped sample counts nowhere, not even in a denominator. `balanced_accuracy_score` has no `labels` parameter, so there is no reference value for that case ([`decisions/0020`](decisions/0020-normalize-is-a-projection-not-a-parameter.md)). |
| `matthews_corrcoef(…)` | scikit-learn | `MatthewsCorrelation.Score(…)` | scikit-learn hard-codes `0.0` when the denominator collapses; here it is `ZeroDivision`, defaulting to that value, with `Throw` available. An extension beyond parity, not a divergence in value. The overload taking a `ConfusionMatrix` scores only the classes that matrix holds; `matthews_corrcoef` has no `labels` parameter, so there is no reference value for a restricted matrix ([`decisions/0020`](decisions/0020-normalize-is-a-projection-not-a-parameter.md)). |
| `cohen_kappa_score(…, weights=…)` | scikit-learn | `CohenKappa.Score(…, KappaWeighting…)` | `weights` renamed `weighting`, because `sampleWeight` shares the signature. `replace_undefined_by` maps onto `ZeroDivision`, defaulting to `NaN` — scikit-learn's value; it also covers a view that holds no weight at all, where scikit-learn returns the same. The weighted forms depend on label order. The overload taking a `ConfusionMatrix` scores only the classes that matrix holds; `cohen_kappa_score` **does** take `labels`, so a reference value exists here, and on the fixture the tests pin the two agree ([`decisions/0020`](decisions/0020-normalize-is-a-projection-not-a-parameter.md)). |
| `confusion_matrix(…, normalize=…)` | scikit-learn | `ConfusionMatrix.ToArray(Normalization)` | A projection, not a parameter on `Compute`: several metrics here read a matrix, and fractions would make them silently wrong ([`decisions/0020`](decisions/0020-normalize-is-a-projection-not-a-parameter.md)). |

## DataNet.Metrics — regression metrics

| Python | Library | C# | Differences |
| --- | --- | --- | --- |
| `mean_squared_error(…, multioutput=…)` | scikit-learn | `MeanSquaredError.Score(…)`, `.PerOutput(…)` | `multioutput` is the choice of method plus an optional `outputWeights` span, not an enum — `raw_values` changes the return type, which [`decisions/0016`](decisions/0016-metrics-package-placement.md) already ruled cannot be an enum member, and [`decisions/0021`](decisions/0021-multioutput-is-a-method-not-an-enum.md) applies that ruling here. 2-D targets arrive row-major with `outputCount`; there is no 2-D overload, because a span cannot carry one. Two refusals every metric in this block shares, both reproduced with the message their Python layer prints: a `sampleWeight` that is zero throughout gives `check_array`'s "Sample weights must contain at least one non-zero number." — the rule is *every* weight zero, not the sum, so `[-1, -2, -3]` still scores — and `outputWeights` summing to zero give `numpy.average`'s "Weights sum to zero, can't be normalized.", where the rule *is* the sum, so `[1, -1]` is refused and `[-1, -1]` scores. `ValueError` and `ZeroDivisionError` both become `ArgumentException`. |
| `root_mean_squared_error(…)` | scikit-learn | `RootMeanSquaredError.Score(…)`, `.PerOutput(…)` | A type of its own: scikit-learn removed `mean_squared_error(squared=False)` in 1.6. The root is taken per output, *before* the reduction, so on more than one output the result is not the root of `MeanSquaredError.Score` — that is scikit-learn's order too. |
| `mean_absolute_error(…)` | scikit-learn | `MeanAbsoluteError.Score(…)`, `.PerOutput(…)` | As above for `multioutput`. |
| `median_absolute_error(…)` | scikit-learn | `MedianAbsoluteError.Score(…)`, `.PerOutput(…)` | With `sampleWeight`, an *averaged* weighted percentile: the mean of the first value whose cumulative weight reaches half the total and the one just past the last that comes within **one machine epsilon** of it. That tolerance is scikit-learn's own (`fraction_above > np.finfo(float64).eps`) and it is load-bearing, not decoration: on `sample_weight = [0.1] * 10` an exact comparison returns `4.0` where scikit-learn returns `4.5`. A uniform weight is therefore *usually* the ordinary median but not always — measured, `[0.7] * 10` gives `5.0` on the weighted path against `4.5` on the unweighted one, because there the overshoot is wider than an epsilon. Both sides agree, divergently, with scikit-learn. |
| `mean_absolute_percentage_error(…)` | scikit-learn | `MeanAbsolutePercentageError.Score(…)`, `.PerOutput(…)` | The denominator is clamped at numpy's machine epsilon, `2**-52` — **not** `double.Epsilon`, which is 292 orders of magnitude smaller. `mean_absolute_percentage_error([0], [1])` is therefore `4503599627370496.0` on both sides. |
| `max_error(y_true, y_pred)` | scikit-learn | `MaxError.Score(yTrue, yPred)` | No `sampleWeight` and no multioutput, because `max_error` has neither and refuses 2-D input. A worst case is not an average. |
| `mean_squared_log_error(…)` | scikit-learn | `MeanSquaredLogError.Score(…)`, `.PerOutput(…)` | Refuses a target at or below −1 on either side, as scikit-learn does — `ArgumentException` for its `ValueError`; the message additionally names the side, which costs no parity because no value is returned either way. The logarithm is numpy's `log1p`, reached through Kahan's identity rather than `Math.Log(1.0 + x)`: on targets around `1e-9` the latter is out by 1.4e-8 relative, where this agrees with scikit-learn to a unit in the last place. |
| `root_mean_squared_log_error(…)` | scikit-learn | `RootMeanSquaredLogError.Score(…)`, `.PerOutput(…)` | As above, and the root is taken per output before the reduction as in `root_mean_squared_error`. |
| `r2_score(…, force_finite=…)` | scikit-learn | `R2.Score(…)`, `.PerOutput(…)`, `.VarianceWeighted(…)` | Two independent undefined cases, deliberately kept apart. Fewer than two samples is `ZeroDivision`, defaulting to `NaN` — scikit-learn's value, recorded in [`decisions/0020`](decisions/0020-normalize-is-a-projection-not-a-parameter.md) — while a truth of zero variance over two or more samples is `forceFinite`. They do not overlap. One shape divergence: on fewer than two samples with more than one output, `PerOutput` returns one `NaN` per output, where `r2_score` returns a single scalar `nan` before it ever consults `multioutput`. No number differs — every scalar-returning path here still gives `nan` — and a one-element array would break `PerOutput`'s own contract of one value per output. |
| `explained_variance_score(…)` | scikit-learn | `ExplainedVariance.Score(…)`, `.PerOutput(…)`, `.VarianceWeighted(…)` | Takes `forceFinite` but **no** `ZeroDivision`: it has no fewer-than-two-samples case to route, so `explained_variance_score([3], [5])` is `1.0`, not `nan`, and `PerOutput` matches scikit-learn exactly there — the divergence noted for `r2_score` is `r2_score`'s alone. |
| `mean_pinball_loss(…, alpha=…)` | scikit-learn | `PinballLoss.Score(…, alpha, …)`, `.PerOutput(…)` | Named for the loss rather than for the Python identifier's `mean_` prefix, matching the other ten. `alpha` outside `[0, 1]` throws `ArgumentOutOfRangeException` where scikit-learn raises `InvalidParameterError`. |

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
