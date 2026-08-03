# Development tools — oracle generation

`generate_oracles.py` produces the frozen reference corpora under
`tests/oracles/`, from the canonical Python libraries. **These libraries are
development dependencies only** — never runtime ones: the C# deliverable depends
only on the committed JSON.

## Regenerate

```bash
python -m venv .venv-oracles
. .venv-oracles/bin/activate          # Windows: .venv-oracles\Scripts\activate
pip install -r tools/requirements.txt
python tools/generate_oracles.py
```

The script is **deterministic** (fixed seed, no timestamps): regenerating on
another machine produces an identical file — diffs stay readable and reviewable.
Committing the regenerated JSON is part of the change.

## Rules

- **Code-point semantics.** rapidfuzz/jellyfish iterate over code points; the C#
  suite therefore replays with `TextElement.CodePoint`. No lone surrogate is
  emitted (it would not survive the JSON round-trip).
- **Provenance.** We *run* these libraries to generate data — which creates no
  right over the outputs — but we do not **transcribe** any code. `python-
  Levenshtein` (GPL) is excluded even from generation, for hygiene. See
  [`../docs/decisions/0003-provenance-and-licensing.md`](../docs/decisions/0003-provenance-and-licensing.md).
