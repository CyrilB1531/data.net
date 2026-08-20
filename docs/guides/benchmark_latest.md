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

_As of 2026-08-20, measured at commit `6cadb6a76b4e172c36be7cdb3f541d742e24373a`._

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
| **UnitLoop**           | **1**          |     **6.147 μs** |   **0.4325 μs** | **0.0237 μs** |  **1.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.426 μs |   1.1243 μs | 0.0616 μs |  1.05 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.512 μs |   0.3421 μs | 0.0187 μs |  1.06 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **8**          |   **111.178 μs** |   **1.2248 μs** | **0.0671 μs** |  **1.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    74.442 μs |  14.5132 μs | 0.7955 μs |  0.67 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    73.575 μs |   5.0715 μs | 0.2780 μs |  0.66 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **32**         |   **412.764 μs** |  **26.4858 μs** | **1.4518 μs** |  **1.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   257.827 μs |  59.9502 μs | 3.2861 μs |  0.62 |  26.8555 |  1.4648 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   250.795 μs |  24.6911 μs | 1.3534 μs |  0.61 |  25.8789 |  1.4648 |   427.8 KB |        0.91 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **128**        | **1,662.869 μs** | **140.3652 μs** | **7.6939 μs** |  **1.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        | 1,023.228 μs |  71.7800 μs | 3.9345 μs |  0.62 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   946.209 μs |  47.8914 μs | 2.6251 μs |  0.57 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-20, measured at commit `6cadb6a76b4e172c36be7cdb3f541d742e24373a`._

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
| Unigram | 607.0 ms | 49.79 ms | 2.73 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 559.4 ms | 46.95 ms | 2.57 ms |  0.92 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-20, measured at commit `6cadb6a76b4e172c36be7cdb3f541d742e24373a`._

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

| Method                    | Length | Mean     | Error    | StdDev  | Gen0   | Allocated |
|-------------------------- |------- |---------:|---------:|--------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    | **103.7 μs** |  **2.49 μs** | **0.14 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **221.4 μs** | **32.67 μs** | **1.79 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **483.9 μs** | **27.12 μs** | **1.49 μs** | **3.9063** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **987.9 μs** | **41.70 μs** | **2.29 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-20, measured at commit `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`._

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

_As of 2026-08-20, measured at commit `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`._

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

_As of 2026-08-20, measured at commit `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`._

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

_As of 2026-08-20, measured at commit `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`._

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

_As of 2026-08-20, measured at commit `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`._

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

_As of 2026-08-20, measured at commit `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`._

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

_As of 2026-08-20, measured at commit `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`._

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

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-20, measured at commit `6cadb6a76b4e172c36be7cdb3f541d742e24373a`._

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
| VocabTxt               |  4.408 ms | 2.4360 ms | 0.1335 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.636 ms | 2.3406 ms | 0.1283 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.475 ms | 0.9455 ms | 0.0518 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.898 ms | 4.2486 ms | 0.2329 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.907 ms | 0.2954 ms | 0.0162 ms |  31.2500 |  25.3906 |  25.3906 |   2.09 MB |
| TfidfLoad              |  4.436 ms | 0.5713 ms | 0.0313 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  6.866 ms | 0.8194 ms | 0.0449 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  6.026 ms | 1.6341 ms | 0.0896 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |

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

## Against rapidfuzz, in this same run

Both sides on this VM in these minutes, which is what makes the ratio readable where the absolutes are not.

- `indel`
- `levenshtein`
- `metrics`
- `persistence`

### compare-indel

_As of 2026-08-20, measured at commit `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`._

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

_As of 2026-08-20, measured at commit `6bde87c833c7f936cefed0881eabbccbb2cb2f1f`._

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
