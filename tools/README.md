# Development tools — oracle generation, vendored data, packaging checks

Three scripts. `generate_oracles.py` produces the reference values the test suite
replays; `fetch_stopwords.py` produces source that is *shipped*, which is why it
verifies what it downloaded before writing anything;
`check_nuspec_dependencies.py` verifies what the packages *declare*.

## `generate_oracles.py`

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

## `fetch_stopwords.py`

Regenerates `src/DataNet.Text/Vectorization/StopWords.Snowball.cs` from the
Snowball stop-word lists (BSD-3-Clause). No third-party package needed — the
standard library is enough:

```bash
python tools/fetch_stopwords.py            # regenerate
python tools/fetch_stopwords.py --check    # verify the checked-in file is current
```

Each file is checked against a pinned SHA-256 before use. A mismatch means
Snowball edited the list upstream: read the diff, update the pin, adjust the
counts in `StopWordsTests`, and record it — do not regenerate quietly. The nltk
stop-word corpus is **not** a permitted source here, whatever its convenience:
see [`../docs/decisions/0010-stop-word-list-provenance.md`](../docs/decisions/0010-stop-word-list-provenance.md).

## `check_nuspec_dependencies.py`

Asserts that the `<dependencies>` of every packed `.nupkg` match an expected
table, exactly — an unexpected dependency fails as loudly as a missing one:

```bash
python tools/check_nuspec_dependencies.py artifacts
```

A package's dependency graph is a *build output*, derived from whatever restore
resolved, so nobody writes it down and nothing notices when it drifts. This
script is where it is written down. It matters more since the three packages
version independently: `DataNet.Fuzzy` reaches `DataNet.Text` through a
`PackageReference`, and that edge is now the one thing holding the two together.
See [`../docs/decisions/0012-per-package-versioning.md`](../docs/decisions/0012-per-package-versioning.md).

## Rules

- **Code-point semantics.** rapidfuzz/jellyfish iterate over code points; the C#
  suite therefore replays with `TextElement.CodePoint`. No lone surrogate is
  emitted (it would not survive the JSON round-trip).
- **Provenance.** We *run* these libraries to generate data — which creates no
  right over the outputs — but we do not **transcribe** any code. `python-
  Levenshtein` (GPL) is excluded even from generation, for hygiene. See
  [`../docs/decisions/0003-provenance-and-licensing.md`](../docs/decisions/0003-provenance-and-licensing.md).
