# Nightly benchmark run

<!-- nightly-baseline: 2483f1a00691271083f00baf3835de96bf0a4076 -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `2483f1a00691271083f00baf3835de96bf0a4076`
- Previous run: `2483f1a00691271083f00baf3835de96bf0a4076`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
- `BlockedTableBenchmarks`
- `BpeBenchmarks`
- `BpeScalingBenchmarks`
- `BucketRouteDiagnostics`
- `FuzzBenchmarks`
- `IndelBenchmarks`
- `LcsGateBenchmarks`
- `LevenshteinBenchmarks`
- `LevenshteinCodePointBenchmarks`
- `MetricsBenchmarks`
- `MyersGateBenchmarks`
- `PersistenceBenchmarks`
- `StopWordBenchmarks`
- `VectorMathBenchmarks`
- `VectorizerBenchmarks`

### Lodestar.Text.Benchmarks.BatchEmbeddingBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | CorpusSize | Mean         | Error       | StdDev     | Ratio | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|-----------:|------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **5.715 μs** |   **0.2390 μs** |  **0.0131 μs** |  **1.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     5.834 μs |   0.9573 μs |  0.0525 μs |  1.02 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     5.920 μs |   0.1700 μs |  0.0093 μs |  1.04 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |            |       |          |         |            |             |
| **UnitLoop**           | **8**          |   **102.109 μs** |   **8.8185 μs** |  **0.4834 μs** |  **1.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    70.076 μs |   6.4254 μs |  0.3522 μs |  0.69 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    67.057 μs |   4.9788 μs |  0.2729 μs |  0.66 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |            |       |          |         |            |             |
| **UnitLoop**           | **32**         |   **370.686 μs** |  **23.9348 μs** |  **1.3119 μs** |  **1.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   241.612 μs |  16.1958 μs |  0.8877 μs |  0.65 |  26.8555 |  1.7090 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   226.319 μs |  13.2666 μs |  0.7272 μs |  0.61 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |             |            |       |          |         |            |             |
| **UnitLoop**           | **128**        | **1,479.332 μs** | **196.8673 μs** | **10.7910 μs** |  **1.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   928.204 μs |  88.8204 μs |  4.8685 μs |  0.63 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   870.741 μs |  63.0107 μs |  3.4538 μs |  0.59 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | length | Mean          | Error         | StdDev       | Allocated |
|------- |------- |--------------:|--------------:|-------------:|----------:|
| **Latin**  | **1000**   |      **57.89 μs** |      **1.368 μs** |     **0.075 μs** |         **-** |
| Cjk    | 1000   |      63.86 μs |      1.236 μs |     0.068 μs |         - |
| **Latin**  | **10000**  |   **5,560.06 μs** |    **203.463 μs** |    **11.152 μs** |         **-** |
| Cjk    | 10000  |   7,132.48 μs |    282.587 μs |    15.490 μs |         - |
| **Latin**  | **65536**  | **228,821.18 μs** | **27,374.117 μs** | **1,500.468 μs** |         **-** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method  | Mean     | Error    | StdDev  | Ratio | Gen0       | Allocated | Alloc Ratio |
|-------- |---------:|---------:|--------:|------:|-----------:|----------:|------------:|
| Unigram | 599.1 ms | 45.35 ms | 2.49 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 567.6 ms | 35.23 ms | 1.93 ms |  0.95 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                    | Length | Mean       | Error    | StdDev  | Gen0   | Allocated |
|-------------------------- |------- |-----------:|---------:|--------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |   **103.5 μs** |  **3.85 μs** | **0.21 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **211.0 μs** |  **7.91 μs** | **0.43 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **490.5 μs** | **57.84 μs** | **3.17 μs** | **3.9063** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,026.8 μs** | **59.38 μs** | **3.25 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Alphabet | Mean      | Error    | StdDev   | Allocated |
|----------- |--------- |----------:|---------:|---------:|----------:|
| **DpGroup**    | **cjk**      |  **19.01 μs** | **1.883 μs** | **0.103 μs** |         **-** |
| MyersGroup | cjk      | 169.25 μs | 1.589 μs | 0.087 μs |         - |
| **DpGroup**    | **latin**    |  **10.65 μs** | **0.050 μs** | **0.003 μs** |         **-** |
| MyersGroup | latin    | 111.64 μs | 0.207 μs | 0.011 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method         | Mean        | Error       | StdDev   | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |------------:|------------:|---------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |    110.2 ns |     2.44 ns |  0.13 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 13,442.7 ns |   248.97 ns | 13.65 ns | 122.02 |    0.17 |      - |         - |          NA |
| TokenSortRatio |    942.3 ns |   144.02 ns |  7.89 ns |   8.55 |    0.06 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,247.3 ns |   508.35 ns | 27.86 ns |  29.48 |    0.22 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,581.7 ns | 1,138.23 ns | 62.39 ns |  41.59 |    0.49 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                     | Length | Mean          | Error         | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|--------------:|-------------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **30.66 ns** |      **0.782 ns** |     **0.043 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     132.26 ns |      0.866 ns |     0.047 ns |  4.31 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      30.35 ns |      0.553 ns |     0.030 ns |  0.99 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      28.33 ns |      1.256 ns |     0.069 ns |  0.92 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **32.51 ns** |      **0.712 ns** |     **0.039 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     137.20 ns |      1.732 ns |     0.095 ns |  4.22 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      33.47 ns |      0.854 ns |     0.047 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      33.28 ns |      0.825 ns |     0.045 ns |  1.02 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **36.11 ns** |      **0.960 ns** |     **0.053 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     146.88 ns |     13.243 ns |     0.726 ns |  4.07 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      36.84 ns |      0.078 ns |     0.004 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      35.24 ns |      1.241 ns |     0.068 ns |  0.98 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **38.90 ns** |      **0.685 ns** |     **0.038 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     187.75 ns |      5.034 ns |     0.276 ns |  4.83 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      38.63 ns |      0.372 ns |     0.020 ns |  0.99 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      35.05 ns |      2.321 ns |     0.127 ns |  0.90 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **56.75 ns** |      **0.891 ns** |     **0.049 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     843.67 ns |    234.785 ns |    12.869 ns | 14.87 |    0.20 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      60.87 ns |      0.947 ns |     0.052 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      60.29 ns |      4.616 ns |     0.253 ns |  1.06 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **69.04 ns** |      **3.522 ns** |     **0.193 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,141.48 ns |  1,300.638 ns |    71.292 ns | 16.53 |    0.90 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      75.13 ns |      4.597 ns |     0.252 ns |  1.09 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      65.48 ns |      1.491 ns |     0.082 ns |  0.95 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **875.60 ns** |     **10.179 ns** |     **0.558 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  21,160.69 ns |    347.192 ns |    19.031 ns | 24.17 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     876.07 ns |     10.071 ns |     0.552 ns |  1.00 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     874.77 ns |     36.457 ns |     1.998 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **8,059.47 ns** |     **25.449 ns** |     **1.395 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 307,455.03 ns | 53,441.184 ns | 2,929.292 ns | 38.15 |    0.31 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   8,126.18 ns |      5.873 ns |     0.322 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   8,268.05 ns |    219.602 ns |    12.037 ns |  1.03 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **8**    |    **130.03 ns** |     **1.645 ns** |   **0.090 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     62.21 ns |     0.706 ns |   0.039 ns |  0.48 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    131.38 ns |     1.526 ns |   0.084 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    106.86 ns |     4.763 ns |   0.261 ns |  0.82 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **228.88 ns** |   **263.103 ns** |  **14.422 ns** |  **1.00** |    **0.08** |         **-** |          **NA** |
| Kernel     | 12   |     69.58 ns |     2.101 ns |   0.115 ns |  0.30 |    0.02 |         - |          NA |
| Dp_Cjk     | 12   |    222.85 ns |    23.981 ns |   1.314 ns |  0.98 |    0.05 |         - |          NA |
| Kernel_Cjk | 12   |    118.96 ns |    10.672 ns |   0.585 ns |  0.52 |    0.03 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **14**   |    **301.99 ns** |   **421.559 ns** |  **23.107 ns** |  **1.00** |    **0.10** |         **-** |          **NA** |
| Kernel     | 14   |     74.33 ns |     2.256 ns |   0.124 ns |  0.25 |    0.02 |         - |          NA |
| Dp_Cjk     | 14   |    310.30 ns |   257.836 ns |  14.133 ns |  1.03 |    0.08 |         - |          NA |
| Kernel_Cjk | 14   |    124.62 ns |     1.754 ns |   0.096 ns |  0.41 |    0.03 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **388.24 ns** |   **438.564 ns** |  **24.039 ns** |  **1.00** |    **0.08** |         **-** |          **NA** |
| Kernel     | 16   |     77.51 ns |     1.032 ns |   0.057 ns |  0.20 |    0.01 |         - |          NA |
| Dp_Cjk     | 16   |    385.92 ns |   587.254 ns |  32.189 ns |  1.00 |    0.09 |         - |          NA |
| Kernel_Cjk | 16   |    132.04 ns |    12.225 ns |   0.670 ns |  0.34 |    0.02 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **18**   |    **736.76 ns** |   **707.736 ns** |  **38.793 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel     | 18   |     84.23 ns |     2.239 ns |   0.123 ns |  0.11 |    0.01 |         - |          NA |
| Dp_Cjk     | 18   |    703.36 ns |   319.178 ns |  17.495 ns |  0.96 |    0.05 |         - |          NA |
| Kernel_Cjk | 18   |    136.16 ns |    17.261 ns |   0.946 ns |  0.19 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **20**   |    **822.71 ns** |   **130.715 ns** |   **7.165 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 20   |     87.30 ns |     2.288 ns |   0.125 ns |  0.11 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    825.31 ns |    43.357 ns |   2.377 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 20   |    141.20 ns |     1.313 ns |   0.072 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **994.10 ns** |     **4.037 ns** |   **0.221 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 24   |     96.78 ns |     1.192 ns |   0.065 ns |  0.10 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    996.96 ns |   127.000 ns |   6.961 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 24   |    152.87 ns |     1.806 ns |   0.099 ns |  0.15 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,645.81 ns** |    **54.450 ns** |   **2.985 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    115.41 ns |     2.331 ns |   0.128 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,641.21 ns |   752.876 ns |  41.268 ns |  1.00 |    0.02 |         - |          NA |
| Kernel_Cjk | 32   |    179.32 ns |     2.340 ns |   0.128 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,278.12 ns** |   **366.328 ns** |  **20.080 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 48   |    144.48 ns |     2.064 ns |   0.113 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,291.89 ns | 1,013.427 ns |  55.549 ns |  1.00 |    0.02 |         - |          NA |
| Kernel_Cjk | 48   |    322.82 ns |   957.700 ns |  52.495 ns |  0.10 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **5,376.79 ns** |   **970.348 ns** |  **53.188 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 64   |    173.02 ns |    47.951 ns |   2.628 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,133.25 ns |   168.402 ns |   9.231 ns |  0.95 |    0.01 |         - |          NA |
| Kernel_Cjk | 64   |    314.60 ns |     3.706 ns |   0.203 ns |  0.06 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **11,520.78 ns** | **6,303.697 ns** | **345.527 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel     | 96   |    736.08 ns |     5.229 ns |   0.287 ns |  0.06 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 11,302.81 ns | 4,067.584 ns | 222.958 ns |  0.98 |    0.03 |         - |          NA |
| Kernel_Cjk | 96   |  1,080.21 ns |     7.670 ns |   0.420 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                     | Length | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **27.97 ns** |     **0.572 ns** |   **0.031 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    125.70 ns |    18.999 ns |   1.041 ns |  4.49 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     28.25 ns |     5.317 ns |   0.291 ns |  1.01 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **312.33 ns** |     **3.984 ns** |   **0.218 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    711.65 ns |    64.585 ns |   3.540 ns |  2.28 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    313.36 ns |     4.843 ns |   0.265 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    | **15,532.55 ns** |   **112.370 ns** |   **6.159 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 18,692.32 ns | 1,858.174 ns | 101.853 ns |  1.20 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 15,413.86 ns |   169.538 ns |   9.293 ns |  0.99 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | Length | Distinct | Mean         | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |-------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |     **354.2 ns** |      **4.39 ns** |     **0.24 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     255.7 ns |      6.48 ns |     0.36 ns |  0.72 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **354.6 ns** |      **4.68 ns** |     **0.26 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     255.9 ns |      2.48 ns |     0.14 ns |  0.72 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **474.8 ns** |      **9.96 ns** |     **0.55 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     349.6 ns |      3.40 ns |     0.19 ns |  0.74 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **446.7 ns** |     **17.84 ns** |     **0.98 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     346.6 ns |      0.81 ns |     0.04 ns |  0.78 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **547.9 ns** |     **96.74 ns** |     **5.30 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     444.6 ns |     26.39 ns |     1.45 ns |  0.81 |    0.01 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **536.6 ns** |     **12.32 ns** |     **0.68 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     452.4 ns |    101.23 ns |     5.55 ns |  0.84 |    0.01 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **637.8 ns** |      **4.68 ns** |     **0.26 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,339.8 ns |     57.00 ns |     3.12 ns |  2.10 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **659.6 ns** |    **345.12 ns** |    **18.92 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,311.8 ns |     15.32 ns |     0.84 ns |  1.99 |    0.05 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,581.3 ns** |    **416.70 ns** |    **22.84 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,788.4 ns |    260.26 ns |    14.27 ns |  2.24 |    0.02 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,583.8 ns** |     **42.22 ns** |     **2.31 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,773.0 ns |    190.99 ns |    10.47 ns |  2.23 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **19,748.3 ns** |    **439.07 ns** |    **24.07 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  65,252.5 ns |  1,445.21 ns |    79.22 ns |  3.30 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **434,101.8 ns** | **67,313.58 ns** | **3,689.68 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  69,178.6 ns |    864.36 ns |    47.38 ns |  0.16 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method         | Samples | Classes | Mean          | Error         | StdDev     | Gen0   | Allocated |
|--------------- |-------- |-------- |--------------:|--------------:|-----------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |      **6.992 μs** |     **1.4504 μs** |  **0.0795 μs** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      6.922 μs |     0.2477 μs |  0.0136 μs | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |      1.033 μs |     0.0791 μs |  0.0043 μs |      - |         - |
| F1Macro        | 1000    | 2       |      6.944 μs |     0.2435 μs |  0.0133 μs | 0.0229 |     472 B |
| Report         | 1000    | 2       |      9.772 μs |     0.0972 μs |  0.0053 μs | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7.222 μs** |     **0.1891 μs** |  **0.0104 μs** | **0.0687** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7.257 μs |     0.2306 μs |  0.0126 μs | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |      1.030 μs |     0.0041 μs |  0.0002 μs |      - |         - |
| F1Macro        | 1000    | 10      |      7.705 μs |     0.2471 μs |  0.0135 μs | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     14.107 μs |     0.1306 μs |  0.0072 μs | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **854.652 μs** |    **89.7319 μs** |  **4.9185 μs** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    820.976 μs |   127.5164 μs |  6.9896 μs |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    134.878 μs |    33.3763 μs |  1.8295 μs |      - |         - |
| F1Macro        | 100000  | 2       |    821.090 μs |   189.4737 μs | 10.3857 μs |      - |     473 B |
| Report         | 100000  | 2       |    791.617 μs |    53.6832 μs |  2.9426 μs |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **951.857 μs** |   **132.1174 μs** |  **7.2418 μs** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    965.790 μs |   242.8810 μs | 13.3131 μs |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    231.060 μs |   293.5387 μs | 16.0898 μs |      - |         - |
| F1Macro        | 100000  | 10      |    977.732 μs |    36.7259 μs |  2.0131 μs |      - |    1665 B |
| Report         | 100000  | 10      |    971.064 μs |   157.6669 μs |  8.6423 μs |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,731.954 μs** | **1,373.8078 μs** | **75.3030 μs** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,964.577 μs |   799.4884 μs | 43.8227 μs |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,916.196 μs |    53.4906 μs |  2.9320 μs |      - |         - |
| F1Macro        | 1000000 | 2       |  8,476.928 μs |   241.8293 μs | 13.2555 μs |      - |     484 B |
| Report         | 1000000 | 2       |  8,789.936 μs |   100.2981 μs |  5.4977 μs |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **10,095.983 μs** |   **972.8520 μs** | **53.3253 μs** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 10,157.126 μs |   678.6928 μs | 37.2014 μs |      - |    1255 B |
| AccuracyScore  | 1000000 | 10      |  3,110.696 μs |    40.6531 μs |  2.2283 μs |      - |         - |
| F1Macro        | 1000000 | 10      |  9,985.869 μs |    98.7127 μs |  5.4108 μs |      - |    1676 B |
| Report         | 1000000 | 10      |  9,929.414 μs |   222.3812 μs | 12.1895 μs |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **4**    |     **81.89 ns** |     **1.326 ns** |   **0.073 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 4    |     80.97 ns |     1.432 ns |   0.078 ns |  0.99 |    0.00 |         - |          NA |
| Dp_Cjk     | 4    |     81.76 ns |     3.229 ns |   0.177 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 4    |     81.60 ns |     0.581 ns |   0.032 ns |  1.00 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **6**    |    **120.96 ns** |     **7.269 ns** |   **0.398 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 6    |     86.52 ns |     0.818 ns |   0.045 ns |  0.72 |    0.00 |         - |          NA |
| Dp_Cjk     | 6    |    120.53 ns |     4.682 ns |   0.257 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 6    |    165.84 ns |     8.437 ns |   0.462 ns |  1.37 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **8**    |    **157.83 ns** |    **12.843 ns** |   **0.704 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 8    |    101.48 ns |    15.216 ns |   0.834 ns |  0.64 |    0.01 |         - |          NA |
| Dp_Cjk     | 8    |    156.45 ns |     8.731 ns |   0.479 ns |  0.99 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    195.06 ns |     9.856 ns |   0.540 ns |  1.24 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **10**   |    **210.55 ns** |    **20.597 ns** |   **1.129 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 10   |    107.97 ns |     0.910 ns |   0.050 ns |  0.51 |    0.00 |         - |          NA |
| Dp_Cjk     | 10   |    208.91 ns |     5.480 ns |   0.300 ns |  0.99 |    0.00 |         - |          NA |
| Kernel_Cjk | 10   |    168.48 ns |     0.386 ns |   0.021 ns |  0.80 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **269.92 ns** |     **2.152 ns** |   **0.118 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |    121.10 ns |     3.062 ns |   0.168 ns |  0.45 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    269.65 ns |    19.716 ns |   1.081 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 12   |    177.51 ns |     1.397 ns |   0.077 ns |  0.66 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **427.05 ns** |    **27.782 ns** |   **1.523 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 16   |    142.84 ns |     1.922 ns |   0.105 ns |  0.33 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    420.61 ns |    11.422 ns |   0.626 ns |  0.98 |    0.00 |         - |          NA |
| Kernel_Cjk | 16   |    200.08 ns |    12.666 ns |   0.694 ns |  0.47 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **862.54 ns** |     **8.385 ns** |   **0.460 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 24   |    181.29 ns |     4.116 ns |   0.226 ns |  0.21 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    864.33 ns |     7.882 ns |   0.432 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 24   |    246.08 ns |     5.033 ns |   0.276 ns |  0.29 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,481.14 ns** |    **37.663 ns** |   **2.064 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    219.64 ns |    19.510 ns |   1.069 ns |  0.15 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,482.53 ns |    35.542 ns |   1.948 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 32   |    296.87 ns |     2.654 ns |   0.145 ns |  0.20 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,445.28 ns** | **5,843.255 ns** | **320.289 ns** |  **1.01** |    **0.11** |         **-** |          **NA** |
| Kernel     | 48   |    302.06 ns |    13.648 ns |   0.748 ns |  0.09 |    0.01 |         - |          NA |
| Dp_Cjk     | 48   |  3,243.60 ns |   125.317 ns |   6.869 ns |  0.95 |    0.07 |         - |          NA |
| Kernel_Cjk | 48   |    390.99 ns |     7.971 ns |   0.437 ns |  0.11 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **5,666.65 ns** |   **483.223 ns** |  **26.487 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 64   |    380.87 ns |    12.894 ns |   0.707 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,651.98 ns |   211.230 ns |  11.578 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 64   |    496.90 ns |     3.711 ns |   0.203 ns |  0.09 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **12,548.57 ns** |   **288.869 ns** |  **15.834 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 96   |  1,280.89 ns |    14.553 ns |   0.798 ns |  0.10 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 12,511.21 ns |   156.476 ns |   8.577 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 96   |  1,558.38 ns |    31.192 ns |   1.710 ns |  0.12 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                 | Mean      | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated |
|----------------------- |----------:|----------:|----------:|---------:|---------:|---------:|----------:|
| VocabTxt               |  4.285 ms | 4.4435 ms | 0.2436 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 10.789 ms | 1.2890 ms | 0.0707 ms | 156.2500 | 125.0000 |  31.2500 |   5.72 MB |
| TokenizerJsonUnigram   | 11.128 ms | 0.8632 ms | 0.0473 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.916 ms | 1.2111 ms | 0.0664 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  2.018 ms | 0.5765 ms | 0.0316 ms |  33.2031 |  27.3438 |  27.3438 |   2.09 MB |
| TfidfLoad              |  4.537 ms | 1.0388 ms | 0.0569 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  4.869 ms | 0.4277 ms | 0.0234 ms | 453.1250 | 453.1250 | 453.1250 |  39.64 MB |
| EmbeddingIndexLoad     |  5.306 ms | 0.4166 ms | 0.0228 ms | 500.0000 | 468.7500 | 437.5000 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method               | Documents | Mean      | Error     | StdDev    | Ratio | Gen0      | Gen1      | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|----------:|----------:|------:|----------:|----------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.492 ms** | **0.7557 ms** | **0.0414 ms** |  **1.00** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.175 ms | 0.6579 ms | 0.0361 ms |  0.82 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.425 ms | 1.4131 ms | 0.0775 ms |  0.99 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.403 ms | 0.7136 ms | 0.0391 ms |  0.85 |  406.2500 |  148.4375 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |           |          |           |             |
| **Count**                | **1000**      | **30.227 ms** | **1.3520 ms** | **0.0741 ms** |  **1.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 23.323 ms | 3.2614 ms | 0.1788 ms |  0.77 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 29.070 ms | 3.0746 ms | 0.1685 ms |  0.96 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 24.339 ms | 2.6587 ms | 0.1457 ms |  0.81 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | Dim  | Mean      | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|------- |----- |----------:|---------:|---------:|------:|----------:|------------:|
| **Dot**    | **384**  |  **60.63 ns** | **2.443 ns** | **0.134 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.63 ns | 1.191 ns | 0.065 ns |  0.80 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  |  **99.81 ns** | **1.147 ns** | **0.063 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  90.73 ns | 3.587 ns | 0.197 ns |  0.91 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **132.71 ns** | **2.538 ns** | **0.139 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 125.10 ns | 1.355 ns | 0.074 ns |  0.94 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method       | Documents | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|------------- |---------- |----------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **Count**        | **200**       |  **3.011 ms** | **2.0515 ms** | **0.1124 ms** |  **1.00** |    **0.05** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.038 ms | 1.9607 ms | 0.1075 ms |  1.01 |    0.04 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.847 ms | 0.5933 ms | 0.0325 ms |  1.28 |    0.04 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  3.037 ms | 1.9245 ms | 0.1055 ms |  1.01 |    0.04 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.058 ms** | **1.4429 ms** | **0.0791 ms** |  **1.00** |    **0.01** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.239 ms | 1.1104 ms | 0.0609 ms |  1.03 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.635 ms | 0.6589 ms | 0.0361 ms |  1.65 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.093 ms | 0.1322 ms | 0.0072 ms |  1.01 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

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
| latin | 8 | 114.0 | 23.7 | 4.80x C# faster |
| latin | 32 | 161.8 | 67.9 | 2.38x C# faster |
| latin | 128 | 478.5 | 843.4 | 1.76x Py faster |
| latin | 512 | 4872.0 | 8258.7 | 1.70x Py faster |
| cjk | 8 | 130.7 | 23.8 | 5.50x C# faster |
| cjk | 32 | 238.1 | 143.6 | 1.66x C# faster |
| cjk | 128 | 2049.6 | 1663.3 | 1.23x C# faster |
| cjk | 512 | 16675.1 | 11766.7 | 1.42x C# faster |

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
| latin | 8 | 143.1 | 18.3 | 7.83x C# faster |
| latin | 32 | 256.1 | 127.6 | 2.01x C# faster |
| latin | 128 | 1815.6 | 1616.4 | 1.12x C# faster |
| latin | 512 | 15667.3 | 18115.4 | 1.16x Py faster |
| cjk | 8 | 142.9 | 18.1 | 7.90x C# faster |
| cjk | 32 | 295.9 | 190.9 | 1.55x C# faster |
| cjk | 128 | 3010.5 | 2534.8 | 1.19x C# faster |
| cjk | 512 | 26207.8 | 21663.9 | 1.21x C# faster |

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
| confusion_matrix_n1000_k2 | 0.009 | 0.768 | 84.44x | 0.009 | 0.767 | 84.44x |
| accuracy_n1000_k2 | 0.001 | 0.402 | 382.67x | 0.001 | 0.402 | 382.64x |
| precision_recall_f1_macro_n1000_k2 | 0.007 | 1.392 | 186.96x | 0.007 | 1.392 | 186.96x |
| classification_report_n1000_k2 | 0.010 | 5.243 | 524.78x | 0.010 | 5.242 | 524.81x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.545 | 96.49x | 0.016 | 1.544 | 96.49x |
| balanced_accuracy_n1000_k2 | 0.007 | 0.825 | 112.14x | 0.007 | 0.825 | 112.14x |
| matthews_n1000_k2 | 0.007 | 1.550 | 210.68x | 0.007 | 1.550 | 210.67x |
| cohen_kappa_n1000_k2 | 0.007 | 0.868 | 117.69x | 0.007 | 0.868 | 117.69x |
| mse_n1000_k2 | 0.003 | 0.214 | 78.08x | 0.003 | 0.214 | 78.09x |
| mae_n1000_k2 | 0.003 | 0.214 | 77.97x | 0.003 | 0.214 | 77.97x |
| median_ae_n1000_k2 | 0.006 | 0.231 | 38.81x | 0.006 | 0.231 | 38.81x |
| r2_n1000_k2 | 0.003 | 0.271 | 99.12x | 0.003 | 0.271 | 99.10x |
| confusion_matrix_n1000_k10 | 0.009 | 0.779 | 83.24x | 0.009 | 0.779 | 83.24x |
| accuracy_n1000_k10 | 0.001 | 0.406 | 384.43x | 0.001 | 0.406 | 384.44x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.414 | 174.70x | 0.008 | 1.414 | 174.66x |
| classification_report_n1000_k10 | 0.014 | 5.489 | 381.16x | 0.014 | 5.488 | 381.16x |
| roc_auc_ovr_macro_n1000_k10 | 0.453 | 8.097 | 17.86x | 0.453 | 8.096 | 17.86x |
| balanced_accuracy_n1000_k10 | 0.008 | 0.833 | 102.00x | 0.008 | 0.833 | 102.00x |
| matthews_n1000_k10 | 0.008 | 1.592 | 199.25x | 0.008 | 1.592 | 199.25x |
| cohen_kappa_n1000_k10 | 0.008 | 0.880 | 103.96x | 0.008 | 0.880 | 103.96x |
| mse_n1000_k10 | 0.003 | 0.216 | 78.99x | 0.003 | 0.216 | 78.98x |
| mae_n1000_k10 | 0.003 | 0.215 | 78.97x | 0.003 | 0.215 | 78.98x |
| median_ae_n1000_k10 | 0.006 | 0.231 | 38.83x | 0.006 | 0.231 | 38.83x |
| r2_n1000_k10 | 0.003 | 0.270 | 98.86x | 0.003 | 0.270 | 98.87x |
| confusion_matrix_n100000_k2 | 1.022 | 10.948 | 10.71x | 1.022 | 10.947 | 10.71x |
| accuracy_n100000_k2 | 0.106 | 3.775 | 35.70x | 0.106 | 3.775 | 35.70x |
| precision_recall_f1_macro_n100000_k2 | 0.873 | 12.524 | 14.34x | 0.873 | 12.522 | 14.34x |
| classification_report_n100000_k2 | 0.826 | 26.807 | 32.45x | 0.826 | 26.801 | 32.45x |
| roc_auc_binary_n100000_k2 | 2.877 | 28.359 | 9.86x | 2.877 | 28.356 | 9.85x |
| balanced_accuracy_n100000_k2 | 0.826 | 11.022 | 13.35x | 0.825 | 11.020 | 13.35x |
| matthews_n100000_k2 | 0.818 | 22.057 | 26.95x | 0.818 | 22.055 | 26.95x |
| cohen_kappa_n100000_k2 | 0.812 | 11.072 | 13.63x | 0.812 | 11.070 | 13.63x |
| mse_n100000_k2 | 0.266 | 0.370 | 1.39x | 0.266 | 0.370 | 1.39x |
| mae_n100000_k2 | 0.266 | 0.368 | 1.38x | 0.266 | 0.368 | 1.38x |
| median_ae_n100000_k2 | 0.685 | 1.881 | 2.75x | 0.706 | 1.881 | 2.66x |
| r2_n100000_k2 | 0.260 | 0.588 | 2.26x | 0.260 | 0.588 | 2.26x |
| confusion_matrix_n100000_k10 | 1.066 | 11.031 | 10.35x | 1.066 | 11.030 | 10.35x |
| accuracy_n100000_k10 | 0.108 | 3.779 | 35.07x | 0.108 | 3.778 | 35.08x |
| precision_recall_f1_macro_n100000_k10 | 1.032 | 13.251 | 12.85x | 1.031 | 13.249 | 12.85x |
| classification_report_n100000_k10 | 1.006 | 29.774 | 29.61x | 1.006 | 29.772 | 29.61x |
| roc_auc_ovr_macro_n100000_k10 | 30.465 | 230.801 | 7.58x | 30.462 | 230.780 | 7.58x |
| balanced_accuracy_n100000_k10 | 1.005 | 11.008 | 10.95x | 1.005 | 11.007 | 10.95x |
| matthews_n100000_k10 | 0.984 | 22.817 | 23.19x | 0.984 | 22.814 | 23.19x |
| cohen_kappa_n100000_k10 | 1.008 | 11.062 | 10.98x | 1.008 | 11.061 | 10.98x |
| mse_n100000_k10 | 0.267 | 0.379 | 1.42x | 0.267 | 0.378 | 1.42x |
| mae_n100000_k10 | 0.267 | 0.377 | 1.41x | 0.267 | 0.377 | 1.41x |
| median_ae_n100000_k10 | 0.713 | 1.887 | 2.64x | 0.756 | 1.887 | 2.49x |
| r2_n100000_k10 | 0.261 | 0.597 | 2.29x | 0.261 | 0.597 | 2.29x |
| confusion_matrix_n1000000_k2 | 9.228 | 102.493 | 11.11x | 9.227 | 102.487 | 11.11x |
| accuracy_n1000000_k2 | 2.307 | 34.206 | 14.83x | 2.307 | 34.204 | 14.83x |
| precision_recall_f1_macro_n1000000_k2 | 8.972 | 112.558 | 12.54x | 8.971 | 112.547 | 12.55x |
| classification_report_n1000000_k2 | 8.938 | 219.291 | 24.54x | 8.938 | 219.250 | 24.53x |
| roc_auc_binary_n1000000_k2 | 41.207 | 305.260 | 7.41x | 41.202 | 305.230 | 7.41x |
| balanced_accuracy_n1000000_k2 | 8.977 | 102.598 | 11.43x | 8.975 | 102.588 | 11.43x |
| matthews_n1000000_k2 | 8.826 | 206.686 | 23.42x | 8.826 | 206.660 | 23.42x |
| cohen_kappa_n1000000_k2 | 8.828 | 102.787 | 11.64x | 8.827 | 102.780 | 11.64x |
| mse_n1000000_k2 | 2.656 | 1.820 | 0.69x | 2.656 | 1.820 | 0.69x |
| mae_n1000000_k2 | 2.656 | 1.775 | 0.67x | 2.656 | 1.775 | 0.67x |
| median_ae_n1000000_k2 | 6.199 | 15.441 | 2.49x | 6.230 | 15.440 | 2.48x |
| r2_n1000000_k2 | 2.601 | 3.432 | 1.32x | 2.600 | 3.432 | 1.32x |
| confusion_matrix_n1000000_k10 | 11.028 | 102.276 | 9.27x | 11.027 | 102.268 | 9.27x |
| accuracy_n1000000_k10 | 3.310 | 34.147 | 10.32x | 3.310 | 34.142 | 10.32x |
| precision_recall_f1_macro_n1000000_k10 | 10.541 | 119.275 | 11.32x | 10.541 | 119.268 | 11.31x |
| classification_report_n1000000_k10 | 10.428 | 246.366 | 23.63x | 10.427 | 246.335 | 23.62x |
| balanced_accuracy_n1000000_k10 | 10.432 | 102.385 | 9.81x | 10.432 | 102.371 | 9.81x |
| matthews_n1000000_k10 | 10.157 | 214.215 | 21.09x | 10.156 | 214.202 | 21.09x |
| cohen_kappa_n1000000_k10 | 10.166 | 102.661 | 10.10x | 10.165 | 102.652 | 10.10x |
| mse_n1000000_k10 | 2.653 | 1.822 | 0.69x | 2.652 | 1.822 | 0.69x |
| mae_n1000000_k10 | 2.658 | 1.797 | 0.68x | 2.658 | 1.797 | 0.68x |
| median_ae_n1000000_k10 | 6.273 | 15.496 | 2.47x | 6.343 | 15.494 | 2.44x |
| r2_n1000000_k10 | 2.776 | 3.472 | 1.25x | 2.776 | 3.471 | 1.25x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.69x
  mae_n1000000_k2                  0.67x
  mse_n1000000_k10                 0.69x
  mae_n1000000_k10                 0.68x

<!-- markdownlint-enable MD060 -->

### compare-persistence

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 5.204 | 9.890 | 1.90x | 5.462 | 9.889 | 1.81x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.343 | 17.594 | 1.43x | 13.144 | 17.592 | 1.34x | 706,526 | 706,526 |
| tokenizer_json_unigram | 11.317 | 37.159 | 3.28x | 11.814 | 37.152 | 3.14x | 1,990,038 | 1,990,038 |
| spiece_model | 4.818 | 30.384 | 6.31x | 4.981 | 30.380 | 6.10x | 533,084 | 533,084 |
| tfidf_save | 1.666 | 2.444 | 1.47x | 1.722 | 2.444 | 1.42x | 581,787 | 591,922 |
| tfidf_load | 4.704 | 4.267 | 0.91x | 4.896 | 4.266 | 0.87x | 581,787 | 591,922 |
| embedding_index_save | 4.670 | 1.347 | 0.29x | 5.230 | 1.347 | 0.26x | 20,589,007 | 15,360,128 |
| embedding_index_load | 4.329 | 1.349 | 0.31x | 4.907 | 1.348 | 0.27x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 5.598 | 0.834 | 0.15x | 6.300 | 0.833 | 0.13x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 3.012 | 1.399 | 0.46x | 3.407 | 1.399 | 0.41x | 20,589,007 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 76.74x | 0.000 | 0.001 | 76.80x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 454.761 | 639.466 | 1.41x | 457.526 | 639.447 | 1.40x | 15,251,458 | 14,022,374 |
| embedding_index_load_gzip | 81.072 | 72.519 | 0.89x | 82.366 | 72.512 | 0.88x | 15,251,458 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
