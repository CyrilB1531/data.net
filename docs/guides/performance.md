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
> chiffres définitifs.

## Comparaison face à Python (rapidfuzz)

Banc croisé à **méthodologie identique des deux côtés** (même corpus ASCII
committé, débit ns/paire, autoscaling, best-of-5). Voir [`bench/README.md`](../../bench/README.md)
pour lancer :

```bash
python bench/python/bench_levenshtein.py                       # rapidfuzz
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare   # DataNet.Text
python bench/compare.py                                        # tableau
```

Mesure indicative (rapidfuzz 3.14.5 / Python 3.12 ; DataNet.Text / .NET 10 ;
machine de dev — chiffres non normatifs) :

| Longueur | Python (rapidfuzz) | C# (DataNet.Text) | Rapport |
|---:|---:|---:|---|
| 8 | 175 ns/paire | **34 ns/paire** | **5,2× C# plus rapide** |
| 32 | **309 ns/paire** | 1 429 ns/paire | 4,6× Python plus rapide |
| 128 | **2,5 µs/paire** | 39,6 µs/paire | 16× Python plus rapide |
| 512 | **20 µs/paire** | 755 µs/paire | 37× Python plus rapide |

**Lecture honnête.**

- Sur chaînes **courtes**, C# domine : pas d'overhead d'appel interpréteur→C par
  paire. C'est le cas typique du rapprochement de noms/identifiants.
- Sur chaînes **longues**, rapidfuzz écrase : son noyau C utilise l'algorithme
  **bit-parallèle de Myers** (`O(nm/w)`, `w`=64), là où notre implémentation est
  un DP naïf `O(nm)`. L'écart n'est donc pas un problème de langage mais
  d'algorithme.
- **Action.** Implémenter Myers bit-parallèle pour `Distance` (et `Indel`) est la
  prochaine optimisation ; la validation est déjà en place (les 1235 cas d'oracle
  + tests de propriétés valideront instantanément la nouvelle implémentation).
  Suivi dans [`../decisions/0004-levenshtein-myers-backlog.md`](../decisions/0004-levenshtein-myers-backlog.md).

> rapidfuzz expose aussi des API **batch** (`process.cdist`) qui amortissent la
> frontière Python→C : plus rapides que la boucle par-paire mesurée ici. La
> comparaison ci-dessus reflète l'usage « une paire à la fois » qu'écrit un
> utilisateur Python courant.
