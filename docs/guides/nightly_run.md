# Nightly benchmark run

<!-- nightly-baseline: 7946f61772df2cbb0a6c140e557731e594b801fc -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `7946f61772df2cbb0a6c140e557731e594b801fc`
- Previous run: `7946f61772df2cbb0a6c140e557731e594b801fc`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
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

| Method             | CorpusSize | Mean         | Error       | StdDev     | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|-----------:|------:|--------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **6.055 μs** |   **0.4960 μs** |  **0.0272 μs** |  **1.00** |    **0.01** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.032 μs |   0.0168 μs |  0.0009 μs |  1.00 |    0.00 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     5.970 μs |   0.2114 μs |  0.0116 μs |  0.99 |    0.00 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **8**          |   **109.842 μs** |  **29.8926 μs** |  **1.6385 μs** |  **1.00** |    **0.02** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    68.536 μs |   0.7210 μs |  0.0395 μs |  0.62 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    71.211 μs |  18.8124 μs |  1.0312 μs |  0.65 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **32**         |   **414.477 μs** |   **8.7068 μs** |  **0.4772 μs** |  **1.00** |    **0.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   240.034 μs |   8.7065 μs |  0.4772 μs |  0.58 |    0.00 |  26.8555 |  1.7090 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   232.990 μs | 112.9029 μs |  6.1886 μs |  0.56 |    0.01 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **128**        | **1,583.137 μs** | **258.4457 μs** | **14.1663 μs** |  **1.00** |    **0.01** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   957.366 μs | 167.9800 μs |  9.2076 μs |  0.60 |    0.01 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   933.548 μs |  50.8695 μs |  2.7883 μs |  0.59 |    0.00 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

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
| Unigram | 685.0 ms | 100.86 ms | 5.53 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 550.6 ms |  50.95 ms | 2.79 ms |  0.80 |  7000.0000 | 112.18 MB |        0.22 |

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

| Method                    | Length | Mean     | Error    | StdDev  | Gen0   | Gen1   | Allocated |
|-------------------------- |------- |---------:|---------:|--------:|-------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    | **103.4 μs** | **12.08 μs** | **0.66 μs** | **1.2207** |      **-** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **220.0 μs** | **52.01 μs** | **2.85 μs** | **2.4414** |      **-** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **481.2 μs** | **14.68 μs** | **0.80 μs** | **4.3945** |      **-** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **974.5 μs** | **35.83 μs** | **1.96 μs** | **8.7891** | **0.9766** | **157.03 KB** |

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

| Method     | Mean      | Error     | StdDev   | Allocated |
|----------- |----------:|----------:|---------:|----------:|
| DpGroup    |  12.75 μs |  4.647 μs | 0.255 μs |         - |
| MyersGroup | 134.22 μs | 15.608 μs | 0.856 μs |         - |

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
| Ratio          |     98.92 ns |     0.797 ns |   0.044 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 18,645.18 ns | 3,578.370 ns | 196.143 ns | 188.49 |    1.72 |      - |         - |          NA |
| TokenSortRatio |  1,048.15 ns |   450.848 ns |  24.713 ns |  10.60 |    0.22 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,331.51 ns |   683.035 ns |  37.439 ns |  33.68 |    0.33 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,503.32 ns |   367.659 ns |  20.153 ns |  45.53 |    0.18 | 0.4272 |    7200 B |          NA |

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

| Method                     | Length | Mean          | Error         | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|--------------:|-----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **26.79 ns** |      **5.281 ns** |   **0.289 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     135.35 ns |      6.651 ns |   0.365 ns |  5.05 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      27.33 ns |      1.098 ns |   0.060 ns |  1.02 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.67 ns |      1.914 ns |   0.105 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |               |            |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.42 ns** |      **4.088 ns** |   **0.224 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     152.49 ns |     20.265 ns |   1.111 ns |  5.37 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.45 ns |      1.243 ns |   0.068 ns |  1.00 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.22 ns |      0.875 ns |   0.048 ns |  0.99 |    0.01 |         - |          NA |
|                            |        |               |               |            |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **31.00 ns** |      **3.891 ns** |   **0.213 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     173.45 ns |      6.011 ns |   0.329 ns |  5.60 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      32.13 ns |      1.824 ns |   0.100 ns |  1.04 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      30.73 ns |      2.154 ns |   0.118 ns |  0.99 |    0.01 |         - |          NA |
|                            |        |               |               |            |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **86.94 ns** |      **9.304 ns** |   **0.510 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     189.93 ns |      7.121 ns |   0.390 ns |  2.18 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      86.54 ns |      2.145 ns |   0.118 ns |  1.00 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      93.33 ns |      5.656 ns |   0.310 ns |  1.07 |    0.01 |         - |          NA |
|                            |        |               |               |            |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **55.92 ns** |     **11.883 ns** |   **0.651 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     632.62 ns |    721.741 ns |  39.561 ns | 11.31 |    0.62 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      54.46 ns |      1.778 ns |   0.097 ns |  0.97 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      61.10 ns |     17.811 ns |   0.976 ns |  1.09 |    0.02 |         - |          NA |
|                            |        |               |               |            |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **59.01 ns** |      **1.482 ns** |   **0.081 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,030.82 ns |    102.558 ns |   5.622 ns | 17.47 |    0.09 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      63.20 ns |      1.224 ns |   0.067 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      60.85 ns |      2.134 ns |   0.117 ns |  1.03 |    0.00 |         - |          NA |
|                            |        |               |               |            |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,288.99 ns** |     **73.329 ns** |   **4.019 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  24,036.85 ns |  5,779.939 ns | 316.818 ns | 18.65 |    0.22 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,414.86 ns |     79.123 ns |   4.337 ns |  1.10 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,381.37 ns |      7.429 ns |   0.407 ns |  1.07 |    0.00 |         - |          NA |
|                            |        |               |               |            |       |         |           |             |
| **Distance_Utf16**             | **512**    |  **11,455.12 ns** |  **1,100.840 ns** |  **60.341 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 339,870.54 ns | 13,900.829 ns | 761.951 ns | 29.67 |    0.15 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |  11,771.02 ns |    413.154 ns |  22.646 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |  11,455.49 ns |    355.239 ns |  19.472 ns |  1.00 |    0.00 |         - |          NA |

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

| Method | Band | Mean         | Error         | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |-------------:|--------------:|-------------:|------:|--------:|----------:|------------:|
| **Dp**     | **8**    |    **135.02 ns** |     **67.005 ns** |     **3.673 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 8    |     56.11 ns |      0.754 ns |     0.041 ns |  0.42 |    0.01 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **12**   |    **218.39 ns** |     **40.015 ns** |     **2.193 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 12   |     61.40 ns |      0.839 ns |     0.046 ns |  0.28 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **14**   |    **296.35 ns** |    **169.600 ns** |     **9.296 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 14   |     64.69 ns |      4.839 ns |     0.265 ns |  0.22 |    0.01 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **16**   |    **360.68 ns** |     **49.661 ns** |     **2.722 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 16   |     69.54 ns |     14.800 ns |     0.811 ns |  0.19 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **18**   |    **439.26 ns** |     **44.538 ns** |     **2.441 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 18   |     72.83 ns |      1.452 ns |     0.080 ns |  0.17 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **20**   |    **769.21 ns** |     **34.819 ns** |     **1.909 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 20   |     77.46 ns |      0.463 ns |     0.025 ns |  0.10 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **24**   |  **1,016.34 ns** |     **96.383 ns** |     **5.283 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 24   |     86.92 ns |      2.117 ns |     0.116 ns |  0.09 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **32**   |  **1,595.87 ns** |    **206.523 ns** |    **11.320 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 32   |    103.46 ns |      2.408 ns |     0.132 ns |  0.06 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **48**   |  **3,124.78 ns** |  **1,096.630 ns** |    **60.110 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 48   |    133.60 ns |      4.769 ns |     0.261 ns |  0.04 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **64**   |  **5,554.25 ns** |    **671.180 ns** |    **36.790 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 64   |    159.30 ns |      3.584 ns |     0.196 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **96**   | **13,123.09 ns** | **27,222.790 ns** | **1,492.173 ns** |  **1.01** |    **0.15** |         **-** |          **NA** |
| Kernel | 96   |  1,104.40 ns |     29.709 ns |     1.628 ns |  0.08 |    0.01 |         - |          NA |

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
| **Distance_Utf16**             | **8**      |     **25.64 ns** |   **0.361 ns** |  **0.020 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    118.50 ns |   2.243 ns |  0.123 ns |  4.62 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.02 ns |   1.381 ns |  0.076 ns |  0.98 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **64**     |    **267.29 ns** |   **8.845 ns** |  **0.485 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    660.06 ns |   9.018 ns |  0.494 ns |  2.47 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    268.91 ns |  39.672 ns |  2.175 ns |  1.01 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **512**    | **13,844.14 ns** | **160.635 ns** |  **8.805 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 16,633.08 ns | 410.864 ns | 22.521 ns |  1.20 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,251.22 ns | 200.249 ns | 10.976 ns |  1.03 |         - |          NA |

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

| Method             | Length | Distinct | Mean           | Error         | StdDev       | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |---------------:|--------------:|-------------:|-------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |       **330.9 ns** |       **4.16 ns** |      **0.23 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,442.2 ns |     146.71 ns |      8.04 ns |   4.36 |    0.02 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **333.9 ns** |       **3.55 ns** |      **0.19 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,576.2 ns |     834.02 ns |     45.72 ns |   4.72 |    0.12 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **414.8 ns** |      **38.50 ns** |      **2.11 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,501.3 ns |     179.98 ns |      9.87 ns |   8.44 |    0.04 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **420.6 ns** |       **9.79 ns** |      **0.54 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,674.1 ns |     186.98 ns |     10.25 ns |   8.74 |    0.02 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **495.7 ns** |       **4.67 ns** |      **0.26 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,398.1 ns |     281.51 ns |     15.43 ns |  12.91 |    0.03 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **501.4 ns** |       **4.89 ns** |      **0.27 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,741.4 ns |   1,841.26 ns |    100.93 ns |  13.44 |    0.17 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **579.1 ns** |       **2.82 ns** |      **0.15 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |    10,209.6 ns |      63.19 ns |      3.46 ns |  17.63 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **615.9 ns** |     **754.31 ns** |     **41.35 ns** |   **1.00** |    **0.08** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |    10,840.5 ns |   1,738.88 ns |     95.31 ns |  17.65 |    1.00 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,296.5 ns** |     **349.50 ns** |     **19.16 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   128,741.7 ns |  48,237.74 ns |  2,644.07 ns |  56.06 |    1.08 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,353.2 ns** |     **144.39 ns** |      **7.91 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   111,747.1 ns |  17,073.87 ns |    935.88 ns |  47.49 |    0.37 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **18,416.9 ns** |     **432.52 ns** |     **23.71 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,348,294.3 ns | 301,978.52 ns | 16,552.46 ns | 127.51 |    0.79 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **445,933.0 ns** | **111,305.72 ns** |  **6,101.04 ns** |   **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,821,333.0 ns |  36,225.45 ns |  1,985.64 ns |   4.08 |    0.05 |         - |          NA |

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

| Method         | Samples | Classes | Mean           | Error           | StdDev        | Gen0   | Allocated |
|--------------- |-------- |-------- |---------------:|----------------:|--------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |     **7,653.1 ns** |     **1,317.85 ns** |      **72.24 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     7,361.7 ns |       608.87 ns |      33.37 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |       919.6 ns |        47.24 ns |       2.59 ns |      - |         - |
| F1Macro        | 1000    | 2       |     7,640.7 ns |       965.86 ns |      52.94 ns | 0.0229 |     472 B |
| Report         | 1000    | 2       |    10,417.8 ns |     2,561.84 ns |     140.42 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **7,859.8 ns** |       **908.55 ns** |      **49.80 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     7,595.3 ns |       146.17 ns |       8.01 ns | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |       918.1 ns |         7.81 ns |       0.43 ns |      - |         - |
| F1Macro        | 1000    | 10      |     8,163.3 ns |       333.85 ns |      18.30 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |    14,971.7 ns |       238.88 ns |      13.09 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **869,173.9 ns** |    **78,928.55 ns** |   **4,326.34 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   836,665.3 ns |   409,937.06 ns |  22,470.03 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   165,424.5 ns |     4,153.58 ns |     227.67 ns |      - |         - |
| F1Macro        | 100000  | 2       |   870,502.4 ns |    22,318.76 ns |   1,223.37 ns |      - |     473 B |
| Report         | 100000  | 2       |   871,989.5 ns |    52,710.19 ns |   2,889.22 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **986,476.4 ns** |     **5,700.15 ns** |     **312.44 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   937,140.0 ns |    21,640.07 ns |   1,186.17 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   273,247.0 ns |    44,436.45 ns |   2,435.71 ns |      - |         - |
| F1Macro        | 100000  | 10      |   965,554.2 ns |    49,311.61 ns |   2,702.94 ns |      - |    1665 B |
| Report         | 100000  | 10      |   984,614.0 ns |    76,479.34 ns |   4,192.09 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **8,668,171.4 ns** |   **141,239.44 ns** |   **7,741.81 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       | 8,457,542.3 ns |   362,789.54 ns |  19,885.72 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       | 1,745,944.5 ns |    62,563.58 ns |   3,429.32 ns |      - |         - |
| F1Macro        | 1000000 | 2       | 8,314,837.6 ns |   488,311.03 ns |  26,765.98 ns |      - |     484 B |
| Report         | 1000000 | 2       | 8,536,935.1 ns |   540,668.02 ns |  29,635.84 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **9,343,238.2 ns** | **2,195,702.87 ns** | **120,353.88 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 9,438,244.4 ns | 1,202,386.04 ns |  65,906.84 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      | 2,685,855.8 ns |    17,069.90 ns |     935.66 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 9,781,057.7 ns | 2,870,269.92 ns | 157,329.18 ns |      - |    1676 B |
| Report         | 1000000 | 10      | 9,889,460.7 ns |   816,289.51 ns |  44,743.58 ns |      - |   15892 B |

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

| Method | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**     | **4**    |     **73.47 ns** |     **1.069 ns** |   **0.059 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 4    |     73.67 ns |     7.418 ns |   0.407 ns |  1.00 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **6**    |    **108.97 ns** |     **3.137 ns** |   **0.172 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 6    |    108.46 ns |     8.671 ns |   0.475 ns |  1.00 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **8**    |    **158.69 ns** |    **18.662 ns** |   **1.023 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 8    |     87.57 ns |     0.542 ns |   0.030 ns |  0.55 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **10**   |    **204.22 ns** |    **43.249 ns** |   **2.371 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 10   |     95.29 ns |    21.913 ns |   1.201 ns |  0.47 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **12**   |    **250.91 ns** |    **61.649 ns** |   **3.379 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 12   |    107.44 ns |     7.482 ns |   0.410 ns |  0.43 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **16**   |    **410.69 ns** |     **8.257 ns** |   **0.453 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 16   |    121.22 ns |     4.785 ns |   0.262 ns |  0.30 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **24**   |    **887.81 ns** |    **82.390 ns** |   **4.516 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 24   |    155.01 ns |     2.804 ns |   0.154 ns |  0.17 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **32**   |  **1,507.24 ns** |   **460.874 ns** |  **25.262 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 32   |    190.91 ns |     4.366 ns |   0.239 ns |  0.13 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **48**   |  **3,470.11 ns** |    **66.748 ns** |   **3.659 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 48   |    261.92 ns |     8.048 ns |   0.441 ns |  0.08 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **64**   |  **6,311.03 ns** | **3,411.788 ns** | **187.012 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 64   |    334.03 ns |    18.166 ns |   0.996 ns |  0.05 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **96**   | **14,628.02 ns** | **2,837.071 ns** | **155.509 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 96   |  1,084.25 ns |   208.716 ns |  11.440 ns |  0.07 |    0.00 |         - |          NA |

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
| VocabTxt               |  4.209 ms | 3.9470 ms | 0.2163 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.222 ms | 1.7109 ms | 0.0938 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.607 ms | 0.2991 ms | 0.0164 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.804 ms | 4.0455 ms | 0.2217 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.871 ms | 0.7248 ms | 0.0397 ms |  33.2031 |  27.3438 |  27.3438 |   2.09 MB |
| TfidfLoad              |  4.431 ms | 1.3901 ms | 0.0762 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  6.710 ms | 0.4784 ms | 0.0262 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  7.293 ms | 0.9727 ms | 0.0533 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |

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
| **Count**                | **200**       |  **7.053 ms** | **0.6577 ms** | **0.0360 ms** |  **1.00** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  5.969 ms | 0.5619 ms | 0.0308 ms |  0.85 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.004 ms | 0.3267 ms | 0.0179 ms |  0.99 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.135 ms | 0.2227 ms | 0.0122 ms |  0.87 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |           |          |           |             |
| **Count**                | **1000**      | **28.809 ms** | **2.0211 ms** | **0.1108 ms** |  **1.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 22.645 ms | 2.2743 ms | 0.1247 ms |  0.79 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 27.549 ms | 2.1014 ms | 0.1152 ms |  0.96 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 23.492 ms | 1.2820 ms | 0.0703 ms |  0.82 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

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

| Method | Dim  | Mean      | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|------- |----- |----------:|---------:|---------:|------:|----------:|------------:|
| **Dot**    | **384**  |  **47.82 ns** | **4.771 ns** | **0.262 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.21 ns | 0.518 ns | 0.028 ns |  1.01 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  |  **94.68 ns** | **0.717 ns** | **0.039 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  92.70 ns | 0.656 ns | 0.036 ns |  0.98 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **123.98 ns** | **1.124 ns** | **0.062 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 122.58 ns | 0.802 ns | 0.044 ns |  0.99 |         - |          NA |

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
| **Count**        | **200**       |  **2.801 ms** | **1.2783 ms** | **0.0701 ms** |  **1.00** |    **0.03** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  2.876 ms | 2.1327 ms | 0.1169 ms |  1.03 |    0.04 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.652 ms | 1.4623 ms | 0.0802 ms |  1.30 |    0.04 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.768 ms | 0.2241 ms | 0.0123 ms |  0.99 |    0.02 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.729 ms** | **0.6616 ms** | **0.0363 ms** |  **1.00** |    **0.01** | **492.1875** | **351.5625** |  **70.3125** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  6.935 ms | 0.6365 ms | 0.0349 ms |  1.03 |    0.01 | 492.1875 | 304.6875 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 10.906 ms | 0.4142 ms | 0.0227 ms |  1.62 |    0.01 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  6.644 ms | 0.6207 ms | 0.0340 ms |  0.99 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

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
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 117.2 | 27.1 | 4.32x C# faster |
| 32 | 181.3 | 93.6 | 1.94x C# faster |
| 128 | 483.1 | 966.7 | 2.00x Py faster |
| 512 | 4465.7 | 10777.6 | 2.41x Py faster |

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
| 8 | 152.3 | 19.3 | 7.90x C# faster |
| 32 | 281.8 | 167.2 | 1.69x C# faster |
| 128 | 1686.9 | 1347.9 | 1.25x C# faster |
| 512 | 14268.9 | 14781.5 | 1.04x Py faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.13)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 1.041 | 113.08x | 0.009 | 1.041 | 113.08x |
| accuracy_n1000_k2 | 0.001 | 0.516 | 552.15x | 0.001 | 0.516 | 552.13x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.737 | 224.45x | 0.008 | 1.737 | 224.45x |
| classification_report_n1000_k2 | 0.010 | 7.133 | 695.36x | 0.010 | 7.132 | 695.39x |
| roc_auc_binary_n1000_k2 | 0.015 | 1.915 | 124.25x | 0.015 | 1.915 | 124.22x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.119 | 144.73x | 0.008 | 1.119 | 144.73x |
| matthews_n1000_k2 | 0.008 | 1.982 | 259.14x | 0.008 | 1.982 | 259.14x |
| cohen_kappa_n1000_k2 | 0.008 | 1.100 | 144.42x | 0.008 | 1.100 | 144.43x |
| mse_n1000_k2 | 0.002 | 0.304 | 125.05x | 0.002 | 0.304 | 125.05x |
| mae_n1000_k2 | 0.002 | 0.303 | 124.51x | 0.002 | 0.303 | 124.51x |
| median_ae_n1000_k2 | 0.006 | 0.313 | 55.83x | 0.006 | 0.313 | 55.82x |
| r2_n1000_k2 | 0.002 | 0.362 | 146.37x | 0.002 | 0.362 | 146.37x |
| confusion_matrix_n1000_k10 | 0.009 | 0.985 | 104.62x | 0.009 | 0.985 | 104.62x |
| accuracy_n1000_k10 | 0.001 | 0.516 | 552.64x | 0.001 | 0.516 | 552.67x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.761 | 213.09x | 0.008 | 1.761 | 213.10x |
| classification_report_n1000_k10 | 0.015 | 6.828 | 462.83x | 0.015 | 6.828 | 462.81x |
| roc_auc_ovr_macro_n1000_k10 | 0.543 | 9.705 | 17.88x | 0.543 | 9.705 | 17.88x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.040 | 125.05x | 0.008 | 1.040 | 125.05x |
| matthews_n1000_k10 | 0.008 | 1.994 | 240.81x | 0.008 | 1.994 | 240.81x |
| cohen_kappa_n1000_k10 | 0.009 | 1.090 | 127.05x | 0.009 | 1.090 | 127.06x |
| mse_n1000_k10 | 0.002 | 0.303 | 124.94x | 0.002 | 0.303 | 124.94x |
| mae_n1000_k10 | 0.002 | 0.302 | 123.88x | 0.002 | 0.302 | 123.89x |
| median_ae_n1000_k10 | 0.006 | 0.318 | 56.57x | 0.006 | 0.318 | 56.56x |
| r2_n1000_k10 | 0.002 | 0.365 | 147.50x | 0.002 | 0.365 | 147.50x |
| confusion_matrix_n100000_k2 | 0.985 | 10.660 | 10.82x | 0.985 | 10.660 | 10.82x |
| accuracy_n100000_k2 | 0.159 | 3.738 | 23.51x | 0.159 | 3.737 | 23.51x |
| precision_recall_f1_macro_n100000_k2 | 0.862 | 12.227 | 14.18x | 0.862 | 12.226 | 14.18x |
| classification_report_n100000_k2 | 0.860 | 26.868 | 31.24x | 0.860 | 26.866 | 31.23x |
| roc_auc_binary_n100000_k2 | 3.538 | 26.298 | 7.43x | 3.538 | 26.297 | 7.43x |
| balanced_accuracy_n100000_k2 | 0.863 | 10.731 | 12.43x | 0.863 | 10.731 | 12.43x |
| matthews_n100000_k2 | 0.863 | 21.433 | 24.82x | 0.863 | 21.431 | 24.82x |
| cohen_kappa_n100000_k2 | 0.855 | 10.774 | 12.59x | 0.855 | 10.774 | 12.59x |
| mse_n100000_k2 | 0.479 | 0.444 | 0.93x | 0.479 | 0.443 | 0.93x |
| mae_n100000_k2 | 0.473 | 0.435 | 0.92x | 0.473 | 0.435 | 0.92x |
| median_ae_n100000_k2 | 0.672 | 1.785 | 2.65x | 0.698 | 1.785 | 2.56x |
| r2_n100000_k2 | 0.235 | 0.662 | 2.81x | 0.235 | 0.662 | 2.81x |
| confusion_matrix_n100000_k10 | 0.974 | 10.675 | 10.96x | 0.974 | 10.672 | 10.96x |
| accuracy_n100000_k10 | 0.257 | 3.750 | 14.56x | 0.257 | 3.749 | 14.56x |
| precision_recall_f1_macro_n100000_k10 | 0.985 | 12.879 | 13.08x | 0.985 | 12.878 | 13.08x |
| classification_report_n100000_k10 | 0.978 | 29.403 | 30.06x | 0.978 | 29.401 | 30.06x |
| roc_auc_ovr_macro_n100000_k10 | 36.595 | 210.625 | 5.76x | 36.591 | 210.598 | 5.76x |
| balanced_accuracy_n100000_k10 | 0.980 | 10.767 | 10.99x | 0.980 | 10.766 | 10.99x |
| matthews_n100000_k10 | 0.981 | 22.138 | 22.57x | 0.981 | 22.135 | 22.57x |
| cohen_kappa_n100000_k10 | 0.990 | 10.807 | 10.91x | 0.990 | 10.806 | 10.91x |
| mse_n100000_k10 | 0.476 | 0.445 | 0.94x | 0.476 | 0.445 | 0.94x |
| mae_n100000_k10 | 0.479 | 0.436 | 0.91x | 0.479 | 0.436 | 0.91x |
| median_ae_n100000_k10 | 0.685 | 1.786 | 2.61x | 0.729 | 1.786 | 2.45x |
| r2_n100000_k10 | 0.235 | 0.660 | 2.81x | 0.235 | 0.660 | 2.81x |
| confusion_matrix_n1000000_k2 | 8.607 | 98.325 | 11.42x | 8.606 | 98.295 | 11.42x |
| accuracy_n1000000_k2 | 1.764 | 32.718 | 18.55x | 1.764 | 32.718 | 18.55x |
| precision_recall_f1_macro_n1000000_k2 | 8.607 | 107.050 | 12.44x | 8.608 | 107.046 | 12.44x |
| classification_report_n1000000_k2 | 8.571 | 207.834 | 24.25x | 8.571 | 207.833 | 24.25x |
| roc_auc_binary_n1000000_k2 | 41.711 | 285.235 | 6.84x | 41.713 | 285.212 | 6.84x |
| balanced_accuracy_n1000000_k2 | 8.671 | 98.533 | 11.36x | 8.670 | 98.531 | 11.36x |
| matthews_n1000000_k2 | 8.666 | 198.719 | 22.93x | 8.665 | 198.707 | 22.93x |
| cohen_kappa_n1000000_k2 | 8.602 | 98.553 | 11.46x | 8.602 | 98.547 | 11.46x |
| mse_n1000000_k2 | 2.391 | 1.937 | 0.81x | 2.391 | 1.937 | 0.81x |
| mae_n1000000_k2 | 2.367 | 1.918 | 0.81x | 2.367 | 1.918 | 0.81x |
| median_ae_n1000000_k2 | 6.255 | 13.918 | 2.23x | 6.331 | 13.917 | 2.20x |
| r2_n1000000_k2 | 2.344 | 3.223 | 1.38x | 2.344 | 3.223 | 1.38x |
| confusion_matrix_n1000000_k10 | 9.780 | 98.386 | 10.06x | 9.779 | 98.384 | 10.06x |
| accuracy_n1000000_k10 | 2.721 | 32.707 | 12.02x | 2.721 | 32.705 | 12.02x |
| precision_recall_f1_macro_n1000000_k10 | 9.845 | 113.111 | 11.49x | 9.844 | 113.103 | 11.49x |
| classification_report_n1000000_k10 | 9.802 | 232.322 | 23.70x | 9.801 | 232.305 | 23.70x |
| balanced_accuracy_n1000000_k10 | 9.861 | 98.614 | 10.00x | 9.861 | 98.605 | 10.00x |
| matthews_n1000000_k10 | 9.834 | 205.474 | 20.89x | 9.833 | 205.450 | 20.89x |
| cohen_kappa_n1000000_k10 | 9.761 | 98.626 | 10.10x | 9.760 | 98.622 | 10.10x |
| mse_n1000000_k10 | 2.395 | 1.990 | 0.83x | 2.395 | 1.990 | 0.83x |
| mae_n1000000_k10 | 2.371 | 1.966 | 0.83x | 2.371 | 1.966 | 0.83x |
| median_ae_n1000000_k10 | 5.994 | 13.944 | 2.33x | 6.038 | 13.943 | 2.31x |
| r2_n1000000_k10 | 2.752 | 3.418 | 1.24x | 2.752 | 3.418 | 1.24x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n100000_k2                   0.93x
  mae_n100000_k2                   0.92x
  mse_n100000_k10                  0.94x
  mae_n100000_k10                  0.91x
  mse_n1000000_k2                  0.81x
  mae_n1000000_k2                  0.81x
  mse_n1000000_k10                 0.83x
  mae_n1000000_k10                 0.83x

<!-- markdownlint-enable MD060 -->

### compare-persistence

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.13)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.584 | 9.590 | 2.09x | 4.937 | 9.590 | 1.94x |
| tokenizer_json_wordpiece | 10.867 | 16.095 | 1.48x | 11.335 | 16.095 | 1.42x |
| tokenizer_json_unigram | 12.317 | 34.618 | 2.81x | 13.221 | 34.615 | 2.62x |
| spiece_model | 4.660 | 27.911 | 5.99x | 4.905 | 27.853 | 5.68x |
| tfidf_save | 2.052 | 2.447 | 1.19x | 2.129 | 2.446 | 1.15x |
| tfidf_load | 4.274 | 4.032 | 0.94x | 4.456 | 4.032 | 0.90x |
| embedding_index_save | 6.254 | 4.478 | 0.72x | 6.812 | 4.478 | 0.66x |
| embedding_index_load | 6.805 | 1.190 | 0.17x | 7.629 | 1.190 | 0.16x |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.

<!-- markdownlint-enable MD060 -->
