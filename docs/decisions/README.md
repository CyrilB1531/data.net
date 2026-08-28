# Architecture decision records — index

Every accepted or superseded decision behind this codebase, one row per file. The status and
date columns are copied from each file's own `**Status:** … · **Date:** …` line — correct the
ADR to correct this table, not the other way around. The relationships column is a reading: what
a `**Status:**` line does not say but an ADR's body does, either in its own words or in an
`> **update:**` block naming what a later change made stale. An ADR's body is never rewritten to
agree with a later one — see [`0022`](0022-added-token-matching-flags.md) §10 for the convention
a later decision uses instead.

| # | Title | Status | Date | Relationships |
| --- | --- | --- | --- | --- |
| [`0001`](0001-target-framework.md) | Target frameworks: `net10.0` and `netstandard2.0` | accepted | 2026-08-01 (revised 2026-08-04) | — |
| [`0002`](0002-unicode-comparison-unit.md) | Unicode comparison unit | accepted | 2026-08-01 | — |
| [`0003`](0003-provenance-and-licensing.md) | Code provenance and license | accepted | 2026-08-01 | — |
| [`0004`](0004-levenshtein-myers-backlog.md) | Levenshtein: bit-parallel (Myers) optimization | accepted | 2026-08-01 (revised 2026-08-05) | backlog partially cleared by issue #208 (not an ADR — see the two `> **#208 update:**` blocks) |
| [`0005`](0005-hamming-jellyfish-divergence.md) | Accepted divergence from jellyfish on combining marks (Hamming, Jaro) | accepted | 2026-08-01 | — |
| [`0006`](0006-ratcliff-autojunk.md) | Ratcliff-Obershelp: difflib's autojunk heuristic | accepted | 2026-08-01 | — |
| [`0007`](0007-metaphone-scope.md) | Metaphone: validation scope (real words) | accepted | 2026-08-01 | — |
| [`0008`](0008-italian-enza-nltk-divergence.md) | Italian `enza`/`enze`: follow nltk, not the published description | accepted | 2026-08-05 | — |
| [`0009`](0009-sample-consumes-a-local-feed.md) | The sample restores from a local feed, not nuget.org | accepted | 2026-08-05 | amended by [`0012`](0012-per-package-versioning.md) |
| [`0010`](0010-stop-word-list-provenance.md) | Stop-word lists come from Snowball, not from the `nltk` corpus | accepted | 2026-08-05 | — |
| [`0011`](0011-persistence-format.md) | Persisted artifacts are versioned JSON, written with `System.Text.Json` | accepted | 2026-08-05 | — |
| [`0012`](0012-per-package-versioning.md) | Each package versions and releases on its own | accepted | 2026-08-05 | amends [`0009`](0009-sample-consumes-a-local-feed.md) |
| [`0013`](0013-sentencepiece-parity-scope.md) | What `SentencePieceTokenizer` promises, and where it knowingly differs | accepted, superseded in part by `0014` | 2026-08-06 | §1 (the oracle covers the vocabulary, not the stock pipeline) superseded by [`0014`](0014-precompiled-normalizer.md); §2 (the unknown-piece penalty floor) stands |
| [`0014`](0014-precompiled-normalizer.md) | Interpret the `precompiled_charsmap`, do not reimplement the rules | accepted | 2026-08-06 | supersedes [`0013`](0013-sentencepiece-parity-scope.md) §1 |
| [`0015`](0015-sonar-rules-in-the-build.md) | The Sonar rules run in the build, not only after the push | accepted | 2026-08-06 | amended by [`0019`](0019-the-net-analysers-run-in-the-build-too.md) |
| [`0016`](0016-metrics-package-placement.md) | Classification metrics ship as `DataNet.Metrics`, not inside `DataNet.Text` | accepted | 2026-08-06 | — |
| [`0017`](0017-bpe-parity-scope.md) | What `BpeTokenizer` promises, and where it knowingly differs | accepted | 2026-08-09 | — |
| [`0018`](0018-multiclass-roc-auc-parallelism-is-opt-in.md) | Multiclass ROC-AUC parallelism is opt-in, and the caller names the worker count | accepted | 2026-08-10 | — |
| [`0019`](0019-the-net-analysers-run-in-the-build-too.md) | The .NET code-quality analysers run in the build too, and `samples/` is analysed at all | accepted | 2026-08-10 | amends [`0015`](0015-sonar-rules-in-the-build.md) |
| [`0020`](0020-normalize-is-a-projection-not-a-parameter.md) | `normalize=` is a projection, and `ZeroDivision` keeps a default per metric | accepted | 2026-08-10 | — |
| [`0021`](0021-multioutput-is-a-method-not-an-enum.md) | Eleven regression metrics first, and `multioutput` as a method rather than an enum | accepted | 2026-08-10 | — |
| [`0022`](0022-added-token-matching-flags.md) | How an added token matches, and what it costs the round trip | accepted | 2026-08-10 | §10 partially revised by issues #119 and #120 (not ADRs — see the `> **#119 and #120 update:**` block) |
| [`0023`](0023-byte-level-decode-substitutes.md) | Byte-level `Decode` substitutes U+FFFD instead of throwing | accepted | 2026-08-14 | — |
| [`0024`](0024-weighted-median-averages-within-scikit-learns-epsilon.md) | The weighted median averages two order statistics within scikit-learn's epsilon, not exactly at half | accepted | 2026-08-14 | — |
| [`0025`](0025-quickselect-replaces-a-full-sort-for-the-median.md) | Quickselect, with an introselect fallback and a branchless partition, replaces a full sort for the unweighted median | accepted | 2026-08-14 | — |
| [`0026`](0026-r2-and-explainedvariance-split-their-undefined-cases-differently.md) | R² and ExplainedVariance split their undefined cases differently | accepted | 2026-08-14 | — |
| [`0027`](0027-r2-and-explainedvariance-vectorize-only-a-single-output.md) | R²'s and ExplainedVariance's unweighted accumulation vectorizes only for a single output | accepted | 2026-08-14 | — |
| [`0028`](0028-log1p-is-kahans-identity-not-math-log-1-plus-x.md) | `Log1P` is Kahan's identity, not `Math.Log(1 + x)` | accepted | 2026-08-14 | — |
| [`0029`](0029-balanced-accuracy-adjusted-is-left-to-ieee-754-at-the-edge.md) | `BalancedAccuracy`'s `adjusted` is left to IEEE 754 at the one-class edge | accepted | 2026-08-14 | — |
| [`0030`](0030-cohen-kappa-keeps-scikit-learns-expected-matrix-orientation.md) | `CohenKappa` keeps scikit-learn's expected-matrix orientation, and weighting only orders | accepted | 2026-08-14 | — |
| [`0031`](0031-nosamplecorrect-mirrors-numpys-float64-upcast.md) | `NoSampleCorrect` mirrors NumPy's float64 upcast, not requested-label accuracy | accepted | 2026-08-14 | — |
| [`0032`](0032-fbeta-substitutes-tp-predicted-and-support-algebraically.md) | `FScore` substitutes tp/predicted/support algebraically, not via precision and recall | accepted | 2026-08-14 | — |
| [`0033`](0033-compensated-sum-is-neumaiers-variant.md) | `CompensatedSum` is Neumaier's variant, and its SIMD lanes are not bit-identical to it | accepted | 2026-08-14 | — |
| [`0034`](0034-dropout-is-refused-for-want-of-a-user.md) | Distributional proof is admissible, and `dropout` is still refused | accepted | 2026-08-14 | — |
| [`0035`](0035-a-null-pre-split-is-removed-with-invert-not-isolated.md) | A null pre-split drives `Apply` as Removed with invert, not Isolated | accepted | 2026-08-15 | — |
| [`0036`](0036-a-member-may-ship-without-an-oracle-if-it-says-so.md) | A member may ship without an oracle, if the documentation says so | accepted | 2026-08-16 | — |
| [`0037`](0037-the-guards-run-before-the-commit.md) | The guards run before the commit, through `core.hooksPath` and no framework | accepted | 2026-08-18 | extends [`0015`](0015-sonar-rules-in-the-build.md)'s reasoning to the Python guards |
| [`0038`](0038-the-gate-confronts-an-exception-tag-with-the-page-that-documents-it.md) | The gate confronts an exception tag with the page that documents it | accepted | 2026-08-18 | — |
| [`0039`](0039-mutual-information-returns-zero-on-an-empty-input.md) | [`MutualInformation`](../reference/metrics/clustering/mutualinformation.md) returns `0.0` on an empty input; scikit-learn raises | accepted | 2026-08-18 | — |
| [`0040`](0040-a-curve-is-a-sealed-class-per-curve.md) | A curve is a sealed class per curve, not a record and not out-parameters; `drop_intermediate`'s asymmetric defaults are reproduced | accepted | 2026-08-18 | — |
| [`0041`](0041-one-sample-file-per-public-class.md) | One sample file per public class, named after it | accepted | 2026-08-19 | — |
| [`0042`](0042-phonetic-encoders-refuse-a-null-word.md) | Phonetic encoders refuse a `null` word | accepted | 2026-08-20 | — |
| [`0043`](0043-the-equality-table-is-sized-to-the-pattern.md) | The equality table is sized to the pattern, not to Latin-1 | accepted | 2026-08-20 | amends [`0004`](0004-levenshtein-myers-backlog.md) |
| [`0044`](0044-compression-belongs-to-the-caller.md) | Compression belongs to the caller, not to the artifact format | accepted | 2026-08-20 | leaves [`0011`](0011-persistence-format.md) untouched: the artifact on disk does not change |
| [`0045`](0045-a-console-call-carries-its-reason-on-the-line.md) | A `Console` call carries its reason on the line, not in an exemption list | accepted | 2026-08-21 | adds a seventh guard to [`0037`](0037-the-guards-run-before-the-commit.md)'s hook, under the rule it set |
| [`0046`](0046-check-adr-immutable-runs-in-ci-only.md) | `check_adr_immutable.py` runs in CI only, not the pre-commit hook | accepted | 2026-08-20 | — |
| [`0047`](0047-one-gate-per-kernel-not-one-per-alphabet.md) | One bit-parallel gate per kernel, not one per alphabet | accepted | 2026-08-21 | amends [`0043`](0043-the-equality-table-is-sized-to-the-pattern.md) |
| [`0048`](0048-the-gate-depends-on-the-kernel-and-the-alphabet.md) | The bit-parallel gate depends on the kernel *and* the alphabet | accepted | 2026-08-21 | amends [`0047`](0047-one-gate-per-kernel-not-one-per-alphabet.md) |
| [`0049`](0049-two-gates-per-kernel-tested-where-the-width-is-known.md) | Two gates per kernel, the second tested where the width is already known | accepted | 2026-08-21 | amends [`0048`](0048-the-gate-depends-on-the-kernel-and-the-alphabet.md) |
| [`0050`](0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md) | The SentencePiece-BPE lineage stays a BPE model, and metaspace becomes one transform | accepted | 2026-08-21 | amends [`0017`](0017-bpe-parity-scope.md) §3, whose `byte_fallback` refusal and "no path here" for Llama-2 and Mistral v0.1 both fall |
| [`0051`](0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md) | The save path's cost is the buffer, not the encoding | accepted | 2026-08-27 | amends [`0044`](0044-compression-belongs-to-the-caller.md): its `× save` column moves, because the save now allocates half of what it did, and is deliberately left un-restated for want of a measurement under 0044's own conditions — its decision stands strengthened. Extends [`0011`](0011-persistence-format.md)'s `> **#324 update:**` conclusion to the write direction |

## What `accepted` means here

All fifty-one carry `accepted`. None has been rejected or withdrawn — a status this table
would otherwise need a second word for. `0004` read a progress sentence
(`single-word and blocked shipped`) where a status belongs; that sentence is now the opening line
of its own `## Done` section, and its status reads `accepted` like the other thirty-two.

## Relationships not stated on a `**Status:**` line

Two pairs supersede or amend each other in the body only, not in the status line:

- [`0009`](0009-sample-consumes-a-local-feed.md) is amended by
  [`0012`](0012-per-package-versioning.md): 0012 replaced the repository-wide `$(Version)` 0009
  bound the sample to with one property per package, in a `> **Amended by 0012.**` block inside
  0009's own `## Decision` section.
- [`0015`](0015-sonar-rules-in-the-build.md) is amended by
  [`0019`](0019-the-net-analysers-run-in-the-build-too.md): 0019 measured false the reason 0015
  gave for keeping `samples/` off the analyser, in two `> **Amended by 0019 (2026-08-10).**`
  blocks inside 0015 itself, and 0019's own text says so directly — "this ADR amends 0015
  accordingly."

`0013`'s partial supersession by `0014` is the one relationship already on a status line; its
body adds the detail that only §1 (the oracle's normalizer scope) is superseded — §2 (the
unknown-piece penalty floor) stands, unaffected by 0014.
