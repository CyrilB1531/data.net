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
