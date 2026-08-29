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

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| **UnitLoop**           | **1**          |     **6.116 μs** |   **1.9257 μs** |  **0.1056 μs** |  **1.00** |    **0.02** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.124 μs |   0.4333 μs |  0.0238 μs |  1.00 |    0.02 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.027 μs |   1.1677 μs |  0.0640 μs |  0.99 |    0.02 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **8**          |   **104.006 μs** |   **8.1904 μs** |  **0.4489 μs** |  **1.00** |    **0.01** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    67.478 μs |   7.2854 μs |  0.3993 μs |  0.65 |    0.00 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    66.968 μs |   0.2331 μs |  0.0128 μs |  0.64 |    0.00 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **32**         |   **392.130 μs** |  **65.5968 μs** |  **3.5956 μs** |  **1.00** |    **0.01** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   235.709 μs |   2.8734 μs |  0.1575 μs |  0.60 |    0.00 |  26.8555 |  1.7090 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   228.846 μs |  50.5144 μs |  2.7689 μs |  0.58 |    0.01 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **128**        | **1,650.076 μs** | **942.1597 μs** | **51.6430 μs** |  **1.00** |    **0.04** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   947.578 μs | 261.8425 μs | 14.3525 μs |  0.57 |    0.02 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   889.771 μs | 277.1729 μs | 15.1928 μs |  0.54 |    0.02 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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

| Method | length | Mean          | Error      | StdDev    | Allocated |
|------- |------- |--------------:|-----------:|----------:|----------:|
| **Latin**  | **1000**   |      **55.80 μs** |   **2.981 μs** |  **0.163 μs** |         **-** |
| Cjk    | 1000   |      55.72 μs |   1.466 μs |  0.080 μs |         - |
| **Latin**  | **10000**  |   **5,468.33 μs** |  **56.622 μs** |  **3.104 μs** |         **-** |
| Cjk    | 10000  |   6,701.07 μs | 182.249 μs |  9.990 μs |         - |
| **Latin**  | **65536**  | **201,640.66 μs** | **931.172 μs** | **51.041 μs** |         **-** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github (run 2)

_As of 2026-08-27, measured at commit `2483f1a00691271083f00baf3835de96bf0a4076`._

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

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| Unigram | 596.6 ms | 25.20 ms | 1.38 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 572.3 ms | 89.78 ms | 4.92 ms |  0.96 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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

| Method                    | Length | Mean     | Error     | StdDev  | Gen0   | Allocated |
|-------------------------- |------- |---------:|----------:|--------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    | **104.8 μs** |   **2.49 μs** | **0.14 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **219.6 μs** |  **14.24 μs** | **0.78 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **473.7 μs** |  **12.60 μs** | **0.69 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **997.0 μs** | **124.91 μs** | **6.85 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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

| Method     | Alphabet | Mean      | Error     | StdDev   | Allocated |
|----------- |--------- |----------:|----------:|---------:|----------:|
| **DpGroup**    | **cjk**      |  **20.16 μs** |  **0.362 μs** | **0.020 μs** |         **-** |
| MyersGroup | cjk      | 241.63 μs | 17.990 μs | 0.986 μs |         - |
| **DpGroup**    | **latin**    |  **10.09 μs** |  **0.901 μs** | **0.049 μs** |         **-** |
| MyersGroup | latin    | 132.19 μs | 10.769 μs | 0.590 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| Ratio          |     99.14 ns |   2.862 ns |  0.157 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 12,680.05 ns | 740.890 ns | 40.611 ns | 127.90 |    0.40 |      - |         - |          NA |
| TokenSortRatio |  1,103.06 ns | 346.026 ns | 18.967 ns |  11.13 |    0.17 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,416.12 ns | 369.980 ns | 20.280 ns |  34.46 |    0.18 | 0.3433 |    5760 B |          NA |
| WRatio         |  5,041.11 ns | 509.989 ns | 27.954 ns |  50.85 |    0.25 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| **Distance_Utf16**             | **8**      |      **26.60 ns** |      **1.891 ns** |     **0.104 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     137.98 ns |      8.845 ns |     0.485 ns |  5.19 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      27.09 ns |      1.180 ns |     0.065 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.59 ns |      0.267 ns |     0.015 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.28 ns** |      **0.423 ns** |     **0.023 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     146.58 ns |      1.957 ns |     0.107 ns |  5.18 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.54 ns |      1.347 ns |     0.074 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      27.84 ns |      0.442 ns |     0.024 ns |  0.98 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **31.87 ns** |      **1.775 ns** |     **0.097 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     164.58 ns |      8.202 ns |     0.450 ns |  5.16 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.36 ns |      1.043 ns |     0.057 ns |  0.98 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      29.95 ns |      1.806 ns |     0.099 ns |  0.94 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **35.70 ns** |     **16.134 ns** |     **0.884 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     191.97 ns |     46.746 ns |     2.562 ns |  5.38 |    0.13 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      34.74 ns |      0.817 ns |     0.045 ns |  0.97 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      33.10 ns |      3.964 ns |     0.217 ns |  0.93 |    0.02 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **54.56 ns** |      **4.695 ns** |     **0.257 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     684.03 ns |    281.185 ns |    15.413 ns | 12.54 |    0.25 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      56.00 ns |     17.134 ns |     0.939 ns |  1.03 |    0.02 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      53.83 ns |     13.141 ns |     0.720 ns |  0.99 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **61.06 ns** |      **2.511 ns** |     **0.138 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,017.06 ns |     15.731 ns |     0.862 ns | 16.66 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      64.88 ns |      0.801 ns |     0.044 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      58.93 ns |      6.525 ns |     0.358 ns |  0.97 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **932.58 ns** |      **4.420 ns** |     **0.242 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  21,796.18 ns |  4,087.163 ns |   224.031 ns | 23.37 |    0.21 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     931.70 ns |     13.199 ns |     0.723 ns |  1.00 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     935.24 ns |     17.444 ns |     0.956 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **7,681.63 ns** |     **31.397 ns** |     **1.721 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 355,598.25 ns | 22,762.020 ns | 1,247.663 ns | 46.29 |    0.14 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   7,340.72 ns |     52.039 ns |     2.852 ns |  0.96 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   7,356.66 ns |    189.725 ns |    10.399 ns |  0.96 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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

| Method     | Band | Mean         | Error         | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|--------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **8**    |    **131.42 ns** |      **1.460 ns** |   **0.080 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     55.54 ns |      0.864 ns |   0.047 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    131.37 ns |      7.129 ns |   0.391 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |     97.03 ns |      0.359 ns |   0.020 ns |  0.74 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **12**   |    **216.17 ns** |     **15.035 ns** |   **0.824 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |     62.90 ns |      2.012 ns |   0.110 ns |  0.29 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    232.04 ns |    177.192 ns |   9.712 ns |  1.07 |    0.04 |         - |          NA |
| Kernel_Cjk | 12   |    108.60 ns |      1.157 ns |   0.063 ns |  0.50 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **14**   |    **277.26 ns** |    **318.931 ns** |  **17.482 ns** |  **1.00** |    **0.08** |         **-** |          **NA** |
| Kernel     | 14   |     67.05 ns |      1.333 ns |   0.073 ns |  0.24 |    0.01 |         - |          NA |
| Dp_Cjk     | 14   |    277.10 ns |    222.075 ns |  12.173 ns |  1.00 |    0.07 |         - |          NA |
| Kernel_Cjk | 14   |    113.12 ns |      4.826 ns |   0.265 ns |  0.41 |    0.02 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **16**   |    **356.19 ns** |    **113.313 ns** |   **6.211 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 16   |     72.59 ns |      7.587 ns |   0.416 ns |  0.20 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    358.02 ns |    296.356 ns |  16.244 ns |  1.01 |    0.04 |         - |          NA |
| Kernel_Cjk | 16   |    120.78 ns |      5.998 ns |   0.329 ns |  0.34 |    0.01 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **18**   |    **436.30 ns** |     **29.584 ns** |   **1.622 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 18   |     74.01 ns |      2.736 ns |   0.150 ns |  0.17 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    447.47 ns |    408.027 ns |  22.365 ns |  1.03 |    0.04 |         - |          NA |
| Kernel_Cjk | 18   |    124.29 ns |      1.564 ns |   0.086 ns |  0.28 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **20**   |    **770.29 ns** |     **12.790 ns** |   **0.701 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 20   |     81.06 ns |     39.754 ns |   2.179 ns |  0.11 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    772.26 ns |      8.053 ns |   0.441 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 20   |    133.24 ns |      9.798 ns |   0.537 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **24**   |    **972.51 ns** |    **461.223 ns** |  **25.281 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 24   |     86.14 ns |      3.576 ns |   0.196 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    991.58 ns |    255.600 ns |  14.010 ns |  1.02 |    0.03 |         - |          NA |
| Kernel_Cjk | 24   |    145.35 ns |      6.812 ns |   0.373 ns |  0.15 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **32**   |  **1,564.64 ns** |     **47.507 ns** |   **2.604 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    106.07 ns |      3.836 ns |   0.210 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,720.22 ns |     80.595 ns |   4.418 ns |  1.10 |    0.00 |         - |          NA |
| Kernel_Cjk | 32   |    164.81 ns |      2.323 ns |   0.127 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **48**   |  **3,227.18 ns** |     **48.033 ns** |   **2.633 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 48   |    130.47 ns |      1.598 ns |   0.088 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,134.03 ns |  1,412.564 ns |  77.427 ns |  0.97 |    0.02 |         - |          NA |
| Kernel_Cjk | 48   |    243.45 ns |      5.596 ns |   0.307 ns |  0.08 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **64**   |  **5,208.54 ns** |  **2,974.395 ns** | **163.037 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel     | 64   |    155.16 ns |     10.758 ns |   0.590 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,511.92 ns |    725.065 ns |  39.743 ns |  1.06 |    0.03 |         - |          NA |
| Kernel_Cjk | 64   |    290.98 ns |     16.641 ns |   0.912 ns |  0.06 |    0.00 |         - |          NA |
|            |      |              |               |            |       |         |           |             |
| **Dp**         | **96**   | **14,200.61 ns** | **10,992.281 ns** | **602.524 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel     | 96   |    795.73 ns |      7.055 ns |   0.387 ns |  0.06 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 12,619.01 ns |  1,996.786 ns | 109.451 ns |  0.89 |    0.03 |         - |          NA |
| Kernel_Cjk | 96   |  1,011.02 ns |     91.141 ns |   4.996 ns |  0.07 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| **Distance_Utf16**             | **8**      |     **25.72 ns** |   **0.562 ns** |  **0.031 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    121.80 ns |  26.032 ns |  1.427 ns |  4.73 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.37 ns |   0.098 ns |  0.005 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **283.50 ns** |  **15.459 ns** |  **0.847 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    671.43 ns | 260.581 ns | 14.283 ns |  2.37 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    275.07 ns | 147.528 ns |  8.086 ns |  0.97 |    0.02 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **512**    | **14,147.46 ns** | **284.053 ns** | **15.570 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 16,797.33 ns | 369.026 ns | 20.228 ns |  1.19 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,022.99 ns | 132.000 ns |  7.235 ns |  0.99 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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

| Method             | Length | Distinct | Mean         | Error         | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |-------------:|--------------:|------------:|------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |     **347.7 ns** |       **8.32 ns** |     **0.46 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     237.7 ns |       4.94 ns |     0.27 ns |  0.68 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **339.2 ns** |       **6.68 ns** |     **0.37 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     255.8 ns |     150.98 ns |     8.28 ns |  0.75 |    0.02 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **419.4 ns** |     **100.53 ns** |     **5.51 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     319.5 ns |       6.70 ns |     0.37 ns |  0.76 |    0.01 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **415.0 ns** |      **14.22 ns** |     **0.78 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     314.0 ns |      42.06 ns |     2.31 ns |  0.76 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **499.6 ns** |      **45.41 ns** |     **2.49 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     385.5 ns |      37.76 ns |     2.07 ns |  0.77 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **502.3 ns** |      **34.95 ns** |     **1.92 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     402.3 ns |      88.45 ns |     4.85 ns |  0.80 |    0.01 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **600.2 ns** |      **62.18 ns** |     **3.41 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,272.7 ns |     244.61 ns |    13.41 ns |  2.12 |    0.02 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **596.0 ns** |       **6.33 ns** |     **0.35 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,295.9 ns |       7.38 ns |     0.40 ns |  2.17 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,544.3 ns** |     **221.38 ns** |    **12.13 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,277.6 ns |     534.62 ns |    29.30 ns |  2.07 |    0.01 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,567.5 ns** |      **51.93 ns** |     **2.85 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,465.6 ns |      73.19 ns |     4.01 ns |  2.13 |    0.00 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **17,773.9 ns** |   **3,164.09 ns** |   **173.43 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  57,034.6 ns |  16,640.41 ns |   912.12 ns |  3.21 |    0.05 |         - |          NA |
|                    |        |          |              |               |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **466,541.2 ns** | **164,413.91 ns** | **9,012.08 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  60,506.8 ns |   3,134.01 ns |   171.79 ns |  0.13 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| **Matrix**         | **1000**    | **2**       |     **7,532.6 ns** |       **237.81 ns** |      **13.04 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     7,398.1 ns |     1,444.40 ns |      79.17 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |       916.8 ns |         3.51 ns |       0.19 ns |      - |         - |
| F1Macro        | 1000    | 2       |     7,768.3 ns |       614.15 ns |      33.66 ns | 0.0153 |     472 B |
| Report         | 1000    | 2       |    10,347.1 ns |       352.38 ns |      19.32 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **7,743.9 ns** |       **928.37 ns** |      **50.89 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     7,621.4 ns |     1,683.63 ns |      92.29 ns | 0.0610 |    1248 B |
| AccuracyScore  | 1000    | 10      |       920.4 ns |        27.40 ns |       1.50 ns |      - |         - |
| F1Macro        | 1000    | 10      |     8,002.7 ns |       267.96 ns |      14.69 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |    14,923.6 ns |       378.40 ns |      20.74 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **857,974.9 ns** |    **10,837.68 ns** |     **594.05 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   832,802.6 ns |    73,023.28 ns |   4,002.65 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   164,286.6 ns |    47,575.33 ns |   2,607.76 ns |      - |         - |
| F1Macro        | 100000  | 2       |   876,465.3 ns |    92,282.11 ns |   5,058.29 ns |      - |     473 B |
| Report         | 100000  | 2       |   876,134.4 ns |   416,997.98 ns |  22,857.07 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **975,273.4 ns** |     **6,098.45 ns** |     **334.28 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   936,794.8 ns |    33,462.61 ns |   1,834.20 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   274,606.0 ns |    34,267.11 ns |   1,878.30 ns |      - |         - |
| F1Macro        | 100000  | 10      |   983,112.0 ns |    16,701.01 ns |     915.44 ns |      - |    1665 B |
| Report         | 100000  | 10      |   993,996.8 ns |    72,145.24 ns |   3,954.52 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **8,823,664.3 ns** |   **804,925.51 ns** |  **44,120.68 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       | 8,609,921.2 ns | 4,986,152.24 ns | 273,307.83 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       | 1,843,974.4 ns | 1,456,111.39 ns |  79,814.38 ns |      - |         - |
| F1Macro        | 1000000 | 2       | 8,275,445.7 ns |   745,891.24 ns |  40,884.82 ns |      - |     484 B |
| Report         | 1000000 | 2       | 8,629,375.9 ns |   176,127.89 ns |   9,654.16 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **9,910,186.7 ns** |   **849,799.39 ns** |  **46,580.37 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 9,279,690.5 ns |   759,291.33 ns |  41,619.32 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      | 2,577,583.8 ns |   678,257.97 ns |  37,177.61 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 9,771,421.2 ns |   268,593.58 ns |  14,722.52 ns |      - |    1676 B |
| Report         | 1000000 | 10      | 9,720,048.8 ns | 2,530,827.85 ns | 138,723.21 ns |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| **Dp**         | **4**    |     **73.81 ns** |    **13.601 ns** |   **0.745 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 4    |     74.57 ns |    42.841 ns |   2.348 ns |  1.01 |    0.03 |         - |          NA |
| Dp_Cjk     | 4    |     74.30 ns |     2.021 ns |   0.111 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 4    |     73.95 ns |     0.899 ns |   0.049 ns |  1.00 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **6**    |    **104.32 ns** |    **12.267 ns** |   **0.672 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 6    |     76.64 ns |     2.485 ns |   0.136 ns |  0.73 |    0.00 |         - |          NA |
| Dp_Cjk     | 6    |    105.61 ns |    51.900 ns |   2.845 ns |  1.01 |    0.02 |         - |          NA |
| Kernel_Cjk | 6    |    148.54 ns |    76.394 ns |   4.187 ns |  1.42 |    0.04 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **8**    |    **156.60 ns** |     **9.891 ns** |   **0.542 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     89.91 ns |     0.491 ns |   0.027 ns |  0.57 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    161.62 ns |    14.048 ns |   0.770 ns |  1.03 |    0.01 |         - |          NA |
| Kernel_Cjk | 8    |    184.18 ns |     4.789 ns |   0.263 ns |  1.18 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **10**   |    **199.30 ns** |    **73.426 ns** |   **4.025 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 10   |     96.17 ns |     2.193 ns |   0.120 ns |  0.48 |    0.01 |         - |          NA |
| Dp_Cjk     | 10   |    200.45 ns |    74.613 ns |   4.090 ns |  1.01 |    0.02 |         - |          NA |
| Kernel_Cjk | 10   |    151.28 ns |     0.208 ns |   0.011 ns |  0.76 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **259.32 ns** |   **233.284 ns** |  **12.787 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel     | 12   |    103.01 ns |     0.981 ns |   0.054 ns |  0.40 |    0.02 |         - |          NA |
| Dp_Cjk     | 12   |    247.56 ns |    67.106 ns |   3.678 ns |  0.96 |    0.04 |         - |          NA |
| Kernel_Cjk | 12   |    165.89 ns |     6.385 ns |   0.350 ns |  0.64 |    0.03 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **420.47 ns** |   **242.109 ns** |  **13.271 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel     | 16   |    122.56 ns |     2.840 ns |   0.156 ns |  0.29 |    0.01 |         - |          NA |
| Dp_Cjk     | 16   |    416.18 ns |   220.981 ns |  12.113 ns |  0.99 |    0.04 |         - |          NA |
| Kernel_Cjk | 16   |    181.40 ns |     5.020 ns |   0.275 ns |  0.43 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **843.96 ns** |    **65.577 ns** |   **3.595 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 24   |    160.79 ns |     1.833 ns |   0.100 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    848.46 ns |   144.724 ns |   7.933 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 24   |    226.91 ns |     8.622 ns |   0.473 ns |  0.27 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,477.15 ns** |   **763.345 ns** |  **41.842 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 32   |    193.23 ns |     3.737 ns |   0.205 ns |  0.13 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,439.67 ns |   571.071 ns |  31.302 ns |  0.98 |    0.03 |         - |          NA |
| Kernel_Cjk | 32   |    270.98 ns |     2.850 ns |   0.156 ns |  0.18 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,398.39 ns** |   **194.101 ns** |  **10.639 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 48   |    264.91 ns |     1.561 ns |   0.086 ns |  0.08 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,301.29 ns |    13.266 ns |   0.727 ns |  0.97 |    0.00 |         - |          NA |
| Kernel_Cjk | 48   |    353.39 ns |     9.051 ns |   0.496 ns |  0.10 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **6,324.82 ns** | **4,356.228 ns** | **238.780 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel     | 64   |    339.09 ns |     6.018 ns |   0.330 ns |  0.05 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,201.15 ns |    58.898 ns |   3.228 ns |  0.98 |    0.03 |         - |          NA |
| Kernel_Cjk | 64   |    439.84 ns |    11.221 ns |   0.615 ns |  0.07 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **13,912.85 ns** |   **624.874 ns** |  **34.251 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 96   |  1,246.03 ns |   362.951 ns |  19.895 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 13,986.81 ns |   708.328 ns |  38.826 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 96   |  1,738.03 ns | 1,921.262 ns | 105.311 ns |  0.12 |    0.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| VocabTxt               |  4.072 ms | 1.5686 ms | 0.0860 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.219 ms | 1.9109 ms | 0.1047 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.506 ms | 1.1389 ms | 0.0624 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.750 ms | 3.7849 ms | 0.2075 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.918 ms | 0.6622 ms | 0.0363 ms |  33.2031 |  27.3438 |  27.3438 |   2.09 MB |
| TfidfLoad              |  4.372 ms | 0.4265 ms | 0.0234 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  6.288 ms | 0.5606 ms | 0.0307 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  5.600 ms | 0.3726 ms | 0.0204 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| **Count**                | **200**       |  **7.081 ms** | **0.7806 ms** | **0.0428 ms** |  **1.00** |  **507.8125** |  **242.1875** |  **70.3125** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  5.973 ms | 0.2937 ms | 0.0161 ms |  0.84 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.122 ms | 1.5500 ms | 0.0850 ms |  1.01 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.149 ms | 0.2126 ms | 0.0117 ms |  0.87 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |           |          |           |             |
| **Count**                | **1000**      | **28.688 ms** | **0.7053 ms** | **0.0387 ms** |  **1.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 22.710 ms | 3.8645 ms | 0.2118 ms |  0.79 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 27.221 ms | 2.7695 ms | 0.1518 ms |  0.95 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 23.348 ms | 1.0337 ms | 0.0567 ms |  0.81 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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

| Method | Dim  | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
| **Dot**    | **384**  |  **51.54 ns** |  **1.879 ns** | **0.103 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| L2Norm | 384  |  47.92 ns |  0.395 ns | 0.022 ns |  0.93 |    0.00 |         - |          NA |
|        |      |           |           |          |       |         |           |             |
| **Dot**    | **768**  |  **94.19 ns** | **18.983 ns** | **1.041 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| L2Norm | 768  |  92.62 ns |  0.315 ns | 0.017 ns |  0.98 |    0.01 |         - |          NA |
|        |      |           |           |          |       |         |           |             |
| **Dot**    | **1024** | **134.04 ns** |  **1.142 ns** | **0.063 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| L2Norm | 1024 | 125.20 ns | 64.106 ns | 3.514 ns |  0.93 |    0.02 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

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
| **Count**        | **200**       |  **3.147 ms** | **6.3403 ms** | **0.3475 ms** |  **1.01** |    **0.14** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  2.892 ms | 2.5213 ms | 0.1382 ms |  0.93 |    0.10 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.609 ms | 0.4641 ms | 0.0254 ms |  1.16 |    0.11 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.772 ms | 0.1624 ms | 0.0089 ms |  0.89 |    0.08 |  97.6563 |  31.2500 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.812 ms** | **1.3441 ms** | **0.0737 ms** |  **1.00** |    **0.01** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  6.947 ms | 1.1145 ms | 0.0611 ms |  1.02 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 10.967 ms | 0.2681 ms | 0.0147 ms |  1.61 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  6.853 ms | 0.6333 ms | 0.0347 ms |  1.01 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 116.2 | 25.3 | 4.60x C# faster |
| latin | 32 | 181.4 | 86.4 | 2.10x C# faster |
| latin | 128 | 471.8 | 895.2 | 1.90x Py faster |
| latin | 512 | 4452.4 | 7735.5 | 1.74x Py faster |
| cjk | 8 | 139.8 | 25.3 | 5.53x C# faster |
| cjk | 32 | 330.2 | 224.2 | 1.47x C# faster |
| cjk | 128 | 1904.5 | 1648.6 | 1.16x C# faster |
| cjk | 512 | 14854.7 | 11015.1 | 1.35x C# faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is Hyyro's bit-parallel LLCS above a pattern of 8 and a rolling-row dynamic program below it (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 157.5 | 17.0 | 9.24x C# faster |
| latin | 32 | 291.5 | 157.5 | 1.85x C# faster |
| latin | 128 | 1687.5 | 1509.1 | 1.12x C# faster |
| latin | 512 | 14014.7 | 15949.2 | 1.14x Py faster |
| cjk | 8 | 150.5 | 17.2 | 8.73x C# faster |
| cjk | 32 | 369.7 | 287.2 | 1.29x C# faster |
| cjk | 128 | 2833.8 | 2350.1 | 1.21x C# faster |
| cjk | 512 | 23647.3 | 19148.8 | 1.23x C# faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.943 | 103.28x | 0.009 | 0.943 | 103.27x |
| accuracy_n1000_k2 | 0.001 | 0.502 | 490.98x | 0.001 | 0.502 | 490.98x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.741 | 226.76x | 0.008 | 1.741 | 226.77x |
| classification_report_n1000_k2 | 0.010 | 6.546 | 640.68x | 0.010 | 6.546 | 640.71x |
| roc_auc_binary_n1000_k2 | 0.015 | 1.881 | 127.45x | 0.015 | 1.881 | 127.45x |
| balanced_accuracy_n1000_k2 | 0.007 | 1.029 | 137.90x | 0.007 | 1.029 | 137.90x |
| matthews_n1000_k2 | 0.007 | 1.937 | 260.02x | 0.007 | 1.936 | 260.02x |
| cohen_kappa_n1000_k2 | 0.008 | 1.082 | 141.54x | 0.008 | 1.082 | 141.54x |
| mse_n1000_k2 | 0.002 | 0.300 | 124.28x | 0.002 | 0.300 | 124.28x |
| mae_n1000_k2 | 0.002 | 0.299 | 126.50x | 0.002 | 0.299 | 126.51x |
| median_ae_n1000_k2 | 0.006 | 0.311 | 49.18x | 0.006 | 0.311 | 49.18x |
| r2_n1000_k2 | 0.002 | 0.363 | 147.52x | 0.002 | 0.363 | 147.51x |
| confusion_matrix_n1000_k10 | 0.009 | 0.978 | 109.12x | 0.009 | 0.978 | 109.12x |
| accuracy_n1000_k10 | 0.001 | 0.520 | 466.61x | 0.001 | 0.520 | 466.64x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.772 | 214.89x | 0.008 | 1.772 | 214.89x |
| classification_report_n1000_k10 | 0.015 | 6.687 | 457.44x | 0.015 | 6.687 | 457.47x |
| roc_auc_ovr_macro_n1000_k10 | 0.540 | 9.676 | 17.93x | 0.540 | 9.675 | 17.93x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.025 | 124.71x | 0.008 | 1.025 | 124.71x |
| matthews_n1000_k10 | 0.008 | 1.963 | 247.31x | 0.008 | 1.963 | 247.31x |
| cohen_kappa_n1000_k10 | 0.009 | 1.051 | 121.73x | 0.009 | 1.051 | 121.74x |
| mse_n1000_k10 | 0.002 | 0.291 | 124.87x | 0.002 | 0.291 | 124.87x |
| mae_n1000_k10 | 0.002 | 0.296 | 123.24x | 0.002 | 0.296 | 123.23x |
| median_ae_n1000_k10 | 0.006 | 0.304 | 49.37x | 0.006 | 0.304 | 49.37x |
| r2_n1000_k10 | 0.002 | 0.353 | 145.31x | 0.002 | 0.353 | 145.31x |
| confusion_matrix_n100000_k2 | 0.983 | 10.082 | 10.26x | 0.983 | 10.082 | 10.26x |
| accuracy_n100000_k2 | 0.180 | 3.533 | 19.62x | 0.180 | 3.533 | 19.62x |
| precision_recall_f1_macro_n100000_k2 | 0.846 | 11.764 | 13.90x | 0.846 | 11.763 | 13.90x |
| classification_report_n100000_k2 | 0.848 | 25.464 | 30.02x | 0.848 | 25.462 | 30.02x |
| roc_auc_binary_n100000_k2 | 3.619 | 25.284 | 6.99x | 3.619 | 25.282 | 6.99x |
| balanced_accuracy_n100000_k2 | 0.864 | 10.228 | 11.84x | 0.864 | 10.227 | 11.84x |
| matthews_n100000_k2 | 0.843 | 20.768 | 24.63x | 0.843 | 20.767 | 24.63x |
| cohen_kappa_n100000_k2 | 0.863 | 10.275 | 11.91x | 0.863 | 10.274 | 11.91x |
| mse_n100000_k2 | 0.237 | 0.422 | 1.78x | 0.237 | 0.422 | 1.78x |
| mae_n100000_k2 | 0.237 | 0.415 | 1.75x | 0.238 | 0.415 | 1.75x |
| median_ae_n100000_k2 | 0.759 | 1.725 | 2.27x | 0.781 | 1.725 | 2.21x |
| r2_n100000_k2 | 0.229 | 0.616 | 2.69x | 0.229 | 0.615 | 2.69x |
| confusion_matrix_n100000_k10 | 0.968 | 10.244 | 10.58x | 0.968 | 10.242 | 10.58x |
| accuracy_n100000_k10 | 0.269 | 3.558 | 13.20x | 0.269 | 3.557 | 13.20x |
| precision_recall_f1_macro_n100000_k10 | 0.980 | 12.340 | 12.59x | 0.980 | 12.340 | 12.59x |
| classification_report_n100000_k10 | 0.981 | 28.572 | 29.12x | 0.981 | 28.570 | 29.11x |
| roc_auc_ovr_macro_n100000_k10 | 35.446 | 201.176 | 5.68x | 35.445 | 201.153 | 5.68x |
| balanced_accuracy_n100000_k10 | 0.973 | 10.293 | 10.58x | 0.973 | 10.292 | 10.58x |
| matthews_n100000_k10 | 0.955 | 20.973 | 21.97x | 0.955 | 20.972 | 21.97x |
| cohen_kappa_n100000_k10 | 1.141 | 10.387 | 9.11x | 1.140 | 10.387 | 9.11x |
| mse_n100000_k10 | 0.240 | 0.425 | 1.77x | 0.240 | 0.425 | 1.77x |
| mae_n100000_k10 | 0.240 | 0.437 | 1.82x | 0.240 | 0.437 | 1.82x |
| median_ae_n100000_k10 | 0.793 | 1.733 | 2.18x | 0.834 | 1.732 | 2.08x |
| r2_n100000_k10 | 0.230 | 0.625 | 2.72x | 0.230 | 0.625 | 2.72x |
| confusion_matrix_n1000000_k2 | 8.399 | 94.900 | 11.30x | 8.398 | 94.870 | 11.30x |
| accuracy_n1000000_k2 | 1.950 | 31.314 | 16.06x | 1.949 | 31.311 | 16.06x |
| precision_recall_f1_macro_n1000000_k2 | 8.602 | 102.307 | 11.89x | 8.602 | 102.294 | 11.89x |
| classification_report_n1000000_k2 | 8.437 | 197.971 | 23.46x | 8.437 | 197.954 | 23.46x |
| roc_auc_binary_n1000000_k2 | 43.886 | 269.787 | 6.15x | 43.883 | 269.753 | 6.15x |
| balanced_accuracy_n1000000_k2 | 8.539 | 94.739 | 11.10x | 8.538 | 94.736 | 11.10x |
| matthews_n1000000_k2 | 8.620 | 190.755 | 22.13x | 8.620 | 190.753 | 22.13x |
| cohen_kappa_n1000000_k2 | 8.575 | 94.599 | 11.03x | 8.575 | 94.595 | 11.03x |
| mse_n1000000_k2 | 2.335 | 1.902 | 0.81x | 2.335 | 1.902 | 0.81x |
| mae_n1000000_k2 | 2.403 | 1.937 | 0.81x | 2.403 | 1.936 | 0.81x |
| median_ae_n1000000_k2 | 7.021 | 13.560 | 1.93x | 7.056 | 13.559 | 1.92x |
| r2_n1000000_k2 | 2.315 | 3.339 | 1.44x | 2.315 | 3.338 | 1.44x |
| confusion_matrix_n1000000_k10 | 9.366 | 98.067 | 10.47x | 9.366 | 98.058 | 10.47x |
| accuracy_n1000000_k10 | 2.693 | 32.608 | 12.11x | 2.693 | 32.606 | 12.11x |
| precision_recall_f1_macro_n1000000_k10 | 9.442 | 107.946 | 11.43x | 9.442 | 107.930 | 11.43x |
| classification_report_n1000000_k10 | 9.687 | 226.008 | 23.33x | 9.680 | 226.004 | 23.35x |
| balanced_accuracy_n1000000_k10 | 9.660 | 94.573 | 9.79x | 9.659 | 94.566 | 9.79x |
| matthews_n1000000_k10 | 9.405 | 196.279 | 20.87x | 9.404 | 196.262 | 20.87x |
| cohen_kappa_n1000000_k10 | 9.481 | 94.131 | 9.93x | 9.480 | 94.129 | 9.93x |
| mse_n1000000_k10 | 2.312 | 1.919 | 0.83x | 2.312 | 1.919 | 0.83x |
| mae_n1000000_k10 | 2.280 | 1.934 | 0.85x | 2.280 | 1.934 | 0.85x |
| median_ae_n1000000_k10 | 6.843 | 13.426 | 1.96x | 6.946 | 13.426 | 1.93x |
| r2_n1000000_k10 | 2.587 | 3.261 | 1.26x | 2.587 | 3.261 | 1.26x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.81x
  mae_n1000000_k2                  0.81x
  mse_n1000000_k10                 0.83x
  mae_n1000000_k10                 0.85x

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-28, measured at commit `0f05972b208bdd0213057d6ea2f19ab6e525d3b7`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.960 | 9.502 | 1.92x | 5.126 | 9.502 | 1.85x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.060 | 15.759 | 1.31x | 12.468 | 15.759 | 1.26x | 706,526 | 706,526 |
| tokenizer_json_unigram | 12.374 | 33.340 | 2.69x | 12.710 | 33.336 | 2.62x | 1,990,038 | 1,990,038 |
| spiece_model | 4.406 | 27.626 | 6.27x | 4.551 | 27.622 | 6.07x | 533,084 | 533,084 |
| tfidf_save | 1.734 | 2.344 | 1.35x | 1.829 | 2.343 | 1.28x | 581,787 | 591,922 |
| tfidf_load | 4.992 | 3.930 | 0.79x | 5.170 | 3.929 | 0.76x | 581,787 | 591,922 |
| embedding_index_save | 6.326 | 1.369 | 0.22x | 6.860 | 1.369 | 0.20x | 20,589,007 | 15,360,128 |
| embedding_index_load | 5.611 | 1.305 | 0.23x | 6.303 | 1.305 | 0.21x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 6.291 | 0.769 | 0.12x | 6.935 | 0.768 | 0.11x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 4.000 | 1.168 | 0.29x | 4.422 | 1.167 | 0.26x | 20,589,007 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 92.71x | 0.000 | 0.001 | 92.71x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 395.599 | 548.111 | 1.39x | 398.216 | 548.079 | 1.38x | 15,251,458 | 14,022,374 |
| embedding_index_load_gzip | 73.209 | 64.070 | 0.88x | 74.130 | 64.067 | 0.86x | 15,251,458 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
