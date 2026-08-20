# Nightly benchmark run

<!-- nightly-baseline: 6bde87c833c7f936cefed0881eabbccbb2cb2f1f -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`
- Previous run: `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BucketRouteDiagnostics`
- `FuzzBenchmarks`
- `IndelBenchmarks`
- `LcsGateBenchmarks`
- `LevenshteinBenchmarks`
- `LevenshteinCodePointBenchmarks`
- `MyersGateBenchmarks`

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

| Method     | Mean      | Error    | StdDev   | Allocated |
|----------- |----------:|---------:|---------:|----------:|
| DpGroup    |  13.66 μs | 0.293 μs | 0.016 μs |         - |
| MyersGroup | 114.45 μs | 1.663 μs | 0.091 μs |         - |

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

| Method         | Mean        | Error     | StdDev   | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |------------:|----------:|---------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |    108.8 ns |   1.56 ns |  0.09 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 18,009.4 ns | 903.43 ns | 49.52 ns | 165.48 |    0.41 |      - |         - |          NA |
| TokenSortRatio |    944.5 ns |  39.15 ns |  2.15 ns |   8.68 |    0.02 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,184.8 ns | 132.87 ns |  7.28 ns |  29.26 |    0.06 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,565.5 ns | 850.73 ns | 46.63 ns |  41.95 |    0.37 | 0.4272 |    7200 B |          NA |

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
| **Distance_Utf16**             | **8**      |      **29.47 ns** |      **0.283 ns** |     **0.015 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     131.49 ns |      7.121 ns |     0.390 ns |  4.46 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      30.27 ns |      0.552 ns |     0.030 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      28.81 ns |      0.194 ns |     0.011 ns |  0.98 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **32.06 ns** |      **0.542 ns** |     **0.030 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     139.71 ns |      4.418 ns |     0.242 ns |  4.36 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      32.95 ns |      0.115 ns |     0.006 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      30.50 ns |      0.998 ns |     0.055 ns |  0.95 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **34.77 ns** |      **1.635 ns** |     **0.090 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     147.94 ns |     22.131 ns |     1.213 ns |  4.25 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      36.70 ns |      1.701 ns |     0.093 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      35.67 ns |      1.469 ns |     0.081 ns |  1.03 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **89.91 ns** |      **1.795 ns** |     **0.098 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     186.54 ns |      2.142 ns |     0.117 ns |  2.07 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      91.64 ns |      1.835 ns |     0.101 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      92.23 ns |      2.542 ns |     0.139 ns |  1.03 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **61.77 ns** |      **3.992 ns** |     **0.219 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     859.29 ns |    352.191 ns |    19.305 ns | 13.91 |    0.27 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      57.71 ns |      8.947 ns |     0.490 ns |  0.93 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      63.84 ns |     10.009 ns |     0.549 ns |  1.03 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **65.22 ns** |      **3.411 ns** |     **0.187 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,063.72 ns |    207.056 ns |    11.349 ns | 16.31 |    0.16 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      69.71 ns |      0.601 ns |     0.033 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      65.01 ns |      3.627 ns |     0.199 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,449.13 ns** |    **155.845 ns** |     **8.542 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  21,824.95 ns | 22,364.650 ns | 1,225.882 ns | 15.06 |    0.74 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,465.74 ns |     83.533 ns |     4.579 ns |  1.01 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,408.28 ns |      5.026 ns |     0.276 ns |  0.97 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |  **12,544.34 ns** |    **219.591 ns** |    **12.037 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 310,332.47 ns | 77,998.119 ns | 4,275.340 ns | 24.74 |    0.30 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |  12,625.76 ns |     81.047 ns |     4.442 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |  12,663.70 ns |     92.240 ns |     5.056 ns |  1.01 |    0.00 |         - |          NA |

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

| Method | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**     | **8**    |    **130.92 ns** |    **29.556 ns** |   **1.620 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 8    |     65.76 ns |     1.471 ns |   0.081 ns |  0.50 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **12**   |    **222.28 ns** |    **39.802 ns** |   **2.182 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 12   |     69.46 ns |     0.501 ns |   0.027 ns |  0.31 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **14**   |    **296.74 ns** |   **322.353 ns** |  **17.669 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel | 14   |     73.20 ns |     0.596 ns |   0.033 ns |  0.25 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **16**   |    **358.57 ns** |   **299.625 ns** |  **16.423 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel | 16   |     77.81 ns |     0.883 ns |   0.048 ns |  0.22 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **18**   |    **745.92 ns** |   **301.683 ns** |  **16.536 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 18   |     83.55 ns |     3.556 ns |   0.195 ns |  0.11 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **20**   |    **828.44 ns** |   **140.667 ns** |   **7.710 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 20   |     88.93 ns |     0.835 ns |   0.046 ns |  0.11 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **24**   |  **1,005.43 ns** |   **323.306 ns** |  **17.721 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 24   |     96.48 ns |     1.030 ns |   0.056 ns |  0.10 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **32**   |  **1,603.80 ns** |   **721.131 ns** |  **39.528 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 32   |    116.79 ns |    27.889 ns |   1.529 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **48**   |  **3,259.76 ns** |   **713.374 ns** |  **39.102 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |    145.96 ns |    17.750 ns |   0.973 ns |  0.04 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **64**   |  **5,258.48 ns** | **3,832.506 ns** | **210.073 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel | 64   |    173.82 ns |     2.862 ns |   0.157 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **96**   | **11,607.35 ns** | **2,164.185 ns** | **118.626 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 96   |  1,146.96 ns |    15.852 ns |   0.869 ns |  0.10 |    0.00 |         - |          NA |

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
| **Distance_Utf16**             | **8**      |     **34.56 ns** |     **1.854 ns** |   **0.102 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    128.73 ns |    42.829 ns |   2.348 ns |  3.73 |    0.06 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     28.12 ns |     0.211 ns |   0.012 ns |  0.81 |    0.00 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **304.54 ns** |   **109.428 ns** |   **5.998 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    738.81 ns |   196.952 ns |  10.796 ns |  2.43 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    303.71 ns |     4.114 ns |   0.225 ns |  1.00 |    0.02 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    | **15,337.55 ns** |   **673.909 ns** |  **36.939 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 18,758.97 ns | 2,026.986 ns | 111.106 ns |  1.22 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 16,412.86 ns | 3,675.965 ns | 201.492 ns |  1.07 |    0.01 |         - |          NA |

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

| Method             | Length | Distinct | Mean           | Error         | StdDev      | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |---------------:|--------------:|------------:|-------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |       **354.1 ns** |       **2.73 ns** |     **0.15 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,516.3 ns |      40.90 ns |     2.24 ns |   4.28 |    0.01 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **353.0 ns** |       **0.59 ns** |     **0.03 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,513.7 ns |     205.49 ns |    11.26 ns |   4.29 |    0.03 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **446.4 ns** |       **4.23 ns** |     **0.23 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,423.2 ns |      42.93 ns |     2.35 ns |   7.67 |    0.01 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **489.7 ns** |     **389.40 ns** |    **21.34 ns** |   **1.00** |    **0.05** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,557.7 ns |     109.33 ns |     5.99 ns |   7.27 |    0.28 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **535.4 ns** |       **0.32 ns** |     **0.02 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,063.4 ns |     601.71 ns |    32.98 ns |  11.33 |    0.05 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **566.7 ns** |      **14.74 ns** |     **0.81 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,330.7 ns |     251.83 ns |    13.80 ns |  11.17 |    0.03 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **623.9 ns** |      **47.05 ns** |     **2.58 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |    10,211.9 ns |     223.72 ns |    12.26 ns |  16.37 |    0.06 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **654.8 ns** |      **68.17 ns** |     **3.74 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |     9,799.3 ns |     141.43 ns |     7.75 ns |  14.97 |    0.07 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,384.3 ns** |      **56.06 ns** |     **3.07 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   103,097.9 ns |  15,093.75 ns |   827.34 ns |  43.24 |    0.30 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,406.0 ns** |       **8.50 ns** |     **0.47 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   109,957.4 ns |  15,676.07 ns |   859.26 ns |  45.70 |    0.31 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **20,475.5 ns** |     **169.41 ns** |     **9.29 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,391,970.0 ns | 174,601.66 ns | 9,570.51 ns | 116.82 |    0.41 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **447,403.6 ns** |  **69,150.90 ns** | **3,790.39 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,803,494.5 ns |  86,631.99 ns | 4,748.59 ns |   4.03 |    0.03 |         - |          NA |

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

| Method | Band | Mean         | Error      | StdDev    | Ratio | Allocated | Alloc Ratio |
|------- |----- |-------------:|-----------:|----------:|------:|----------:|------------:|
| **Dp**     | **4**    |     **84.36 ns** |   **4.417 ns** |  **0.242 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 4    |     82.32 ns |   0.359 ns |  0.020 ns |  0.98 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **6**    |    **117.28 ns** |  **16.750 ns** |  **0.918 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 6    |    116.84 ns |   1.883 ns |  0.103 ns |  1.00 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **8**    |    **157.75 ns** |  **12.413 ns** |  **0.680 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 8    |     98.64 ns |   3.593 ns |  0.197 ns |  0.63 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **10**   |    **203.13 ns** |   **0.828 ns** |  **0.045 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 10   |    111.75 ns |   0.960 ns |  0.053 ns |  0.55 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **12**   |    **269.85 ns** |   **1.480 ns** |  **0.081 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 12   |    119.37 ns |   5.058 ns |  0.277 ns |  0.44 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **16**   |    **428.12 ns** |   **6.445 ns** |  **0.353 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 16   |    140.85 ns |   0.064 ns |  0.004 ns |  0.33 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **24**   |    **863.38 ns** |   **5.261 ns** |  **0.288 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 24   |    178.74 ns |   1.590 ns |  0.087 ns |  0.21 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **32**   |  **1,480.81 ns** |  **33.616 ns** |  **1.843 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 32   |    220.62 ns |  41.108 ns |  2.253 ns |  0.15 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **48**   |  **3,260.29 ns** | **254.775 ns** | **13.965 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 48   |    307.04 ns |  16.014 ns |  0.878 ns |  0.09 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **64**   |  **5,654.41 ns** | **149.414 ns** |  **8.190 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 64   |    390.27 ns |  29.095 ns |  1.595 ns |  0.07 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **96**   | **12,679.77 ns** | **143.441 ns** |  **7.862 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 96   |  1,097.10 ns |  33.113 ns |  1.815 ns |  0.09 |         - |          NA |

<!-- markdownlint-enable MD060 -->

## Against rapidfuzz, in this same run

Both sides on this VM in these minutes, which is what makes the ratio readable where the absolutes are not.

- `indel`
- `levenshtein`

### compare-indel

```text
Python: rapidfuzz 3.14.5 (py 3.12.13)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 111.1 | 24.1 | 4.60x C# faster |
| 32 | 157.8 | 75.3 | 2.10x C# faster |
| 128 | 478.8 | 957.1 | 2.00x Py faster |
| 512 | 4873.8 | 11410.3 | 2.34x Py faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

```text
Python: rapidfuzz 3.14.5 (py 3.12.13)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 151.0 | 18.4 | 8.21x C# faster |
| 32 | 267.5 | 132.9 | 2.01x C# faster |
| 128 | 1828.1 | 1435.6 | 1.27x C# faster |
| 512 | 15596.2 | 16268.0 | 1.04x Py faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->
