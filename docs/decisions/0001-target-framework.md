# 0001 — Cible de framework : `net8.0`

**Statut :** accepté · **Date :** 2026-08-01

## Contexte

Le brief demande `net8.0` (LTS), avec `netstandard2.0` seulement si un besoin de
compatibilité descendante est avéré. La machine de développement / CI ne dispose
que du **runtime .NET 10** (aucun runtime .NET 8 installé).

## Décision

- La bibliothèque `DataNet.Text` cible **`net8.0`** : LTS, large adoption, socle
  d'API stable, et supporté par le SDK .NET 10.
- Les projets **exécutables** (tests, benchmarks) ciblent aussi `net8.0` mais
  fixent `<RollForward>LatestMajor</RollForward>` afin de s'exécuter sur le
  runtime .NET 10 présent, sans exiger l'installation du runtime 8.
- `netstandard2.0` **non** ajouté pour l'instant : aucun consommateur .NET
  Framework / Unity identifié. À reconsidérer si un tel besoin apparaît (coût :
  polyfills pour `Span`, `ArrayPool`, etc.).

## Conséquences

- Compilation et packaging visent des consommateurs `net8.0+`.
- Le comportement testé est celui du runtime 10 via roll-forward ; c'est
  acceptable car les API utilisées (`Span`, `ArrayPool`, `Rune`) sont stables
  entre 8 et 10. Si un jour un écart de runtime est suspecté, installer le
  runtime 8 et retirer le roll-forward pour tester à l'identique.
