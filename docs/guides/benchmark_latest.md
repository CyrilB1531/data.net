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

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method             | CorpusSize | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|----------:|------:|--------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **5.811 μs** |   **0.1979 μs** | **0.0108 μs** |  **1.00** |    **0.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.053 μs |   0.6646 μs | 0.0364 μs |  1.04 |    0.01 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.124 μs |   2.2856 μs | 0.1253 μs |  1.05 |    0.02 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **8**          |   **108.987 μs** |   **1.6801 μs** | **0.0921 μs** |  **1.00** |    **0.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    71.320 μs |   8.9215 μs | 0.4890 μs |  0.65 |    0.00 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    71.229 μs |   3.4918 μs | 0.1914 μs |  0.65 |    0.00 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **32**         |   **388.324 μs** |  **38.2830 μs** | **2.0984 μs** |  **1.00** |    **0.01** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   246.058 μs |   7.6959 μs | 0.4218 μs |  0.63 |    0.00 |  26.8555 |  1.7090 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   242.704 μs |  28.0848 μs | 1.5394 μs |  0.63 |    0.00 |  25.8789 |  1.4648 |   427.8 KB |        0.91 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **128**        | **1,542.597 μs** | **133.4911 μs** | **7.3171 μs** |  **1.00** |    **0.01** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   982.223 μs | 141.7421 μs | 7.7694 μs |  0.64 |    0.01 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   933.619 μs | 171.4936 μs | 9.4001 μs |  0.61 |    0.01 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Latin**  | **1000**   |      **55.78 μs** |     **0.636 μs** |   **0.035 μs** |         **-** |
| Cjk    | 1000   |      68.51 μs |    19.830 μs |   1.087 μs |         - |
| **Latin**  | **10000**  |   **5,519.22 μs** |   **443.375 μs** |  **24.303 μs** |         **-** |
| Cjk    | 10000  |   6,994.51 μs |   129.945 μs |   7.123 μs |         - |
| **Latin**  | **65536**  | **228,111.05 μs** | **4,529.561 μs** | **248.281 μs** |         **-** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github (run 2)

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Latin**  | **1000**   |      **51.93 μs** |     **0.631 μs** |   **0.035 μs** |         **-** |
| Cjk    | 1000   |      56.06 μs |     2.402 μs |   0.132 μs |         - |
| **Latin**  | **10000**  |   **5,514.80 μs** | **1,122.506 μs** |  **61.528 μs** |         **-** |
| Cjk    | 10000  |   6,234.71 μs |   289.373 μs |  15.862 μs |         - |
| **Latin**  | **65536**  | **202,152.38 μs** | **5,826.234 μs** | **319.356 μs** |         **-** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-24, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| Unigram | 624.8 ms | 73.52 ms | 4.03 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 569.8 ms | 36.95 ms | 2.03 ms |  0.91 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method                    | Length | Mean       | Error     | StdDev  | Gen0   | Allocated |
|-------------------------- |------- |-----------:|----------:|--------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |   **102.6 μs** |   **4.60 μs** | **0.25 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **217.7 μs** |  **29.07 μs** | **1.59 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **482.4 μs** |  **23.82 μs** | **1.31 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,029.0 μs** | **103.85 μs** | **5.69 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method     | Alphabet | Mean      | Error    | StdDev   | Allocated |
|----------- |--------- |----------:|---------:|---------:|----------:|
| **DpGroup**    | **cjk**      |  **19.16 μs** | **1.001 μs** | **0.055 μs** |         **-** |
| MyersGroup | cjk      | 168.23 μs | 1.894 μs | 0.104 μs |         - |
| **DpGroup**    | **latin**    |  **10.59 μs** | **0.057 μs** | **0.003 μs** |         **-** |
| MyersGroup | latin    | 112.46 μs | 0.541 μs | 0.030 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| Ratio          |    108.3 ns |   0.88 ns |  0.05 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 13,455.0 ns | 129.06 ns |  7.07 ns | 124.19 |    0.07 |      - |         - |          NA |
| TokenSortRatio |  1,001.5 ns |  58.60 ns |  3.21 ns |   9.24 |    0.03 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,355.6 ns | 671.99 ns | 36.83 ns |  30.97 |    0.29 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,623.5 ns | 609.93 ns | 33.43 ns |  42.68 |    0.27 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Distance_Utf16**             | **8**      |      **29.49 ns** |      **0.588 ns** |     **0.032 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     131.07 ns |      1.335 ns |     0.073 ns |  4.45 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      31.30 ns |      0.371 ns |     0.020 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      30.17 ns |      0.288 ns |     0.016 ns |  1.02 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **30.50 ns** |      **0.429 ns** |     **0.024 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     141.25 ns |     53.270 ns |     2.920 ns |  4.63 |    0.08 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      33.15 ns |      0.576 ns |     0.032 ns |  1.09 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      31.88 ns |      2.721 ns |     0.149 ns |  1.05 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **35.90 ns** |      **1.669 ns** |     **0.091 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     146.11 ns |      1.193 ns |     0.065 ns |  4.07 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      37.65 ns |      0.200 ns |     0.011 ns |  1.05 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      41.07 ns |      0.434 ns |     0.024 ns |  1.14 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **39.03 ns** |      **5.629 ns** |     **0.309 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     190.56 ns |      1.694 ns |     0.093 ns |  4.88 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      38.82 ns |      0.317 ns |     0.017 ns |  0.99 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      34.67 ns |      1.974 ns |     0.108 ns |  0.89 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **56.64 ns** |      **0.522 ns** |     **0.029 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     854.27 ns |    218.674 ns |    11.986 ns | 15.08 |    0.18 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      60.84 ns |      0.344 ns |     0.019 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      58.88 ns |      1.951 ns |     0.107 ns |  1.04 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **68.60 ns** |      **1.508 ns** |     **0.083 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,029.06 ns |     95.058 ns |     5.210 ns | 15.00 |    0.07 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      72.76 ns |      2.376 ns |     0.130 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      65.76 ns |      1.545 ns |     0.085 ns |  0.96 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **874.49 ns** |      **9.522 ns** |     **0.522 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  17,835.63 ns |  1,143.124 ns |    62.659 ns | 20.40 |    0.06 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     875.32 ns |     41.084 ns |     2.252 ns |  1.00 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     872.23 ns |      9.552 ns |     0.524 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **8,137.35 ns** |     **99.767 ns** |     **5.469 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 312,810.46 ns | 83,733.498 ns | 4,589.716 ns | 38.44 |    0.49 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   8,124.34 ns |    124.732 ns |     6.837 ns |  1.00 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   8,045.29 ns |     85.084 ns |     4.664 ns |  0.99 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Dp**         | **8**    |    **131.81 ns** |    **53.670 ns** |   **2.942 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 8    |     59.37 ns |     2.071 ns |   0.114 ns |  0.45 |    0.01 |         - |          NA |
| Dp_Cjk     | 8    |    130.15 ns |     1.510 ns |   0.083 ns |  0.99 |    0.02 |         - |          NA |
| Kernel_Cjk | 8    |    108.94 ns |     4.191 ns |   0.230 ns |  0.83 |    0.02 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **228.20 ns** |   **238.495 ns** |  **13.073 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel     | 12   |     68.80 ns |     1.746 ns |   0.096 ns |  0.30 |    0.01 |         - |          NA |
| Dp_Cjk     | 12   |    221.52 ns |    19.786 ns |   1.085 ns |  0.97 |    0.05 |         - |          NA |
| Kernel_Cjk | 12   |    120.19 ns |     1.199 ns |   0.066 ns |  0.53 |    0.03 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **14**   |    **275.17 ns** |     **2.724 ns** |   **0.149 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 14   |     73.24 ns |     1.352 ns |   0.074 ns |  0.27 |    0.00 |         - |          NA |
| Dp_Cjk     | 14   |    277.97 ns |    63.595 ns |   3.486 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 14   |    124.92 ns |    14.304 ns |   0.784 ns |  0.45 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **365.25 ns** |   **493.648 ns** |  **27.058 ns** |  **1.00** |    **0.09** |         **-** |          **NA** |
| Kernel     | 16   |     77.46 ns |     0.055 ns |   0.003 ns |  0.21 |    0.01 |         - |          NA |
| Dp_Cjk     | 16   |    381.44 ns |   200.799 ns |  11.006 ns |  1.05 |    0.07 |         - |          NA |
| Kernel_Cjk | 16   |    133.02 ns |    43.076 ns |   2.361 ns |  0.37 |    0.02 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **18**   |    **716.19 ns** |   **561.429 ns** |  **30.774 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel     | 18   |     84.96 ns |     0.875 ns |   0.048 ns |  0.12 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    727.59 ns |   508.262 ns |  27.860 ns |  1.02 |    0.05 |         - |          NA |
| Kernel_Cjk | 18   |    143.37 ns |     2.641 ns |   0.145 ns |  0.20 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **20**   |    **821.58 ns** |    **77.710 ns** |   **4.260 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 20   |     88.13 ns |     7.211 ns |   0.395 ns |  0.11 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    840.10 ns |   401.237 ns |  21.993 ns |  1.02 |    0.02 |         - |          NA |
| Kernel_Cjk | 20   |    143.06 ns |    46.698 ns |   2.560 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |  **1,007.06 ns** |   **490.533 ns** |  **26.888 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 24   |     96.89 ns |     3.930 ns |   0.215 ns |  0.10 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |  1,041.09 ns |   121.836 ns |   6.678 ns |  1.03 |    0.02 |         - |          NA |
| Kernel_Cjk | 24   |    152.93 ns |     3.332 ns |   0.183 ns |  0.15 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,585.56 ns** |   **128.461 ns** |   **7.041 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 32   |    119.15 ns |     0.499 ns |   0.027 ns |  0.08 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,679.85 ns |   489.215 ns |  26.816 ns |  1.06 |    0.02 |         - |          NA |
| Kernel_Cjk | 32   |    182.68 ns |     1.300 ns |   0.071 ns |  0.12 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,201.45 ns** | **1,302.232 ns** |  **71.380 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 48   |    143.67 ns |     1.326 ns |   0.073 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,288.84 ns |   617.808 ns |  33.864 ns |  1.03 |    0.02 |         - |          NA |
| Kernel_Cjk | 48   |    259.41 ns |     2.667 ns |   0.146 ns |  0.08 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **5,244.83 ns** | **3,040.984 ns** | **166.687 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel     | 64   |    171.73 ns |     2.329 ns |   0.128 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,387.76 ns | 1,368.431 ns |  75.008 ns |  1.03 |    0.03 |         - |          NA |
| Kernel_Cjk | 64   |    325.36 ns |    11.140 ns |   0.611 ns |  0.06 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **11,373.56 ns** | **3,858.675 ns** | **211.507 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 96   |    761.51 ns |    12.382 ns |   0.679 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 11,206.93 ns | 3,970.932 ns | 217.660 ns |  0.99 |    0.02 |         - |          NA |
| Kernel_Cjk | 96   |  1,060.51 ns |     6.436 ns |   0.353 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Distance_Utf16**             | **8**      |     **28.33 ns** |     **1.276 ns** |   **0.070 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    126.68 ns |     3.663 ns |   0.201 ns |  4.47 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     28.43 ns |     0.235 ns |   0.013 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **314.77 ns** |    **15.021 ns** |   **0.823 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    717.31 ns |   114.168 ns |   6.258 ns |  2.28 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    314.13 ns |    34.890 ns |   1.912 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    | **15,563.86 ns** | **2,298.831 ns** | **126.007 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 17,964.21 ns | 1,507.131 ns |  82.611 ns |  1.15 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 15,932.19 ns | 3,168.470 ns | 173.675 ns |  1.02 |    0.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method             | Length | Distinct | Mean         | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |-------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |     **356.1 ns** |      **3.52 ns** |     **0.19 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     255.9 ns |      2.83 ns |     0.15 ns |  0.72 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **355.7 ns** |      **1.67 ns** |     **0.09 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     256.6 ns |     11.47 ns |     0.63 ns |  0.72 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **448.2 ns** |     **20.38 ns** |     **1.12 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     356.4 ns |     19.03 ns |     1.04 ns |  0.80 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **459.3 ns** |     **10.69 ns** |     **0.59 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     347.5 ns |     16.57 ns |     0.91 ns |  0.76 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **539.8 ns** |     **63.92 ns** |     **3.50 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     444.5 ns |      4.19 ns |     0.23 ns |  0.82 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **538.2 ns** |      **3.45 ns** |     **0.19 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     443.8 ns |      3.09 ns |     0.17 ns |  0.82 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **630.2 ns** |    **123.13 ns** |     **6.75 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,350.4 ns |    398.80 ns |    21.86 ns |  2.14 |    0.04 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **644.8 ns** |     **98.90 ns** |     **5.42 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,346.0 ns |     53.21 ns |     2.92 ns |  2.09 |    0.02 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,598.1 ns** |    **391.67 ns** |    **21.47 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   6,036.3 ns |    182.56 ns |    10.01 ns |  2.32 |    0.02 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,615.6 ns** |    **746.13 ns** |    **40.90 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,738.2 ns |     99.45 ns |     5.45 ns |  2.19 |    0.03 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **20,428.0 ns** |     **72.58 ns** |     **3.98 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  65,236.8 ns |    867.79 ns |    47.57 ns |  3.19 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **441,387.2 ns** | **23,009.82 ns** | **1,261.25 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  66,563.0 ns |    550.99 ns |    30.20 ns |  0.15 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method         | Samples | Classes | Mean          | Error         | StdDev      | Gen0   | Allocated |
|--------------- |-------- |-------- |--------------:|--------------:|------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |      **7.059 μs** |     **1.2888 μs** |   **0.0706 μs** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7.339 μs |     0.2322 μs |   0.0127 μs | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |      1.030 μs |     0.0135 μs |   0.0007 μs |      - |         - |
| F1Macro        | 1000    | 2       |      6.949 μs |     0.2539 μs |   0.0139 μs | 0.0229 |     472 B |
| Report         | 1000    | 2       |      9.898 μs |     0.1538 μs |   0.0084 μs | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7.263 μs** |     **0.3474 μs** |   **0.0190 μs** | **0.0687** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7.212 μs |     0.3518 μs |   0.0193 μs | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |      1.031 μs |     0.0077 μs |   0.0004 μs |      - |         - |
| F1Macro        | 1000    | 10      |      7.663 μs |     0.3843 μs |   0.0211 μs | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     15.112 μs |     0.5290 μs |   0.0290 μs | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **861.407 μs** |    **33.9259 μs** |   **1.8596 μs** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    829.490 μs |    43.7391 μs |   2.3975 μs |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    134.712 μs |    22.9314 μs |   1.2569 μs |      - |         - |
| F1Macro        | 100000  | 2       |    804.161 μs |   450.6082 μs |  24.6994 μs |      - |     473 B |
| Report         | 100000  | 2       |    786.982 μs |   111.5784 μs |   6.1160 μs |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **964.783 μs** |   **117.3203 μs** |   **6.4307 μs** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    963.305 μs |   115.4891 μs |   6.3303 μs |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    246.502 μs |    23.5777 μs |   1.2924 μs |      - |         - |
| F1Macro        | 100000  | 10      |    955.768 μs |    21.0917 μs |   1.1561 μs |      - |    1665 B |
| Report         | 100000  | 10      |  1,008.654 μs |   136.7456 μs |   7.4955 μs |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,813.860 μs** |   **103.4961 μs** |   **5.6730 μs** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,529.730 μs |   426.8295 μs |  23.3960 μs |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,921.772 μs |   120.5121 μs |   6.6057 μs |      - |         - |
| F1Macro        | 1000000 | 2       |  9,109.033 μs | 5,440.1303 μs | 298.1919 μs |      - |     484 B |
| Report         | 1000000 | 2       |  8,491.819 μs |   336.0658 μs |  18.4209 μs |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **10,003.272 μs** |   **112.3667 μs** |   **6.1592 μs** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 10,182.909 μs |   903.1171 μs |  49.5029 μs |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  3,106.068 μs |    77.4111 μs |   4.2432 μs |      - |         - |
| F1Macro        | 1000000 | 10      | 10,165.727 μs |   303.2265 μs |  16.6209 μs |      - |    1676 B |
| Report         | 1000000 | 10      | 10,099.492 μs | 2,827.7571 μs | 154.9989 μs |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method     | Band | Mean         | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| **Dp**         | **4**    |     **83.46 ns** |   **0.904 ns** |  **0.050 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 4    |     81.27 ns |   2.074 ns |  0.114 ns |  0.97 |    0.00 |         - |          NA |
| Dp_Cjk     | 4    |     81.85 ns |   1.836 ns |  0.101 ns |  0.98 |    0.00 |         - |          NA |
| Kernel_Cjk | 4    |     79.99 ns |   0.780 ns |  0.043 ns |  0.96 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **6**    |    **119.71 ns** |  **10.429 ns** |  **0.572 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 6    |     88.31 ns |   0.905 ns |  0.050 ns |  0.74 |    0.00 |         - |          NA |
| Dp_Cjk     | 6    |    117.54 ns |   7.788 ns |  0.427 ns |  0.98 |    0.01 |         - |          NA |
| Kernel_Cjk | 6    |    145.55 ns |   5.194 ns |  0.285 ns |  1.22 |    0.01 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **8**    |    **156.21 ns** |   **3.864 ns** |  **0.212 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     99.47 ns |   2.433 ns |  0.133 ns |  0.64 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    156.99 ns |   6.056 ns |  0.332 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    217.18 ns |  14.118 ns |  0.774 ns |  1.39 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **10**   |    **208.77 ns** |  **48.221 ns** |  **2.643 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 10   |    107.73 ns |   0.828 ns |  0.045 ns |  0.52 |    0.01 |         - |          NA |
| Dp_Cjk     | 10   |    209.92 ns |  57.595 ns |  3.157 ns |  1.01 |    0.02 |         - |          NA |
| Kernel_Cjk | 10   |    168.04 ns |  25.550 ns |  1.401 ns |  0.81 |    0.01 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **12**   |    **269.14 ns** |  **11.261 ns** |  **0.617 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |    117.89 ns |   2.076 ns |  0.114 ns |  0.44 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    267.77 ns |   3.318 ns |  0.182 ns |  0.99 |    0.00 |         - |          NA |
| Kernel_Cjk | 12   |    179.59 ns |   2.507 ns |  0.137 ns |  0.67 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **16**   |    **428.79 ns** |  **23.711 ns** |  **1.300 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 16   |    139.14 ns |   3.341 ns |  0.183 ns |  0.32 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    433.54 ns |  92.706 ns |  5.082 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 16   |    200.20 ns |   4.481 ns |  0.246 ns |  0.47 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **24**   |    **865.14 ns** |  **52.676 ns** |  **2.887 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 24   |    183.24 ns | 113.175 ns |  6.203 ns |  0.21 |    0.01 |         - |          NA |
| Dp_Cjk     | 24   |    869.12 ns | 167.352 ns |  9.173 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 24   |    245.83 ns |   6.030 ns |  0.331 ns |  0.28 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **32**   |  **1,481.48 ns** |   **8.223 ns** |  **0.451 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    222.40 ns |  10.854 ns |  0.595 ns |  0.15 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,480.82 ns |  30.793 ns |  1.688 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 32   |    296.26 ns |  30.440 ns |  1.669 ns |  0.20 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **48**   |  **3,269.43 ns** | **543.401 ns** | **29.786 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 48   |    300.91 ns |   5.128 ns |  0.281 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,255.96 ns |  92.770 ns |  5.085 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 48   |    389.22 ns |   5.544 ns |  0.304 ns |  0.12 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **64**   |  **5,663.83 ns** | **377.042 ns** | **20.667 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 64   |    382.01 ns |  18.365 ns |  1.007 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,651.63 ns | 147.997 ns |  8.112 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 64   |    487.43 ns |  33.146 ns |  1.817 ns |  0.09 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **96**   | **12,528.79 ns** | **401.489 ns** | **22.007 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 96   |  1,264.27 ns | 311.682 ns | 17.084 ns |  0.10 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 12,526.19 ns | 360.880 ns | 19.781 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 96   |  1,589.85 ns |   1.261 ns |  0.069 ns |  0.13 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| VocabTxt               |  4.288 ms | 4.5989 ms | 0.2521 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 10.952 ms | 0.8955 ms | 0.0491 ms | 156.2500 | 125.0000 |  31.2500 |   5.72 MB |
| TokenizerJsonUnigram   | 11.393 ms | 4.1273 ms | 0.2262 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  4.125 ms | 3.4439 ms | 0.1888 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  2.036 ms | 1.6418 ms | 0.0900 ms |  27.3438 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              |  4.572 ms | 1.7732 ms | 0.0972 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  5.055 ms | 0.5440 ms | 0.0298 ms | 453.1250 | 453.1250 | 453.1250 |  39.64 MB |
| EmbeddingIndexLoad     |  5.474 ms | 5.7388 ms | 0.3146 ms | 500.0000 | 468.7500 | 437.5000 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method               | Documents | Mean      | Error     | StdDev    | Ratio | Gen0      | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|----------:|----------:|------:|----------:|---------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.798 ms** | **1.3319 ms** | **0.0730 ms** |  **1.00** |  **500.0000** | **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.389 ms | 0.6755 ms | 0.0370 ms |  0.82 |  390.6250 | 187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.577 ms | 1.2243 ms | 0.0671 ms |  0.97 |  500.0000 | 156.2500 |  62.5000 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.572 ms | 1.1493 ms | 0.0630 ms |  0.84 |  406.2500 | 156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |          |          |           |             |
| **Count**                | **1000**      | **31.309 ms** | **6.4582 ms** | **0.3540 ms** |  **1.00** | **2625.0000** | **875.0000** | **500.0000** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 24.515 ms | 7.0901 ms | 0.3886 ms |  0.78 | 1968.7500 | 781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 29.530 ms | 0.1070 ms | 0.0059 ms |  0.94 | 2562.5000 | 750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 25.096 ms | 1.5851 ms | 0.0869 ms |  0.80 | 2031.2500 | 625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Dot**    | **384**  |  **51.57 ns** | **6.701 ns** | **0.367 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.69 ns | 0.081 ns | 0.004 ns |  0.94 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  | **100.15 ns** | **2.255 ns** | **0.124 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  90.95 ns | 2.706 ns | 0.148 ns |  0.91 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **132.53 ns** | **1.202 ns** | **0.066 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 125.04 ns | 0.634 ns | 0.035 ns |  0.94 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Count**        | **200**       |  **3.006 ms** | **0.4514 ms** | **0.0247 ms** |  **1.00** |    **0.01** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.053 ms | 1.8870 ms | 0.1034 ms |  1.02 |    0.03 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.855 ms | 0.8642 ms | 0.0474 ms |  1.28 |    0.02 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.956 ms | 0.1465 ms | 0.0080 ms |  0.98 |    0.01 |  97.6563 |  31.2500 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.151 ms** | **0.6629 ms** | **0.0363 ms** |  **1.00** |    **0.01** | **492.1875** | **351.5625** |  **70.3125** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.505 ms | 3.9234 ms | 0.2151 ms |  1.05 |    0.03 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.860 ms | 2.0967 ms | 0.1149 ms |  1.66 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.210 ms | 0.9742 ms | 0.0534 ms |  1.01 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 109.5 | 23.8 | 4.60x C# faster |
| latin | 32 | 157.6 | 69.3 | 2.27x C# faster |
| latin | 128 | 473.7 | 840.7 | 1.77x Py faster |
| latin | 512 | 4873.3 | 8279.3 | 1.70x Py faster |
| cjk | 8 | 129.5 | 23.6 | 5.48x C# faster |
| cjk | 32 | 233.6 | 143.9 | 1.62x C# faster |
| cjk | 128 | 2049.3 | 1700.3 | 1.21x C# faster |
| cjk | 512 | 16738.7 | 11787.2 | 1.42x C# faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is Hyyro's bit-parallel LLCS above a pattern of 8 and a rolling-row dynamic program below it (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 140.4 | 18.2 | 7.72x C# faster |
| latin | 32 | 256.6 | 127.7 | 2.01x C# faster |
| latin | 128 | 1815.8 | 1527.7 | 1.19x C# faster |
| latin | 512 | 15585.1 | 16614.8 | 1.07x Py faster |
| cjk | 8 | 144.7 | 18.1 | 7.98x C# faster |
| cjk | 32 | 297.5 | 190.8 | 1.56x C# faster |
| cjk | 128 | 3009.8 | 2525.5 | 1.19x C# faster |
| cjk | 512 | 26216.0 | 20453.1 | 1.28x C# faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.767 | 87.81x | 0.009 | 0.767 | 87.79x |
| accuracy_n1000_k2 | 0.001 | 0.397 | 377.88x | 0.001 | 0.397 | 377.86x |
| precision_recall_f1_macro_n1000_k2 | 0.007 | 1.392 | 194.38x | 0.007 | 1.392 | 194.40x |
| classification_report_n1000_k2 | 0.010 | 5.261 | 537.53x | 0.010 | 5.261 | 537.58x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.545 | 93.85x | 0.016 | 1.545 | 93.86x |
| balanced_accuracy_n1000_k2 | 0.007 | 0.822 | 118.20x | 0.007 | 0.822 | 118.21x |
| matthews_n1000_k2 | 0.007 | 1.544 | 216.62x | 0.007 | 1.544 | 216.62x |
| cohen_kappa_n1000_k2 | 0.007 | 0.875 | 122.65x | 0.007 | 0.875 | 122.65x |
| mse_n1000_k2 | 0.003 | 0.214 | 77.79x | 0.003 | 0.214 | 77.79x |
| mae_n1000_k2 | 0.003 | 0.213 | 77.55x | 0.003 | 0.213 | 77.55x |
| median_ae_n1000_k2 | 0.006 | 0.230 | 37.49x | 0.006 | 0.230 | 37.49x |
| r2_n1000_k2 | 0.003 | 0.269 | 98.30x | 0.003 | 0.269 | 98.28x |
| confusion_matrix_n1000_k10 | 0.009 | 0.774 | 84.58x | 0.009 | 0.774 | 84.57x |
| accuracy_n1000_k10 | 0.001 | 0.401 | 383.12x | 0.001 | 0.401 | 383.12x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.415 | 182.70x | 0.008 | 1.415 | 182.72x |
| classification_report_n1000_k10 | 0.014 | 5.509 | 385.10x | 0.014 | 5.507 | 385.02x |
| roc_auc_ovr_macro_n1000_k10 | 0.533 | 8.214 | 15.41x | 0.533 | 8.214 | 15.41x |
| balanced_accuracy_n1000_k10 | 0.008 | 0.830 | 107.05x | 0.008 | 0.830 | 107.06x |
| matthews_n1000_k10 | 0.008 | 1.589 | 206.26x | 0.008 | 1.589 | 206.25x |
| cohen_kappa_n1000_k10 | 0.008 | 0.874 | 105.30x | 0.008 | 0.874 | 105.31x |
| mse_n1000_k10 | 0.003 | 0.215 | 78.81x | 0.003 | 0.215 | 78.81x |
| mae_n1000_k10 | 0.003 | 0.215 | 78.42x | 0.003 | 0.215 | 78.42x |
| median_ae_n1000_k10 | 0.006 | 0.228 | 37.15x | 0.006 | 0.228 | 37.15x |
| r2_n1000_k10 | 0.003 | 0.268 | 98.03x | 0.003 | 0.268 | 98.03x |
| confusion_matrix_n100000_k2 | 0.967 | 11.046 | 11.43x | 0.967 | 11.044 | 11.43x |
| accuracy_n100000_k2 | 0.106 | 3.790 | 35.81x | 0.106 | 3.790 | 35.81x |
| precision_recall_f1_macro_n100000_k2 | 0.830 | 12.601 | 15.17x | 0.830 | 12.599 | 15.17x |
| classification_report_n100000_k2 | 0.801 | 26.865 | 33.53x | 0.801 | 26.864 | 33.53x |
| roc_auc_binary_n100000_k2 | 2.884 | 28.629 | 9.93x | 2.884 | 28.629 | 9.93x |
| balanced_accuracy_n100000_k2 | 0.782 | 11.095 | 14.18x | 0.782 | 11.095 | 14.18x |
| matthews_n100000_k2 | 0.775 | 22.235 | 28.69x | 0.775 | 22.234 | 28.69x |
| cohen_kappa_n100000_k2 | 0.787 | 11.126 | 14.14x | 0.787 | 11.125 | 14.14x |
| mse_n100000_k2 | 0.268 | 0.369 | 1.38x | 0.268 | 0.369 | 1.38x |
| mae_n100000_k2 | 0.269 | 0.368 | 1.37x | 0.269 | 0.368 | 1.37x |
| median_ae_n100000_k2 | 0.683 | 1.881 | 2.76x | 0.703 | 1.881 | 2.68x |
| r2_n100000_k2 | 0.260 | 0.581 | 2.24x | 0.260 | 0.581 | 2.24x |
| confusion_matrix_n100000_k10 | 0.956 | 11.052 | 11.57x | 0.956 | 11.051 | 11.57x |
| accuracy_n100000_k10 | 0.106 | 3.793 | 35.94x | 0.106 | 3.793 | 35.94x |
| precision_recall_f1_macro_n100000_k10 | 0.975 | 13.320 | 13.67x | 0.975 | 13.319 | 13.67x |
| classification_report_n100000_k10 | 0.983 | 30.003 | 30.53x | 0.983 | 30.002 | 30.53x |
| roc_auc_ovr_macro_n100000_k10 | 31.526 | 236.230 | 7.49x | 31.518 | 236.196 | 7.49x |
| balanced_accuracy_n100000_k10 | 0.965 | 11.129 | 11.53x | 0.965 | 11.127 | 11.53x |
| matthews_n100000_k10 | 0.938 | 23.148 | 24.69x | 0.937 | 23.144 | 24.69x |
| cohen_kappa_n100000_k10 | 1.144 | 11.278 | 9.86x | 1.143 | 11.276 | 9.86x |
| mse_n100000_k10 | 0.267 | 0.375 | 1.40x | 0.267 | 0.375 | 1.40x |
| mae_n100000_k10 | 0.268 | 0.373 | 1.39x | 0.268 | 0.373 | 1.39x |
| median_ae_n100000_k10 | 0.669 | 1.888 | 2.82x | 0.707 | 1.888 | 2.67x |
| r2_n100000_k10 | 0.260 | 0.593 | 2.28x | 0.260 | 0.593 | 2.28x |
| confusion_matrix_n1000000_k2 | 8.526 | 106.066 | 12.44x | 8.527 | 106.063 | 12.44x |
| accuracy_n1000000_k2 | 2.137 | 34.695 | 16.23x | 2.137 | 34.685 | 16.23x |
| precision_recall_f1_macro_n1000000_k2 | 8.488 | 113.461 | 13.37x | 8.487 | 113.446 | 13.37x |
| classification_report_n1000000_k2 | 8.477 | 221.587 | 26.14x | 8.477 | 221.569 | 26.14x |
| roc_auc_binary_n1000000_k2 | 46.814 | 317.120 | 6.77x | 46.808 | 317.074 | 6.77x |
| balanced_accuracy_n1000000_k2 | 8.466 | 107.256 | 12.67x | 8.466 | 107.235 | 12.67x |
| matthews_n1000000_k2 | 8.371 | 208.839 | 24.95x | 8.372 | 208.831 | 24.95x |
| cohen_kappa_n1000000_k2 | 8.376 | 107.269 | 12.81x | 8.374 | 107.270 | 12.81x |
| mse_n1000000_k2 | 2.660 | 1.999 | 0.75x | 2.660 | 1.999 | 0.75x |
| mae_n1000000_k2 | 2.667 | 1.988 | 0.75x | 2.667 | 1.988 | 0.75x |
| median_ae_n1000000_k2 | 6.280 | 15.780 | 2.51x | 6.316 | 15.778 | 2.50x |
| r2_n1000000_k2 | 2.605 | 3.922 | 1.51x | 2.604 | 3.922 | 1.51x |
| confusion_matrix_n1000000_k10 | 10.022 | 106.561 | 10.63x | 10.020 | 106.508 | 10.63x |
| accuracy_n1000000_k10 | 3.117 | 34.614 | 11.10x | 3.117 | 34.605 | 11.10x |
| precision_recall_f1_macro_n1000000_k10 | 9.938 | 120.707 | 12.15x | 9.936 | 120.686 | 12.15x |
| classification_report_n1000000_k10 | 10.030 | 247.954 | 24.72x | 10.030 | 247.947 | 24.72x |
| balanced_accuracy_n1000000_k10 | 10.020 | 105.632 | 10.54x | 10.020 | 105.624 | 10.54x |
| matthews_n1000000_k10 | 9.769 | 215.810 | 22.09x | 9.769 | 215.799 | 22.09x |
| cohen_kappa_n1000000_k10 | 9.725 | 105.793 | 10.88x | 9.725 | 105.786 | 10.88x |
| mse_n1000000_k10 | 2.680 | 1.953 | 0.73x | 2.680 | 1.953 | 0.73x |
| mae_n1000000_k10 | 2.688 | 1.968 | 0.73x | 2.688 | 1.968 | 0.73x |
| median_ae_n1000000_k10 | 6.206 | 15.669 | 2.52x | 6.254 | 15.666 | 2.50x |
| r2_n1000000_k10 | 2.783 | 3.743 | 1.34x | 2.783 | 3.742 | 1.34x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.75x
  mae_n1000000_k2                  0.75x
  mse_n1000000_k10                 0.73x
  mae_n1000000_k10                 0.73x

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-25, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.987 | 10.801 | 2.17x | 5.253 | 10.799 | 2.06x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.946 | 19.270 | 1.49x | 13.344 | 19.270 | 1.44x | 706,526 | 706,526 |
| tokenizer_json_unigram | 11.148 | 47.321 | 4.24x | 11.506 | 47.313 | 4.11x | 1,990,038 | 1,990,038 |
| spiece_model | 5.007 | 32.471 | 6.49x | 5.224 | 32.466 | 6.21x | 533,084 | 533,084 |
| tfidf_save | 1.835 | 2.497 | 1.36x | 1.900 | 2.497 | 1.31x | 581,787 | 591,922 |
| tfidf_load | 4.755 | 4.375 | 0.92x | 4.971 | 4.375 | 0.88x | 581,787 | 591,922 |
| embedding_index_save | 5.313 | 1.403 | 0.26x | 5.924 | 1.402 | 0.24x | 20,589,007 | 15,360,128 |
| embedding_index_load | 5.026 | 1.674 | 0.33x | 5.781 | 1.674 | 0.29x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 5.842 | 1.015 | 0.17x | 6.616 | 1.014 | 0.15x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 3.168 | 1.696 | 0.54x | 3.543 | 1.696 | 0.48x | 20,589,007 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 55.91x | 0.000 | 0.001 | 55.92x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 455.719 | 637.458 | 1.40x | 457.034 | 637.438 | 1.39x | 15,251,458 | 14,022,374 |
| embedding_index_load_gzip | 80.082 | 72.797 | 0.91x | 81.361 | 72.795 | 0.89x | 15,251,458 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
