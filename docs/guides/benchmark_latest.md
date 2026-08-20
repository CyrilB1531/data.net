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

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **UnitLoop**           | **1**          |     **6.323 μs** |   **0.2313 μs** | **0.0127 μs** |  **1.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.236 μs |   1.0506 μs | 0.0576 μs |  0.99 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.452 μs |   0.7766 μs | 0.0426 μs |  1.02 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **8**          |   **111.773 μs** |   **0.5279 μs** | **0.0289 μs** |  **1.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    70.883 μs |   7.2251 μs | 0.3960 μs |  0.63 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    71.845 μs |   3.1481 μs | 0.1726 μs |  0.64 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **32**         |   **403.618 μs** |  **17.6675 μs** | **0.9684 μs** |  **1.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   253.712 μs |   9.3942 μs | 0.5149 μs |  0.63 |  26.8555 |  1.4648 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   250.259 μs |  30.0168 μs | 1.6453 μs |  0.62 |  25.8789 |  1.4648 |   427.8 KB |        0.91 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **128**        | **1,627.888 μs** | **137.4659 μs** | **7.5350 μs** |  **1.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   996.785 μs |  45.2384 μs | 2.4797 μs |  0.61 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   979.491 μs |  38.7742 μs | 2.1253 μs |  0.60 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| Unigram | 591.9 ms | 11.34 ms | 0.62 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 546.2 ms | 13.74 ms | 0.75 ms |  0.92 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **BpeOnOnePathologicalToken** | **512**    | **104.7 μs** |  **3.36 μs** | **0.18 μs** | **1.2207** |      **-** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **223.5 μs** |  **6.32 μs** | **0.35 μs** | **2.4414** |      **-** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **486.1 μs** | **36.88 μs** | **2.02 μs** | **4.3945** |      **-** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **975.5 μs** | **43.55 μs** | **2.39 μs** | **8.7891** | **0.9766** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| DpGroup    |  12.87 μs | 2.291 μs | 0.126 μs |         - |
| MyersGroup | 134.17 μs | 9.308 μs | 0.510 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| Ratio          |     96.12 ns |   3.164 ns |  0.173 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 18,491.01 ns | 778.987 ns | 42.699 ns | 192.38 |    0.49 |      - |         - |          NA |
| TokenSortRatio |  1,073.11 ns |  65.618 ns |  3.597 ns |  11.16 |    0.04 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,444.78 ns | 731.900 ns | 40.118 ns |  35.84 |    0.37 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,830.04 ns | 716.543 ns | 39.276 ns |  50.25 |    0.36 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **Distance_Utf16**             | **8**      |      **26.73 ns** |      **0.678 ns** |     **0.037 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     129.68 ns |     14.246 ns |     0.781 ns |  4.85 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      27.08 ns |      1.597 ns |     0.088 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.79 ns |      0.232 ns |     0.013 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.35 ns** |      **0.824 ns** |     **0.045 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     148.35 ns |     18.904 ns |     1.036 ns |  5.23 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.81 ns |      7.650 ns |     0.419 ns |  1.02 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.00 ns |      0.718 ns |     0.039 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **31.47 ns** |      **4.219 ns** |     **0.231 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     157.80 ns |     16.584 ns |     0.909 ns |  5.01 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      32.17 ns |      0.370 ns |     0.020 ns |  1.02 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      31.18 ns |      2.093 ns |     0.115 ns |  0.99 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **87.90 ns** |      **2.283 ns** |     **0.125 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     191.74 ns |     12.059 ns |     0.661 ns |  2.18 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      89.05 ns |      3.997 ns |     0.219 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      89.83 ns |      2.789 ns |     0.153 ns |  1.02 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **56.33 ns** |      **6.340 ns** |     **0.347 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     662.99 ns |     30.602 ns |     1.677 ns | 11.77 |    0.07 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      55.02 ns |      2.904 ns |     0.159 ns |  0.98 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      62.51 ns |      8.120 ns |     0.445 ns |  1.11 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **60.50 ns** |      **0.518 ns** |     **0.028 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,029.84 ns |    152.669 ns |     8.368 ns | 17.02 |    0.12 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      63.46 ns |      4.119 ns |     0.226 ns |  1.05 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      58.90 ns |      0.715 ns |     0.039 ns |  0.97 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,294.23 ns** |     **15.687 ns** |     **0.860 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  22,176.97 ns | 41,217.149 ns | 2,259.251 ns | 17.14 |    1.51 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,405.18 ns |     20.137 ns |     1.104 ns |  1.09 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,388.01 ns |      3.107 ns |     0.170 ns |  1.07 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |  **11,412.08 ns** |    **287.294 ns** |    **15.748 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 339,380.63 ns |  8,021.241 ns |   439.671 ns | 29.74 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |  11,800.27 ns |    237.611 ns |    13.024 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |  11,590.10 ns |     85.483 ns |     4.686 ns |  1.02 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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

| Method | Band | Mean         | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |-------------:|-------------:|----------:|------:|--------:|----------:|------------:|
| **Dp**     | **8**    |    **133.08 ns** |    **13.041 ns** |  **0.715 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 8    |     56.17 ns |     1.204 ns |  0.066 ns |  0.42 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **12**   |    **227.63 ns** |   **227.306 ns** | **12.459 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel | 12   |     61.98 ns |     0.535 ns |  0.029 ns |  0.27 |    0.01 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **14**   |    **280.85 ns** |   **290.563 ns** | **15.927 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel | 14   |     64.94 ns |    10.712 ns |  0.587 ns |  0.23 |    0.01 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **16**   |    **352.27 ns** |    **93.608 ns** |  **5.131 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 16   |     69.15 ns |     1.569 ns |  0.086 ns |  0.20 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **18**   |    **443.98 ns** |    **65.924 ns** |  **3.614 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 18   |     72.81 ns |    16.451 ns |  0.902 ns |  0.16 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **20**   |    **763.98 ns** |   **170.313 ns** |  **9.335 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 20   |     78.16 ns |    17.584 ns |  0.964 ns |  0.10 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **24**   |    **961.65 ns** |   **180.182 ns** |  **9.876 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 24   |     87.11 ns |     1.284 ns |  0.070 ns |  0.09 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **32**   |  **1,572.96 ns** |   **286.888 ns** | **15.725 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 32   |    104.29 ns |     1.814 ns |  0.099 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **48**   |  **3,204.88 ns** |   **260.716 ns** | **14.291 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |    132.67 ns |     6.441 ns |  0.353 ns |  0.04 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **64**   |  **5,466.61 ns** |   **192.477 ns** | **10.550 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 64   |    160.33 ns |     6.932 ns |  0.380 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **96**   | **13,918.28 ns** | **1,343.366 ns** | **73.634 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 96   |  1,109.45 ns |    31.159 ns |  1.708 ns |  0.08 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **Distance_Utf16**             | **8**      |     **25.30 ns** |   **0.438 ns** |  **0.024 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    118.82 ns |   5.765 ns |  0.316 ns |  4.70 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.29 ns |   0.327 ns |  0.018 ns |  1.00 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **64**     |    **265.78 ns** |   **2.209 ns** |  **0.121 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    677.16 ns |  40.580 ns |  2.224 ns |  2.55 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    266.59 ns |   2.677 ns |  0.147 ns |  1.00 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **512**    | **14,251.24 ns** | **169.930 ns** |  **9.314 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 16,523.69 ns | 317.640 ns | 17.411 ns |  1.16 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,438.23 ns | 962.333 ns | 52.749 ns |  1.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **Distance_CodePoint** | **16**     | **32**       |       **331.7 ns** |      **2.89 ns** |     **0.16 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,344.5 ns |     45.41 ns |     2.49 ns |   4.05 |    0.01 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **337.3 ns** |     **17.15 ns** |     **0.94 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,491.3 ns |     18.52 ns |     1.02 ns |   4.42 |    0.01 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **430.2 ns** |     **32.32 ns** |     **1.77 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,513.1 ns |    130.46 ns |     7.15 ns |   8.17 |    0.03 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **417.8 ns** |      **4.41 ns** |     **0.24 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     4,036.0 ns |    177.65 ns |     9.74 ns |   9.66 |    0.02 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **519.5 ns** |     **60.35 ns** |     **3.31 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,947.1 ns |    152.64 ns |     8.37 ns |  13.37 |    0.07 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **506.4 ns** |     **19.76 ns** |     **1.08 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,647.9 ns |    839.05 ns |    45.99 ns |  13.13 |    0.08 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **597.4 ns** |     **14.92 ns** |     **0.82 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |     9,877.9 ns |  3,474.62 ns |   190.46 ns |  16.53 |    0.28 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **602.7 ns** |      **8.77 ns** |     **0.48 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |    10,839.7 ns |  1,331.43 ns |    72.98 ns |  17.99 |    0.11 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,326.3 ns** |    **351.53 ns** |    **19.27 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   138,635.6 ns |  5,373.01 ns |   294.51 ns |  59.60 |    0.44 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,338.3 ns** |     **39.41 ns** |     **2.16 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   102,594.3 ns | 21,768.25 ns | 1,193.19 ns |  43.88 |    0.44 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **17,884.8 ns** |    **314.34 ns** |    **17.23 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,362,234.3 ns | 20,634.13 ns | 1,131.03 ns | 132.08 |    0.12 |         - |          NA |
|                    |        |          |                |              |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **427,977.0 ns** | **50,074.55 ns** | **2,744.76 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,774,854.1 ns | 14,876.20 ns |   815.41 ns |   4.15 |    0.02 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **Matrix**         | **1000**    | **2**       |     **7,805.4 ns** |       **355.41 ns** |     **19.48 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     7,191.7 ns |       142.88 ns |      7.83 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |       921.0 ns |        26.54 ns |      1.45 ns |      - |         - |
| F1Macro        | 1000    | 2       |     7,638.9 ns |       540.01 ns |     29.60 ns | 0.0153 |     472 B |
| Report         | 1000    | 2       |    10,871.3 ns |     2,905.72 ns |    159.27 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **7,886.8 ns** |       **945.29 ns** |     **51.81 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     7,493.1 ns |       117.68 ns |      6.45 ns | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |       918.7 ns |        24.97 ns |      1.37 ns |      - |         - |
| F1Macro        | 1000    | 10      |     8,066.8 ns |       403.99 ns |     22.14 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |    15,579.9 ns |     3,045.75 ns |    166.95 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **873,748.0 ns** |   **183,124.81 ns** | **10,037.69 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   828,270.5 ns |    28,685.16 ns |  1,572.33 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   163,520.3 ns |     4,952.32 ns |    271.45 ns |      - |         - |
| F1Macro        | 100000  | 2       |   866,200.1 ns |    23,908.85 ns |  1,310.52 ns |      - |     473 B |
| Report         | 100000  | 2       |   872,946.7 ns |    53,363.30 ns |  2,925.02 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **979,970.8 ns** |    **18,615.58 ns** |  **1,020.38 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   941,622.4 ns |   159,571.29 ns |  8,746.64 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   273,308.2 ns |     2,631.35 ns |    144.23 ns |      - |         - |
| F1Macro        | 100000  | 10      |   982,164.0 ns |    19,871.60 ns |  1,089.23 ns |      - |    1665 B |
| Report         | 100000  | 10      |   991,485.6 ns |    61,239.42 ns |  3,356.74 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **8,650,392.2 ns** |   **104,184.14 ns** |  **5,710.68 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       | 8,292,924.8 ns |   694,332.86 ns | 38,058.73 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       | 1,745,566.9 ns |    15,737.75 ns |    862.64 ns |      - |         - |
| F1Macro        | 1000000 | 2       | 8,488,402.0 ns | 1,416,584.63 ns | 77,647.78 ns |      - |     484 B |
| Report         | 1000000 | 2       | 8,644,758.9 ns |   123,894.61 ns |  6,791.08 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **9,820,834.3 ns** |    **67,309.86 ns** |  **3,689.48 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 9,257,525.8 ns |   274,673.47 ns | 15,055.78 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      | 2,768,566.3 ns |    69,312.22 ns |  3,799.24 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 9,778,711.2 ns | 1,388,825.78 ns | 76,126.23 ns |      - |    1676 B |
| Report         | 1000000 | 10      | 9,846,626.4 ns |    82,238.16 ns |  4,507.75 ns |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **Dp**     | **4**    |     **73.31 ns** |     **1.366 ns** |   **0.075 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 4    |     73.69 ns |     1.778 ns |   0.097 ns |  1.01 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **6**    |    **103.71 ns** |     **1.991 ns** |   **0.109 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 6    |    104.01 ns |     1.844 ns |   0.101 ns |  1.00 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **8**    |    **155.00 ns** |     **5.902 ns** |   **0.324 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |     87.73 ns |    12.807 ns |   0.702 ns |  0.57 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **10**   |    **207.33 ns** |   **182.048 ns** |   **9.979 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel | 10   |     97.01 ns |     2.387 ns |   0.131 ns |  0.47 |    0.02 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **12**   |    **252.16 ns** |   **234.322 ns** |  **12.844 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel | 12   |    103.53 ns |     4.068 ns |   0.223 ns |  0.41 |    0.02 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **16**   |    **411.22 ns** |    **36.725 ns** |   **2.013 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 16   |    122.12 ns |    13.506 ns |   0.740 ns |  0.30 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **24**   |    **847.13 ns** |    **97.504 ns** |   **5.345 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 24   |    155.27 ns |     0.070 ns |   0.004 ns |  0.18 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **32**   |  **1,444.35 ns** |    **13.527 ns** |   **0.741 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    190.86 ns |     2.365 ns |   0.130 ns |  0.13 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **48**   |  **3,295.01 ns** |   **255.231 ns** |  **13.990 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |    261.96 ns |     4.592 ns |   0.252 ns |  0.08 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **64**   |  **6,322.28 ns** | **1,326.833 ns** |  **72.728 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 64   |    330.64 ns |     1.978 ns |   0.108 ns |  0.05 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **96**   | **14,326.29 ns** | **6,952.753 ns** | **381.104 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 96   |  1,096.69 ns |   282.789 ns |  15.501 ns |  0.08 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| VocabTxt               |  4.438 ms | 2.4610 ms | 0.1349 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.614 ms | 4.5159 ms | 0.2475 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.469 ms | 0.6261 ms | 0.0343 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.955 ms | 3.9324 ms | 0.2155 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.852 ms | 0.2845 ms | 0.0156 ms |  27.3438 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              |  4.441 ms | 1.3868 ms | 0.0760 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  6.880 ms | 0.7324 ms | 0.0401 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  6.029 ms | 0.8890 ms | 0.0487 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **Count**                | **200**       |  **7.529 ms** | **1.6519 ms** | **0.0905 ms** |  **1.00** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.293 ms | 0.5739 ms | 0.0315 ms |  0.84 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.361 ms | 0.2672 ms | 0.0146 ms |  0.98 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.429 ms | 0.4058 ms | 0.0222 ms |  0.85 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |           |          |           |             |
| **Count**                | **1000**      | **30.631 ms** | **6.0696 ms** | **0.3327 ms** |  **1.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 24.671 ms | 0.7855 ms | 0.0431 ms |  0.81 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 29.081 ms | 1.9117 ms | 0.1048 ms |  0.95 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 25.044 ms | 1.5254 ms | 0.0836 ms |  0.82 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **Dot**    | **384**  |  **47.98 ns** | **0.769 ns** | **0.042 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.57 ns | 0.272 ns | 0.015 ns |  1.01 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  |  **94.69 ns** | **1.586 ns** | **0.087 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  92.76 ns | 3.642 ns | 0.200 ns |  0.98 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **123.98 ns** | **3.653 ns** | **0.200 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 122.67 ns | 1.909 ns | 0.105 ns |  0.99 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

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
| **Count**        | **200**       |  **3.243 ms** | **4.6509 ms** | **0.2549 ms** |  **1.00** |    **0.10** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.178 ms | 6.0860 ms | 0.3336 ms |  0.98 |    0.11 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.818 ms | 0.1463 ms | 0.0080 ms |  1.18 |    0.08 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.905 ms | 0.8131 ms | 0.0446 ms |  0.90 |    0.07 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.195 ms** | **1.2123 ms** | **0.0664 ms** |  **1.00** |    **0.01** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.478 ms | 0.6631 ms | 0.0363 ms |  1.04 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.969 ms | 1.8187 ms | 0.0997 ms |  1.66 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.065 ms | 0.7149 ms | 0.0392 ms |  0.98 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.13)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 117.0 | 26.2 | 4.46x C# faster |
| 32 | 180.7 | 91.4 | 1.98x C# faster |
| 128 | 483.1 | 958.7 | 1.98x Py faster |
| 512 | 4471.5 | 10816.7 | 2.42x Py faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.13)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 154.5 | 18.1 | 8.55x C# faster |
| 32 | 284.6 | 167.5 | 1.70x C# faster |
| 128 | 1685.8 | 1324.1 | 1.27x C# faster |
| 512 | 13999.5 | 14465.1 | 1.03x Py faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.13)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.993 | 105.68x | 0.009 | 0.993 | 105.63x |
| accuracy_n1000_k2 | 0.001 | 0.534 | 571.54x | 0.001 | 0.534 | 571.51x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.760 | 224.82x | 0.008 | 1.760 | 224.83x |
| classification_report_n1000_k2 | 0.011 | 6.740 | 620.44x | 0.011 | 6.739 | 620.32x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.943 | 123.83x | 0.016 | 1.943 | 123.82x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.076 | 139.00x | 0.008 | 1.076 | 139.02x |
| matthews_n1000_k2 | 0.007 | 2.016 | 269.46x | 0.007 | 2.016 | 269.47x |
| cohen_kappa_n1000_k2 | 0.007 | 1.131 | 150.87x | 0.007 | 1.131 | 150.88x |
| mse_n1000_k2 | 0.002 | 0.317 | 131.20x | 0.002 | 0.317 | 131.21x |
| mae_n1000_k2 | 0.002 | 0.316 | 130.53x | 0.002 | 0.316 | 130.53x |
| median_ae_n1000_k2 | 0.007 | 0.329 | 49.18x | 0.007 | 0.329 | 49.19x |
| r2_n1000_k2 | 0.002 | 0.379 | 152.21x | 0.002 | 0.379 | 152.19x |
| confusion_matrix_n1000_k10 | 0.009 | 1.012 | 106.63x | 0.009 | 1.012 | 106.63x |
| accuracy_n1000_k10 | 0.001 | 0.545 | 583.35x | 0.001 | 0.545 | 583.36x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.792 | 213.50x | 0.008 | 1.788 | 212.97x |
| classification_report_n1000_k10 | 0.016 | 7.011 | 443.27x | 0.016 | 7.011 | 443.34x |
| roc_auc_ovr_macro_n1000_k10 | 0.545 | 9.916 | 18.18x | 0.545 | 9.915 | 18.18x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.073 | 128.83x | 0.008 | 1.073 | 128.83x |
| matthews_n1000_k10 | 0.008 | 2.035 | 253.63x | 0.008 | 2.035 | 253.64x |
| cohen_kappa_n1000_k10 | 0.008 | 1.118 | 133.02x | 0.008 | 1.118 | 133.03x |
| mse_n1000_k10 | 0.002 | 0.312 | 128.97x | 0.002 | 0.312 | 128.97x |
| mae_n1000_k10 | 0.002 | 0.313 | 129.07x | 0.002 | 0.313 | 129.06x |
| median_ae_n1000_k10 | 0.007 | 0.324 | 48.23x | 0.007 | 0.324 | 48.23x |
| r2_n1000_k10 | 0.002 | 0.378 | 151.81x | 0.002 | 0.378 | 151.82x |
| confusion_matrix_n100000_k2 | 1.002 | 10.733 | 10.72x | 1.001 | 10.733 | 10.72x |
| accuracy_n100000_k2 | 0.164 | 3.759 | 22.92x | 0.164 | 3.759 | 22.92x |
| precision_recall_f1_macro_n100000_k2 | 0.854 | 12.283 | 14.39x | 0.854 | 12.282 | 14.38x |
| classification_report_n100000_k2 | 0.866 | 26.883 | 31.04x | 0.866 | 26.880 | 31.04x |
| roc_auc_binary_n100000_k2 | 3.894 | 26.965 | 6.92x | 3.894 | 26.964 | 6.92x |
| balanced_accuracy_n100000_k2 | 0.858 | 10.781 | 12.57x | 0.858 | 10.781 | 12.57x |
| matthews_n100000_k2 | 0.838 | 21.553 | 25.72x | 0.838 | 21.552 | 25.72x |
| cohen_kappa_n100000_k2 | 0.839 | 10.846 | 12.92x | 0.839 | 10.845 | 12.92x |
| mse_n100000_k2 | 0.242 | 0.467 | 1.93x | 0.242 | 0.467 | 1.93x |
| mae_n100000_k2 | 0.238 | 0.458 | 1.93x | 0.238 | 0.458 | 1.93x |
| median_ae_n100000_k2 | 0.784 | 1.797 | 2.29x | 0.806 | 1.797 | 2.23x |
| r2_n100000_k2 | 0.235 | 0.711 | 3.02x | 0.235 | 0.711 | 3.02x |
| confusion_matrix_n100000_k10 | 0.941 | 10.746 | 11.42x | 0.941 | 10.743 | 11.42x |
| accuracy_n100000_k10 | 0.276 | 3.767 | 13.67x | 0.276 | 3.767 | 13.67x |
| precision_recall_f1_macro_n100000_k10 | 0.957 | 12.957 | 13.54x | 0.957 | 12.956 | 13.54x |
| classification_report_n100000_k10 | 0.970 | 29.565 | 30.49x | 0.970 | 29.563 | 30.49x |
| roc_auc_ovr_macro_n100000_k10 | 36.458 | 219.479 | 6.02x | 36.457 | 219.460 | 6.02x |
| balanced_accuracy_n100000_k10 | 0.968 | 10.847 | 11.20x | 0.968 | 10.847 | 11.20x |
| matthews_n100000_k10 | 0.942 | 22.494 | 23.88x | 0.942 | 22.493 | 23.88x |
| cohen_kappa_n100000_k10 | 1.160 | 10.869 | 9.37x | 1.160 | 10.868 | 9.37x |
| mse_n100000_k10 | 0.242 | 0.468 | 1.93x | 0.242 | 0.468 | 1.93x |
| mae_n100000_k10 | 0.238 | 0.455 | 1.91x | 0.238 | 0.455 | 1.91x |
| median_ae_n100000_k10 | 0.810 | 1.800 | 2.22x | 0.864 | 1.800 | 2.08x |
| r2_n100000_k10 | 0.236 | 0.662 | 2.81x | 0.236 | 0.662 | 2.81x |
| confusion_matrix_n1000000_k2 | 8.454 | 101.835 | 12.05x | 8.454 | 101.810 | 12.04x |
| accuracy_n1000000_k2 | 1.829 | 33.808 | 18.48x | 1.829 | 33.803 | 18.48x |
| precision_recall_f1_macro_n1000000_k2 | 8.629 | 109.125 | 12.65x | 8.628 | 109.108 | 12.65x |
| classification_report_n1000000_k2 | 8.646 | 213.302 | 24.67x | 8.645 | 213.281 | 24.67x |
| roc_auc_binary_n1000000_k2 | 55.291 | 294.713 | 5.33x | 55.285 | 294.655 | 5.33x |
| balanced_accuracy_n1000000_k2 | 8.706 | 101.199 | 11.62x | 8.704 | 101.180 | 11.62x |
| matthews_n1000000_k2 | 8.453 | 202.799 | 23.99x | 8.451 | 202.776 | 23.99x |
| cohen_kappa_n1000000_k2 | 8.460 | 101.376 | 11.98x | 8.459 | 101.363 | 11.98x |
| mse_n1000000_k2 | 2.420 | 2.572 | 1.06x | 2.420 | 2.572 | 1.06x |
| mae_n1000000_k2 | 2.400 | 2.583 | 1.08x | 2.400 | 2.583 | 1.08x |
| median_ae_n1000000_k2 | 7.199 | 14.912 | 2.07x | 7.270 | 14.909 | 2.05x |
| r2_n1000000_k2 | 2.423 | 5.116 | 2.11x | 2.423 | 5.115 | 2.11x |
| confusion_matrix_n1000000_k10 | 9.449 | 101.779 | 10.77x | 9.448 | 101.735 | 10.77x |
| accuracy_n1000000_k10 | 2.755 | 34.039 | 12.36x | 2.755 | 34.038 | 12.36x |
| precision_recall_f1_macro_n1000000_k10 | 9.639 | 116.969 | 12.13x | 9.639 | 116.957 | 12.13x |
| classification_report_n1000000_k10 | 9.714 | 241.324 | 24.84x | 9.713 | 241.292 | 24.84x |
| balanced_accuracy_n1000000_k10 | 9.631 | 102.976 | 10.69x | 9.630 | 102.962 | 10.69x |
| matthews_n1000000_k10 | 9.455 | 213.243 | 22.55x | 9.454 | 213.230 | 22.55x |
| cohen_kappa_n1000000_k10 | 9.462 | 102.936 | 10.88x | 9.461 | 102.921 | 10.88x |
| mse_n1000000_k10 | 2.406 | 2.848 | 1.18x | 2.405 | 2.847 | 1.18x |
| mae_n1000000_k10 | 2.381 | 2.877 | 1.21x | 2.381 | 2.876 | 1.21x |
| median_ae_n1000000_k10 | 7.033 | 15.125 | 2.15x | 7.090 | 15.123 | 2.13x |
| r2_n1000000_k10 | 2.849 | 5.432 | 1.91x | 2.849 | 5.431 | 1.91x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-20, measured at commit `abfaf002802c5e98262d1a1d5072a1f6bfa644b0`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.13)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.424 | 10.675 | 2.41x | 4.721 | 10.674 | 2.26x |
| tokenizer_json_wordpiece | 12.140 | 17.650 | 1.45x | 12.495 | 17.648 | 1.41x |
| tokenizer_json_unigram | 12.702 | 41.925 | 3.30x | 13.247 | 41.923 | 3.16x |
| spiece_model | 4.660 | 29.713 | 6.38x | 4.911 | 29.710 | 6.05x |
| tfidf_save | 1.999 | 2.483 | 1.24x | 2.139 | 2.482 | 1.16x |
| tfidf_load | 4.211 | 4.444 | 1.06x | 4.454 | 4.443 | 1.00x |
| embedding_index_save | 7.216 | 4.686 | 0.65x | 7.814 | 4.685 | 0.60x |
| embedding_index_load | 6.634 | 1.693 | 0.26x | 7.589 | 1.693 | 0.22x |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.

<!-- markdownlint-enable MD060 -->
