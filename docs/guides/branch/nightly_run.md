# Nightly benchmark run

<!-- nightly-baseline: de615ac0f5f32ce43f49da89648be9b66e97bf1a -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `dfada789d001d6a01eac314abdf3bcaa9a514105`
- Previous run: `de615ac0f5f32ce43f49da89648be9b66e97bf1a`
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
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | CorpusSize | Mean         | Error       | StdDev    | Ratio | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|----------:|------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **6.254 μs** |   **0.4584 μs** | **0.0251 μs** |  **1.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.375 μs |   0.5247 μs | 0.0288 μs |  1.02 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.396 μs |   0.6681 μs | 0.0366 μs |  1.02 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **8**          |   **111.681 μs** |  **15.5491 μs** | **0.8523 μs** |  **1.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    71.504 μs |  20.2885 μs | 1.1121 μs |  0.64 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    74.677 μs |  11.8107 μs | 0.6474 μs |  0.67 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **32**         |   **409.692 μs** |  **15.9432 μs** | **0.8739 μs** |  **1.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   258.902 μs |  22.8846 μs | 1.2544 μs |  0.63 |  26.8555 |  1.4648 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   248.764 μs | 100.5868 μs | 5.5135 μs |  0.61 |  25.8789 |  1.4648 |   427.8 KB |        0.91 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **128**        | **1,676.816 μs** | **136.4403 μs** | **7.4788 μs** |  **1.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        | 1,041.655 μs | 104.8144 μs | 5.7452 μs |  0.62 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   981.070 μs | 151.4631 μs | 8.3022 μs |  0.59 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

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

| Method | length | Mean          | Error         | StdDev       | Allocated |
|------- |------- |--------------:|--------------:|-------------:|----------:|
| **Latin**  | **1000**   |      **51.90 μs** |      **0.574 μs** |     **0.031 μs** |         **-** |
| Cjk    | 1000   |      55.67 μs |      1.326 μs |     0.073 μs |         - |
| **Latin**  | **10000**  |   **5,522.54 μs** |    **942.052 μs** |    **51.637 μs** |         **-** |
| Cjk    | 10000  |   6,865.36 μs |    523.671 μs |    28.704 μs |         - |
| **Latin**  | **65536**  | **204,940.05 μs** | **56,741.253 μs** | **3,110.180 μs** |         **-** |

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

| Method  | Mean     | Error     | StdDev  | Ratio | Gen0       | Allocated | Alloc Ratio |
|-------- |---------:|----------:|--------:|------:|-----------:|----------:|------------:|
| Unigram | 615.4 ms | 100.11 ms | 5.49 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 553.1 ms |  92.61 ms | 5.08 ms |  0.90 |  7000.0000 | 112.18 MB |        0.22 |

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

| Method                    | Length | Mean       | Error     | StdDev  | Gen0   | Allocated |
|-------------------------- |------- |-----------:|----------:|--------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |   **105.3 μs** |   **6.64 μs** | **0.36 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **220.4 μs** |  **13.45 μs** | **0.74 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **483.8 μs** |  **27.65 μs** | **1.52 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,005.3 μs** | **122.57 μs** | **6.72 μs** | **7.8125** | **157.03 KB** |

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
| **DpGroup**    | **cjk**      |  **20.220 μs** |  **0.4911 μs** | **0.0269 μs** |         **-** |
| MyersGroup | cjk      | 242.919 μs | 10.6438 μs | 0.5834 μs |         - |
| **DpGroup**    | **latin**    |   **9.705 μs** |  **0.5982 μs** | **0.0328 μs** |         **-** |
| MyersGroup | latin    | 130.394 μs |  7.8944 μs | 0.4327 μs |         - |

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

| Method         | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|-------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     97.71 ns |     5.580 ns |   0.306 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 12,696.53 ns | 2,498.829 ns | 136.969 ns | 129.95 |    1.26 |      - |         - |          NA |
| TokenSortRatio |  1,060.41 ns |   211.539 ns |  11.595 ns |  10.85 |    0.11 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,631.74 ns |   553.010 ns |  30.312 ns |  37.17 |    0.29 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,831.92 ns |   432.173 ns |  23.689 ns |  49.45 |    0.25 | 0.4272 |    7200 B |          NA |

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

| Method                     | Length | Mean          | Error         | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|--------------:|-------------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **26.55 ns** |      **0.194 ns** |     **0.011 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     132.89 ns |     10.505 ns |     0.576 ns |  5.00 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      27.02 ns |      0.247 ns |     0.014 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      27.21 ns |      6.181 ns |     0.339 ns |  1.02 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.07 ns** |      **2.970 ns** |     **0.163 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     146.37 ns |      7.811 ns |     0.428 ns |  5.21 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.92 ns |     11.632 ns |     0.638 ns |  1.03 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.10 ns |      0.376 ns |     0.021 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.69 ns** |      **0.447 ns** |     **0.025 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     155.89 ns |      2.327 ns |     0.128 ns |  5.08 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.09 ns |      1.062 ns |     0.058 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      30.85 ns |      3.883 ns |     0.213 ns |  1.01 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **35.36 ns** |      **1.375 ns** |     **0.075 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     194.36 ns |      4.179 ns |     0.229 ns |  5.50 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      35.60 ns |      1.433 ns |     0.079 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      32.44 ns |      1.183 ns |     0.065 ns |  0.92 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **54.42 ns** |      **0.768 ns** |     **0.042 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     777.11 ns |    135.783 ns |     7.443 ns | 14.28 |    0.12 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      56.24 ns |      0.630 ns |     0.035 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      56.61 ns |      5.882 ns |     0.322 ns |  1.04 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **61.65 ns** |      **2.622 ns** |     **0.144 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,027.32 ns |     31.633 ns |     1.734 ns | 16.66 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      65.16 ns |      4.321 ns |     0.237 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      58.82 ns |      0.603 ns |     0.033 ns |  0.95 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **906.76 ns** |     **32.076 ns** |     **1.758 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  22,664.02 ns | 32,166.140 ns | 1,763.135 ns | 24.99 |    1.68 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     932.30 ns |    117.808 ns |     6.457 ns |  1.03 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     946.77 ns |     19.939 ns |     1.093 ns |  1.04 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **7,618.77 ns** |  **2,357.212 ns** |   **129.207 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 335,747.40 ns | 89,452.523 ns | 4,903.195 ns | 44.08 |    0.85 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   7,512.21 ns |    242.494 ns |    13.292 ns |  0.99 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   7,670.15 ns |     33.714 ns |     1.848 ns |  1.01 |    0.01 |         - |          NA |

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
| **Dp**         | **8**    |    **131.85 ns** |     **9.317 ns** |   **0.511 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     55.07 ns |     0.401 ns |   0.022 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    132.45 ns |     3.070 ns |   0.168 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    111.18 ns |    11.857 ns |   0.650 ns |  0.84 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **226.58 ns** |    **19.267 ns** |   **1.056 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 12   |     61.65 ns |     1.328 ns |   0.073 ns |  0.27 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    216.08 ns |     6.544 ns |   0.359 ns |  0.95 |    0.00 |         - |          NA |
| Kernel_Cjk | 12   |    108.72 ns |     1.605 ns |   0.088 ns |  0.48 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **14**   |    **278.26 ns** |   **248.248 ns** |  **13.607 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel     | 14   |     67.08 ns |     1.368 ns |   0.075 ns |  0.24 |    0.01 |         - |          NA |
| Dp_Cjk     | 14   |    286.28 ns |   298.373 ns |  16.355 ns |  1.03 |    0.07 |         - |          NA |
| Kernel_Cjk | 14   |    114.76 ns |     1.608 ns |   0.088 ns |  0.41 |    0.02 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **351.75 ns** |    **98.067 ns** |   **5.375 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 16   |     71.74 ns |     0.554 ns |   0.030 ns |  0.20 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    355.32 ns |   110.205 ns |   6.041 ns |  1.01 |    0.02 |         - |          NA |
| Kernel_Cjk | 16   |    121.15 ns |    71.868 ns |   3.939 ns |  0.34 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **18**   |    **447.49 ns** |    **97.859 ns** |   **5.364 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 18   |     74.36 ns |     2.979 ns |   0.163 ns |  0.17 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    435.55 ns |    64.655 ns |   3.544 ns |  0.97 |    0.01 |         - |          NA |
| Kernel_Cjk | 18   |    124.95 ns |     5.395 ns |   0.296 ns |  0.28 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **20**   |    **771.12 ns** |    **54.588 ns** |   **2.992 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 20   |     79.75 ns |     0.730 ns |   0.040 ns |  0.10 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    772.23 ns |    26.763 ns |   1.467 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 20   |    128.23 ns |     1.167 ns |   0.064 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **987.76 ns** |   **449.215 ns** |  **24.623 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 24   |     88.36 ns |     1.770 ns |   0.097 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    981.33 ns |   363.059 ns |  19.900 ns |  0.99 |    0.03 |         - |          NA |
| Kernel_Cjk | 24   |    140.32 ns |     5.693 ns |   0.312 ns |  0.14 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,570.20 ns** |    **81.517 ns** |   **4.468 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    105.71 ns |     1.555 ns |   0.085 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,577.32 ns |   168.975 ns |   9.262 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 32   |    165.10 ns |     2.106 ns |   0.115 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,391.21 ns** |   **638.616 ns** |  **35.005 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 48   |    130.52 ns |     6.191 ns |   0.339 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,255.32 ns |   363.870 ns |  19.945 ns |  0.96 |    0.01 |         - |          NA |
| Kernel_Cjk | 48   |    243.15 ns |    15.311 ns |   0.839 ns |  0.07 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **5,182.99 ns** | **2,802.675 ns** | **153.624 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel     | 64   |    156.99 ns |     3.339 ns |   0.183 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,170.05 ns | 2,170.367 ns | 118.965 ns |  1.19 |    0.04 |         - |          NA |
| Kernel_Cjk | 64   |    299.93 ns |     9.120 ns |   0.500 ns |  0.06 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **12,213.29 ns** | **3,317.239 ns** | **181.829 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 96   |    792.65 ns |     5.573 ns |   0.305 ns |  0.06 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 12,451.07 ns |   773.848 ns |  42.417 ns |  1.02 |    0.01 |         - |          NA |
| Kernel_Cjk | 96   |  1,045.08 ns |    18.812 ns |   1.031 ns |  0.09 |    0.00 |         - |          NA |

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

| Method                     | Length | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **25.05 ns** |     **2.667 ns** |   **0.146 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    120.37 ns |     4.104 ns |   0.225 ns |  4.80 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.74 ns |     1.086 ns |   0.060 ns |  1.03 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **270.68 ns** |    **34.259 ns** |   **1.878 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    675.85 ns |     9.978 ns |   0.547 ns |  2.50 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    271.96 ns |     8.801 ns |   0.482 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    | **14,173.24 ns** | **2,418.376 ns** | **132.559 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 17,129.67 ns |   488.546 ns |  26.779 ns |  1.21 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,029.13 ns | 1,317.006 ns |  72.190 ns |  0.99 |    0.01 |         - |          NA |

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
| **Distance_CodePoint** | **16**     | **32**       |     **340.1 ns** |       **1.21 ns** |     **0.07 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     237.9 ns |      16.07 ns |     0.88 ns |  0.70 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **351.2 ns** |      **23.66 ns** |     **1.30 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     243.4 ns |      62.92 ns |     3.45 ns |  0.69 |    0.01 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **424.9 ns** |      **10.16 ns** |     **0.56 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     320.4 ns |       3.81 ns |     0.21 ns |  0.75 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **425.8 ns** |      **23.36 ns** |     **1.28 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     325.9 ns |       1.35 ns |     0.07 ns |  0.77 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **505.5 ns** |      **36.95 ns** |     **2.03 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     410.0 ns |      74.93 ns |     4.11 ns |  0.81 |    0.01 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **527.8 ns** |      **21.83 ns** |     **1.20 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     419.3 ns |       1.91 ns |     0.10 ns |  0.79 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **590.0 ns** |      **41.01 ns** |     **2.25 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,614.9 ns |      26.72 ns |     1.46 ns |  2.74 |    0.01 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **589.7 ns** |       **1.97 ns** |     **0.11 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,300.2 ns |      16.12 ns |     0.88 ns |  2.21 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,532.2 ns** |      **14.68 ns** |     **0.80 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,373.3 ns |      98.88 ns |     5.42 ns |  2.12 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,559.9 ns** |      **72.41 ns** |     **3.97 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,427.0 ns |      82.85 ns |     4.54 ns |  2.12 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **18,316.9 ns** |     **215.88 ns** |    **11.83 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  60,000.7 ns |     779.73 ns |    42.74 ns |  3.28 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **447,285.5 ns** | **132,651.64 ns** | **7,271.08 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  65,283.1 ns |      72.15 ns |     3.95 ns |  0.15 |    0.00 |         - |          NA |

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

| Method         | Samples | Classes | Mean           | Error           | StdDev       | Gen0   | Allocated |
|--------------- |-------- |-------- |---------------:|----------------:|-------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |     **7,275.6 ns** |       **201.99 ns** |     **11.07 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     7,188.3 ns |       491.01 ns |     26.91 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |       919.6 ns |        28.25 ns |      1.55 ns |      - |         - |
| F1Macro        | 1000    | 2       |     7,821.6 ns |       566.80 ns |     31.07 ns | 0.0153 |     472 B |
| Report         | 1000    | 2       |    10,740.1 ns |     2,409.97 ns |    132.10 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **7,871.1 ns** |       **297.70 ns** |     **16.32 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     7,744.2 ns |       507.15 ns |     27.80 ns | 0.0610 |    1248 B |
| AccuracyScore  | 1000    | 10      |       920.3 ns |        42.07 ns |      2.31 ns |      - |         - |
| F1Macro        | 1000    | 10      |     8,095.7 ns |     1,302.98 ns |     71.42 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |    15,742.1 ns |     1,674.73 ns |     91.80 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **885,192.3 ns** |   **104,266.82 ns** |  **5,715.22 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   831,734.8 ns |     2,102.69 ns |    115.26 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   166,260.5 ns |    11,577.87 ns |    634.62 ns |      - |         - |
| F1Macro        | 100000  | 2       |   867,246.0 ns |    24,916.97 ns |  1,365.78 ns |      - |     472 B |
| Report         | 100000  | 2       |   886,682.1 ns |    43,234.40 ns |  2,369.82 ns |      - |    6544 B |
| **Matrix**         | **100000**  | **10**      |   **968,843.5 ns** |   **196,129.16 ns** | **10,750.50 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   941,431.7 ns |    28,330.11 ns |  1,552.87 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   264,286.1 ns |     5,179.07 ns |    283.88 ns |      - |         - |
| F1Macro        | 100000  | 10      |   983,060.7 ns |    25,388.15 ns |  1,391.61 ns |      - |    1665 B |
| Report         | 100000  | 10      |   992,434.2 ns |   138,274.40 ns |  7,579.29 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **8,689,966.5 ns** |   **586,595.67 ns** | **32,153.29 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       | 8,304,489.8 ns |   502,995.98 ns | 27,570.91 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       | 1,747,175.9 ns |    40,316.21 ns |  2,209.87 ns |      - |         - |
| F1Macro        | 1000000 | 2       | 8,707,656.5 ns |   140,000.27 ns |  7,673.89 ns |      - |     484 B |
| Report         | 1000000 | 2       | 8,908,445.1 ns | 1,158,739.10 ns | 63,514.40 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **9,669,627.6 ns** |   **273,801.11 ns** | **15,007.96 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 9,386,631.1 ns | 1,508,666.22 ns | 82,695.09 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      | 2,692,892.9 ns |    68,297.81 ns |  3,743.63 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 9,926,889.2 ns |   295,290.37 ns | 16,185.86 ns |      - |    1676 B |
| Report         | 1000000 | 10      | 9,970,067.2 ns |   545,400.92 ns | 29,895.26 ns |      - |   15892 B |

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
| **Dp**         | **4**    |     **73.39 ns** |     **1.102 ns** |   **0.060 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 4    |     73.84 ns |     5.723 ns |   0.314 ns |  1.01 |    0.00 |         - |          NA |
| Dp_Cjk     | 4    |     73.65 ns |     2.725 ns |   0.149 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 4    |     73.92 ns |     1.237 ns |   0.068 ns |  1.01 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **6**    |    **103.98 ns** |     **1.720 ns** |   **0.094 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 6    |     77.52 ns |     3.600 ns |   0.197 ns |  0.75 |    0.00 |         - |          NA |
| Dp_Cjk     | 6    |    103.66 ns |     1.245 ns |   0.068 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 6    |    146.56 ns |    27.503 ns |   1.508 ns |  1.41 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **8**    |    **157.02 ns** |     **2.805 ns** |   **0.154 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     87.78 ns |    15.387 ns |   0.843 ns |  0.56 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    153.97 ns |     2.285 ns |   0.125 ns |  0.98 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    199.40 ns |     3.416 ns |   0.187 ns |  1.27 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **10**   |    **196.69 ns** |     **7.519 ns** |   **0.412 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 10   |     94.62 ns |     1.884 ns |   0.103 ns |  0.48 |    0.00 |         - |          NA |
| Dp_Cjk     | 10   |    197.66 ns |    21.297 ns |   1.167 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 10   |    151.26 ns |     5.818 ns |   0.319 ns |  0.77 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **245.83 ns** |    **33.415 ns** |   **1.832 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 12   |    102.74 ns |     3.155 ns |   0.173 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    248.64 ns |    90.018 ns |   4.934 ns |  1.01 |    0.02 |         - |          NA |
| Kernel_Cjk | 12   |    166.99 ns |    12.196 ns |   0.669 ns |  0.68 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **408.72 ns** |     **3.811 ns** |   **0.209 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 16   |    121.95 ns |     7.558 ns |   0.414 ns |  0.30 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    408.76 ns |     3.860 ns |   0.212 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 16   |    181.75 ns |     5.957 ns |   0.327 ns |  0.44 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **845.17 ns** |    **79.267 ns** |   **4.345 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 24   |    159.86 ns |     0.384 ns |   0.021 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    843.34 ns |    41.770 ns |   2.290 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 24   |    227.35 ns |     1.940 ns |   0.106 ns |  0.27 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,451.91 ns** |    **43.973 ns** |   **2.410 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    192.65 ns |     6.608 ns |   0.362 ns |  0.13 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,445.52 ns |   251.886 ns |  13.807 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 32   |    271.68 ns |     9.914 ns |   0.543 ns |  0.19 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,748.45 ns** |   **920.168 ns** |  **50.438 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 48   |    266.72 ns |    13.721 ns |   0.752 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,435.48 ns | 2,845.259 ns | 155.958 ns |  0.92 |    0.04 |         - |          NA |
| Kernel_Cjk | 48   |    353.49 ns |     4.164 ns |   0.228 ns |  0.09 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **6,964.70 ns** | **7,380.132 ns** | **404.530 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel     | 64   |    351.54 ns |    41.104 ns |   2.253 ns |  0.05 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,334.88 ns | 1,857.518 ns | 101.817 ns |  0.91 |    0.05 |         - |          NA |
| Kernel_Cjk | 64   |    439.69 ns |    21.949 ns |   1.203 ns |  0.06 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **14,162.47 ns** | **3,631.143 ns** | **199.035 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 96   |  1,209.68 ns |   121.005 ns |   6.633 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 15,725.53 ns | 8,195.569 ns | 449.227 ns |  1.11 |    0.03 |         - |          NA |
| Kernel_Cjk | 96   |  1,510.96 ns |   463.918 ns |  25.429 ns |  0.11 |    0.00 |         - |          NA |

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
| VocabTxt               |  4.277 ms | 3.3192 ms | 0.1819 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.916 ms | 3.9576 ms | 0.2169 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 13.024 ms | 0.5674 ms | 0.0311 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.872 ms | 2.2687 ms | 0.1244 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.866 ms | 0.6567 ms | 0.0360 ms |  29.2969 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              |  4.460 ms | 1.0374 ms | 0.0569 ms |  78.1250 |  62.5000 |  15.6250 |   2.86 MB |
| EmbeddingIndexSave     |  4.950 ms | 1.3884 ms | 0.0761 ms | 273.4375 | 273.4375 | 273.4375 |  19.87 MB |
| EmbeddingIndexLoad     |  6.804 ms | 2.3611 ms | 0.1294 ms | 539.0625 | 507.8125 | 476.5625 |  35.35 MB |

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

| Method               | Documents | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|-----------:|----------:|------:|--------:|----------:|---------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **8.009 ms** |  **0.6909 ms** | **0.0379 ms** |  **1.00** |    **0.01** |  **500.0000** | **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.579 ms |  0.7353 ms | 0.0403 ms |  0.82 |    0.01 |  390.6250 | 187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.675 ms |  2.1617 ms | 0.1185 ms |  0.96 |    0.01 |  500.0000 | 156.2500 |  62.5000 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.662 ms |  0.5703 ms | 0.0313 ms |  0.83 |    0.00 |  406.2500 | 156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |            |           |       |         |           |          |          |           |             |
| **Count**                | **1000**      | **31.897 ms** |  **3.6696 ms** | **0.2011 ms** |  **1.00** |    **0.01** | **2625.0000** | **875.0000** | **500.0000** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 24.794 ms |  2.6169 ms | 0.1434 ms |  0.78 |    0.01 | 1968.7500 | 781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 30.370 ms | 10.7288 ms | 0.5881 ms |  0.95 |    0.02 | 2562.5000 | 750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 25.968 ms |  2.8410 ms | 0.1557 ms |  0.81 |    0.01 | 2031.2500 | 625.0000 | 250.0000 |  31.83 MB |        0.82 |

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
| **Dot**    | **384**  |  **50.99 ns** |  **5.616 ns** | **0.308 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  51.21 ns |  0.905 ns | 0.050 ns |  1.00 |         - |          NA |
|        |      |           |           |          |       |           |             |
| **Dot**    | **768**  |  **95.55 ns** | **17.199 ns** | **0.943 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  92.84 ns |  2.556 ns | 0.140 ns |  0.97 |         - |          NA |
|        |      |           |           |          |       |           |             |
| **Dot**    | **1024** | **124.04 ns** |  **1.059 ns** | **0.058 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 122.74 ns |  0.515 ns | 0.028 ns |  0.99 |         - |          NA |

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
| **Count**        | **200**       |  **2.992 ms** | **2.1350 ms** | **0.1170 ms** |  **1.00** |    **0.05** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.002 ms | 1.7146 ms | 0.0940 ms |  1.00 |    0.04 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.791 ms | 0.9116 ms | 0.0500 ms |  1.27 |    0.04 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.986 ms | 1.1048 ms | 0.0606 ms |  1.00 |    0.04 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.486 ms** | **0.7613 ms** | **0.0417 ms** |  **1.00** |    **0.01** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.900 ms | 1.7384 ms | 0.0953 ms |  1.06 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 12.320 ms | 2.1751 ms | 0.1192 ms |  1.65 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.430 ms | 4.1158 ms | 0.2256 ms |  0.99 |    0.03 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

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
| latin | 8 | 115.8 | 28.4 | 4.07x C# faster |
| latin | 32 | 177.4 | 88.8 | 2.00x C# faster |
| latin | 128 | 471.8 | 892.5 | 1.89x Py faster |
| latin | 512 | 4466.7 | 7683.2 | 1.72x Py faster |
| cjk | 8 | 139.6 | 27.0 | 5.17x C# faster |
| cjk | 32 | 331.1 | 223.8 | 1.48x C# faster |
| cjk | 128 | 1903.5 | 1653.3 | 1.15x C# faster |
| cjk | 512 | 14870.6 | 11166.8 | 1.33x C# faster |

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
| latin | 8 | 157.7 | 17.9 | 8.79x C# faster |
| latin | 32 | 294.5 | 156.8 | 1.88x C# faster |
| latin | 128 | 1711.9 | 1503.9 | 1.14x C# faster |
| latin | 512 | 14164.5 | 15208.7 | 1.07x Py faster |
| cjk | 8 | 148.7 | 17.5 | 8.51x C# faster |
| cjk | 32 | 369.0 | 286.0 | 1.29x C# faster |
| cjk | 128 | 2821.7 | 2365.0 | 1.19x C# faster |
| cjk | 512 | 23599.9 | 18808.0 | 1.25x C# faster |

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
| confusion_matrix_n1000_k2 | 0.009 | 0.979 | 104.07x | 0.009 | 0.979 | 104.07x |
| accuracy_n1000_k2 | 0.001 | 0.516 | 500.57x | 0.001 | 0.515 | 500.57x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.748 | 221.57x | 0.008 | 1.748 | 221.57x |
| classification_report_n1000_k2 | 0.011 | 6.655 | 617.89x | 0.011 | 6.654 | 617.92x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.919 | 117.90x | 0.016 | 1.918 | 117.90x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.044 | 133.85x | 0.008 | 1.044 | 133.87x |
| matthews_n1000_k2 | 0.008 | 1.962 | 250.88x | 0.008 | 1.961 | 250.87x |
| cohen_kappa_n1000_k2 | 0.008 | 1.094 | 140.26x | 0.008 | 1.094 | 140.26x |
| mse_n1000_k2 | 0.002 | 0.304 | 124.42x | 0.002 | 0.304 | 124.40x |
| mae_n1000_k2 | 0.002 | 0.303 | 125.41x | 0.002 | 0.303 | 125.41x |
| median_ae_n1000_k2 | 0.006 | 0.314 | 51.95x | 0.006 | 0.314 | 51.94x |
| r2_n1000_k2 | 0.002 | 0.364 | 146.67x | 0.002 | 0.364 | 146.67x |
| confusion_matrix_n1000_k10 | 0.010 | 0.990 | 103.94x | 0.010 | 0.990 | 103.93x |
| accuracy_n1000_k10 | 0.001 | 0.521 | 461.53x | 0.001 | 0.521 | 461.54x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.773 | 211.41x | 0.008 | 1.773 | 211.41x |
| classification_report_n1000_k10 | 0.016 | 6.927 | 444.14x | 0.016 | 6.926 | 444.14x |
| roc_auc_ovr_macro_n1000_k10 | 0.546 | 9.971 | 18.27x | 0.546 | 9.969 | 18.27x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.063 | 127.64x | 0.008 | 1.063 | 127.62x |
| matthews_n1000_k10 | 0.008 | 2.013 | 240.86x | 0.008 | 2.012 | 240.83x |
| cohen_kappa_n1000_k10 | 0.009 | 1.098 | 126.57x | 0.009 | 1.097 | 126.55x |
| mse_n1000_k10 | 0.002 | 0.304 | 124.43x | 0.002 | 0.304 | 124.43x |
| mae_n1000_k10 | 0.002 | 0.300 | 124.29x | 0.002 | 0.300 | 124.29x |
| median_ae_n1000_k10 | 0.006 | 0.314 | 52.59x | 0.006 | 0.314 | 52.59x |
| r2_n1000_k10 | 0.002 | 0.367 | 147.94x | 0.002 | 0.366 | 147.94x |
| confusion_matrix_n100000_k2 | 1.000 | 10.779 | 10.77x | 1.000 | 10.778 | 10.78x |
| accuracy_n100000_k2 | 0.186 | 3.779 | 20.36x | 0.186 | 3.779 | 20.36x |
| precision_recall_f1_macro_n100000_k2 | 0.874 | 12.445 | 14.24x | 0.874 | 12.444 | 14.24x |
| classification_report_n100000_k2 | 0.886 | 27.270 | 30.79x | 0.885 | 27.268 | 30.80x |
| roc_auc_binary_n100000_k2 | 3.614 | 27.684 | 7.66x | 3.613 | 27.680 | 7.66x |
| balanced_accuracy_n100000_k2 | 0.874 | 11.060 | 12.66x | 0.874 | 11.059 | 12.66x |
| matthews_n100000_k2 | 0.880 | 22.053 | 25.07x | 0.880 | 22.048 | 25.07x |
| cohen_kappa_n100000_k2 | 0.873 | 11.126 | 12.74x | 0.873 | 11.124 | 12.74x |
| mse_n100000_k2 | 0.238 | 0.439 | 1.84x | 0.238 | 0.439 | 1.84x |
| mae_n100000_k2 | 0.239 | 0.431 | 1.80x | 0.239 | 0.430 | 1.80x |
| median_ae_n100000_k2 | 0.691 | 1.797 | 2.60x | 0.709 | 1.796 | 2.53x |
| r2_n100000_k2 | 0.235 | 0.656 | 2.79x | 0.235 | 0.656 | 2.79x |
| confusion_matrix_n100000_k10 | 0.974 | 10.909 | 11.20x | 0.974 | 10.906 | 11.20x |
| accuracy_n100000_k10 | 0.274 | 3.775 | 13.80x | 0.274 | 3.774 | 13.80x |
| precision_recall_f1_macro_n100000_k10 | 0.990 | 13.105 | 13.24x | 0.990 | 13.104 | 13.24x |
| classification_report_n100000_k10 | 0.988 | 29.492 | 29.84x | 0.988 | 29.490 | 29.85x |
| roc_auc_ovr_macro_n100000_k10 | 38.644 | 220.961 | 5.72x | 38.639 | 220.946 | 5.72x |
| balanced_accuracy_n100000_k10 | 0.976 | 10.986 | 11.25x | 0.976 | 10.985 | 11.25x |
| matthews_n100000_k10 | 0.980 | 22.650 | 23.11x | 0.980 | 22.649 | 23.11x |
| cohen_kappa_n100000_k10 | 0.992 | 11.085 | 11.17x | 0.992 | 11.084 | 11.17x |
| mse_n100000_k10 | 0.238 | 0.435 | 1.83x | 0.238 | 0.435 | 1.83x |
| mae_n100000_k10 | 0.238 | 0.428 | 1.80x | 0.238 | 0.428 | 1.80x |
| median_ae_n100000_k10 | 0.726 | 1.789 | 2.46x | 0.780 | 1.788 | 2.29x |
| r2_n100000_k10 | 0.235 | 0.649 | 2.76x | 0.235 | 0.649 | 2.76x |
| confusion_matrix_n1000000_k2 | 8.762 | 101.207 | 11.55x | 8.761 | 101.173 | 11.55x |
| accuracy_n1000000_k2 | 1.960 | 33.632 | 17.16x | 1.960 | 33.631 | 17.16x |
| precision_recall_f1_macro_n1000000_k2 | 8.846 | 109.402 | 12.37x | 8.845 | 109.375 | 12.37x |
| classification_report_n1000000_k2 | 8.850 | 214.643 | 24.25x | 8.849 | 214.623 | 24.25x |
| roc_auc_binary_n1000000_k2 | 53.307 | 295.884 | 5.55x | 53.301 | 295.842 | 5.55x |
| balanced_accuracy_n1000000_k2 | 8.759 | 101.540 | 11.59x | 8.758 | 101.523 | 11.59x |
| matthews_n1000000_k2 | 8.871 | 203.679 | 22.96x | 8.869 | 203.650 | 22.96x |
| cohen_kappa_n1000000_k2 | 8.751 | 101.741 | 11.63x | 8.749 | 101.733 | 11.63x |
| mse_n1000000_k2 | 2.393 | 2.624 | 1.10x | 2.393 | 2.624 | 1.10x |
| mae_n1000000_k2 | 2.412 | 2.607 | 1.08x | 2.412 | 2.606 | 1.08x |
| median_ae_n1000000_k2 | 6.605 | 14.707 | 2.23x | 6.762 | 14.704 | 2.17x |
| r2_n1000000_k2 | 2.391 | 4.956 | 2.07x | 2.390 | 4.955 | 2.07x |
| confusion_matrix_n1000000_k10 | 9.749 | 102.737 | 10.54x | 9.747 | 102.711 | 10.54x |
| accuracy_n1000000_k10 | 2.803 | 33.900 | 12.09x | 2.803 | 33.897 | 12.09x |
| precision_recall_f1_macro_n1000000_k10 | 9.845 | 116.162 | 11.80x | 9.844 | 116.143 | 11.80x |
| classification_report_n1000000_k10 | 9.901 | 238.695 | 24.11x | 9.900 | 238.674 | 24.11x |
| balanced_accuracy_n1000000_k10 | 9.780 | 101.113 | 10.34x | 9.780 | 101.108 | 10.34x |
| matthews_n1000000_k10 | 9.869 | 211.191 | 21.40x | 9.867 | 211.163 | 21.40x |
| cohen_kappa_n1000000_k10 | 9.838 | 101.656 | 10.33x | 9.837 | 101.650 | 10.33x |
| mse_n1000000_k10 | 2.397 | 2.514 | 1.05x | 2.397 | 2.513 | 1.05x |
| mae_n1000000_k10 | 2.411 | 2.688 | 1.11x | 2.411 | 2.687 | 1.11x |
| median_ae_n1000000_k10 | 6.717 | 14.768 | 2.20x | 6.854 | 14.768 | 2.15x |
| r2_n1000000_k10 | 2.698 | 4.518 | 1.67x | 2.698 | 4.518 | 1.67x |

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
| vocab_txt | 4.949 | 9.964 | 2.01x | 5.245 | 9.963 | 1.90x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.111 | 17.001 | 1.40x | 12.474 | 16.998 | 1.36x | 706,526 | 706,526 |
| tokenizer_json_unigram | 13.410 | 39.565 | 2.95x | 14.208 | 39.557 | 2.78x | 1,990,038 | 1,990,038 |
| spiece_model | 4.757 | 29.482 | 6.20x | 5.011 | 29.480 | 5.88x | 533,084 | 533,084 |
| tfidf_save | 1.893 | 2.494 | 1.32x | 1.935 | 2.493 | 1.29x | 581,787 | 591,922 |
| tfidf_load | 4.750 | 4.042 | 0.85x | 4.980 | 4.042 | 0.81x | 581,787 | 591,922 |
| embedding_index_save | 4.469 | 1.723 | 0.39x | 4.765 | 1.723 | 0.36x | 20,589,007 | 15,360,128 |
| embedding_index_load | 6.545 | 1.653 | 0.25x | 7.329 | 1.653 | 0.23x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 7.596 | 1.014 | 0.13x | 8.379 | 1.013 | 0.12x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 4.871 | 1.616 | 0.33x | 5.421 | 1.616 | 0.30x | 20,589,007 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 86.71x | 0.000 | 0.001 | 86.70x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 411.733 | 556.806 | 1.35x | 413.683 | 556.731 | 1.35x | 15,250,490 | 14,022,374 |
| embedding_index_load_gzip | 79.392 | 66.979 | 0.84x | 81.254 | 66.970 | 0.82x | 15,250,490 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
