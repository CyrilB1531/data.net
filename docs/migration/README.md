# De Python à .NET — inventaire de migration

Cette page est le **hub de migration** de DataNet. Elle répond à une question
simple : « je fais ça en Python, qu'est-ce que je fais en C# ? »

Le principe directeur du projet (voir la [justification](../../README.md)) est
**honnête** : on ne réécrit pas l'écosystème data science de Python. La majeure
partie existe déjà en .NET, et l'algèbre linéaire dense de Python s'appuie de
toute façon sur des noyaux BLAS/LAPACK Fortran qu'il n'y a aucun intérêt à
réimplémenter. On **utilise** l'existant, et on n'**écrit** du code natif que là
où .NET a un trou réel : le **texte** (similarité, vectorisation).

## Les trois colonnes

| Python | Rôle | Recommandation .NET | Verdict |
|---|---|---|---|
| **PyTorch** | tenseurs, autograd, entraînement, GPU | [TorchSharp](https://github.com/dotnet/TorchSharp) (= libtorch) ; [ONNX Runtime](https://onnxruntime.ai/) pour l'inférence seule | ✅ **Utiliser** |
| **matplotlib** | tracé | [ScottPlot](https://scottplot.net/), [Plotly.NET](https://plotly.net/), OxyPlot | ✅ **Utiliser** |
| **NumPy** | tableaux N-dim, algèbre dense | [Math.NET Numerics](https://numerics.mathdotnet.com/) (+ fournisseur natif MKL/OpenBLAS) ; `System.Numerics.Tensors` | ✅ **Utiliser** |
| **scikit-learn** | ML classique, pipelines, métriques | [ML.NET](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet) ; [SharpLearning](https://github.com/mdabros/SharpLearning) | ✅ **Utiliser** *sauf* vectorisation de texte → **DataNet.Text** |
| **pandas** | DataFrame, groupby, IO | [`Microsoft.Data.Analysis`](https://www.nuget.org/packages/Microsoft.Data.Analysis) ; [Deedle](https://fslab.org/Deedle/) | 🟡 **Utiliser** (plus rugueux) |
| **statsmodels** | régression éco, séries temporelles, tests | Math.NET (base) ; Accord.NET | 🟠 **À trancher** — l'économétrie riche est un manque |
| **seaborn** | viz statistique tidy | ScottPlot / Plotly.NET (graphes reconstruits) | 🟠 **À trancher** — presets statistiques absents |

**Légende.** ✅ un équivalent solide existe, on l'utilise tel quel. 🟡 un
équivalent existe mais moins mûr que Python ; prévoir de la colle. 🟠 le socle
existe, mais un pan entier manque : candidat à du code natif *si votre usage le
justifie*.

## Ce que DataNet écrit en natif

Une seule zone justifie vraiment du code : le **texte**. C'est le paquet
[`DataNet.Text`](../../src/DataNet.Text), livré par lots (voir le brief) :

1. **Distances & similarité de chaînes** — Levenshtein, Damerau-Levenshtein,
   Jaro-Winkler, Jaccard, Ratcliff-Obershelp, phonétique… _(en cours)_
2. **Tokenisation & vectorisation creuse** — `CountVectorizer`, `TfidfVectorizer`
   (sémantique sklearn exacte), matrice CSR maison.
3. **Embeddings & recherche sémantique** — ONNX Runtime + tokeniseurs de sous-mots.
4. **Appariement approximatif** — équivalents `rapidfuzz.fuzz` / `process`.

## Guides par bibliothèque

| Guide | Statut |
|---|---|
| [NumPy → .NET](numpy.md) | ébauche |
| [pandas → .NET](pandas.md) | ébauche |
| [scikit-learn → .NET](sklearn.md) | ébauche |
| [statsmodels → .NET](statsmodels.md) | ébauche |
| [PyTorch → .NET](pytorch.md) | ébauche |
| [matplotlib → .NET](matplotlib.md) | ébauche |
| [seaborn → .NET](seaborn.md) | ébauche |

Le **tableau d'équivalence détaillé** (appel Python → appel C#, différences de
comportement, notes de perf), alimenté au fil de l'eau, est dans
[`equivalence.md`](../equivalence.md).

> Ce document n'est pas un avis juridique ; les licences des dépendances tierces
> sont recensées dans [`THIRD-PARTY-NOTICES.md`](../../THIRD-PARTY-NOTICES.md).
