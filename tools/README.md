# Outils de développement — génération des oracles

`generate_oracles.py` produit les corpus de référence figés sous
`tests/oracles/`, à partir des bibliothèques Python canoniques. **Ces
bibliothèques sont des dépendances de développement uniquement** — jamais
d'exécution : le livrable C# ne dépend que du JSON committé.

## Régénérer

```bash
python -m venv .venv-oracles
. .venv-oracles/bin/activate          # Windows : .venv-oracles\Scripts\activate
pip install -r tools/requirements.txt
python tools/generate_oracles.py
```

Le script est **déterministe** (graine fixe, aucun horodatage) : régénérer sur une
autre machine produit un fichier identique — les diffs restent lisibles et
révisables. Committer le JSON régénéré fait partie du changement.

## Règles

- **Sémantique point de code.** rapidfuzz/jellyfish itèrent sur des points de
  code ; la suite C# rejoue donc avec `TextElement.CodePoint`. Aucun surrogate
  isolé n'est émis (il ne survivrait pas à l'aller-retour JSON).
- **Provenance.** On *exécute* ces libs pour générer des données — ce qui ne crée
  aucun droit sur les sorties — mais on ne **transcrit** aucun code. `python-
  Levenshtein` (GPL) est exclu même de la génération, par hygiène. Voir
  [`../docs/decisions/0003-provenance-and-licensing.md`](../docs/decisions/0003-provenance-and-licensing.md).
