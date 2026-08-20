# Nightly benchmark run

<!-- nightly-baseline: 2e945c5bf694f6837523cee6e75afcdc78f87d38 -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `2e945c5bf694f6837523cee6e75afcdc78f87d38`
- Previous run: `2e945c5bf694f6837523cee6e75afcdc78f87d38`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BpeBenchmarks`
- `BpeScalingBenchmarks`
- `BucketRouteDiagnostics`
- `FuzzBenchmarks`
- `IndelBenchmarks`
- `LcsGateBenchmarks`
- `LevenshteinBenchmarks`
- `LevenshteinCodePointBenchmarks`
- `MyersGateBenchmarks`
- `PersistenceBenchmarks`
- `VectorMathBenchmarks`

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method  | Mean     | Error    | StdDev  | Ratio | Gen0      | Allocated | Alloc Ratio |
|-------- |---------:|---------:|--------:|------:|----------:|----------:|------------:|
| Unigram | 495.0 ms | 37.27 ms | 2.04 ms |  1.00 | 6000.0000 | 519.51 MB |        1.00 |
| Bpe     | 502.3 ms | 63.10 ms | 3.46 ms |  1.01 | 1000.0000 | 112.17 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                    | Length | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------------------- |------- |----------:|---------:|---------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |  **91.04 μs** | **22.64 μs** | **1.241 μs** | **0.2441** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **213.57 μs** | **21.53 μs** | **1.180 μs** | **0.4883** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **449.09 μs** | **47.26 μs** | **2.590 μs** | **0.4883** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **939.03 μs** | **23.04 μs** | **1.263 μs** | **0.9766** | **157.02 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Mean       | Error     | StdDev    | Allocated |
|----------- |-----------:|----------:|----------:|----------:|
| DpGroup    |   9.840 μs | 0.3231 μs | 0.0177 μs |         - |
| MyersGroup | 118.828 μs | 3.0032 μs | 0.1646 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method         | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|-------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     72.21 ns |     2.590 ns |   0.142 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 14,677.41 ns | 9,023.862 ns | 494.628 ns | 203.25 |    5.94 |      - |         - |          NA |
| TokenSortRatio |    840.55 ns |    55.175 ns |   3.024 ns |  11.64 |    0.04 | 0.0153 |    1312 B |          NA |
| TokenSetRatio  |  2,735.67 ns |    92.388 ns |   5.064 ns |  37.88 |    0.09 | 0.0687 |    5760 B |          NA |
| WRatio         |  3,737.51 ns |   332.207 ns |  18.209 ns |  51.76 |    0.24 | 0.0839 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                     | Length | Mean          | Error         | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|--------------:|-------------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **19.14 ns** |      **3.570 ns** |     **0.196 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     118.13 ns |      8.108 ns |     0.444 ns |  6.17 |    0.06 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      21.96 ns |      9.638 ns |     0.528 ns |  1.15 |    0.03 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      20.46 ns |      9.223 ns |     0.506 ns |  1.07 |    0.02 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **23.39 ns** |     **13.051 ns** |     **0.715 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     127.88 ns |      4.062 ns |     0.223 ns |  5.47 |    0.14 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      23.06 ns |      4.053 ns |     0.222 ns |  0.99 |    0.03 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      21.99 ns |      1.762 ns |     0.097 ns |  0.94 |    0.03 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **24.52 ns** |      **5.061 ns** |     **0.277 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     135.03 ns |     20.246 ns |     1.110 ns |  5.51 |    0.07 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      26.24 ns |      7.926 ns |     0.434 ns |  1.07 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      24.46 ns |      6.273 ns |     0.344 ns |  1.00 |    0.02 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **75.72 ns** |      **5.172 ns** |     **0.284 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     169.22 ns |     71.559 ns |     3.922 ns |  2.23 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      79.93 ns |      3.641 ns |     0.200 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      78.10 ns |      0.657 ns |     0.036 ns |  1.03 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **39.89 ns** |      **7.586 ns** |     **0.416 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     507.63 ns |    314.677 ns |    17.248 ns | 12.73 |    0.39 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      44.57 ns |     18.777 ns |     1.029 ns |  1.12 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      39.72 ns |      1.468 ns |     0.080 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **47.51 ns** |     **18.144 ns** |     **0.995 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |     695.71 ns |     31.289 ns |     1.715 ns | 14.65 |    0.26 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      51.04 ns |      4.167 ns |     0.228 ns |  1.07 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      45.69 ns |      1.198 ns |     0.066 ns |  0.96 |    0.02 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **763.03 ns** |     **82.126 ns** |     **4.502 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  14,896.46 ns |  1,540.407 ns |    84.435 ns | 19.52 |    0.14 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     873.14 ns |    265.706 ns |    14.564 ns |  1.14 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     760.64 ns |     29.299 ns |     1.606 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **5,703.80 ns** |    **647.476 ns** |    **35.490 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 305,935.26 ns | 28,476.884 ns | 1,560.914 ns | 53.64 |    0.37 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   5,804.13 ns |  1,183.560 ns |    64.875 ns |  1.02 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   5,661.42 ns |    119.529 ns |     6.552 ns |  0.99 |    0.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | Band | Mean        | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**     | **8**    |   **101.13 ns** |     **5.549 ns** |   **0.304 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |    44.18 ns |     2.425 ns |   0.133 ns |  0.44 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **12**   |   **185.83 ns** |    **75.998 ns** |   **4.166 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 12   |    52.36 ns |     1.858 ns |   0.102 ns |  0.28 |    0.01 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **14**   |   **235.54 ns** |     **5.016 ns** |   **0.275 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 14   |    56.96 ns |     5.937 ns |   0.325 ns |  0.24 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **16**   |   **287.43 ns** |    **33.886 ns** |   **1.857 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 16   |    59.55 ns |     0.477 ns |   0.026 ns |  0.21 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **18**   |   **347.97 ns** |    **26.440 ns** |   **1.449 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 18   |    62.01 ns |     1.338 ns |   0.073 ns |  0.18 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **20**   |   **410.73 ns** |     **8.773 ns** |   **0.481 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 20   |    63.35 ns |    14.645 ns |   0.803 ns |  0.15 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **24**   |   **595.21 ns** |   **316.122 ns** |  **17.328 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 24   |    67.64 ns |     0.805 ns |   0.044 ns |  0.11 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **32**   |   **949.15 ns** |   **150.265 ns** |   **8.237 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 32   |    82.05 ns |     2.862 ns |   0.157 ns |  0.09 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **48**   | **2,106.00 ns** |   **466.554 ns** |  **25.573 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |    98.14 ns |     2.479 ns |   0.136 ns |  0.05 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **64**   | **3,654.21 ns** |   **362.898 ns** |  **19.892 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 64   |   119.28 ns |    10.747 ns |   0.589 ns |  0.03 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **96**   | **8,435.62 ns** | **5,636.889 ns** | **308.977 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel | 96   |   659.39 ns |    90.117 ns |   4.940 ns |  0.08 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                     | Length | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **17.36 ns** |     **2.245 ns** |   **0.123 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    112.57 ns |    14.238 ns |   0.780 ns |  6.49 |    0.06 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     20.36 ns |     2.754 ns |   0.151 ns |  1.17 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **231.08 ns** |     **4.272 ns** |   **0.234 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    568.09 ns |    60.921 ns |   3.339 ns |  2.46 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    233.77 ns |    56.106 ns |   3.075 ns |  1.01 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    | **10,945.88 ns** |   **511.505 ns** |  **28.037 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 13,144.13 ns | 4,977.751 ns | 272.847 ns |  1.20 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 10,988.16 ns |   192.148 ns |  10.532 ns |  1.00 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | Length | Distinct | Mean           | Error           | StdDev       | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |---------------:|----------------:|-------------:|-------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |       **274.4 ns** |         **3.80 ns** |      **0.21 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |       179.6 ns |         3.07 ns |      0.17 ns |   0.65 |    0.00 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **276.6 ns** |        **29.06 ns** |      **1.59 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |       180.0 ns |        64.45 ns |      3.53 ns |   0.65 |    0.01 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **358.6 ns** |        **25.13 ns** |      **1.38 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |       251.6 ns |        43.34 ns |      2.38 ns |   0.70 |    0.01 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **361.3 ns** |       **203.16 ns** |     **11.14 ns** |   **1.00** |    **0.04** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |       255.1 ns |         1.30 ns |      0.07 ns |   0.71 |    0.02 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **437.5 ns** |        **58.70 ns** |      **3.22 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |       336.0 ns |        16.97 ns |      0.93 ns |   0.77 |    0.01 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **430.2 ns** |       **181.05 ns** |      **9.92 ns** |   **1.00** |    **0.03** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |       331.7 ns |        70.12 ns |      3.84 ns |   0.77 |    0.02 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **512.3 ns** |        **89.69 ns** |      **4.92 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |     7,519.8 ns |       842.23 ns |     46.17 ns |  14.68 |    0.14 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **511.0 ns** |        **56.90 ns** |      **3.12 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |     8,208.5 ns |     5,443.44 ns |    298.37 ns |  16.06 |    0.51 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **1,775.7 ns** |        **20.02 ns** |      **1.10 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   131,772.3 ns |    21,199.01 ns |  1,161.99 ns |  74.21 |    0.57 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **1,766.3 ns** |        **45.32 ns** |      **2.48 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |    96,348.4 ns |    11,654.24 ns |    638.81 ns |  54.55 |    0.32 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **14,428.3 ns** |     **1,467.02 ns** |     **80.41 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,335,268.3 ns | 1,078,438.54 ns | 59,112.86 ns | 161.86 |    3.63 |         - |          NA |
|                    |        |          |                |                 |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **450,860.2 ns** |    **56,610.50 ns** |  **3,103.01 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,641,956.8 ns |    61,797.25 ns |  3,387.32 ns |   3.64 |    0.02 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | Band | Mean        | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| **Dp**     | **4**    |    **61.24 ns** |  **14.550 ns** |  **0.798 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 4    |    60.22 ns |   3.158 ns |  0.173 ns |  0.98 |    0.01 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **6**    |    **80.84 ns** |   **1.575 ns** |  **0.086 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 6    |    81.66 ns |   0.719 ns |  0.039 ns |  1.01 |    0.00 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **8**    |   **109.72 ns** |   **0.847 ns** |  **0.046 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |    64.65 ns |   0.807 ns |  0.044 ns |  0.59 |    0.00 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **10**   |   **155.72 ns** |  **64.363 ns** |  **3.528 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 10   |    72.31 ns |   3.512 ns |  0.193 ns |  0.46 |    0.01 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **12**   |   **203.83 ns** |  **44.987 ns** |  **2.466 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 12   |    81.41 ns |  19.166 ns |  1.051 ns |  0.40 |    0.01 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **16**   |   **318.11 ns** |   **6.701 ns** |  **0.367 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 16   |    96.07 ns |   3.518 ns |  0.193 ns |  0.30 |    0.00 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **24**   |   **637.07 ns** |   **5.666 ns** |  **0.311 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 24   |   127.24 ns |   2.326 ns |  0.128 ns |  0.20 |    0.00 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **32**   | **1,135.82 ns** |  **37.974 ns** |  **2.081 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |   160.94 ns |  52.017 ns |  2.851 ns |  0.14 |    0.00 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **48**   | **2,728.97 ns** | **441.761 ns** | **24.214 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |   223.58 ns |  10.214 ns |  0.560 ns |  0.08 |    0.00 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **64**   | **4,849.68 ns** | **823.078 ns** | **45.116 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 64   |   288.11 ns |  37.907 ns |  2.078 ns |  0.06 |    0.00 |         - |          NA |
|        |      |             |            |           |       |         |           |             |
| **Dp**     | **96**   | **9,790.13 ns** | **861.271 ns** | **47.209 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 96   |   773.48 ns |  23.297 ns |  1.277 ns |  0.08 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                 | Mean     | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated |
|----------------------- |---------:|----------:|----------:|---------:|---------:|---------:|----------:|
| VocabTxt               | 2.868 ms | 0.2299 ms | 0.0126 ms |  46.8750 |  42.9688 |  31.2500 |   3.62 MB |
| TokenizerJsonWordPiece | 6.684 ms | 0.3537 ms | 0.0194 ms |  46.8750 |  31.2500 |  31.2500 |   5.72 MB |
| TokenizerJsonUnigram   | 8.040 ms | 0.6453 ms | 0.0354 ms |  31.2500 |  31.2500 |  31.2500 |   4.64 MB |
| SpieceModel            | 2.606 ms | 1.6617 ms | 0.0911 ms |  46.8750 |  42.9688 |  31.2500 |   3.36 MB |
| TfidfSave              | 1.467 ms | 0.4811 ms | 0.0264 ms |  23.4375 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              | 3.365 ms | 2.3341 ms | 0.1279 ms |  35.1563 |  31.2500 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     | 6.495 ms | 2.3196 ms | 0.1271 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     | 6.455 ms | 7.3155 ms | 0.4010 ms | 437.5000 | 437.5000 | 437.5000 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | Dim  | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|------- |----- |---------:|---------:|---------:|------:|----------:|------------:|
| **Dot**    | **384**  | **35.91 ns** | **3.721 ns** | **0.204 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  | 30.43 ns | 0.804 ns | 0.044 ns |  0.85 |         - |          NA |
|        |      |          |          |          |       |           |             |
| **Dot**    | **768**  | **69.16 ns** | **1.012 ns** | **0.055 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  | 57.26 ns | 2.868 ns | 0.157 ns |  0.83 |         - |          NA |
|        |      |          |          |          |       |           |             |
| **Dot**    | **1024** | **91.55 ns** | **4.220 ns** | **0.231 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 74.27 ns | 3.377 ns | 0.185 ns |  0.81 |         - |          NA |

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
| 8 | 91.3 | 13.8 | 6.63x C# faster |
| 32 | 132.1 | 75.0 | 1.76x C# faster |
| 128 | 378.9 | 541.5 | 1.43x Py faster |
| 512 | 3360.5 | 5319.3 | 1.58x Py faster |

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
| 8 | 115.3 | 14.8 | 7.80x C# faster |
| 32 | 197.7 | 132.5 | 1.49x C# faster |
| 128 | 1317.2 | 1066.7 | 1.23x C# faster |
| 512 | 10427.5 | 11779.2 | 1.13x Py faster |

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
| vocab_txt | 2.701 | 8.257 | 3.06x | 2.908 | 8.256 | 2.84x |
| tokenizer_json_wordpiece | 5.918 | 14.435 | 2.44x | 6.201 | 14.435 | 2.33x |
| tokenizer_json_unigram | 8.850 | 32.567 | 3.68x | 9.274 | 32.565 | 3.51x |
| spiece_model | 2.304 | 25.839 | 11.22x | 2.462 | 25.839 | 10.50x |
| tfidf_save | 1.535 | 2.172 | 1.42x | 1.614 | 2.172 | 1.35x |
| tfidf_load | 3.160 | 3.459 | 1.09x | 3.312 | 3.459 | 1.04x |
| embedding_index_save | 9.395 | 4.159 | 0.44x | 10.545 | 4.159 | 0.39x |
| embedding_index_load | 8.567 | 1.204 | 0.14x | 9.777 | 1.204 | 0.12x |
| embedding_index_load_file | 9.812 | 1.007 | 0.10x | 10.530 | 1.004 | 0.10x |
| embedding_index_load_memory | 4.466 | 1.207 | 0.27x | 4.919 | 1.207 | 0.25x |
| embedding_index_view_floor | 0.000 | 0.000 | 81.57x | 0.000 | 0.000 | 81.56x |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.

<!-- markdownlint-enable MD060 -->
