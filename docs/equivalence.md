# Tableau d'équivalence Python → C#

Alimenté **au fil de l'eau** : une ligne est ajoutée en même temps que chaque
fonction implémentée, jamais rétroactivement (§6.1 du brief).

## DataNet.Text — distances & similarité

| Python | Bibliothèque | C# | Différences |
|---|---|---|---|
| `Levenshtein.distance(a, b)` | rapidfuzz | `Levenshtein.Distance(a, b)` | Compare des **unités UTF-16** par défaut ; passer `TextElement.CodePoint` pour la parité exacte avec Python sur les caractères hors BMP (émojis…). Poids `(1,1,1)`. |
| `Levenshtein.normalized_distance(a, b)` | rapidfuzz | `Levenshtein.NormalizedDistance(a, b)` | `distance / max(len(a), len(b))`, `0` si les deux sont vides. Identique. |
| `Levenshtein.normalized_similarity(a, b)` | rapidfuzz | `Levenshtein.NormalizedSimilarity(a, b)` | `1 - normalized_distance`. Deux chaînes vides ⇒ `1`. Identique. |

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
