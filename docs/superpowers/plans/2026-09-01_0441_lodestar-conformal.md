# `Lodestar.Conformal` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `Lodestar.Conformal` 0.1.0 — split conformal prediction at MAPIE 1.5.0 parity — through every gate this repository runs, with the exchangeability warning carried where a caller cannot miss it.

**Architecture:** One public static class, `SplitConformal`, already written and building on both targets: arrays in, numbers out, the same boundary `Lodestar.Metrics` draws. What remains is everything that turns a compiling class into a package: a frozen oracle corpus generated from MAPIE, the two test projects, the packaging and CI wiring, the sample, the reference pages, the guide, and the record of the one place the behaviour deliberately diverges.

**Tech Stack:** C# / .NET (`net10.0;netstandard2.0`), xunit, MAPIE 1.5.0 + scikit-learn 1.9.0 + numpy 2.5.1 for oracle generation, Python 3.12+ for the generators.

**Spec:** [`docs/superpowers/specs/2026-09-01_0441_lodestar-conformal-split-conformal-prediction.md`](../specs/2026-09-01_0441_lodestar-conformal-split-conformal-prediction.md)

**Branch:** `feat/441-lodestar-conformal` (already created; `SplitConformal.cs`, `Version.props`, `Lodestar.Conformal.csproj` and the spec are committed on it).

## Global Constraints

- **Two target frameworks, one public API.** `net10.0;netstandard2.0`, never a reduced surface. Gaps close in the order polyfill → target-only package reference → hand-written fallback.
- **Zero dependencies.** `Lodestar.Conformal` carries nothing on `net10.0` and only `System.Memory` 4.6.3 + `System.Numerics.Vectors` 4.6.1 on `netstandard2.0` — exactly `Lodestar.Metrics`' graph. No `System.Text.Json`: there is nothing to serialise.
- **No `ProjectReference` from `src/`.** A CI job asserts it through evaluated MSBuild.
- **Version 0.1.0**, declared only in `src/Lodestar.Conformal/Version.props` as `$(LodestarConformalVersion)`. Release tag would be `Lodestar.Conformal/v0.1.0`.
- **Warnings are errors.** `SonarAnalyzer.CSharp` at `AnalysisMode=All`, `AnalysisLevel` 10.0. A suppression carries a reason a reviewer can disagree with; "too noisy" is not one.
- **Comments:** why, not what; two lines inline, eight lines of prose in XML documentation; past that a `long-comment:` marker with its reason, which must stay exceptional. `tools/check_comment_length.py` counts.
- **The quantile rule, verbatim:** `k = ceil((n + 1) * (1 - alpha))`, `q` is the `k`-th smallest calibration score, 1-based.
- **Oracle replay tolerance is `1e-9`.** Corpora are generated from a working directory that is **not** an ancestor of the checkout, and the generator's **own** exit code is read — never a pipeline's.
- **The empty prediction set is reproduced, not repaired.** A LAC set with no class above `1 - q` stays empty.
- **`k > n` returns `double.PositiveInfinity`** — an infinite interval, a full label set. This **diverges from MAPIE** and Task 2 records why.
- **Exchangeability** is a named section in the guide, a sentence in the package `<Description>`, and a line in the XML documentation of every member returning a quantile or an interval.
- Everything written in English — code, comments, ADRs, commit messages.
- Samples print every number through `Inv.*` so the run reads the same in every culture (`tools/check_sample_culture.py`).

---

### Task 1: The MAPIE oracle corpus

**Files:**

- Modify: `tools/requirements.txt` (add the MAPIE pin)
- Modify: `tools/requirements.lock.txt` (regenerated, not hand-edited)
- Modify: `tools/generate_oracles.py` (new generator + one entry in the `generators` dict)
- Create: `tests/oracles/conformal.json` (generated output, committed)

**Interfaces:**

- Consumes: nothing from earlier tasks.
- Produces: `tests/oracles/conformal.json` with this exact shape, which Task 2 reads:
  - `metadata`: `{"library": "mapie", "version": "1.5.0", "count": <int>}`
  - `quantile`: array of `{"name", "alpha", "scores": [double], "k": int, "quantile": double}`
  - `regression`: array of `{"name", "alpha", "y_calib": [double], "y_calib_pred": [double], "quantile": double, "y_test_pred": [double], "lower": [double], "upper": [double]}`
  - `classification`: array of `{"name", "alpha", "class_count": int, "calib_proba": [double] (row-major), "calib_labels": [int], "scores": [double], "quantile": double, "test_count": int, "test_proba": [double] (row-major), "sets": [int] (row-major, 0/1)}`

- [ ] **Step 1: Pin MAPIE and rebuild the lock**

Append to `tools/requirements.txt`, after the `sentencepiece` block and before the `protobuf` comment:

```text
# mapie — the reference for Lodestar.Conformal's split-conformal corpus (#441).
# Its own dependencies (numpy, scikit-learn, scipy) are already resolved here, so
# the graph grows by one package rather than by a tree.
mapie==1.5.0
```

Then, from the repository root:

```bash
python3.12 -m venv .venv-oracles
.venv-oracles/bin/pip install --upgrade pip pip-tools
.venv-oracles/bin/pip-compile --generate-hashes --output-file tools/requirements.lock.txt tools/requirements.txt
.venv-oracles/bin/pip install --only-binary :all: --require-hashes -r tools/requirements.lock.txt
```

Expected: the lock gains `mapie==1.5.0` and `scipy` (MAPIE's own dependency, if it was not already pinned there). Read the diff — anything else moving means the resolver picked up an unrelated upgrade and must be understood before committing.

- [ ] **Step 2: Add the frozen estimators and the fixtures**

MAPIE with `prefit=True` only ever calls `predict` / `predict_proba`, so the estimator can be a frozen lookup table indexed by `X[:, 0]`. That is deliberate: a real `LinearRegression` would put a BLAS reduction between the fixture and the corpus, and this repository has already had one drift gate turn into a hardware check that way (`stable()`'s docstring, issue #97). With a table there is no reduction at all and the corpus is bit-identical on every machine.

Insert into `tools/generate_oracles.py`, after `generate_regression_deviance` and before `_internal_validity_fixtures`:

```python
# --- Split conformal prediction (#441) ------------------------------------


def _frozen_estimators():
    """MAPIE-compatible estimators whose predictions are a frozen table.

    prefit=True means MAPIE only ever calls predict / predict_proba, so a table
    indexed by X[:, 0] is a complete estimator here -- and unlike a fitted
    regressor it puts no BLAS reduction between the fixture and the corpus.
    """
    import numpy as np
    from sklearn.base import BaseEstimator, ClassifierMixin, RegressorMixin

    class FrozenRegressor(RegressorMixin, BaseEstimator):
        def __init__(self, table=None):
            self.table = table

        def fit(self, X, y=None):
            self.n_features_in_ = 1
            self.is_fitted_ = True
            return self

        def predict(self, X):
            return np.asarray(self.table, dtype=float)[np.asarray(X)[:, 0].astype(int)]

    class FrozenClassifier(ClassifierMixin, BaseEstimator):
        def __init__(self, table=None, n_classes=0):
            self.table = table
            self.n_classes = n_classes

        def fit(self, X, y=None):
            self.classes_ = np.arange(self.n_classes)
            self.n_features_in_ = 1
            self.is_fitted_ = True
            return self

        def predict_proba(self, X):
            return np.asarray(self.table, dtype=float)[np.asarray(X)[:, 0].astype(int)]

        def predict(self, X):
            return self.classes_[self.predict_proba(X).argmax(axis=1)]

    return FrozenRegressor, FrozenClassifier


def _conformal_quantile(scores: list[float], alpha: float) -> tuple[int, float]:
    """The rule under test, computed here so the corpus can assert MAPIE against it."""
    n = len(scores)
    k = math.ceil((n + 1) * (1.0 - alpha))
    if k > n:
        raise ValueError(f"k={k} exceeds n={n}; this corpus holds only cases MAPIE answers")
    return k, sorted(scores)[k - 1]


def _conformal_regression_fixtures() -> list[dict]:
    """Calibration/test splits for the absolute-residual score.

    Every alpha here satisfies MAPIE's own precondition -- 1/alpha and
    1/(1 - alpha) both below the calibration size -- because below it MAPIE
    refuses to answer at all (see decision 0070).
    """
    rng = SeededRandom(SEED + 441)
    y_calib = [round(rng.gauss(10.0, 3.0), 6) for _ in range(30)]
    predicted = [round(v + rng.gauss(0.0, 1.5), 6) for v in y_calib]
    test = [round(rng.gauss(10.0, 3.0), 6) for _ in range(6)]
    return [
        {"name": "thirty calibration points at 90 %",
         "alpha": 0.1, "y_calib": y_calib, "y_calib_pred": predicted, "y_test_pred": test},
        {"name": "the same points at 50 %",
         "alpha": 0.5, "y_calib": y_calib, "y_calib_pred": predicted, "y_test_pred": test},
        # (n + 1)(1 - alpha) = 20 * 0.9 = 18 exactly, so the ceiling must not round up:
        # k = 18, not 19. An implementation carrying an epsilon gets this one wrong.
        {"name": "an exact integer at the ceiling",
         "alpha": 0.1, "y_calib": y_calib[:19], "y_calib_pred": predicted[:19],
         "y_test_pred": test},
        # Repeated scores: the k-th smallest is a position, not a distinct value.
        {"name": "ties in the calibration scores",
         "alpha": 0.2,
         "y_calib": [float(v) for v in range(1, 13)],
         "y_calib_pred": [1.5, 2.5, 2.5, 4.5, 5.5, 5.5, 7.5, 8.5, 8.5, 10.5, 11.5, 11.5],
         "y_test_pred": [0.0, 6.25, 100.0]},
    ]


def generate_conformal() -> dict:
    """Split conformal prediction, against MAPIE 1.5.0 (#441)."""
    import numpy as np
    from mapie.classification import SplitConformalClassifier
    from mapie.regression import SplitConformalRegressor

    frozen_regressor, frozen_classifier = _frozen_estimators()
    quantile_cases: list[dict] = []
    regression_cases: list[dict] = []
    classification_cases: list[dict] = []

    for fx in _conformal_regression_fixtures():
        y_calib = fx["y_calib"]
        calib_pred = fx["y_calib_pred"]
        test_pred = fx["y_test_pred"]
        n = len(y_calib)
        scores = [abs(t - p) for t, p in zip(y_calib, calib_pred)]
        k, q = _conformal_quantile(scores, fx["alpha"])

        # numpy's spelling of the same rule, asserted rather than assumed: the two
        # conventions agreeing is what lets the ceiling form be the one implemented.
        level = (1.0 - fx["alpha"]) * (n + 1) / n
        assert q == float(np.quantile(np.array(scores), level, method="higher")), fx["name"]

        estimator = frozen_regressor(table=np.array(calib_pred + test_pred)).fit(
            np.zeros((1, 1)), np.zeros(1))
        mapie = SplitConformalRegressor(
            estimator=estimator, confidence_level=1.0 - fx["alpha"], prefit=True)
        mapie.conformalize(np.arange(n).reshape(-1, 1), np.array(y_calib))
        _, interval = mapie.predict_interval(
            np.arange(n, n + len(test_pred)).reshape(-1, 1))

        lower = [p - q for p in test_pred]
        upper = [p + q for p in test_pred]
        assert np.allclose(interval[:, 0, 0], lower, rtol=0, atol=1e-12), fx["name"]
        assert np.allclose(interval[:, 1, 0], upper, rtol=0, atol=1e-12), fx["name"]

        quantile_cases.append({
            "name": fx["name"], "alpha": fx["alpha"], "scores": scores, "k": k, "quantile": q})
        regression_cases.append({
            "name": fx["name"], "alpha": fx["alpha"], "y_calib": y_calib,
            "y_calib_pred": calib_pred, "quantile": q, "y_test_pred": test_pred,
            "lower": lower, "upper": upper})

    for fx in _conformal_classification_fixtures():
        classification_cases.append(_conformal_classification_case(
            fx, frozen_classifier, SplitConformalClassifier))
        quantile_cases.append({
            "name": fx["name"], "alpha": fx["alpha"],
            "scores": classification_cases[-1]["scores"],
            "k": classification_cases[-1]["k"],
            "quantile": classification_cases[-1]["quantile"]})

    return {
        "metadata": {"library": "mapie", "version": version("mapie"),
                     "count": len(regression_cases) + len(classification_cases)},
        "quantile": quantile_cases,
        "regression": regression_cases,
        "classification": classification_cases,
    }
```

- [ ] **Step 3: Add the classification half**

The LAC fixtures need their own builder, because one of them exists only to produce an **empty** prediction set — the edge the spec refuses to repair. Insert immediately before `generate_conformal`:

```python
def _dirichlet_rows(rng: SeededRandom, rows: int, classes: int, sharpness: float) -> list[list[float]]:
    """Probability rows, normalised in pure Python so the corpus holds exact doubles.

    A numpy row-sum would put a reduction between the fixture and the file; the
    values are committed, so they are computed the way they are written.
    """
    out = []
    for _ in range(rows):
        raw = [rng.random() ** sharpness + 1e-3 for _ in range(classes)]
        total = math.fsum(raw)
        out.append([v / total for v in raw])
    return out


def _conformal_classification_fixtures() -> list[dict]:
    """LAC fixtures, including the one whose point is an empty prediction set."""
    rng = SeededRandom(SEED + 4410)
    flat = _dirichlet_rows(rng, 88, 4, 1.0)
    confident = _dirichlet_rows(rng, 64, 3, 6.0)
    # A deliberately flat test row under a confident model: no class clears 1 - q,
    # and LAC's answer is the empty set rather than the arg-max.
    confident[-1] = [0.34, 0.33, 0.33]
    binary = _dirichlet_rows(rng, 40, 2, 2.0)
    return [
        {"name": "eighty calibration points, four classes, at 80 %",
         "alpha": 0.2, "class_count": 4, "calib": 80, "proba": flat, "sharp": False},
        {"name": "a confident model, where a flat row gets an empty set",
         "alpha": 0.2, "class_count": 3, "calib": 60, "proba": confident, "sharp": True},
        {"name": "two classes at 75 %",
         "alpha": 0.25, "class_count": 2, "calib": 36, "proba": binary, "sharp": True},
    ]


def _conformal_classification_case(fx: dict, frozen_classifier, split_classifier) -> dict:
    """One LAC case: MAPIE's prediction sets, asserted against the threshold rule."""
    import numpy as np

    rng = SeededRandom(SEED + 44100)
    proba = fx["proba"]
    classes = fx["class_count"]
    n = fx["calib"]
    # Labels drawn from each row's own distribution, so the model is calibrated and
    # 1 - p(true) is small: that is what pushes the threshold high enough for the
    # flat test row above to clear nothing at all.
    labels = []
    for row in proba[:n]:
        draw = rng.random()
        cumulative = 0.0
        chosen = classes - 1
        for index, p in enumerate(row):
            cumulative += p
            if draw < cumulative:
                chosen = index
                break
        labels.append(chosen)

    scores = [1.0 - proba[i][labels[i]] for i in range(n)]
    k, q = _conformal_quantile(scores, fx["alpha"])

    estimator = frozen_classifier(table=np.array(proba), n_classes=classes).fit(
        np.zeros((1, 1)), np.zeros(1, dtype=int))
    mapie = split_classifier(
        estimator=estimator, confidence_level=1.0 - fx["alpha"],
        conformity_score="lac", prefit=True)
    mapie.conformalize(np.arange(n).reshape(-1, 1), np.array(labels))
    _, sets = mapie.predict_set(np.arange(n, len(proba)).reshape(-1, 1))

    test = proba[n:]
    mine = [[1 if p >= 1.0 - q else 0 for p in row] for row in test]
    assert np.array_equal(sets[:, :, 0].astype(int), np.array(mine)), fx["name"]
    if fx["sharp"]:
        assert any(sum(row) == 0 for row in mine), f"{fx['name']}: no empty set to freeze"

    return {
        "name": fx["name"], "alpha": fx["alpha"], "class_count": classes,
        "calib_proba": [p for row in proba[:n] for p in row],
        "calib_labels": labels,
        "scores": scores, "k": k, "quantile": q,
        "test_count": len(test),
        "test_proba": [p for row in test for p in row],
        "sets": [flag for row in mine for flag in row],
    }
```

- [ ] **Step 4: Register the corpus**

In `main()`'s `generators` dict, after the `"regression_deviance.json"` entry:

```python
        "conformal.json": generate_conformal,
```

- [ ] **Step 5: Generate, reading the generator's own exit code**

The repository lives under `/home/user`, so `/tmp` is a neutral directory here — it is not an ancestor of the checkout, which is the only property `nltk`'s import guard cares about.

```bash
cd /tmp && PYTHONSAFEPATH=1 /home/user/lodestar/.venv-oracles/bin/python \
  /home/user/lodestar/tools/generate_oracles.py
echo "generator exit: $?"
```

Expected: `conformal.json: 7 cases -> .../tests/oracles/conformal.json`, exit 0, and **no other corpus changing** (`git status --short tests/oracles` names one file). A failed assertion inside `generate_conformal` is the corpus refusing to freeze a value MAPIE does not agree with — read it, do not weaken it. Never pipe this command into `tail`: the shell would report `tail`'s status.

- [ ] **Step 6: Verify the corpus carries the two edges it exists for**

```bash
cd /home/user/lodestar && .venv-oracles/bin/python - <<'PY'
import json, pathlib
d = json.loads(pathlib.Path("tests/oracles/conformal.json").read_text())
print("quantile cases:", len(d["quantile"]))
for c in d["classification"]:
    rows = [c["sets"][i:i + c["class_count"]]
            for i in range(0, len(c["sets"]), c["class_count"])]
    print(c["name"], "k=", c["k"], "empty rows:", sum(1 for r in rows if not any(r)))
for c in d["regression"]:
    print(c["name"], "k rule ->", c["quantile"])
PY
```

Expected: at least one classification case reporting `empty rows: 1`, and the exact-integer regression case present.

- [ ] **Step 7: Commit**

```bash
git add tools/requirements.txt tools/requirements.lock.txt tools/generate_oracles.py tests/oracles/conformal.json
git commit -m "Freeze the split-conformal corpus from MAPIE 1.5.0"
```

---

### Task 2: The k > n divergence, recorded

**Files:**

- Create: `docs/decisions/0070-k-greater-than-n-returns-an-infinite-interval.md`
- Modify: `docs/decisions/README.md` (the index, and the ADR count it spells **twice**)
- Modify: `docs/superpowers/specs/2026-09-01_0441_lodestar-conformal-split-conformal-prediction.md` (record what MAPIE was measured to do at that edge)

**Interfaces:**

- Consumes: the measurements below, already taken against MAPIE 1.5.0.
- Produces: the citation `docs/decisions/0070-...` that Tasks 4, 5 and 6 link from the XML documentation, the reference page and the guide.

The measurements, taken with a frozen estimator, 9 calibration points and `alpha = 0.05` (so `k = ceil(10 * 0.95) = 10 > 9`):

| call | MAPIE 1.5.0 |
| --- | --- |
| `SplitConformalRegressor.predict_interval(X)` | raises `ValueError`: *"Number of samples of the score is too low, 1/confidence_level and 1/(1 - confidence_level) must be lower than the number of samples."* |
| `SplitConformalRegressor.predict_interval(X, allow_infinite_bounds=True)` | returns a **finite** interval of half-width `0.5` — the largest calibration score |
| `SplitConformalClassifier.predict_set(X)` | raises the same `ValueError`; there is no flag |

- [ ] **Step 1: Write the ADR**

```bash
cat > docs/decisions/0070-k-greater-than-n-returns-an-infinite-interval.md <<'ADR'
# 0070 — When the calibration set is too small, the answer is infinite, not the widest score

**Status:** accepted · **Date:** 2026-09-01 · **Issue:** [#441](https://github.com/CyrilB1531/lodestar/issues/441)

## Context

Split conformal prediction reads off the `k`-th smallest calibration score, with
`k = ceil((n + 1) * (1 - alpha))`. When `alpha < 1 / (n + 1)` that index does not
exist: the level asks for a score beyond the calibration set. `Lodestar.Conformal`
reproduces MAPIE 1.5.0 everywhere else, so what it does here needs a reason.

Measured against MAPIE 1.5.0, with nine calibration points at `alpha = 0.05`:

- `SplitConformalRegressor.predict_interval` raises `ValueError` — *"Number of
  samples of the score is too low, 1/confidence_level and 1/(1 - confidence_level)
  must be lower than the number of samples."*
- The same call with `allow_infinite_bounds=True` returns a **finite** interval,
  half-width `0.5`, which is the largest calibration score.
- `SplitConformalClassifier.predict_set` raises the same error, and has no flag.

## Decision

`SplitConformal.Quantile` returns `double.PositiveInfinity`. `Interval` carries it
to `(-inf, +inf)` and `PredictionSet` to the full label set. Neither throws.

## Consequences

The interval a caller gets back is useless and says so. That is the point: it is
the only answer at that level with the coverage the type's name promises.

Clamping to the largest score — MAPIE's answer under `allow_infinite_bounds` — is
narrower than the level asked for, so it **under-covers**, silently, in exactly the
regime where the calibration set was already too small to notice. A package whose
front page promises a finite-sample guarantee cannot ship that as its edge case.

Raising, MAPIE's default, is defensible and was rejected for a smaller reason: the
quantile is a value this API hands back to the caller rather than holding, so an
infinity flows through arithmetic they can see, and `double.IsInfinity(q)` is a
cheaper thing to check than a `try`/`catch` around a calibration step.

The cost is that a caller who never inspects `q` gets an infinite interval instead
of an exception. The XML documentation of all three members states the edge, the
guide's *Exchangeability* section states it beside the other way the guarantee can
be worth nothing, and the reference pages carry it under **Remarks**.
ADR
```

- [ ] **Step 2: Index it**

Add the row to `docs/decisions/README.md`'s table, in number order:

```markdown
| [0070](0070-k-greater-than-n-returns-an-infinite-interval.md) | When the calibration set is too small, the answer is infinite, not the widest score | accepted |
```

Then find **both** places the file states how many ADRs there are and raise each by one:

```bash
grep -n "69\|sixty-nine\|decisions" docs/decisions/README.md | head -20
```

Match the surrounding sentence's own style — one of the two spells the count in words.

- [ ] **Step 3: Verify the ADR gate**

```bash
python3 tools/check_adr_immutable.py
npx markdownlint-cli2 "docs/**/*.md"
```

Expected: both clean. `check_adr_immutable.py` compares against `origin/main`, so a **new** file is fine; an edit to an accepted one is not.

- [ ] **Step 4: Correct the spec's account of the edge**

The spec asserts the trivial answer is right without saying what MAPIE does, which reads as agreement. Replace its `k > n` paragraph (the one beginning **"`k > n` is the other edge"**) with:

```markdown
**`k > n` is the other edge**, and it is the one place this package does not
reproduce MAPIE. When `alpha < 1 / (n + 1)` the rule asks for a score that does not
exist. Measured: MAPIE 1.5.0 raises `ValueError` in both halves, and under
`allow_infinite_bounds=True` its regressor returns a *finite* interval whose
half-width is the largest calibration score. That last answer is narrower than the
level asked for — it under-covers — so `Quantile` returns
`double.PositiveInfinity` instead, and `Interval` and `PredictionSet` carry it
through to the whole line and the full label set.
[Decision 0070](../../decisions/0070-k-greater-than-n-returns-an-infinite-interval.md)
records the three measurements and why raising was the runner-up.
```

- [ ] **Step 5: Commit**

```bash
git add docs/decisions/0070-k-greater-than-n-returns-an-infinite-interval.md docs/decisions/README.md docs/superpowers/specs/2026-09-01_0441_lodestar-conformal-split-conformal-prediction.md
git commit -m "Record the k > n divergence from MAPIE as decision 0070"
```

---

### Task 3: The two test projects

**Files:**

- Create: `tests/Lodestar.Conformal.Tests/Lodestar.Conformal.Tests.csproj`
- Create: `tests/Lodestar.Conformal.Tests/OracleLoader.cs`
- Create: `tests/Lodestar.Conformal.Tests/ConformalCorpus.cs`
- Create: `tests/Lodestar.Conformal.Tests/SplitConformalOracleTests.cs`
- Create: `tests/Lodestar.Conformal.Tests/SplitConformalEdgeTests.cs`
- Create: `tests/Lodestar.Conformal.NetStandard.Tests/Lodestar.Conformal.NetStandard.Tests.csproj`
- Modify: `Lodestar.slnx`

**Interfaces:**

- Consumes: `tests/oracles/conformal.json` from Task 1; decision 0070 from Task 2 as the behaviour the edge tests assert.
- Produces: two runnable suites. `ConformalCorpus.Tolerance` is `1e-9`. Both csproj files already carry the `docs/reference/conformal/**` copy items Task 5's pages land in, so Task 5 adds pages and not build plumbing.

- [ ] **Step 1: Write the net10.0 test project**

```bash
mkdir -p tests/Lodestar.Conformal.Tests && cat > tests/Lodestar.Conformal.Tests/Lodestar.Conformal.Tests.csproj <<'PROJ'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Lodestar.Conformal/Lodestar.Conformal.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="../oracles/**/*.json" CopyToOutputDirectory="PreserveNewest" LinkBase="oracles" />
  </ItemGroup>

  <!-- The gate's engine is shared by every package's suite, so it is linked rather
       than copied; the pages and the map are read from the output directory, the
       way the oracle corpora already are. -->
  <ItemGroup>
    <Compile Include="../Shared/ReferenceDocumentation.cs" Link="Documentation/ReferenceDocumentation.cs" />
    <None Include="../../docs/reference/conformal/**/*.md" CopyToOutputDirectory="PreserveNewest"
          LinkBase="reference" />
    <None Include="../../docs/wiki-map.json" CopyToOutputDirectory="PreserveNewest" />
    <None Include="../../docs/**/*.md" Exclude="../../docs/superpowers/**"
          CopyToOutputDirectory="PreserveNewest" LinkBase="docs" />
  </ItemGroup>

</Project>
PROJ
```

- [ ] **Step 2: Write the netstandard2.0 mirror**

```bash
mkdir -p tests/Lodestar.Conformal.NetStandard.Tests && cat > tests/Lodestar.Conformal.NetStandard.Tests/Lodestar.Conformal.NetStandard.Tests.csproj <<'PROJ'
<Project Sdk="Microsoft.NET.Sdk">

  <!--
    Replays the entire Lodestar.Conformal.Tests suite against the *netstandard2.0*
    build of the library, instead of the net10.0 one the original project
    references.

    netstandard2.0 is a contract, not a runtime, so the tests cannot run *on* it.
    They run on net10.0 — identical host — and only the assembly under test
    changes. Without this, the assemblies shipped to .NET Framework, Mono and
    Unity consumers are compile-verified but never executed.

    The test sources are linked, never copied: one suite, two builds.
  -->

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <AssemblyName>Lodestar.Conformal.NetStandard.Tests</AssemblyName>
    <RootNamespace>Lodestar.Conformal.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <!-- SetTargetFramework is what pins the reference to the netstandard2.0 build. -->
  <ItemGroup>
    <ProjectReference Include="../../src/Lodestar.Conformal/Lodestar.Conformal.csproj"
                      SetTargetFramework="TargetFramework=netstandard2.0" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="../Lodestar.Conformal.Tests/**/*.cs"
             Exclude="../Lodestar.Conformal.Tests/bin/**;../Lodestar.Conformal.Tests/obj/**"
             Link="%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <None Include="../oracles/**/*.json" CopyToOutputDirectory="PreserveNewest" LinkBase="oracles" />
  </ItemGroup>

  <!-- The gate's engine is shared by every package's suite, so it is linked rather
       than copied; the pages and the map are read from the output directory, the
       way the oracle corpora already are. -->
  <ItemGroup>
    <Compile Include="../Shared/ReferenceDocumentation.cs" Link="Documentation/ReferenceDocumentation.cs" />
    <None Include="../../docs/reference/conformal/**/*.md" CopyToOutputDirectory="PreserveNewest"
          LinkBase="reference" />
    <None Include="../../docs/wiki-map.json" CopyToOutputDirectory="PreserveNewest" />
    <None Include="../../docs/**/*.md" Exclude="../../docs/superpowers/**"
          CopyToOutputDirectory="PreserveNewest" LinkBase="docs" />
  </ItemGroup>

</Project>
PROJ
```

- [ ] **Step 3: Put all three projects in the solution**

Edit `Lodestar.slnx`: add `<Project Path="src/Lodestar.Conformal/Lodestar.Conformal.csproj" />` to the `/src/` folder, and both test projects to `/tests/`. Keep the existing ordering style of each folder.

- [ ] **Step 4: Write the loader and the corpus accessor**

```bash
cat > tests/Lodestar.Conformal.Tests/OracleLoader.cs <<'CS'
using System.Text.Json;

namespace Lodestar.Conformal.Tests;

/// <summary>Minimal loader for the committed oracle JSON files.</summary>
internal static class OracleLoader
{
    public static JsonDocument Load(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "oracles", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Oracle '{fileName}' not found at '{path}'. Run tools/generate_oracles.py.", path);
        }
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
CS
cat > tests/Lodestar.Conformal.Tests/ConformalCorpus.cs <<'CS'
using System.Text.Json;
using Xunit;

namespace Lodestar.Conformal.Tests;

/// <summary>Shared access to the frozen MAPIE corpus.</summary>
internal static class ConformalCorpus
{
    /// <summary>The tolerance the whole repository uses for oracle replay.</summary>
    public const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("conformal.json");

    public static IReadOnlyList<JsonElement> Section(string name) =>
        [.. Document.RootElement.GetProperty(name).EnumerateArray()];

    /// <summary>One theory row per case, so a failure names the case that failed.</summary>
    public static TheoryData<int> Indices(string name)
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Section(name).Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    public static string Name(JsonElement c) => c.GetProperty("name").GetString()!;

    public static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    public static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    /// <summary>A row of a row-major block, as the span the API takes.</summary>
    public static ReadOnlySpan<double> Row(double[] flat, int index, int width) =>
        flat.AsSpan(index * width, width);
}
CS
```

- [ ] **Step 5: Write the failing oracle tests**

```bash
cat > tests/Lodestar.Conformal.Tests/SplitConformalOracleTests.cs <<'CS'
using System.Text.Json;
using Xunit;

namespace Lodestar.Conformal.Tests;

/// <summary>Every member replayed against the frozen MAPIE 1.5.0 corpus.</summary>
public sealed class SplitConformalOracleTests
{
    public static TheoryData<int> QuantileCases() => ConformalCorpus.Indices("quantile");

    public static TheoryData<int> RegressionCases() => ConformalCorpus.Indices("regression");

    public static TheoryData<int> ClassificationCases() => ConformalCorpus.Indices("classification");

    [Theory]
    [MemberData(nameof(QuantileCases))]
    public void Quantile_matches_the_k_th_smallest_score(int index)
    {
        JsonElement c = ConformalCorpus.Section("quantile")[index];
        double[] scores = ConformalCorpus.Doubles(c, "scores");

        Assert.Equal(c.GetProperty("quantile").GetDouble(),
                     SplitConformal.Quantile(scores, c.GetProperty("alpha").GetDouble()),
                     ConformalCorpus.Tolerance);
    }

    [Theory]
    [MemberData(nameof(QuantileCases))]
    public void Quantile_leaves_the_caller_s_scores_in_order(int index)
    {
        JsonElement c = ConformalCorpus.Section("quantile")[index];
        double[] scores = ConformalCorpus.Doubles(c, "scores");
        double[] untouched = [.. scores];

        _ = SplitConformal.Quantile(scores, c.GetProperty("alpha").GetDouble());

        Assert.Equal(untouched, scores);
    }

    [Theory]
    [MemberData(nameof(RegressionCases))]
    public void Regression_intervals_match_MAPIE(int index)
    {
        JsonElement c = ConformalCorpus.Section("regression")[index];
        double[] residuals = SplitConformal.AbsoluteResiduals(
            ConformalCorpus.Doubles(c, "y_calib"), ConformalCorpus.Doubles(c, "y_calib_pred"));
        double quantile = SplitConformal.Quantile(residuals, c.GetProperty("alpha").GetDouble());

        Assert.Equal(c.GetProperty("quantile").GetDouble(), quantile, ConformalCorpus.Tolerance);

        double[] predictions = ConformalCorpus.Doubles(c, "y_test_pred");
        double[] lower = ConformalCorpus.Doubles(c, "lower");
        double[] upper = ConformalCorpus.Doubles(c, "upper");
        for (int i = 0; i < predictions.Length; i++)
        {
            (double Lower, double Upper) interval = SplitConformal.Interval(predictions[i], quantile);
            Assert.Equal(lower[i], interval.Lower, ConformalCorpus.Tolerance);
            Assert.Equal(upper[i], interval.Upper, ConformalCorpus.Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(ClassificationCases))]
    public void Prediction_sets_match_MAPIE(int index)
    {
        JsonElement c = ConformalCorpus.Section("classification")[index];
        int classes = c.GetProperty("class_count").GetInt32();
        double[] calibration = ConformalCorpus.Doubles(c, "calib_proba");
        int[] labels = ConformalCorpus.Ints(c, "calib_labels");

        double[] scores = SplitConformal.LeastAmbiguousScores(calibration, labels, classes);
        Assert.Equal(ConformalCorpus.Doubles(c, "scores"), scores);

        double quantile = SplitConformal.Quantile(scores, c.GetProperty("alpha").GetDouble());
        Assert.Equal(c.GetProperty("quantile").GetDouble(), quantile, ConformalCorpus.Tolerance);

        double[] test = ConformalCorpus.Doubles(c, "test_proba");
        int[] expected = ConformalCorpus.Ints(c, "sets");
        for (int row = 0; row < c.GetProperty("test_count").GetInt32(); row++)
        {
            bool[] set = SplitConformal.PredictionSet(
                ConformalCorpus.Row(test, row, classes), quantile);
            for (int j = 0; j < classes; j++)
            {
                Assert.Equal(expected[(row * classes) + j] != 0, set[j]);
            }
        }
    }

    /// <summary>
    /// The corpus exists partly to prove this case is real: LAC returns nothing at all
    /// when no class clears the threshold, and substituting the arg-max there would
    /// return a set with no coverage guarantee under a name that promises one.
    /// </summary>
    [Fact]
    public void An_empty_prediction_set_is_reproduced_rather_than_repaired()
    {
        int empty = 0;
        foreach (JsonElement c in ConformalCorpus.Section("classification"))
        {
            int classes = c.GetProperty("class_count").GetInt32();
            double quantile = c.GetProperty("quantile").GetDouble();
            double[] test = ConformalCorpus.Doubles(c, "test_proba");
            for (int row = 0; row < c.GetProperty("test_count").GetInt32(); row++)
            {
                bool[] set = SplitConformal.PredictionSet(
                    ConformalCorpus.Row(test, row, classes), quantile);
                if (Array.TrueForAll(set, included => !included))
                {
                    empty++;
                }
            }
        }

        Assert.True(empty > 0, "the corpus no longer carries an empty prediction set");
    }
}
CS
```

- [ ] **Step 6: Write the edge and validation tests**

```bash
cat > tests/Lodestar.Conformal.Tests/SplitConformalEdgeTests.cs <<'CS'
using Xunit;

namespace Lodestar.Conformal.Tests;

/// <summary>
/// The edges no oracle can carry: MAPIE raises where these return, so what they
/// assert is decision 0070 rather than a frozen value.
/// </summary>
public sealed class SplitConformalEdgeTests
{
    // k = ceil(10 * 0.95) = 10 > 9: the level asks for a score the set does not hold.
    private static readonly double[] NineScores = [0.2, 0.1, 0.4, 0.3, 0.5, 0.1, 0.4, 0.3, 0.1];

    [Fact]
    public void A_calibration_set_too_small_for_the_level_yields_an_infinite_quantile() =>
        Assert.Equal(double.PositiveInfinity, SplitConformal.Quantile(NineScores, 0.05));

    [Fact]
    public void An_infinite_quantile_yields_the_whole_line()
    {
        (double Lower, double Upper) interval =
            SplitConformal.Interval(4.0, double.PositiveInfinity);

        Assert.Equal(double.NegativeInfinity, interval.Lower);
        Assert.Equal(double.PositiveInfinity, interval.Upper);
    }

    [Fact]
    public void An_infinite_quantile_yields_the_full_label_set()
    {
        bool[] set = SplitConformal.PredictionSet([0.4, 0.35, 0.25], double.PositiveInfinity);

        Assert.Equal([true, true, true], set);
    }

    [Fact]
    public void The_ceiling_does_not_round_an_exact_integer_up()
    {
        // (n + 1)(1 - alpha) = 20 * 0.9 = 18 exactly, so k is 18: the 18th smallest.
        double[] scores = [.. Enumerable.Range(1, 19).Select(v => (double)v)];

        Assert.Equal(18.0, SplitConformal.Quantile(scores, 0.1));
    }

    [Fact]
    public void One_calibration_score_is_enough_at_a_level_it_can_answer() =>
        Assert.Equal(7.0, SplitConformal.Quantile([7.0], 0.5));

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void A_level_outside_the_open_unit_interval_is_refused(double alpha) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SplitConformal.Quantile(NineScores, alpha));

    [Fact]
    public void An_empty_calibration_set_is_refused() =>
        Assert.Throws<ArgumentException>(() => SplitConformal.Quantile([], 0.1));

    [Fact]
    public void Residuals_refuse_spans_of_different_lengths() =>
        Assert.Throws<ArgumentException>(
            () => SplitConformal.AbsoluteResiduals([1.0, 2.0], [1.0]));

    [Fact]
    public void Residuals_are_the_absolute_difference() =>
        Assert.Equal([1.0, 0.5, 0.0], SplitConformal.AbsoluteResiduals(
            [1.0, 2.0, 3.0], [2.0, 1.5, 3.0]));

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void A_quantile_that_is_not_a_score_is_refused(double quantile)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SplitConformal.Interval(1.0, quantile));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SplitConformal.PredictionSet([0.5, 0.5], quantile));
    }

    [Fact]
    public void A_class_count_that_is_not_positive_is_refused() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SplitConformal.LeastAmbiguousScores([0.5, 0.5], [0], 0));

    [Fact]
    public void A_probability_block_that_does_not_fit_the_labels_is_refused() =>
        Assert.Throws<ArgumentException>(
            () => SplitConformal.LeastAmbiguousScores([0.5, 0.5, 0.5], [0, 1], 2));

    [Fact]
    public void A_label_outside_the_class_range_is_refused() =>
        Assert.Throws<ArgumentException>(
            () => SplitConformal.LeastAmbiguousScores([0.5, 0.5], [2], 2));

    [Fact]
    public void A_class_exactly_on_the_threshold_is_included()
    {
        // 1 - q is the boundary and LAC includes it: p >= 1 - q, not p > 1 - q.
        bool[] set = SplitConformal.PredictionSet([0.25, 0.75], 0.75);

        Assert.Equal([true, true], set);
    }
}
CS
```

- [ ] **Step 7: Run both suites**

```bash
dotnet test Lodestar.slnx -c Release 2>&1 | tail -20
```

Expected: two new assemblies in the run, and the conformal tests appearing **twice** — once per target. Read the test **count**, not the colour: a suite that discovered nothing exits zero.

- [ ] **Step 8: Commit**

```bash
git add tests/Lodestar.Conformal.Tests tests/Lodestar.Conformal.NetStandard.Tests Lodestar.slnx
git commit -m "Replay the split-conformal corpus against both target frameworks"
```

---

### Task 4: The packaging graph and the CI wiring

**Files:**

- Modify: `tools/check_nuspec_dependencies.py`
- Modify: `.github/workflows/ci.yml` (four `for proj in …` loops)
- Modify: `.github/workflows/sonarcloud.yml` (one loop)
- Modify: `.github/workflows/release.yml` (the package allow-list)
- Modify: `.github/workflows/release-nuget-org.yml` (the matrix)
- Modify: `.github/workflows/wiki.yml` (the package loop)
- Modify: `tools/check_sample_coverage.py` (`CONVERTED`)

**Interfaces:**

- Consumes: the package id `Lodestar.Conformal` and version `0.1.0` from `src/Lodestar.Conformal/Version.props`.
- Produces: `./artifacts/Lodestar.Conformal.0.1.0.nupkg` on every packing step, which Task 5's sample and the doc-snippets project consume.

- [ ] **Step 1: Declare the intended dependency graph**

In `tools/check_nuspec_dependencies.py`, after the `METRICS = "Lodestar.Metrics"` line:

```python
CONFORMAL = "Lodestar.Conformal"
```

and after the `METRICS` block inside `EXPECTED`:

```python
    CONFORMAL: {
        # Nothing on net10.0, only the polyfills on netstandard2.0: split conformal
        # prediction is arithmetic over spans, with no model and nothing to serialise.
        NET: {},
        NETSTANDARD: {**POLYFILLS},
    },
```

- [ ] **Step 2: Add the package to every loop that names the four**

```bash
grep -rn "src/Lodestar.Metrics;\|src/Lodestar.Metrics " .github/workflows/*.yml
grep -rn "Lodestar.Metrics" .github/workflows/*.yml
```

Each hit is one of six edits:

- `.github/workflows/ci.yml` lines ~141, ~152, ~216, ~251 — append a space and `src/Lodestar.Conformal` to `for proj in src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy src/Lodestar.Metrics; do`
- `.github/workflows/sonarcloud.yml` line ~193 — the same append
- `.github/workflows/wiki.yml` line ~55 — `for package in Lodestar.Text Lodestar.Embeddings Lodestar.Fuzzy Lodestar.Metrics Lodestar.Conformal; do`
- `.github/workflows/release.yml` line ~56 — `Lodestar.Text|Lodestar.Embeddings|Lodestar.Fuzzy|Lodestar.Metrics|Lodestar.Conformal) ;;`
- `.github/workflows/release-nuget-org.yml` line ~20 — add `- Lodestar.Conformal` to the matrix list, at the indentation the others use

- [ ] **Step 3: Make the sample-coverage gate enforce this package from birth**

`Lodestar.Conformal` ships exactly one public class, so it is born with its per-class sample rather than waiting for a lot to be split. In `tools/check_sample_coverage.py`:

```python
CONVERTED = ["Lodestar.Text", "Lodestar.Conformal"]
WAITING = ["Lodestar.Fuzzy", "Lodestar.Embeddings", "Lodestar.Metrics"]
```

- [ ] **Step 4: Pack and check the graph**

```bash
rm -rf ./artifacts
for p in src/Lodestar.Text src/Lodestar.Embeddings src/Lodestar.Fuzzy src/Lodestar.Metrics src/Lodestar.Conformal; do
  dotnet pack "$p" -c Release -o ./artifacts || break
done
python3 tools/check_nuspec_dependencies.py ./artifacts --require-all
```

Expected: five packages, and the checker silent. A `System.Text.Json` appearing under `Lodestar.Conformal` means something pulled persistence in and must be removed, not added to `EXPECTED`.

- [ ] **Step 5: Commit**

```bash
git add tools/check_nuspec_dependencies.py tools/check_sample_coverage.py .github/workflows
git commit -m "Pack, release and document Lodestar.Conformal alongside the other four"
```

---

### Task 5: The sample, which is the packaging gate

**Files:**

- Create: `samples/Lodestar.Sample/SplitConformalSample.cs`
- Modify: `samples/Lodestar.Sample/Program.cs`
- Modify: `samples/Lodestar.Sample/PackagingGate.cs`
- Modify: `samples/Lodestar.Sample/Lodestar.Sample.csproj`
- Modify: `samples/Lodestar.DocSnippets/Lodestar.DocSnippets.csproj`

**Interfaces:**

- Consumes: `./artifacts/Lodestar.Conformal.0.1.0.nupkg` from Task 4.
- Produces: a member reference to **every** public member of `SplitConformal` — `Quantile`, `AbsoluteResiduals`, `Interval`, `LeastAmbiguousScores`, `PredictionSet` — which is what `PackagingGate` counts. `SplitConformalSample.Run()` is the entry point `Program.cs` calls.

- [ ] **Step 1: Reference the package from both sample projects**

In `samples/Lodestar.Sample/Lodestar.Sample.csproj` and `samples/Lodestar.DocSnippets/Lodestar.DocSnippets.csproj`, add after the `Lodestar.Metrics` import and reference respectively:

```xml
  <Import Project="../../src/Lodestar.Conformal/Version.props" />
```

```xml
    <PackageReference Include="Lodestar.Conformal" Version="$(LodestarConformalVersion)" />
```

- [ ] **Step 2: Write the sample**

```bash
cat > samples/Lodestar.Sample/SplitConformalSample.cs <<'CS'
using Lodestar.Conformal;

namespace Lodestar.Sample;

/// <summary>
/// Split conformal prediction — a point prediction becomes an interval, a class
/// becomes a set, and both carry a coverage guarantee that assumes exchangeability.
/// </summary>
internal static class SplitConformalSample
{
    // Nine calibration points: what the model said, and what actually happened.
    private static readonly double[] Observed = [12.1, 9.4, 15.0, 11.2, 8.8, 13.9, 10.5, 14.2, 9.9];
    private static readonly double[] Predicted = [11.8, 10.1, 14.2, 11.9, 8.2, 13.1, 10.9, 13.4, 10.6];

    // Four classes, four calibration rows and their true labels, then one row to score.
    private static readonly double[] CalibrationProbabilities =
    [
        0.80, 0.10, 0.05, 0.05,
        0.10, 0.70, 0.15, 0.05,
        0.05, 0.15, 0.75, 0.05,
        0.20, 0.20, 0.10, 0.50,
    ];
    private static readonly int[] CalibrationLabels = [0, 1, 2, 3];
    private static readonly double[] Unseen = [0.45, 0.30, 0.15, 0.10];

    public static void Run()
    {
        Console.WriteLine("split conformal prediction");

        double[] residuals = SplitConformal.AbsoluteResiduals(Observed, Predicted);
        double quantile = SplitConformal.Quantile(residuals, alpha: 0.2);
        (double Lower, double Upper) interval = SplitConformal.Interval(11.0, quantile);
        Console.WriteLine($"  calibrated quantile   = {Inv.F3(quantile)}");
        Console.WriteLine($"  11.0 becomes          = [{Inv.F3(interval.Lower)}, {Inv.F3(interval.Upper)}]");

        double[] scores = SplitConformal.LeastAmbiguousScores(
            CalibrationProbabilities, CalibrationLabels, classCount: 4);
        double classQuantile = SplitConformal.Quantile(scores, alpha: 0.25);
        bool[] set = SplitConformal.PredictionSet(Unseen, classQuantile);
        Console.WriteLine($"  LAC quantile          = {Inv.F3(classQuantile)}");
        Console.WriteLine($"  prediction set        = {{{string.Join(", ",
            Enumerable.Range(0, set.Length).Where(i => set[i]))}}}");

        // The one thing the numbers above cannot say. See docs/guides/conformal.md.
        Console.WriteLine("  the guarantee holds only if calibration and test data are exchangeable");
        Console.WriteLine();
    }
}
CS
```

- [ ] **Step 3: Call it, and add the assembly to the gate**

In `samples/Lodestar.Sample/Program.cs`: add `using Lodestar.Conformal;` to the using block, a line
`Console.WriteLine($"Lodestar.Conformal : {FrameworkOf(typeof(SplitConformal))}");` after the `Lodestar.Metrics` line, and `SplitConformalSample.Run();` after `Lot6Regression.Run();`.

In `samples/Lodestar.Sample/PackagingGate.cs`: add `using Lodestar.Conformal;` and `typeof(SplitConformal).Assembly,` to the `Assembly[] packaged` initialiser.

- [ ] **Step 4: Run the gate against the packed packages**

`NUGET_PACKAGES` must be isolated or the sample resolves the published packages instead of the working tree (ADR 0009).

```bash
NUGET_PACKAGES="$PWD/.nuget-sample" dotnet run -c Release --project samples/Lodestar.Sample
python3 tools/check_sample_coverage.py
python3 tools/check_sample_culture.py
```

Expected: the run prints `Lodestar.Conformal : .NETCoreApp,Version=v10.0`, ends `OK`, exits 0, and both checkers are silent. `PackagingGate` naming an uncovered member means that member has no call above — add one, never suppress.

- [ ] **Step 5: Commit**

```bash
git add samples/Lodestar.Sample samples/Lodestar.DocSnippets/Lodestar.DocSnippets.csproj
git commit -m "Exercise every SplitConformal member from the packaged sample"
```

---

### Task 6: The reference pages and the map

**Files:**

- Create: `docs/reference/conformal/prediction.md` (the index)
- Create: `docs/reference/conformal/prediction/splitconformal.md`
- Create: `docs/reference/conformal/prediction/splitconformal-quantile.md`
- Create: `docs/reference/conformal/prediction/splitconformal-absoluteresiduals.md`
- Create: `docs/reference/conformal/prediction/splitconformal-interval.md`
- Create: `docs/reference/conformal/prediction/splitconformal-leastambiguousscores.md`
- Create: `docs/reference/conformal/prediction/splitconformal-predictionset.md`
- Modify: `docs/wiki-map.json`

**Interfaces:**

- Consumes: the sample's `SplitConformal` surface from Task 5; decision 0070 from Task 2.
- Produces: the pages the reference gate enforces once `Lodestar.Conformal` is in `covered`. Every ` ```csharp ` fence here is **compiled and executed**, and a trailing `// =>` is an assertion on the printed value.

- [ ] **Step 1: Declare the package in the map**

Add to `docs/wiki-map.json`'s `packages` object, after `Lodestar.Metrics`:

```json
    "Lodestar.Conformal": {
      "wiki": "Conformal",
      "pages": [
        "docs/guides/conformal.md",
        "docs/reference/conformal/*.md",
        "docs/reference/conformal/*/*.md"
      ],
      "covered": {
        "Lodestar.Conformal": "docs/reference/conformal/prediction"
      }
    }
```

- [ ] **Step 2: Write the index page**

```bash
mkdir -p docs/reference/conformal/prediction && cat > docs/reference/conformal/prediction.md <<'MD'
# Split conformal prediction — `Lodestar.Conformal`

A model gives you one number, or one class. This page turns it into an **interval**, or a **set**,
that contains the truth a stated fraction of the time — 90 % of the time, say — and the fraction is
a finite-sample guarantee rather than an asymptotic hope. It costs you a held-out calibration set
and nothing else: no retraining, no distributional assumption about the model, no assumption that
the model is any good. A bad model gets wide intervals, which is the correct answer.

There is one type, [`SplitConformal`](prediction/splitconformal.md), and it is static. The
calibrated quantile is handed back to you rather than kept inside an object, because it is the
number that carries the guarantee and you should be able to look at it.

The whole procedure is three calls:

1. Score the calibration set — [`AbsoluteResiduals`](prediction/splitconformal-absoluteresiduals.md)
   for a regressor, [`LeastAmbiguousScores`](prediction/splitconformal-leastambiguousscores.md) for
   a classifier.
2. Turn the scores into one number — [`Quantile`](prediction/splitconformal-quantile.md).
3. Apply it to a new prediction — [`Interval`](prediction/splitconformal-interval.md) or
   [`PredictionSet`](prediction/splitconformal-predictionset.md).

> **The guarantee assumes exchangeability.** It does not hold for time series, for data with drift,
> or for any split that leaks. The intervals still come out; they simply do not cover, and nothing
> in the output says so. The guide's [*Exchangeability*](../../guides/conformal.md#exchangeability)
> section is the one part of this documentation worth reading before the API.

| Member | What it does |
| --- | --- |
| [`SplitConformal.Quantile`](prediction/splitconformal-quantile.md) | The calibrated quantile: the `k`-th smallest score, with `k = ceil((n + 1)(1 − α))`. |
| [`SplitConformal.AbsoluteResiduals`](prediction/splitconformal-absoluteresiduals.md) | A regressor's calibration scores, `\|y − ŷ\|`. |
| [`SplitConformal.Interval`](prediction/splitconformal-interval.md) | `[ŷ − q, ŷ + q]` around a point prediction. |
| [`SplitConformal.LeastAmbiguousScores`](prediction/splitconformal-leastambiguousscores.md) | A classifier's LAC calibration scores, `1 − p̂(true class)`. |
| [`SplitConformal.PredictionSet`](prediction/splitconformal-predictionset.md) | Every class whose probability clears `1 − q`. Possibly none. |
MD
```

- [ ] **Step 3: Write the five member pages and the type page**

Each follows the layout of `docs/reference/metrics/regression/maxerror-score.md` exactly: an H1 naming the member, one sentence, `<!-- docs-declaration -->`, the signature fence, then **Parameters**, **Returns**, **Exceptions**, **Example**, **Remarks**, **Applies to**, **See also**. The example fence is executed, so every `// =>` must be the value the code actually prints.

`docs/reference/conformal/prediction/splitconformal-quantile.md`:

````markdown
# SplitConformal.Quantile

The calibrated quantile: the score a new point must not exceed to fall inside the prediction.

<!-- docs-declaration -->

```csharp
public static double Quantile(ReadOnlySpan<double> scores, double alpha)
```

**Parameters** — `scores` are the calibration scores, in any order; the span is read, never
modified. `alpha` is the miscoverage level, strictly between 0 and 1: `0.1` asks for 90 % coverage.

**Returns** — `double`, the `k`-th smallest score with `k = ceil((n + 1)(1 − alpha))`, 1-based. Or
`double.PositiveInfinity` when `k` exceeds the number of scores.

**Exceptions** — `ArgumentException` when `scores` is empty; `ArgumentOutOfRangeException` when
`alpha` is `NaN` or outside `(0, 1)`.

**Example** — nine scores at 20 % miscoverage: `k = ceil(10 × 0.8) = 8`, so the answer is the
eighth smallest.

```csharp
using Lodestar.Conformal;

double[] scores = [0.2, 0.1, 0.4, 0.3, 0.5, 0.1, 0.4, 0.3, 0.1];

double q = SplitConformal.Quantile(scores, 0.2);   // => 0.4
```

**Remarks** — this is `numpy.quantile(scores, (1 − alpha)(n + 1)/n, method="higher")`, which is
what MAPIE computes; the ceiling form is implemented because it says what it means. The `+ 1` is
not a rounding fudge: it is the new point counting itself, and it is what makes the coverage
guarantee finite-sample rather than asymptotic.

When `alpha < 1 / (n + 1)` the rule asks for a score the calibration set does not hold, and the
answer is `double.PositiveInfinity` — a trivial prediction with real coverage. MAPIE raises there,
and under `allow_infinite_bounds` returns the largest score instead, which is *narrower* than the
level asked for;
[decision 0070](../../../decisions/0070-k-greater-than-n-returns-an-infinite-interval.md) has the
measurements. If an infinite interval is not acceptable in your call site, test
`double.IsInfinity(q)` and collect more calibration data — there is no third answer.

**The guarantee assumes exchangeability** between the calibration and the test data. See the
guide's [*Exchangeability*](../../../guides/conformal.md#exchangeability) section.

**Applies to** — net10.0, netstandard2.0.

**See also** — `SplitConformal.Interval`, `SplitConformal.PredictionSet`, the
[Python equivalence table](../../../equivalence.md).
````

Write the remaining five to the same shape, with these examples — each verified against the
implementation before it is committed:

- **`splitconformal-absoluteresiduals.md`** — `AbsoluteResiduals([1.0, 2.0, 3.0], [2.0, 1.5, 3.0])`
  → `// => [1, 0.5, 0]`. Remarks: this is MAPIE's `AbsoluteConformityScore`, the default for
  `SplitConformalRegressor`; a signed residual would give a one-sided interval and is not what this
  computes.
- **`splitconformal-interval.md`** — `SplitConformal.Interval(11.0, 0.4)` → `// => (10.6, 11.4)`.
  Remarks: the interval is symmetric because the score is, and it is the same width for every
  prediction — conformal prediction with an absolute residual score buys coverage, not adaptivity.
- **`splitconformal-leastambiguousscores.md`** — four rows of three classes, labels `[0, 1, 2, 0]`,
  showing `1 − p̂(true)`. Remarks: the row-major layout, and that the class order here must be the
  order `PredictionSet` is later given.
- **`splitconformal-predictionset.md`** — one row clearing two classes, and a second example whose
  set is **empty**, with the empty case in the remarks: LAC returns nothing when nothing clears the
  threshold, and substituting the arg-max would return a set with no guarantee under a name that
  promises one.
- **`splitconformal.md`** — the type page: what the class is, the three-call procedure, the
  exchangeability warning, and a table linking the five members.

- [ ] **Step 4: Compile and execute the fences**

```bash
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES="$PWD/.nuget-sample" dotnet run -c Release --project samples/Lodestar.DocSnippets
```

Expected: exit 0. A `// =>` that disagrees with the value the code prints fails here with both
numbers — fix the page, never the assertion marker.

- [ ] **Step 5: Run the reference gate**

```bash
dotnet test Lodestar.slnx -c Release --filter "FullyQualifiedName~ReferenceDocumentation" 2>&1 | tail -20
```

Expected: a non-zero test count, all passing. Two failures are routine here and both are the page's
fault: a signature that drifted from the assembly, and a member **named** on a page without being
linked to its entry at least once on that page.

- [ ] **Step 6: Commit**

```bash
git add docs/reference/conformal docs/wiki-map.json
git commit -m "Document the SplitConformal surface, entry by entry"
```

---

### Task 7: The guide, the equivalence table and the changelog

**Files:**

- Create: `docs/guides/conformal.md`
- Modify: `docs/equivalence.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/migration/README.md`
- Modify: `README.md`
- Modify: `bench/README.md`

**Interfaces:**

- Consumes: everything above. The guide's `## Exchangeability` heading is the anchor Task 6's pages
  and `SplitConformal.cs`'s XML documentation already link to as
  `docs/guides/conformal.md#exchangeability` — the heading text fixes the anchor, so it is not
  free to change.
- Produces: no code.

- [ ] **Step 1: Write the guide**

`docs/guides/conformal.md`, in the shape of `docs/guides/metrics.md`. It must contain, in this
order:

1. **What this buys you** — a worked regression example, end to end, in one executed ` ```csharp `
   fence: a calibration set, `AbsoluteResiduals`, `Quantile` at `alpha = 0.1`, `Interval` on a new
   prediction, printing the width.
2. **The classification half** — the same shape with `LeastAmbiguousScores` and `PredictionSet`,
   including a row whose set is empty and a sentence saying that is the correct answer.
3. **`## Exchangeability`** — a named H2, whose anchor is `#exchangeability`. It says: the
   guarantee holds when calibration and test data are exchangeable; it does **not** hold for time
   series, for data with drift, or for any split that leaks; the intervals still come out, they
   simply do not cover, and nothing in the output says so. It gives the three concrete ways a split
   leaks in practice — calibrating on data the model trained on, splitting a time-ordered dataset
   at random, and calibrating before a deployment that changed the input distribution — and it says
   what to do instead in each case. This section is not a remark at the bottom of the page; issue
   #441's own words are that a conformal package whose front page does not lead with it is worse
   than no package, because it hands people a number they will trust.
4. **When the calibration set is too small** — the infinite quantile, linking decision 0070.
5. **What this is not** — not a calibrated probability, not adaptivity: every interval has the same
   width, and a heteroscedastic problem wants a normalised score this package does not yet ship.

- [ ] **Step 2: Add the equivalence rows**

Append to `docs/equivalence.md`'s table, matching the existing column order exactly:

| Python | C# | Notes |
| --- | --- | --- |
| `mapie.regression.SplitConformalRegressor(prefit=True).conformalize(...)` then `.predict_interval(X)` | `SplitConformal.AbsoluteResiduals` → `SplitConformal.Quantile` → `SplitConformal.Interval` | The quantile is returned rather than held. `k > n` diverges — decision 0070. |
| `mapie.classification.SplitConformalClassifier(conformity_score="lac", prefit=True)` then `.predict_set(X)` | `SplitConformal.LeastAmbiguousScores` → `SplitConformal.Quantile` → `SplitConformal.PredictionSet` | An empty set is reproduced. `k > n` diverges — decision 0070. |
| `numpy.quantile(s, (1 - a) * (n + 1) / n, method="higher")` | `SplitConformal.Quantile(s, a)` | The same value; the ceiling form is the one implemented. |

- [ ] **Step 3: Add the changelog entry**

Under `CHANGELOG.md`'s unreleased heading, a `Lodestar.Conformal 0.1.0` section: first release,
split conformal prediction at MAPIE 1.5.0 parity for regression and LAC classification, no
dependencies, the two documented edges (empty prediction set, `k > n`).

- [ ] **Step 4: Place it in the migration hub and the README**

In `docs/migration/README.md`'s *What Lodestar writes natively* list, add a sixth entry:

```markdown
6. **Split conformal prediction** — MAPIE-parity intervals and prediction sets, with the
   finite-sample coverage guarantee and the exchangeability assumption it rests on. *(done)*
   The survey behind [#441](https://github.com/CyrilB1531/lodestar/issues/441) found **no C#
   implementation at all**, which is why this one is written rather than delegated.
```

In `README.md`'s *What has no .NET equivalent* section, add conformal prediction beside sparse
vectorization, with the same "no .NET implementation exists" framing.

- [ ] **Step 5: State the benchmark position rather than leaving it blank**

Add a short subsection to `bench/README.md`'s section 15: `Lodestar.Conformal` has **no .NET
incumbent** to measure against — that is the survey's finding, not an omission — and the arithmetic
is a sort of the calibration set plus a linear scan, so a benchmark against nothing would report
the sort. Name what would change that: a second .NET implementation appearing, or a normalised
conformity score whose cost is not obvious by inspection.

- [ ] **Step 6: Lint and re-run the executed fences**

```bash
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/extract_doc_snippets.py
NUGET_PACKAGES="$PWD/.nuget-sample" dotnet run -c Release --project samples/Lodestar.DocSnippets
```

- [ ] **Step 7: Commit**

```bash
git add docs/guides/conformal.md docs/equivalence.md CHANGELOG.md docs/migration/README.md README.md bench/README.md
git commit -m "Lead the conformal documentation with the assumption the guarantee rests on"
```

---

## Before the pull request

Per this repository's process the gates run **once**, here, not inside each task's review.

- [ ] `dotnet format Lodestar.slnx --verify-no-changes`
- [ ] `dotnet build Lodestar.slnx -c Release` — both frameworks, warnings are errors
- [ ] `dotnet test Lodestar.slnx -c Release` — read the **count**
- [ ] `python3 tools/check_version_floor.py`
- [ ] `python3 tools/check_machine_paths.py`
- [ ] `python3 tools/check_sample_culture.py`
- [ ] `python3 tools/check_sample_coverage.py`
- [ ] `python3 tools/check_comment_length.py`
- [ ] `python3 tools/check_repeated_literals.py --base origin/main`
- [ ] `python3 tools/check_adr_immutable.py`
- [ ] `python3 tools/check_bench_map.py`
- [ ] `npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"`
- [ ] pack all five, then `python3 tools/check_nuspec_dependencies.py ./artifacts --require-all`
- [ ] the two sample projects, with an isolated `NUGET_PACKAGES`
- [ ] regenerate the corpora from a neutral directory and confirm `git status` shows no drift
- [ ] SonarQube MCP: `toggle_automatic_analysis` off at the start, `analyze_file_list` on everything
      created or modified, then back on
- [ ] PR body carries `Closes #441` — the line, not a mention: #464 stayed open for a week because a
      body rewrite turned one into the other

## Self-Review

**1. Spec coverage.** *Problem* and *What the algorithm is* → Task 1's corpus and Task 3's tests.
*Scope* (two halves, static, quantile returned) → already implemented, exercised by Tasks 3 and 5.
*Exchangeability is half the deliverable* → the `<Description>` is already written, the XML
documentation is already written, Task 7 step 1 item 3 is the named guide section. *Placement*
(core tier, zero dependencies, `check_nuspec_dependencies`) → Task 4. *Testing* (frozen corpus,
MAPIE in requirements, both targets) → Tasks 1 and 3. *Benchmarks* (no incumbent, say so) → Task 7
step 5. The empty set → Task 1 step 3's assertion and Task 3's dedicated test. `k > n` → Task 2,
which corrects the spec rather than implementing what it said, because MAPIE was measured to do
something else.

**2. Placeholders.** Task 6 step 3 describes five pages by their examples and remarks rather than
writing all five out. That is a deliberate compression of one repeated shape whose template is
given in full in the same step, with each page's example values named exactly — not a "similar to
Task N". Everything else carries its code.

**3. Type consistency.** `SplitConformal.Quantile(ReadOnlySpan<double>, double)`,
`AbsoluteResiduals(ReadOnlySpan<double>, ReadOnlySpan<double>) → double[]`,
`Interval(double, double) → (double Lower, double Upper)`,
`LeastAmbiguousScores(ReadOnlySpan<double>, ReadOnlySpan<int>, int) → double[]`,
`PredictionSet(ReadOnlySpan<double>, double) → bool[]` — used identically in Tasks 3, 5, 6 and 7,
and matching `src/Lodestar.Conformal/SplitConformal.cs` as committed. The corpus keys written in
Task 1's *Produces* block are the keys read in Task 3's tests: `scores`, `alpha`, `k`, `quantile`,
`y_calib`, `y_calib_pred`, `y_test_pred`, `lower`, `upper`, `class_count`, `calib_proba`,
`calib_labels`, `test_count`, `test_proba`, `sets`.
