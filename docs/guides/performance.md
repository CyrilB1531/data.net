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
machine de dev bruitée — chiffres non normatifs), **après** l'ajout du chemin
rapide Myers mono-mot (motif 16–64, Latin-1) :

| Longueur | Python (rapidfuzz) | C# (DataNet.Text) | Rapport | Chemin C# |
|---:|---:|---:|---|---|
| 8 | 175 ns/paire | **35 ns/paire** | **5,0× C# plus rapide** | DP |
| 32 | **309 ns/paire** | ~350 ns/paire | ≈ parité | Myers |
| 128 | **2,5 µs/paire** | 34 µs/paire | ~14× Python | DP (motif > 64) |
| 512 | **20 µs/paire** | 630 µs/paire | ~31× Python | DP (motif > 64) |

**Lecture honnête.**

- **Chaînes courtes (≤ ~40)** — le cas typique du rapprochement de noms /
  identifiants : C# est au niveau ou devant Python. Le mono-mot de Myers a fait
  passer la tranche 32 de 4,6× *plus lent* à la parité ; sous 16 caractères le DP
  reste le plus rapide (construire la table d'égalité coûterait plus que le calcul).
- **Chaînes longues (motif > 64)** — rapidfuzz garde l'avantage : son noyau C
  utilise le Myers **multi-mots** (`O(nm/w)`), là où l'on retombe sur le DP `O(nm)`.
  L'écart n'est pas un problème de langage mais d'algorithme.
- **Fait / à faire.** Le Myers **mono-mot** est livré (chemin rapide de `Distance`,
  validé par les cas d'oracle BMP). Le Myers **multi-mots** pour les longues
  chaînes reste au backlog — voir
  [`../decisions/0004-levenshtein-myers-backlog.md`](../decisions/0004-levenshtein-myers-backlog.md).

> rapidfuzz expose aussi des API **batch** (`process.cdist`) qui amortissent la
> frontière Python→C : plus rapides que la boucle par-paire mesurée ici. La
> comparaison ci-dessus reflète l'usage « une paire à la fois » qu'écrit un
> utilisateur Python courant.
