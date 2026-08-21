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

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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

| Method             | CorpusSize | Mean         | Error      | StdDev    | Ratio | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|-----------:|----------:|------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **5.818 μs** |  **0.2418 μs** | **0.0133 μs** |  **1.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     5.779 μs |  1.3129 μs | 0.0720 μs |  0.99 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     5.830 μs |  0.1911 μs | 0.0105 μs |  1.00 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **8**          |   **100.446 μs** |  **2.4891 μs** | **0.1364 μs** |  **1.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    65.952 μs |  2.0690 μs | 0.1134 μs |  0.66 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    66.214 μs |  3.5555 μs | 0.1949 μs |  0.66 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **32**         |   **365.199 μs** | **10.8440 μs** | **0.5944 μs** |  **1.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   231.232 μs | 18.2241 μs | 0.9989 μs |  0.63 |  26.8555 |  1.7090 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   223.574 μs | 24.9645 μs | 1.3684 μs |  0.61 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **128**        | **1,520.299 μs** | **15.3734 μs** | **0.8427 μs** |  **1.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   928.196 μs | 38.3418 μs | 2.1016 μs |  0.61 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   878.948 μs | 39.2791 μs | 2.1530 μs |  0.58 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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
| Unigram | 590.1 ms |  9.15 ms | 0.50 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 561.3 ms | 29.98 ms | 1.64 ms |  0.95 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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
| **BpeOnOnePathologicalToken** | **512**    |   **103.0 μs** |  **5.35 μs** | **0.29 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **212.2 μs** | **19.39 μs** | **1.06 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **488.9 μs** | **32.44 μs** | **1.78 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,028.7 μs** | **56.57 μs** | **3.10 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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
| DpGroup    |  13.64 μs | 0.159 μs | 0.009 μs |         - |
| MyersGroup | 115.64 μs | 9.968 μs | 0.546 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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

| Method         | Mean        | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |    107.7 ns |     5.34 ns |   0.29 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 18,592.1 ns | 5,145.87 ns | 282.06 ns | 172.66 |    2.30 |      - |         - |          NA |
| TokenSortRatio |    945.4 ns |   118.77 ns |   6.51 ns |   8.78 |    0.06 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,293.8 ns |   557.95 ns |  30.58 ns |  30.59 |    0.26 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,437.0 ns |   264.77 ns |  14.51 ns |  41.21 |    0.15 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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

| Method                     | Length | Mean          | Error          | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|---------------:|-------------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **29.34 ns** |       **0.780 ns** |     **0.043 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     132.30 ns |       2.679 ns |     0.147 ns |  4.51 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      29.62 ns |       0.402 ns |     0.022 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      30.41 ns |       0.618 ns |     0.034 ns |  1.04 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **31.47 ns** |       **6.568 ns** |     **0.360 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     142.66 ns |       0.966 ns |     0.053 ns |  4.53 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      33.28 ns |       0.422 ns |     0.023 ns |  1.06 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      32.38 ns |       2.491 ns |     0.137 ns |  1.03 |    0.01 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **35.00 ns** |       **1.092 ns** |     **0.060 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     148.19 ns |      50.069 ns |     2.744 ns |  4.23 |    0.07 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      36.39 ns |       0.467 ns |     0.026 ns |  1.04 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      38.83 ns |       0.389 ns |     0.021 ns |  1.11 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **89.04 ns** |       **3.617 ns** |     **0.198 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     186.84 ns |       3.509 ns |     0.192 ns |  2.10 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      91.11 ns |       1.478 ns |     0.081 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      89.38 ns |       6.319 ns |     0.346 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **59.89 ns** |       **1.906 ns** |     **0.104 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     864.58 ns |     155.517 ns |     8.524 ns | 14.44 |    0.13 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      59.09 ns |       0.507 ns |     0.028 ns |  0.99 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      62.18 ns |       8.361 ns |     0.458 ns |  1.04 |    0.01 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **65.24 ns** |       **3.579 ns** |     **0.196 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,021.69 ns |      79.404 ns |     4.352 ns | 15.66 |    0.07 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      69.01 ns |       1.608 ns |     0.088 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      65.23 ns |       0.106 ns |     0.006 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,231.26 ns** |     **111.888 ns** |     **6.133 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  18,250.39 ns |   6,564.742 ns |   359.836 ns | 14.82 |    0.26 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,285.26 ns |      99.618 ns |     5.460 ns |  1.04 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,179.65 ns |      59.955 ns |     3.286 ns |  0.96 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **8,833.68 ns** |     **121.572 ns** |     **6.664 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 338,113.99 ns | 126,115.478 ns | 6,912.815 ns | 38.28 |    0.68 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   8,972.92 ns |     667.623 ns |    36.595 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   8,730.71 ns |      81.271 ns |     4.455 ns |  0.99 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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
| **Dp**     | **8**    |    **130.56 ns** |     **2.018 ns** |   **0.111 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |     65.60 ns |     0.280 ns |   0.015 ns |  0.50 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **12**   |    **220.72 ns** |     **5.569 ns** |   **0.305 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 12   |     69.89 ns |     3.227 ns |   0.177 ns |  0.32 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **14**   |    **291.77 ns** |   **441.796 ns** |  **24.216 ns** |  **1.00** |    **0.10** |         **-** |          **NA** |
| Kernel | 14   |     72.24 ns |     2.636 ns |   0.144 ns |  0.25 |    0.02 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **16**   |    **366.93 ns** |   **382.090 ns** |  **20.944 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel | 16   |     77.75 ns |     1.262 ns |   0.069 ns |  0.21 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **18**   |    **718.56 ns** |   **431.659 ns** |  **23.661 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 18   |     84.22 ns |     4.406 ns |   0.242 ns |  0.12 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **20**   |    **843.67 ns** |   **880.096 ns** |  **48.241 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel | 20   |     86.95 ns |     2.071 ns |   0.114 ns |  0.10 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **24**   |    **993.02 ns** |     **0.457 ns** |   **0.025 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 24   |     96.64 ns |     4.914 ns |   0.269 ns |  0.10 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **32**   |  **1,583.08 ns** |    **55.485 ns** |   **3.041 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    116.67 ns |     2.232 ns |   0.122 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **48**   |  **3,294.58 ns** |   **467.762 ns** |  **25.640 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |    147.12 ns |     0.648 ns |   0.036 ns |  0.04 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **64**   |  **5,280.25 ns** | **3,058.168 ns** | **167.628 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 64   |    171.93 ns |     3.904 ns |   0.214 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **96**   | **11,536.22 ns** | **4,735.827 ns** | **259.587 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 96   |  1,003.40 ns |    11.835 ns |   0.649 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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

| Method                     | Length | Mean         | Error        | StdDev     | Ratio | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-------------:|-----------:|------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **27.56 ns** |     **0.436 ns** |   **0.024 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    124.68 ns |     1.238 ns |   0.068 ns |  4.52 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     28.84 ns |     0.260 ns |   0.014 ns |  1.05 |         - |          NA |
|                            |        |              |              |            |       |           |             |
| **Distance_Utf16**             | **64**     |    **302.10 ns** |     **4.075 ns** |   **0.223 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    704.74 ns |     9.421 ns |   0.516 ns |  2.33 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    304.19 ns |     3.965 ns |   0.217 ns |  1.01 |         - |          NA |
|                            |        |              |              |            |       |           |             |
| **Distance_Utf16**             | **512**    | **16,200.25 ns** | **2,963.904 ns** | **162.462 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 18,365.81 ns |   493.475 ns |  27.049 ns |  1.13 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 16,519.07 ns |   658.619 ns |  36.101 ns |  1.02 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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
| **Distance_CodePoint** | **16**     | **32**       |       **359.2 ns** |       **1.51 ns** |      **0.08 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,524.2 ns |     207.84 ns |     11.39 ns |   4.24 |    0.03 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **352.0 ns** |      **12.55 ns** |      **0.69 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,519.9 ns |      78.11 ns |      4.28 ns |   4.32 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **473.1 ns** |      **51.59 ns** |      **2.83 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,495.7 ns |      84.32 ns |      4.62 ns |   7.39 |    0.04 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **444.4 ns** |      **19.46 ns** |      **1.07 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,540.1 ns |     270.59 ns |     14.83 ns |   7.97 |    0.03 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **537.7 ns** |      **19.23 ns** |      **1.05 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,390.7 ns |     415.02 ns |     22.75 ns |  11.88 |    0.04 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **537.7 ns** |       **2.75 ns** |      **0.15 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,323.7 ns |     261.69 ns |     14.34 ns |  11.76 |    0.02 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **623.1 ns** |      **10.03 ns** |      **0.55 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |     9,597.9 ns |     395.76 ns |     21.69 ns |  15.40 |    0.03 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **635.4 ns** |      **75.23 ns** |      **4.12 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |     9,582.3 ns |   1,458.98 ns |     79.97 ns |  15.08 |    0.14 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,520.8 ns** |      **73.13 ns** |      **4.01 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   103,454.9 ns |   5,528.62 ns |    303.04 ns |  41.04 |    0.12 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,419.8 ns** |     **307.22 ns** |     **16.84 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   104,816.5 ns |   1,185.39 ns |     64.98 ns |  43.32 |    0.26 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **20,519.5 ns** |     **901.03 ns** |     **49.39 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,404,673.2 ns |  32,437.23 ns |  1,777.99 ns | 117.19 |    0.26 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **440,600.9 ns** |  **11,222.85 ns** |    **615.16 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,790,467.4 ns | 459,490.08 ns | 25,186.20 ns |   4.06 |    0.05 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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

| Method         | Samples | Classes | Mean          | Error       | StdDev     | Gen0   | Allocated |
|--------------- |-------- |-------- |--------------:|------------:|-----------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |      **7.000 μs** |   **0.2180 μs** |  **0.0120 μs** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7.415 μs |   1.2874 μs |  0.0706 μs | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |      1.030 μs |   0.0084 μs |  0.0005 μs |      - |         - |
| F1Macro        | 1000    | 2       |      7.037 μs |   0.1970 μs |  0.0108 μs | 0.0229 |     472 B |
| Report         | 1000    | 2       |      9.611 μs |   0.6049 μs |  0.0332 μs | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7.412 μs** |   **0.1441 μs** |  **0.0079 μs** | **0.0687** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7.210 μs |   0.2101 μs |  0.0115 μs | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |      1.039 μs |   0.1496 μs |  0.0082 μs |      - |         - |
| F1Macro        | 1000    | 10      |      7.490 μs |   1.3057 μs |  0.0716 μs | 0.0992 |    1664 B |
| Report         | 1000    | 10      |     14.537 μs |   0.3319 μs |  0.0182 μs | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **815.039 μs** | **253.3374 μs** | **13.8863 μs** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    844.080 μs |  38.9071 μs |  2.1326 μs |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    139.827 μs | 159.9763 μs |  8.7688 μs |      - |         - |
| F1Macro        | 100000  | 2       |    828.095 μs |  52.8826 μs |  2.8987 μs |      - |     473 B |
| Report         | 100000  | 2       |    792.108 μs | 141.2667 μs |  7.7433 μs |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **996.820 μs** |  **50.8446 μs** |  **2.7870 μs** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    969.335 μs | 107.0702 μs |  5.8689 μs |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    243.543 μs |   8.3161 μs |  0.4558 μs |      - |         - |
| F1Macro        | 100000  | 10      |  1,011.313 μs | 166.2649 μs |  9.1135 μs |      - |    1665 B |
| Report         | 100000  | 10      |    999.211 μs | 124.5253 μs |  6.8257 μs |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,575.806 μs** | **347.9632 μs** | **19.0730 μs** |      **-** |     **516 B** |
| MatrixWeighted | 1000000 | 2       |  8,512.566 μs | 899.1219 μs | 49.2839 μs |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,919.064 μs |  23.9900 μs |  1.3150 μs |      - |         - |
| F1Macro        | 1000000 | 2       |  8,698.202 μs | 224.0359 μs | 12.2802 μs |      - |     484 B |
| Report         | 1000000 | 2       |  8,683.006 μs | 646.6373 μs | 35.4444 μs |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **10,011.881 μs** | **763.3417 μs** | **41.8413 μs** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 10,035.075 μs | 314.1404 μs | 17.2191 μs |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  3,111.252 μs |  71.7428 μs |  3.9325 μs |      - |         - |
| F1Macro        | 1000000 | 10      |  9,824.662 μs | 335.5571 μs | 18.3930 μs |      - |    1676 B |
| Report         | 1000000 | 10      | 10,242.368 μs | 387.3665 μs | 21.2329 μs |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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

| Method | Band | Mean         | Error        | StdDev    | Ratio | Allocated | Alloc Ratio |
|------- |----- |-------------:|-------------:|----------:|------:|----------:|------------:|
| **Dp**     | **4**    |     **82.88 ns** |     **2.728 ns** |  **0.150 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 4    |     80.38 ns |     0.694 ns |  0.038 ns |  0.97 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **6**    |    **117.57 ns** |    **15.739 ns** |  **0.863 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 6    |    115.87 ns |     0.633 ns |  0.035 ns |  0.99 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **8**    |    **156.95 ns** |     **3.532 ns** |  **0.194 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 8    |    100.15 ns |     0.514 ns |  0.028 ns |  0.64 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **10**   |    **206.72 ns** |     **7.150 ns** |  **0.392 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 10   |    109.68 ns |    16.605 ns |  0.910 ns |  0.53 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **12**   |    **271.40 ns** |    **27.641 ns** |  **1.515 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 12   |    119.79 ns |    10.732 ns |  0.588 ns |  0.44 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **16**   |    **427.93 ns** |    **13.089 ns** |  **0.717 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 16   |    142.99 ns |     8.988 ns |  0.493 ns |  0.33 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **24**   |    **863.72 ns** |     **9.539 ns** |  **0.523 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 24   |    179.89 ns |     1.269 ns |  0.070 ns |  0.21 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **32**   |  **1,485.07 ns** |    **29.315 ns** |  **1.607 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 32   |    220.34 ns |    14.045 ns |  0.770 ns |  0.15 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **48**   |  **3,249.41 ns** |    **16.566 ns** |  **0.908 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 48   |    305.44 ns |    13.863 ns |  0.760 ns |  0.09 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **64**   |  **5,663.70 ns** |    **77.829 ns** |  **4.266 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 64   |    389.20 ns |     4.731 ns |  0.259 ns |  0.07 |         - |          NA |
|        |      |              |              |           |       |           |             |
| **Dp**     | **96**   | **12,607.98 ns** | **1,585.081 ns** | **86.884 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 96   |  1,109.55 ns |   117.262 ns |  6.428 ns |  0.09 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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
| VocabTxt               |  4.295 ms | 2.7053 ms | 0.1483 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.147 ms | 2.9413 ms | 0.1612 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 11.194 ms | 0.1656 ms | 0.0091 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  4.099 ms | 2.5921 ms | 0.1421 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.938 ms | 0.6578 ms | 0.0361 ms |  27.3438 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              |  4.507 ms | 1.3405 ms | 0.0735 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  4.851 ms | 0.3017 ms | 0.0165 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  4.786 ms | 0.9430 ms | 0.0517 ms | 500.0000 | 468.7500 | 437.5000 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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
| **Count**                | **200**       |  **7.388 ms** | **0.8859 ms** | **0.0486 ms** |  **1.00** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.155 ms | 0.2949 ms | 0.0162 ms |  0.83 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.339 ms | 0.3948 ms | 0.0216 ms |  0.99 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.342 ms | 0.1707 ms | 0.0094 ms |  0.86 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |           |          |           |             |
| **Count**                | **1000**      | **30.034 ms** | **1.2246 ms** | **0.0671 ms** |  **1.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 23.033 ms | 2.9238 ms | 0.1603 ms |  0.77 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 28.587 ms | 3.0184 ms | 0.1654 ms |  0.95 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 24.163 ms | 1.2275 ms | 0.0673 ms |  0.80 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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
| **Dot**    | **384**  |  **51.48 ns** | **4.389 ns** | **0.241 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.75 ns | 1.725 ns | 0.095 ns |  0.95 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  |  **99.40 ns** | **0.857 ns** | **0.047 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  91.32 ns | 0.377 ns | 0.021 ns |  0.92 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **132.96 ns** | **7.851 ns** | **0.430 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 125.25 ns | 3.790 ns | 0.208 ns |  0.94 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

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
| **Count**        | **200**       |  **3.202 ms** | **8.1551 ms** | **0.4470 ms** |  **1.01** |    **0.17** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.021 ms | 1.6817 ms | 0.0922 ms |  0.95 |    0.11 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.786 ms | 0.3771 ms | 0.0207 ms |  1.20 |    0.13 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  3.019 ms | 2.2460 ms | 0.1231 ms |  0.95 |    0.11 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.997 ms** | **0.8019 ms** | **0.0440 ms** |  **1.00** |    **0.01** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.184 ms | 0.7588 ms | 0.0416 ms |  1.03 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.540 ms | 0.1867 ms | 0.0102 ms |  1.65 |    0.01 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  6.969 ms | 0.5918 ms | 0.0324 ms |  1.00 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 111.3 | 23.9 | 4.65x C# faster |
| 32 | 161.0 | 76.4 | 2.11x C# faster |
| 128 | 494.8 | 781.4 | 1.58x Py faster |
| 512 | 4895.5 | 8046.0 | 1.64x Py faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 140.4 | 18.3 | 7.66x C# faster |
| 32 | 255.7 | 129.4 | 1.98x C# faster |
| 128 | 1822.5 | 1435.0 | 1.27x C# faster |
| 512 | 15647.6 | 16407.1 | 1.05x Py faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.780 | 88.20x | 0.009 | 0.780 | 88.20x |
| accuracy_n1000_k2 | 0.001 | 0.404 | 384.20x | 0.001 | 0.404 | 384.00x |
| precision_recall_f1_macro_n1000_k2 | 0.007 | 1.406 | 192.19x | 0.007 | 1.406 | 192.18x |
| classification_report_n1000_k2 | 0.010 | 5.320 | 538.65x | 0.010 | 5.320 | 538.68x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.572 | 98.03x | 0.016 | 1.572 | 98.02x |
| balanced_accuracy_n1000_k2 | 0.007 | 0.834 | 115.29x | 0.007 | 0.834 | 115.29x |
| matthews_n1000_k2 | 0.007 | 1.567 | 216.62x | 0.007 | 1.567 | 216.63x |
| cohen_kappa_n1000_k2 | 0.007 | 0.880 | 121.37x | 0.007 | 0.880 | 121.37x |
| mse_n1000_k2 | 0.003 | 0.216 | 78.87x | 0.003 | 0.216 | 78.87x |
| mae_n1000_k2 | 0.003 | 0.217 | 78.89x | 0.003 | 0.216 | 78.88x |
| median_ae_n1000_k2 | 0.006 | 0.233 | 39.31x | 0.006 | 0.233 | 39.30x |
| r2_n1000_k2 | 0.003 | 0.271 | 99.32x | 0.003 | 0.271 | 99.33x |
| confusion_matrix_n1000_k10 | 0.009 | 0.781 | 85.41x | 0.009 | 0.781 | 85.41x |
| accuracy_n1000_k10 | 0.001 | 0.407 | 385.88x | 0.001 | 0.407 | 385.93x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.430 | 180.59x | 0.008 | 1.429 | 180.56x |
| classification_report_n1000_k10 | 0.014 | 5.500 | 385.21x | 0.014 | 5.499 | 385.20x |
| roc_auc_ovr_macro_n1000_k10 | 0.515 | 8.118 | 15.77x | 0.515 | 8.118 | 15.77x |
| balanced_accuracy_n1000_k10 | 0.008 | 0.841 | 105.20x | 0.008 | 0.841 | 105.19x |
| matthews_n1000_k10 | 0.008 | 1.593 | 200.32x | 0.008 | 1.593 | 200.32x |
| cohen_kappa_n1000_k10 | 0.008 | 0.884 | 105.46x | 0.008 | 0.884 | 105.45x |
| mse_n1000_k10 | 0.003 | 0.216 | 78.94x | 0.003 | 0.216 | 78.93x |
| mae_n1000_k10 | 0.003 | 0.215 | 78.83x | 0.003 | 0.215 | 78.84x |
| median_ae_n1000_k10 | 0.006 | 0.235 | 39.96x | 0.006 | 0.235 | 39.96x |
| r2_n1000_k10 | 0.003 | 0.272 | 99.77x | 0.003 | 0.272 | 99.77x |
| confusion_matrix_n100000_k2 | 1.002 | 10.954 | 10.93x | 1.002 | 10.953 | 10.93x |
| accuracy_n100000_k2 | 0.106 | 3.785 | 35.79x | 0.106 | 3.785 | 35.79x |
| precision_recall_f1_macro_n100000_k2 | 0.810 | 12.536 | 15.48x | 0.810 | 12.536 | 15.48x |
| classification_report_n100000_k2 | 0.844 | 26.775 | 31.73x | 0.844 | 26.773 | 31.73x |
| roc_auc_binary_n100000_k2 | 2.956 | 28.409 | 9.61x | 2.956 | 28.407 | 9.61x |
| balanced_accuracy_n100000_k2 | 0.814 | 11.049 | 13.57x | 0.814 | 11.048 | 13.57x |
| matthews_n100000_k2 | 0.815 | 22.068 | 27.09x | 0.815 | 22.066 | 27.09x |
| cohen_kappa_n100000_k2 | 0.817 | 11.076 | 13.56x | 0.817 | 11.075 | 13.56x |
| mse_n100000_k2 | 0.268 | 0.373 | 1.39x | 0.268 | 0.373 | 1.39x |
| mae_n100000_k2 | 0.268 | 0.371 | 1.38x | 0.268 | 0.371 | 1.38x |
| median_ae_n100000_k2 | 0.673 | 1.885 | 2.80x | 0.691 | 1.885 | 2.73x |
| r2_n100000_k2 | 0.260 | 0.585 | 2.25x | 0.260 | 0.585 | 2.25x |
| confusion_matrix_n100000_k10 | 0.963 | 10.952 | 11.37x | 0.963 | 10.950 | 11.37x |
| accuracy_n100000_k10 | 0.106 | 3.782 | 35.68x | 0.106 | 3.782 | 35.68x |
| precision_recall_f1_macro_n100000_k10 | 0.977 | 13.244 | 13.55x | 0.978 | 13.243 | 13.55x |
| classification_report_n100000_k10 | 1.002 | 29.683 | 29.61x | 1.002 | 29.680 | 29.61x |
| roc_auc_ovr_macro_n100000_k10 | 31.283 | 230.649 | 7.37x | 31.283 | 230.619 | 7.37x |
| balanced_accuracy_n100000_k10 | 0.915 | 11.022 | 12.05x | 0.915 | 11.020 | 12.05x |
| matthews_n100000_k10 | 0.919 | 22.828 | 24.85x | 0.919 | 22.828 | 24.85x |
| cohen_kappa_n100000_k10 | 1.151 | 11.057 | 9.61x | 1.150 | 11.057 | 9.61x |
| mse_n100000_k10 | 0.267 | 0.371 | 1.39x | 0.267 | 0.371 | 1.39x |
| mae_n100000_k10 | 0.268 | 0.371 | 1.38x | 0.268 | 0.371 | 1.38x |
| median_ae_n100000_k10 | 0.672 | 1.882 | 2.80x | 0.709 | 1.882 | 2.66x |
| r2_n100000_k10 | 0.260 | 0.582 | 2.24x | 0.260 | 0.582 | 2.24x |
| confusion_matrix_n1000000_k2 | 9.009 | 102.432 | 11.37x | 9.008 | 102.398 | 11.37x |
| accuracy_n1000000_k2 | 2.300 | 34.091 | 14.82x | 2.300 | 34.092 | 14.82x |
| precision_recall_f1_macro_n1000000_k2 | 8.744 | 112.384 | 12.85x | 8.744 | 112.360 | 12.85x |
| classification_report_n1000000_k2 | 9.007 | 219.049 | 24.32x | 9.006 | 219.028 | 24.32x |
| roc_auc_binary_n1000000_k2 | 41.962 | 306.147 | 7.30x | 41.961 | 306.120 | 7.30x |
| balanced_accuracy_n1000000_k2 | 8.711 | 102.441 | 11.76x | 8.711 | 102.437 | 11.76x |
| matthews_n1000000_k2 | 8.723 | 206.749 | 23.70x | 8.722 | 206.742 | 23.70x |
| cohen_kappa_n1000000_k2 | 8.734 | 102.607 | 11.75x | 8.734 | 102.598 | 11.75x |
| mse_n1000000_k2 | 2.676 | 1.813 | 0.68x | 2.676 | 1.813 | 0.68x |
| mae_n1000000_k2 | 2.671 | 1.796 | 0.67x | 2.671 | 1.796 | 0.67x |
| median_ae_n1000000_k2 | 6.404 | 15.401 | 2.40x | 6.489 | 15.398 | 2.37x |
| r2_n1000000_k2 | 2.599 | 3.481 | 1.34x | 2.599 | 3.480 | 1.34x |
| confusion_matrix_n1000000_k10 | 10.842 | 102.240 | 9.43x | 10.841 | 102.229 | 9.43x |
| accuracy_n1000000_k10 | 3.310 | 34.132 | 10.31x | 3.310 | 34.129 | 10.31x |
| precision_recall_f1_macro_n1000000_k10 | 10.241 | 119.434 | 11.66x | 10.241 | 119.417 | 11.66x |
| classification_report_n1000000_k10 | 10.861 | 247.075 | 22.75x | 10.860 | 247.052 | 22.75x |
| balanced_accuracy_n1000000_k10 | 10.105 | 102.283 | 10.12x | 10.104 | 102.280 | 10.12x |
| matthews_n1000000_k10 | 10.241 | 214.200 | 20.92x | 10.240 | 214.189 | 20.92x |
| cohen_kappa_n1000000_k10 | 10.301 | 102.416 | 9.94x | 10.300 | 102.408 | 9.94x |
| mse_n1000000_k10 | 2.679 | 1.804 | 0.67x | 2.679 | 1.803 | 0.67x |
| mae_n1000000_k10 | 2.686 | 1.789 | 0.67x | 2.686 | 1.789 | 0.67x |
| median_ae_n1000000_k10 | 6.272 | 15.412 | 2.46x | 6.345 | 15.410 | 2.43x |
| r2_n1000000_k10 | 2.819 | 3.460 | 1.23x | 2.819 | 3.460 | 1.23x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.68x
  mae_n1000000_k2                  0.67x
  mse_n1000000_k10                 0.67x
  mae_n1000000_k10                 0.67x

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-21, measured at commit `50bc8248b7a2359701283a3427b403aea46e437d`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.891 | 9.816 | 2.01x | 5.242 | 9.815 | 1.87x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.378 | 17.368 | 1.40x | 12.752 | 17.368 | 1.36x | 706,526 | 706,526 |
| tokenizer_json_unigram | 11.546 | 36.452 | 3.16x | 11.930 | 36.443 | 3.05x | 1,990,038 | 1,990,038 |
| spiece_model | 5.197 | 30.255 | 5.82x | 5.455 | 30.253 | 5.55x | 533,084 | 533,084 |
| tfidf_save | 2.295 | 2.539 | 1.11x | 2.353 | 2.539 | 1.08x | 581,787 | 591,922 |
| tfidf_load | 5.573 | 4.236 | 0.76x | 5.837 | 4.236 | 0.73x | 581,787 | 591,922 |
| embedding_index_save | 5.144 | 1.337 | 0.26x | 5.949 | 1.337 | 0.22x | 20,589,007 | 15,360,128 |
| embedding_index_load | 4.730 | 1.327 | 0.28x | 5.519 | 1.327 | 0.24x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 5.729 | 0.865 | 0.15x | 6.470 | 0.864 | 0.13x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 3.258 | 1.316 | 0.40x | 3.639 | 1.315 | 0.36x | 20,589,007 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 74.30x | 0.000 | 0.001 | 74.29x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 455.887 | 638.991 | 1.40x | 456.995 | 638.992 | 1.40x | 15,251,458 | 14,022,374 |
| embedding_index_load_gzip | 80.767 | 72.373 | 0.90x | 81.774 | 72.368 | 0.88x | 15,251,458 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
