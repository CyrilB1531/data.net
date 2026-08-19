# Nightly benchmark run

<!-- nightly-baseline: dfa2b7885eed72b2bcb9c82bca1ef016cf4cc53d -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `dfa2b7885eed72b2bcb9c82bca1ef016cf4cc53d`
- Previous run: `dfa2b7885eed72b2bcb9c82bca1ef016cf4cc53d`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
- `BpeBenchmarks`
- `BpeScalingBenchmarks`
- `FuzzBenchmarks`
- `IndelBenchmarks`
- `LcsGateBenchmarks`
- `LevenshteinBenchmarks`
- `LevenshteinCodePointBenchmarks`
- `MetricsBenchmarks`
- `PersistenceBenchmarks`
- `StopWordBenchmarks`
- `VectorMathBenchmarks`
- `VectorizerBenchmarks`

### Lodestar.Text.Benchmarks.BatchEmbeddingBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | CorpusSize | Mean         | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|-----------:|------:|--------:|--------:|-------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **4.467 μs** |   **6.6944 μs** |  **0.3669 μs** |  **1.00** |    **0.10** |  **0.0305** |      **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     4.318 μs |   0.4210 μs |  0.0231 μs |  0.97 |    0.07 |  0.0381 |      - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     4.411 μs |   1.5534 μs |  0.0851 μs |  0.99 |    0.07 |  0.0381 |      - |    3.16 KB |        1.14 |
|                    |            |              |             |            |       |         |         |        |            |             |
| **UnitLoop**           | **8**          |    **79.495 μs** |  **25.4879 μs** |  **1.3971 μs** |  **1.00** |    **0.02** |  **1.5869** |      **-** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    50.076 μs |   3.3620 μs |  0.1843 μs |  0.63 |    0.01 |  1.5259 | 0.0610 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    48.942 μs |   6.6873 μs |  0.3666 μs |  0.62 |    0.01 |  1.5259 | 0.0610 |  127.31 KB |        0.95 |
|                    |            |              |             |            |       |         |         |        |            |             |
| **UnitLoop**           | **32**         |   **287.680 μs** |  **22.7420 μs** |  **1.2466 μs** |  **1.00** |    **0.01** |  **5.3711** |      **-** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   173.938 μs |  17.4780 μs |  0.9580 μs |  0.60 |    0.00 |  5.3711 | 0.2441 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   169.004 μs | 139.3828 μs |  7.6400 μs |  0.59 |    0.02 |  5.1270 | 0.2441 |   427.8 KB |        0.91 |
|                    |            |              |             |            |       |         |         |        |            |             |
| **UnitLoop**           | **128**        | **1,132.055 μs** | **493.2407 μs** | **27.0362 μs** |  **1.00** |    **0.03** | **21.4844** |      **-** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   701.486 μs |  99.4776 μs |  5.4527 μs |  0.62 |    0.01 | 21.4844 | 2.9297 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   668.984 μs |  28.1772 μs |  1.5445 μs |  0.59 |    0.01 | 20.5078 | 2.9297 |  1696.9 KB |        0.91 |

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method  | Mean     | Error    | StdDev  | Ratio | Gen0      | Allocated | Alloc Ratio |
|-------- |---------:|---------:|--------:|------:|----------:|----------:|------------:|
| Unigram | 456.2 ms | 17.41 ms | 0.95 ms |  1.00 | 6000.0000 | 519.51 MB |        1.00 |
| Bpe     | 435.0 ms | 27.35 ms | 1.50 ms |  0.95 | 1000.0000 | 112.18 MB |        0.22 |

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Length | Mean      | Error     | StdDev   | Gen0   | Allocated |
|-------------------------- |------- |----------:|----------:|---------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |  **89.21 μs** |  **9.283 μs** | **0.509 μs** | **0.2441** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **187.26 μs** |  **5.391 μs** | **0.296 μs** | **0.4883** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **484.47 μs** | **71.341 μs** | **3.910 μs** | **0.4883** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **784.49 μs** | **16.165 μs** | **0.886 μs** | **0.9766** | **157.03 KB** |

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|-------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     85.59 ns |     0.727 ns |   0.040 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 27,902.02 ns | 1,697.291 ns |  93.034 ns | 326.00 |    0.95 |      - |         - |          NA |
| TokenSortRatio |    726.79 ns |    71.731 ns |   3.932 ns |   8.49 |    0.04 | 0.0153 |    1312 B |          NA |
| TokenSetRatio  |  2,645.26 ns | 2,402.447 ns | 131.686 ns |  30.91 |    1.33 | 0.0687 |    5760 B |          NA |
| WRatio         |  3,274.12 ns |   857.854 ns |  47.022 ns |  38.25 |    0.48 | 0.0839 |    7200 B |          NA |

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Length | Mean          | Error          | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|---------------:|-------------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **16.14 ns** |       **0.404 ns** |     **0.022 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |      91.71 ns |       1.179 ns |     0.065 ns |  5.68 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      17.02 ns |       1.084 ns |     0.059 ns |  1.05 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      16.64 ns |      14.850 ns |     0.814 ns |  1.03 |    0.04 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **17.78 ns** |       **0.175 ns** |     **0.010 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     109.88 ns |     159.848 ns |     8.762 ns |  6.18 |    0.43 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      18.09 ns |       5.127 ns |     0.281 ns |  1.02 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      17.29 ns |       2.689 ns |     0.147 ns |  0.97 |    0.01 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **18.38 ns** |       **0.187 ns** |     **0.010 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     104.64 ns |       6.445 ns |     0.353 ns |  5.69 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      20.44 ns |       1.724 ns |     0.094 ns |  1.11 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      20.87 ns |      12.121 ns |     0.664 ns |  1.14 |    0.03 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **65.25 ns** |      **67.697 ns** |     **3.711 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     130.79 ns |       8.268 ns |     0.453 ns |  2.01 |    0.10 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      65.56 ns |       6.219 ns |     0.341 ns |  1.01 |    0.05 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      64.20 ns |       1.987 ns |     0.109 ns |  0.99 |    0.05 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **48.97 ns** |       **0.379 ns** |     **0.021 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     474.94 ns |       3.261 ns |     0.179 ns |  9.70 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      56.70 ns |      43.360 ns |     2.377 ns |  1.16 |    0.04 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      48.38 ns |       0.121 ns |     0.007 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **55.95 ns** |       **2.594 ns** |     **0.142 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |     524.36 ns |      53.920 ns |     2.956 ns |  9.37 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      58.30 ns |       0.597 ns |     0.033 ns |  1.04 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      58.24 ns |      28.162 ns |     1.544 ns |  1.04 |    0.02 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **717.95 ns** |      **10.314 ns** |     **0.565 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  14,604.76 ns |     373.738 ns |    20.486 ns | 20.34 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     754.01 ns |       1.719 ns |     0.094 ns |  1.05 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     791.80 ns |     328.138 ns |    17.986 ns |  1.10 |    0.02 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **7,525.67 ns** |   **7,672.283 ns** |   **420.544 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 277,099.42 ns | 107,005.495 ns | 5,865.332 ns | 36.90 |    1.86 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   7,470.12 ns |   6,597.146 ns |   361.612 ns |  0.99 |    0.06 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   7,185.56 ns |     100.365 ns |     5.501 ns |  0.96 |    0.04 |         - |          NA |

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Band | Mean        | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |------------:|-------------:|----------:|------:|--------:|----------:|------------:|
| **Dp**     | **8**    |    **86.98 ns** |     **6.819 ns** |  **0.374 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 8    |    97.98 ns |    45.598 ns |  2.499 ns |  1.13 |    0.03 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **12**   |   **154.03 ns** |    **67.345 ns** |  **3.691 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 12   |   154.07 ns |    43.365 ns |  2.377 ns |  1.00 |    0.02 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **14**   |   **195.12 ns** |     **5.106 ns** |  **0.280 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 14   |   191.70 ns |     2.293 ns |  0.126 ns |  0.98 |    0.00 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **16**   |   **240.24 ns** |    **13.219 ns** |  **0.725 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 16   |    68.44 ns |     1.992 ns |  0.109 ns |  0.28 |    0.00 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **18**   |   **302.53 ns** |   **314.477 ns** | **17.238 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel | 18   |    88.11 ns |    86.669 ns |  4.751 ns |  0.29 |    0.02 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **20**   |   **392.10 ns** |    **26.118 ns** |  **1.432 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 20   |    72.50 ns |    56.280 ns |  3.085 ns |  0.18 |    0.01 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **24**   |   **516.67 ns** |    **64.091 ns** |  **3.513 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 24   |    81.73 ns |   117.876 ns |  6.461 ns |  0.16 |    0.01 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **32**   |   **907.51 ns** | **1,321.389 ns** | **72.430 ns** |  **1.00** |    **0.10** |         **-** |          **NA** |
| Kernel | 32   |    99.05 ns |     0.662 ns |  0.036 ns |  0.11 |    0.01 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **48**   | **2,126.17 ns** |   **140.079 ns** |  **7.678 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 48   |   129.28 ns |    64.300 ns |  3.524 ns |  0.06 |    0.00 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **64**   | **3,556.02 ns** |   **132.698 ns** |  **7.274 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 64   |   134.88 ns |    10.591 ns |  0.581 ns |  0.04 |    0.00 |         - |          NA |
|        |      |             |              |           |       |         |           |             |
| **Dp**     | **96**   | **7,786.50 ns** |   **382.999 ns** | **20.993 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 96   |   643.67 ns |   719.474 ns | 39.437 ns |  0.08 |    0.00 |         - |          NA |

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Length | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **14.79 ns** |     **0.472 ns** |   **0.026 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     89.49 ns |    16.412 ns |   0.900 ns |  6.05 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     16.85 ns |     1.552 ns |   0.085 ns |  1.14 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **245.49 ns** |     **2.618 ns** |   **0.144 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    602.12 ns |    21.918 ns |   1.201 ns |  2.45 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    247.53 ns |    26.630 ns |   1.460 ns |  1.01 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    |  **9,059.01 ns** |   **132.932 ns** |   **7.286 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 11,048.12 ns | 7,165.460 ns | 392.763 ns |  1.22 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |  9,078.77 ns |   184.743 ns |  10.126 ns |  1.00 |    0.00 |         - |          NA |

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Length | Distinct | Mean           | Error         | StdDev       | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |---------------:|--------------:|-------------:|-------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |       **261.0 ns** |      **16.01 ns** |      **0.88 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,053.4 ns |      47.48 ns |      2.60 ns |   4.04 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **283.5 ns** |       **6.95 ns** |      **0.38 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,098.1 ns |     661.59 ns |     36.26 ns |   3.87 |    0.11 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **390.3 ns** |     **546.01 ns** |     **29.93 ns** |   **1.00** |    **0.09** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     2,441.0 ns |       0.35 ns |      0.02 ns |   6.28 |    0.40 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **380.6 ns** |      **24.85 ns** |      **1.36 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     2,622.2 ns |     923.14 ns |     50.60 ns |   6.89 |    0.12 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **441.7 ns** |     **556.27 ns** |     **30.49 ns** |   **1.00** |    **0.08** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     4,835.6 ns |     460.07 ns |     25.22 ns |  10.98 |    0.63 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **449.7 ns** |       **9.58 ns** |      **0.52 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     4,930.8 ns |   6,966.13 ns |    381.84 ns |  10.96 |    0.74 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **562.4 ns** |     **258.12 ns** |     **14.15 ns** |   **1.00** |    **0.03** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |     7,727.9 ns |   9,774.30 ns |    535.76 ns |  13.75 |    0.88 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **513.6 ns** |      **53.90 ns** |      **2.95 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |     7,742.6 ns |     116.75 ns |      6.40 ns |  15.08 |    0.08 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **1,549.9 ns** |      **18.45 ns** |      **1.01 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   121,043.1 ns |  47,787.62 ns |  2,619.40 ns |  78.10 |    1.46 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **1,609.4 ns** |      **14.22 ns** |      **0.78 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |    97,031.9 ns | 128,829.87 ns |  7,061.60 ns |  60.29 |    3.80 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **11,691.7 ns** |      **16.35 ns** |      **0.90 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,062,540.7 ns |  29,987.68 ns |  1,643.73 ns | 176.41 |    0.12 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **473,148.1 ns** | **305,787.98 ns** | **16,761.27 ns** |   **1.00** |    **0.04** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,564,921.9 ns |  16,357.19 ns |    896.59 ns |   3.31 |    0.10 |         - |          NA |

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Samples | Classes | Mean           | Error           | StdDev        | Gen0   | Allocated |
|--------------- |-------- |-------- |---------------:|----------------:|--------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |     **4,327.1 ns** |     **4,864.51 ns** |     **266.64 ns** |      **-** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     4,304.9 ns |       150.47 ns |       8.25 ns |      - |     312 B |
| AccuracyScore  | 1000    | 2       |       616.6 ns |        13.17 ns |       0.72 ns |      - |         - |
| F1Macro        | 1000    | 2       |     4,220.4 ns |        96.13 ns |       5.27 ns |      - |     472 B |
| Report         | 1000    | 2       |     6,380.9 ns |     2,979.22 ns |     163.30 ns | 0.0763 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **4,257.0 ns** |       **119.86 ns** |       **6.57 ns** | **0.0076** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     4,502.2 ns |       202.40 ns |      11.09 ns | 0.0076 |    1248 B |
| AccuracyScore  | 1000    | 10      |       651.2 ns |       479.75 ns |      26.30 ns |      - |         - |
| F1Macro        | 1000    | 10      |     4,416.4 ns |       182.77 ns |      10.02 ns | 0.0153 |    1664 B |
| Report         | 1000    | 10      |    10,324.1 ns |     5,764.51 ns |     315.97 ns | 0.1831 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **494,979.9 ns** |    **53,351.45 ns** |   **2,924.37 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   489,627.4 ns |    19,820.01 ns |   1,086.40 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   153,307.1 ns |     2,009.21 ns |     110.13 ns |      - |         - |
| F1Macro        | 100000  | 2       |   500,602.3 ns |    16,510.55 ns |     905.00 ns |      - |     473 B |
| Report         | 100000  | 2       |   493,809.2 ns |    24,839.09 ns |   1,361.51 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **598,615.1 ns** |    **15,871.78 ns** |     **869.99 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   714,105.5 ns |    45,589.11 ns |   2,498.89 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   227,901.7 ns |    29,239.82 ns |   1,602.73 ns |      - |         - |
| F1Macro        | 100000  | 10      |   680,017.4 ns |   500,404.69 ns |  27,428.87 ns |      - |    1665 B |
| Report         | 100000  | 10      |   614,276.9 ns |    11,755.65 ns |     644.37 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **5,420,567.8 ns** |   **786,930.30 ns** |  **43,134.31 ns** |      **-** |     **318 B** |
| MatrixWeighted | 1000000 | 2       | 5,439,226.1 ns |   103,153.90 ns |   5,654.21 ns |      - |     318 B |
| AccuracyScore  | 1000000 | 2       | 1,634,588.7 ns |    36,475.84 ns |   1,999.36 ns |      - |         - |
| F1Macro        | 1000000 | 2       | 5,378,718.2 ns |   205,078.16 ns |  11,241.03 ns |      - |     478 B |
| Report         | 1000000 | 2       | 5,412,722.0 ns |   559,008.34 ns |  30,641.13 ns |      - |    6566 B |
| **Matrix**         | **1000000** | **10**      | **6,847,264.0 ns** | **6,041,708.27 ns** | **331,166.42 ns** |      **-** |    **1254 B** |
| MatrixWeighted | 1000000 | 10      | 6,495,335.8 ns |   101,201.40 ns |   5,547.19 ns |      - |    1254 B |
| AccuracyScore  | 1000000 | 10      | 2,381,511.6 ns |    36,705.98 ns |   2,011.98 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 6,606,728.7 ns |    42,156.74 ns |   2,310.75 ns |      - |    1670 B |
| Report         | 1000000 | 10      | 6,464,220.3 ns |   928,892.59 ns |  50,915.74 ns |      - |   15886 B |

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | Mean      | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated |
|----------------------- |----------:|----------:|----------:|---------:|---------:|---------:|----------:|
| VocabTxt               |  3.437 ms | 0.2872 ms | 0.0157 ms |  31.2500 |  23.4375 |  15.6250 |   3.62 MB |
| TokenizerJsonWordPiece |  6.174 ms | 2.8940 ms | 0.1586 ms |  62.5000 |  54.6875 |  39.0625 |   5.72 MB |
| TokenizerJsonUnigram   |  8.193 ms | 5.7239 ms | 0.3137 ms |  31.2500 |  31.2500 |  31.2500 |   4.64 MB |
| SpieceModel            |  2.731 ms | 1.1015 ms | 0.0604 ms |  39.0625 |  35.1563 |  23.4375 |   3.36 MB |
| TfidfSave              |  1.318 ms | 0.3795 ms | 0.0208 ms |  23.4375 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              |  3.441 ms | 3.7338 ms | 0.2047 ms |  23.4375 |  15.6250 |  15.6250 |   2.86 MB |
| EmbeddingIndexSave     | 13.047 ms | 0.5437 ms | 0.0298 ms | 562.5000 | 562.5000 | 562.5000 |  54.29 MB |
| EmbeddingIndexLoad     |  9.525 ms | 3.2917 ms | 0.1804 ms | 437.5000 | 437.5000 | 437.5000 |  35.35 MB |

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Documents | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|-----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **5.622 ms** |  **1.4327 ms** | **0.0785 ms** |  **1.00** |    **0.02** | **140.6250** | **101.5625** |  **70.3125** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  4.488 ms |  0.8984 ms | 0.0492 ms |  0.80 |    0.01 |  78.1250 |  39.0625 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  6.459 ms |  1.3392 ms | 0.0734 ms |  1.15 |    0.02 | 140.6250 |  70.3125 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  4.746 ms |  0.2300 ms | 0.0126 ms |  0.84 |    0.01 |  78.1250 |  31.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |            |           |       |         |          |          |          |           |             |
| **Count**                | **1000**      | **24.955 ms** |  **0.5453 ms** | **0.0299 ms** |  **1.00** |    **0.00** | **875.0000** | **843.7500** | **500.0000** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 18.275 ms |  1.1160 ms | 0.0612 ms |  0.73 |    0.00 | 500.0000 | 343.7500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 24.265 ms | 21.7848 ms | 1.1941 ms |  0.97 |    0.04 | 875.0000 | 593.7500 | 500.0000 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 18.730 ms |  9.7776 ms | 0.5359 ms |  0.75 |    0.02 | 500.0000 | 250.0000 | 250.0000 |  31.83 MB |        0.82 |

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Dim  | Mean      | Error      | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |----------:|-----------:|---------:|------:|--------:|----------:|------------:|
| **Dot**    | **384**  |  **40.25 ns** |   **3.235 ns** | **0.177 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| L2Norm | 384  |  34.08 ns |   0.586 ns | 0.032 ns |  0.85 |    0.00 |         - |          NA |
|        |      |           |            |          |       |         |           |             |
| **Dot**    | **768**  |  **76.18 ns** | **116.770 ns** | **6.401 ns** |  **1.00** |    **0.11** |         **-** |          **NA** |
| L2Norm | 768  |  55.84 ns |   1.055 ns | 0.058 ns |  0.74 |    0.06 |         - |          NA |
|        |      |           |            |          |       |         |           |             |
| **Dot**    | **1024** | **103.41 ns** |   **6.809 ns** | **0.373 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| L2Norm | 1024 |  70.04 ns |   0.687 ns | 0.038 ns |  0.68 |    0.00 |         - |          NA |

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon 6973P-C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Documents | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|------------- |---------- |---------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **Count**        | **200**       | **2.211 ms** | **0.0523 ms** | **0.0029 ms** |  **1.00** |    **0.00** |  **19.5313** |  **15.6250** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       | 2.239 ms | 0.0963 ms | 0.0053 ms |  1.01 |    0.00 |  19.5313 |  15.6250 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       | 3.282 ms | 1.5341 ms | 0.0841 ms |  1.48 |    0.03 |  31.2500 |  23.4375 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       | 2.204 ms | 0.4189 ms | 0.0230 ms |  1.00 |    0.01 |  19.5313 |  15.6250 |        - |    1.6 MB |        1.00 |
|              |           |          |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      | **5.324 ms** | **2.6426 ms** | **0.1448 ms** |  **1.00** |    **0.03** | **140.6250** | **132.8125** |  **70.3125** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      | 6.872 ms | 2.5139 ms | 0.1378 ms |  1.29 |    0.04 |  93.7500 |  93.7500 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 9.192 ms | 4.7522 ms | 0.2605 ms |  1.73 |    0.06 | 265.6250 | 265.6250 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      | 5.963 ms | 0.1821 ms | 0.0100 ms |  1.12 |    0.03 | 140.6250 |  70.3125 |  70.3125 |   7.85 MB |        1.00 |

## Against rapidfuzz, in this same run

Both sides on this VM in these minutes, which is what makes the ratio readable where the absolutes are not.

- `indel`
- `levenshtein`
- `metrics`
- `persistence`

### compare-indel

```text
Python: rapidfuzz 3.14.5 (py 3.12.13)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)

  length |   Python ns/pair |     C# ns/pair |  speedup (py/C#)
---------+------------------+----------------+-----------------
       8 |             77.5 |           11.8 |   6.57x C# faster
      32 |            113.2 |          177.0 |   1.56x Py faster
     128 |            336.8 |          596.6 |   1.77x Py faster
     512 |           2939.5 |         7045.9 |   2.40x Py faster

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).
```

### compare-levenshtein

```text
Python: rapidfuzz 3.14.5 (py 3.12.13)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)

  length |   Python ns/pair |     C# ns/pair |  speedup (py/C#)
---------+------------------+----------------+-----------------
       8 |             95.9 |           12.1 |   7.90x C# faster
      32 |            167.4 |          276.4 |   1.65x Py faster
     128 |           1090.0 |          869.0 |   1.25x C# faster
     512 |           8913.0 |         9647.1 |   1.08x Py faster

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.
```

### compare-metrics

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.13)
C#:     Lodestar on .NET 10.0.11

operation                             C# ms      Py ms    wall |     C# cpu     Py cpu     cpu
confusion_matrix_n1000_k2             0.005      0.501 103.61x |      0.005      0.501 103.61x
accuracy_n1000_k2                     0.001      0.261 447.23x |      0.001      0.261 447.22x
precision_recall_f1_macro_n1000_k2      0.004      0.868 202.08x |      0.004      0.868 202.08x
classification_report_n1000_k2        0.006      3.330 566.47x |      0.006      3.329 566.39x
roc_auc_binary_n1000_k2               0.011      1.010  93.72x |      0.011      1.010  93.72x
balanced_accuracy_n1000_k2            0.004      0.536 127.54x |      0.004      0.536 127.53x
matthews_n1000_k2                     0.004      0.991 236.52x |      0.004      0.991 236.51x
cohen_kappa_n1000_k2                  0.004      0.553 131.22x |      0.004      0.553 131.22x
mse_n1000_k2                          0.004      0.143  37.66x |      0.004      0.143  37.66x
mae_n1000_k2                          0.003      0.143  41.57x |      0.003      0.143  41.57x
median_ae_n1000_k2                    0.005      0.150  31.35x |      0.005      0.150  31.35x
r2_n1000_k2                           0.003      0.170  68.08x |      0.003      0.170  68.09x
confusion_matrix_n1000_k10            0.005      0.507 102.37x |      0.005      0.507 102.37x
accuracy_n1000_k10                    0.001      0.266 451.39x |      0.001      0.266 451.36x
precision_recall_f1_macro_n1000_k10      0.005      0.897 193.75x |      0.005      0.897 193.76x
classification_report_n1000_k10       0.009      3.434 393.43x |      0.009      3.433 393.44x
roc_auc_ovr_macro_n1000_k10           0.394      5.164  13.12x |      0.394      5.163  13.12x
balanced_accuracy_n1000_k10           0.005      0.536 117.94x |      0.005      0.536 117.93x
matthews_n1000_k10                    0.005      1.027 222.91x |      0.005      1.027 222.93x
cohen_kappa_n1000_k10                 0.005      0.562 116.86x |      0.005      0.562 116.86x
mse_n1000_k10                         0.003      0.145  42.15x |      0.003      0.145  42.15x
mae_n1000_k10                         0.003      0.145  42.20x |      0.003      0.145  42.20x
median_ae_n1000_k10                   0.005      0.151  30.10x |      0.005      0.151  30.10x
r2_n1000_k10                          0.003      0.169  67.34x |      0.003      0.169  67.34x
confusion_matrix_n100000_k2           0.587      8.538  14.54x |      0.587      8.538  14.53x
accuracy_n100000_k2                   0.142      2.953  20.76x |      0.142      2.953  20.76x
precision_recall_f1_macro_n100000_k2      0.490      9.588  19.57x |      0.490      9.588  19.56x
classification_report_n100000_k2      0.494     20.361  41.22x |      0.494     20.360  41.22x
roc_auc_binary_n100000_k2             2.713     20.172   7.44x |      2.713     20.172   7.44x
balanced_accuracy_n100000_k2          0.494      8.590  17.38x |      0.494      8.590  17.38x
matthews_n100000_k2                   0.497     17.245  34.67x |      0.497     17.245  34.67x
cohen_kappa_n100000_k2                0.490      8.616  17.58x |      0.490      8.616  17.58x
mse_n100000_k2                        0.341      0.396   1.16x |      0.341      0.396   1.16x
mae_n100000_k2                        0.341      0.388   1.14x |      0.341      0.388   1.14x
median_ae_n100000_k2                  0.577      1.325   2.29x |      0.601      1.325   2.20x
r2_n100000_k2                         0.242      0.605   2.49x |      0.242      0.605   2.49x
confusion_matrix_n100000_k10          0.600      8.541  14.23x |      0.600      8.538  14.22x
accuracy_n100000_k10                  0.215      2.955  13.76x |      0.215      2.955  13.76x
precision_recall_f1_macro_n100000_k10      0.597     10.009  16.76x |      0.597     10.008  16.76x
classification_report_n100000_k10      0.601     22.023  36.63x |      0.601     22.023  36.63x
roc_auc_ovr_macro_n100000_k10        30.235    159.703   5.28x |     30.243    159.697   5.28x
balanced_accuracy_n100000_k10         0.600      8.579  14.30x |      0.600      8.579  14.30x
matthews_n100000_k10                  0.601     17.733  29.49x |      0.601     17.731  29.49x
cohen_kappa_n100000_k10               0.695      8.611  12.38x |      0.695      8.610  12.38x
mse_n100000_k10                       0.342      0.397   1.16x |      0.342      0.397   1.16x
mae_n100000_k10                       0.343      0.389   1.14x |      0.343      0.389   1.13x
median_ae_n100000_k10                 0.613      1.331   2.17x |      0.659      1.331   2.02x
r2_n100000_k10                        0.245      0.599   2.44x |      0.245      0.599   2.44x
confusion_matrix_n1000000_k2          5.458     81.870  15.00x |      5.457     81.839  15.00x
accuracy_n1000000_k2                  1.518     26.985  17.77x |      1.518     26.977  17.77x
precision_recall_f1_macro_n1000000_k2      5.468     87.688  16.04x |      5.467     87.673  16.04x
classification_report_n1000000_k2      5.386    170.247  31.61x |      5.386    170.232  31.61x
roc_auc_binary_n1000000_k2           56.777    223.790   3.94x |     56.772    223.772   3.94x
balanced_accuracy_n1000000_k2         5.423     81.824  15.09x |      5.423     81.815  15.09x
matthews_n1000000_k2                  5.448    163.922  30.09x |      5.448    163.910  30.09x
cohen_kappa_n1000000_k2               5.397     81.800  15.16x |      5.396     81.793  15.16x
mse_n1000000_k2                       4.160      2.482   0.60x |      4.160      2.482   0.60x
mae_n1000000_k2                       4.086      2.469   0.60x |      4.086      2.469   0.60x
median_ae_n1000000_k2                 7.154     11.283   1.58x |      7.234     11.282   1.56x
r2_n1000000_k2                        2.976      5.271   1.77x |      2.976      5.271   1.77x
confusion_matrix_n1000000_k10         6.569     81.924  12.47x |      6.569     81.854  12.46x
accuracy_n1000000_k10                 2.285     27.032  11.83x |      2.285     27.029  11.83x
precision_recall_f1_macro_n1000000_k10      6.826     94.003  13.77x |      6.825     93.996  13.77x
classification_report_n1000000_k10      6.376    193.093  30.29x |      6.375    193.066  30.28x
balanced_accuracy_n1000000_k10        6.498     82.505  12.70x |      6.497     82.496  12.70x
matthews_n1000000_k10                 6.509    171.110  26.29x |      6.509    171.094  26.29x
cohen_kappa_n1000000_k10              6.458     82.814  12.82x |      6.458     82.803  12.82x
mse_n1000000_k10                      3.928      2.489   0.63x |      3.928      2.489   0.63x
mae_n1000000_k10                      3.880      2.498   0.64x |      3.880      2.498   0.64x
median_ae_n1000000_k10                6.805     11.411   1.68x |      6.879     11.411   1.66x
r2_n1000000_k10                       3.218      5.305   1.65x |      3.218      5.304   1.65x

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.60x
  mae_n1000000_k2                  0.60x
  mse_n1000000_k10                 0.63x
  mae_n1000000_k10                 0.64x
```

### compare-persistence

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.13)
C#:     Lodestar on .NET 10.0.11

operation                       C# ms    Py ms    wall |   C# cpu   Py cpu     cpu
vocab_txt                       2.853    7.175   2.51x |    3.187    7.175   2.25x
tokenizer_json_wordpiece        6.091   11.817   1.94x |    6.587   11.816   1.79x
tokenizer_json_unigram          8.354   27.625   3.31x |    8.889   27.622   3.11x
spiece_model                    2.638   21.856   8.29x |    2.878   21.854   7.59x
tfidf_save                      1.241    1.933   1.56x |    1.315    1.933   1.47x
tfidf_load                      2.811    3.014   1.07x |    3.113    3.014   0.97x
embedding_index_save           13.121    3.770   0.29x |   13.965    3.769   0.27x
embedding_index_load            9.626    1.208   0.13x |   11.018    1.208   0.11x

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
```
