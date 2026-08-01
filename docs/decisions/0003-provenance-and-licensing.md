# 0003 — Provenance du code et licence

**Statut :** accepté · **Date :** 2026-08-01

## Contexte

Le projet sera publié sur GitHub — une distribution au sens des licences libres.
Les obligations s'appliquent pleinement dès le premier commit (§10).

## Décision

- **Licence du projet : Apache-2.0.** Aussi permissive que MIT, avec en plus une
  concession de brevet explicite et une clause de contribution — le défaut usuel
  pour une bibliothèque destinée à l'entreprise. Fichier `LICENSE` présent dès
  l'initialisation ; `PackageLicenseExpression=Apache-2.0` dans les métadonnées.
- **Règle de provenance : aucune transcription de code copyleft.** Une traduction
  d'un langage vers un autre est un travail dérivé. `python-Levenshtein` (GPL)
  est donc **exclu** — ni transcrit, ni même utilisé pour générer des données de
  test (par hygiène, bien que la GPL ne revendique rien sur les sorties d'un
  programme).
- **Sources autorisées**, par ordre de préférence : articles/pseudo-codes publiés
  (les algorithmes ne sont pas protégeables) ; manuels et documentation ;
  implémentations sous licence permissive à titre de *référence de comportement*
  uniquement — rapidfuzz (MIT), jellyfish (MIT), textdistance (MIT),
  scikit-learn (BSD-3). On reproduit les entrées/sorties et un nommage analogue,
  jamais le source.
- **Génération de données de test.** rapidfuzz/jellyfish sont exécutés par
  `tools/generate_oracles.py` pour produire les JSON d'oracle. Ce sont des
  dépendances de **développement**, jamais d'exécution ; elles ne sont pas
  redistribuées.

## Conséquences

- Chaque implémentation dont la source d'inspiration mérite d'être tracée fera
  l'objet d'une note dans ce dossier `decisions/`.
- `NOTICE` et `THIRD-PARTY-NOTICES.md` recensent les attributions ; ils sont mis
  à jour en même temps que l'ajout de toute dépendance ou ressource tierce.
- Les **poids de modèles ONNX** (lot 3) ne seront pas redistribués dans le dépôt :
  téléchargement à l'exécution + licence documentée au cas par cas.
