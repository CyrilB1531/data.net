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

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| **UnitLoop**           | **1**          |     **5.992 μs** |   **0.0996 μs** | **0.0055 μs** |  **1.00** |    **0.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.022 μs |   0.7156 μs | 0.0392 μs |  1.00 |    0.01 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.030 μs |   0.3517 μs | 0.0193 μs |  1.01 |    0.00 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **8**          |   **106.317 μs** |  **25.2660 μs** | **1.3849 μs** |  **1.00** |    **0.02** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    68.654 μs |   6.7110 μs | 0.3679 μs |  0.65 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    67.835 μs |   2.2726 μs | 0.1246 μs |  0.64 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **32**         |   **389.498 μs** |  **18.7607 μs** | **1.0283 μs** |  **1.00** |    **0.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   237.056 μs |   9.5189 μs | 0.5218 μs |  0.61 |    0.00 |  26.8555 |  1.7090 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   225.652 μs |  12.4026 μs | 0.6798 μs |  0.58 |    0.00 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **128**        | **1,572.018 μs** |  **67.4882 μs** | **3.6993 μs** |  **1.00** |    **0.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   954.128 μs | 106.5519 μs | 5.8405 μs |  0.61 |    0.00 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   883.355 μs |  55.7574 μs | 3.0563 μs |  0.56 |    0.00 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| Unigram | 562.1 ms | 32.23 ms | 1.77 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 540.2 ms | 35.43 ms | 1.94 ms |  0.96 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| **BpeOnOnePathologicalToken** | **512**    | **104.6 μs** | **11.59 μs** | **0.64 μs** | **1.2207** |      **-** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **217.8 μs** | **14.06 μs** | **0.77 μs** | **2.4414** |      **-** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **481.0 μs** | **40.41 μs** | **2.21 μs** | **4.3945** |      **-** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **981.6 μs** | **62.68 μs** | **3.44 μs** | **8.7891** | **0.9766** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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

| Method     | Mean      | Error    | StdDev   | Allocated |
|----------- |----------:|---------:|---------:|----------:|
| DpGroup    |  12.75 μs | 1.869 μs | 0.102 μs |         - |
| MyersGroup | 133.77 μs | 5.485 μs | 0.301 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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

| Method         | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     99.38 ns |   0.700 ns |  0.038 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 18,267.93 ns | 986.055 ns | 54.049 ns | 183.81 |    0.47 |      - |         - |          NA |
| TokenSortRatio |  1,070.93 ns |  28.977 ns |  1.588 ns |  10.78 |    0.01 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,326.45 ns |  63.009 ns |  3.454 ns |  33.47 |    0.03 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,622.87 ns | 189.076 ns | 10.364 ns |  46.52 |    0.09 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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

| Method                     | Length | Mean          | Error          | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|---------------:|-------------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **27.51 ns** |       **1.824 ns** |     **0.100 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     132.68 ns |       1.186 ns |     0.065 ns |  4.82 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      27.02 ns |       0.612 ns |     0.034 ns |  0.98 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.60 ns |       0.437 ns |     0.024 ns |  0.97 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.31 ns** |       **4.943 ns** |     **0.271 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     153.49 ns |      38.235 ns |     2.096 ns |  5.42 |    0.08 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.07 ns |       1.341 ns |     0.074 ns |  0.99 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.36 ns |       0.339 ns |     0.019 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.87 ns** |       **1.918 ns** |     **0.105 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     173.66 ns |      33.384 ns |     1.830 ns |  5.62 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.08 ns |       1.578 ns |     0.087 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      33.43 ns |       2.491 ns |     0.137 ns |  1.08 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **86.99 ns** |       **0.667 ns** |     **0.037 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     183.40 ns |      12.527 ns |     0.687 ns |  2.11 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      86.53 ns |       1.278 ns |     0.070 ns |  0.99 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      87.30 ns |       3.565 ns |     0.195 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **54.95 ns** |       **9.816 ns** |     **0.538 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     666.34 ns |      40.663 ns |     2.229 ns | 12.13 |    0.11 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      54.46 ns |       3.311 ns |     0.181 ns |  0.99 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      57.11 ns |      16.483 ns |     0.904 ns |  1.04 |    0.02 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **59.06 ns** |       **3.734 ns** |     **0.205 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,020.93 ns |       5.012 ns |     0.275 ns | 17.29 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      63.25 ns |       0.323 ns |     0.018 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      59.18 ns |       3.489 ns |     0.191 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,294.93 ns** |      **78.304 ns** |     **4.292 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  21,887.33 ns |   7,999.071 ns |   438.456 ns | 16.90 |    0.30 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,360.30 ns |      99.734 ns |     5.467 ns |  1.05 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,385.68 ns |      27.702 ns |     1.518 ns |  1.07 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |  **11,395.36 ns** |      **71.596 ns** |     **3.924 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 335,662.35 ns | 147,728.530 ns | 8,097.499 ns | 29.46 |    0.62 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |  11,860.63 ns |     453.031 ns |    24.832 ns |  1.04 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |  11,435.04 ns |     558.131 ns |    30.593 ns |  1.00 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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

| Method | Band | Mean         | Error         | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |-------------:|--------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**     | **8**    |    **131.31 ns** |      **9.024 ns** |   **0.495 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |     56.16 ns |      0.121 ns |   0.007 ns |  0.43 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **12**   |    **230.19 ns** |     **73.540 ns** |   **4.031 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 12   |     62.12 ns |      2.421 ns |   0.133 ns |  0.27 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **14**   |    **277.68 ns** |    **107.804 ns** |   **5.909 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 14   |     64.66 ns |      3.105 ns |   0.170 ns |  0.23 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **16**   |    **362.87 ns** |    **173.191 ns** |   **9.493 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 16   |     69.35 ns |      4.819 ns |   0.264 ns |  0.19 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **18**   |    **444.01 ns** |    **156.923 ns** |   **8.601 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 18   |     72.35 ns |      3.131 ns |   0.172 ns |  0.16 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **20**   |    **788.64 ns** |     **37.945 ns** |   **2.080 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 20   |     77.56 ns |      7.726 ns |   0.423 ns |  0.10 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **24**   |    **975.10 ns** |    **457.513 ns** |  **25.078 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 24   |     87.24 ns |      4.800 ns |   0.263 ns |  0.09 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **32**   |  **1,522.86 ns** |     **45.079 ns** |   **2.471 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    103.50 ns |      5.845 ns |   0.320 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **48**   |  **3,234.68 ns** |  **2,831.082 ns** | **155.181 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel | 48   |    133.01 ns |      0.726 ns |   0.040 ns |  0.04 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **64**   |  **5,450.67 ns** |     **25.283 ns** |   **1.386 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 64   |    160.20 ns |      3.589 ns |   0.197 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |               |            |       |         |           |             |
| **Dp**     | **96**   | **12,176.42 ns** | **15,956.755 ns** | **874.644 ns** |  **1.00** |    **0.09** |         **-** |          **NA** |
| Kernel | 96   |  1,128.76 ns |      5.468 ns |   0.300 ns |  0.09 |    0.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| **Distance_Utf16**             | **8**      |     **25.38 ns** |   **0.822 ns** |  **0.045 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    119.56 ns |  20.241 ns |  1.109 ns |  4.71 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.46 ns |   0.551 ns |  0.030 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **269.21 ns** |  **79.772 ns** |  **4.373 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    658.59 ns |  22.864 ns |  1.253 ns |  2.45 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    267.61 ns |  26.259 ns |  1.439 ns |  0.99 |    0.01 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **512**    | **14,249.09 ns** |  **66.657 ns** |  **3.654 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 16,473.28 ns | 415.249 ns | 22.761 ns |  1.16 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,439.40 ns | 133.667 ns |  7.327 ns |  1.01 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| **Distance_CodePoint** | **16**     | **32**       |       **343.0 ns** |       **6.72 ns** |      **0.37 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,427.9 ns |     340.44 ns |     18.66 ns |   4.16 |    0.05 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **341.2 ns** |       **3.30 ns** |      **0.18 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,349.5 ns |      65.31 ns |      3.58 ns |   3.96 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **428.3 ns** |       **3.00 ns** |      **0.16 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,384.6 ns |     108.35 ns |      5.94 ns |   7.90 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **417.2 ns** |       **2.68 ns** |      **0.15 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     4,038.3 ns |   2,151.61 ns |    117.94 ns |   9.68 |    0.24 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **506.6 ns** |       **2.35 ns** |      **0.13 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,346.2 ns |     713.03 ns |     39.08 ns |  12.53 |    0.07 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **511.8 ns** |      **24.49 ns** |      **1.34 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,856.7 ns |   2,002.15 ns |    109.74 ns |  13.40 |    0.19 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **603.5 ns** |       **8.79 ns** |      **0.48 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |    10,093.6 ns |   4,714.30 ns |    258.41 ns |  16.73 |    0.37 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **589.3 ns** |       **8.75 ns** |      **0.48 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |    10,372.4 ns |   2,243.61 ns |    122.98 ns |  17.60 |    0.18 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,346.8 ns** |      **82.25 ns** |      **4.51 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   134,940.3 ns |  15,510.66 ns |    850.19 ns |  57.50 |    0.33 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,323.4 ns** |     **146.09 ns** |      **8.01 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   101,153.1 ns |   5,168.94 ns |    283.33 ns |  43.54 |    0.17 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **18,012.8 ns** |     **194.04 ns** |     **10.64 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,359,602.3 ns |  23,937.66 ns |  1,312.10 ns | 131.00 |    0.09 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **483,620.2 ns** |  **85,148.84 ns** |  **4,667.30 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,903,131.3 ns | 187,361.75 ns | 10,269.93 ns |   3.94 |    0.04 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| **Matrix**         | **1000**    | **2**       |     **7,299.3 ns** |       **188.59 ns** |      **10.34 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     7,245.0 ns |       815.64 ns |      44.71 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |       919.1 ns |        44.26 ns |       2.43 ns |      - |         - |
| F1Macro        | 1000    | 2       |     7,607.8 ns |       159.92 ns |       8.77 ns | 0.0229 |     472 B |
| Report         | 1000    | 2       |    10,370.0 ns |       384.79 ns |      21.09 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **7,800.1 ns** |       **393.46 ns** |      **21.57 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     7,636.3 ns |       603.31 ns |      33.07 ns | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |       920.1 ns |        61.62 ns |       3.38 ns |      - |         - |
| F1Macro        | 1000    | 10      |     7,980.1 ns |       371.37 ns |      20.36 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |    14,989.5 ns |       299.58 ns |      16.42 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **873,030.1 ns** |    **21,691.11 ns** |   **1,188.96 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   815,746.4 ns |    34,549.53 ns |   1,893.78 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   167,089.3 ns |    47,923.76 ns |   2,626.86 ns |      - |         - |
| F1Macro        | 100000  | 2       |   881,930.6 ns |    24,785.41 ns |   1,358.57 ns |      - |     473 B |
| Report         | 100000  | 2       |   862,903.1 ns |    38,324.41 ns |   2,100.69 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **984,648.1 ns** |    **48,837.15 ns** |   **2,676.93 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   935,912.0 ns |     8,977.64 ns |     492.09 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   265,132.0 ns |    24,275.49 ns |   1,330.62 ns |      - |         - |
| F1Macro        | 100000  | 10      |   980,379.8 ns |    30,215.36 ns |   1,656.21 ns |      - |    1665 B |
| Report         | 100000  | 10      |   991,658.0 ns |    54,407.76 ns |   2,982.27 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **8,685,533.6 ns** |   **324,808.21 ns** |  **17,803.83 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       | 8,432,232.0 ns |   253,179.09 ns |  13,877.60 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       | 1,745,611.5 ns |    92,252.16 ns |   5,056.65 ns |      - |         - |
| F1Macro        | 1000000 | 2       | 8,513,132.1 ns | 6,058,747.42 ns | 332,100.39 ns |      - |     484 B |
| Report         | 1000000 | 2       | 8,272,492.8 ns |    69,946.19 ns |   3,833.99 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **9,369,538.0 ns** |   **444,105.68 ns** |  **24,342.93 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 9,324,317.4 ns |   179,060.59 ns |   9,814.92 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      | 2,694,813.9 ns |   380,800.66 ns |  20,872.97 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 9,620,431.8 ns |   193,846.60 ns |  10,625.39 ns |      - |    1676 B |
| Report         | 1000000 | 10      | 9,942,701.1 ns |   485,808.02 ns |  26,628.78 ns |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| **Dp**     | **4**    |     **73.36 ns** |     **2.450 ns** |   **0.134 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 4    |     73.36 ns |     1.107 ns |   0.061 ns |  1.00 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **6**    |    **105.03 ns** |     **1.270 ns** |   **0.070 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 6    |    103.74 ns |     0.981 ns |   0.054 ns |  0.99 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **8**    |    **156.04 ns** |     **4.387 ns** |   **0.240 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |     88.23 ns |     1.841 ns |   0.101 ns |  0.57 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **10**   |    **197.09 ns** |     **4.209 ns** |   **0.231 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 10   |     95.29 ns |     0.567 ns |   0.031 ns |  0.48 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **12**   |    **245.08 ns** |    **10.403 ns** |   **0.570 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 12   |    105.63 ns |     2.447 ns |   0.134 ns |  0.43 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **16**   |    **409.73 ns** |     **5.516 ns** |   **0.302 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 16   |    120.75 ns |     0.792 ns |   0.043 ns |  0.29 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **24**   |    **844.21 ns** |   **146.864 ns** |   **8.050 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 24   |    155.17 ns |    13.942 ns |   0.764 ns |  0.18 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **32**   |  **1,451.27 ns** |     **8.517 ns** |   **0.467 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    193.99 ns |     6.813 ns |   0.373 ns |  0.13 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **48**   |  **3,365.65 ns** | **2,165.948 ns** | **118.723 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel | 48   |    262.34 ns |     3.624 ns |   0.199 ns |  0.08 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **64**   |  **6,180.54 ns** |   **127.118 ns** |   **6.968 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 64   |    330.88 ns |     2.184 ns |   0.120 ns |  0.05 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **96**   | **14,024.68 ns** | **1,639.926 ns** |  **89.890 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 96   |  1,093.02 ns |    78.034 ns |   4.277 ns |  0.08 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| VocabTxt               |  4.018 ms | 1.6810 ms | 0.0921 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.243 ms | 2.2292 ms | 0.1222 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.369 ms | 0.2937 ms | 0.0161 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.732 ms | 3.5719 ms | 0.1958 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.868 ms | 0.9823 ms | 0.0538 ms |  33.2031 |  27.3438 |  27.3438 |   2.09 MB |
| TfidfLoad              |  4.363 ms | 0.7157 ms | 0.0392 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  6.688 ms | 7.5832 ms | 0.4157 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  5.744 ms | 0.1129 ms | 0.0062 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| **Count**                | **200**       |  **7.092 ms** | **0.8670 ms** | **0.0475 ms** |  **1.00** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.013 ms | 0.4023 ms | 0.0221 ms |  0.85 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.054 ms | 0.7589 ms | 0.0416 ms |  0.99 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.219 ms | 0.7940 ms | 0.0435 ms |  0.88 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |           |          |           |             |
| **Count**                | **1000**      | **28.802 ms** | **4.0133 ms** | **0.2200 ms** |  **1.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 22.834 ms | 3.0648 ms | 0.1680 ms |  0.79 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 27.687 ms | 2.4773 ms | 0.1358 ms |  0.96 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 23.919 ms | 0.7753 ms | 0.0425 ms |  0.83 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| **Dot**    | **384**  |  **49.38 ns** | **0.213 ns** | **0.012 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.55 ns | 0.297 ns | 0.016 ns |  0.98 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  |  **95.32 ns** | **3.820 ns** | **0.209 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  93.09 ns | 0.294 ns | 0.016 ns |  0.98 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **123.98 ns** | **3.740 ns** | **0.205 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 122.63 ns | 0.853 ns | 0.047 ns |  0.99 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

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
| **Count**        | **200**       |  **3.181 ms** | **3.9821 ms** | **0.2183 ms** |  **1.00** |    **0.09** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  2.878 ms | 1.5810 ms | 0.0867 ms |  0.91 |    0.06 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.624 ms | 0.8409 ms | 0.0461 ms |  1.14 |    0.07 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.882 ms | 1.5747 ms | 0.0863 ms |  0.91 |    0.06 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.945 ms** | **1.7192 ms** | **0.0942 ms** |  **1.00** |    **0.02** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.098 ms | 1.1616 ms | 0.0637 ms |  1.02 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.620 ms | 0.8667 ms | 0.0475 ms |  1.67 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  6.834 ms | 0.0668 ms | 0.0037 ms |  0.98 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 114.8 | 25.2 | 4.55x C# faster |
| 32 | 175.2 | 91.6 | 1.91x C# faster |
| 128 | 470.6 | 951.8 | 2.02x Py faster |
| 512 | 4458.1 | 10658.3 | 2.39x Py faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 158.4 | 18.6 | 8.51x C# faster |
| 32 | 290.8 | 165.8 | 1.75x C# faster |
| 128 | 1717.4 | 1351.6 | 1.27x C# faster |
| 512 | 13979.4 | 14427.8 | 1.03x Py faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.961 | 104.14x | 0.009 | 0.961 | 104.09x |
| accuracy_n1000_k2 | 0.001 | 0.513 | 498.76x | 0.001 | 0.513 | 498.78x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.738 | 225.89x | 0.008 | 1.738 | 225.90x |
| classification_report_n1000_k2 | 0.010 | 6.635 | 665.37x | 0.010 | 6.635 | 665.33x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.917 | 121.91x | 0.016 | 1.917 | 121.91x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.041 | 136.77x | 0.008 | 1.041 | 136.78x |
| matthews_n1000_k2 | 0.007 | 1.957 | 267.19x | 0.007 | 1.957 | 267.19x |
| cohen_kappa_n1000_k2 | 0.007 | 1.084 | 147.88x | 0.007 | 1.084 | 147.88x |
| mse_n1000_k2 | 0.002 | 0.300 | 122.82x | 0.002 | 0.300 | 122.83x |
| mae_n1000_k2 | 0.002 | 0.300 | 123.70x | 0.002 | 0.300 | 123.70x |
| median_ae_n1000_k2 | 0.006 | 0.313 | 48.51x | 0.006 | 0.313 | 48.51x |
| r2_n1000_k2 | 0.003 | 0.365 | 130.50x | 0.003 | 0.365 | 130.50x |
| confusion_matrix_n1000_k10 | 0.009 | 0.981 | 103.24x | 0.009 | 0.981 | 103.24x |
| accuracy_n1000_k10 | 0.001 | 0.515 | 458.31x | 0.001 | 0.515 | 458.31x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.771 | 213.67x | 0.008 | 1.771 | 213.66x |
| classification_report_n1000_k10 | 0.015 | 6.844 | 463.39x | 0.015 | 6.844 | 463.39x |
| roc_auc_ovr_macro_n1000_k10 | 0.555 | 9.659 | 17.42x | 0.555 | 9.659 | 17.42x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.047 | 126.91x | 0.008 | 1.047 | 126.91x |
| matthews_n1000_k10 | 0.008 | 1.973 | 248.71x | 0.008 | 1.973 | 248.71x |
| cohen_kappa_n1000_k10 | 0.008 | 1.087 | 130.81x | 0.008 | 1.087 | 130.83x |
| mse_n1000_k10 | 0.002 | 0.299 | 123.64x | 0.002 | 0.299 | 123.64x |
| mae_n1000_k10 | 0.002 | 0.298 | 123.75x | 0.002 | 0.298 | 123.74x |
| median_ae_n1000_k10 | 0.006 | 0.308 | 47.65x | 0.006 | 0.308 | 47.65x |
| r2_n1000_k10 | 0.003 | 0.360 | 128.94x | 0.003 | 0.360 | 128.95x |
| confusion_matrix_n100000_k2 | 0.994 | 10.648 | 10.71x | 0.994 | 10.647 | 10.71x |
| accuracy_n100000_k2 | 0.186 | 3.727 | 20.08x | 0.186 | 3.727 | 20.08x |
| precision_recall_f1_macro_n100000_k2 | 0.845 | 12.211 | 14.44x | 0.845 | 12.211 | 14.44x |
| classification_report_n100000_k2 | 0.823 | 26.637 | 32.38x | 0.823 | 26.636 | 32.38x |
| roc_auc_binary_n100000_k2 | 3.728 | 26.271 | 7.05x | 3.727 | 26.268 | 7.05x |
| balanced_accuracy_n100000_k2 | 0.847 | 10.728 | 12.66x | 0.847 | 10.728 | 12.66x |
| matthews_n100000_k2 | 0.822 | 21.466 | 26.11x | 0.822 | 21.465 | 26.11x |
| cohen_kappa_n100000_k2 | 0.823 | 10.799 | 13.12x | 0.823 | 10.798 | 13.12x |
| mse_n100000_k2 | 0.237 | 0.439 | 1.85x | 0.237 | 0.439 | 1.85x |
| mae_n100000_k2 | 0.243 | 0.432 | 1.77x | 0.243 | 0.432 | 1.77x |
| median_ae_n100000_k2 | 0.762 | 1.782 | 2.34x | 0.783 | 1.782 | 2.27x |
| r2_n100000_k2 | 0.235 | 0.655 | 2.79x | 0.235 | 0.655 | 2.79x |
| confusion_matrix_n100000_k10 | 0.971 | 10.656 | 10.97x | 0.971 | 10.656 | 10.97x |
| accuracy_n100000_k10 | 0.271 | 3.741 | 13.78x | 0.271 | 3.741 | 13.78x |
| precision_recall_f1_macro_n100000_k10 | 0.951 | 12.865 | 13.53x | 0.951 | 12.865 | 13.53x |
| classification_report_n100000_k10 | 0.943 | 29.310 | 31.09x | 0.943 | 29.308 | 31.09x |
| roc_auc_ovr_macro_n100000_k10 | 39.119 | 215.537 | 5.51x | 39.119 | 215.507 | 5.51x |
| balanced_accuracy_n100000_k10 | 0.958 | 10.747 | 11.21x | 0.958 | 10.747 | 11.21x |
| matthews_n100000_k10 | 0.941 | 22.070 | 23.45x | 0.941 | 22.068 | 23.45x |
| cohen_kappa_n100000_k10 | 1.155 | 10.782 | 9.34x | 1.155 | 10.782 | 9.34x |
| mse_n100000_k10 | 0.238 | 0.442 | 1.86x | 0.238 | 0.442 | 1.86x |
| mae_n100000_k10 | 0.244 | 0.435 | 1.78x | 0.244 | 0.435 | 1.78x |
| median_ae_n100000_k10 | 0.838 | 1.780 | 2.12x | 0.898 | 1.780 | 1.98x |
| r2_n100000_k10 | 0.234 | 0.657 | 2.81x | 0.234 | 0.657 | 2.81x |
| confusion_matrix_n1000000_k2 | 8.583 | 99.777 | 11.63x | 8.583 | 99.759 | 11.62x |
| accuracy_n1000000_k2 | 1.953 | 32.829 | 16.81x | 1.953 | 32.819 | 16.81x |
| precision_recall_f1_macro_n1000000_k2 | 8.404 | 107.462 | 12.79x | 8.404 | 107.454 | 12.79x |
| classification_report_n1000000_k2 | 8.250 | 208.986 | 25.33x | 8.250 | 208.974 | 25.33x |
| roc_auc_binary_n1000000_k2 | 43.778 | 287.458 | 6.57x | 43.775 | 287.447 | 6.57x |
| balanced_accuracy_n1000000_k2 | 8.489 | 99.740 | 11.75x | 8.489 | 99.719 | 11.75x |
| matthews_n1000000_k2 | 8.274 | 200.316 | 24.21x | 8.274 | 200.313 | 24.21x |
| cohen_kappa_n1000000_k2 | 8.277 | 99.380 | 12.01x | 8.276 | 99.373 | 12.01x |
| mse_n1000000_k2 | 2.379 | 2.089 | 0.88x | 2.379 | 2.089 | 0.88x |
| mae_n1000000_k2 | 2.421 | 2.049 | 0.85x | 2.421 | 2.049 | 0.85x |
| median_ae_n1000000_k2 | 7.212 | 14.054 | 1.95x | 7.345 | 14.052 | 1.91x |
| r2_n1000000_k2 | 2.342 | 3.647 | 1.56x | 2.341 | 3.647 | 1.56x |
| confusion_matrix_n1000000_k10 | 9.734 | 99.376 | 10.21x | 9.734 | 99.371 | 10.21x |
| accuracy_n1000000_k10 | 2.788 | 32.836 | 11.78x | 2.788 | 32.827 | 11.77x |
| precision_recall_f1_macro_n1000000_k10 | 9.597 | 113.379 | 11.81x | 9.596 | 113.374 | 11.81x |
| classification_report_n1000000_k10 | 9.385 | 232.863 | 24.81x | 9.385 | 232.855 | 24.81x |
| balanced_accuracy_n1000000_k10 | 9.680 | 99.048 | 10.23x | 9.680 | 99.037 | 10.23x |
| matthews_n1000000_k10 | 9.396 | 204.927 | 21.81x | 9.395 | 204.925 | 21.81x |
| cohen_kappa_n1000000_k10 | 9.433 | 99.256 | 10.52x | 9.433 | 99.255 | 10.52x |
| mse_n1000000_k10 | 2.385 | 2.011 | 0.84x | 2.385 | 2.011 | 0.84x |
| mae_n1000000_k10 | 2.437 | 2.051 | 0.84x | 2.437 | 2.051 | 0.84x |
| median_ae_n1000000_k10 | 7.181 | 14.043 | 1.96x | 7.298 | 14.041 | 1.92x |
| r2_n1000000_k10 | 2.819 | 3.313 | 1.18x | 2.819 | 3.313 | 1.18x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.88x
  mae_n1000000_k2                  0.85x
  mse_n1000000_k10                 0.84x
  mae_n1000000_k10                 0.84x

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-20, measured at commit `bda170f7bcad59ca70ba7c0e184f3e589e40d91b`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.379 | 9.716 | 2.22x | 5.221 | 9.716 | 1.86x |
| tokenizer_json_wordpiece | 11.097 | 15.988 | 1.44x | 11.495 | 15.987 | 1.39x |
| tokenizer_json_unigram | 12.304 | 36.029 | 2.93x | 13.099 | 36.025 | 2.75x |
| spiece_model | 4.250 | 27.993 | 6.59x | 4.479 | 27.991 | 6.25x |
| tfidf_save | 1.784 | 2.466 | 1.38x | 1.872 | 2.466 | 1.32x |
| tfidf_load | 3.851 | 3.996 | 1.04x | 4.051 | 3.996 | 0.99x |
| embedding_index_save | 6.758 | 3.786 | 0.56x | 7.360 | 3.786 | 0.51x |
| embedding_index_load | 5.878 | 1.237 | 0.21x | 6.726 | 1.237 | 0.18x |
| embedding_index_load_memory | 4.173 | 1.237 | 0.30x | 4.698 | 1.237 | 0.26x |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.

<!-- markdownlint-enable MD060 -->
