# DataNet

Une boîte à outils **data science pour C#/.NET**, dans une philosophie honnête :

> On ne réécrit pas Python. On **utilise** l'écosystème .NET là où il est bon, et
> on n'**écrit du code natif** que là où .NET a un trou réel : le **texte**
> (similarité, vectorisation, recherche sémantique). Le tout **sans Python à
> l'exécution**.

## Pourquoi

Python domine l'analyse de données par son écosystème et son flux exploratoire,
pas par le langage — ses performances viennent de noyaux C/Fortran. C# apporte le
typage statique, un vrai parallélisme sans GIL, un refactoring sûr et un
déploiement simple. Le seul frein objectif dans le domaine du texte était
l'absence de bibliothèque .NET équivalente. DataNet lève ce frein.

## Deux livrables

1. **Code natif** là où c'est un trou → le paquet [`DataNet.Text`](src/DataNet.Text)
   (distances de chaînes, vectorisation, embeddings, fuzzy) — allocation-lean,
   `Span`, SIMD, zéro dépendance externe.
2. **Guides de migration** « je viens de Python » → [`docs/migration/`](docs/migration/README.md),
   qui pour chaque besoin (NumPy, pandas, scikit-learn, statsmodels, PyTorch,
   matplotlib, seaborn) indique la brique .NET à utiliser et les pièges.

Voir l'[**inventaire de migration à trois colonnes**](docs/migration/README.md) :
c'est la carte du projet (utiliser / écrire / trancher).

## État

| Lot | Contenu | État |
|---|---|---|
| 1 | Distances & similarité de chaînes | 🚧 Levenshtein livré (distance + ratio normalisé) |
| 2 | Tokenisation & vectorisation creuse (TF-IDF…) | à venir |
| 3 | Embeddings & recherche sémantique (ONNX) | à venir |
| 4 | Appariement approximatif applicatif (fuzz/process) | à venir |

## Démarrer

```bash
dotnet add package DataNet.Text
```

```csharp
using DataNet.Text.Distances;

Levenshtein.Distance("kitten", "sitting");             // 3
Levenshtein.NormalizedSimilarity("kitten", "sitting"); // 0.5714…
```

Guide complet : [`docs/guides/quickstart.md`](docs/guides/quickstart.md).

## Développer

```bash
dotnet build                                   # compile la solution
dotnet test                                    # rejoue les oracles + tests de propriétés
dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter '*Levenshtein*'
```

### Validation par oracles

La conformité au comportement Python est **prouvée**, pas supposée (§4 du brief) :
`tools/generate_oracles.py` fige quelques milliers de cas de référence issus de
rapidfuzz/jellyfish dans `tests/oracles/*.json` (versionnés) ; la suite C# les
rejoue avec une tolérance de `1e-9`. Python n'est qu'une dépendance de
développement. Voir [`tools/README.md`](tools/README.md).

## Structure

```
DataNet.sln
├── src/DataNet.Text/            distances, métriques, tokeniseurs, vectoriseurs
├── tests/DataNet.Text.Tests/    xUnit : oracles + propriétés
├── tests/oracles/               corpus JSON figés (générés depuis Python)
├── bench/DataNet.Text.Benchmarks/  BenchmarkDotNet
├── tools/generate_oracles.py    génération des références
└── docs/                        guides, tableau d'équivalence, journal de décisions
```

## Licence

[Apache-2.0](LICENSE). Voir [`NOTICE`](NOTICE) et
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) pour les attributions. Le choix
de licence et la règle de provenance du code sont documentés dans
[`docs/decisions/0003-provenance-and-licensing.md`](docs/decisions/0003-provenance-and-licensing.md).

_Ce dépôt ne constitue pas un avis juridique._
