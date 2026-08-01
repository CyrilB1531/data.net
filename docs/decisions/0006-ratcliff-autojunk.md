# 0006 — Ratcliff-Obershelp : heuristique autojunk de difflib

**Statut :** accepté · **Date :** 2026-08-01

## Contexte

`difflib.SequenceMatcher` applique par défaut un filtre **autojunk** : dans une
séquence de plus de 200 éléments, tout élément apparaissant dans plus de 1 % des
positions est traité comme « junk » et ignoré lors de la recherche de blocs
communs. Cela peut modifier le `ratio()` sur les longues chaînes.

## Décision

- **Implémenter le vrai Ratcliff-Obershelp**, sans autojunk : `RatcliffObershelp`
  calcule `2·M/T` sur l'appariement récursif du plus long sous-bloc commun, sans
  écarter aucun élément.
- **Générer l'oracle avec `autojunk=False`**, donc en parité exacte avec notre
  implémentation à toutes les longueurs.

## Conséquences

- Pour toute entrée ≤ 200 éléments, `RatcliffObershelp.Similarity` est identique
  à `difflib` par défaut (l'autojunk ne se déclenche pas).
- Au-delà de 200 éléments, DataNet peut différer de `difflib` **par défaut**
  (mais coïncide avec `difflib(autojunk=False)`). C'est un choix assumé :
  l'autojunk est une optimisation heuristique de difflib, pas une propriété de la
  métrique Ratcliff-Obershelp. Écart documenté conformément au §5 du brief.
