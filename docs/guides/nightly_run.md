# Nightly benchmark run

<!-- nightly-baseline: 330991da387451cd3f4cfbe0dc421e67c1c1faf0 -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `330991da387451cd3f4cfbe0dc421e67c1c1faf0`
- Previous run: `330991da387451cd3f4cfbe0dc421e67c1c1faf0`
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
| DpGroup    |  13.72 μs | 0.711 μs | 0.039 μs |         - |
| MyersGroup | 114.84 μs | 0.787 μs | 0.043 μs |         - |

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
| Ratio          |    107.7 ns |   4.33 ns |  0.24 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 18,208.4 ns | 245.68 ns | 13.47 ns | 169.08 |    0.34 |      - |         - |          NA |
| TokenSortRatio |    985.4 ns | 242.91 ns | 13.31 ns |   9.15 |    0.11 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,192.0 ns | 477.35 ns | 26.17 ns |  29.64 |    0.22 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,423.6 ns | 842.21 ns | 46.16 ns |  41.08 |    0.38 | 0.4272 |    7200 B |          NA |

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
| **Distance_Utf16**             | **8**      |      **29.27 ns** |      **2.537 ns** |     **0.139 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     130.76 ns |      1.982 ns |     0.109 ns |  4.47 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      31.42 ns |      0.933 ns |     0.051 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      30.14 ns |      1.109 ns |     0.061 ns |  1.03 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **30.76 ns** |      **3.804 ns** |     **0.208 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     138.72 ns |     11.688 ns |     0.641 ns |  4.51 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      32.98 ns |      0.263 ns |     0.014 ns |  1.07 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      32.25 ns |      0.507 ns |     0.028 ns |  1.05 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **34.85 ns** |      **0.847 ns** |     **0.046 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     148.28 ns |      0.984 ns |     0.054 ns |  4.25 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      33.64 ns |      3.162 ns |     0.173 ns |  0.97 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      36.11 ns |      4.866 ns |     0.267 ns |  1.04 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **93.39 ns** |      **0.881 ns** |     **0.048 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     187.84 ns |      8.861 ns |     0.486 ns |  2.01 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      90.69 ns |      0.980 ns |     0.054 ns |  0.97 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      91.86 ns |     16.065 ns |     0.881 ns |  0.98 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **61.31 ns** |      **5.143 ns** |     **0.282 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     864.38 ns |     67.551 ns |     3.703 ns | 14.10 |    0.08 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      57.64 ns |      3.464 ns |     0.190 ns |  0.94 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      61.24 ns |      2.252 ns |     0.123 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **65.23 ns** |      **3.054 ns** |     **0.167 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,048.63 ns |    339.315 ns |    18.599 ns | 16.08 |    0.25 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      69.77 ns |      4.158 ns |     0.228 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      65.70 ns |      1.254 ns |     0.069 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,227.83 ns** |    **131.420 ns** |     **7.204 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  18,270.94 ns |  4,343.668 ns |   238.091 ns | 14.88 |    0.18 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,274.96 ns |      5.696 ns |     0.312 ns |  1.04 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,222.80 ns |     96.247 ns |     5.276 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **8,893.99 ns** |    **229.089 ns** |    **12.557 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 312,200.79 ns | 20,465.680 ns | 1,121.793 ns | 35.10 |    0.12 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   9,039.00 ns |     92.460 ns |     5.068 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   8,735.41 ns |    383.961 ns |    21.046 ns |  0.98 |    0.00 |         - |          NA |

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
| **Dp**     | **8**    |    **130.05 ns** |     **3.266 ns** |   **0.179 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |     65.84 ns |     3.493 ns |   0.191 ns |  0.51 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **12**   |    **221.12 ns** |    **12.483 ns** |   **0.684 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 12   |     69.33 ns |     2.124 ns |   0.116 ns |  0.31 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **14**   |    **296.86 ns** |   **326.256 ns** |  **17.883 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel | 14   |     73.06 ns |     3.127 ns |   0.171 ns |  0.25 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **16**   |    **347.82 ns** |    **39.334 ns** |   **2.156 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 16   |     77.64 ns |     0.522 ns |   0.029 ns |  0.22 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **18**   |    **732.72 ns** |   **556.945 ns** |  **30.528 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel | 18   |     83.34 ns |     0.605 ns |   0.033 ns |  0.11 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **20**   |    **819.61 ns** |    **21.407 ns** |   **1.173 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 20   |     86.62 ns |     0.639 ns |   0.035 ns |  0.11 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **24**   |  **1,008.78 ns** |   **485.689 ns** |  **26.622 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 24   |     96.58 ns |    15.206 ns |   0.834 ns |  0.10 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **32**   |  **1,679.69 ns** |   **763.791 ns** |  **41.866 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 32   |    117.09 ns |     1.044 ns |   0.057 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **48**   |  **3,258.17 ns** |   **244.331 ns** |  **13.393 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |    146.02 ns |    49.771 ns |   2.728 ns |  0.04 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **64**   |  **5,404.92 ns** | **1,948.832 ns** | **106.822 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 64   |    171.90 ns |     0.009 ns |   0.000 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **96**   | **10,935.03 ns** | **7,009.641 ns** | **384.222 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 96   |  1,006.65 ns |    12.133 ns |   0.665 ns |  0.09 |    0.00 |         - |          NA |

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
| **Distance_Utf16**             | **8**      |     **27.64 ns** |     **1.959 ns** |   **0.107 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    125.56 ns |     5.611 ns |   0.308 ns |  4.54 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     28.21 ns |     1.114 ns |   0.061 ns |  1.02 |    0.00 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **301.76 ns** |     **2.738 ns** |   **0.150 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    704.88 ns |   111.862 ns |   6.132 ns |  2.34 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    304.21 ns |    11.057 ns |   0.606 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    | **16,298.87 ns** | **2,769.946 ns** | **151.830 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 18,023.07 ns |   428.709 ns |  23.499 ns |  1.11 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 16,764.06 ns | 1,285.401 ns |  70.457 ns |  1.03 |    0.01 |         - |          NA |

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

| Method             | Length | Distinct | Mean           | Error         | StdDev       | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |---------------:|--------------:|-------------:|-------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |       **357.7 ns** |      **10.73 ns** |      **0.59 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,531.1 ns |      35.56 ns |      1.95 ns |   4.28 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **379.2 ns** |      **13.54 ns** |      **0.74 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,514.7 ns |      18.27 ns |      1.00 ns |   3.99 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **445.2 ns** |       **6.41 ns** |      **0.35 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,426.6 ns |      99.36 ns |      5.45 ns |   7.70 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **470.7 ns** |      **57.42 ns** |      **3.15 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,430.7 ns |      43.21 ns |      2.37 ns |   7.29 |    0.04 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **535.1 ns** |     **125.96 ns** |      **6.90 ns** |   **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,210.8 ns |     350.71 ns |     19.22 ns |  11.61 |    0.13 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **563.7 ns** |       **1.25 ns** |      **0.07 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,226.0 ns |     240.68 ns |     13.19 ns |  11.05 |    0.02 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **630.4 ns** |      **93.60 ns** |      **5.13 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |     9,876.5 ns |     360.09 ns |     19.74 ns |  15.67 |    0.11 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **630.8 ns** |       **3.31 ns** |      **0.18 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |     9,551.4 ns |      90.66 ns |      4.97 ns |  15.14 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,444.7 ns** |     **680.21 ns** |     **37.28 ns** |   **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   102,340.9 ns |  19,108.19 ns |  1,047.38 ns |  41.87 |    0.66 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,430.6 ns** |     **137.69 ns** |      **7.55 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   102,882.9 ns |   7,113.39 ns |    389.91 ns |  42.33 |    0.18 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **20,539.6 ns** |     **468.18 ns** |     **25.66 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,460,236.9 ns | 598,446.25 ns | 32,802.86 ns | 119.78 |    1.39 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **446,586.1 ns** |  **83,767.15 ns** |  **4,591.56 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,801,763.2 ns |  94,908.99 ns |  5,202.28 ns |   4.03 |    0.04 |         - |          NA |

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
| **Dp**     | **4**    |     **81.86 ns** |   **2.656 ns** |  **0.146 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 4    |     81.84 ns |   0.650 ns |  0.036 ns |  1.00 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **6**    |    **115.43 ns** |   **9.434 ns** |  **0.517 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 6    |    115.37 ns |   2.029 ns |  0.111 ns |  1.00 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **8**    |    **157.52 ns** |   **4.805 ns** |  **0.263 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 8    |    100.77 ns |   0.864 ns |  0.047 ns |  0.64 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **10**   |    **207.52 ns** |   **8.324 ns** |  **0.456 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 10   |    112.16 ns |   1.213 ns |  0.066 ns |  0.54 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **12**   |    **271.20 ns** |   **8.000 ns** |  **0.439 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 12   |    126.83 ns |   0.417 ns |  0.023 ns |  0.47 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **16**   |    **429.52 ns** |  **23.317 ns** |  **1.278 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 16   |    140.01 ns |   3.503 ns |  0.192 ns |  0.33 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **24**   |    **871.17 ns** | **166.398 ns** |  **9.121 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 24   |    179.40 ns |   2.096 ns |  0.115 ns |  0.21 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **32**   |  **1,488.98 ns** |   **9.789 ns** |  **0.537 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 32   |    219.65 ns |   3.384 ns |  0.185 ns |  0.15 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **48**   |  **3,250.63 ns** |   **9.112 ns** |  **0.499 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 48   |    304.48 ns |  58.036 ns |  3.181 ns |  0.09 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **64**   |  **5,689.22 ns** | **599.770 ns** | **32.875 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 64   |    390.30 ns |  14.506 ns |  0.795 ns |  0.07 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **96**   | **12,566.61 ns** | **168.858 ns** |  **9.256 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 96   |  1,107.22 ns |  70.299 ns |  3.853 ns |  0.09 |         - |          NA |

<!-- markdownlint-enable MD060 -->

## Against rapidfuzz, in this same run

Both sides on this VM in these minutes, which is what makes the ratio readable where the absolutes are not.

- `indel`
- `levenshtein`
- `persistence`

### compare-indel

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 111.4 | 23.9 | 4.66x C# faster |
| 32 | 158.9 | 75.1 | 2.12x C# faster |
| 128 | 479.0 | 769.1 | 1.61x Py faster |
| 512 | 4864.3 | 7981.2 | 1.64x Py faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 140.7 | 18.3 | 7.70x C# faster |
| 32 | 264.4 | 133.0 | 1.99x C# faster |
| 128 | 1817.6 | 1439.7 | 1.26x C# faster |
| 512 | 15633.0 | 16366.8 | 1.05x Py faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-persistence

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.844 | 9.865 | 2.04x | 5.174 | 9.863 | 1.91x |
| tokenizer_json_wordpiece | 12.101 | 17.551 | 1.45x | 12.454 | 17.550 | 1.41x |
| tokenizer_json_unigram | 10.671 | 40.574 | 3.80x | 11.389 | 40.565 | 3.56x |
| spiece_model | 5.006 | 31.123 | 6.22x | 5.319 | 31.118 | 5.85x |
| tfidf_save | 1.823 | 2.559 | 1.40x | 1.909 | 2.559 | 1.34x |
| tfidf_load | 4.064 | 4.346 | 1.07x | 4.320 | 4.345 | 1.01x |
| embedding_index_save | 4.910 | 2.825 | 0.58x | 5.566 | 2.824 | 0.51x |
| embedding_index_load | 4.405 | 1.415 | 0.32x | 4.794 | 1.415 | 0.30x |
| embedding_index_load_file | 5.488 | 0.938 | 0.17x | 6.039 | 0.936 | 0.15x |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.

<!-- markdownlint-enable MD060 -->
