# Nightly benchmark run

<!-- nightly-baseline: 3aab1281a849e2b8790dd705f6e031dc5a79eb73 -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `3aab1281a849e2b8790dd705f6e031dc5a79eb73`
- Previous run: `3aab1281a849e2b8790dd705f6e031dc5a79eb73`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
- `BkTreeBenchmarks`
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

| Method             | CorpusSize | Mean         | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|-----------:|----------:|------:|--------:|--------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **5.908 μs** |  **0.4463 μs** | **0.0245 μs** |  **1.00** |    **0.01** |  **0.1526** |       **-** |     **2.6 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.242 μs |  3.6329 μs | 0.1991 μs |  1.06 |    0.03 |  0.1831 |       - |       3 KB |        1.15 |
| EmbedBatchBucketed | 1          |     6.027 μs |  0.4469 μs | 0.0245 μs |  1.02 |    0.01 |  0.1831 |       - |       3 KB |        1.15 |
|                    |            |              |            |           |       |         |         |         |            |             |
| **UnitLoop**           | **8**          |    **91.175 μs** |  **4.5951 μs** | **0.2519 μs** |  **1.00** |    **0.00** |  **5.7373** |  **0.1221** |   **94.76 KB** |        **1.00** |
| EmbedBatch         | 8          |    53.174 μs |  3.1630 μs | 0.1734 μs |  0.58 |    0.00 |  5.3711 |  0.2441 |   87.78 KB |        0.93 |
| EmbedBatchBucketed | 8          |    52.844 μs |  0.5791 μs | 0.0317 μs |  0.58 |    0.00 |  5.3711 |  0.2441 |   87.78 KB |        0.93 |
|                    |            |              |            |           |       |         |         |         |            |             |
| **UnitLoop**           | **32**         |   **355.566 μs** | **11.2220 μs** | **0.6151 μs** |  **1.00** |    **0.00** | **20.0195** |  **0.4883** |  **334.02 KB** |        **1.00** |
| EmbedBatch         | 32         |   186.193 μs | 18.4256 μs | 1.0100 μs |  0.52 |    0.00 | 18.5547 |  1.2207 |  306.63 KB |        0.92 |
| EmbedBatchBucketed | 32         |   191.771 μs |  8.7972 μs | 0.4822 μs |  0.54 |    0.00 | 17.8223 |  1.2207 |  293.12 KB |        0.88 |
|                    |            |              |            |           |       |         |         |         |            |             |
| **UnitLoop**           | **128**        | **1,389.276 μs** | **58.8074 μs** | **3.2234 μs** |  **1.00** |    **0.00** | **80.0781** |  **3.9063** | **1336.03 KB** |        **1.00** |
| EmbedBatch         | 128        |   765.335 μs | 30.0400 μs | 1.6466 μs |  0.55 |    0.00 | 74.2188 | 10.7422 | 1225.67 KB |        0.92 |
| EmbedBatchBucketed | 128        |   695.383 μs | 50.4979 μs | 2.7680 μs |  0.50 |    0.00 | 70.3125 |  9.7656 | 1158.15 KB |        0.87 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BkTreeBenchmarks-report-github

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

| Method             | Radius | Shape     | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------- |------- |---------- |----------:|----------:|---------:|------:|--------:|-----------:|------------:|
| **LengthFilteredScan** | **1**      | **clustered** | **131.38 ms** |  **1.351 ms** | **0.074 ms** |  **1.00** |    **0.00** |   **27.25 KB** |        **1.00** |
| TreeWithinDistance | 1      | clustered |  65.92 ms | 10.207 ms | 0.560 ms |  0.50 |    0.00 |  103.68 KB |        3.80 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **1**      | **uniform**   | **138.20 ms** | **54.221 ms** | **2.972 ms** |  **1.00** |    **0.03** |   **23.86 KB** |        **1.00** |
| TreeWithinDistance | 1      | uniform   |  61.04 ms |  9.567 ms | 0.524 ms |  0.44 |    0.01 |  116.47 KB |        4.88 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **2**      | **clustered** | **199.15 ms** |  **1.807 ms** | **0.099 ms** |  **1.00** |    **0.00** |  **103.44 KB** |        **1.00** |
| TreeWithinDistance | 2      | clustered | 240.23 ms |  4.624 ms | 0.253 ms |  1.21 |    0.00 |     259 KB |        2.50 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **2**      | **uniform**   | **204.82 ms** | **17.028 ms** | **0.933 ms** |  **1.00** |    **0.01** |   **54.65 KB** |        **1.00** |
| TreeWithinDistance | 2      | uniform   | 210.37 ms | 14.949 ms | 0.819 ms |  1.03 |    0.01 |   192.8 KB |        3.53 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **3**      | **clustered** | **250.41 ms** |  **4.893 ms** | **0.268 ms** |  **1.00** |    **0.00** |   **949.9 KB** |        **1.00** |
| TreeWithinDistance | 3      | clustered | 339.76 ms | 47.366 ms | 2.596 ms |  1.36 |    0.01 | 1366.63 KB |        1.44 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **3**      | **uniform**   | **256.46 ms** |  **3.980 ms** | **0.218 ms** |  **1.00** |    **0.00** |  **741.56 KB** |        **1.00** |
| TreeWithinDistance | 3      | uniform   | 310.25 ms | 18.977 ms | 1.040 ms |  1.21 |    0.00 | 1152.95 KB |        1.55 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **4**      | **clustered** | **293.65 ms** | **18.694 ms** | **1.025 ms** |  **1.00** |    **0.00** | **5113.56 KB** |        **1.00** |
| TreeWithinDistance | 4      | clustered | 391.61 ms | 20.324 ms | 1.114 ms |  1.33 |    0.01 |  7216.2 KB |        1.41 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **4**      | **uniform**   | **299.08 ms** | **13.015 ms** | **0.713 ms** |  **1.00** |    **0.00** | **5514.13 KB** |        **1.00** |
| TreeWithinDistance | 4      | uniform   | 395.35 ms | 39.597 ms | 2.170 ms |  1.32 |    0.01 |  7964.5 KB |        1.44 |

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

| Method | length | Mean          | Error         | StdDev     | Allocated |
|------- |------- |--------------:|--------------:|-----------:|----------:|
| **Latin**  | **1000**   |      **50.14 μs** |      **0.393 μs** |   **0.022 μs** |         **-** |
| Cjk    | 1000   |      62.62 μs |     23.655 μs |   1.297 μs |         - |
| **Latin**  | **10000**  |   **5,486.80 μs** |    **508.784 μs** |  **27.888 μs** |         **-** |
| Cjk    | 10000  |   6,647.52 μs |    592.701 μs |  32.488 μs |         - |
| **Latin**  | **65536**  | **202,113.17 μs** | **11,937.497 μs** | **654.334 μs** |         **-** |

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

| Method  | Mean     | Error    | StdDev  | Ratio | Gen0      | Allocated | Alloc Ratio |
|-------- |---------:|---------:|--------:|------:|----------:|----------:|------------:|
| Unigram | 316.1 ms | 16.35 ms | 0.90 ms |  1.00 | 1500.0000 |  30.32 MB |        1.00 |
| Bpe     | 543.3 ms | 40.51 ms | 2.22 ms |  1.72 | 7000.0000 | 112.18 MB |        3.70 |

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
| **BpeOnOnePathologicalToken** | **512**    | **103.5 μs** |  **1.82 μs** | **0.10 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **216.6 μs** | **18.72 μs** | **1.03 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **477.0 μs** | **11.90 μs** | **0.65 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **985.0 μs** | **81.57 μs** | **4.47 μs** | **7.8125** | **157.03 KB** |

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

| Method     | Alphabet | Mean       | Error     | StdDev    | Allocated |
|----------- |--------- |-----------:|----------:|----------:|----------:|
| **DpGroup**    | **cjk**      |  **19.338 μs** |  **5.879 μs** | **0.3222 μs** |         **-** |
| MyersGroup | cjk      | 238.995 μs | 25.976 μs | 1.4238 μs |         - |
| **DpGroup**    | **latin**    |   **9.876 μs** |  **2.805 μs** | **0.1538 μs** |         **-** |
| MyersGroup | latin    | 130.730 μs |  5.949 μs | 0.3261 μs |         - |

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
| TruncatedSvd_Rank20                       |  28.62 ms |  3.346 ms | 0.183 ms |  1.00 |    0.01 |
| Nmf_Rank20                                | 205.10 ms | 41.921 ms | 2.298 ms |  7.17 |    0.08 |
| MlNet_ProjectToPrincipalComponents_Rank20 |  24.07 ms |  1.707 ms | 0.094 ms |  0.84 |    0.01 |

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
| Ratio          |     96.43 ns |   1.314 ns |  0.072 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 11,350.22 ns | 361.052 ns | 19.790 ns | 117.71 |    0.19 |      - |         - |          NA |
| TokenSortRatio |  1,040.56 ns | 260.321 ns | 14.269 ns |  10.79 |    0.13 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  1,134.00 ns | 332.205 ns | 18.209 ns |  11.76 |    0.16 | 0.0858 |    1448 B |          NA |
| WRatio         |  2,254.30 ns | 208.745 ns | 11.442 ns |  23.38 |    0.10 | 0.1640 |    2760 B |          NA |

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
| **Lodestar**   | **Ratio**         |     **97.71 ns** |     **0.394 ns** |  **0.022 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| FuzzySharp | Ratio         |    212.02 ns |     3.875 ns |  0.212 ns |  2.17 | 0.0048 |      80 B |          NA |
|            |               |              |              |           |       |        |           |             |
| **Lodestar**   | **PartialRatio**  | **11,360.93 ns** |    **77.125 ns** |  **4.227 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| FuzzySharp | PartialRatio  |  9,703.38 ns | 1,051.427 ns | 57.632 ns |  0.85 |      - |     160 B |          NA |
|            |               |              |              |           |       |        |           |             |
| **Lodestar**   | **TokenSetRatio** |  **1,166.29 ns** |   **116.269 ns** |  **6.373 ns** |  **1.00** | **0.0858** |    **1448 B** |        **1.00** |
| FuzzySharp | TokenSetRatio |  2,097.68 ns |    71.263 ns |  3.906 ns |  1.80 | 0.1144 |    1944 B |        1.34 |
|            |               |              |              |           |       |        |           |             |
| **Lodestar**   | **WRatio**        |  **2,339.20 ns** |    **71.058 ns** |  **3.895 ns** |  **1.00** | **0.1640** |    **2760 B** |        **1.00** |
| FuzzySharp | WRatio        |  4,834.59 ns |   302.575 ns | 16.585 ns |  2.07 | 0.1831 |    3128 B |        1.13 |

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
| **Distance_Utf16**             | **8**      |      **26.63 ns** |     **1.513 ns** |   **0.083 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     128.38 ns |     2.435 ns |   0.133 ns |  4.82 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      26.78 ns |     0.111 ns |   0.006 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.58 ns |     6.527 ns |   0.358 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.32 ns** |     **8.284 ns** |   **0.454 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     152.77 ns |     3.880 ns |   0.213 ns |  5.40 |    0.07 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.51 ns |     0.658 ns |   0.036 ns |  1.01 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      27.87 ns |     0.160 ns |   0.009 ns |  0.98 |    0.01 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.54 ns** |     **0.192 ns** |   **0.011 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     163.84 ns |     2.710 ns |   0.149 ns |  5.36 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.82 ns |     1.033 ns |   0.057 ns |  1.04 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      30.44 ns |     6.456 ns |   0.354 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **36.20 ns** |     **0.674 ns** |   **0.037 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     180.18 ns |    12.046 ns |   0.660 ns |  4.98 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      35.58 ns |     0.538 ns |   0.029 ns |  0.98 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      32.80 ns |     0.600 ns |   0.033 ns |  0.91 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **54.71 ns** |     **0.166 ns** |   **0.009 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     703.36 ns |    28.761 ns |   1.577 ns | 12.86 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      56.54 ns |     0.272 ns |   0.015 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      59.25 ns |    24.822 ns |   1.361 ns |  1.08 |    0.02 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **61.16 ns** |     **2.075 ns** |   **0.114 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,026.00 ns |   129.539 ns |   7.100 ns | 16.77 |    0.10 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      65.28 ns |     2.600 ns |   0.143 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      58.89 ns |     1.026 ns |   0.056 ns |  0.96 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **955.23 ns** |   **230.683 ns** |  **12.645 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  21,659.21 ns | 1,773.227 ns |  97.197 ns | 22.68 |    0.27 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     905.33 ns |    33.680 ns |   1.846 ns |  0.95 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     924.75 ns |     3.991 ns |   0.219 ns |  0.97 |    0.01 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **7,734.47 ns** |   **716.779 ns** |  **39.289 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 346,391.40 ns | 8,475.624 ns | 464.578 ns | 44.79 |    0.20 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   7,701.56 ns |    42.089 ns |   2.307 ns |  1.00 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   7,702.25 ns |    26.638 ns |   1.460 ns |  1.00 |    0.00 |         - |          NA |

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
| **Dp**         | **8**    |    **133.20 ns** |    **22.202 ns** |   **1.217 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 8    |     55.53 ns |     0.449 ns |   0.025 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    131.88 ns |     4.414 ns |   0.242 ns |  0.99 |    0.01 |         - |          NA |
| Kernel_Cjk | 8    |     97.53 ns |     0.716 ns |   0.039 ns |  0.73 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **229.42 ns** |   **106.653 ns** |   **5.846 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 12   |     61.55 ns |     0.847 ns |   0.046 ns |  0.27 |    0.01 |         - |          NA |
| Dp_Cjk     | 12   |    217.12 ns |    68.987 ns |   3.781 ns |  0.95 |    0.03 |         - |          NA |
| Kernel_Cjk | 12   |    110.09 ns |     1.006 ns |   0.055 ns |  0.48 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **14**   |    **287.31 ns** |   **264.217 ns** |  **14.483 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel     | 14   |     66.86 ns |     0.449 ns |   0.025 ns |  0.23 |    0.01 |         - |          NA |
| Dp_Cjk     | 14   |    277.80 ns |   170.414 ns |   9.341 ns |  0.97 |    0.05 |         - |          NA |
| Kernel_Cjk | 14   |    113.73 ns |     0.917 ns |   0.050 ns |  0.40 |    0.02 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **363.99 ns** |   **114.181 ns** |   **6.259 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 16   |     72.68 ns |    18.186 ns |   0.997 ns |  0.20 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    352.67 ns |   107.568 ns |   5.896 ns |  0.97 |    0.02 |         - |          NA |
| Kernel_Cjk | 16   |    120.83 ns |     0.572 ns |   0.031 ns |  0.33 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **18**   |    **441.10 ns** |   **113.125 ns** |   **6.201 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 18   |     73.93 ns |     2.742 ns |   0.150 ns |  0.17 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    437.01 ns |    31.680 ns |   1.736 ns |  0.99 |    0.01 |         - |          NA |
| Kernel_Cjk | 18   |    125.31 ns |     5.437 ns |   0.298 ns |  0.28 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **20**   |    **767.08 ns** |   **170.554 ns** |   **9.349 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 20   |     79.85 ns |     1.312 ns |   0.072 ns |  0.10 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    764.27 ns |   317.618 ns |  17.410 ns |  1.00 |    0.02 |         - |          NA |
| Kernel_Cjk | 20   |    127.54 ns |     0.478 ns |   0.026 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **993.96 ns** |   **158.492 ns** |   **8.687 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 24   |     88.55 ns |     1.493 ns |   0.082 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    995.53 ns |   146.497 ns |   8.030 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 24   |    141.74 ns |    14.208 ns |   0.779 ns |  0.14 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,569.27 ns** |   **123.722 ns** |   **6.782 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 32   |    106.06 ns |     5.720 ns |   0.314 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,529.99 ns |   144.476 ns |   7.919 ns |  0.97 |    0.01 |         - |          NA |
| Kernel_Cjk | 32   |    166.30 ns |     1.688 ns |   0.093 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,160.26 ns** | **1,024.228 ns** |  **56.141 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 48   |    131.19 ns |     1.238 ns |   0.068 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,206.93 ns | 2,555.720 ns | 140.088 ns |  1.01 |    0.04 |         - |          NA |
| Kernel_Cjk | 48   |    245.52 ns |     4.890 ns |   0.268 ns |  0.08 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **5,353.11 ns** | **8,239.870 ns** | **451.655 ns** |  **1.00** |    **0.10** |         **-** |          **NA** |
| Kernel     | 64   |    155.45 ns |     1.538 ns |   0.084 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,165.51 ns |   367.301 ns |  20.133 ns |  1.16 |    0.08 |         - |          NA |
| Kernel_Cjk | 64   |    289.99 ns |     4.964 ns |   0.272 ns |  0.05 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **12,397.68 ns** | **3,720.953 ns** | **203.958 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 96   |    789.08 ns |     2.772 ns |   0.152 ns |  0.06 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 13,668.50 ns |   804.193 ns |  44.081 ns |  1.10 |    0.02 |         - |          NA |
| Kernel_Cjk | 96   |  1,010.28 ns |    63.471 ns |   3.479 ns |  0.08 |    0.00 |         - |          NA |

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

| Method                     | Length | Mean         | Error      | StdDev    | Ratio | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-----------:|----------:|------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **24.66 ns** |   **0.298 ns** |  **0.016 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    118.87 ns |   2.905 ns |  0.159 ns |  4.82 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.26 ns |   0.092 ns |  0.005 ns |  1.02 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **64**     |    **272.05 ns** |   **8.523 ns** |  **0.467 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    666.71 ns |  61.370 ns |  3.364 ns |  2.45 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    273.10 ns |  20.618 ns |  1.130 ns |  1.00 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **512**    | **14,170.57 ns** |  **33.453 ns** |  **1.834 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 16,753.34 ns | 284.945 ns | 15.619 ns |  1.18 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,620.75 ns | 340.948 ns | 18.689 ns |  1.03 |         - |          NA |

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

| Method             | Length | Distinct | Mean         | Error         | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |-------------:|--------------:|------------:|------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |     **330.6 ns** |      **13.58 ns** |     **0.74 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     239.8 ns |      14.57 ns |     0.80 ns |  0.73 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **331.9 ns** |       **2.77 ns** |     **0.15 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     241.3 ns |      26.12 ns |     1.43 ns |  0.73 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **419.6 ns** |       **3.74 ns** |     **0.21 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     323.0 ns |       2.93 ns |     0.16 ns |  0.77 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **422.8 ns** |      **47.53 ns** |     **2.61 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     324.0 ns |     164.85 ns |     9.04 ns |  0.77 |    0.02 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **497.2 ns** |       **4.85 ns** |     **0.27 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     418.5 ns |      66.42 ns |     3.64 ns |  0.84 |    0.01 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **503.8 ns** |       **4.75 ns** |     **0.26 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     417.7 ns |       1.08 ns |     0.06 ns |  0.83 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **587.1 ns** |      **89.36 ns** |     **4.90 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,300.4 ns |     151.28 ns |     8.29 ns |  2.22 |    0.02 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **603.3 ns** |       **4.19 ns** |     **0.23 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,293.4 ns |     207.36 ns |    11.37 ns |  2.14 |    0.02 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,585.5 ns** |     **985.20 ns** |    **54.00 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,542.3 ns |     171.99 ns |     9.43 ns |  2.14 |    0.04 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,566.6 ns** |     **296.01 ns** |    **16.23 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,423.6 ns |     626.15 ns |    34.32 ns |  2.11 |    0.02 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **18,339.0 ns** |     **268.49 ns** |    **14.72 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  64,313.3 ns |   1,055.14 ns |    57.84 ns |  3.51 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **482,609.5 ns** | **137,627.59 ns** | **7,543.83 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  65,609.3 ns |   4,248.28 ns |   232.86 ns |  0.14 |    0.00 |         - |          NA |

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

| Method               | Length | Mean          | Error         | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |------- |--------------:|--------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| **Lodestar**             | **8**      |      **24.48 ns** |      **0.221 ns** |   **0.012 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Fastenshtein         | 8      |      74.96 ns |      4.393 ns |   0.241 ns |  3.06 |    0.01 | 0.0033 |      56 B |          NA |
| Quickenshtein        | 8      |      81.10 ns |      1.211 ns |   0.066 ns |  3.31 |    0.00 |      - |         - |          NA |
| F23_StringSimilarity | 8      |     182.92 ns |      4.110 ns |   0.225 ns |  7.47 |    0.01 | 0.0076 |     128 B |          NA |
|                      |        |               |               |            |       |         |        |           |             |
| **Lodestar**             | **64**     |     **275.71 ns** |     **10.843 ns** |   **0.594 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Fastenshtein         | 64     |   6,189.96 ns |  3,491.096 ns | 191.359 ns | 22.45 |    0.60 | 0.0153 |     280 B |          NA |
| Quickenshtein        | 64     |   1,420.40 ns |     16.402 ns |   0.899 ns |  5.15 |    0.01 |      - |         - |          NA |
| F23_StringSimilarity | 64     |  11,190.47 ns |    776.624 ns |  42.569 ns | 40.59 |    0.15 | 0.0305 |     576 B |          NA |
|                      |        |               |               |            |       |         |        |           |             |
| **Lodestar**             | **512**    |  **14,273.45 ns** |    **260.295 ns** |  **14.268 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Fastenshtein         | 512    | 427,384.33 ns | 10,695.151 ns | 586.237 ns | 29.94 |    0.04 |      - |    2072 B |          NA |
| Quickenshtein        | 512    |  37,311.35 ns |    811.268 ns |  44.468 ns |  2.61 |    0.00 |      - |         - |          NA |
| F23_StringSimilarity | 512    | 713,556.64 ns | 11,443.960 ns | 627.282 ns | 49.99 |    0.06 |      - |    4161 B |          NA |

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

| Method         | Samples | Classes | Mean            | Error           | StdDev       | Gen0   | Allocated |
|--------------- |-------- |-------- |----------------:|----------------:|-------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |      **7,616.8 ns** |       **560.38 ns** |     **30.72 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7,867.6 ns |       285.52 ns |     15.65 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |        922.8 ns |        80.62 ns |      4.42 ns |      - |         - |
| F1Macro        | 1000    | 2       |      7,324.9 ns |       185.73 ns |     10.18 ns | 0.0229 |     472 B |
| Report         | 1000    | 2       |     10,371.1 ns |       190.37 ns |     10.44 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **8,047.7 ns** |       **272.60 ns** |     **14.94 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7,590.5 ns |       169.44 ns |      9.29 ns | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |        917.9 ns |         6.41 ns |      0.35 ns |      - |         - |
| F1Macro        | 1000    | 10      |      7,994.9 ns |       191.67 ns |     10.51 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     15,469.4 ns |       272.29 ns |     14.92 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **870,327.5 ns** |   **110,910.30 ns** |  **6,079.37 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    813,986.4 ns |    15,678.27 ns |    859.38 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    163,492.0 ns |     8,276.68 ns |    453.67 ns |      - |         - |
| F1Macro        | 100000  | 2       |    880,525.6 ns |    17,770.17 ns |    974.04 ns |      - |     473 B |
| Report         | 100000  | 2       |    868,675.9 ns |    84,822.41 ns |  4,649.40 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **975,209.5 ns** |    **46,902.82 ns** |  **2,570.90 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    943,642.6 ns |    48,707.57 ns |  2,669.83 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    263,274.0 ns |     3,401.16 ns |    186.43 ns |      - |         - |
| F1Macro        | 100000  | 10      |    974,037.7 ns |    39,294.77 ns |  2,153.88 ns |      - |    1665 B |
| Report         | 100000  | 10      |  1,002,731.1 ns |    59,172.83 ns |  3,243.46 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,828,883.2 ns** | **1,135,798.78 ns** | **62,256.96 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,429,557.4 ns |   448,648.10 ns | 24,591.92 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,744,167.0 ns |     6,084.57 ns |    333.52 ns |      - |         - |
| F1Macro        | 1000000 | 2       |  8,365,775.8 ns |   417,610.93 ns | 22,890.66 ns |      - |     484 B |
| Report         | 1000000 | 2       |  8,538,537.9 ns |    79,100.89 ns |  4,335.79 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **10,035,002.3 ns** |   **226,659.39 ns** | **12,423.97 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      |  9,409,085.9 ns | 1,254,608.35 ns | 68,769.32 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  2,686,566.8 ns |    16,541.55 ns |    906.70 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 10,000,839.3 ns |    63,024.94 ns |  3,454.61 ns |      - |    1676 B |
| Report         | 1000000 | 10      |  9,839,306.6 ns |    59,832.35 ns |  3,279.61 ns |      - |   15892 B |

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
| **Lodestar** | **100000**  | **Bundle**        |   **8,109.6 μs** |     **84.32 μs** |     **4.62 μs** |   **1.00** |    **0.00** |        **-** |        **-** |        **-** |     **1016 B** |        **1.00** |
| MlNet    | 100000  | Bundle        |  33,839.7 μs |  1,491.17 μs |    81.74 μs |   4.17 |    0.01 | 600.0000 | 600.0000 | 600.0000 |  5089210 B |    5,009.06 |
|          |         |               |              |              |             |        |         |          |          |          |            |             |
| **Lodestar** | **100000**  | **AccuracyAlone** |     **258.0 μs** |     **20.41 μs** |     **1.12 μs** |   **1.00** |    **0.01** |        **-** |        **-** |        **-** |          **-** |          **NA** |
| MlNet    | 100000  | AccuracyAlone |  34,525.5 μs |    172.87 μs |     9.48 μs | 133.83 |    0.50 | 666.6667 | 666.6667 | 666.6667 |  5089833 B |          NA |
|          |         |               |              |              |             |        |         |          |          |          |            |             |
| **Lodestar** | **1000000** | **Bundle**        | **103,325.5 μs** | **50,635.55 μs** | **2,775.51 μs** |   **1.00** |    **0.03** |        **-** |        **-** |        **-** |          **-** |          **NA** |
| MlNet    | 1000000 | Bundle        | 220,544.2 μs |  5,120.47 μs |   280.67 μs |   2.14 |    0.05 |        - |        - |        - | 23232104 B |          NA |
|          |         |               |              |              |             |        |         |          |          |          |            |             |
| **Lodestar** | **1000000** | **AccuracyAlone** |   **2,603.8 μs** |     **67.39 μs** |     **3.69 μs** |   **1.00** |    **0.00** |        **-** |        **-** |        **-** |          **-** |          **NA** |
| MlNet    | 1000000 | AccuracyAlone | 219,803.4 μs |  3,646.07 μs |   199.85 μs |  84.42 |    0.12 |        - |        - |        - | 23231816 B |          NA |

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

| Method     | Band | Mean         | Error         | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|--------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **4**    |     **73.53 ns** |      **2.067 ns** |   **0.113 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 4    |     73.11 ns |      0.980 ns |   0.054 ns |  0.99 |    0.00 |         - |          NA |
| Dp_Cjk     | 4    |     73.38 ns |      1.713 ns |   0.094 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 4    |     73.17 ns |      0.896 ns |   0.049 ns |  1.00 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **6**    |    **103.81 ns** |      **1.809 ns** |   **0.099 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 6    |     77.61 ns |      0.750 ns |   0.041 ns |  0.75 |    0.00 |         - |          NA |
| Dp_Cjk     | 6    |    104.16 ns |      3.293 ns |   0.180 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 6    |    130.82 ns |     44.411 ns |   2.434 ns |  1.26 |    0.02 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **8**    |    **156.37 ns** |      **4.009 ns** |   **0.220 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     88.51 ns |      4.718 ns |   0.259 ns |  0.57 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    157.26 ns |      1.256 ns |   0.069 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    182.88 ns |      3.524 ns |   0.193 ns |  1.17 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **10**   |    **196.72 ns** |      **3.854 ns** |   **0.211 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 10   |     95.34 ns |      2.818 ns |   0.154 ns |  0.48 |    0.00 |         - |          NA |
| Dp_Cjk     | 10   |    196.49 ns |      3.421 ns |   0.188 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 10   |    149.28 ns |      5.605 ns |   0.307 ns |  0.76 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **12**   |    **247.15 ns** |     **71.500 ns** |   **3.919 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 12   |    102.84 ns |      0.774 ns |   0.042 ns |  0.42 |    0.01 |         - |          NA |
| Dp_Cjk     | 12   |    244.92 ns |      2.771 ns |   0.152 ns |  0.99 |    0.01 |         - |          NA |
| Kernel_Cjk | 12   |    176.71 ns |      5.128 ns |   0.281 ns |  0.72 |    0.01 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **16**   |    **419.81 ns** |    **354.451 ns** |  **19.429 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel     | 16   |    124.10 ns |     19.942 ns |   1.093 ns |  0.30 |    0.01 |         - |          NA |
| Dp_Cjk     | 16   |    446.76 ns |    100.737 ns |   5.522 ns |  1.07 |    0.04 |         - |          NA |
| Kernel_Cjk | 16   |    182.07 ns |      4.103 ns |   0.225 ns |  0.43 |    0.02 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **24**   |    **847.65 ns** |    **178.995 ns** |   **9.811 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 24   |    158.89 ns |      5.917 ns |   0.324 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    857.61 ns |    246.822 ns |  13.529 ns |  1.01 |    0.02 |         - |          NA |
| Kernel_Cjk | 24   |    224.52 ns |      9.206 ns |   0.505 ns |  0.26 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **32**   |  **1,449.29 ns** |      **9.508 ns** |   **0.521 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    196.02 ns |     15.420 ns |   0.845 ns |  0.14 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,449.85 ns |     12.457 ns |   0.683 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 32   |    269.53 ns |      4.986 ns |   0.273 ns |  0.19 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **48**   |  **3,604.33 ns** |  **1,685.781 ns** |  **92.403 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 48   |    265.73 ns |      1.106 ns |   0.061 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,298.26 ns |     48.735 ns |   2.671 ns |  0.92 |    0.02 |         - |          NA |
| Kernel_Cjk | 48   |    356.81 ns |      5.350 ns |   0.293 ns |  0.10 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **64**   |  **6,246.89 ns** |  **1,404.536 ns** |  **76.987 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 64   |    342.10 ns |     46.631 ns |   2.556 ns |  0.05 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,207.93 ns |    148.520 ns |   8.141 ns |  0.99 |    0.01 |         - |          NA |
| Kernel_Cjk | 64   |    449.68 ns |    193.313 ns |  10.596 ns |  0.07 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **96**   | **14,515.90 ns** | **15,300.249 ns** | **838.658 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel     | 96   |  1,256.67 ns |    334.241 ns |  18.321 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 14,381.11 ns | 12,720.399 ns | 697.248 ns |  0.99 |    0.06 |         - |          NA |
| Kernel_Cjk | 96   |  1,489.33 ns |    347.052 ns |  19.023 ns |  0.10 |    0.01 |         - |          NA |

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
| VocabTxt               |  4.486 ms | 2.1940 ms | 0.1203 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.151 ms | 2.3748 ms | 0.1302 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.570 ms | 0.4617 ms | 0.0253 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.815 ms | 3.6822 ms | 0.2018 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.817 ms | 0.4209 ms | 0.0231 ms |  27.3438 |  21.4844 |  21.4844 |   2.09 MB |
| TfidfLoad              |  5.427 ms | 0.0943 ms | 0.0052 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  4.020 ms | 0.4362 ms | 0.0239 ms | 273.4375 | 273.4375 | 273.4375 |  19.87 MB |
| EmbeddingIndexLoad     |  5.373 ms | 0.9080 ms | 0.0498 ms | 203.1250 | 171.8750 | 140.6250 |  15.72 MB |

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

| Method               | Documents | Mean      | Error     | StdDev    | Ratio | Gen0      | Gen1      | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|----------:|----------:|------:|----------:|----------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.057 ms** | **0.5691 ms** | **0.0312 ms** |  **1.00** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  5.956 ms | 0.4572 ms | 0.0251 ms |  0.84 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.047 ms | 0.8669 ms | 0.0475 ms |  1.00 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.271 ms | 0.1527 ms | 0.0084 ms |  0.89 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |           |          |           |             |
| **Count**                | **1000**      | **28.697 ms** | **0.9627 ms** | **0.0528 ms** |  **1.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 22.631 ms | 3.8346 ms | 0.2102 ms |  0.79 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 27.373 ms | 3.8727 ms | 0.2123 ms |  0.95 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 23.594 ms | 0.4701 ms | 0.0258 ms |  0.82 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

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
| **Lodestar**     | **WordPiece**     |  **61.42 ms** |  **1.640 ms** | **0.090 ms** |  **1.00** | **4222.2222** | **111.1111** |  **68.25 MB** |        **1.00** |
| MlTokenizers | WordPiece     |  53.11 ms |  0.513 ms | 0.028 ms |  0.86 |  200.0000 |        - |   3.55 MB |        0.05 |
|              |               |           |           |          |       |           |          |           |             |
| **Lodestar**     | **SentencePiece** | **326.25 ms** |  **5.856 ms** | **0.321 ms** |  **1.00** | **1500.0000** |        **-** |  **30.33 MB** |        **1.00** |
| MlTokenizers | SentencePiece |  50.55 ms | 13.135 ms | 0.720 ms |  0.15 |  100.0000 |        - |   3.09 MB |        0.10 |

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

| Method | Dim  | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
| **Dot**    | **384**  |  **48.31 ns** | **11.418 ns** | **0.626 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| L2Norm | 384  |  50.51 ns |  0.452 ns | 0.025 ns |  1.05 |    0.01 |         - |          NA |
|        |      |           |           |          |       |         |           |             |
| **Dot**    | **768**  |  **95.44 ns** | **16.983 ns** | **0.931 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| L2Norm | 768  |  92.43 ns |  2.325 ns | 0.127 ns |  0.97 |    0.01 |         - |          NA |
|        |      |           |           |          |       |         |           |             |
| **Dot**    | **1024** | **124.33 ns** |  **5.909 ns** | **0.324 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| L2Norm | 1024 | 183.46 ns | 10.760 ns | 0.590 ns |  1.48 |    0.01 |         - |          NA |

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
| **Count**        | **200**       |  **2.958 ms** | **1.4669 ms** | **0.0804 ms** |  **1.00** |    **0.03** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  2.864 ms | 1.6200 ms | 0.0888 ms |  0.97 |    0.03 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.613 ms | 0.2222 ms | 0.0122 ms |  1.22 |    0.03 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.798 ms | 0.1267 ms | 0.0069 ms |  0.95 |    0.02 |  97.6563 |  31.2500 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.792 ms** | **0.5053 ms** | **0.0277 ms** |  **1.00** |    **0.00** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  6.958 ms | 0.5623 ms | 0.0308 ms |  1.02 |    0.01 | 492.1875 | 304.6875 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 10.893 ms | 0.4931 ms | 0.0270 ms |  1.60 |    0.01 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  6.701 ms | 0.4985 ms | 0.0273 ms |  0.99 |    0.00 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

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
| **Lodestar** | **200**       |   **7.037 ms** |   **0.4192 ms** |  **0.0230 ms** |  **1.00** |    **0.00** |   **296.8750** |   **218.7500** |   **140.6250** |   **5.13 MB** |        **1.00** |
| MlNet    | 200       |  63.299 ms | 289.2402 ms | 15.8542 ms |  8.99 |    1.95 |  7500.0000 |  7500.0000 |  7500.0000 |  28.27 MB |        5.52 |
|          |           |            |             |            |       |         |            |            |            |           |             |
| **Lodestar** | **1000**      |  **29.487 ms** |   **5.9944 ms** |  **0.3286 ms** |  **1.00** |    **0.01** |  **2687.5000** |  **2468.7500** |  **1500.0000** |  **24.92 MB** |        **1.00** |
| MlNet    | 1000      | 373.533 ms |  58.7671 ms |  3.2212 ms | 12.67 |    0.16 | 78000.0000 | 78000.0000 | 78000.0000 |  324.7 MB |       13.03 |

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
| latin | 8 | 115.6 | 25.7 | 4.49x C# faster |
| latin | 32 | 177.3 | 85.0 | 2.09x C# faster |
| latin | 128 | 472.5 | 899.3 | 1.90x Py faster |
| latin | 512 | 4440.5 | 7647.1 | 1.72x Py faster |
| cjk | 8 | 139.8 | 25.6 | 5.47x C# faster |
| cjk | 32 | 329.7 | 234.1 | 1.41x C# faster |
| cjk | 128 | 1903.0 | 1621.6 | 1.17x C# faster |
| cjk | 512 | 14884.0 | 11005.8 | 1.35x C# faster |

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
| latin | 8 | 156.2 | 17.8 | 8.78x C# faster |
| latin | 32 | 291.2 | 156.9 | 1.86x C# faster |
| latin | 128 | 1702.9 | 1574.1 | 1.08x C# faster |
| latin | 512 | 14004.0 | 16058.5 | 1.15x Py faster |
| cjk | 8 | 150.4 | 17.8 | 8.45x C# faster |
| cjk | 32 | 370.0 | 283.5 | 1.31x C# faster |
| cjk | 128 | 2832.1 | 2391.5 | 1.18x C# faster |
| cjk | 512 | 23591.3 | 19537.2 | 1.21x C# faster |

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
| confusion_matrix_n1000_k2 | 0.009 | 0.957 | 103.69x | 0.009 | 0.956 | 103.68x |
| accuracy_n1000_k2 | 0.001 | 0.503 | 538.84x | 0.001 | 0.503 | 538.88x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.720 | 221.44x | 0.008 | 1.720 | 221.42x |
| classification_report_n1000_k2 | 0.011 | 6.541 | 616.75x | 0.011 | 6.541 | 616.69x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.880 | 119.47x | 0.016 | 1.880 | 119.46x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.025 | 125.36x | 0.008 | 1.025 | 125.32x |
| matthews_n1000_k2 | 0.008 | 1.932 | 252.71x | 0.008 | 1.932 | 252.70x |
| cohen_kappa_n1000_k2 | 0.008 | 1.080 | 141.33x | 0.008 | 1.080 | 141.33x |
| mse_n1000_k2 | 0.002 | 0.299 | 123.60x | 0.002 | 0.299 | 123.59x |
| mae_n1000_k2 | 0.002 | 0.298 | 123.17x | 0.002 | 0.298 | 123.18x |
| median_ae_n1000_k2 | 0.007 | 0.310 | 46.97x | 0.007 | 0.309 | 46.98x |
| r2_n1000_k2 | 0.003 | 0.370 | 143.48x | 0.003 | 0.370 | 143.48x |
| confusion_matrix_n1000_k10 | 0.009 | 0.977 | 103.19x | 0.009 | 0.977 | 103.20x |
| accuracy_n1000_k10 | 0.001 | 0.519 | 555.51x | 0.001 | 0.519 | 555.54x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.780 | 211.69x | 0.008 | 1.780 | 211.71x |
| classification_report_n1000_k10 | 0.015 | 6.896 | 448.10x | 0.015 | 6.895 | 448.10x |
| roc_auc_ovr_macro_n1000_k10 | 0.547 | 9.812 | 17.94x | 0.547 | 9.811 | 17.94x |
| balanced_accuracy_n1000_k10 | 0.009 | 1.049 | 113.20x | 0.009 | 1.049 | 113.22x |
| matthews_n1000_k10 | 0.008 | 1.994 | 239.54x | 0.008 | 1.994 | 239.55x |
| cohen_kappa_n1000_k10 | 0.009 | 1.092 | 126.46x | 0.009 | 1.092 | 126.47x |
| mse_n1000_k10 | 0.002 | 0.305 | 126.08x | 0.002 | 0.305 | 126.09x |
| mae_n1000_k10 | 0.002 | 0.304 | 125.87x | 0.002 | 0.304 | 125.87x |
| median_ae_n1000_k10 | 0.007 | 0.318 | 48.32x | 0.007 | 0.318 | 48.23x |
| r2_n1000_k10 | 0.003 | 0.368 | 143.30x | 0.003 | 0.368 | 143.32x |
| confusion_matrix_n100000_k2 | 0.984 | 10.721 | 10.89x | 0.984 | 10.720 | 10.89x |
| accuracy_n100000_k2 | 0.167 | 3.747 | 22.41x | 0.167 | 3.747 | 22.41x |
| precision_recall_f1_macro_n100000_k2 | 0.864 | 12.284 | 14.22x | 0.864 | 12.283 | 14.22x |
| classification_report_n100000_k2 | 0.867 | 26.828 | 30.93x | 0.867 | 26.828 | 30.94x |
| roc_auc_binary_n100000_k2 | 3.567 | 26.814 | 7.52x | 3.566 | 26.809 | 7.52x |
| balanced_accuracy_n100000_k2 | 0.858 | 10.815 | 12.60x | 0.858 | 10.815 | 12.60x |
| matthews_n100000_k2 | 0.863 | 21.655 | 25.09x | 0.863 | 21.654 | 25.09x |
| cohen_kappa_n100000_k2 | 0.865 | 10.842 | 12.54x | 0.865 | 10.841 | 12.54x |
| mse_n100000_k2 | 0.238 | 0.436 | 1.83x | 0.238 | 0.436 | 1.83x |
| mae_n100000_k2 | 0.238 | 0.427 | 1.80x | 0.238 | 0.427 | 1.80x |
| median_ae_n100000_k2 | 0.760 | 1.774 | 2.33x | 0.779 | 1.774 | 2.28x |
| r2_n100000_k2 | 0.234 | 0.650 | 2.77x | 0.234 | 0.650 | 2.77x |
| confusion_matrix_n100000_k10 | 0.976 | 10.769 | 11.04x | 0.976 | 10.769 | 11.04x |
| accuracy_n100000_k10 | 0.277 | 3.767 | 13.58x | 0.277 | 3.767 | 13.59x |
| precision_recall_f1_macro_n100000_k10 | 0.983 | 13.020 | 13.25x | 0.983 | 13.019 | 13.25x |
| classification_report_n100000_k10 | 0.987 | 29.491 | 29.89x | 0.987 | 29.488 | 29.89x |
| roc_auc_ovr_macro_n100000_k10 | 38.803 | 217.961 | 5.62x | 38.798 | 217.925 | 5.62x |
| balanced_accuracy_n100000_k10 | 0.977 | 10.886 | 11.14x | 0.977 | 10.885 | 11.14x |
| matthews_n100000_k10 | 0.980 | 22.410 | 22.87x | 0.980 | 22.407 | 22.87x |
| cohen_kappa_n100000_k10 | 0.997 | 10.867 | 10.90x | 0.997 | 10.866 | 10.90x |
| mse_n100000_k10 | 0.237 | 0.441 | 1.86x | 0.237 | 0.441 | 1.86x |
| mae_n100000_k10 | 0.237 | 0.434 | 1.83x | 0.237 | 0.434 | 1.83x |
| median_ae_n100000_k10 | 0.772 | 1.778 | 2.30x | 0.819 | 1.778 | 2.17x |
| r2_n100000_k10 | 0.234 | 0.658 | 2.81x | 0.234 | 0.658 | 2.81x |
| confusion_matrix_n1000000_k2 | 8.600 | 101.075 | 11.75x | 8.598 | 101.064 | 11.75x |
| accuracy_n1000000_k2 | 1.815 | 33.577 | 18.50x | 1.814 | 33.568 | 18.50x |
| precision_recall_f1_macro_n1000000_k2 | 8.641 | 109.233 | 12.64x | 8.641 | 109.225 | 12.64x |
| classification_report_n1000000_k2 | 8.682 | 213.219 | 24.56x | 8.682 | 213.197 | 24.56x |
| roc_auc_binary_n1000000_k2 | 49.016 | 296.011 | 6.04x | 49.011 | 295.972 | 6.04x |
| balanced_accuracy_n1000000_k2 | 8.605 | 100.661 | 11.70x | 8.604 | 100.651 | 11.70x |
| matthews_n1000000_k2 | 8.686 | 201.459 | 23.19x | 8.685 | 201.456 | 23.20x |
| cohen_kappa_n1000000_k2 | 8.690 | 100.517 | 11.57x | 8.689 | 100.511 | 11.57x |
| mse_n1000000_k2 | 2.388 | 2.570 | 1.08x | 2.388 | 2.570 | 1.08x |
| mae_n1000000_k2 | 2.390 | 2.549 | 1.07x | 2.389 | 2.549 | 1.07x |
| median_ae_n1000000_k2 | 7.156 | 14.549 | 2.03x | 7.274 | 14.547 | 2.00x |
| r2_n1000000_k2 | 2.377 | 5.344 | 2.25x | 2.376 | 5.343 | 2.25x |
| confusion_matrix_n1000000_k10 | 9.763 | 101.520 | 10.40x | 9.762 | 101.507 | 10.40x |
| accuracy_n1000000_k10 | 2.779 | 33.684 | 12.12x | 2.779 | 33.676 | 12.12x |
| precision_recall_f1_macro_n1000000_k10 | 9.904 | 115.490 | 11.66x | 9.904 | 115.473 | 11.66x |
| classification_report_n1000000_k10 | 9.856 | 239.405 | 24.29x | 9.855 | 239.361 | 24.29x |
| balanced_accuracy_n1000000_k10 | 9.801 | 101.737 | 10.38x | 9.800 | 101.726 | 10.38x |
| matthews_n1000000_k10 | 9.839 | 211.604 | 21.51x | 9.837 | 211.597 | 21.51x |
| cohen_kappa_n1000000_k10 | 9.828 | 101.823 | 10.36x | 9.827 | 101.813 | 10.36x |
| mse_n1000000_k10 | 2.399 | 2.886 | 1.20x | 2.399 | 2.886 | 1.20x |
| mae_n1000000_k10 | 2.382 | 2.933 | 1.23x | 2.382 | 2.932 | 1.23x |
| median_ae_n1000000_k10 | 6.931 | 14.946 | 2.16x | 6.977 | 14.944 | 2.14x |
| r2_n1000000_k10 | 2.674 | 5.235 | 1.96x | 2.674 | 5.235 | 1.96x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

<!-- markdownlint-enable MD060 -->

### compare-persistence

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.375 | 9.776 | 2.23x | 4.656 | 9.776 | 2.10x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.271 | 16.290 | 1.33x | 12.592 | 16.289 | 1.29x | 706,526 | 706,526 |
| tokenizer_json_unigram | 13.871 | 38.161 | 2.75x | 14.198 | 38.155 | 2.69x | 1,990,038 | 1,990,038 |
| spiece_model | 5.006 | 28.189 | 5.63x | 5.232 | 28.186 | 5.39x | 533,084 | 533,084 |
| tfidf_save | 1.631 | 2.456 | 1.51x | 1.644 | 2.455 | 1.49x | 581,787 | 591,922 |
| tfidf_load | 4.677 | 4.015 | 0.86x | 4.890 | 4.015 | 0.82x | 581,787 | 591,922 |
| embedding_index_save | 4.360 | 1.569 | 0.36x | 4.561 | 1.569 | 0.34x | 20,589,007 | 15,360,128 |
| embedding_index_save_file | 49.356 | 37.230 | 0.75x | 10.937 | 5.072 | 0.46x | 20,589,007 | 15,360,128 |
| embedding_index_load | 5.432 | 1.626 | 0.30x | 5.808 | 1.625 | 0.28x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 6.135 | 1.132 | 0.18x | 6.455 | 1.131 | 0.18x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 4.585 | 1.636 | 0.36x | 4.878 | 1.636 | 0.34x | 20,589,007 | 15,360,128 |
| embedding_index_ingest_npy | 1.367 | 1.607 | 1.18x | 1.501 | 1.607 | 1.07x | 15,360,128 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 91.74x | 0.000 | 0.001 | 91.75x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 409.817 | 555.601 | 1.36x | 409.902 | 555.559 | 1.36x | 15,250,490 | 14,022,374 |
| embedding_index_load_gzip | 78.289 | 69.980 | 0.89x | 79.635 | 69.972 | 0.88x | 15,250,490 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
