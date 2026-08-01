# 0004 — Levenshtein : optimisation bit-parallèle (Myers)

**Statut :** mono-mot livré ; multi-mots au backlog · **Date :** 2026-08-01

## Contexte

Le banc croisé (`bench/`) montre que l'implémentation actuelle de
`Levenshtein.Distance` — un DP à ligne roulante `O(n·m)` — est nettement plus
lente que rapidfuzz sur les chaînes longues (≈ 37× à 512 caractères), alors
qu'elle est plus rapide sur les chaînes courtes (pas d'overhead d'appel). rapidfuzz
doit cet avantage à l'algorithme **bit-parallèle de Myers** (1999), en `O(n·⌈m/w⌉)`
avec `w = 64` bits par mot machine.

La performance étant l'argument central du projet, cet écart algorithmique doit
être comblé pour les chaînes moyennes/longues.

## Fait

- **Myers mono-mot livré** (`src/DataNet.Text/Distances/Myers.cs`), branché comme
  chemin rapide de `Distance` sur le chemin `char` pour un motif de longueur
  16–64 en Latin-1 ; repli sur le DP sinon. Zéro allocation (table `Peq` en
  `stackalloc`). Validé sans code de test supplémentaire par les cas d'oracle BMP
  (`Distance_default_utf16_matches_rapidfuzz_for_bmp_cases`).

## À faire

- **Myers multi-mots (blocs)** pour les motifs > 64 : c'est ce qui manque pour
  rattraper rapidfuzz sur les chaînes longues (tranches 128/512 du banc).
- Étendre le chemin rapide au mode point de code (`Distance<int>`) et à `Indel`.
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
