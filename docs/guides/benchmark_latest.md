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

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **UnitLoop**           | **1**          |     **6.189 μs** |   **2.3101 μs** |  **0.1266 μs** |  **1.00** |    **0.03** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.243 μs |   0.9375 μs |  0.0514 μs |  1.01 |    0.02 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.253 μs |   0.6547 μs |  0.0359 μs |  1.01 |    0.02 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **8**          |   **109.359 μs** |  **18.2062 μs** |  **0.9979 μs** |  **1.00** |    **0.01** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    71.402 μs |   3.5338 μs |  0.1937 μs |  0.65 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    70.391 μs |  41.6756 μs |  2.2844 μs |  0.64 |    0.02 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **32**         |   **394.532 μs** |  **31.6358 μs** |  **1.7341 μs** |  **1.00** |    **0.01** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   242.950 μs |  37.7236 μs |  2.0678 μs |  0.62 |    0.01 |  26.8555 |  1.7090 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   228.132 μs |  29.5060 μs |  1.6173 μs |  0.58 |    0.00 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **128**        | **1,549.338 μs** | **137.4077 μs** |  **7.5318 μs** |  **1.00** |    **0.01** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   966.078 μs | 273.6909 μs | 15.0019 μs |  0.62 |    0.01 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   895.027 μs |  57.2511 μs |  3.1381 μs |  0.58 |    0.00 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method | length | Mean          | Error        | StdDev     | Allocated |
|------- |------- |--------------:|-------------:|-----------:|----------:|
| **Latin**  | **1000**   |      **50.19 μs** |     **1.767 μs** |   **0.097 μs** |         **-** |
| Cjk    | 1000   |      55.57 μs |     2.793 μs |   0.153 μs |         - |
| **Latin**  | **10000**  |   **5,488.77 μs** |   **270.213 μs** |  **14.811 μs** |         **-** |
| Cjk    | 10000  |   6,713.08 μs |   578.218 μs |  31.694 μs |         - |
| **Latin**  | **65536**  | **200,567.30 μs** | **3,779.691 μs** | **207.178 μs** |         **-** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method  | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0       | Allocated | Alloc Ratio |
|-------- |---------:|----------:|---------:|------:|--------:|-----------:|----------:|------------:|
| Unigram | 578.2 ms |  35.87 ms |  1.97 ms |  1.00 |    0.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 550.9 ms | 195.57 ms | 10.72 ms |  0.95 |    0.02 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **BpeOnOnePathologicalToken** | **512**    | **104.9 μs** |  **6.68 μs** | **0.37 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **217.8 μs** | **10.94 μs** | **0.60 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **477.7 μs** | **43.15 μs** | **2.37 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **986.2 μs** | **57.33 μs** | **3.14 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method     | Alphabet | Mean       | Error      | StdDev    | Allocated |
|----------- |--------- |-----------:|-----------:|----------:|----------:|
| **DpGroup**    | **cjk**      |  **20.236 μs** |  **0.6906 μs** | **0.0379 μs** |         **-** |
| MyersGroup | cjk      | 242.932 μs | 35.2016 μs | 1.9295 μs |         - |
| **DpGroup**    | **latin**    |   **9.914 μs** |  **2.4776 μs** | **0.1358 μs** |         **-** |
| MyersGroup | latin    | 130.723 μs | 22.0919 μs | 1.2109 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| Ratio          |     99.13 ns |   1.415 ns |  0.078 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 12,473.75 ns | 211.275 ns | 11.581 ns | 125.83 |    0.13 |      - |         - |          NA |
| TokenSortRatio |  1,053.48 ns | 159.239 ns |  8.728 ns |  10.63 |    0.08 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,508.66 ns | 127.459 ns |  6.986 ns |  35.39 |    0.07 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,652.19 ns | 563.444 ns | 30.884 ns |  46.93 |    0.27 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Distance_Utf16**             | **8**      |      **26.53 ns** |      **0.172 ns** |     **0.009 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     139.03 ns |      1.785 ns |     0.098 ns |  5.24 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      26.92 ns |      0.429 ns |     0.024 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.61 ns |      0.526 ns |     0.029 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.04 ns** |      **0.509 ns** |     **0.028 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     152.66 ns |      0.142 ns |     0.008 ns |  5.44 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.95 ns |      4.504 ns |     0.247 ns |  1.03 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.12 ns |      0.021 ns |     0.001 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **31.85 ns** |      **1.434 ns** |     **0.079 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     165.25 ns |     14.705 ns |     0.806 ns |  5.19 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      33.87 ns |      1.160 ns |     0.064 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      30.91 ns |      0.947 ns |     0.052 ns |  0.97 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **35.28 ns** |      **1.915 ns** |     **0.105 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     183.84 ns |      6.802 ns |     0.373 ns |  5.21 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      34.70 ns |      4.239 ns |     0.232 ns |  0.98 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      32.67 ns |      0.140 ns |     0.008 ns |  0.93 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **55.51 ns** |      **0.220 ns** |     **0.012 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     686.42 ns |    168.333 ns |     9.227 ns | 12.37 |    0.14 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      55.68 ns |      6.681 ns |     0.366 ns |  1.00 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      54.24 ns |     26.961 ns |     1.478 ns |  0.98 |    0.02 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **61.62 ns** |      **0.228 ns** |     **0.013 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,021.99 ns |     35.540 ns |     1.948 ns | 16.58 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      64.30 ns |      0.596 ns |     0.033 ns |  1.04 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      59.59 ns |     16.978 ns |     0.931 ns |  0.97 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **931.81 ns** |     **12.567 ns** |     **0.689 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  20,218.61 ns | 19,039.334 ns | 1,043.610 ns | 21.70 |    0.97 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     922.29 ns |      3.802 ns |     0.208 ns |  0.99 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     977.24 ns |     18.495 ns |     1.014 ns |  1.05 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **7,444.21 ns** |    **215.915 ns** |    **11.835 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 340,001.09 ns |  2,830.969 ns |   155.175 ns | 45.67 |    0.07 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   7,324.61 ns |    316.699 ns |    17.359 ns |  0.98 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   7,740.50 ns |     83.088 ns |     4.554 ns |  1.04 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method     | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **8**    |    **131.65 ns** |     **5.150 ns** |   **0.282 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     55.22 ns |     1.350 ns |   0.074 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    130.53 ns |     5.712 ns |   0.313 ns |  0.99 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |     97.63 ns |    11.224 ns |   0.615 ns |  0.74 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **227.38 ns** |    **70.728 ns** |   **3.877 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 12   |     61.55 ns |     0.566 ns |   0.031 ns |  0.27 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    225.78 ns |   115.547 ns |   6.334 ns |  0.99 |    0.03 |         - |          NA |
| Kernel_Cjk | 12   |    108.40 ns |     6.723 ns |   0.369 ns |  0.48 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **14**   |    **271.50 ns** |    **84.324 ns** |   **4.622 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 14   |     67.85 ns |     1.865 ns |   0.102 ns |  0.25 |    0.00 |         - |          NA |
| Dp_Cjk     | 14   |    277.03 ns |   193.197 ns |  10.590 ns |  1.02 |    0.04 |         - |          NA |
| Kernel_Cjk | 14   |    112.21 ns |     1.595 ns |   0.087 ns |  0.41 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **351.38 ns** |     **4.851 ns** |   **0.266 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 16   |     71.90 ns |     1.182 ns |   0.065 ns |  0.20 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    357.37 ns |   194.532 ns |  10.663 ns |  1.02 |    0.03 |         - |          NA |
| Kernel_Cjk | 16   |    118.51 ns |     1.254 ns |   0.069 ns |  0.34 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **18**   |    **435.56 ns** |     **3.881 ns** |   **0.213 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 18   |     73.80 ns |     0.935 ns |   0.051 ns |  0.17 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    447.80 ns |   198.757 ns |  10.895 ns |  1.03 |    0.02 |         - |          NA |
| Kernel_Cjk | 18   |    122.98 ns |     0.617 ns |   0.034 ns |  0.28 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **20**   |    **761.27 ns** |   **178.217 ns** |   **9.769 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 20   |     79.84 ns |     1.875 ns |   0.103 ns |  0.10 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    769.54 ns |     3.628 ns |   0.199 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 20   |    128.25 ns |     2.643 ns |   0.145 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **981.75 ns** |   **182.204 ns** |   **9.987 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 24   |     89.35 ns |    35.923 ns |   1.969 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |  1,002.68 ns |   443.824 ns |  24.328 ns |  1.02 |    0.02 |         - |          NA |
| Kernel_Cjk | 24   |    140.50 ns |     0.314 ns |   0.017 ns |  0.14 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,570.18 ns** |   **132.328 ns** |   **7.253 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 32   |    104.97 ns |     3.364 ns |   0.184 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,726.34 ns |    70.732 ns |   3.877 ns |  1.10 |    0.00 |         - |          NA |
| Kernel_Cjk | 32   |    167.02 ns |     5.486 ns |   0.301 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,216.13 ns** |   **194.745 ns** |  **10.675 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 48   |    143.62 ns |    30.433 ns |   1.668 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,594.85 ns |   981.008 ns |  53.772 ns |  1.12 |    0.01 |         - |          NA |
| Kernel_Cjk | 48   |    249.38 ns |     1.055 ns |   0.058 ns |  0.08 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **5,484.10 ns** |   **154.720 ns** |   **8.481 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 64   |    156.25 ns |     1.245 ns |   0.068 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,335.29 ns | 6,550.765 ns | 359.070 ns |  0.97 |    0.06 |         - |          NA |
| Kernel_Cjk | 64   |    291.56 ns |     4.090 ns |   0.224 ns |  0.05 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **13,929.72 ns** | **2,094.190 ns** | **114.790 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 96   |    786.40 ns |    10.548 ns |   0.578 ns |  0.06 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 12,410.25 ns |   519.360 ns |  28.468 ns |  0.89 |    0.01 |         - |          NA |
| Kernel_Cjk | 96   |  1,028.75 ns |    58.752 ns |   3.220 ns |  0.07 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Distance_Utf16**             | **8**      |     **24.94 ns** |   **0.425 ns** |  **0.023 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    118.38 ns |   3.893 ns |  0.213 ns |  4.75 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.01 ns |   0.418 ns |  0.023 ns |  1.00 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **64**     |    **270.75 ns** |  **31.573 ns** |  **1.731 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    658.33 ns |  23.818 ns |  1.306 ns |  2.43 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    271.84 ns |   2.013 ns |  0.110 ns |  1.00 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **512**    | **14,831.18 ns** | **914.479 ns** | **50.126 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 17,036.34 ns | 133.972 ns |  7.343 ns |  1.15 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,485.47 ns | 377.408 ns | 20.687 ns |  0.98 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method             | Length | Distinct | Mean         | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |-------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |     **339.6 ns** |     **71.60 ns** |     **3.92 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     240.4 ns |     47.36 ns |     2.60 ns |  0.71 |    0.01 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **334.4 ns** |     **10.16 ns** |     **0.56 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     241.8 ns |     15.15 ns |     0.83 ns |  0.72 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **453.3 ns** |     **22.32 ns** |     **1.22 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     321.3 ns |     18.61 ns |     1.02 ns |  0.71 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **414.0 ns** |      **3.22 ns** |     **0.18 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     327.8 ns |     57.68 ns |     3.16 ns |  0.79 |    0.01 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **499.5 ns** |    **140.02 ns** |     **7.67 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     415.9 ns |      7.82 ns |     0.43 ns |  0.83 |    0.01 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **513.5 ns** |      **3.78 ns** |     **0.21 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     419.1 ns |      3.97 ns |     0.22 ns |  0.82 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **602.0 ns** |    **140.13 ns** |     **7.68 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,307.8 ns |    262.63 ns |    14.40 ns |  2.17 |    0.03 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **593.1 ns** |      **3.73 ns** |     **0.20 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,303.3 ns |     23.23 ns |     1.27 ns |  2.20 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,552.8 ns** |     **15.03 ns** |     **0.82 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,398.5 ns |     81.22 ns |     4.45 ns |  2.11 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,572.8 ns** |    **541.14 ns** |    **29.66 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,442.5 ns |     19.41 ns |     1.06 ns |  2.12 |    0.02 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **18,455.4 ns** |    **630.38 ns** |    **34.55 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  66,210.7 ns |  9,351.92 ns |   512.61 ns |  3.59 |    0.02 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **452,277.6 ns** | **19,357.25 ns** | **1,061.04 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  58,573.0 ns |  1,213.94 ns |    66.54 ns |  0.13 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method         | Samples | Classes | Mean           | Error         | StdDev       | Gen0   | Allocated |
|--------------- |-------- |-------- |---------------:|--------------:|-------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |     **7,802.2 ns** |     **313.29 ns** |     **17.17 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     7,278.6 ns |   2,556.71 ns |    140.14 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |       919.0 ns |      25.91 ns |      1.42 ns |      - |         - |
| F1Macro        | 1000    | 2       |     7,793.1 ns |     646.91 ns |     35.46 ns | 0.0153 |     472 B |
| Report         | 1000    | 2       |    10,434.4 ns |     738.68 ns |     40.49 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **7,812.8 ns** |     **290.37 ns** |     **15.92 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     7,634.1 ns |     298.40 ns |     16.36 ns | 0.0610 |    1248 B |
| AccuracyScore  | 1000    | 10      |       920.0 ns |      30.78 ns |      1.69 ns |      - |         - |
| F1Macro        | 1000    | 10      |     8,031.9 ns |     273.10 ns |     14.97 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |    15,288.0 ns |   1,545.55 ns |     84.72 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **879,834.5 ns** |  **16,423.29 ns** |    **900.22 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   812,863.7 ns |  23,351.18 ns |  1,279.96 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   165,529.2 ns |   7,978.72 ns |    437.34 ns |      - |         - |
| F1Macro        | 100000  | 2       |   866,433.5 ns |   9,080.25 ns |    497.72 ns |      - |     473 B |
| Report         | 100000  | 2       |   887,088.7 ns |  48,413.74 ns |  2,653.72 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **972,568.1 ns** |  **20,123.26 ns** |  **1,103.02 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   939,584.1 ns |  29,045.90 ns |  1,592.10 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   272,970.4 ns |  21,393.49 ns |  1,172.65 ns |      - |         - |
| F1Macro        | 100000  | 10      |   959,510.4 ns |  21,633.12 ns |  1,185.78 ns |      - |    1665 B |
| Report         | 100000  | 10      | 1,248,085.4 ns | 108,927.34 ns |  5,970.68 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **8,696,650.2 ns** | **144,121.49 ns** |  **7,899.79 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       | 8,459,490.2 ns | 493,661.38 ns | 27,059.25 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       | 1,744,613.0 ns |  19,783.86 ns |  1,084.42 ns |      - |         - |
| F1Macro        | 1000000 | 2       | 8,328,628.2 ns | 119,692.98 ns |  6,560.78 ns |      - |     484 B |
| Report         | 1000000 | 2       | 8,336,704.3 ns | 576,651.59 ns | 31,608.22 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **9,344,820.8 ns** | **992,825.42 ns** | **54,420.11 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 9,381,731.5 ns | 740,162.61 ns | 40,570.81 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      | 2,688,688.0 ns |  15,972.60 ns |    875.51 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 9,778,451.3 ns |  65,791.87 ns |  3,606.27 ns |      - |    1676 B |
| Report         | 1000000 | 10      | 9,961,907.3 ns | 103,546.26 ns |  5,675.72 ns |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method     | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **4**    |     **73.73 ns** |     **2.207 ns** |   **0.121 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 4    |     74.24 ns |     2.929 ns |   0.161 ns |  1.01 |    0.00 |         - |          NA |
| Dp_Cjk     | 4    |     73.47 ns |     0.335 ns |   0.018 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 4    |     76.42 ns |     3.200 ns |   0.175 ns |  1.04 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **6**    |    **106.01 ns** |    **48.285 ns** |   **2.647 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 6    |     77.42 ns |     4.553 ns |   0.250 ns |  0.73 |    0.02 |         - |          NA |
| Dp_Cjk     | 6    |    103.44 ns |     1.608 ns |   0.088 ns |  0.98 |    0.02 |         - |          NA |
| Kernel_Cjk | 6    |    146.26 ns |    19.159 ns |   1.050 ns |  1.38 |    0.03 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **8**    |    **156.40 ns** |     **2.895 ns** |   **0.159 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     88.48 ns |     0.623 ns |   0.034 ns |  0.57 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    165.07 ns |    12.106 ns |   0.664 ns |  1.06 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    199.67 ns |     3.421 ns |   0.188 ns |  1.28 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **10**   |    **196.95 ns** |     **9.194 ns** |   **0.504 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 10   |     94.40 ns |     1.976 ns |   0.108 ns |  0.48 |    0.00 |         - |          NA |
| Dp_Cjk     | 10   |    197.22 ns |     5.555 ns |   0.304 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 10   |    153.15 ns |    18.465 ns |   1.012 ns |  0.78 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **245.69 ns** |     **5.662 ns** |   **0.310 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |    102.72 ns |     0.992 ns |   0.054 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    246.28 ns |     2.329 ns |   0.128 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 12   |    164.10 ns |     2.174 ns |   0.119 ns |  0.67 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **409.83 ns** |    **26.845 ns** |   **1.471 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 16   |    120.75 ns |    23.470 ns |   1.286 ns |  0.29 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    420.88 ns |   190.367 ns |  10.435 ns |  1.03 |    0.02 |         - |          NA |
| Kernel_Cjk | 16   |    180.79 ns |     1.638 ns |   0.090 ns |  0.44 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **841.09 ns** |     **6.885 ns** |   **0.377 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 24   |    158.28 ns |     0.931 ns |   0.051 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    845.47 ns |    67.005 ns |   3.673 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 24   |    235.13 ns |     2.732 ns |   0.150 ns |  0.28 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,450.78 ns** |    **10.602 ns** |   **0.581 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    194.67 ns |     6.380 ns |   0.350 ns |  0.13 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,449.89 ns |    30.297 ns |   1.661 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 32   |    270.30 ns |    14.984 ns |   0.821 ns |  0.19 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,354.79 ns** | **1,630.192 ns** |  **89.356 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 48   |    265.07 ns |     4.979 ns |   0.273 ns |  0.08 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,300.70 ns |    45.635 ns |   2.501 ns |  0.98 |    0.02 |         - |          NA |
| Kernel_Cjk | 48   |    353.43 ns |     3.748 ns |   0.205 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **6,606.95 ns** | **2,377.565 ns** | **130.322 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 64   |    340.86 ns |    22.080 ns |   1.210 ns |  0.05 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,207.08 ns |   100.680 ns |   5.519 ns |  0.94 |    0.02 |         - |          NA |
| Kernel_Cjk | 64   |    451.97 ns |    26.654 ns |   1.461 ns |  0.07 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **14,950.26 ns** | **4,334.199 ns** | **237.572 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 96   |  1,225.43 ns |     7.254 ns |   0.398 ns |  0.08 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 14,274.74 ns | 8,312.195 ns | 455.619 ns |  0.95 |    0.03 |         - |          NA |
| Kernel_Cjk | 96   |  1,513.43 ns |   381.250 ns |  20.898 ns |  0.10 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| VocabTxt               |  4.054 ms | 3.8838 ms | 0.2129 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.325 ms | 3.1970 ms | 0.1752 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.753 ms | 0.1949 ms | 0.0107 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.643 ms | 2.2416 ms | 0.1229 ms | 121.0938 | 117.1875 |  39.0625 |   3.36 MB |
| TfidfSave              |  1.898 ms | 0.1046 ms | 0.0057 ms |  33.2031 |  27.3438 |  27.3438 |   2.09 MB |
| TfidfLoad              |  4.330 ms | 0.9794 ms | 0.0537 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  6.692 ms | 4.7319 ms | 0.2594 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  5.811 ms | 4.0341 ms | 0.2211 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

| Method               | Documents | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0      | Gen1      | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|-----------:|----------:|------:|--------:|----------:|----------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.346 ms** |  **1.4714 ms** | **0.0807 ms** |  **1.00** |    **0.01** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.111 ms |  0.3505 ms | 0.0192 ms |  0.83 |    0.01 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.271 ms |  0.2382 ms | 0.0131 ms |  0.99 |    0.01 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.325 ms |  1.4336 ms | 0.0786 ms |  0.86 |    0.01 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |            |           |       |         |           |           |          |           |             |
| **Count**                | **1000**      | **30.377 ms** |  **9.3437 ms** | **0.5122 ms** |  **1.00** |    **0.02** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 23.190 ms |  4.2890 ms | 0.2351 ms |  0.76 |    0.01 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 28.371 ms |  1.7137 ms | 0.0939 ms |  0.93 |    0.01 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 24.881 ms | 11.2968 ms | 0.6192 ms |  0.82 |    0.02 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Dot**    | **384**  |  **48.34 ns** | **1.169 ns** | **0.064 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  51.27 ns | 0.878 ns | 0.048 ns |  1.06 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  |  **95.18 ns** | **3.706 ns** | **0.203 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  93.26 ns | 0.565 ns | 0.031 ns |  0.98 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **123.85 ns** | **1.940 ns** | **0.106 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 124.81 ns | 1.177 ns | 0.064 ns |  1.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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
| **Count**        | **200**       |  **2.824 ms** | **0.6283 ms** | **0.0344 ms** |  **1.00** |    **0.01** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.108 ms | 6.8355 ms | 0.3747 ms |  1.10 |    0.12 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.685 ms | 0.1307 ms | 0.0072 ms |  1.30 |    0.01 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.806 ms | 0.1367 ms | 0.0075 ms |  0.99 |    0.01 |  97.6563 |  31.2500 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.976 ms** | **0.8438 ms** | **0.0463 ms** |  **1.00** |    **0.01** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.168 ms | 1.4442 ms | 0.0792 ms |  1.03 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.466 ms | 1.4787 ms | 0.0811 ms |  1.64 |    0.01 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  6.851 ms | 0.5606 ms | 0.0307 ms |  0.98 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 115.9 | 26.9 | 4.31x C# faster |
| latin | 32 | 176.0 | 89.1 | 1.97x C# faster |
| latin | 128 | 473.2 | 878.3 | 1.86x Py faster |
| latin | 512 | 4457.7 | 7582.4 | 1.70x Py faster |
| cjk | 8 | 139.6 | 25.4 | 5.49x C# faster |
| cjk | 32 | 330.1 | 220.8 | 1.50x C# faster |
| cjk | 128 | 1904.6 | 1613.8 | 1.18x C# faster |
| cjk | 512 | 14851.8 | 10713.5 | 1.39x C# faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is Hyyro's bit-parallel LLCS above a pattern of 8 and a rolling-row dynamic program below it (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 157.0 | 17.5 | 8.96x C# faster |
| latin | 32 | 286.9 | 157.3 | 1.82x C# faster |
| latin | 128 | 1692.7 | 1536.7 | 1.10x C# faster |
| latin | 512 | 13959.7 | 16101.1 | 1.15x Py faster |
| cjk | 8 | 149.9 | 17.4 | 8.61x C# faster |
| cjk | 32 | 368.5 | 293.9 | 1.25x C# faster |
| cjk | 128 | 2829.0 | 2425.9 | 1.17x C# faster |
| cjk | 512 | 23586.7 | 19714.8 | 1.20x C# faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.949 | 102.78x | 0.009 | 0.949 | 102.78x |
| accuracy_n1000_k2 | 0.001 | 0.500 | 535.29x | 0.001 | 0.500 | 535.28x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.693 | 217.06x | 0.008 | 1.693 | 217.05x |
| classification_report_n1000_k2 | 0.010 | 6.459 | 618.77x | 0.010 | 6.459 | 618.77x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.861 | 117.93x | 0.016 | 1.861 | 117.93x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.014 | 133.17x | 0.008 | 1.014 | 133.16x |
| matthews_n1000_k2 | 0.008 | 1.901 | 249.98x | 0.008 | 1.901 | 249.99x |
| cohen_kappa_n1000_k2 | 0.008 | 1.057 | 138.63x | 0.008 | 1.056 | 138.61x |
| mse_n1000_k2 | 0.002 | 0.294 | 120.88x | 0.002 | 0.294 | 120.89x |
| mae_n1000_k2 | 0.002 | 0.292 | 120.11x | 0.002 | 0.292 | 120.11x |
| median_ae_n1000_k2 | 0.006 | 0.306 | 48.10x | 0.006 | 0.306 | 48.10x |
| r2_n1000_k2 | 0.003 | 0.354 | 137.36x | 0.003 | 0.354 | 137.36x |
| confusion_matrix_n1000_k10 | 0.009 | 0.959 | 101.24x | 0.009 | 0.959 | 101.24x |
| accuracy_n1000_k10 | 0.001 | 0.507 | 542.57x | 0.001 | 0.507 | 542.61x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.730 | 203.85x | 0.008 | 1.730 | 203.85x |
| classification_report_n1000_k10 | 0.015 | 6.723 | 444.78x | 0.015 | 6.722 | 444.79x |
| roc_auc_ovr_macro_n1000_k10 | 0.547 | 9.610 | 17.57x | 0.547 | 9.610 | 17.57x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.022 | 123.32x | 0.008 | 1.022 | 123.32x |
| matthews_n1000_k10 | 0.008 | 1.951 | 235.96x | 0.008 | 1.951 | 235.98x |
| cohen_kappa_n1000_k10 | 0.009 | 1.069 | 123.56x | 0.009 | 1.069 | 123.55x |
| mse_n1000_k10 | 0.002 | 0.295 | 122.31x | 0.002 | 0.295 | 122.31x |
| mae_n1000_k10 | 0.002 | 0.294 | 121.92x | 0.002 | 0.294 | 121.92x |
| median_ae_n1000_k10 | 0.006 | 0.307 | 48.75x | 0.006 | 0.307 | 48.75x |
| r2_n1000_k10 | 0.003 | 0.355 | 138.14x | 0.003 | 0.355 | 138.14x |
| confusion_matrix_n100000_k2 | 0.987 | 10.680 | 10.82x | 0.987 | 10.679 | 10.82x |
| accuracy_n100000_k2 | 0.174 | 3.734 | 21.49x | 0.174 | 3.734 | 21.49x |
| precision_recall_f1_macro_n100000_k2 | 0.871 | 12.218 | 14.03x | 0.871 | 12.217 | 14.03x |
| classification_report_n100000_k2 | 0.868 | 26.671 | 30.74x | 0.868 | 26.669 | 30.74x |
| roc_auc_binary_n100000_k2 | 3.602 | 26.486 | 7.35x | 3.602 | 26.484 | 7.35x |
| balanced_accuracy_n100000_k2 | 0.856 | 10.756 | 12.56x | 0.856 | 10.755 | 12.56x |
| matthews_n100000_k2 | 0.857 | 21.417 | 25.00x | 0.856 | 21.414 | 25.00x |
| cohen_kappa_n100000_k2 | 0.859 | 10.785 | 12.56x | 0.859 | 10.785 | 12.56x |
| mse_n100000_k2 | 0.240 | 0.456 | 1.90x | 0.240 | 0.456 | 1.90x |
| mae_n100000_k2 | 0.238 | 0.447 | 1.88x | 0.238 | 0.447 | 1.88x |
| median_ae_n100000_k2 | 0.758 | 1.791 | 2.36x | 0.779 | 1.791 | 2.30x |
| r2_n100000_k2 | 0.235 | 0.696 | 2.97x | 0.235 | 0.696 | 2.97x |
| confusion_matrix_n100000_k10 | 0.983 | 10.702 | 10.89x | 0.983 | 10.699 | 10.89x |
| accuracy_n100000_k10 | 0.275 | 3.735 | 13.58x | 0.275 | 3.735 | 13.58x |
| precision_recall_f1_macro_n100000_k10 | 0.992 | 12.854 | 12.96x | 0.991 | 12.853 | 12.96x |
| classification_report_n100000_k10 | 0.989 | 29.236 | 29.57x | 0.988 | 29.233 | 29.57x |
| roc_auc_ovr_macro_n100000_k10 | 37.475 | 213.974 | 5.71x | 37.473 | 213.939 | 5.71x |
| balanced_accuracy_n100000_k10 | 0.971 | 10.726 | 11.04x | 0.971 | 10.725 | 11.04x |
| matthews_n100000_k10 | 0.971 | 22.088 | 22.74x | 0.971 | 22.087 | 22.74x |
| cohen_kappa_n100000_k10 | 0.995 | 10.776 | 10.83x | 0.995 | 10.776 | 10.83x |
| mse_n100000_k10 | 0.243 | 0.448 | 1.84x | 0.243 | 0.448 | 1.84x |
| mae_n100000_k10 | 0.238 | 0.440 | 1.85x | 0.238 | 0.440 | 1.85x |
| median_ae_n100000_k10 | 0.783 | 1.782 | 2.28x | 0.829 | 1.782 | 2.15x |
| r2_n100000_k10 | 0.235 | 0.687 | 2.92x | 0.235 | 0.687 | 2.92x |
| confusion_matrix_n1000000_k2 | 8.666 | 99.736 | 11.51x | 8.665 | 99.711 | 11.51x |
| accuracy_n1000000_k2 | 1.832 | 32.941 | 17.98x | 1.832 | 32.936 | 17.98x |
| precision_recall_f1_macro_n1000000_k2 | 8.697 | 107.602 | 12.37x | 8.696 | 107.591 | 12.37x |
| classification_report_n1000000_k2 | 8.697 | 209.785 | 24.12x | 8.697 | 209.766 | 24.12x |
| roc_auc_binary_n1000000_k2 | 45.127 | 288.630 | 6.40x | 45.132 | 288.615 | 6.39x |
| balanced_accuracy_n1000000_k2 | 8.617 | 99.787 | 11.58x | 8.617 | 99.772 | 11.58x |
| matthews_n1000000_k2 | 8.629 | 200.245 | 23.21x | 8.629 | 200.225 | 23.20x |
| cohen_kappa_n1000000_k2 | 8.633 | 99.752 | 11.55x | 8.633 | 99.747 | 11.55x |
| mse_n1000000_k2 | 2.390 | 2.238 | 0.94x | 2.390 | 2.238 | 0.94x |
| mae_n1000000_k2 | 2.367 | 2.228 | 0.94x | 2.367 | 2.228 | 0.94x |
| median_ae_n1000000_k2 | 6.999 | 14.227 | 2.03x | 7.080 | 14.225 | 2.01x |
| r2_n1000000_k2 | 2.339 | 3.978 | 1.70x | 2.339 | 3.978 | 1.70x |
| confusion_matrix_n1000000_k10 | 9.813 | 99.701 | 10.16x | 9.813 | 99.697 | 10.16x |
| accuracy_n1000000_k10 | 2.722 | 33.037 | 12.14x | 2.722 | 33.035 | 12.14x |
| precision_recall_f1_macro_n1000000_k10 | 9.944 | 113.908 | 11.45x | 9.943 | 113.901 | 11.46x |
| classification_report_n1000000_k10 | 9.848 | 234.677 | 23.83x | 9.847 | 234.671 | 23.83x |
| balanced_accuracy_n1000000_k10 | 9.753 | 99.873 | 10.24x | 9.752 | 99.862 | 10.24x |
| matthews_n1000000_k10 | 9.775 | 207.704 | 21.25x | 9.774 | 207.689 | 21.25x |
| cohen_kappa_n1000000_k10 | 9.757 | 99.850 | 10.23x | 9.756 | 99.846 | 10.23x |
| mse_n1000000_k10 | 2.402 | 2.223 | 0.93x | 2.402 | 2.223 | 0.93x |
| mae_n1000000_k10 | 2.379 | 2.231 | 0.94x | 2.379 | 2.231 | 0.94x |
| median_ae_n1000000_k10 | 7.107 | 14.278 | 2.01x | 7.191 | 14.276 | 1.99x |
| r2_n1000000_k10 | 2.650 | 4.042 | 1.53x | 2.649 | 4.041 | 1.53x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.94x
  mae_n1000000_k2                  0.94x
  mse_n1000000_k10                 0.93x
  mae_n1000000_k10                 0.94x

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-22, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.808 | 9.628 | 2.00x | 4.978 | 9.627 | 1.93x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.292 | 16.297 | 1.33x | 12.705 | 16.296 | 1.28x | 706,526 | 706,526 |
| tokenizer_json_unigram | 12.828 | 36.296 | 2.83x | 13.270 | 36.293 | 2.74x | 1,990,038 | 1,990,038 |
| spiece_model | 4.535 | 28.079 | 6.19x | 4.684 | 28.077 | 5.99x | 533,084 | 533,084 |
| tfidf_save | 1.713 | 2.458 | 1.44x | 1.785 | 2.458 | 1.38x | 581,787 | 591,922 |
| tfidf_load | 4.703 | 3.933 | 0.84x | 4.848 | 3.933 | 0.81x | 581,787 | 591,922 |
| embedding_index_save | 6.598 | 1.406 | 0.21x | 7.123 | 1.406 | 0.20x | 20,589,007 | 15,360,128 |
| embedding_index_load | 5.738 | 1.405 | 0.24x | 6.456 | 1.405 | 0.22x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 6.397 | 0.888 | 0.14x | 7.071 | 0.886 | 0.13x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 4.165 | 1.304 | 0.31x | 4.677 | 1.304 | 0.28x | 20,589,007 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 84.11x | 0.000 | 0.001 | 84.12x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 408.730 | 552.577 | 1.35x | 410.401 | 552.503 | 1.35x | 15,251,458 | 14,022,374 |
| embedding_index_load_gzip | 77.277 | 66.367 | 0.86x | 78.379 | 66.359 | 0.85x | 15,251,458 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
