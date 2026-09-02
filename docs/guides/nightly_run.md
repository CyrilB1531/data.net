# Nightly benchmark run

<!-- nightly-baseline: 61c67f300c69292b5f2743f80508b1e0903991f6 -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `61c67f300c69292b5f2743f80508b1e0903991f6`
- Previous run: `61c67f300c69292b5f2743f80508b1e0903991f6`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
- `BlockedTableBenchmarks`
- `BpeBenchmarks`
- `BpeScalingBenchmarks`
- `BucketRouteDiagnostics`
- `DecompositionBenchmarks`
- `FuzzBenchmarks`
- `FuzzIncumbentBenchmarks`
- `IndelBenchmarks`
- `LcsGateBenchmarks`
- `LevenshteinBenchmarks`
- `LevenshteinCodePointBenchmarks`
- `LevenshteinIncumbentBenchmarks`
- `MetricsBenchmarks`
- `MetricsIncumbentBenchmarks`
- `MyersGateBenchmarks`
- `PersistenceBenchmarks`
- `StopWordBenchmarks`
- `TokenizerIncumbentBenchmarks`
- `VectorMathBenchmarks`
- `VectorizerBenchmarks`
- `VectorizerIncumbentBenchmarks`

### Lodestar.Text.Benchmarks.BatchEmbeddingBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | CorpusSize | Mean         | Error       | StdDev    | Ratio | Gen0    | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|----------:|------:|--------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **6.308 μs** |   **0.1415 μs** | **0.0078 μs** |  **1.00** |  **0.1526** |       **-** |     **2.6 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.213 μs |   0.8702 μs | 0.0477 μs |  0.98 |  0.1831 |       - |       3 KB |        1.15 |
| EmbedBatchBucketed | 1          |     6.231 μs |   1.3355 μs | 0.0732 μs |  0.99 |  0.1831 |       - |       3 KB |        1.15 |
|                    |            |              |             |           |       |         |         |            |             |
| **UnitLoop**           | **8**          |    **95.037 μs** |   **2.0141 μs** | **0.1104 μs** |  **1.00** |  **5.7373** |  **0.1221** |   **94.76 KB** |        **1.00** |
| EmbedBatch         | 8          |    55.121 μs |   2.7727 μs | 0.1520 μs |  0.58 |  5.3711 |  0.2441 |   87.78 KB |        0.93 |
| EmbedBatchBucketed | 8          |    53.792 μs |   2.1392 μs | 0.1173 μs |  0.57 |  5.3711 |  0.2441 |   87.78 KB |        0.93 |
|                    |            |              |             |           |       |         |         |            |             |
| **UnitLoop**           | **32**         |   **348.354 μs** |  **33.5271 μs** | **1.8377 μs** |  **1.00** | **20.0195** |  **0.4883** |  **334.02 KB** |        **1.00** |
| EmbedBatch         | 32         |   190.606 μs |  22.6132 μs | 1.2395 μs |  0.55 | 18.5547 |  1.2207 |  306.63 KB |        0.92 |
| EmbedBatchBucketed | 32         |   183.299 μs |  29.8133 μs | 1.6342 μs |  0.53 | 17.8223 |  1.2207 |  293.12 KB |        0.88 |
|                    |            |              |             |           |       |         |         |            |             |
| **UnitLoop**           | **128**        | **1,431.807 μs** | **174.3016 μs** | **9.5541 μs** |  **1.00** | **80.0781** |  **3.9063** | **1336.03 KB** |        **1.00** |
| EmbedBatch         | 128        |   796.948 μs |  32.7423 μs | 1.7947 μs |  0.56 | 74.2188 | 10.7422 | 1225.67 KB |        0.92 |
| EmbedBatchBucketed | 128        |   734.093 μs | 154.1878 μs | 8.4516 μs |  0.51 | 70.3125 |  9.7656 | 1158.15 KB |        0.87 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | length | Mean          | Error        | StdDev     | Allocated |
|------- |------- |--------------:|-------------:|-----------:|----------:|
| **Latin**  | **1000**   |      **57.72 μs** |     **2.835 μs** |   **0.155 μs** |         **-** |
| Cjk    | 1000   |      59.46 μs |     0.570 μs |   0.031 μs |         - |
| **Latin**  | **10000**  |   **5,476.23 μs** |   **161.736 μs** |   **8.865 μs** |         **-** |
| Cjk    | 10000  |   6,712.73 μs |   578.505 μs |  31.710 μs |         - |
| **Latin**  | **65536**  | **202,497.55 μs** | **4,086.876 μs** | **224.015 μs** |         **-** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method  | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0      | Allocated | Alloc Ratio |
|-------- |---------:|---------:|--------:|------:|--------:|----------:|----------:|------------:|
| Unigram | 315.2 ms | 53.94 ms | 2.96 ms |  1.00 |    0.01 | 1500.0000 |  30.32 MB |        1.00 |
| Bpe     | 542.7 ms | 74.76 ms | 4.10 ms |  1.72 |    0.02 | 7000.0000 | 112.18 MB |        3.70 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                    | Length | Mean     | Error    | StdDev  | Gen0   | Allocated |
|-------------------------- |------- |---------:|---------:|--------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    | **104.1 μs** | **10.84 μs** | **0.59 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **221.1 μs** | **65.31 μs** | **3.58 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **482.2 μs** | **17.31 μs** | **0.95 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **989.1 μs** | **66.67 μs** | **3.65 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Alphabet | Mean       | Error      | StdDev    | Allocated |
|----------- |--------- |-----------:|-----------:|----------:|----------:|
| **DpGroup**    | **cjk**      |  **19.401 μs** |  **0.5894 μs** | **0.0323 μs** |         **-** |
| MyersGroup | cjk      | 244.087 μs | 36.0950 μs | 1.9785 μs |         - |
| **DpGroup**    | **latin**    |   **9.886 μs** |  **0.5830 μs** | **0.0320 μs** |         **-** |
| MyersGroup | latin    | 129.953 μs |  8.3058 μs | 0.4553 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.DecompositionBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                                    | Mean      | Error     | StdDev   | Ratio | RatioSD |
|------------------------------------------ |----------:|----------:|---------:|------:|--------:|
| TruncatedSvd_Rank20                       |  28.64 ms |  1.516 ms | 0.083 ms |  1.00 |    0.00 |
| Nmf_Rank20                                | 205.39 ms | 42.848 ms | 2.349 ms |  7.17 |    0.07 |
| MlNet_ProjectToPrincipalComponents_Rank20 |  23.94 ms |  2.520 ms | 0.138 ms |  0.84 |    0.00 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method         | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     97.91 ns |   1.694 ns |  0.093 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 11,314.96 ns | 545.739 ns | 29.914 ns | 115.57 |    0.28 |      - |         - |          NA |
| TokenSortRatio |  1,119.61 ns | 112.195 ns |  6.150 ns |  11.44 |    0.06 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  1,142.95 ns |  48.323 ns |  2.649 ns |  11.67 |    0.03 | 0.0858 |    1448 B |          NA |
| WRatio         |  2,328.09 ns |  77.646 ns |  4.256 ns |  23.78 |    0.04 | 0.1640 |    2760 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzIncumbentBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Operation     | Mean         | Error        | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------- |-------------- |-------------:|-------------:|----------:|------:|-------:|----------:|------------:|
| **Lodestar**   | **Ratio**         |     **96.96 ns** |     **0.855 ns** |  **0.047 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| FuzzySharp | Ratio         |    205.58 ns |     6.600 ns |  0.362 ns |  2.12 | 0.0048 |      80 B |          NA |
|            |               |              |              |           |       |        |           |             |
| **Lodestar**   | **PartialRatio**  | **11,446.54 ns** |   **909.883 ns** | **49.874 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| FuzzySharp | PartialRatio  | 10,132.46 ns | 1,359.471 ns | 74.517 ns |  0.89 |      - |     160 B |          NA |
|            |               |              |              |           |       |        |           |             |
| **Lodestar**   | **TokenSetRatio** |  **1,192.77 ns** |   **101.320 ns** |  **5.554 ns** |  **1.00** | **0.0858** |    **1448 B** |        **1.00** |
| FuzzySharp | TokenSetRatio |  2,068.65 ns |   197.370 ns | 10.819 ns |  1.73 | 0.1144 |    1944 B |        1.34 |
|            |               |              |              |           |       |        |           |             |
| **Lodestar**   | **WRatio**        |  **2,292.08 ns** |    **46.073 ns** |  **2.525 ns** |  **1.00** | **0.1640** |    **2760 B** |        **1.00** |
| FuzzySharp | WRatio        |  5,025.04 ns |   453.986 ns | 24.884 ns |  2.19 | 0.1831 |    3128 B |        1.13 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                     | Length | Mean          | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **33.51 ns** |     **5.424 ns** |   **0.297 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     130.31 ns |     2.923 ns |   0.160 ns |  3.89 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      27.03 ns |     0.425 ns |   0.023 ns |  0.81 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.51 ns |     4.797 ns |   0.263 ns |  0.79 |    0.01 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **27.94 ns** |     **0.593 ns** |   **0.032 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     147.03 ns |     6.713 ns |   0.368 ns |  5.26 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.66 ns |     1.093 ns |   0.060 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.51 ns |     5.230 ns |   0.287 ns |  1.02 |    0.01 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.74 ns** |     **0.846 ns** |   **0.046 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     157.90 ns |     1.811 ns |   0.099 ns |  5.14 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      30.65 ns |     0.381 ns |   0.021 ns |  1.00 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      30.55 ns |    11.732 ns |   0.643 ns |  0.99 |    0.02 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **35.44 ns** |     **0.605 ns** |   **0.033 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     184.88 ns |     4.132 ns |   0.227 ns |  5.22 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      34.77 ns |     0.388 ns |   0.021 ns |  0.98 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      32.65 ns |     0.161 ns |   0.009 ns |  0.92 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **55.02 ns** |     **0.222 ns** |   **0.012 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     712.06 ns |    22.466 ns |   1.231 ns | 12.94 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      56.92 ns |     0.350 ns |   0.019 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      59.59 ns |     3.143 ns |   0.172 ns |  1.08 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **61.31 ns** |     **3.963 ns** |   **0.217 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,018.23 ns |    13.341 ns |   0.731 ns | 16.61 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      65.18 ns |     0.662 ns |   0.036 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      59.49 ns |     0.323 ns |   0.018 ns |  0.97 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **936.13 ns** |    **18.499 ns** |   **1.014 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  21,719.81 ns | 5,740.263 ns | 314.643 ns | 23.20 |    0.29 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     922.04 ns |     6.142 ns |   0.337 ns |  0.98 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     921.61 ns |     6.085 ns |   0.334 ns |  0.98 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **7,762.48 ns** |    **15.322 ns** |   **0.840 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 341,010.79 ns | 5,967.663 ns | 327.108 ns | 43.93 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   7,500.01 ns |   185.367 ns |  10.161 ns |  0.97 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   7,708.25 ns |    96.572 ns |   5.293 ns |  0.99 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **8**    |    **131.97 ns** |     **4.418 ns** |   **0.242 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     55.47 ns |     0.651 ns |   0.036 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    132.68 ns |    16.492 ns |   0.904 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 8    |     97.00 ns |     5.361 ns |   0.294 ns |  0.74 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **217.48 ns** |     **7.143 ns** |   **0.392 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |     61.60 ns |     1.496 ns |   0.082 ns |  0.28 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    231.61 ns |   146.094 ns |   8.008 ns |  1.07 |    0.03 |         - |          NA |
| Kernel_Cjk | 12   |    109.30 ns |     1.440 ns |   0.079 ns |  0.50 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **14**   |    **267.22 ns** |     **7.487 ns** |   **0.410 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 14   |     66.94 ns |     1.973 ns |   0.108 ns |  0.25 |    0.00 |         - |          NA |
| Dp_Cjk     | 14   |    271.29 ns |    85.696 ns |   4.697 ns |  1.02 |    0.02 |         - |          NA |
| Kernel_Cjk | 14   |    112.37 ns |     4.125 ns |   0.226 ns |  0.42 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **355.59 ns** |    **96.407 ns** |   **5.284 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 16   |     72.11 ns |    10.559 ns |   0.579 ns |  0.20 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    359.60 ns |   338.279 ns |  18.542 ns |  1.01 |    0.05 |         - |          NA |
| Kernel_Cjk | 16   |    118.94 ns |     3.917 ns |   0.215 ns |  0.33 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **18**   |    **435.48 ns** |     **8.054 ns** |   **0.441 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 18   |     73.93 ns |     1.603 ns |   0.088 ns |  0.17 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    446.27 ns |    84.039 ns |   4.606 ns |  1.02 |    0.01 |         - |          NA |
| Kernel_Cjk | 18   |    124.25 ns |     6.712 ns |   0.368 ns |  0.29 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **20**   |    **770.73 ns** |   **250.197 ns** |  **13.714 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 20   |     81.02 ns |     0.409 ns |   0.022 ns |  0.11 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    755.96 ns |   157.117 ns |   8.612 ns |  0.98 |    0.02 |         - |          NA |
| Kernel_Cjk | 20   |    128.57 ns |     1.311 ns |   0.072 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **986.31 ns** |   **501.145 ns** |  **27.469 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 24   |     88.20 ns |     1.918 ns |   0.105 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    994.26 ns |   219.347 ns |  12.023 ns |  1.01 |    0.03 |         - |          NA |
| Kernel_Cjk | 24   |    140.89 ns |     6.790 ns |   0.372 ns |  0.14 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,550.33 ns** |   **563.433 ns** |  **30.884 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 32   |    106.19 ns |     2.972 ns |   0.163 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,550.50 ns |   595.737 ns |  32.654 ns |  1.00 |    0.03 |         - |          NA |
| Kernel_Cjk | 32   |    169.27 ns |    47.457 ns |   2.601 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,206.79 ns** |   **110.942 ns** |   **6.081 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 48   |    131.51 ns |     2.464 ns |   0.135 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,142.16 ns |   691.283 ns |  37.892 ns |  0.98 |    0.01 |         - |          NA |
| Kernel_Cjk | 48   |    241.35 ns |     7.463 ns |   0.409 ns |  0.08 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **5,315.81 ns** | **6,407.935 ns** | **351.241 ns** |  **1.00** |    **0.08** |         **-** |          **NA** |
| Kernel     | 64   |    157.16 ns |     3.794 ns |   0.208 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,106.27 ns | 1,189.893 ns |  65.222 ns |  1.15 |    0.06 |         - |          NA |
| Kernel_Cjk | 64   |    293.62 ns |    29.598 ns |   1.622 ns |  0.06 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **13,806.15 ns** |   **627.199 ns** |  **34.379 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 96   |    778.98 ns |     8.140 ns |   0.446 ns |  0.06 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 12,377.76 ns |   979.466 ns |  53.688 ns |  0.90 |    0.00 |         - |          NA |
| Kernel_Cjk | 96   |  1,034.88 ns |    17.404 ns |   0.954 ns |  0.07 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                     | Length | Mean         | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **24.99 ns** |   **9.025 ns** |  **0.495 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    122.17 ns |  18.134 ns |  0.994 ns |  4.89 |    0.09 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.21 ns |   0.161 ns |  0.009 ns |  1.01 |    0.02 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **270.66 ns** |  **16.573 ns** |  **0.908 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    671.11 ns |  18.058 ns |  0.990 ns |  2.48 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    271.21 ns |   2.548 ns |  0.140 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **512**    | **14,371.02 ns** | **368.472 ns** | **20.197 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 16,996.64 ns | 361.831 ns | 19.833 ns |  1.18 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,337.45 ns | 123.837 ns |  6.788 ns |  1.00 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | Length | Distinct | Mean         | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |-------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |     **349.4 ns** |     **18.38 ns** |     **1.01 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     237.6 ns |     10.54 ns |     0.58 ns |  0.68 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **334.1 ns** |     **20.12 ns** |     **1.10 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     240.4 ns |     70.56 ns |     3.87 ns |  0.72 |    0.01 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **414.3 ns** |      **5.93 ns** |     **0.33 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     320.4 ns |      1.16 ns |     0.06 ns |  0.77 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **426.4 ns** |      **4.34 ns** |     **0.24 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     325.4 ns |      0.73 ns |     0.04 ns |  0.76 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **518.7 ns** |     **27.38 ns** |     **1.50 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     412.6 ns |     45.70 ns |     2.50 ns |  0.80 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **509.9 ns** |      **5.94 ns** |     **0.33 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     418.6 ns |      6.50 ns |     0.36 ns |  0.82 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **601.4 ns** |    **113.96 ns** |     **6.25 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,308.8 ns |    146.47 ns |     8.03 ns |  2.18 |    0.02 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **588.1 ns** |     **11.18 ns** |     **0.61 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,313.5 ns |    324.05 ns |    17.76 ns |  2.23 |    0.03 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,548.9 ns** |     **26.86 ns** |     **1.47 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,399.1 ns |    205.65 ns |    11.27 ns |  2.12 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,577.5 ns** |     **18.58 ns** |     **1.02 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,485.2 ns |    122.68 ns |     6.72 ns |  2.13 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **18,176.1 ns** |    **141.15 ns** |     **7.74 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  57,582.8 ns |  1,840.40 ns |   100.88 ns |  3.17 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **444,169.1 ns** | **50,372.15 ns** | **2,761.07 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  60,505.2 ns |    328.95 ns |    18.03 ns |  0.14 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinIncumbentBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method               | Length | Mean          | Error          | StdDev       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |------- |--------------:|---------------:|-------------:|------:|--------:|-------:|----------:|------------:|
| **Lodestar**             | **8**      |      **24.63 ns** |       **0.371 ns** |     **0.020 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Fastenshtein         | 8      |      88.08 ns |       3.409 ns |     0.187 ns |  3.58 |    0.01 | 0.0033 |      56 B |          NA |
| Quickenshtein        | 8      |      84.50 ns |       1.798 ns |     0.099 ns |  3.43 |    0.00 |      - |         - |          NA |
| F23_StringSimilarity | 8      |     178.94 ns |      23.571 ns |     1.292 ns |  7.27 |    0.05 | 0.0076 |     128 B |          NA |
|                      |        |               |                |              |       |         |        |           |             |
| **Lodestar**             | **64**     |     **276.98 ns** |      **22.787 ns** |     **1.249 ns** |  **1.00** |    **0.01** |      **-** |         **-** |          **NA** |
| Fastenshtein         | 64     |   6,856.18 ns |     927.383 ns |    50.833 ns | 24.75 |    0.19 | 0.0153 |     280 B |          NA |
| Quickenshtein        | 64     |   1,386.03 ns |     111.664 ns |     6.121 ns |  5.00 |    0.03 |      - |         - |          NA |
| F23_StringSimilarity | 64     |  10,307.91 ns |   4,243.307 ns |   232.590 ns | 37.22 |    0.74 | 0.0305 |     576 B |          NA |
|                      |        |               |                |              |       |         |        |           |             |
| **Lodestar**             | **512**    |  **14,001.37 ns** |      **51.943 ns** |     **2.847 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Fastenshtein         | 512    | 425,631.86 ns |  20,613.303 ns | 1,129.885 ns | 30.40 |    0.07 |      - |    2072 B |          NA |
| Quickenshtein        | 512    |  38,486.96 ns |   2,151.964 ns |   117.956 ns |  2.75 |    0.01 |      - |         - |          NA |
| F23_StringSimilarity | 512    | 705,315.74 ns | 177,877.757 ns | 9,750.080 ns | 50.37 |    0.60 |      - |    4161 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method         | Samples | Classes | Mean            | Error           | StdDev        | Gen0   | Allocated |
|--------------- |-------- |-------- |----------------:|----------------:|--------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |      **7,761.5 ns** |       **462.32 ns** |      **25.34 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7,613.8 ns |       805.31 ns |      44.14 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |        936.2 ns |       256.94 ns |      14.08 ns |      - |         - |
| F1Macro        | 1000    | 2       |      7,662.7 ns |       823.91 ns |      45.16 ns | 0.0153 |     472 B |
| Report         | 1000    | 2       |     10,682.5 ns |       120.81 ns |       6.62 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7,824.9 ns** |       **506.00 ns** |      **27.74 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7,717.5 ns |       293.78 ns |      16.10 ns | 0.0610 |    1248 B |
| AccuracyScore  | 1000    | 10      |        917.4 ns |         5.75 ns |       0.32 ns |      - |         - |
| F1Macro        | 1000    | 10      |      7,964.1 ns |       480.30 ns |      26.33 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     15,359.5 ns |     2,102.44 ns |     115.24 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **866,758.4 ns** |    **33,570.96 ns** |   **1,840.14 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    814,094.6 ns |    55,784.60 ns |   3,057.74 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    163,689.4 ns |     5,218.03 ns |     286.02 ns |      - |         - |
| F1Macro        | 100000  | 2       |    856,339.0 ns |    20,190.51 ns |   1,106.71 ns |      - |     473 B |
| Report         | 100000  | 2       |    873,117.7 ns |    77,336.52 ns |   4,239.08 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **968,689.6 ns** |    **10,149.16 ns** |     **556.31 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    937,179.5 ns |    23,015.18 ns |   1,261.54 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    264,565.1 ns |     7,407.86 ns |     406.05 ns |      - |         - |
| F1Macro        | 100000  | 10      |    943,939.8 ns |    40,124.43 ns |   2,199.36 ns |      - |    1665 B |
| Report         | 100000  | 10      |    994,617.6 ns |    16,761.28 ns |     918.74 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,694,799.8 ns** |   **271,735.31 ns** |  **14,894.73 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,649,756.5 ns | 5,954,161.98 ns | 326,367.71 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,748,494.5 ns |   134,757.99 ns |   7,386.54 ns |      - |         - |
| F1Macro        | 1000000 | 2       |  8,265,673.0 ns |   195,100.66 ns |  10,694.13 ns |      - |     484 B |
| Report         | 1000000 | 2       |  8,761,121.1 ns |   129,429.82 ns |   7,094.49 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **10,024,105.4 ns** | **1,270,497.72 ns** |  **69,640.27 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      |  9,359,776.3 ns |   153,085.75 ns |   8,391.15 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  2,766,912.2 ns |   130,879.76 ns |   7,173.96 ns |      - |         - |
| F1Macro        | 1000000 | 10      |  9,962,469.8 ns |   539,002.82 ns |  29,544.56 ns |      - |    1676 B |
| Report         | 1000000 | 10      |  9,773,047.3 ns |   595,450.78 ns |  32,638.67 ns |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsIncumbentBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method   | Samples | Request       | Mean         | Error        | StdDev      | Ratio  | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------- |-------- |-------------- |-------------:|-------------:|------------:|-------:|--------:|---------:|---------:|---------:|-----------:|------------:|
| **Lodestar** | **100000**  | **Bundle**        |   **8,954.2 μs** |    **340.35 μs** |    **18.66 μs** |   **1.00** |    **0.00** |        **-** |        **-** |        **-** |     **1016 B** |        **1.00** |
| MlNet    | 100000  | Bundle        |  34,630.7 μs |    403.18 μs |    22.10 μs |   3.87 |    0.01 | 666.6667 | 666.6667 | 666.6667 |  5090245 B |    5,010.08 |
|          |         |               |              |              |             |        |         |          |          |          |            |             |
| **Lodestar** | **100000**  | **AccuracyAlone** |     **237.3 μs** |     **82.92 μs** |     **4.55 μs** |   **1.00** |    **0.02** |        **-** |        **-** |        **-** |          **-** |          **NA** |
| MlNet    | 100000  | AccuracyAlone |  34,920.7 μs |    605.72 μs |    33.20 μs | 147.17 |    2.42 | 600.0000 | 600.0000 | 600.0000 |  5089622 B |          NA |
|          |         |               |              |              |             |        |         |          |          |          |            |             |
| **Lodestar** | **1000000** | **Bundle**        | **109,639.8 μs** | **44,434.44 μs** | **2,435.60 μs** |   **1.00** |    **0.03** |        **-** |        **-** |        **-** |          **-** |          **NA** |
| MlNet    | 1000000 | Bundle        | 225,048.7 μs | 16,364.71 μs |   897.00 μs |   2.05 |    0.04 |        - |        - |        - | 23228104 B |          NA |
|          |         |               |              |              |             |        |         |          |          |          |            |             |
| **Lodestar** | **1000000** | **AccuracyAlone** |   **2,631.6 μs** |    **663.93 μs** |    **36.39 μs** |   **1.00** |    **0.02** |        **-** |        **-** |        **-** |          **-** |          **NA** |
| MlNet    | 1000000 | AccuracyAlone | 246,591.1 μs | 11,946.81 μs |   654.84 μs |  93.72 |    1.13 |        - |        - |        - | 23231816 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **4**    |     **73.61 ns** |     **1.528 ns** |   **0.084 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 4    |     73.14 ns |     0.497 ns |   0.027 ns |  0.99 |    0.00 |         - |          NA |
| Dp_Cjk     | 4    |     73.39 ns |     0.603 ns |   0.033 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 4    |     73.78 ns |     1.139 ns |   0.062 ns |  1.00 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **6**    |    **104.18 ns** |     **1.615 ns** |   **0.089 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 6    |     75.42 ns |     0.666 ns |   0.036 ns |  0.72 |    0.00 |         - |          NA |
| Dp_Cjk     | 6    |    103.82 ns |     2.911 ns |   0.160 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 6    |    150.16 ns |     3.823 ns |   0.210 ns |  1.44 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **8**    |    **158.08 ns** |     **1.947 ns** |   **0.107 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     87.41 ns |     1.221 ns |   0.067 ns |  0.55 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    157.00 ns |     1.038 ns |   0.057 ns |  0.99 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    202.74 ns |     6.171 ns |   0.338 ns |  1.28 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **10**   |    **202.63 ns** |    **23.271 ns** |   **1.276 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 10   |     97.38 ns |     0.704 ns |   0.039 ns |  0.48 |    0.00 |         - |          NA |
| Dp_Cjk     | 10   |    196.60 ns |     8.341 ns |   0.457 ns |  0.97 |    0.01 |         - |          NA |
| Kernel_Cjk | 10   |    153.56 ns |    32.110 ns |   1.760 ns |  0.76 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **244.85 ns** |     **3.818 ns** |   **0.209 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |    104.53 ns |     0.932 ns |   0.051 ns |  0.43 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    245.16 ns |    10.287 ns |   0.564 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 12   |    163.92 ns |     5.162 ns |   0.283 ns |  0.67 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **408.87 ns** |    **15.595 ns** |   **0.855 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 16   |    122.23 ns |     2.304 ns |   0.126 ns |  0.30 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    416.73 ns |    13.140 ns |   0.720 ns |  1.02 |    0.00 |         - |          NA |
| Kernel_Cjk | 16   |    181.06 ns |    12.377 ns |   0.678 ns |  0.44 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **848.63 ns** |   **151.485 ns** |   **8.303 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 24   |    158.36 ns |     0.749 ns |   0.041 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    844.01 ns |    51.329 ns |   2.814 ns |  0.99 |    0.01 |         - |          NA |
| Kernel_Cjk | 24   |    227.71 ns |     2.937 ns |   0.161 ns |  0.27 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,445.01 ns** |    **10.407 ns** |   **0.570 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    195.55 ns |    13.474 ns |   0.739 ns |  0.14 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,448.39 ns |    30.259 ns |   1.659 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 32   |    271.38 ns |    11.042 ns |   0.605 ns |  0.19 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,310.48 ns** |   **296.127 ns** |  **16.232 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 48   |    265.31 ns |    13.892 ns |   0.761 ns |  0.08 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,306.14 ns |   200.875 ns |  11.011 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 48   |    353.73 ns |     1.088 ns |   0.060 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **6,193.46 ns** |   **150.596 ns** |   **8.255 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 64   |    339.89 ns |     2.445 ns |   0.134 ns |  0.05 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  7,101.07 ns |   447.642 ns |  24.537 ns |  1.15 |    0.00 |         - |          NA |
| Kernel_Cjk | 64   |    436.86 ns |     2.445 ns |   0.134 ns |  0.07 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **14,352.42 ns** | **7,423.690 ns** | **406.917 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 96   |  1,746.48 ns |   395.931 ns |  21.702 ns |  0.12 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 13,985.24 ns |   552.712 ns |  30.296 ns |  0.97 |    0.02 |         - |          NA |
| Kernel_Cjk | 96   |  1,529.55 ns |    79.577 ns |   4.362 ns |  0.11 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                 | Mean      | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated |
|----------------------- |----------:|----------:|----------:|---------:|---------:|---------:|----------:|
| VocabTxt               |  4.284 ms | 3.9823 ms | 0.2183 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.523 ms | 3.2221 ms | 0.1766 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.618 ms | 2.2364 ms | 0.1226 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.913 ms | 3.0087 ms | 0.1649 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.885 ms | 0.3888 ms | 0.0213 ms |  33.2031 |  27.3438 |  27.3438 |   2.09 MB |
| TfidfLoad              |  4.487 ms | 1.8960 ms | 0.1039 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  4.312 ms | 0.9567 ms | 0.0524 ms | 273.4375 | 273.4375 | 273.4375 |  19.87 MB |
| EmbeddingIndexLoad     |  5.863 ms | 0.3480 ms | 0.0191 ms | 203.1250 | 171.8750 | 140.6250 |  15.72 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method               | Documents | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1      | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|----------:|----------:|------:|--------:|----------:|----------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.624 ms** | **3.9717 ms** | **0.2177 ms** |  **1.00** |    **0.03** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.216 ms | 0.3996 ms | 0.0219 ms |  0.82 |    0.02 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.471 ms | 1.1820 ms | 0.0648 ms |  0.98 |    0.03 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.465 ms | 0.4712 ms | 0.0258 ms |  0.85 |    0.02 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |         |           |           |          |           |             |
| **Count**                | **1000**      | **30.535 ms** | **0.7885 ms** | **0.0432 ms** |  **1.00** |    **0.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 24.562 ms | 0.4655 ms | 0.0255 ms |  0.80 |    0.00 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 29.474 ms | 3.8791 ms | 0.2126 ms |  0.97 |    0.01 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 24.850 ms | 1.3464 ms | 0.0738 ms |  0.81 |    0.00 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.TokenizerIncumbentBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method       | Model         | Mean      | Error     | StdDev   | Ratio | Gen0      | Gen1     | Allocated | Alloc Ratio |
|------------- |-------------- |----------:|----------:|---------:|------:|----------:|---------:|----------:|------------:|
| **Lodestar**     | **WordPiece**     |  **66.87 ms** |  **8.618 ms** | **0.472 ms** |  **1.00** | **4250.0000** | **125.0000** |  **68.25 MB** |        **1.00** |
| MlTokenizers | WordPiece     |  54.29 ms |  4.407 ms | 0.242 ms |  0.81 |  200.0000 |        - |   3.55 MB |        0.05 |
|              |               |           |           |          |       |           |          |           |             |
| **Lodestar**     | **SentencePiece** | **344.12 ms** | **29.055 ms** | **1.593 ms** |  **1.00** | **1000.0000** |        **-** |  **30.33 MB** |        **1.00** |
| MlTokenizers | SentencePiece |  51.22 ms |  5.378 ms | 0.295 ms |  0.15 |  100.0000 |        - |   3.09 MB |        0.10 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | Dim  | Mean      | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------- |----- |----------:|----------:|---------:|------:|----------:|------------:|
| **Dot**    | **384**  | **131.83 ns** |  **0.380 ns** | **0.021 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  51.38 ns | 15.244 ns | 0.836 ns |  0.39 |         - |          NA |
|        |      |           |           |          |       |           |             |
| **Dot**    | **768**  |  **94.99 ns** |  **5.494 ns** | **0.301 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  92.47 ns |  0.270 ns | 0.015 ns |  0.97 |         - |          NA |
|        |      |           |           |          |       |           |             |
| **Dot**    | **1024** | **123.95 ns** |  **1.595 ns** | **0.087 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 122.34 ns |  1.702 ns | 0.093 ns |  0.99 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method       | Documents | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|------------- |---------- |----------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **Count**        | **200**       |  **3.057 ms** | **5.5554 ms** | **0.3045 ms** |  **1.01** |    **0.12** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  2.972 ms | 2.6045 ms | 0.1428 ms |  0.98 |    0.09 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.753 ms | 0.8754 ms | 0.0480 ms |  1.24 |    0.10 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.944 ms | 1.7660 ms | 0.0968 ms |  0.97 |    0.08 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.202 ms** | **1.2529 ms** | **0.0687 ms** |  **1.00** |    **0.01** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.558 ms | 0.6619 ms | 0.0363 ms |  1.05 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.806 ms | 2.0852 ms | 0.1143 ms |  1.64 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.073 ms | 0.4444 ms | 0.0244 ms |  0.98 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerIncumbentBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method   | Documents | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0       | Gen1       | Gen2       | Allocated | Alloc Ratio |
|--------- |---------- |-----------:|------------:|-----------:|------:|--------:|-----------:|-----------:|-----------:|----------:|------------:|
| **Lodestar** | **200**       |   **7.482 ms** |   **0.2349 ms** |  **0.0129 ms** |  **1.00** |    **0.00** |   **296.8750** |   **218.7500** |   **140.6250** |   **5.13 MB** |        **1.00** |
| MlNet    | 200       |  53.013 ms |  98.5404 ms |  5.4013 ms |  7.09 |    0.63 |  7500.0000 |  7500.0000 |  7500.0000 |  28.27 MB |        5.52 |
|          |           |            |             |            |       |         |            |            |            |           |             |
| **Lodestar** | **1000**      |  **30.448 ms** |   **9.9631 ms** |  **0.5461 ms** |  **1.00** |    **0.02** |  **2625.0000** |  **2437.5000** |  **1500.0000** |  **24.92 MB** |        **1.00** |
| MlNet    | 1000      | 392.357 ms | 700.6388 ms | 38.4044 ms | 12.89 |    1.11 | 79000.0000 | 79000.0000 | 79000.0000 | 324.46 MB |       13.02 |

<!-- markdownlint-enable MD060 -->

## Against rapidfuzz, in this same run

Both sides on this VM in these minutes, which is what makes the ratio readable where the absolutes are not.

- `indel`
- `levenshtein`
- `metrics`
- `persistence`

### compare-indel

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 115.2 | 27.8 | 4.14x C# faster |
| latin | 32 | 179.1 | 89.1 | 2.01x C# faster |
| latin | 128 | 471.0 | 900.3 | 1.91x Py faster |
| latin | 512 | 4442.2 | 7579.2 | 1.71x Py faster |
| cjk | 8 | 138.6 | 27.4 | 5.05x C# faster |
| cjk | 32 | 329.3 | 225.6 | 1.46x C# faster |
| cjk | 128 | 1900.1 | 1618.3 | 1.17x C# faster |
| cjk | 512 | 14909.1 | 10828.9 | 1.38x C# faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is Hyyro's bit-parallel LLCS above a pattern of 8 and a rolling-row dynamic program below it (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 160.5 | 20.1 | 7.97x C# faster |
| latin | 32 | 293.4 | 158.7 | 1.85x C# faster |
| latin | 128 | 1702.9 | 1480.4 | 1.15x C# faster |
| latin | 512 | 14020.2 | 14877.6 | 1.06x Py faster |
| cjk | 8 | 153.1 | 17.6 | 8.70x C# faster |
| cjk | 32 | 373.3 | 284.3 | 1.31x C# faster |
| cjk | 128 | 2827.5 | 2303.3 | 1.23x C# faster |
| cjk | 512 | 23658.2 | 18414.9 | 1.28x C# faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.981 | 104.31x | 0.009 | 0.981 | 104.30x |
| accuracy_n1000_k2 | 0.001 | 0.520 | 505.41x | 0.001 | 0.520 | 505.38x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.749 | 221.18x | 0.008 | 1.749 | 221.18x |
| classification_report_n1000_k2 | 0.011 | 6.676 | 620.90x | 0.011 | 6.675 | 620.85x |
| roc_auc_binary_n1000_k2 | 0.015 | 1.927 | 126.29x | 0.015 | 1.927 | 125.57x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.049 | 134.72x | 0.008 | 1.049 | 134.70x |
| matthews_n1000_k2 | 0.008 | 1.956 | 252.15x | 0.008 | 1.956 | 252.14x |
| cohen_kappa_n1000_k2 | 0.008 | 1.087 | 139.02x | 0.008 | 1.087 | 139.00x |
| mse_n1000_k2 | 0.002 | 0.304 | 125.44x | 0.002 | 0.304 | 125.43x |
| mae_n1000_k2 | 0.002 | 0.302 | 124.83x | 0.002 | 0.302 | 124.83x |
| median_ae_n1000_k2 | 0.007 | 0.315 | 48.22x | 0.007 | 0.315 | 48.21x |
| r2_n1000_k2 | 0.003 | 0.365 | 141.48x | 0.003 | 0.365 | 141.48x |
| confusion_matrix_n1000_k10 | 0.010 | 0.979 | 102.89x | 0.010 | 0.979 | 102.88x |
| accuracy_n1000_k10 | 0.001 | 0.520 | 461.45x | 0.001 | 0.520 | 461.45x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.756 | 208.84x | 0.008 | 1.756 | 208.86x |
| classification_report_n1000_k10 | 0.016 | 6.850 | 440.91x | 0.016 | 6.849 | 440.87x |
| roc_auc_ovr_macro_n1000_k10 | 0.542 | 9.725 | 17.95x | 0.542 | 9.724 | 17.95x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.039 | 124.29x | 0.008 | 1.039 | 124.29x |
| matthews_n1000_k10 | 0.008 | 1.984 | 239.24x | 0.008 | 1.984 | 239.24x |
| cohen_kappa_n1000_k10 | 0.009 | 1.079 | 123.52x | 0.009 | 1.079 | 123.50x |
| mse_n1000_k10 | 0.002 | 0.303 | 124.96x | 0.002 | 0.303 | 124.95x |
| mae_n1000_k10 | 0.002 | 0.302 | 124.99x | 0.002 | 0.302 | 124.99x |
| median_ae_n1000_k10 | 0.006 | 0.314 | 48.79x | 0.006 | 0.314 | 48.70x |
| r2_n1000_k10 | 0.003 | 0.363 | 140.84x | 0.003 | 0.363 | 140.83x |
| confusion_matrix_n100000_k2 | 1.000 | 10.687 | 10.69x | 1.000 | 10.685 | 10.69x |
| accuracy_n100000_k2 | 0.186 | 3.738 | 20.13x | 0.186 | 3.737 | 20.13x |
| precision_recall_f1_macro_n100000_k2 | 0.880 | 12.244 | 13.91x | 0.880 | 12.244 | 13.91x |
| classification_report_n100000_k2 | 0.883 | 26.740 | 30.27x | 0.883 | 26.739 | 30.27x |
| roc_auc_binary_n100000_k2 | 3.471 | 26.451 | 7.62x | 3.471 | 26.448 | 7.62x |
| balanced_accuracy_n100000_k2 | 0.880 | 10.762 | 12.23x | 0.880 | 10.762 | 12.23x |
| matthews_n100000_k2 | 0.871 | 21.404 | 24.58x | 0.871 | 21.403 | 24.58x |
| cohen_kappa_n100000_k2 | 0.878 | 10.796 | 12.29x | 0.878 | 10.795 | 12.29x |
| mse_n100000_k2 | 0.239 | 0.441 | 1.85x | 0.239 | 0.441 | 1.85x |
| mae_n100000_k2 | 0.238 | 0.434 | 1.82x | 0.238 | 0.434 | 1.82x |
| median_ae_n100000_k2 | 0.757 | 1.788 | 2.36x | 0.775 | 1.788 | 2.31x |
| r2_n100000_k2 | 0.235 | 0.656 | 2.79x | 0.235 | 0.656 | 2.79x |
| confusion_matrix_n100000_k10 | 0.991 | 10.698 | 10.80x | 0.991 | 10.695 | 10.79x |
| accuracy_n100000_k10 | 0.272 | 3.749 | 13.78x | 0.272 | 3.749 | 13.78x |
| precision_recall_f1_macro_n100000_k10 | 1.007 | 12.882 | 12.79x | 1.007 | 12.881 | 12.79x |
| classification_report_n100000_k10 | 1.001 | 29.377 | 29.36x | 1.001 | 29.375 | 29.36x |
| roc_auc_ovr_macro_n100000_k10 | 36.375 | 215.810 | 5.93x | 36.371 | 215.797 | 5.93x |
| balanced_accuracy_n100000_k10 | 0.994 | 10.755 | 10.82x | 0.994 | 10.755 | 10.82x |
| matthews_n100000_k10 | 0.991 | 22.164 | 22.37x | 0.991 | 22.161 | 22.37x |
| cohen_kappa_n100000_k10 | 1.013 | 10.788 | 10.65x | 1.012 | 10.787 | 10.65x |
| mse_n100000_k10 | 0.238 | 0.440 | 1.84x | 0.238 | 0.440 | 1.84x |
| mae_n100000_k10 | 0.238 | 0.433 | 1.82x | 0.238 | 0.433 | 1.82x |
| median_ae_n100000_k10 | 0.786 | 1.787 | 2.27x | 0.835 | 1.787 | 2.14x |
| r2_n100000_k10 | 0.235 | 0.656 | 2.79x | 0.235 | 0.656 | 2.79x |
| confusion_matrix_n1000000_k2 | 8.826 | 100.028 | 11.33x | 8.825 | 99.997 | 11.33x |
| accuracy_n1000000_k2 | 1.954 | 33.092 | 16.93x | 1.954 | 33.086 | 16.93x |
| precision_recall_f1_macro_n1000000_k2 | 8.759 | 107.716 | 12.30x | 8.758 | 107.707 | 12.30x |
| classification_report_n1000000_k2 | 8.855 | 209.910 | 23.70x | 8.855 | 209.895 | 23.70x |
| roc_auc_binary_n1000000_k2 | 45.078 | 288.353 | 6.40x | 45.082 | 288.337 | 6.40x |
| balanced_accuracy_n1000000_k2 | 8.826 | 99.890 | 11.32x | 8.827 | 99.865 | 11.31x |
| matthews_n1000000_k2 | 8.780 | 200.691 | 22.86x | 8.780 | 200.669 | 22.85x |
| cohen_kappa_n1000000_k2 | 8.828 | 100.030 | 11.33x | 8.826 | 100.025 | 11.33x |
| mse_n1000000_k2 | 2.377 | 2.207 | 0.93x | 2.377 | 2.207 | 0.93x |
| mae_n1000000_k2 | 2.375 | 2.182 | 0.92x | 2.374 | 2.182 | 0.92x |
| median_ae_n1000000_k2 | 7.019 | 14.272 | 2.03x | 7.126 | 14.271 | 2.00x |
| r2_n1000000_k2 | 2.346 | 4.270 | 1.82x | 2.346 | 4.269 | 1.82x |
| confusion_matrix_n1000000_k10 | 9.939 | 99.895 | 10.05x | 9.938 | 99.854 | 10.05x |
| accuracy_n1000000_k10 | 2.790 | 33.187 | 11.89x | 2.790 | 33.177 | 11.89x |
| precision_recall_f1_macro_n1000000_k10 | 9.987 | 113.941 | 11.41x | 9.986 | 113.872 | 11.40x |
| classification_report_n1000000_k10 | 9.949 | 233.854 | 23.51x | 9.948 | 233.825 | 23.50x |
| balanced_accuracy_n1000000_k10 | 9.974 | 100.366 | 10.06x | 9.973 | 100.359 | 10.06x |
| matthews_n1000000_k10 | 9.825 | 207.928 | 21.16x | 9.824 | 207.920 | 21.16x |
| cohen_kappa_n1000000_k10 | 9.954 | 100.169 | 10.06x | 9.953 | 100.166 | 10.06x |
| mse_n1000000_k10 | 2.386 | 2.247 | 0.94x | 2.386 | 2.247 | 0.94x |
| mae_n1000000_k10 | 2.384 | 2.114 | 0.89x | 2.384 | 2.114 | 0.89x |
| median_ae_n1000000_k10 | 7.021 | 14.214 | 2.02x | 7.099 | 14.213 | 2.00x |
| r2_n1000000_k10 | 2.648 | 3.576 | 1.35x | 2.648 | 3.575 | 1.35x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.93x
  mae_n1000000_k2                  0.92x
  mse_n1000000_k10                 0.94x
  mae_n1000000_k10                 0.89x

<!-- markdownlint-enable MD060 -->

### compare-persistence

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.899 | 9.665 | 1.97x | 5.173 | 9.664 | 1.87x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.745 | 16.091 | 1.26x | 13.174 | 16.088 | 1.22x | 706,526 | 706,526 |
| tokenizer_json_unigram | 13.772 | 38.345 | 2.78x | 13.928 | 38.341 | 2.75x | 1,990,038 | 1,990,038 |
| spiece_model | 4.789 | 28.796 | 6.01x | 4.972 | 28.794 | 5.79x | 533,084 | 533,084 |
| tfidf_save | 1.630 | 2.515 | 1.54x | 1.668 | 2.515 | 1.51x | 581,787 | 591,922 |
| tfidf_load | 4.787 | 4.041 | 0.84x | 5.000 | 4.040 | 0.81x | 581,787 | 591,922 |
| embedding_index_save | 4.255 | 1.566 | 0.37x | 4.447 | 1.566 | 0.35x | 20,589,007 | 15,360,128 |
| embedding_index_save_file | 49.719 | 36.896 | 0.74x | 10.902 | 5.160 | 0.47x | 20,589,007 | 15,360,128 |
| embedding_index_load | 5.498 | 1.531 | 0.28x | 5.815 | 1.531 | 0.26x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 6.319 | 0.987 | 0.16x | 6.768 | 0.987 | 0.15x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 4.421 | 1.469 | 0.33x | 4.707 | 1.469 | 0.31x | 20,589,007 | 15,360,128 |
| embedding_index_ingest_npy | 1.316 | 1.461 | 1.11x | 1.474 | 1.461 | 0.99x | 15,360,128 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 92.59x | 0.000 | 0.001 | 92.59x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 409.765 | 557.068 | 1.36x | 412.041 | 556.948 | 1.35x | 15,250,490 | 14,022,374 |
| embedding_index_load_gzip | 77.484 | 66.618 | 0.86x | 78.294 | 66.612 | 0.85x | 15,250,490 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
