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

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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

| Method             | CorpusSize | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|----------:|------:|--------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **6.257 μs** |   **0.1694 μs** | **0.0093 μs** |  **1.00** |    **0.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.296 μs |   0.2046 μs | 0.0112 μs |  1.01 |    0.00 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.401 μs |   0.3541 μs | 0.0194 μs |  1.02 |    0.00 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **8**          |   **111.242 μs** |  **29.4443 μs** | **1.6139 μs** |  **1.00** |    **0.02** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    72.337 μs |   8.1580 μs | 0.4472 μs |  0.65 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    74.453 μs |  17.5995 μs | 0.9647 μs |  0.67 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **32**         |   **413.403 μs** |  **31.8566 μs** | **1.7462 μs** |  **1.00** |    **0.01** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   252.431 μs |  36.7127 μs | 2.0123 μs |  0.61 |    0.00 |  26.8555 |  1.4648 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   251.279 μs |  14.9337 μs | 0.8186 μs |  0.61 |    0.00 |  25.8789 |  1.4648 |   427.8 KB |        0.91 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **128**        | **1,651.758 μs** | **129.5684 μs** | **7.1021 μs** |  **1.00** |    **0.01** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        | 1,024.948 μs | 116.6129 μs | 6.3919 μs |  0.62 |    0.00 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   980.352 μs | 119.9149 μs | 6.5729 μs |  0.59 |    0.00 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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

| Method  | Mean     | Error    | StdDev  | Ratio | Gen0       | Allocated | Alloc Ratio |
|-------- |---------:|---------:|--------:|------:|-----------:|----------:|------------:|
| Unigram | 599.6 ms | 25.37 ms | 1.39 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 560.7 ms | 49.46 ms | 2.71 ms |  0.94 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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

| Method                    | Length | Mean       | Error     | StdDev   | Gen0   | Allocated |
|-------------------------- |------- |-----------:|----------:|---------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |   **103.5 μs** |   **5.51 μs** |  **0.30 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **223.9 μs** |  **40.53 μs** |  **2.22 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **490.6 μs** |  **54.92 μs** |  **3.01 μs** | **3.9063** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,013.8 μs** | **287.91 μs** | **15.78 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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
| DpGroup    |  12.87 μs |  6.391 μs | 0.350 μs |         - |
| MyersGroup | 134.94 μs | 34.321 μs | 1.881 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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
| Ratio          |     97.77 ns |     1.218 ns |   0.067 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 18,806.44 ns | 2,242.507 ns | 122.919 ns | 192.35 |    1.09 |      - |         - |          NA |
| TokenSortRatio |  1,082.16 ns |   171.822 ns |   9.418 ns |  11.07 |    0.08 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,599.58 ns |   349.609 ns |  19.163 ns |  36.82 |    0.17 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,917.25 ns |   712.934 ns |  39.078 ns |  50.29 |    0.35 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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
| **Distance_Utf16**             | **8**      |      **26.61 ns** |      **0.952 ns** |     **0.052 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     135.11 ns |      8.317 ns |     0.456 ns |  5.08 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      27.07 ns |      2.045 ns |     0.112 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.65 ns |      0.123 ns |     0.007 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.64 ns** |     **23.225 ns** |     **1.273 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     153.83 ns |     48.049 ns |     2.634 ns |  5.38 |    0.22 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.94 ns |      0.256 ns |     0.014 ns |  1.01 |    0.04 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.18 ns |      1.818 ns |     0.100 ns |  0.98 |    0.04 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.96 ns** |      **0.499 ns** |     **0.027 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     154.33 ns |      3.613 ns |     0.198 ns |  4.99 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.92 ns |      0.549 ns |     0.030 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      30.82 ns |      3.162 ns |     0.173 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **87.60 ns** |      **1.717 ns** |     **0.094 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     187.12 ns |      0.320 ns |     0.018 ns |  2.14 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      86.49 ns |      2.598 ns |     0.142 ns |  0.99 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      91.55 ns |     18.417 ns |     1.010 ns |  1.05 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **55.15 ns** |     **16.560 ns** |     **0.908 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     660.13 ns |      8.875 ns |     0.486 ns | 11.97 |    0.17 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      54.38 ns |      1.238 ns |     0.068 ns |  0.99 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      61.25 ns |      4.261 ns |     0.234 ns |  1.11 |    0.02 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **59.15 ns** |      **0.569 ns** |     **0.031 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,015.13 ns |      7.700 ns |     0.422 ns | 17.16 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      63.58 ns |      3.611 ns |     0.198 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      58.83 ns |      1.899 ns |     0.104 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,347.46 ns** |    **259.910 ns** |    **14.247 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  23,622.42 ns | 14,720.125 ns |   806.860 ns | 17.53 |    0.54 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,324.41 ns |     45.211 ns |     2.478 ns |  0.98 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,321.27 ns |     29.853 ns |     1.636 ns |  0.98 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **8,278.54 ns** |    **222.171 ns** |    **12.178 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 340,583.56 ns | 71,640.549 ns | 3,926.860 ns | 41.14 |    0.41 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   8,464.34 ns |    359.030 ns |    19.680 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   8,282.70 ns |     39.223 ns |     2.150 ns |  1.00 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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
| **Dp**     | **8**    |    **132.22 ns** |      **2.423 ns** |     **0.133 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |     56.27 ns |     20.038 ns |     1.098 ns |  0.43 |    0.01 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **12**   |    **230.26 ns** |    **149.571 ns** |     **8.198 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 12   |     61.34 ns |      2.382 ns |     0.131 ns |  0.27 |    0.01 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **14**   |    **270.54 ns** |     **79.356 ns** |     **4.350 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 14   |     64.62 ns |      0.793 ns |     0.043 ns |  0.24 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **16**   |    **356.50 ns** |    **227.927 ns** |    **12.493 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 16   |     69.32 ns |      2.755 ns |     0.151 ns |  0.19 |    0.01 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **18**   |    **435.67 ns** |     **19.068 ns** |     **1.045 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 18   |     72.43 ns |      4.668 ns |     0.256 ns |  0.17 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **20**   |    **560.20 ns** |     **47.654 ns** |     **2.612 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 20   |     78.00 ns |     15.931 ns |     0.873 ns |  0.14 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **24**   |    **965.32 ns** |    **269.284 ns** |    **14.760 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 24   |     87.01 ns |      2.072 ns |     0.114 ns |  0.09 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **32**   |  **1,565.20 ns** |    **105.541 ns** |     **5.785 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    104.13 ns |      5.667 ns |     0.311 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **48**   |  **3,583.11 ns** |    **602.649 ns** |    **33.033 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |    133.21 ns |      9.741 ns |     0.534 ns |  0.04 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **64**   |  **6,183.16 ns** |    **717.045 ns** |    **39.304 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 64   |    161.91 ns |      4.219 ns |     0.231 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |               |              |       |         |           |             |
| **Dp**     | **96**   | **12,049.71 ns** | **20,834.508 ns** | **1,142.010 ns** |  **1.01** |    **0.11** |         **-** |          **NA** |
| Kernel | 96   |  1,010.38 ns |      5.872 ns |     0.322 ns |  0.08 |    0.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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

| Method                     | Length | Mean         | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **27.18 ns** |   **2.938 ns** |  **0.161 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    124.84 ns |  23.290 ns |  1.277 ns |  4.59 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.23 ns |   0.476 ns |  0.026 ns |  0.93 |    0.00 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **268.27 ns** |  **74.994 ns** |  **4.111 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    656.50 ns |   4.124 ns |  0.226 ns |  2.45 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    269.85 ns |  89.660 ns |  4.915 ns |  1.01 |    0.02 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **512**    | **14,205.40 ns** | **344.590 ns** | **18.888 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 16,594.42 ns | 233.602 ns | 12.804 ns |  1.17 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,310.02 ns | 506.571 ns | 27.767 ns |  1.01 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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

| Method             | Length | Distinct | Mean           | Error        | StdDev      | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |---------------:|-------------:|------------:|-------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |       **331.7 ns** |     **72.58 ns** |     **3.98 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,347.9 ns |     67.79 ns |     3.72 ns |   4.06 |    0.04 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **354.2 ns** |     **39.03 ns** |     **2.14 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,347.5 ns |    127.97 ns |     7.01 ns |   3.80 |    0.03 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **413.8 ns** |     **14.74 ns** |     **0.81 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,621.4 ns |  3,384.13 ns |   185.50 ns |   8.75 |    0.39 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **442.5 ns** |      **4.47 ns** |     **0.24 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,631.5 ns |    466.33 ns |    25.56 ns |   8.21 |    0.05 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **502.2 ns** |     **92.50 ns** |     **5.07 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,464.1 ns |  1,061.16 ns |    58.17 ns |  12.87 |    0.15 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **522.0 ns** |      **4.16 ns** |     **0.23 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,548.0 ns |    227.68 ns |    12.48 ns |  12.54 |    0.02 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **597.4 ns** |     **10.26 ns** |     **0.56 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |    10,125.1 ns |  2,087.60 ns |   114.43 ns |  16.95 |    0.17 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **607.2 ns** |     **10.59 ns** |     **0.58 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |    10,443.7 ns |    622.79 ns |    34.14 ns |  17.20 |    0.05 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,371.9 ns** |  **1,280.13 ns** |    **70.17 ns** |   **1.00** |    **0.04** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   135,590.5 ns |  6,978.72 ns |   382.53 ns |  57.20 |    1.45 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,363.0 ns** |    **275.66 ns** |    **15.11 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   110,994.7 ns |  4,851.85 ns |   265.95 ns |  46.97 |    0.28 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **18,144.3 ns** |    **284.62 ns** |    **15.60 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,360,091.9 ns | 27,708.34 ns | 1,518.79 ns | 130.07 |    0.12 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **478,473.6 ns** | **81,932.97 ns** | **4,491.02 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,771,286.7 ns | 89,889.12 ns | 4,927.13 ns |   3.70 |    0.03 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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

| Method         | Samples | Classes | Mean            | Error           | StdDev        | Gen0   | Allocated |
|--------------- |-------- |-------- |----------------:|----------------:|--------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |      **7,790.5 ns** |       **404.06 ns** |      **22.15 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7,337.0 ns |       568.04 ns |      31.14 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |        920.4 ns |        41.42 ns |       2.27 ns |      - |         - |
| F1Macro        | 1000    | 2       |      7,794.0 ns |       608.45 ns |      33.35 ns | 0.0153 |     472 B |
| Report         | 1000    | 2       |     10,699.5 ns |     1,973.73 ns |     108.19 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7,913.2 ns** |       **223.92 ns** |      **12.27 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7,745.9 ns |       251.89 ns |      13.81 ns | 0.0610 |    1248 B |
| AccuracyScore  | 1000    | 10      |        918.5 ns |        29.31 ns |       1.61 ns |      - |         - |
| F1Macro        | 1000    | 10      |      8,222.3 ns |       170.22 ns |       9.33 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     15,405.7 ns |       919.51 ns |      50.40 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **882,459.3 ns** |    **54,359.09 ns** |   **2,979.61 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    822,010.5 ns |   139,870.15 ns |   7,666.76 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    163,376.0 ns |     7,054.93 ns |     386.70 ns |      - |         - |
| F1Macro        | 100000  | 2       |    866,098.7 ns |    47,414.63 ns |   2,598.96 ns |      - |     473 B |
| Report         | 100000  | 2       |    877,778.4 ns |    73,453.86 ns |   4,026.25 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **981,582.9 ns** |    **32,283.56 ns** |   **1,769.57 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    944,840.4 ns |   142,303.05 ns |   7,800.11 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    264,497.9 ns |     3,258.84 ns |     178.63 ns |      - |         - |
| F1Macro        | 100000  | 10      |    983,090.0 ns |    26,976.80 ns |   1,478.69 ns |      - |    1665 B |
| Report         | 100000  | 10      |    952,000.7 ns |    33,270.74 ns |   1,823.68 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,705,995.4 ns** |   **261,913.04 ns** |  **14,356.34 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,293,142.6 ns |   518,595.42 ns |  28,425.97 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,757,101.0 ns |   262,318.83 ns |  14,378.58 ns |      - |         - |
| F1Macro        | 1000000 | 2       |  8,298,556.0 ns |    73,650.60 ns |   4,037.04 ns |      - |     484 B |
| Report         | 1000000 | 2       |  8,888,845.9 ns |   497,524.05 ns |  27,270.97 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      |  **9,797,959.7 ns** |   **435,992.00 ns** |  **23,898.19 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 10,448,258.9 ns |   240,915.96 ns |  13,205.42 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  2,691,004.4 ns |   104,988.40 ns |   5,754.77 ns |      - |         - |
| F1Macro        | 1000000 | 10      |  9,990,607.0 ns |   305,444.53 ns |  16,742.45 ns |      - |    1676 B |
| Report         | 1000000 | 10      | 11,407,251.7 ns | 3,940,175.01 ns | 215,974.29 ns |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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
| **Dp**     | **4**    |     **74.83 ns** |     **3.964 ns** |   **0.217 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 4    |     73.52 ns |     0.776 ns |   0.043 ns |  0.98 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **6**    |    **106.96 ns** |    **80.939 ns** |   **4.437 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel | 6    |    104.41 ns |     4.306 ns |   0.236 ns |  0.98 |    0.03 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **8**    |    **158.45 ns** |     **5.756 ns** |   **0.316 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |     90.11 ns |     2.655 ns |   0.146 ns |  0.57 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **10**   |    **202.42 ns** |    **11.807 ns** |   **0.647 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 10   |     96.83 ns |     0.667 ns |   0.037 ns |  0.48 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **12**   |    **251.36 ns** |    **83.221 ns** |   **4.562 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 12   |    103.70 ns |     0.466 ns |   0.026 ns |  0.41 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **16**   |    **417.80 ns** |   **286.065 ns** |  **15.680 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel | 16   |    122.06 ns |    38.421 ns |   2.106 ns |  0.29 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **24**   |    **858.82 ns** |    **62.650 ns** |   **3.434 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 24   |    156.76 ns |    15.746 ns |   0.863 ns |  0.18 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **32**   |  **1,447.70 ns** |    **15.113 ns** |   **0.828 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    193.04 ns |     6.237 ns |   0.342 ns |  0.13 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **48**   |  **3,323.73 ns** |   **573.206 ns** |  **31.419 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |    260.51 ns |     1.520 ns |   0.083 ns |  0.08 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **64**   |  **6,326.69 ns** | **2,187.832 ns** | **119.922 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 64   |    332.72 ns |    22.433 ns |   1.230 ns |  0.05 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **96**   | **14,166.58 ns** | **6,160.798 ns** | **337.694 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 96   |  1,084.64 ns |   577.958 ns |  31.680 ns |  0.08 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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
| VocabTxt               |  4.028 ms | 1.5198 ms | 0.0833 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.210 ms | 2.4108 ms | 0.1321 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.766 ms | 0.5382 ms | 0.0295 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.704 ms | 3.9057 ms | 0.2141 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.903 ms | 0.2381 ms | 0.0131 ms |  33.2031 |  27.3438 |  27.3438 |   2.09 MB |
| TfidfLoad              |  4.384 ms | 0.6029 ms | 0.0330 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  6.940 ms | 1.7929 ms | 0.0983 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  5.963 ms | 0.7394 ms | 0.0405 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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

| Method               | Documents | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0      | Gen1      | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|---------:|----------:|------:|--------:|----------:|----------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.231 ms** | **1.455 ms** | **0.0798 ms** |  **1.00** |    **0.01** |  **507.8125** |  **242.1875** |  **70.3125** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.066 ms | 1.499 ms | 0.0822 ms |  0.84 |    0.01 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.366 ms | 2.106 ms | 0.1154 ms |  1.02 |    0.02 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.466 ms | 1.317 ms | 0.0722 ms |  0.89 |    0.01 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |          |           |       |         |           |           |          |           |             |
| **Count**                | **1000**      | **29.662 ms** | **4.643 ms** | **0.2545 ms** |  **1.00** |    **0.01** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 23.007 ms | 1.886 ms | 0.1034 ms |  0.78 |    0.01 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 28.274 ms | 4.795 ms | 0.2629 ms |  0.95 |    0.01 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 24.117 ms | 1.498 ms | 0.0821 ms |  0.81 |    0.01 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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
| **Dot**    | **384**  |  **51.26 ns** |  **1.619 ns** | **0.089 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.55 ns |  1.514 ns | 0.083 ns |  0.95 |         - |          NA |
|        |      |           |           |          |       |           |             |
| **Dot**    | **768**  |  **94.60 ns** |  **3.555 ns** | **0.195 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  93.25 ns | 15.744 ns | 0.863 ns |  0.99 |         - |          NA |
|        |      |           |           |          |       |           |             |
| **Dot**    | **1024** | **123.99 ns** |  **0.210 ns** | **0.012 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 122.90 ns |  8.190 ns | 0.449 ns |  0.99 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

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
| **Count**        | **200**       |  **2.888 ms** | **2.4548 ms** | **0.1346 ms** |  **1.00** |    **0.06** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.172 ms | 8.5273 ms | 0.4674 ms |  1.10 |    0.15 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.697 ms | 0.1829 ms | 0.0100 ms |  1.28 |    0.05 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.914 ms | 1.7941 ms | 0.0983 ms |  1.01 |    0.05 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.858 ms** | **0.5966 ms** | **0.0327 ms** |  **1.00** |    **0.01** | **492.1875** | **351.5625** |  **70.3125** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.081 ms | 0.8053 ms | 0.0441 ms |  1.03 |    0.01 | 492.1875 | 304.6875 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.354 ms | 0.6715 ms | 0.0368 ms |  1.66 |    0.01 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  6.827 ms | 0.4736 ms | 0.0260 ms |  1.00 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 115.5 | 27.2 | 4.24x C# faster |
| 32 | 175.3 | 93.2 | 1.88x C# faster |
| 128 | 474.6 | 848.0 | 1.79x Py faster |
| 512 | 4455.5 | 7380.5 | 1.66x Py faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 158.6 | 18.4 | 8.64x C# faster |
| 32 | 290.1 | 165.2 | 1.76x C# faster |
| 128 | 1717.3 | 1357.1 | 1.27x C# faster |
| 512 | 13988.8 | 14546.6 | 1.04x Py faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.955 | 102.67x | 0.009 | 0.955 | 102.67x |
| accuracy_n1000_k2 | 0.001 | 0.506 | 489.70x | 0.001 | 0.506 | 489.71x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.708 | 220.05x | 0.008 | 1.707 | 220.01x |
| classification_report_n1000_k2 | 0.010 | 6.514 | 623.12x | 0.010 | 6.513 | 623.08x |
| roc_auc_binary_n1000_k2 | 0.015 | 1.874 | 123.40x | 0.015 | 1.874 | 123.39x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.017 | 132.41x | 0.008 | 1.017 | 132.40x |
| matthews_n1000_k2 | 0.008 | 1.912 | 251.30x | 0.008 | 1.912 | 251.29x |
| cohen_kappa_n1000_k2 | 0.008 | 1.060 | 138.24x | 0.008 | 1.059 | 138.25x |
| mse_n1000_k2 | 0.002 | 0.297 | 122.47x | 0.002 | 0.297 | 122.46x |
| mae_n1000_k2 | 0.002 | 0.294 | 121.66x | 0.002 | 0.294 | 121.67x |
| median_ae_n1000_k2 | 0.007 | 0.309 | 47.47x | 0.007 | 0.309 | 47.47x |
| r2_n1000_k2 | 0.002 | 0.360 | 145.53x | 0.002 | 0.360 | 145.54x |
| confusion_matrix_n1000_k10 | 0.010 | 0.962 | 100.56x | 0.010 | 0.962 | 100.56x |
| accuracy_n1000_k10 | 0.001 | 0.513 | 456.30x | 0.001 | 0.513 | 456.36x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.749 | 207.02x | 0.008 | 1.749 | 206.97x |
| classification_report_n1000_k10 | 0.015 | 6.804 | 445.27x | 0.015 | 6.803 | 445.23x |
| roc_auc_ovr_macro_n1000_k10 | 0.541 | 9.711 | 17.94x | 0.541 | 9.710 | 17.94x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.031 | 123.04x | 0.008 | 1.031 | 123.06x |
| matthews_n1000_k10 | 0.008 | 1.957 | 236.63x | 0.008 | 1.956 | 236.66x |
| cohen_kappa_n1000_k10 | 0.009 | 1.073 | 123.13x | 0.009 | 1.072 | 123.14x |
| mse_n1000_k10 | 0.002 | 0.300 | 123.79x | 0.002 | 0.300 | 123.80x |
| mae_n1000_k10 | 0.002 | 0.297 | 122.94x | 0.002 | 0.297 | 122.94x |
| median_ae_n1000_k10 | 0.007 | 0.310 | 47.04x | 0.007 | 0.310 | 47.04x |
| r2_n1000_k10 | 0.002 | 0.363 | 146.85x | 0.002 | 0.363 | 146.86x |
| confusion_matrix_n100000_k2 | 0.995 | 10.746 | 10.80x | 0.995 | 10.745 | 10.80x |
| accuracy_n100000_k2 | 0.186 | 3.748 | 20.16x | 0.186 | 3.748 | 20.16x |
| precision_recall_f1_macro_n100000_k2 | 0.869 | 12.302 | 14.16x | 0.869 | 12.301 | 14.16x |
| classification_report_n100000_k2 | 0.861 | 26.816 | 31.14x | 0.861 | 26.815 | 31.14x |
| roc_auc_binary_n100000_k2 | 3.563 | 26.575 | 7.46x | 3.563 | 26.571 | 7.46x |
| balanced_accuracy_n100000_k2 | 0.866 | 10.830 | 12.51x | 0.866 | 10.829 | 12.51x |
| matthews_n100000_k2 | 0.855 | 21.557 | 25.20x | 0.855 | 21.554 | 25.20x |
| cohen_kappa_n100000_k2 | 0.863 | 10.840 | 12.55x | 0.863 | 10.839 | 12.55x |
| mse_n100000_k2 | 0.240 | 0.440 | 1.84x | 0.240 | 0.440 | 1.84x |
| mae_n100000_k2 | 0.237 | 0.451 | 1.90x | 0.237 | 0.451 | 1.90x |
| median_ae_n100000_k2 | 0.760 | 1.791 | 2.36x | 0.781 | 1.791 | 2.29x |
| r2_n100000_k2 | 0.235 | 0.702 | 2.98x | 0.235 | 0.702 | 2.98x |
| confusion_matrix_n100000_k10 | 0.977 | 10.747 | 11.00x | 0.977 | 10.745 | 10.99x |
| accuracy_n100000_k10 | 0.270 | 3.761 | 13.93x | 0.270 | 3.760 | 13.93x |
| precision_recall_f1_macro_n100000_k10 | 1.000 | 12.947 | 12.94x | 1.000 | 12.946 | 12.94x |
| classification_report_n100000_k10 | 0.981 | 29.448 | 30.02x | 0.981 | 29.446 | 30.02x |
| roc_auc_ovr_macro_n100000_k10 | 35.391 | 217.152 | 6.14x | 35.389 | 217.117 | 6.14x |
| balanced_accuracy_n100000_k10 | 0.980 | 10.863 | 11.09x | 0.980 | 10.863 | 11.08x |
| matthews_n100000_k10 | 0.971 | 22.254 | 22.91x | 0.971 | 22.253 | 22.91x |
| cohen_kappa_n100000_k10 | 0.995 | 10.885 | 10.94x | 0.995 | 10.884 | 10.94x |
| mse_n100000_k10 | 0.239 | 0.459 | 1.92x | 0.239 | 0.459 | 1.92x |
| mae_n100000_k10 | 0.238 | 0.451 | 1.90x | 0.238 | 0.451 | 1.90x |
| median_ae_n100000_k10 | 0.775 | 1.790 | 2.31x | 0.826 | 1.790 | 2.17x |
| r2_n100000_k10 | 0.235 | 0.704 | 2.99x | 0.235 | 0.704 | 2.99x |
| confusion_matrix_n1000000_k2 | 8.628 | 100.250 | 11.62x | 8.627 | 100.223 | 11.62x |
| accuracy_n1000000_k2 | 1.943 | 33.100 | 17.04x | 1.943 | 33.098 | 17.04x |
| precision_recall_f1_macro_n1000000_k2 | 8.715 | 107.821 | 12.37x | 8.714 | 107.804 | 12.37x |
| classification_report_n1000000_k2 | 8.609 | 209.867 | 24.38x | 8.608 | 209.852 | 24.38x |
| roc_auc_binary_n1000000_k2 | 49.547 | 288.119 | 5.82x | 49.540 | 288.102 | 5.82x |
| balanced_accuracy_n1000000_k2 | 8.672 | 100.049 | 11.54x | 8.672 | 100.038 | 11.54x |
| matthews_n1000000_k2 | 8.618 | 200.493 | 23.26x | 8.618 | 200.483 | 23.26x |
| cohen_kappa_n1000000_k2 | 8.703 | 100.017 | 11.49x | 8.702 | 100.008 | 11.49x |
| mse_n1000000_k2 | 2.408 | 2.230 | 0.93x | 2.408 | 2.230 | 0.93x |
| mae_n1000000_k2 | 2.387 | 2.227 | 0.93x | 2.387 | 2.227 | 0.93x |
| median_ae_n1000000_k2 | 7.171 | 14.255 | 1.99x | 7.254 | 14.253 | 1.96x |
| r2_n1000000_k2 | 2.374 | 4.036 | 1.70x | 2.374 | 4.036 | 1.70x |
| confusion_matrix_n1000000_k10 | 9.795 | 100.224 | 10.23x | 9.795 | 100.216 | 10.23x |
| accuracy_n1000000_k10 | 2.815 | 33.120 | 11.77x | 2.815 | 33.118 | 11.77x |
| precision_recall_f1_macro_n1000000_k10 | 9.971 | 114.256 | 11.46x | 9.969 | 114.255 | 11.46x |
| classification_report_n1000000_k10 | 9.864 | 235.194 | 23.84x | 9.863 | 235.179 | 23.84x |
| balanced_accuracy_n1000000_k10 | 9.882 | 100.619 | 10.18x | 9.882 | 100.607 | 10.18x |
| matthews_n1000000_k10 | 9.771 | 208.195 | 21.31x | 9.770 | 208.171 | 21.31x |
| cohen_kappa_n1000000_k10 | 9.900 | 100.960 | 10.20x | 9.898 | 100.947 | 10.20x |
| mse_n1000000_k10 | 2.408 | 2.389 | 0.99x | 2.408 | 2.389 | 0.99x |
| mae_n1000000_k10 | 2.385 | 2.445 | 1.02x | 2.385 | 2.444 | 1.02x |
| median_ae_n1000000_k10 | 7.265 | 14.491 | 1.99x | 7.423 | 14.489 | 1.95x |
| r2_n1000000_k10 | 2.776 | 4.471 | 1.61x | 2.775 | 4.471 | 1.61x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.93x
  mae_n1000000_k2                  0.93x
  mse_n1000000_k10                 0.99x

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-20, measured at commit `9f66a0b2e09f204dd9d7e680f3655d6d084d26c8`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.485 | 9.692 | 2.16x | 5.281 | 9.691 | 1.83x |
| tokenizer_json_wordpiece | 12.273 | 15.853 | 1.29x | 12.660 | 15.853 | 1.25x |
| tokenizer_json_unigram | 12.889 | 38.410 | 2.98x | 13.758 | 38.406 | 2.79x |
| spiece_model | 5.036 | 28.378 | 5.64x | 5.294 | 28.375 | 5.36x |
| tfidf_save | 1.842 | 2.546 | 1.38x | 1.874 | 2.546 | 1.36x |
| tfidf_load | 4.036 | 4.003 | 0.99x | 4.235 | 4.003 | 0.95x |
| embedding_index_save | 6.457 | 4.702 | 0.73x | 7.030 | 4.701 | 0.67x |
| embedding_index_load | 5.843 | 1.430 | 0.24x | 6.697 | 1.430 | 0.21x |
| embedding_index_load_file | 6.487 | 0.877 | 0.14x | 7.321 | 0.876 | 0.12x |
| embedding_index_load_memory | 4.178 | 1.433 | 0.34x | 4.702 | 1.433 | 0.30x |
| embedding_index_view_floor | 0.000 | 0.001 | 94.16x | 0.000 | 0.001 | 94.16x |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.

<!-- markdownlint-enable MD060 -->
