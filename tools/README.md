# Development tools — oracle generation, vendored data, packaging checks

Scripts under `tools/`, each responsible for one input the test suite treats as
given: `generate_oracles.py` produces the reference values the test suite
replays; `fetch_stopwords.py` produces source that is *shipped*, which is why it
verifies what it downloaded before writing anything; `fetch_xlmr_vocab.py` and
`build_normalizer_fixtures.py` produce fixtures `generate_oracles.py` reads;
`build_tiny_models.py` builds fixtures too small for that pipeline to bother
with — two ONNX graphs and a trained BPE — and commits them directly;
`check_nuspec_dependencies.py` verifies what the packages *declare*; and
`check_version_floor.py` verifies that the version numbers the source tree keeps
in three places still agree.

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

## `fetch_xlmr_vocab.py`

Rebuilds `tests/oracles/xlmr_fairseq.model`, the fixture behind the XLM-R oracle:

```bash
python tools/fetch_xlmr_vocab.py            # rebuild
python tools/fetch_xlmr_vocab.py --check    # verify the checked-in fixture
```

It downloads `xlm-roberta-base`'s SentencePiece vocabulary (MIT — vocabulary
only, never weights), checks it against a pinned SHA-256, and **re-emits** it:
same 250 000 pieces, scores and types, at the ids HuggingFace gives them
(`<s>`=0, `<pad>`=1, `</s>`=2, `<unk>`=3, `<mask>`=250001). The relabelling is
the point — the stock file is laid out `<unk>`=0/`<s>`=1/`</s>`=2, which is the
one layout the old id-based control filter got right. The `normalizer_spec` is
copied across untouched, `nmt_nfkc` and its character map included; it was
overwritten with `identity` until #75 made the map readable. See
[`../docs/decisions/0014-precompiled-normalizer.md`](../docs/decisions/0014-precompiled-normalizer.md).

Like `tiny_sp.model`, the result is an *input* to `generate_oracles.py`, not one
of its outputs: it is committed, and the `Oracles are reproducible` job replays
it without touching the network. Rebuilding it is a deliberate act, and a
changed pin means the ids in `xlmr_fairseq.json` move with it.

## `build_normalizer_fixtures.py`

Trains the two small models the normalizer oracle needs beyond the XLM-R one:

```bash
python tools/build_normalizer_fixtures.py            # rebuild
python tools/build_normalizer_fixtures.py --check    # verify the checked-in fixtures
```

`nmt_nfkc_cf.model` carries the case-folding map; `custom_norm.model` carries a
map compiled from three hand-written rules and calls its normalizer nothing more
than `user_defined`. The second is the one that keeps the claim honest: what
`PrecompiledNormalizer` interprets is *a* character map, not the one map every
stock model happens to share. Both are committed inputs to
`generate_oracles.py`, like `tiny_sp.model` — CI never retrains them, and
training is not guaranteed reproducible across `sentencepiece` versions.

## `build_tiny_models.py`

Builds fixtures too small to fit the pipeline the other scripts share: two
ONNX graphs (`tiny_encoder.onnx`, `tiny_embedder.onnx`) and a trained
character-level BPE (`tiny_bpe.json`), all committed rather than rebuilt by
CI. See the module docstring for what each one proves.

`tiny_bpe.json`'s trainer is not byte-reproducible across runs: tokens and
merges that tie in frequency land at different ids each time, because the
Rust `HashMap` behind the frequency counts seeds its hash randomly per
process. So, like `tiny_sp.model`, the committed file is authoritative and
rebuilding it is a deliberate act — a diff there is expected, and must not be
committed without regenerating `bpe.json` in the same commit.

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

Dependency **ids and version ranges** are both asserted. The range matters as
much as the id here: a `PackageReference` emits the floor from
`src/Directory.Packages.props`, while the `DataNetUseProjectRefs` developer loop
emits `DataNet.Text`'s own current version. Same id, different number — which is
what lets this check catch a package accidentally built with the escape hatch
left on.

## `check_version_floor.py`

Checks the three places a `DataNet.Text` version number lives, each for a
different reason, none of which MSBuild relates to the others:

```bash
python tools/check_version_floor.py               # offline: the two rules below
python tools/check_version_floor.py --check-feed  # also: the floor is published
```

- `src/DataNet.Text/Version.props` — what `DataNet.Text` *is*.
- `src/Directory.Packages.props` — the *floor* `DataNet.Fuzzy` requires of it.
- `check_nuspec_dependencies.py` — the floor that check asserts actually shipped.

The floor must not exceed the declared version, and must already be on nuget.org
— that is what makes `git clone && dotnet build` work with no pack step. A floor
naming an unpublished version still builds for whoever raised it, whose cache is
warm, and fails for everyone else; `--check-feed` is what turns that into a CI
failure rather than a contributor's bug report.

## Rules

- **Code-point semantics.** rapidfuzz/jellyfish iterate over code points; the C#
  suite therefore replays with `TextElement.CodePoint`. No lone surrogate is
  emitted (it would not survive the JSON round-trip).
- **Provenance.** We *run* these libraries to generate data — which creates no
  right over the outputs — but we do not **transcribe** any code. `python-
  Levenshtein` (GPL) is excluded even from generation, for hygiene. See
  [`../docs/decisions/0003-provenance-and-licensing.md`](../docs/decisions/0003-provenance-and-licensing.md).
