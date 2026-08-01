# 0001 — Cible de framework : `net10.0`

**Statut :** accepté · **Date :** 2026-08-01 · **Révisé :** 2026-08-01

## Contexte

Le brief suggérait `net8.0` (LTS). La machine de développement / CI dispose du
**runtime .NET 10**. Après arbitrage, la cible retenue est explicitement
**`net10.0`**.

## Décision

- La bibliothèque `DataNet.Text` **et** les projets exécutables (tests,
  benchmarks) ciblent **`net10.0`**.
- Le `RollForward` qui servait à exécuter un hôte `net8.0` sur le runtime 10 est
  **retiré** : il n'a plus lieu d'être puisqu'on cible directement le runtime
  présent.
- `netstandard2.0` **non** ajouté : aucun consommateur .NET Framework / Unity
  identifié. À reconsidérer si un tel besoin apparaît (coût : polyfills pour
  `Span`, `ArrayPool`, etc.).

## Conséquences

- Compilation et packaging visent des consommateurs **`net10.0+`**. C'est un
  choix assumé en faveur des dernières API et performances runtime, au prix d'une
  portée plus étroite que `net8.0` LTS. Si un consommateur sur LTS 8 se présente,
  ajouter `net8.0` au `TargetFrameworks` (multi-ciblage) plutôt que de rétrograder.
- Le comportement testé est celui du runtime 10, à l'identique du runtime de
  production ciblé — plus de décalage test/prod introduit par le roll-forward.
- La CI installe le SDK **10.0.x**.
