# Latest known benchmark result, per method

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml` from the
> wiki's own history, alongside [nightly_run](nightly_run).

**Not a comparison across methods.** Each section below is the last night that method was
actually re-run -- whichever night touched the source near it, not necessarily last night,
and not the same night as its neighbours here. Every run measures on a GitHub hosted
runner whose hardware differs night to night, so a number here says "this is the last
known reading", never "faster than the section above it".

## Per method

### Lodestar.Text.Benchmarks.BatchEmbeddingBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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

| Method             | CorpusSize | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|----------:|------:|--------:|--------:|-------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **5.006 μs** |   **0.7123 μs** | **0.0390 μs** |  **1.00** |    **0.01** |  **0.0305** |      **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     5.254 μs |   2.0236 μs | 0.1109 μs |  1.05 |    0.02 |  0.0381 |      - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     5.259 μs |   1.7274 μs | 0.0947 μs |  1.05 |    0.02 |  0.0381 |      - |    3.16 KB |        1.14 |
|                    |            |              |             |           |       |         |         |        |            |             |
| **UnitLoop**           | **8**          |    **92.170 μs** |   **5.8730 μs** | **0.3219 μs** |  **1.00** |    **0.00** |  **1.5869** |      **-** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    59.960 μs |  12.7367 μs | 0.6981 μs |  0.65 |    0.01 |  1.5259 | 0.0610 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    63.913 μs |  22.6881 μs | 1.2436 μs |  0.69 |    0.01 |  1.4648 |      - |  127.31 KB |        0.95 |
|                    |            |              |             |           |       |         |         |        |            |             |
| **UnitLoop**           | **32**         |   **343.762 μs** |  **32.3893 μs** | **1.7754 μs** |  **1.00** |    **0.01** |  **5.3711** |      **-** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   206.061 μs |  34.8066 μs | 1.9079 μs |  0.60 |    0.01 |  5.3711 | 0.2441 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   210.418 μs | 100.2452 μs | 5.4948 μs |  0.61 |    0.01 |  5.1270 | 0.2441 |   427.8 KB |        0.91 |
|                    |            |              |             |           |       |         |         |        |            |             |
| **UnitLoop**           | **128**        | **1,381.498 μs** | **150.3268 μs** | **8.2399 μs** |  **1.00** |    **0.01** | **21.4844** |      **-** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   820.234 μs |  77.7261 μs | 4.2604 μs |  0.59 |    0.00 | 21.4844 | 2.9297 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   822.567 μs |  35.0201 μs | 1.9196 μs |  0.60 |    0.00 | 20.5078 | 2.9297 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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
| Unigram | 492.4 ms | 90.82 ms | 4.98 ms |  1.00 | 6000.0000 | 519.51 MB |        1.00 |
| Bpe     | 510.7 ms | 49.31 ms | 2.70 ms |  1.04 | 1000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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

| Method                    | Length | Mean      | Error      | StdDev    | Gen0   | Allocated |
|-------------------------- |------- |----------:|-----------:|----------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |  **90.15 μs** |  **17.840 μs** |  **0.978 μs** | **0.2441** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **203.50 μs** |   **9.153 μs** |  **0.502 μs** | **0.4883** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **446.30 μs** |  **39.685 μs** |  **2.175 μs** | **0.4883** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **950.93 μs** | **429.885 μs** | **23.563 μs** | **0.9766** | **157.02 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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

| Method         | Mean         | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|--------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     94.55 ns |      3.853 ns |   0.211 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 32,075.41 ns | 15,574.377 ns | 853.684 ns | 339.25 |    7.85 |      - |         - |          NA |
| TokenSortRatio |    814.46 ns |     82.715 ns |   4.534 ns |   8.61 |    0.04 | 0.0153 |    1312 B |          NA |
| TokenSetRatio  |  2,744.47 ns |    241.228 ns |  13.223 ns |  29.03 |    0.13 | 0.0687 |    5760 B |          NA |
| WRatio         |  3,775.49 ns |    558.150 ns |  30.594 ns |  39.93 |    0.29 | 0.0839 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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
| **Distance_Utf16**             | **8**      |      **20.77 ns** |      **3.298 ns** |     **0.181 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     121.55 ns |     65.849 ns |     3.609 ns |  5.85 |    0.16 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      22.57 ns |     11.135 ns |     0.610 ns |  1.09 |    0.03 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      20.69 ns |      2.395 ns |     0.131 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **23.05 ns** |      **3.759 ns** |     **0.206 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     125.59 ns |     62.650 ns |     3.434 ns |  5.45 |    0.14 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      23.33 ns |     11.467 ns |     0.629 ns |  1.01 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      21.80 ns |      5.433 ns |     0.298 ns |  0.95 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **23.15 ns** |      **7.298 ns** |     **0.400 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     135.62 ns |     10.363 ns |     0.568 ns |  5.86 |    0.09 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      26.56 ns |      2.081 ns |     0.114 ns |  1.15 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      23.47 ns |      0.669 ns |     0.037 ns |  1.01 |    0.02 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **78.82 ns** |     **11.597 ns** |     **0.636 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     165.48 ns |     21.611 ns |     1.185 ns |  2.10 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      79.41 ns |      6.357 ns |     0.348 ns |  1.01 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      76.81 ns |     11.799 ns |     0.647 ns |  0.97 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **59.80 ns** |      **8.811 ns** |     **0.483 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     552.01 ns |     38.934 ns |     2.134 ns |  9.23 |    0.07 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      60.99 ns |      0.923 ns |     0.051 ns |  1.02 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      56.70 ns |      2.933 ns |     0.161 ns |  0.95 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **64.37 ns** |      **8.525 ns** |     **0.467 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |     646.37 ns |    221.410 ns |    12.136 ns | 10.04 |    0.17 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      82.26 ns |      0.992 ns |     0.054 ns |  1.28 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      63.05 ns |     47.166 ns |     2.585 ns |  0.98 |    0.04 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **848.34 ns** |     **14.527 ns** |     **0.796 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  14,963.68 ns |  2,973.042 ns |   162.962 ns | 17.64 |    0.17 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     907.03 ns |     34.007 ns |     1.864 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     861.17 ns |     29.623 ns |     1.624 ns |  1.02 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **8,504.54 ns** |    **851.689 ns** |    **46.684 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 292,959.75 ns | 43,811.479 ns | 2,401.455 ns | 34.45 |    0.29 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   8,501.71 ns |    385.463 ns |    21.129 ns |  1.00 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   8,348.75 ns |     40.667 ns |     2.229 ns |  0.98 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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
| **Dp**     | **8**    |   **101.25 ns** |     **7.562 ns** |   **0.414 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 8    |   102.17 ns |    17.587 ns |   0.964 ns |  1.01 |    0.01 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **12**   |   **183.44 ns** |     **5.067 ns** |   **0.278 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 12   |   181.98 ns |    22.214 ns |   1.218 ns |  0.99 |    0.01 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **14**   |   **237.71 ns** |    **19.268 ns** |   **1.056 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 14   |   222.95 ns |    40.603 ns |   2.226 ns |  0.94 |    0.01 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **16**   |   **285.24 ns** |     **3.588 ns** |   **0.197 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 16   |    77.44 ns |     2.609 ns |   0.143 ns |  0.27 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **18**   |   **348.65 ns** |     **9.379 ns** |   **0.514 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 18   |    79.40 ns |     5.810 ns |   0.318 ns |  0.23 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **20**   |   **412.91 ns** |    **33.099 ns** |   **1.814 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 20   |    81.47 ns |     8.676 ns |   0.476 ns |  0.20 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **24**   |   **555.53 ns** |    **37.252 ns** |   **2.042 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 24   |    85.13 ns |     1.564 ns |   0.086 ns |  0.15 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **32**   | **1,071.87 ns** |    **50.132 ns** |   **2.748 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    98.44 ns |     1.959 ns |   0.107 ns |  0.09 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **48**   | **2,086.86 ns** |   **219.796 ns** |  **12.048 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |   120.71 ns |    14.310 ns |   0.784 ns |  0.06 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **64**   | **3,682.66 ns** |   **573.406 ns** |  **31.430 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 64   |   145.62 ns |    43.605 ns |   2.390 ns |  0.04 |    0.00 |         - |          NA |
|        |      |             |              |            |       |         |           |             |
| **Dp**     | **96**   | **8,248.24 ns** | **4,898.021 ns** | **268.477 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 96   |   716.83 ns |    19.395 ns |   1.063 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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

| Method                     | Length | Mean         | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **17.80 ns** |   **1.959 ns** |  **0.107 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    111.79 ns |   5.279 ns |  0.289 ns |  6.28 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     20.87 ns |   7.400 ns |  0.406 ns |  1.17 |    0.02 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **252.28 ns** |  **20.112 ns** |  **1.102 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    615.21 ns | 414.812 ns | 22.737 ns |  2.44 |    0.08 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    258.68 ns |  83.935 ns |  4.601 ns |  1.03 |    0.02 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **512**    | **11,010.02 ns** | **176.543 ns** |  **9.677 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 13,006.94 ns | 230.337 ns | 12.626 ns |  1.18 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 10,979.83 ns | 128.431 ns |  7.040 ns |  1.00 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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

| Method             | Length | Distinct | Mean           | Error         | StdDev       | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |---------------:|--------------:|-------------:|-------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |       **286.4 ns** |       **6.74 ns** |      **0.37 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,219.7 ns |     180.54 ns |      9.90 ns |   4.26 |    0.03 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **295.1 ns** |       **5.63 ns** |      **0.31 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,398.2 ns |     525.96 ns |     28.83 ns |   4.74 |    0.08 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **370.9 ns** |     **103.94 ns** |      **5.70 ns** |   **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     2,708.3 ns |     380.07 ns |     20.83 ns |   7.30 |    0.11 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **363.9 ns** |      **10.88 ns** |      **0.60 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,045.1 ns |     166.93 ns |      9.15 ns |   8.37 |    0.02 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **456.2 ns** |      **77.27 ns** |      **4.24 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     5,560.5 ns |     468.93 ns |     25.70 ns |  12.19 |    0.11 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **447.8 ns** |     **143.79 ns** |      **7.88 ns** |   **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     5,389.5 ns |     671.66 ns |     36.82 ns |  12.04 |    0.20 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **541.2 ns** |     **591.79 ns** |     **32.44 ns** |   **1.00** |    **0.07** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |     7,518.6 ns |     665.72 ns |     36.49 ns |  13.92 |    0.70 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **531.6 ns** |       **0.47 ns** |      **0.03 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |     8,615.5 ns |     310.80 ns |     17.04 ns |  16.21 |    0.03 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **1,825.8 ns** |      **72.55 ns** |      **3.98 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   145,520.2 ns |  14,500.92 ns |    794.84 ns |  79.70 |    0.41 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **1,791.3 ns** |     **194.62 ns** |     **10.67 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |    90,880.6 ns |  32,872.72 ns |  1,801.86 ns |  50.74 |    0.91 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **14,439.9 ns** |   **3,304.46 ns** |    **181.13 ns** |   **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,353,023.2 ns | 144,055.54 ns |  7,896.17 ns | 162.97 |    1.84 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **451,198.3 ns** | **188,237.40 ns** | **10,317.93 ns** |   **1.00** |    **0.03** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,645,325.2 ns |   5,014.38 ns |    274.86 ns |   3.65 |    0.07 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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

| Method         | Samples | Classes | Mean           | Error           | StdDev        | Gen0   | Allocated |
|--------------- |-------- |-------- |---------------:|----------------:|--------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |     **4,738.4 ns** |       **175.45 ns** |       **9.62 ns** |      **-** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     5,043.4 ns |       298.68 ns |      16.37 ns |      - |     312 B |
| AccuracyScore  | 1000    | 2       |       561.2 ns |       107.31 ns |       5.88 ns |      - |         - |
| F1Macro        | 1000    | 2       |     4,702.4 ns |       150.96 ns |       8.27 ns |      - |     472 B |
| Report         | 1000    | 2       |     7,110.1 ns |     2,990.37 ns |     163.91 ns | 0.0763 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **5,098.6 ns** |        **46.46 ns** |       **2.55 ns** | **0.0076** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     5,130.5 ns |       860.37 ns |      47.16 ns | 0.0076 |    1248 B |
| AccuracyScore  | 1000    | 10      |       747.2 ns |       269.82 ns |      14.79 ns |      - |         - |
| F1Macro        | 1000    | 10      |     5,077.8 ns |       140.26 ns |       7.69 ns | 0.0153 |    1664 B |
| Report         | 1000    | 10      |    11,101.1 ns |       437.01 ns |      23.95 ns | 0.1831 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **581,930.6 ns** |     **5,795.43 ns** |     **317.67 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   580,128.4 ns |    60,097.46 ns |   3,294.14 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   175,305.0 ns |     4,140.83 ns |     226.97 ns |      - |         - |
| F1Macro        | 100000  | 2       |   568,237.3 ns |   112,052.60 ns |   6,141.98 ns |      - |     473 B |
| Report         | 100000  | 2       |   571,276.4 ns |   102,198.85 ns |   5,601.86 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **715,344.5 ns** |    **33,697.68 ns** |   **1,847.08 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   729,758.9 ns |    37,777.76 ns |   2,070.73 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   285,856.0 ns |   142,343.06 ns |   7,802.30 ns |      - |         - |
| F1Macro        | 100000  | 10      |   697,781.2 ns |    55,191.41 ns |   3,025.23 ns |      - |    1665 B |
| Report         | 100000  | 10      |   726,683.8 ns |    12,904.68 ns |     707.35 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **5,941,577.4 ns** |   **243,745.57 ns** |  **13,360.52 ns** |      **-** |     **318 B** |
| MatrixWeighted | 1000000 | 2       | 6,098,579.9 ns |   352,529.61 ns |  19,323.34 ns |      - |     318 B |
| AccuracyScore  | 1000000 | 2       | 1,885,889.9 ns |   531,655.62 ns |  29,141.84 ns |      - |         - |
| F1Macro        | 1000000 | 2       | 5,803,761.7 ns |   114,660.64 ns |   6,284.94 ns |      - |     478 B |
| Report         | 1000000 | 2       | 5,888,865.4 ns | 2,144,297.30 ns | 117,536.17 ns |      - |    6566 B |
| **Matrix**         | **1000000** | **10**      | **7,257,649.6 ns** |    **66,976.59 ns** |   **3,671.21 ns** |      **-** |    **1254 B** |
| MatrixWeighted | 1000000 | 10      | 7,341,758.7 ns |   174,886.46 ns |   9,586.12 ns |      - |    1254 B |
| AccuracyScore  | 1000000 | 10      | 2,735,262.1 ns |    12,454.26 ns |     682.66 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 7,225,536.5 ns |   226,897.28 ns |  12,437.01 ns |      - |    1670 B |
| Report         | 1000000 | 10      | 7,494,399.9 ns |   224,861.49 ns |  12,325.42 ns |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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

| Method                 | Mean      | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated |
|----------------------- |----------:|----------:|----------:|---------:|---------:|---------:|----------:|
| VocabTxt               |  3.071 ms | 1.3960 ms | 0.0765 ms |  46.8750 |  42.9688 |  31.2500 |   3.62 MB |
| TokenizerJsonWordPiece |  6.616 ms | 1.0661 ms | 0.0584 ms |  46.8750 |  31.2500 |  31.2500 |   5.72 MB |
| TokenizerJsonUnigram   |  8.073 ms | 0.5787 ms | 0.0317 ms |  31.2500 |  31.2500 |  31.2500 |   4.64 MB |
| SpieceModel            |  2.668 ms | 2.9569 ms | 0.1621 ms |  46.8750 |  42.9688 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.467 ms | 0.2763 ms | 0.0151 ms |  31.2500 |  31.2500 |  31.2500 |   2.09 MB |
| TfidfLoad              |  2.949 ms | 1.0809 ms | 0.0592 ms |  23.4375 |  15.6250 |  15.6250 |   2.86 MB |
| EmbeddingIndexSave     | 10.900 ms | 8.3526 ms | 0.4578 ms | 562.5000 | 562.5000 | 562.5000 |  54.29 MB |
| EmbeddingIndexLoad     |  6.746 ms | 4.7561 ms | 0.2607 ms | 476.5625 | 468.7500 | 468.7500 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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

| Method               | Documents | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|-----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **6.897 ms** |  **6.4238 ms** | **0.3521 ms** |  **1.00** |    **0.06** | **125.0000** |  **78.1250** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  5.229 ms |  0.1804 ms | 0.0099 ms |  0.76 |    0.03 |  78.1250 |  39.0625 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  6.686 ms |  0.9911 ms | 0.0543 ms |  0.97 |    0.04 | 140.6250 |  70.3125 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  5.404 ms |  0.8684 ms | 0.0476 ms |  0.79 |    0.04 |  78.1250 |  31.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |            |           |       |         |          |          |          |           |             |
| **Count**                | **1000**      | **27.450 ms** | **11.7902 ms** | **0.6463 ms** |  **1.00** |    **0.03** | **875.0000** | **843.7500** | **500.0000** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 20.608 ms |  2.6275 ms | 0.1440 ms |  0.75 |    0.02 | 500.0000 | 343.7500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 26.496 ms | 19.7678 ms | 1.0835 ms |  0.97 |    0.04 | 875.0000 | 625.0000 | 500.0000 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 21.222 ms |  2.4879 ms | 0.1364 ms |  0.77 |    0.02 | 500.0000 | 250.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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
| **Dot**    | **384**  | **35.72 ns** | **1.500 ns** | **0.082 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  | 30.78 ns | 4.509 ns | 0.247 ns |  0.86 |         - |          NA |
|        |      |          |          |          |       |           |             |
| **Dot**    | **768**  | **69.29 ns** | **4.876 ns** | **0.267 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  | 56.98 ns | 1.997 ns | 0.109 ns |  0.82 |         - |          NA |
|        |      |          |          |          |       |           |             |
| **Dot**    | **1024** | **91.33 ns** | **4.005 ns** | **0.220 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 74.13 ns | 5.775 ns | 0.317 ns |  0.81 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

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

| Method       | Documents | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|------------- |---------- |----------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **Count**        | **200**       |  **2.638 ms** | **0.1693 ms** | **0.0093 ms** |  **1.00** |    **0.00** |  **19.5313** |  **15.6250** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  2.716 ms | 1.3690 ms | 0.0750 ms |  1.03 |    0.02 |  15.6250 |   7.8125 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.456 ms | 0.7349 ms | 0.0403 ms |  1.31 |    0.01 |  31.2500 |  23.4375 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.644 ms | 0.2672 ms | 0.0146 ms |  1.00 |    0.01 |  19.5313 |  15.6250 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.224 ms** | **0.3822 ms** | **0.0209 ms** |  **1.00** |    **0.00** | **140.6250** | **132.8125** |  **70.3125** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.202 ms | 1.4505 ms | 0.0795 ms |  1.16 |    0.01 |  93.7500 |  93.7500 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 10.688 ms | 0.8828 ms | 0.0484 ms |  1.72 |    0.01 | 265.6250 | 265.6250 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  6.446 ms | 0.7029 ms | 0.0385 ms |  1.04 |    0.01 | 140.6250 |  70.3125 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

````text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)

  length |   Python ns/pair |     C# ns/pair |  speedup (py/C#)
---------+------------------+----------------+-----------------
       8 |             92.1 |           13.7 |   6.74x C# faster
      32 |            135.1 |          216.8 |   1.60x Py faster
     128 |            379.9 |          693.0 |   1.82x Py faster
     512 |           3356.6 |         8098.1 |   2.41x Py faster

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).
````

### compare-levenshtein

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

````text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)

  length |   Python ns/pair |     C# ns/pair |  speedup (py/C#)
---------+------------------+----------------+-----------------
       8 |            117.4 |           14.6 |   8.03x C# faster
      32 |            203.0 |          340.5 |   1.68x Py faster
     128 |           1315.8 |         1093.9 |   1.20x C# faster
     512 |          10420.6 |        11711.7 |   1.12x Py faster

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.
````

### compare-metrics

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

````text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11

operation                             C# ms      Py ms    wall |     C# cpu     Py cpu     cpu
confusion_matrix_n1000_k2             0.006      0.627 109.21x |      0.006      0.627 109.19x
accuracy_n1000_k2                     0.001      0.315 519.22x |      0.001      0.315 519.25x
precision_recall_f1_macro_n1000_k2      0.005      1.107 232.66x |      0.005      1.107 232.68x
classification_report_n1000_k2        0.007      4.120 591.16x |      0.007      4.119 591.18x
roc_auc_binary_n1000_k2               0.013      1.281 100.88x |      0.013      1.281 100.89x
balanced_accuracy_n1000_k2            0.005      0.669 141.72x |      0.005      0.669 141.72x
matthews_n1000_k2                     0.005      1.238 266.18x |      0.005      1.238 266.19x
cohen_kappa_n1000_k2                  0.005      0.704 149.10x |      0.005      0.704 149.09x
mse_n1000_k2                          0.004      0.173  43.15x |      0.004      0.173  43.15x
mae_n1000_k2                          0.004      0.173  43.27x |      0.004      0.173  43.27x
median_ae_n1000_k2                    0.006      0.181  30.58x |      0.006      0.181  30.58x
r2_n1000_k2                           0.003      0.206  69.98x |      0.003      0.206  69.99x
confusion_matrix_n1000_k10            0.006      0.623 105.72x |      0.006      0.623 105.71x
accuracy_n1000_k10                    0.001      0.319 469.35x |      0.001      0.319 469.35x
precision_recall_f1_macro_n1000_k10      0.005      1.125 217.29x |      0.005      1.125 217.30x
classification_report_n1000_k10       0.011      4.318 404.55x |      0.011      4.318 404.51x
roc_auc_ovr_macro_n1000_k10           0.474      6.609  13.95x |      0.474      6.608  13.95x
balanced_accuracy_n1000_k10           0.005      0.674 133.72x |      0.005      0.674 133.71x
matthews_n1000_k10                    0.005      1.286 256.24x |      0.005      1.286 256.22x
cohen_kappa_n1000_k10                 0.005      0.710 135.12x |      0.005      0.710 135.12x
mse_n1000_k10                         0.004      0.174  43.46x |      0.004      0.173  43.46x
mae_n1000_k10                         0.004      0.173  43.19x |      0.004      0.173  43.19x
median_ae_n1000_k10                   0.006      0.184  31.71x |      0.006      0.184  31.70x
r2_n1000_k10                          0.003      0.207  69.84x |      0.003      0.206  69.83x
confusion_matrix_n100000_k2           0.676     10.026  14.82x |      0.676     10.026  14.82x
accuracy_n100000_k2                   0.161      3.457  21.44x |      0.161      3.456  21.44x
precision_recall_f1_macro_n100000_k2      0.574     11.299  19.69x |      0.574     11.299  19.69x
classification_report_n100000_k2      0.579     23.881  41.22x |      0.579     23.881  41.23x
roc_auc_binary_n100000_k2             3.365     23.130   6.87x |      3.365     23.130   6.87x
balanced_accuracy_n100000_k2          0.562     10.068  17.92x |      0.562     10.068  17.92x
matthews_n100000_k2                   0.566     20.274  35.80x |      0.566     20.272  35.80x
cohen_kappa_n100000_k2                0.567     10.105  17.82x |      0.567     10.104  17.82x
mse_n100000_k2                        0.398      0.405   1.02x |      0.398      0.405   1.02x
mae_n100000_k2                        0.397      0.396   1.00x |      0.397      0.396   1.00x
median_ae_n100000_k2                  0.672      1.506   2.24x |      0.686      1.506   2.19x
r2_n100000_k2                         0.284      0.617   2.17x |      0.284      0.617   2.17x
confusion_matrix_n100000_k10          0.686      9.978  14.54x |      0.686      9.977  14.54x
accuracy_n100000_k10                  0.246      3.458  14.03x |      0.246      3.457  14.03x
precision_recall_f1_macro_n100000_k10      0.694     11.794  16.99x |      0.694     11.793  16.99x
classification_report_n100000_k10      0.705     25.949  36.83x |      0.705     25.948  36.83x
roc_auc_ovr_macro_n100000_k10        37.824    182.194   4.82x |     37.823    182.175   4.82x
balanced_accuracy_n100000_k10         0.700     10.066  14.39x |      0.699     10.065  14.39x
matthews_n100000_k10                  0.696     20.771  29.83x |      0.696     20.770  29.82x
cohen_kappa_n100000_k10               0.825     10.094  12.23x |      0.825     10.093  12.23x
mse_n100000_k10                       0.398      0.403   1.01x |      0.398      0.403   1.01x
mae_n100000_k10                       0.397      0.394   0.99x |      0.397      0.394   0.99x
median_ae_n100000_k10                 0.689      1.499   2.17x |      0.726      1.499   2.06x
r2_n100000_k10                        0.282      0.603   2.14x |      0.282      0.603   2.14x
confusion_matrix_n1000000_k2          5.798     96.102  16.57x |      5.799     96.100  16.57x
accuracy_n1000000_k2                  1.755     31.617  18.02x |      1.754     31.616  18.02x
precision_recall_f1_macro_n1000000_k2      5.885    103.346  17.56x |      5.885    103.340  17.56x
classification_report_n1000000_k2      5.949    198.853  33.42x |      5.949    198.846  33.43x
roc_auc_binary_n1000000_k2           59.969    258.722   4.31x |     59.965    258.702   4.31x
balanced_accuracy_n1000000_k2         5.862     95.914  16.36x |      5.861     95.910  16.36x
matthews_n1000000_k2                  5.850    192.082  32.84x |      5.849    192.072  32.84x
cohen_kappa_n1000000_k2               5.844     95.486  16.34x |      5.844     95.480  16.34x
mse_n1000000_k2                       4.008      2.368   0.59x |      4.008      2.368   0.59x
mae_n1000000_k2                       3.993      2.359   0.59x |      3.993      2.359   0.59x
median_ae_n1000000_k2                 6.712     12.425   1.85x |      6.760     12.424   1.84x
r2_n1000000_k2                        2.884      4.969   1.72x |      2.884      4.969   1.72x
confusion_matrix_n1000000_k10         7.056     95.697  13.56x |      7.056     95.673  13.56x
accuracy_n1000000_k10                 2.608     31.732  12.17x |      2.608     31.731  12.17x
precision_recall_f1_macro_n1000000_k10      7.122    108.007  15.16x |      7.122    108.003  15.17x
classification_report_n1000000_k10      7.162    217.418  30.36x |      7.161    217.411  30.36x
balanced_accuracy_n1000000_k10        7.137     96.389  13.51x |      7.133     96.383  13.51x
matthews_n1000000_k10                 7.125    200.960  28.21x |      7.125    200.951  28.20x
cohen_kappa_n1000000_k10              7.112     96.659  13.59x |      7.111     96.652  13.59x
mse_n1000000_k10                      4.001      2.382   0.60x |      4.001      2.382   0.60x
mae_n1000000_k10                      3.991      2.366   0.59x |      3.991      2.366   0.59x
median_ae_n1000000_k10                6.179     12.469   2.02x |      6.205     12.469   2.01x
r2_n1000000_k10                       2.973      4.989   1.68x |      2.973      4.989   1.68x

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mae_n100000_k2                   1.00x
  mae_n100000_k10                  0.99x
  mse_n1000000_k2                  0.59x
  mae_n1000000_k2                  0.59x
  mse_n1000000_k10                 0.60x
  mae_n1000000_k10                 0.59x
````

### compare-persistence

_As of 2026-08-19, measured at commit `824aea54cc6366a40949a59f4cd68a72353f7dae`._

````text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11

operation                       C# ms    Py ms    wall |   C# cpu   Py cpu     cpu
vocab_txt                       2.644    8.191   3.10x |    2.844    8.191   2.88x
tokenizer_json_wordpiece        6.079   14.000   2.30x |    6.409   13.999   2.18x
tokenizer_json_unigram          8.926   32.163   3.60x |    9.372   32.160   3.43x
spiece_model                    2.435   25.647  10.53x |    2.628   25.599   9.74x
tfidf_save                      1.441    2.134   1.48x |    1.496    2.134   1.43x
tfidf_load                      3.012    3.419   1.14x |    3.188    3.419   1.07x
embedding_index_save           11.143    4.164   0.37x |   11.696    4.164   0.36x
embedding_index_load            8.137    1.215   0.15x |    9.556    1.215   0.13x

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
````
