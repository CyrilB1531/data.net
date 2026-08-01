# 0004 — Levenshtein : optimisation bit-parallèle (Myers) — backlog

**Statut :** identifié, non implémenté · **Date :** 2026-08-01

## Contexte

Le banc croisé (`bench/`) montre que l'implémentation actuelle de
`Levenshtein.Distance` — un DP à ligne roulante `O(n·m)` — est nettement plus
lente que rapidfuzz sur les chaînes longues (≈ 37× à 512 caractères), alors
qu'elle est plus rapide sur les chaînes courtes (pas d'overhead d'appel). rapidfuzz
doit cet avantage à l'algorithme **bit-parallèle de Myers** (1999), en `O(n·⌈m/w⌉)`
avec `w = 64` bits par mot machine.

La performance étant l'argument central du projet, cet écart algorithmique doit
être comblé pour les chaînes moyennes/longues.

## Décision (à venir)

- Implémenter Myers bit-parallèle comme **chemin rapide** de `Distance` quand le
  motif tient sur un petit nombre de mots de 64 bits (cas ultra-courant), en
  conservant le DP générique comme repli (`Distance<T>` pour éléments arbitraires,
  et alphabets larges/points de code hors ASCII si besoin).
- Étendre à `Indel` (base de `fuzz.ratio`, lot 4), qui a sa propre variante
  bit-parallèle.
- **Le filet de sécurité est déjà en place** : les 1235 cas d'oracle rapidfuzz +
  les tests d'axiomes métriques valideront la nouvelle implémentation sans
  travail de vérification supplémentaire. C'est précisément le dividende de la
  stratégie §4 — on peut optimiser agressivement sans risque de régression
  silencieuse.

## Notes

- Myers manipule des masques de bits par caractère de l'alphabet ; l'implémenter
  proprement pour un alphabet Unicode arbitraire demande une table
  `char -> bitmask` (dictionnaire ou tableau selon la plage). Commencer par le
  chemin ASCII/BMP fréquent.
- Source d'inspiration autorisée : l'article publié de Myers, « A Fast Bit-Vector
  Algorithm for Approximate String Matching Based on Dynamic Programming »
  (JACM 1999) — pseudo-code publié, pas de transcription de source sous copyleft
  (cf. [`0003-provenance-and-licensing.md`](0003-provenance-and-licensing.md)).
