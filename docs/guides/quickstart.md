# Démarrage rapide

Comparer deux chaînes en quelques lignes.

## Installer

```bash
dotnet add package DataNet.Text
```

> Le paquet n'a **aucune dépendance externe** : c'est du .NET pur, sans Python à
> l'exécution.

## Comparer deux chaînes

```csharp
using DataNet.Text.Distances;

// Distance d'édition brute : nombre d'insertions/suppressions/substitutions.
int d = Levenshtein.Distance("kitten", "sitting");     // 3

// Similarité normalisée dans [0, 1] : 1 = identiques.
double sim = Levenshtein.NormalizedSimilarity("kitten", "sitting"); // 0.5714…

// Distance normalisée : 1 - similarité.
double nd = Levenshtein.NormalizedDistance("kitten", "sitting");    // 0.4286…
```

Les littéraux `string` se convertissent implicitement en `ReadOnlySpan<char>`,
donc aucun tampon n'est alloué pour les entrées.

## Unicode : choisir l'unité de comparaison

Par défaut, la comparaison porte sur les **unités UTF-16** (`char`) — le choix
.NET natif, le plus rapide. Pour reproduire *exactement* le résultat de Python /
rapidfuzz sur des caractères hors du plan multilingue de base (émojis,
idéogrammes rares), demander la comparaison par **point de code** :

```csharp
// "a😀" -> "a" : l'émoji est UN point de code, mais DEUX unités UTF-16.
Levenshtein.Distance("a\U0001F600", "a");                        // 2 (unités UTF-16)
Levenshtein.Distance("a\U0001F600", "a", TextElement.CodePoint); // 1 (comme Python)
```

C'est le piège Unicode n°1 du portage depuis Python ; il est documenté en détail
dans [`../decisions/0002-unicode-comparison-unit.md`](../decisions/0002-unicode-comparison-unit.md).

## Et ensuite

- [Choisir sa métrique](choisir-sa-metrique.md) _(à venir)_
- [Migrer depuis rapidfuzz](../migration/README.md)
- [Tableau d'équivalence Python → C#](../equivalence.md)
