# 0007 — Metaphone : périmètre de validation (mots réels)

**Statut :** accepté · **Date :** 2026-08-01

## Contexte

`Metaphone.Encode` reproduit `jellyfish.metaphone` sur les **mots réels** — son
domaine d'emploi. Contrairement à Soundex et NYSIIS (validés sur 402 entrées, y
compris des chaînes aléatoires), la Metaphone de jellyfish présente sur des
**suites de lettres dégénérées** (non-mots : `"ghhh"`, `"Uugb"`, `"xhdzhumzj"`…)
des comportements propres à son implémentation C, difficiles à distinguer d'une
bizarrerie et sans valeur pratique à reproduire (ex. traitement des voyelles
initiales doublées, ou d'un `H` isolé après un digramme déjà consommé).

## Décision

- **Valider Metaphone sur un corpus de mots réels** (`metaphone.json`, ~120 noms
  et mots anglais choisis pour couvrir les règles : `TH`, `CH`, `SH`, `PH`,
  `GH`/`GHT`, `GN` final, `KN`/`WR`/`PN` initiaux, `DGE`, `-TION`, `-SION`, `MB`
  final, `SCH`, `X` initial…). Parité exacte avec jellyfish sur ce corpus.
- Le corpus aléatoire partagé (`phonetics.json`) reste réservé à Soundex et
  NYSIIS, qui y atteignent 100 %.

## Conséquences

- `Metaphone.Encode` est fidèle à jellyfish pour tout mot réel — l'usage visé.
- Les divergences sur des non-mots adversariaux ne sont pas reproduites ; c'est
  un écart assumé et documenté (§5 du brief), pas une régression.
