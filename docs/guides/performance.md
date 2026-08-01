# Performance

La performance est l'argument de vente face à Python, donc elle est mesurée dès
le lot 1 avec [BenchmarkDotNet](https://benchmarkdotnet.org/), pas estimée.

## Reproduire

```bash
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Levenshtein*'
```

## Principes appliqués

- **`ReadOnlySpan<char>` partout.** Les entrées ne sont jamais copiées ; les
  littéraux `string` s'y convertissent sans allocation.
- **`ArrayPool<int>` pour la matrice de programmation dynamique.** La ligne DP est
  louée puis rendue : **zéro allocation managée par appel**, donc aucune pression
  sur le GC même sous forte charge.
- **Ligne DP roulante sur le plus court opérande** → mémoire `O(min(n, m))`.
- **Rognage des préfixes/suffixes communs** → effondre la bande DP sur les entrées
  quasi identiques (le cas courant en rapprochement d'enregistrements).

## Chiffres indicatifs — Levenshtein

Mesure `--job short` (itérations réduites : les moyennes sont bruitées, mais la
colonne allocation est fiable). Rejouer en job complet avant publication.

| Méthode | Longueur | Moyenne | Alloué |
|---|---|---:|---:|
| `Distance` (UTF-16) | 8 | ~37 ns | **0 B** |
| `Distance` (point de code) | 8 | ~208 ns | **0 B** |
| `Distance` (UTF-16) | 64 | ~7,0 µs | **0 B** |
| `Distance` (UTF-16) | 512 | ~0,73 ms | **0 B** |

**Lecture.**

- **Zéro allocation** à toutes les tailles : c'est le résultat structurant.
- Sur entrées **très courtes** (8), le mode `CodePoint` coûte ~5× le mode UTF-16 :
  la passe de décodage domine quand le calcul lui-même est minuscule. Dès 64+
  caractères, l'écart se referme (le décodage devient négligeable devant le DP
  quadratique). D'où le choix : **UTF-16 par défaut**, `CodePoint` à la demande.

> Les barres d'erreur du job court sont larges ; ne pas citer ces moyennes comme
> chiffres définitifs. La comparaison face à un baseline Python (rapidfuzz)
> viendra avec une mesure en job complet et un protocole documenté.
