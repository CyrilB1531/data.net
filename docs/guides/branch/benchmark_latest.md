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

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **UnitLoop**           | **1**          |     **5.753 μs** |  **0.2138 μs** | **0.0117 μs** |  **1.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     5.859 μs |  0.1179 μs | 0.0065 μs |  1.02 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     5.852 μs |  0.2253 μs | 0.0123 μs |  1.02 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **8**          |   **101.107 μs** |  **3.1233 μs** | **0.1712 μs** |  **1.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    66.217 μs |  4.9716 μs | 0.2725 μs |  0.65 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    67.652 μs |  3.0497 μs | 0.1672 μs |  0.67 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **32**         |   **378.873 μs** |  **8.9091 μs** | **0.4883 μs** |  **1.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   229.811 μs | 31.7792 μs | 1.7419 μs |  0.61 |  26.8555 |  1.7090 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   226.170 μs | 29.1973 μs | 1.6004 μs |  0.60 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **128**        | **1,496.474 μs** | **49.0731 μs** | **2.6899 μs** |  **1.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   944.253 μs | 28.9268 μs | 1.5856 μs |  0.63 | 107.4219 | 15.6250 | 1764.41 KB |        0.94 |
| EmbedBatchBucketed | 128        |   889.197 μs | 81.2569 μs | 4.4540 μs |  0.59 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| Unigram | 590.1 ms |  3.81 ms | 0.21 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 564.0 ms | 33.36 ms | 1.83 ms |  0.96 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **BpeOnOnePathologicalToken** | **512**    |   **103.4 μs** |   **4.71 μs** | **0.26 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **210.2 μs** |  **20.47 μs** | **1.12 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **503.1 μs** |  **33.29 μs** | **1.82 μs** | **3.9063** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,052.3 μs** | **100.04 μs** | **5.48 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| DpGroup    |  13.60 μs | 0.182 μs | 0.010 μs |         - |
| MyersGroup | 115.25 μs | 1.710 μs | 0.094 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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

| Method         | Mean        | Error       | StdDev   | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |------------:|------------:|---------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |    108.9 ns |     0.39 ns |  0.02 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 18,124.4 ns | 1,329.36 ns | 72.87 ns | 166.47 |    0.58 |      - |         - |          NA |
| TokenSortRatio |    975.5 ns |   672.18 ns | 36.84 ns |   8.96 |    0.29 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,272.0 ns |   390.04 ns | 21.38 ns |  30.05 |    0.17 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,611.9 ns |   749.66 ns | 41.09 ns |  42.36 |    0.33 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **Distance_Utf16**             | **8**      |      **29.18 ns** |      **2.310 ns** |     **0.127 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     132.88 ns |     18.920 ns |     1.037 ns |  4.55 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      33.37 ns |      4.863 ns |     0.267 ns |  1.14 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      34.43 ns |      0.737 ns |     0.040 ns |  1.18 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **32.50 ns** |      **1.371 ns** |     **0.075 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     137.97 ns |     17.681 ns |     0.969 ns |  4.25 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      33.40 ns |      0.195 ns |     0.011 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      32.04 ns |      2.800 ns |     0.153 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **34.82 ns** |      **1.880 ns** |     **0.103 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     149.25 ns |     22.281 ns |     1.221 ns |  4.29 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      36.60 ns |      0.883 ns |     0.048 ns |  1.05 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      35.12 ns |      1.092 ns |     0.060 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **90.51 ns** |      **8.748 ns** |     **0.479 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     191.35 ns |      1.441 ns |     0.079 ns |  2.11 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      91.75 ns |     28.886 ns |     1.583 ns |  1.01 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      91.58 ns |     21.108 ns |     1.157 ns |  1.01 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **61.26 ns** |      **4.809 ns** |     **0.264 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     852.25 ns |    228.487 ns |    12.524 ns | 13.91 |    0.18 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      57.40 ns |      0.484 ns |     0.027 ns |  0.94 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      62.14 ns |      2.003 ns |     0.110 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **66.75 ns** |      **3.090 ns** |     **0.169 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,044.98 ns |     46.698 ns |     2.560 ns | 15.65 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      68.96 ns |      1.587 ns |     0.087 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      64.84 ns |      1.186 ns |     0.065 ns |  0.97 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,221.46 ns** |     **25.644 ns** |     **1.406 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  18,266.81 ns |  5,425.974 ns |   297.416 ns | 14.95 |    0.21 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,293.72 ns |    105.890 ns |     5.804 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,225.25 ns |     18.053 ns |     0.990 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **8,892.22 ns** |     **10.202 ns** |     **0.559 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 302,355.73 ns | 39,775.616 ns | 2,180.236 ns | 34.00 |    0.21 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   9,117.57 ns |    140.651 ns |     7.710 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   8,785.32 ns |    297.786 ns |    16.323 ns |  0.99 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **Dp**     | **8**    |    **130.87 ns** |    **15.258 ns** |   **0.836 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 8    |     65.77 ns |     1.490 ns |   0.082 ns |  0.50 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **12**   |    **220.90 ns** |     **5.399 ns** |   **0.296 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 12   |     69.69 ns |     1.464 ns |   0.080 ns |  0.32 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **14**   |    **280.57 ns** |     **6.972 ns** |   **0.382 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 14   |     72.33 ns |     0.699 ns |   0.038 ns |  0.26 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **16**   |    **368.28 ns** |   **332.093 ns** |  **18.203 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel | 16   |     80.51 ns |     5.323 ns |   0.292 ns |  0.22 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **18**   |    **710.95 ns** |   **336.131 ns** |  **18.424 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 18   |     83.50 ns |     0.758 ns |   0.042 ns |  0.12 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **20**   |    **814.62 ns** |    **88.244 ns** |   **4.837 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 20   |     87.40 ns |     0.592 ns |   0.032 ns |  0.11 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **24**   |    **997.09 ns** |    **80.960 ns** |   **4.438 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 24   |     96.02 ns |     1.726 ns |   0.095 ns |  0.10 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **32**   |  **1,649.07 ns** |   **576.951 ns** |  **31.625 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 32   |    116.73 ns |     9.153 ns |   0.502 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **48**   |  **3,383.91 ns** |   **173.267 ns** |   **9.497 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 48   |    143.68 ns |    11.214 ns |   0.615 ns |  0.04 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **64**   |  **5,376.97 ns** |   **533.280 ns** |  **29.231 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 64   |    171.84 ns |     2.979 ns |   0.163 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **96**   | **11,281.69 ns** | **4,072.730 ns** | **223.240 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 96   |  1,006.98 ns |     5.415 ns |   0.297 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **Distance_Utf16**             | **8**      |     **28.34 ns** |     **2.566 ns** |   **0.141 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    127.76 ns |    22.673 ns |   1.243 ns |  4.51 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     28.11 ns |     0.698 ns |   0.038 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **301.32 ns** |    **13.537 ns** |   **0.742 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    707.63 ns |     3.437 ns |   0.188 ns |  2.35 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    304.38 ns |     2.279 ns |   0.125 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    | **15,064.20 ns** | **2,613.179 ns** | **143.237 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 18,817.62 ns |   738.376 ns |  40.473 ns |  1.25 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 16,155.88 ns |   938.132 ns |  51.422 ns |  1.07 |    0.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **Distance_CodePoint** | **16**     | **32**       |       **354.2 ns** |       **9.78 ns** |      **0.54 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,512.7 ns |      82.19 ns |      4.51 ns |   4.27 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **405.4 ns** |      **16.86 ns** |      **0.92 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,494.7 ns |      47.95 ns |      2.63 ns |   3.69 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **440.9 ns** |       **3.94 ns** |      **0.22 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,426.4 ns |      44.21 ns |      2.42 ns |   7.77 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **446.9 ns** |      **67.18 ns** |      **3.68 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,511.5 ns |     262.50 ns |     14.39 ns |   7.86 |    0.06 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **550.5 ns** |      **29.33 ns** |      **1.61 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,034.9 ns |      40.62 ns |      2.23 ns |  10.96 |    0.03 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **544.5 ns** |       **4.61 ns** |      **0.25 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,202.4 ns |      49.02 ns |      2.69 ns |  11.39 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **650.0 ns** |      **16.64 ns** |      **0.91 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |     9,556.1 ns |      71.92 ns |      3.94 ns |  14.70 |    0.02 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **631.4 ns** |      **42.88 ns** |      **2.35 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |     9,540.3 ns |      76.82 ns |      4.21 ns |  15.11 |    0.05 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,409.3 ns** |     **167.65 ns** |      **9.19 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   103,324.4 ns |  86,292.77 ns |  4,730.00 ns |  42.89 |    1.71 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,409.2 ns** |     **287.42 ns** |     **15.75 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   103,110.6 ns |   3,728.10 ns |    204.35 ns |  42.80 |    0.25 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **20,580.6 ns** |     **580.03 ns** |     **31.79 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,441,203.1 ns | 190,202.74 ns | 10,425.65 ns | 118.62 |    0.47 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **446,646.6 ns** |  **36,278.81 ns** |  **1,988.56 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,797,247.7 ns | 350,212.61 ns | 19,196.34 ns |   4.02 |    0.04 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **Matrix**         | **1000**    | **2**       |      **7.310 μs** |   **0.2595 μs** |  **0.0142 μs** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7.125 μs |   0.2543 μs |  0.0139 μs | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |      1.030 μs |   0.0028 μs |  0.0002 μs |      - |         - |
| F1Macro        | 1000    | 2       |      7.447 μs |   0.1864 μs |  0.0102 μs | 0.0229 |     472 B |
| Report         | 1000    | 2       |     10.139 μs |   0.0748 μs |  0.0041 μs | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7.470 μs** |   **0.1847 μs** |  **0.0101 μs** | **0.0687** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7.620 μs |   0.1735 μs |  0.0095 μs | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |      1.031 μs |   0.0263 μs |  0.0014 μs |      - |         - |
| F1Macro        | 1000    | 10      |      7.874 μs |   0.6027 μs |  0.0330 μs | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     14.358 μs |   0.6558 μs |  0.0359 μs | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **804.073 μs** |  **94.4846 μs** |  **5.1790 μs** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    820.343 μs | 257.6679 μs | 14.1236 μs |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    135.162 μs |  24.6404 μs |  1.3506 μs |      - |         - |
| F1Macro        | 100000  | 2       |    848.743 μs | 118.7908 μs |  6.5113 μs |      - |     473 B |
| Report         | 100000  | 2       |    850.714 μs | 291.5342 μs | 15.9800 μs |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **998.956 μs** | **320.2999 μs** | **17.5567 μs** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    950.471 μs |  62.1568 μs |  3.4070 μs |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    244.485 μs |  40.7266 μs |  2.2324 μs |      - |         - |
| F1Macro        | 100000  | 10      |    967.903 μs |  47.7618 μs |  2.6180 μs |      - |    1665 B |
| Report         | 100000  | 10      |    965.579 μs |  50.5080 μs |  2.7685 μs |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,579.783 μs** | **601.9185 μs** | **32.9932 μs** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,651.642 μs | 752.6098 μs | 41.2531 μs |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,917.849 μs |  24.9448 μs |  1.3673 μs |      - |         - |
| F1Macro        | 1000000 | 2       |  8,700.216 μs | 139.6389 μs |  7.6541 μs |      - |     484 B |
| Report         | 1000000 | 2       |  8,800.480 μs | 362.6687 μs | 19.8791 μs |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **10,027.753 μs** | **373.0899 μs** | **20.4503 μs** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 10,152.890 μs | 380.9515 μs | 20.8812 μs |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  3,114.792 μs | 224.7743 μs | 12.3206 μs |      - |         - |
| F1Macro        | 1000000 | 10      | 10,312.299 μs | 478.2462 μs | 26.2143 μs |      - |    1676 B |
| Report         | 1000000 | 10      | 10,632.870 μs | 379.5529 μs | 20.8046 μs |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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

| Method | Band | Mean         | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |-------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| **Dp**     | **4**    |     **82.47 ns** |   **1.686 ns** |  **0.092 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 4    |     82.01 ns |   3.963 ns |  0.217 ns |  0.99 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **6**    |    **122.37 ns** |   **3.171 ns** |  **0.174 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 6    |    119.66 ns |  14.870 ns |  0.815 ns |  0.98 |    0.01 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **8**    |    **157.30 ns** |   **4.200 ns** |  **0.230 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |    100.34 ns |   2.186 ns |  0.120 ns |  0.64 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **10**   |    **207.24 ns** |   **4.333 ns** |  **0.237 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 10   |    111.01 ns |   0.705 ns |  0.039 ns |  0.54 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **12**   |    **270.56 ns** |   **2.430 ns** |  **0.133 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 12   |    119.40 ns |   2.561 ns |  0.140 ns |  0.44 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **16**   |    **432.35 ns** | **118.983 ns** |  **6.522 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 16   |    139.95 ns |   2.508 ns |  0.137 ns |  0.32 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **24**   |    **864.22 ns** |  **28.109 ns** |  **1.541 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 24   |    179.09 ns |  10.349 ns |  0.567 ns |  0.21 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **32**   |  **1,480.86 ns** |   **3.331 ns** |  **0.183 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    221.01 ns |  23.211 ns |  1.272 ns |  0.15 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **48**   |  **3,252.53 ns** |  **86.564 ns** |  **4.745 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 48   |    402.70 ns |  57.644 ns |  3.160 ns |  0.12 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **64**   |  **5,667.65 ns** | **512.220 ns** | **28.076 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 64   |    404.99 ns |  22.736 ns |  1.246 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **96**   | **12,549.36 ns** | **195.267 ns** | **10.703 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 96   |  1,095.34 ns |  36.140 ns |  1.981 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| VocabTxt               |  4.230 ms | 2.3850 ms | 0.1307 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.307 ms | 2.1535 ms | 0.1180 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 11.119 ms | 0.4958 ms | 0.0272 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.985 ms | 3.1324 ms | 0.1717 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.958 ms | 0.4443 ms | 0.0244 ms |  27.3438 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              |  4.532 ms | 1.4149 ms | 0.0776 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  5.248 ms | 0.4735 ms | 0.0260 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  5.322 ms | 4.5765 ms | 0.2509 ms | 500.0000 | 468.7500 | 437.5000 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **Count**                | **200**       |  **7.430 ms** | **0.8143 ms** | **0.0446 ms** |  **1.00** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.175 ms | 0.2384 ms | 0.0131 ms |  0.83 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.435 ms | 1.0892 ms | 0.0597 ms |  1.00 |  500.0000 |  156.2500 |  62.5000 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.392 ms | 0.4831 ms | 0.0265 ms |  0.86 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |           |          |           |             |
| **Count**                | **1000**      | **30.213 ms** | **1.0928 ms** | **0.0599 ms** |  **1.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 23.323 ms | 3.5301 ms | 0.1935 ms |  0.77 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 28.924 ms | 3.3192 ms | 0.1819 ms |  0.96 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 24.691 ms | 0.8228 ms | 0.0451 ms |  0.82 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **Dot**    | **384**  |  **51.01 ns** | **2.910 ns** | **0.160 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.66 ns | 1.552 ns | 0.085 ns |  0.95 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  |  **99.28 ns** | **2.752 ns** | **0.151 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  91.69 ns | 0.887 ns | 0.049 ns |  0.92 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **132.52 ns** | **2.203 ns** | **0.121 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 125.10 ns | 0.671 ns | 0.037 ns |  0.94 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

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
| **Count**        | **200**       |  **3.147 ms** | **6.4446 ms** | **0.3533 ms** |  **1.01** |    **0.13** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.124 ms | 3.8213 ms | 0.2095 ms |  1.00 |    0.11 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.821 ms | 0.5585 ms | 0.0306 ms |  1.22 |    0.11 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  3.036 ms | 1.8217 ms | 0.0999 ms |  0.97 |    0.09 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.983 ms** | **0.7441 ms** | **0.0408 ms** |  **1.00** |    **0.01** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.210 ms | 0.6349 ms | 0.0348 ms |  1.03 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.567 ms | 0.2385 ms | 0.0131 ms |  1.66 |    0.01 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.009 ms | 0.8866 ms | 0.0486 ms |  1.00 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 112.5 | 23.9 | 4.70x C# faster |
| 32 | 157.3 | 75.0 | 2.10x C# faster |
| 128 | 476.3 | 769.4 | 1.62x Py faster |
| 512 | 4865.7 | 7984.6 | 1.64x Py faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 139.8 | 18.4 | 7.60x C# faster |
| 32 | 259.1 | 133.0 | 1.95x C# faster |
| 128 | 1827.6 | 1550.8 | 1.18x C# faster |
| 512 | 15757.8 | 16229.4 | 1.03x Py faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.777 | 87.20x | 0.009 | 0.777 | 87.20x |
| accuracy_n1000_k2 | 0.001 | 0.408 | 388.22x | 0.001 | 0.408 | 388.17x |
| precision_recall_f1_macro_n1000_k2 | 0.007 | 1.393 | 187.30x | 0.007 | 1.393 | 187.31x |
| classification_report_n1000_k2 | 0.010 | 5.305 | 534.98x | 0.010 | 5.305 | 534.96x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.559 | 96.04x | 0.016 | 1.558 | 96.03x |
| balanced_accuracy_n1000_k2 | 0.007 | 0.826 | 113.53x | 0.007 | 0.826 | 113.54x |
| matthews_n1000_k2 | 0.007 | 1.570 | 218.05x | 0.007 | 1.570 | 218.03x |
| cohen_kappa_n1000_k2 | 0.007 | 0.877 | 121.42x | 0.007 | 0.877 | 121.42x |
| mse_n1000_k2 | 0.003 | 0.219 | 79.91x | 0.003 | 0.219 | 79.91x |
| mae_n1000_k2 | 0.003 | 0.219 | 79.78x | 0.003 | 0.219 | 79.78x |
| median_ae_n1000_k2 | 0.006 | 0.234 | 39.33x | 0.006 | 0.234 | 39.33x |
| r2_n1000_k2 | 0.003 | 0.276 | 100.92x | 0.003 | 0.276 | 100.91x |
| confusion_matrix_n1000_k10 | 0.009 | 0.783 | 83.37x | 0.009 | 0.783 | 83.36x |
| accuracy_n1000_k10 | 0.001 | 0.410 | 388.27x | 0.001 | 0.410 | 388.30x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.439 | 181.39x | 0.008 | 1.439 | 181.41x |
| classification_report_n1000_k10 | 0.014 | 5.537 | 384.34x | 0.014 | 5.537 | 384.35x |
| roc_auc_ovr_macro_n1000_k10 | 0.551 | 8.185 | 14.87x | 0.551 | 8.184 | 14.87x |
| balanced_accuracy_n1000_k10 | 0.008 | 0.842 | 105.60x | 0.008 | 0.842 | 105.60x |
| matthews_n1000_k10 | 0.008 | 1.605 | 202.63x | 0.008 | 1.605 | 202.62x |
| cohen_kappa_n1000_k10 | 0.008 | 0.885 | 105.86x | 0.008 | 0.885 | 105.87x |
| mse_n1000_k10 | 0.003 | 0.220 | 80.54x | 0.003 | 0.220 | 80.54x |
| mae_n1000_k10 | 0.003 | 0.218 | 79.84x | 0.003 | 0.218 | 79.83x |
| median_ae_n1000_k10 | 0.006 | 0.233 | 39.12x | 0.006 | 0.233 | 39.12x |
| r2_n1000_k10 | 0.003 | 0.275 | 100.76x | 0.003 | 0.275 | 100.75x |
| confusion_matrix_n100000_k2 | 1.140 | 10.965 | 9.62x | 1.140 | 10.963 | 9.61x |
| accuracy_n100000_k2 | 0.106 | 3.783 | 35.73x | 0.106 | 3.782 | 35.73x |
| precision_recall_f1_macro_n100000_k2 | 0.860 | 12.564 | 14.61x | 0.860 | 12.562 | 14.61x |
| classification_report_n100000_k2 | 0.868 | 26.809 | 30.89x | 0.868 | 26.807 | 30.89x |
| roc_auc_binary_n100000_k2 | 2.965 | 28.433 | 9.59x | 2.965 | 28.430 | 9.59x |
| balanced_accuracy_n100000_k2 | 0.860 | 11.049 | 12.85x | 0.860 | 11.048 | 12.85x |
| matthews_n100000_k2 | 0.847 | 22.065 | 26.04x | 0.847 | 22.063 | 26.04x |
| cohen_kappa_n100000_k2 | 0.845 | 11.081 | 13.11x | 0.845 | 11.079 | 13.11x |
| mse_n100000_k2 | 0.268 | 0.370 | 1.38x | 0.268 | 0.370 | 1.38x |
| mae_n100000_k2 | 0.268 | 0.371 | 1.38x | 0.268 | 0.371 | 1.38x |
| median_ae_n100000_k2 | 0.691 | 1.883 | 2.73x | 0.711 | 1.883 | 2.65x |
| r2_n100000_k2 | 0.260 | 0.583 | 2.24x | 0.260 | 0.583 | 2.24x |
| confusion_matrix_n100000_k10 | 0.959 | 10.949 | 11.42x | 0.959 | 10.944 | 11.41x |
| accuracy_n100000_k10 | 0.106 | 3.791 | 35.79x | 0.106 | 3.790 | 35.78x |
| precision_recall_f1_macro_n100000_k10 | 1.011 | 13.259 | 13.11x | 1.011 | 13.256 | 13.11x |
| classification_report_n100000_k10 | 1.001 | 29.730 | 29.69x | 1.001 | 29.724 | 29.69x |
| roc_auc_ovr_macro_n100000_k10 | 31.932 | 231.406 | 7.25x | 31.929 | 231.390 | 7.25x |
| balanced_accuracy_n100000_k10 | 0.978 | 11.019 | 11.27x | 0.978 | 11.018 | 11.27x |
| matthews_n100000_k10 | 0.940 | 22.846 | 24.29x | 0.940 | 22.843 | 24.29x |
| cohen_kappa_n100000_k10 | 0.984 | 11.059 | 11.24x | 0.984 | 11.058 | 11.24x |
| mse_n100000_k10 | 0.269 | 0.374 | 1.39x | 0.269 | 0.374 | 1.39x |
| mae_n100000_k10 | 0.269 | 0.374 | 1.39x | 0.269 | 0.374 | 1.39x |
| median_ae_n100000_k10 | 0.696 | 1.887 | 2.71x | 0.738 | 1.887 | 2.56x |
| r2_n100000_k10 | 0.261 | 0.583 | 2.23x | 0.261 | 0.583 | 2.23x |
| confusion_matrix_n1000000_k2 | 8.680 | 102.883 | 11.85x | 8.680 | 102.872 | 11.85x |
| accuracy_n1000000_k2 | 2.122 | 34.231 | 16.13x | 2.122 | 34.222 | 16.13x |
| precision_recall_f1_macro_n1000000_k2 | 8.788 | 112.792 | 12.84x | 8.785 | 112.775 | 12.84x |
| classification_report_n1000000_k2 | 8.788 | 219.960 | 25.03x | 8.787 | 219.930 | 25.03x |
| roc_auc_binary_n1000000_k2 | 41.967 | 313.477 | 7.47x | 41.959 | 313.450 | 7.47x |
| balanced_accuracy_n1000000_k2 | 8.769 | 102.945 | 11.74x | 8.768 | 102.940 | 11.74x |
| matthews_n1000000_k2 | 8.682 | 207.525 | 23.90x | 8.682 | 207.474 | 23.90x |
| cohen_kappa_n1000000_k2 | 8.685 | 102.934 | 11.85x | 8.685 | 102.921 | 11.85x |
| mse_n1000000_k2 | 2.675 | 1.908 | 0.71x | 2.675 | 1.907 | 0.71x |
| mae_n1000000_k2 | 2.678 | 1.888 | 0.70x | 2.677 | 1.887 | 0.70x |
| median_ae_n1000000_k2 | 6.346 | 15.570 | 2.45x | 6.391 | 15.569 | 2.44x |
| r2_n1000000_k2 | 2.603 | 3.526 | 1.35x | 2.603 | 3.526 | 1.35x |
| confusion_matrix_n1000000_k10 | 10.110 | 102.588 | 10.15x | 10.109 | 102.573 | 10.15x |
| accuracy_n1000000_k10 | 3.345 | 34.245 | 10.24x | 3.345 | 34.243 | 10.24x |
| precision_recall_f1_macro_n1000000_k10 | 10.303 | 119.650 | 11.61x | 10.303 | 119.635 | 11.61x |
| classification_report_n1000000_k10 | 10.396 | 247.335 | 23.79x | 10.394 | 247.301 | 23.79x |
| balanced_accuracy_n1000000_k10 | 10.300 | 102.748 | 9.98x | 10.299 | 102.729 | 9.97x |
| matthews_n1000000_k10 | 10.078 | 214.724 | 21.31x | 10.078 | 214.691 | 21.30x |
| cohen_kappa_n1000000_k10 | 10.048 | 102.815 | 10.23x | 10.048 | 102.795 | 10.23x |
| mse_n1000000_k10 | 2.679 | 1.901 | 0.71x | 2.679 | 1.901 | 0.71x |
| mae_n1000000_k10 | 2.681 | 1.887 | 0.70x | 2.680 | 1.886 | 0.70x |
| median_ae_n1000000_k10 | 6.430 | 15.575 | 2.42x | 6.538 | 15.571 | 2.38x |
| r2_n1000000_k10 | 3.054 | 3.590 | 1.18x | 3.054 | 3.589 | 1.18x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.71x
  mae_n1000000_k2                  0.70x
  mse_n1000000_k10                 0.71x
  mae_n1000000_k10                 0.70x

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-20, measured at commit `654dd1137a9c3e200799f9badc0b4892c800ca04`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.540 | 9.821 | 2.16x | 5.338 | 9.820 | 1.84x |
| tokenizer_json_wordpiece | 12.471 | 17.496 | 1.40x | 12.935 | 17.494 | 1.35x |
| tokenizer_json_unigram | 10.955 | 39.118 | 3.57x | 11.302 | 39.113 | 3.46x |
| spiece_model | 5.320 | 31.176 | 5.86x | 5.548 | 31.170 | 5.62x |
| tfidf_save | 1.991 | 2.516 | 1.26x | 2.146 | 2.516 | 1.17x |
| tfidf_load | 4.388 | 4.446 | 1.01x | 4.691 | 4.446 | 0.95x |
| embedding_index_save | 5.071 | 4.002 | 0.79x | 5.735 | 4.002 | 0.70x |
| embedding_index_load | 4.610 | 1.308 | 0.28x | 4.979 | 1.308 | 0.26x |
| embedding_index_load_file | 5.647 | 0.788 | 0.14x | 6.181 | 0.788 | 0.13x |
| embedding_index_load_memory | 3.637 | 1.307 | 0.36x | 4.144 | 1.307 | 0.32x |
| embedding_index_view_floor | 0.000 | 0.001 | 66.02x | 0.000 | 0.001 | 66.03x |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.

<!-- markdownlint-enable MD060 -->
