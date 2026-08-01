# 0005 — Écart assumé avec jellyfish sur les marques combinantes (Hamming, Jaro)

**Statut :** accepté · **Date :** 2026-08-01

## Contexte

Le brief mappe Hamming sur `jellyfish.hamming_distance` et Jaro/Jaro-Winkler sur
`jellyfish.jaro*`. En validant, on a constaté que jellyfish 1.2.1 **diverge de la
définition standard sur ≈ 5 % du corpus** (Hamming : 62/1241), toujours des
chaînes dégénérées comportant des marques combinantes / émojis / scripts mêlés.
Le même phénomène affecte `jaro_similarity` et `jaro_winkler_similarity` sur ces
mêmes entrées (le nombre exact est enregistré dans `jellyfish_divergences` des
métadonnées de chaque oracle).

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

- **Implémenter la définition standard** (Hamming : positions différentes +
  écart de longueur ; Jaro/Jaro-Winkler : algorithme classique avec seuil de
  boost à 0,7 pour Winkler), sur points de code ou unités UTF-16 selon
  `TextElement`.
- **Générer chaque oracle depuis une référence explicite** de cette définition
  (`_hamming_reference`, `_jaro_reference`, `_jaro_winkler_reference` dans
  `tools/generate_oracles.py`), et non depuis la sortie de jellyfish. Le
  générateur **compte et enregistre** le nombre de divergences jellyfish
  (`jellyfish_divergences`) pour la traçabilité.
- **Ancrer la parité jellyfish sur les entrées saines** via des cas de test
  écrits à la main (`[InlineData]`) dont les valeurs sont exactement celles de
  jellyfish (noms réels : MARTHA/MARHTA, DWAYNE/DUANE, DIXON/DICKSONX…).

## Conséquences

- `Hamming.Distance` est correct au sens de la définition et coïncide avec
  jellyfish partout où jellyfish calcule un Hamming standard.
- L'écart est explicite, mesuré et versionné, conformément au principe §5
  (« soit on réplique, soit on documente l'écart »).
