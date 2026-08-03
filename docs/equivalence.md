# Tableau d'équivalence Python → C#

Alimenté **au fil de l'eau** : une ligne est ajoutée en même temps que chaque
fonction implémentée, jamais rétroactivement (§6.1 du brief).

## DataNet.Text — distances & similarité

| Python | Bibliothèque | C# | Différences |
|---|---|---|---|
| `Levenshtein.distance(a, b)` | rapidfuzz | `Levenshtein.Distance(a, b)` | Compare des **unités UTF-16** par défaut ; passer `TextElement.CodePoint` pour la parité exacte avec Python sur les caractères hors BMP (émojis…). Poids `(1,1,1)`. |
| `Levenshtein.normalized_distance(a, b)` | rapidfuzz | `Levenshtein.NormalizedDistance(a, b)` | `distance / max(len(a), len(b))`, `0` si les deux sont vides. Identique. |
| `Levenshtein.normalized_similarity(a, b)` | rapidfuzz | `Levenshtein.NormalizedSimilarity(a, b)` | `1 - normalized_distance`. Deux chaînes vides ⇒ `1`. Identique. |
| `OSA.distance(a, b)` | rapidfuzz | `Osa.Distance(a, b)` | Alignement optimal (Damerau restreint) : transposition adjacente autorisée, sans réédition. Diffère du Damerau complet (`"CA"/"ABC"` ⇒ 3 vs 2). |
| `OSA.normalized_similarity(a, b)` | rapidfuzz | `Osa.NormalizedSimilarity(a, b)` | `1 - dist/max(len)`. Identique. |
| `DamerauLevenshtein.distance(a, b)` | rapidfuzz | `DamerauLevenshtein.Distance(a, b)` | Damerau non restreint (Lowrance-Wagner). `"CA"/"ABC"` ⇒ 2. N'est pas une métrique. |
| `DamerauLevenshtein.normalized_similarity(a, b)` | rapidfuzz | `DamerauLevenshtein.NormalizedSimilarity(a, b)` | `1 - dist/max(len)`. Identique. |
| `hamming_distance(a, b)` | jellyfish | `Hamming.Distance(a, b)` | Positions différentes + écart de longueur. Coïncide avec jellyfish sur entrées normales ; écart documenté sur marques combinantes ([décision 0005](decisions/0005-hamming-jellyfish-divergence.md)). |
| `Indel.distance(a, b)` | rapidfuzz | `Indel.Distance(a, b)` | Insertions/suppressions seules = `len(a)+len(b)-2·LCS`. Base de `fuzz.ratio`. |
| `Indel.normalized_similarity(a, b)` | rapidfuzz | `Indel.NormalizedSimilarity(a, b)` | `1 - dist/(len(a)+len(b))`. **×100 = `fuzz.ratio`.** |
| `jaro_similarity(a, b)` | jellyfish | `Jaro.Similarity(a, b)` | Vide ⇒ `0`. Coïncide avec jellyfish sauf quirks marques combinantes ([décision 0005](decisions/0005-hamming-jellyfish-divergence.md)). |
| `jaro_winkler_similarity(a, b)` | jellyfish | `JaroWinkler.Similarity(a, b)` | Boost de préfixe uniquement si Jaro > 0,7 (seuil de Winkler), poids `0,1`, préfixe ≤ 4. |
| `SequenceMatcher(None,a,b).find_longest_match(...).size` | difflib | `Lcs.SubstringLength(a, b)` | Plus longue sous-chaîne commune (contiguë). Même départage que difflib. |
| — (LCS classique) | — | `Lcs.SubsequenceLength(a, b)` | Plus longue sous-séquence (ordre préservé, non contiguë). Base d'`Indel`. |
| `SequenceMatcher(None,a,b).ratio()` | difflib | `RatcliffObershelp.Similarity(a, b)` | Gestalt `2·M/T`. `autojunk` **non** répliqué (identique pour ≤ 200 éléments ; [décision 0006](decisions/0006-ratcliff-autojunk.md)). |

## DataNet.Text — similarité d'ensembles (multiensembles de q-grammes)

| Python | Bibliothèque | C# | Différences |
|---|---|---|---|
| `Jaccard(qval=1).normalized_similarity(a, b)` | textdistance | `Jaccard.Similarity(a, b)` | Multiensembles (sacs) de q-grammes, `qval=1` par défaut. `\|A∩B\|/\|A∪B\|`. |
| `Sorensen(qval=1).normalized_similarity(a, b)` | textdistance | `SorensenDice.Similarity(a, b)` | `2·\|A∩B\|/(\|A\|+\|B\|)`. |
| `Overlap(qval=1).normalized_similarity(a, b)` | textdistance | `Overlap.Similarity(a, b)` | `\|A∩B\|/min(\|A\|,\|B\|)`. |
| `Tversky(qval=1).normalized_similarity(a, b)` | textdistance | `Tversky.Similarity(a, b)` | `α=β=1` par défaut (⇒ Jaccard). |
| `Cosine(qval=1).normalized_similarity(a, b)` | textdistance | `Cosine.Similarity(a, b)` | `\|A∩B\|/√(\|A\|·\|B\|)`. Passer `qval:2` pour des bigrammes de caractères. |

> textdistance lève une exception sur certaines entrées vides ; DataNet définit
> proprement : deux vides ⇒ `1`, une seule vide ⇒ `0`. L'oracle couvre les paires
> non vides (`qval=1`), les bords sont couverts par des tests unitaires.

## DataNet.Text — encodages phonétiques

| Python | Bibliothèque | C# | Différences |
|---|---|---|---|
| `soundex(s)` | jellyfish | `Soundex.Encode(s)` | Lettre initiale + 3 chiffres. Parité exacte (402 mots). |
| `metaphone(s)` | jellyfish | `Metaphone.Encode(s)` | Parité sur mots réels ; quirks jellyfish sur non-mots non reproduits ([décision 0007](decisions/0007-metaphone-scope.md)). |
| `nysiis(s)` | jellyfish | `Nysiis.Encode(s)` | Variante non tronquée. Parité exacte (402 mots). |

## DataNet.Text — vectorisation creuse

| Python | Bibliothèque | C# | Différences |
|---|---|---|---|
| `CountVectorizer()` | scikit-learn | `new CountVectorizer()` | Vocabulaire trié, `token_pattern` `\b\w\w+\b` (mono-caractères écartés), `lowercase` par défaut. Parité sur 10 configs. |
| `CountVectorizer(ngram_range=(1,2))` | scikit-learn | `new CountVectorizer(new(){ NgramRange=(1,2) })` | n-grammes de mots joints par espace. |
| `CountVectorizer(analyzer="char"/"char_wb")` | scikit-learn | `Analyzer = AnalyzerKind.Char / CharWordBoundary` | n-grammes de caractères (avec/sans franchissement de frontière). |
| `CountVectorizer(min_df=…, max_df=…)` | scikit-learn | `MinDf`, `MaxDf` | `<1` = proportion, `≥1` = compte absolu (sémantique sklearn `_limit_features`). |
| `CountVectorizer(strip_accents="unicode")` | scikit-learn | `StripAccents = true` | Décomposition NFKD + suppression des marques combinantes. |
| `scipy.sparse` (CSR) | scipy | `CsrMatrix` | Format CSR maison : `ToDense`, normes L1/L2, `NormalizeRows`, produit matrice-vecteur. |
| `TfidfVectorizer()` | scikit-learn | `new TfidfVectorizer()` | `smooth_idf` + normalisation L2 par défaut. `idf = ln((1+n)/(1+df)) + 1`. Parité sur 7 configs. |
| `TfidfTransformer()` | scikit-learn | `new TfidfTransformer()` | `use_idf`, `smooth_idf`, `sublinear_tf`, `norm` (L1/L2/aucune). |
| `HashingVectorizer()` | scikit-learn | `new HashingVectorizer()` | Astuce de hachage, sans vocabulaire. MurmurHash3-32 (graine 0) reproduit ; signe alterné + normalisation L2 par défaut. |
| `CountVectorizer(stop_words="english")` | scikit-learn | `StopWords = StopWords.English` | Liste de 318 mots vides de sklearn (identique). Toute collection personnalisée est acceptée. |

## DataNet.Text — racinisation (stemming)

| Python | Bibliothèque | C# | Différences |
|---|---|---|---|
| `PorterStemmer(mode=ORIGINAL_ALGORITHM).stem(w)` | nltk | `PorterStemmer.Stem(w)` | Algorithme de Porter (1980), 5 étapes. Parité exacte (86 mots). |
| `SnowballStemmer("english").stem(w)` | nltk | `EnglishSnowballStemmer.Stem(w)` | Porter2 : régions R1/R2, exceptions. Parité exacte (190 mots). |
| `SnowballStemmer("french").stem(w)` | nltk | `FrenchSnowballStemmer.Stem(w)` | Snowball français : région RV, 6 étapes, entrée normalisée NFC. Parité exacte (152 mots). |

## DataNet.Embeddings — tokenisation de sous-mots & pooling

| Python | Bibliothèque | C# | Différences |
|---|---|---|---|
| `Tokenizer(WordPiece(vocab)).encode(t)` | tokenizers (HF) | `new WordPieceTokenizer(vocab).Encode(t)` | Plus long préfixe glouton, continuation `##`, `[UNK]`. Pré-tokenisation `\w+\|[^\w\s]+`. Parité exacte. |
| mean pooling + `F.normalize` | sentence-transformers | `Pooler.MeanPoolAndNormalize(...)` | Moyenne masquée (padding exclu) + normalisation L2. |
| `util.semantic_search` / `corpus @ query` | sentence-transformers / numpy | `new EmbeddingIndex(dim).Search(q, k)` | Cosinus exhaustif vectorisé SIMD. Top-k, départage par index croissant. |
| `onnxruntime.InferenceSession(...).run(...)` + pooling | onnxruntime | `new OnnxTextEmbedder(path).Embed(ids, mask)` | Charge un modèle ONNX (poids non redistribués), exécute, mean-pool + L2. Passe `token_type_ids` seulement si le modèle le déclare. |

## DataNet.Fuzzy — appariement approximatif applicatif

| Python | Bibliothèque | C# | Différences |
|---|---|---|---|
| `fuzz.ratio(a, b)` | rapidfuzz | `Fuzz.Ratio(a, b)` | Similarité Indel ×100. Sensible à la casse (aucun prétraitement, comme rapidfuzz). |
| `fuzz.partial_ratio(a, b)` | rapidfuzz | `Fuzz.PartialRatio(a, b)` | Meilleure fenêtre glissante (plus court sur plus long ; les deux sens si longueurs égales). |
| `fuzz.token_sort_ratio(a, b)` | rapidfuzz | `Fuzz.TokenSortRatio(a, b)` | Tri des jetons puis `ratio`. |
| `fuzz.token_set_ratio(a, b)` | rapidfuzz | `Fuzz.TokenSetRatio(a, b)` | Jetons communs vs différences. |
| `fuzz.WRatio(a, b)` | rapidfuzz | `Fuzz.WRatio(a, b)` | Combinaison pondérée selon le rapport de longueurs. |
| `process.extract(q, choices, limit=…, score_cutoff=…)` | rapidfuzz | `Process.Extract(q, choices, limit:…, scoreCutoff:…)` | Scoreur par défaut `WRatio`, tri score décroissant (départage par index), seuil, court-circuit. |
| `process.extractOne(q, choices)` | rapidfuzz | `Process.ExtractOne(q, choices)` | Meilleur candidat ou `null`. |
| déduplication avec blocking | — (patron applicatif) | `Deduplicator.FindClusters(...)` | Partitionnement par clé de blocking + clôture transitive (union-find). Évite le O(n²). |

## Conventions

- **Unité de comparaison.** Sauf mention contraire, les distances sur chaînes
  comparent des `char` (unités UTF-16), ce qui est le choix .NET natif et le plus
  rapide. Les bibliothèques Python (rapidfuzz, jellyfish) itèrent sur des points
  de code : pour reproduire *exactement* leurs valeurs sur du texte
  supplémentaire (émojis, idéogrammes rares), passer `TextElement.CodePoint`.
  Voir [`decisions/0002-unicode-comparison-unit.md`](decisions/0002-unicode-comparison-unit.md).
- **`ReadOnlySpan<char>`.** Toutes les signatures de calcul acceptent des spans ;
  les littéraux `string` s'y convertissent implicitement, donc
  `Levenshtein.Distance("a", "b")` fonctionne sans allocation.
- **Culture.** Aucune opération n'est sensible à la culture par défaut. Les
  variantes acceptant une `CultureInfo` seront ajoutées là où la casse/les
  accents entrent en jeu (tokenisation, lot 2).

<!-- Lot 2 (vectorisation), Lot 3 (embeddings), Lot 4 (fuzzy applicatif) : lignes à ajouter au fil de l'eau. -->
