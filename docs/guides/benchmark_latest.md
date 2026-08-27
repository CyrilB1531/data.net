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

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **UnitLoop**           | **1**          |     **5.762 μs** |  **0.4998 μs** | **0.0274 μs** |  **1.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     5.862 μs |  0.2816 μs | 0.0154 μs |  1.02 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     5.953 μs |  1.1615 μs | 0.0637 μs |  1.03 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **8**          |   **104.244 μs** |  **1.3103 μs** | **0.0718 μs** |  **1.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    67.069 μs |  2.3774 μs | 0.1303 μs |  0.64 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    65.684 μs |  2.0940 μs | 0.1148 μs |  0.63 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **32**         |   **385.679 μs** |  **9.9524 μs** | **0.5455 μs** |  **1.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   237.153 μs | 27.3526 μs | 1.4993 μs |  0.61 |  26.8555 |  1.7090 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   224.372 μs | 44.3849 μs | 2.4329 μs |  0.58 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **128**        | **1,474.941 μs** | **17.4570 μs** | **0.9569 μs** |  **1.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   922.552 μs | 30.2488 μs | 1.6580 μs |  0.63 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   856.390 μs | 39.9231 μs | 2.1883 μs |  0.58 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method | length | Mean          | Error        | StdDev     | Allocated |
|------- |------- |--------------:|-------------:|-----------:|----------:|
| **Latin**  | **1000**   |      **55.84 μs** |     **5.850 μs** |   **0.321 μs** |         **-** |
| Cjk    | 1000   |      63.61 μs |     0.579 μs |   0.032 μs |         - |
| **Latin**  | **10000**  |   **5,508.30 μs** |   **610.059 μs** |  **33.439 μs** |         **-** |
| Cjk    | 10000  |   6,967.70 μs |   377.859 μs |  20.712 μs |         - |
| **Latin**  | **65536**  | **228,427.27 μs** | **7,904.409 μs** | **433.267 μs** |         **-** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| Unigram | 594.3 ms |  6.59 ms | 0.36 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 570.9 ms | 32.10 ms | 1.76 ms |  0.96 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **BpeOnOnePathologicalToken** | **512**    |   **103.1 μs** | **12.26 μs** | **0.67 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **210.1 μs** | **12.75 μs** | **0.70 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **499.6 μs** | **34.15 μs** | **1.87 μs** | **3.9063** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,015.4 μs** | **63.85 μs** | **3.50 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method     | Alphabet | Mean      | Error     | StdDev   | Allocated |
|----------- |--------- |----------:|----------:|---------:|----------:|
| **DpGroup**    | **cjk**      |  **19.04 μs** |  **1.690 μs** | **0.093 μs** |         **-** |
| MyersGroup | cjk      | 171.84 μs |  0.489 μs | 0.027 μs |         - |
| **DpGroup**    | **latin**    |  **10.52 μs** |  **0.065 μs** | **0.004 μs** |         **-** |
| MyersGroup | latin    | 114.12 μs | 22.211 μs | 1.217 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| Ratio          |    108.6 ns |   1.07 ns |  0.06 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 13,469.7 ns | 252.92 ns | 13.86 ns | 123.98 |    0.12 |      - |         - |          NA |
| TokenSortRatio |    941.0 ns |  93.16 ns |  5.11 ns |   8.66 |    0.04 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,227.4 ns | 410.85 ns | 22.52 ns |  29.71 |    0.18 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,496.2 ns | 490.25 ns | 26.87 ns |  41.39 |    0.22 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Distance_Utf16**             | **8**      |      **29.43 ns** |       **0.294 ns** |     **0.016 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     132.54 ns |       3.118 ns |     0.171 ns |  4.50 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      30.06 ns |       1.463 ns |     0.080 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      28.91 ns |       0.785 ns |     0.043 ns |  0.98 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **31.63 ns** |       **3.247 ns** |     **0.178 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     139.51 ns |       1.259 ns |     0.069 ns |  4.41 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      33.76 ns |       0.177 ns |     0.010 ns |  1.07 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      32.21 ns |       0.943 ns |     0.052 ns |  1.02 |    0.01 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **34.86 ns** |       **1.354 ns** |     **0.074 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     147.95 ns |       6.170 ns |     0.338 ns |  4.24 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      36.36 ns |       1.323 ns |     0.073 ns |  1.04 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      32.31 ns |       0.650 ns |     0.036 ns |  0.93 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **39.04 ns** |       **0.853 ns** |     **0.047 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     188.03 ns |      14.826 ns |     0.813 ns |  4.82 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      38.32 ns |       0.319 ns |     0.017 ns |  0.98 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      34.56 ns |       2.110 ns |     0.116 ns |  0.89 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **58.18 ns** |       **5.415 ns** |     **0.297 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     852.69 ns |     252.558 ns |    13.844 ns | 14.66 |    0.22 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      60.75 ns |       1.469 ns |     0.081 ns |  1.04 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      61.33 ns |       4.978 ns |     0.273 ns |  1.05 |    0.01 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **69.01 ns** |       **1.429 ns** |     **0.078 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,030.47 ns |     202.566 ns |    11.103 ns | 14.93 |    0.14 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      75.85 ns |       3.178 ns |     0.174 ns |  1.10 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      66.12 ns |       3.105 ns |     0.170 ns |  0.96 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **868.23 ns** |       **3.086 ns** |     **0.169 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  17,840.47 ns |     291.377 ns |    15.971 ns | 20.55 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     874.09 ns |       3.676 ns |     0.201 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     874.70 ns |       4.497 ns |     0.246 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **8,060.50 ns** |      **67.091 ns** |     **3.677 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 307,439.52 ns | 109,944.936 ns | 6,026.453 ns | 38.14 |    0.65 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   8,133.36 ns |     723.181 ns |    39.640 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   8,116.36 ns |     137.228 ns |     7.522 ns |  1.01 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method     | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **8**    |    **130.10 ns** |     **1.332 ns** |   **0.073 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     58.92 ns |     1.086 ns |   0.060 ns |  0.45 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    130.19 ns |     3.683 ns |   0.202 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    107.32 ns |     4.332 ns |   0.237 ns |  0.82 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **231.72 ns** |   **239.874 ns** |  **13.148 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel     | 12   |     68.80 ns |     4.671 ns |   0.256 ns |  0.30 |    0.01 |         - |          NA |
| Dp_Cjk     | 12   |    221.20 ns |     4.267 ns |   0.234 ns |  0.96 |    0.05 |         - |          NA |
| Kernel_Cjk | 12   |    117.90 ns |     3.860 ns |   0.212 ns |  0.51 |    0.02 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **14**   |    **275.82 ns** |     **3.556 ns** |   **0.195 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 14   |     73.31 ns |     1.814 ns |   0.099 ns |  0.27 |    0.00 |         - |          NA |
| Dp_Cjk     | 14   |    305.57 ns |   404.022 ns |  22.146 ns |  1.11 |    0.07 |         - |          NA |
| Kernel_Cjk | 14   |    124.66 ns |     3.272 ns |   0.179 ns |  0.45 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **400.36 ns** |    **78.860 ns** |   **4.323 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 16   |     77.64 ns |     2.030 ns |   0.111 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    369.10 ns |   495.391 ns |  27.154 ns |  0.92 |    0.06 |         - |          NA |
| Kernel_Cjk | 16   |    131.18 ns |    45.416 ns |   2.489 ns |  0.33 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **18**   |    **718.57 ns** |   **291.538 ns** |  **15.980 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 18   |     84.33 ns |     2.013 ns |   0.110 ns |  0.12 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    725.55 ns |   427.335 ns |  23.424 ns |  1.01 |    0.03 |         - |          NA |
| Kernel_Cjk | 18   |    136.62 ns |     5.086 ns |   0.279 ns |  0.19 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **20**   |    **818.21 ns** |     **8.643 ns** |   **0.474 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 20   |     88.16 ns |     3.900 ns |   0.214 ns |  0.11 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    825.52 ns |    21.519 ns |   1.180 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 20   |    141.43 ns |     3.195 ns |   0.175 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **995.70 ns** |    **20.263 ns** |   **1.111 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 24   |     96.85 ns |     0.892 ns |   0.049 ns |  0.10 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    995.35 ns |    39.796 ns |   2.181 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 24   |    154.12 ns |     0.966 ns |   0.053 ns |  0.15 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,577.38 ns** |    **50.653 ns** |   **2.776 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    117.86 ns |     1.799 ns |   0.099 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,595.94 ns |   482.806 ns |  26.464 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 32   |    187.49 ns |     3.924 ns |   0.215 ns |  0.12 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,277.13 ns** |   **856.685 ns** |  **46.958 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 48   |    144.40 ns |     2.373 ns |   0.130 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,278.82 ns |   961.936 ns |  52.727 ns |  1.00 |    0.02 |         - |          NA |
| Kernel_Cjk | 48   |    261.14 ns |    11.160 ns |   0.612 ns |  0.08 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **5,256.35 ns** | **2,238.724 ns** | **122.712 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 64   |    171.50 ns |     2.435 ns |   0.133 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,383.11 ns |   964.200 ns |  52.851 ns |  1.02 |    0.02 |         - |          NA |
| Kernel_Cjk | 64   |    314.39 ns |    44.990 ns |   2.466 ns |  0.06 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **10,993.46 ns** | **9,055.212 ns** | **496.347 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel     | 96   |    738.90 ns |    52.545 ns |   2.880 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 11,318.38 ns | 3,704.579 ns | 203.060 ns |  1.03 |    0.04 |         - |          NA |
| Kernel_Cjk | 96   |  1,090.31 ns |    32.858 ns |   1.801 ns |  0.10 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method                     | Length | Mean         | Error      | StdDev    | Ratio | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-----------:|----------:|------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **27.52 ns** |   **0.114 ns** |  **0.006 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    125.07 ns |   0.586 ns |  0.032 ns |  4.55 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     28.12 ns |   0.656 ns |  0.036 ns |  1.02 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **64**     |    **312.38 ns** |  **13.702 ns** |  **0.751 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    757.50 ns |  90.473 ns |  4.959 ns |  2.42 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    313.51 ns |  28.638 ns |  1.570 ns |  1.00 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **512**    | **15,400.45 ns** | **144.129 ns** |  **7.900 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 18,427.47 ns | 456.752 ns | 25.036 ns |  1.20 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 15,510.60 ns |  62.237 ns |  3.411 ns |  1.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method             | Length | Distinct | Mean         | Error        | StdDev      | Ratio | Allocated | Alloc Ratio |
|------------------- |------- |--------- |-------------:|-------------:|------------:|------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |     **358.5 ns** |     **30.81 ns** |     **1.69 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     257.8 ns |     20.08 ns |     1.10 ns |  0.72 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **352.0 ns** |     **21.35 ns** |     **1.17 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     256.1 ns |      0.38 ns |     0.02 ns |  0.73 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **446.3 ns** |     **11.73 ns** |     **0.64 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     352.7 ns |     20.41 ns |     1.12 ns |  0.79 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **445.6 ns** |      **2.96 ns** |     **0.16 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     348.4 ns |      4.96 ns |     0.27 ns |  0.78 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **538.0 ns** |     **27.77 ns** |     **1.52 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     445.6 ns |      7.84 ns |     0.43 ns |  0.83 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **537.8 ns** |      **5.50 ns** |     **0.30 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     444.8 ns |      3.69 ns |     0.20 ns |  0.83 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **627.8 ns** |     **22.28 ns** |     **1.22 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,345.8 ns |     12.52 ns |     0.69 ns |  2.14 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **631.5 ns** |     **16.35 ns** |     **0.90 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,331.5 ns |      7.39 ns |     0.41 ns |  2.11 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,624.1 ns** |      **7.50 ns** |     **0.41 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,714.6 ns |     26.36 ns |     1.44 ns |  2.18 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,574.2 ns** |    **326.71 ns** |    **17.91 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,762.7 ns |    209.88 ns |    11.50 ns |  2.24 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **19,777.9 ns** |    **185.10 ns** |    **10.15 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  65,862.4 ns |  2,793.35 ns |   153.11 ns |  3.33 |         - |          NA |
|                    |        |          |              |              |             |       |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **446,929.8 ns** | **56,597.44 ns** | **3,102.30 ns** |  **1.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  71,111.7 ns |  2,576.49 ns |   141.23 ns |  0.16 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method         | Samples | Classes | Mean          | Error         | StdDev     | Gen0   | Allocated |
|--------------- |-------- |-------- |--------------:|--------------:|-----------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |      **7.095 μs** |     **5.8493 μs** |  **0.3206 μs** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7.185 μs |     0.9194 μs |  0.0504 μs | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |      1.030 μs |     0.0093 μs |  0.0005 μs |      - |         - |
| F1Macro        | 1000    | 2       |      7.130 μs |     0.3066 μs |  0.0168 μs | 0.0229 |     472 B |
| Report         | 1000    | 2       |      9.833 μs |     0.3122 μs |  0.0171 μs | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7.576 μs** |     **0.5966 μs** |  **0.0327 μs** | **0.0687** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7.287 μs |     0.2115 μs |  0.0116 μs | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |      1.032 μs |     0.0426 μs |  0.0023 μs |      - |         - |
| F1Macro        | 1000    | 10      |      7.737 μs |     0.4397 μs |  0.0241 μs | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     14.570 μs |     0.7803 μs |  0.0428 μs | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **797.538 μs** |    **64.1144 μs** |  **3.5143 μs** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    834.942 μs |   227.2187 μs | 12.4546 μs |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    142.996 μs |   119.4107 μs |  6.5453 μs |      - |         - |
| F1Macro        | 100000  | 2       |    796.810 μs |    41.6167 μs |  2.2812 μs |      - |     473 B |
| Report         | 100000  | 2       |    900.869 μs |    52.3567 μs |  2.8698 μs |      - |    6544 B |
| **Matrix**         | **100000**  | **10**      |    **974.593 μs** |    **18.9619 μs** |  **1.0394 μs** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    943.110 μs |   147.3727 μs |  8.0780 μs |      - |    1248 B |
| AccuracyScore  | 100000  | 10      |    233.056 μs |   417.3221 μs | 22.8748 μs |      - |         - |
| F1Macro        | 100000  | 10      |    971.798 μs |    14.4674 μs |  0.7930 μs |      - |    1665 B |
| Report         | 100000  | 10      |    952.680 μs |    44.2457 μs |  2.4253 μs |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,581.748 μs** |   **726.0817 μs** | **39.7990 μs** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,642.139 μs |   962.1497 μs | 52.7387 μs |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,932.130 μs |    12.9613 μs |  0.7105 μs |      - |         - |
| F1Macro        | 1000000 | 2       |  8,453.837 μs |   233.3103 μs | 12.7885 μs |      - |     484 B |
| Report         | 1000000 | 2       |  8,986.848 μs |   291.7913 μs | 15.9941 μs |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **10,441.496 μs** |   **594.7467 μs** | **32.6001 μs** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 10,063.510 μs | 1,483.4199 μs | 81.3112 μs |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  3,110.547 μs |    77.5317 μs |  4.2498 μs |      - |         - |
| F1Macro        | 1000000 | 10      | 10,132.870 μs |   875.3936 μs | 47.9833 μs |      - |    1676 B |
| Report         | 1000000 | 10      |  9,981.726 μs |   204.1232 μs | 11.1887 μs |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method     | Band | Mean         | Error      | StdDev    | Ratio | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-----------:|----------:|------:|----------:|------------:|
| **Dp**         | **4**    |     **81.78 ns** |   **1.845 ns** |  **0.101 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 4    |     81.01 ns |   1.083 ns |  0.059 ns |  0.99 |         - |          NA |
| Dp_Cjk     | 4    |     81.94 ns |   2.094 ns |  0.115 ns |  1.00 |         - |          NA |
| Kernel_Cjk | 4    |     82.82 ns |   0.443 ns |  0.024 ns |  1.01 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **6**    |    **117.18 ns** |   **3.937 ns** |  **0.216 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 6    |     87.38 ns |   0.980 ns |  0.054 ns |  0.75 |         - |          NA |
| Dp_Cjk     | 6    |    124.62 ns |  12.693 ns |  0.696 ns |  1.06 |         - |          NA |
| Kernel_Cjk | 6    |    147.74 ns |   1.601 ns |  0.088 ns |  1.26 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **8**    |    **157.35 ns** |   **5.569 ns** |  **0.305 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 8    |    100.06 ns |   0.691 ns |  0.038 ns |  0.64 |         - |          NA |
| Dp_Cjk     | 8    |    157.92 ns |   3.807 ns |  0.209 ns |  1.00 |         - |          NA |
| Kernel_Cjk | 8    |    196.39 ns |  26.646 ns |  1.461 ns |  1.25 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **10**   |    **204.07 ns** |  **37.288 ns** |  **2.044 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 10   |    110.08 ns |   1.136 ns |  0.062 ns |  0.54 |         - |          NA |
| Dp_Cjk     | 10   |    216.10 ns |   0.488 ns |  0.027 ns |  1.06 |         - |          NA |
| Kernel_Cjk | 10   |    169.70 ns |   4.199 ns |  0.230 ns |  0.83 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **12**   |    **270.13 ns** |  **31.187 ns** |  **1.709 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 12   |    118.31 ns |   5.541 ns |  0.304 ns |  0.44 |         - |          NA |
| Dp_Cjk     | 12   |    267.45 ns |  15.366 ns |  0.842 ns |  0.99 |         - |          NA |
| Kernel_Cjk | 12   |    178.86 ns |   7.544 ns |  0.414 ns |  0.66 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **16**   |    **433.18 ns** |  **14.454 ns** |  **0.792 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 16   |    139.14 ns |  10.085 ns |  0.553 ns |  0.32 |         - |          NA |
| Dp_Cjk     | 16   |    431.50 ns |   6.720 ns |  0.368 ns |  1.00 |         - |          NA |
| Kernel_Cjk | 16   |    198.34 ns |   0.850 ns |  0.047 ns |  0.46 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **24**   |    **863.01 ns** |  **24.164 ns** |  **1.325 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 24   |    180.19 ns |   2.073 ns |  0.114 ns |  0.21 |         - |          NA |
| Dp_Cjk     | 24   |    865.80 ns |  23.700 ns |  1.299 ns |  1.00 |         - |          NA |
| Kernel_Cjk | 24   |    245.68 ns |  19.229 ns |  1.054 ns |  0.28 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **32**   |  **1,480.35 ns** |   **9.306 ns** |  **0.510 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 32   |    233.69 ns |   5.915 ns |  0.324 ns |  0.16 |         - |          NA |
| Dp_Cjk     | 32   |  1,483.84 ns |  78.067 ns |  4.279 ns |  1.00 |         - |          NA |
| Kernel_Cjk | 32   |    294.69 ns |   2.142 ns |  0.117 ns |  0.20 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **48**   |  **3,243.26 ns** | **118.066 ns** |  **6.472 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 48   |    300.98 ns |   3.421 ns |  0.188 ns |  0.09 |         - |          NA |
| Dp_Cjk     | 48   |  3,265.45 ns | 693.897 ns | 38.035 ns |  1.01 |         - |          NA |
| Kernel_Cjk | 48   |    388.54 ns |   3.044 ns |  0.167 ns |  0.12 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **64**   |  **5,655.40 ns** |  **27.010 ns** |  **1.481 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 64   |    382.46 ns |   2.633 ns |  0.144 ns |  0.07 |         - |          NA |
| Dp_Cjk     | 64   |  5,657.95 ns | 610.936 ns | 33.487 ns |  1.00 |         - |          NA |
| Kernel_Cjk | 64   |    483.75 ns |   1.169 ns |  0.064 ns |  0.09 |         - |          NA |
|            |      |              |            |           |       |           |             |
| **Dp**         | **96**   | **12,540.86 ns** | **534.400 ns** | **29.292 ns** |  **1.00** |         **-** |          **NA** |
| Kernel     | 96   |  1,261.79 ns |   5.311 ns |  0.291 ns |  0.10 |         - |          NA |
| Dp_Cjk     | 96   | 12,533.05 ns | 439.813 ns | 24.108 ns |  1.00 |         - |          NA |
| Kernel_Cjk | 96   |  1,723.19 ns |  44.204 ns |  2.423 ns |  0.14 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| VocabTxt               |  4.309 ms | 2.3881 ms | 0.1309 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.236 ms | 2.5204 ms | 0.1381 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 11.128 ms | 0.9725 ms | 0.0533 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  4.084 ms | 2.4965 ms | 0.1368 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.978 ms | 0.1366 ms | 0.0075 ms |  27.3438 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              |  4.625 ms | 1.5118 ms | 0.0829 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  5.153 ms | 0.7840 ms | 0.0430 ms | 445.3125 | 445.3125 | 445.3125 |  39.64 MB |
| EmbeddingIndexLoad     |  5.114 ms | 3.8587 ms | 0.2115 ms | 500.0000 | 468.7500 | 437.5000 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Count**                | **200**       |  **7.550 ms** | **0.8547 ms** | **0.0469 ms** |  **1.00** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.409 ms | 0.2601 ms | 0.0143 ms |  0.85 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.442 ms | 0.9934 ms | 0.0545 ms |  0.99 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.451 ms | 0.2164 ms | 0.0119 ms |  0.85 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |           |          |           |             |
| **Count**                | **1000**      | **30.961 ms** | **2.8716 ms** | **0.1574 ms** |  **1.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 24.055 ms | 3.2810 ms | 0.1798 ms |  0.78 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 29.047 ms | 3.0191 ms | 0.1655 ms |  0.94 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 24.710 ms | 2.1486 ms | 0.1178 ms |  0.80 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Dot**    | **384**  |  **51.47 ns** | **2.328 ns** | **0.128 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.74 ns | 8.218 ns | 0.450 ns |  0.95 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  | **100.10 ns** | **1.911 ns** | **0.105 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  90.59 ns | 1.355 ns | 0.074 ns |  0.91 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **132.84 ns** | **1.428 ns** | **0.078 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 125.01 ns | 0.419 ns | 0.023 ns |  0.94 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Count**        | **200**       |  **3.001 ms** | **1.9437 ms** | **0.1065 ms** |  **1.00** |    **0.04** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.243 ms | 6.5386 ms | 0.3584 ms |  1.08 |    0.11 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.859 ms | 0.6201 ms | 0.0340 ms |  1.29 |    0.04 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  3.058 ms | 1.8891 ms | 0.1035 ms |  1.02 |    0.04 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.184 ms** | **1.9917 ms** | **0.1092 ms** |  **1.00** |    **0.02** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.315 ms | 0.9306 ms | 0.0510 ms |  1.02 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.861 ms | 1.7554 ms | 0.0962 ms |  1.65 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.049 ms | 1.0836 ms | 0.0594 ms |  0.98 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 113.3 | 23.6 | 4.79x C# faster |
| latin | 32 | 159.4 | 68.2 | 2.34x C# faster |
| latin | 128 | 478.2 | 850.2 | 1.78x Py faster |
| latin | 512 | 4862.9 | 8277.5 | 1.70x Py faster |
| cjk | 8 | 128.4 | 23.7 | 5.42x C# faster |
| cjk | 32 | 234.1 | 144.9 | 1.62x C# faster |
| cjk | 128 | 2047.9 | 1740.6 | 1.18x C# faster |
| cjk | 512 | 16797.4 | 11995.0 | 1.40x C# faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is Hyyro's bit-parallel LLCS above a pattern of 8 and a rolling-row dynamic program below it (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 137.6 | 18.0 | 7.63x C# faster |
| latin | 32 | 256.8 | 127.9 | 2.01x C# faster |
| latin | 128 | 1812.8 | 1557.1 | 1.16x C# faster |
| latin | 512 | 15595.6 | 16669.3 | 1.07x Py faster |
| cjk | 8 | 150.1 | 18.1 | 8.31x C# faster |
| cjk | 32 | 299.7 | 190.6 | 1.57x C# faster |
| cjk | 128 | 3028.6 | 2511.6 | 1.21x C# faster |
| cjk | 512 | 26191.9 | 20163.4 | 1.30x C# faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.773 | 86.83x | 0.009 | 0.773 | 86.83x |
| accuracy_n1000_k2 | 0.001 | 0.402 | 382.64x | 0.001 | 0.402 | 382.67x |
| precision_recall_f1_macro_n1000_k2 | 0.007 | 1.411 | 193.56x | 0.007 | 1.411 | 193.53x |
| classification_report_n1000_k2 | 0.010 | 5.328 | 545.78x | 0.010 | 5.327 | 545.77x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.562 | 99.29x | 0.016 | 1.562 | 99.29x |
| balanced_accuracy_n1000_k2 | 0.007 | 0.837 | 113.92x | 0.007 | 0.837 | 113.91x |
| matthews_n1000_k2 | 0.007 | 1.591 | 221.82x | 0.007 | 1.591 | 221.80x |
| cohen_kappa_n1000_k2 | 0.007 | 0.890 | 120.97x | 0.007 | 0.890 | 120.97x |
| mse_n1000_k2 | 0.003 | 0.219 | 80.72x | 0.003 | 0.219 | 80.71x |
| mae_n1000_k2 | 0.003 | 0.220 | 80.92x | 0.003 | 0.220 | 80.93x |
| median_ae_n1000_k2 | 0.006 | 0.236 | 39.92x | 0.006 | 0.235 | 39.92x |
| r2_n1000_k2 | 0.003 | 0.273 | 99.97x | 0.003 | 0.273 | 99.98x |
| confusion_matrix_n1000_k10 | 0.009 | 0.788 | 85.45x | 0.009 | 0.788 | 85.45x |
| accuracy_n1000_k10 | 0.001 | 0.413 | 390.91x | 0.001 | 0.413 | 390.83x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.453 | 182.67x | 0.008 | 1.453 | 182.67x |
| classification_report_n1000_k10 | 0.014 | 5.586 | 399.87x | 0.014 | 5.586 | 399.89x |
| roc_auc_ovr_macro_n1000_k10 | 0.458 | 8.239 | 17.99x | 0.458 | 8.237 | 17.99x |
| balanced_accuracy_n1000_k10 | 0.008 | 0.854 | 107.40x | 0.008 | 0.854 | 107.39x |
| matthews_n1000_k10 | 0.008 | 1.635 | 202.06x | 0.008 | 1.635 | 202.06x |
| cohen_kappa_n1000_k10 | 0.008 | 0.897 | 107.51x | 0.008 | 0.897 | 107.50x |
| mse_n1000_k10 | 0.003 | 0.220 | 80.51x | 0.003 | 0.220 | 80.49x |
| mae_n1000_k10 | 0.003 | 0.218 | 79.83x | 0.003 | 0.218 | 79.83x |
| median_ae_n1000_k10 | 0.006 | 0.237 | 40.34x | 0.006 | 0.237 | 40.34x |
| r2_n1000_k10 | 0.003 | 0.272 | 99.66x | 0.003 | 0.272 | 99.66x |
| confusion_matrix_n100000_k2 | 1.002 | 11.034 | 11.01x | 1.002 | 11.033 | 11.01x |
| accuracy_n100000_k2 | 0.106 | 3.795 | 35.84x | 0.106 | 3.794 | 35.83x |
| precision_recall_f1_macro_n100000_k2 | 0.859 | 12.616 | 14.69x | 0.859 | 12.615 | 14.69x |
| classification_report_n100000_k2 | 0.867 | 27.037 | 31.18x | 0.867 | 27.034 | 31.18x |
| roc_auc_binary_n100000_k2 | 2.933 | 28.848 | 9.84x | 2.933 | 28.844 | 9.83x |
| balanced_accuracy_n100000_k2 | 0.857 | 11.119 | 12.97x | 0.857 | 11.118 | 12.97x |
| matthews_n100000_k2 | 0.859 | 22.264 | 25.92x | 0.859 | 22.260 | 25.91x |
| cohen_kappa_n100000_k2 | 0.857 | 11.185 | 13.05x | 0.857 | 11.183 | 13.05x |
| mse_n100000_k2 | 0.268 | 0.374 | 1.39x | 0.268 | 0.374 | 1.39x |
| mae_n100000_k2 | 0.268 | 0.373 | 1.39x | 0.268 | 0.373 | 1.39x |
| median_ae_n100000_k2 | 0.676 | 1.893 | 2.80x | 0.693 | 1.893 | 2.73x |
| r2_n100000_k2 | 0.260 | 0.584 | 2.25x | 0.260 | 0.584 | 2.25x |
| confusion_matrix_n100000_k10 | 0.956 | 10.978 | 11.49x | 0.956 | 10.976 | 11.48x |
| accuracy_n100000_k10 | 0.106 | 3.795 | 35.85x | 0.106 | 3.794 | 35.84x |
| precision_recall_f1_macro_n100000_k10 | 1.016 | 13.315 | 13.10x | 1.016 | 13.314 | 13.10x |
| classification_report_n100000_k10 | 0.979 | 29.903 | 30.54x | 0.979 | 29.901 | 30.54x |
| roc_auc_ovr_macro_n100000_k10 | 31.134 | 235.252 | 7.56x | 31.140 | 235.244 | 7.55x |
| balanced_accuracy_n100000_k10 | 0.974 | 11.150 | 11.44x | 0.974 | 11.148 | 11.44x |
| matthews_n100000_k10 | 0.981 | 23.079 | 23.53x | 0.981 | 23.076 | 23.53x |
| cohen_kappa_n100000_k10 | 0.999 | 11.240 | 11.25x | 0.999 | 11.240 | 11.25x |
| mse_n100000_k10 | 0.267 | 0.379 | 1.42x | 0.267 | 0.378 | 1.42x |
| mae_n100000_k10 | 0.267 | 0.378 | 1.42x | 0.267 | 0.378 | 1.42x |
| median_ae_n100000_k10 | 0.674 | 1.892 | 2.81x | 0.713 | 1.891 | 2.65x |
| r2_n100000_k10 | 0.260 | 0.593 | 2.28x | 0.260 | 0.593 | 2.28x |
| confusion_matrix_n1000000_k2 | 8.627 | 103.104 | 11.95x | 8.626 | 103.099 | 11.95x |
| accuracy_n1000000_k2 | 2.296 | 34.332 | 14.95x | 2.296 | 34.329 | 14.95x |
| precision_recall_f1_macro_n1000000_k2 | 8.803 | 112.680 | 12.80x | 8.801 | 112.675 | 12.80x |
| classification_report_n1000000_k2 | 8.730 | 219.722 | 25.17x | 8.730 | 219.713 | 25.17x |
| roc_auc_binary_n1000000_k2 | 42.019 | 312.188 | 7.43x | 42.016 | 312.164 | 7.43x |
| balanced_accuracy_n1000000_k2 | 8.758 | 102.976 | 11.76x | 8.757 | 102.964 | 11.76x |
| matthews_n1000000_k2 | 8.748 | 206.897 | 23.65x | 8.747 | 206.894 | 23.65x |
| cohen_kappa_n1000000_k2 | 8.758 | 102.634 | 11.72x | 8.757 | 102.625 | 11.72x |
| mse_n1000000_k2 | 2.671 | 1.767 | 0.66x | 2.670 | 1.767 | 0.66x |
| mae_n1000000_k2 | 2.674 | 1.748 | 0.65x | 2.674 | 1.748 | 0.65x |
| median_ae_n1000000_k2 | 6.401 | 15.371 | 2.40x | 6.479 | 15.368 | 2.37x |
| r2_n1000000_k2 | 2.598 | 3.422 | 1.32x | 2.598 | 3.421 | 1.32x |
| confusion_matrix_n1000000_k10 | 9.945 | 102.266 | 10.28x | 9.944 | 102.236 | 10.28x |
| accuracy_n1000000_k10 | 3.313 | 34.128 | 10.30x | 3.313 | 34.119 | 10.30x |
| precision_recall_f1_macro_n1000000_k10 | 10.441 | 119.427 | 11.44x | 10.441 | 119.413 | 11.44x |
| classification_report_n1000000_k10 | 10.252 | 247.454 | 24.14x | 10.252 | 247.408 | 24.13x |
| balanced_accuracy_n1000000_k10 | 10.275 | 102.510 | 9.98x | 10.275 | 102.500 | 9.98x |
| matthews_n1000000_k10 | 10.251 | 214.608 | 20.93x | 10.249 | 214.592 | 20.94x |
| cohen_kappa_n1000000_k10 | 10.317 | 102.481 | 9.93x | 10.315 | 102.470 | 9.93x |
| mse_n1000000_k10 | 5.819 | 1.757 | 0.30x | 5.819 | 1.756 | 0.30x |
| mae_n1000000_k10 | 5.807 | 1.739 | 0.30x | 5.806 | 1.739 | 0.30x |
| median_ae_n1000000_k10 | 6.281 | 15.346 | 2.44x | 6.353 | 15.344 | 2.42x |
| r2_n1000000_k10 | 2.951 | 3.328 | 1.13x | 2.950 | 3.328 | 1.13x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.66x
  mae_n1000000_k2                  0.65x
  mse_n1000000_k10                 0.30x
  mae_n1000000_k10                 0.30x

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-26, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 5.210 | 9.855 | 1.89x | 5.506 | 9.855 | 1.79x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.215 | 17.157 | 1.40x | 12.900 | 17.156 | 1.33x | 706,526 | 706,526 |
| tokenizer_json_unigram | 11.075 | 36.994 | 3.34x | 11.360 | 36.989 | 3.26x | 1,990,038 | 1,990,038 |
| spiece_model | 4.696 | 29.936 | 6.38x | 4.891 | 29.935 | 6.12x | 533,084 | 533,084 |
| tfidf_save | 1.657 | 2.402 | 1.45x | 1.706 | 2.402 | 1.41x | 581,787 | 591,922 |
| tfidf_load | 4.623 | 4.201 | 0.91x | 4.819 | 4.201 | 0.87x | 581,787 | 591,922 |
| embedding_index_save | 4.660 | 1.342 | 0.29x | 5.214 | 1.341 | 0.26x | 20,589,007 | 15,360,128 |
| embedding_index_load | 4.621 | 1.366 | 0.30x | 5.243 | 1.366 | 0.26x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 5.735 | 0.875 | 0.15x | 6.452 | 0.874 | 0.14x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 3.250 | 1.326 | 0.41x | 3.640 | 1.326 | 0.36x | 20,589,007 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 81.35x | 0.000 | 0.001 | 81.35x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 454.638 | 639.341 | 1.41x | 455.844 | 639.266 | 1.40x | 15,251,458 | 14,022,374 |
| embedding_index_load_gzip | 80.959 | 72.429 | 0.89x | 81.735 | 72.412 | 0.89x | 15,251,458 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
