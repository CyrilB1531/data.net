# 0002 — Unité de comparaison Unicode

**Statut :** accepté · **Date :** 2026-08-01

## Contexte

C'est le piège n°1 du portage (§5). En Python 3, une `str` s'itère en **points de
code**. En C#, une `string` s'itère en **unités UTF-16** : un caractère hors du
plan multilingue de base (émoji, idéogramme rare) est une paire de substitution
et occupe deux positions. Une distance d'édition naïve en C# diverge donc de
Python sur ces entrées.

Trois unités possibles : unité UTF-16 (`char`), point de code (scalaire Unicode),
ou grappe de graphèmes (caractère perçu, ex. émoji + modificateur de teint).

## Décision

- **Par défaut : unité UTF-16** (`TextElement.Utf16Unit`). C'est le choix .NET
  natif, sans allocation, et il coïncide avec Python pour tout texte du BMP —
  soit l'immense majorité des cas réels.
- **Parité exacte avec Python : point de code** (`TextElement.CodePoint`),
  proposé sur chaque algorithme concerné. Coût : une passe de décodage en tampon
  loué (`ArrayPool`). C'est ce mode que rejoue la suite d'oracle (rapidfuzz
  travaille sur des points de code).
- **Grappe de graphèmes : différée.** Nécessite `StringInfo`/segmentation, alloue,
  et n'a pas d'oracle Python direct dans les bibliothèques ciblées. À ajouter si
  un besoin concret apparaît (ex. comparaison « perçue » d'émojis composites).

Les surrogates isolés sont préservés tels quels (valeur d'unité), comme le fait
une `str` Python, plutôt que de lever une exception.

## Conséquences

- La documentation et le tableau d'équivalence signalent explicitement le mode
  par défaut et quand passer en `CodePoint`.
- Les corpus d'oracle sont générés en sémantique point de code et rejoués avec
  `TextElement.CodePoint` ; des tests unitaires dédiés vérifient la divergence
  attendue UTF-16 vs point de code sur des entrées supplémentaires.
