# 0005 — Hamming : écart assumé avec jellyfish

**Statut :** accepté · **Date :** 2026-08-01

## Contexte

Le brief mappe Hamming sur `jellyfish.hamming_distance`. En validant, on a
constaté que jellyfish 1.2.1 **diverge de la définition standard de Hamming sur
62 cas / 1241** du corpus (≈ 5 %), tous des chaînes dégénérées comportant des
marques combinantes / scripts mêlés.

Investigation :

- Ce **n'est pas** une comparaison octet à octet (UTF-8) : `hamming('é','e')`
  vaut 1 côté jellyfish, 2 en octets.
- Ce **n'est pas** une normalisation NFC : normaliser avant comparaison ne change
  pas le taux d'accord (1179/1241 dans les deux cas).
- Pour toutes les entrées « normales » (ASCII, accents simples, codes de longueur
  égale — l'usage réel de Hamming), jellyfish **coïncide** avec la définition
  standard.

La cause exacte (comportement du cœur Rust de jellyfish sur les marques
combinantes) n'a pas été élucidée, et la reproduire reviendrait à copier une
bizarrerie non spécifiée.

## Décision

- **Implémenter la définition standard** de Hamming : nombre de positions
  différentes sur le préfixe commun (comparaison de points de code, ou d'unités
  UTF-16 selon `TextElement`), plus la différence de longueur.
- **Générer l'oracle depuis une référence explicite** de cette même définition
  (`_hamming_reference` dans `tools/generate_oracles.py`), et non depuis la sortie
  de jellyfish. Le générateur **compte et enregistre** le nombre de divergences
  jellyfish (`jellyfish_divergences` dans les métadonnées) pour la traçabilité.
- **Ancrer la parité jellyfish sur les entrées saines** via des cas de test écrits
  à la main (`[InlineData]`) dont les valeurs sont exactement celles de jellyfish.

## Conséquences

- `Hamming.Distance` est correct au sens de la définition et coïncide avec
  jellyfish partout où jellyfish calcule un Hamming standard.
- L'écart est explicite, mesuré et versionné, conformément au principe §5
  (« soit on réplique, soit on documente l'écart »).
