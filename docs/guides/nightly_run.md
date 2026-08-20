# Nightly benchmark run

<!-- nightly-baseline: cfc1945452be146baba70a196a66a54b11ca256c -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `cfc1945452be146baba70a196a66a54b11ca256c`
- Previous run: `cfc1945452be146baba70a196a66a54b11ca256c`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
- `BpeBenchmarks`
- `BpeScalingBenchmarks`
- `BucketRouteDiagnostics`
- `FuzzBenchmarks`
- `IndelBenchmarks`
- `LcsGateBenchmarks`
- `LevenshteinBenchmarks`
- `LevenshteinCodePointBenchmarks`
- `MetricsBenchmarks`
- `MyersGateBenchmarks`
- `PersistenceBenchmarks`
- `StopWordBenchmarks`
- `VectorMathBenchmarks`
- `VectorizerBenchmarks`

### Lodestar.Text.Benchmarks.BatchEmbeddingBenchmarks-report-github

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

| Method             | CorpusSize | Mean         | Error       | StdDev     | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|-----------:|------:|--------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **5.797 μs** |   **1.3550 μs** |  **0.0743 μs** |  **1.00** |    **0.02** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.113 μs |   0.4433 μs |  0.0243 μs |  1.05 |    0.01 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.129 μs |   0.4011 μs |  0.0220 μs |  1.06 |    0.01 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **8**          |   **106.904 μs** |  **15.6934 μs** |  **0.8602 μs** |  **1.00** |    **0.01** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    71.415 μs |   8.4672 μs |  0.4641 μs |  0.67 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    72.534 μs |   5.2924 μs |  0.2901 μs |  0.68 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **32**         |   **395.204 μs** |  **14.6029 μs** |  **0.8004 μs** |  **1.00** |    **0.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   251.234 μs |  42.7689 μs |  2.3443 μs |  0.64 |    0.01 |  26.8555 |  1.4648 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   240.726 μs |  82.5078 μs |  4.5225 μs |  0.61 |    0.01 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |             |            |       |         |          |         |            |             |
| **UnitLoop**           | **128**        | **1,602.304 μs** | **186.8606 μs** | **10.2425 μs** |  **1.00** |    **0.01** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   998.928 μs |  53.6225 μs |  2.9392 μs |  0.62 |    0.00 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   941.479 μs |  21.0456 μs |  1.1536 μs |  0.59 |    0.00 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

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

| Method  | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0       | Allocated | Alloc Ratio |
|-------- |---------:|---------:|---------:|------:|--------:|-----------:|----------:|------------:|
| Unigram | 642.4 ms | 129.7 ms |  7.11 ms |  1.00 |    0.01 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 596.5 ms | 216.4 ms | 11.86 ms |  0.93 |    0.02 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

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
| **BpeOnOnePathologicalToken** | **512**    |   **103.3 μs** |  **4.31 μs** | **0.24 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **211.5 μs** | **15.86 μs** | **0.87 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **490.5 μs** | **75.28 μs** | **4.13 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,019.4 μs** | **76.96 μs** | **4.22 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

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
| DpGroup    |  13.70 μs | 0.486 μs | 0.027 μs |         - |
| MyersGroup | 114.62 μs | 0.166 μs | 0.009 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

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
| Ratio          |    109.4 ns |   3.51 ns |  0.19 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 18,580.1 ns | 695.46 ns | 38.12 ns | 169.84 |    0.40 |      - |         - |          NA |
| TokenSortRatio |  1,018.7 ns | 528.31 ns | 28.96 ns |   9.31 |    0.23 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,498.8 ns | 433.25 ns | 23.75 ns |  31.98 |    0.19 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,832.4 ns | 538.64 ns | 29.52 ns |  44.17 |    0.24 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

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
| **Distance_Utf16**             | **8**      |      **29.17 ns** |       **3.643 ns** |     **0.200 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     134.85 ns |       1.604 ns |     0.088 ns |  4.62 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      29.62 ns |       0.828 ns |     0.045 ns |  1.02 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      30.39 ns |       0.334 ns |     0.018 ns |  1.04 |    0.01 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **32.39 ns** |       **0.115 ns** |     **0.006 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     138.87 ns |       9.458 ns |     0.518 ns |  4.29 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      34.43 ns |       5.630 ns |     0.309 ns |  1.06 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      33.51 ns |      22.955 ns |     1.258 ns |  1.03 |    0.03 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **37.09 ns** |       **2.525 ns** |     **0.138 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     149.11 ns |      22.807 ns |     1.250 ns |  4.02 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      36.52 ns |       1.031 ns |     0.057 ns |  0.98 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      35.69 ns |       0.107 ns |     0.006 ns |  0.96 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **91.30 ns** |      **11.240 ns** |     **0.616 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     186.20 ns |      11.112 ns |     0.609 ns |  2.04 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      90.79 ns |       3.374 ns |     0.185 ns |  0.99 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      93.31 ns |      34.188 ns |     1.874 ns |  1.02 |    0.02 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **62.37 ns** |       **2.365 ns** |     **0.130 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     846.04 ns |     176.280 ns |     9.663 ns | 13.57 |    0.14 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      57.54 ns |       1.469 ns |     0.081 ns |  0.92 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      61.71 ns |       6.472 ns |     0.355 ns |  0.99 |    0.01 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **64.85 ns** |       **1.968 ns** |     **0.108 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,051.80 ns |     117.374 ns |     6.434 ns | 16.22 |    0.09 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      69.68 ns |       2.062 ns |     0.113 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      64.94 ns |       2.208 ns |     0.121 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,226.21 ns** |     **135.831 ns** |     **7.445 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  18,339.21 ns |   2,290.459 ns |   125.548 ns | 14.96 |    0.12 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,276.79 ns |      11.294 ns |     0.619 ns |  1.04 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,226.24 ns |     124.345 ns |     6.816 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |                |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **8,766.44 ns** |     **970.824 ns** |    **53.214 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 307,936.61 ns | 109,157.628 ns | 5,983.298 ns | 35.13 |    0.62 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   8,949.53 ns |      98.591 ns |     5.404 ns |  1.02 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   8,731.30 ns |      58.638 ns |     3.214 ns |  1.00 |    0.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

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

| Method | Band | Mean         | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |-------------:|-------------:|----------:|------:|--------:|----------:|------------:|
| **Dp**     | **8**    |    **130.87 ns** |    **28.191 ns** |  **1.545 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 8    |     65.80 ns |     3.132 ns |  0.172 ns |  0.50 |    0.01 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **12**   |    **223.78 ns** |    **52.486 ns** |  **2.877 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 12   |     69.70 ns |     0.823 ns |  0.045 ns |  0.31 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **14**   |    **306.74 ns** |   **406.320 ns** | **22.272 ns** |  **1.00** |    **0.09** |         **-** |          **NA** |
| Kernel | 14   |     72.75 ns |    12.661 ns |  0.694 ns |  0.24 |    0.02 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **16**   |    **365.05 ns** |   **315.808 ns** | **17.310 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Kernel | 16   |     77.71 ns |     0.514 ns |  0.028 ns |  0.21 |    0.01 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **18**   |    **732.56 ns** |   **212.333 ns** | **11.639 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 18   |     82.89 ns |     4.900 ns |  0.269 ns |  0.11 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **20**   |    **812.54 ns** |    **29.663 ns** |  **1.626 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 20   |     86.68 ns |     1.252 ns |  0.069 ns |  0.11 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **24**   |    **994.11 ns** |    **39.893 ns** |  **2.187 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 24   |     97.10 ns |    10.344 ns |  0.567 ns |  0.10 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **32**   |  **1,665.01 ns** |    **30.006 ns** |  **1.645 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    117.27 ns |     2.437 ns |  0.134 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **48**   |  **3,239.15 ns** | **1,261.439 ns** | **69.144 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel | 48   |    145.32 ns |     4.523 ns |  0.248 ns |  0.04 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **64**   |  **5,457.27 ns** |   **325.565 ns** | **17.845 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 64   |    173.82 ns |     9.604 ns |  0.526 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |              |           |       |         |           |             |
| **Dp**     | **96**   | **11,197.97 ns** |   **217.595 ns** | **11.927 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 96   |  1,007.08 ns |     7.676 ns |  0.421 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

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
| **Distance_Utf16**             | **8**      |     **28.24 ns** |     **0.432 ns** |   **0.024 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    124.86 ns |     0.634 ns |   0.035 ns |  4.42 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     28.24 ns |     0.983 ns |   0.054 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **301.91 ns** |    **34.098 ns** |   **1.869 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    705.13 ns |     6.188 ns |   0.339 ns |  2.34 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    304.16 ns |     2.535 ns |   0.139 ns |  1.01 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    | **16,561.59 ns** | **5,503.175 ns** | **301.648 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 18,888.54 ns | 2,668.629 ns | 146.277 ns |  1.14 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 16,085.65 ns |   166.791 ns |   9.142 ns |  0.97 |    0.02 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

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
| **Distance_CodePoint** | **16**     | **32**       |       **352.6 ns** |       **7.60 ns** |     **0.42 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,551.4 ns |     780.16 ns |    42.76 ns |   4.40 |    0.11 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **354.4 ns** |       **2.88 ns** |     **0.16 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,507.8 ns |      27.69 ns |     1.52 ns |   4.25 |    0.00 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **447.1 ns** |     **117.61 ns** |     **6.45 ns** |   **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,456.7 ns |   1,232.33 ns |    67.55 ns |   7.73 |    0.16 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **444.1 ns** |       **8.94 ns** |     **0.49 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,434.9 ns |      86.81 ns |     4.76 ns |   7.73 |    0.01 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **536.1 ns** |       **8.73 ns** |     **0.48 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,342.5 ns |     141.68 ns |     7.77 ns |  11.83 |    0.02 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **537.5 ns** |       **3.56 ns** |     **0.20 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,212.0 ns |     217.02 ns |    11.90 ns |  11.56 |    0.02 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **623.0 ns** |       **8.42 ns** |     **0.46 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |     9,990.6 ns |     290.09 ns |    15.90 ns |  16.04 |    0.02 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **632.0 ns** |      **68.58 ns** |     **3.76 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |     9,572.6 ns |   1,035.02 ns |    56.73 ns |  15.15 |    0.11 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,372.6 ns** |      **16.03 ns** |     **0.88 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   105,551.5 ns |  11,264.83 ns |   617.46 ns |  44.49 |    0.23 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,443.4 ns** |      **46.17 ns** |     **2.53 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   105,987.7 ns |  11,729.79 ns |   642.95 ns |  43.38 |    0.23 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **20,594.3 ns** |   **1,391.80 ns** |    **76.29 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,393,274.7 ns | 143,787.99 ns | 7,881.50 ns | 116.21 |    0.50 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **432,741.7 ns** |   **8,201.74 ns** |   **449.56 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,747,780.9 ns |  61,027.41 ns | 3,345.12 ns |   4.04 |    0.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

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
| **Matrix**         | **1000**    | **2**       |      **7.195 μs** |     **0.0362 μs** |   **0.0020 μs** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7.003 μs |     1.0433 μs |   0.0572 μs | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |      1.031 μs |     0.0171 μs |   0.0009 μs |      - |         - |
| F1Macro        | 1000    | 2       |      7.191 μs |     0.1989 μs |   0.0109 μs | 0.0229 |     472 B |
| Report         | 1000    | 2       |      9.948 μs |     2.6283 μs |   0.1441 μs | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7.506 μs** |     **0.8727 μs** |   **0.0478 μs** | **0.0687** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7.462 μs |     0.2156 μs |   0.0118 μs | 0.0687 |    1248 B |
| AccuracyScore  | 1000    | 10      |      1.034 μs |     0.0490 μs |   0.0027 μs |      - |         - |
| F1Macro        | 1000    | 10      |      7.952 μs |     1.1171 μs |   0.0612 μs | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     15.117 μs |     1.6293 μs |   0.0893 μs | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **846.899 μs** |     **7.4322 μs** |   **0.4074 μs** |      **-** |     **312 B** |
| MatrixWeighted | 100000  | 2       |    854.051 μs |   209.1911 μs |  11.4665 μs |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    133.853 μs |    33.0834 μs |   1.8134 μs |      - |         - |
| F1Macro        | 100000  | 2       |    821.720 μs |   198.0289 μs |  10.8546 μs |      - |     473 B |
| Report         | 100000  | 2       |    797.462 μs |   155.3301 μs |   8.5142 μs |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **952.321 μs** |    **56.2116 μs** |   **3.0811 μs** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    977.269 μs |    47.7243 μs |   2.6159 μs |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    246.650 μs |    37.2262 μs |   2.0405 μs |      - |         - |
| F1Macro        | 100000  | 10      |    964.092 μs |   116.8965 μs |   6.4075 μs |      - |    1665 B |
| Report         | 100000  | 10      |    985.401 μs |    83.2758 μs |   4.5646 μs |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **9,131.176 μs** | **3,445.8089 μs** | **188.8764 μs** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,594.608 μs |   389.4564 μs |  21.3474 μs |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,916.712 μs |    17.5993 μs |   0.9647 μs |      - |         - |
| F1Macro        | 1000000 | 2       |  8,812.520 μs |   220.8679 μs |  12.1065 μs |      - |     484 B |
| Report         | 1000000 | 2       |  8,845.671 μs |    83.7944 μs |   4.5931 μs |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      |  **9,806.872 μs** |   **461.0094 μs** |  **25.2695 μs** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 10,120.870 μs |   777.0141 μs |  42.5908 μs |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  3,084.606 μs |    40.7465 μs |   2.2335 μs |      - |         - |
| F1Macro        | 1000000 | 10      | 10,198.860 μs | 1,821.1036 μs |  99.8208 μs |      - |    1676 B |
| Report         | 1000000 | 10      | 10,214.884 μs |   196.5872 μs |  10.7756 μs |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

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
| **Dp**     | **4**    |     **82.09 ns** |   **4.854 ns** |  **0.266 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 4    |     80.08 ns |   1.730 ns |  0.095 ns |  0.98 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **6**    |    **122.59 ns** |   **8.539 ns** |  **0.468 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 6    |    118.10 ns |   5.838 ns |  0.320 ns |  0.96 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **8**    |    **157.06 ns** |  **14.158 ns** |  **0.776 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 8    |     99.19 ns |   1.130 ns |  0.062 ns |  0.63 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **10**   |    **208.17 ns** |  **39.308 ns** |  **2.155 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 10   |    109.31 ns |   3.669 ns |  0.201 ns |  0.53 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **12**   |    **272.50 ns** |  **10.410 ns** |  **0.571 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 12   |    125.33 ns |  55.632 ns |  3.049 ns |  0.46 |    0.01 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **16**   |    **439.36 ns** | **310.567 ns** | **17.023 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel | 16   |    142.69 ns |  35.227 ns |  1.931 ns |  0.33 |    0.01 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **24**   |    **867.65 ns** | **131.879 ns** |  **7.229 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 24   |    178.72 ns |   2.870 ns |  0.157 ns |  0.21 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **32**   |  **1,480.44 ns** |  **19.645 ns** |  **1.077 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    223.51 ns |  16.686 ns |  0.915 ns |  0.15 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **48**   |  **3,250.77 ns** | **233.937 ns** | **12.823 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 48   |    306.04 ns |  48.095 ns |  2.636 ns |  0.09 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **64**   |  **5,654.06 ns** |  **85.943 ns** |  **4.711 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 64   |    389.55 ns |  15.493 ns |  0.849 ns |  0.07 |    0.00 |         - |          NA |
|        |      |              |            |           |       |         |           |             |
| **Dp**     | **96**   | **12,548.14 ns** | **350.072 ns** | **19.189 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 96   |  1,093.61 ns |   7.595 ns |  0.416 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

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
| VocabTxt               |  4.443 ms | 5.7182 ms | 0.3134 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 10.838 ms | 1.9119 ms | 0.1048 ms | 156.2500 | 125.0000 |  31.2500 |   5.72 MB |
| TokenizerJsonUnigram   | 11.258 ms | 2.0090 ms | 0.1101 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  4.054 ms | 0.8584 ms | 0.0471 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.954 ms | 0.5353 ms | 0.0293 ms |  27.3438 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              |  4.578 ms | 1.1942 ms | 0.0655 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  5.389 ms | 2.6900 ms | 0.1475 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  5.149 ms | 2.1522 ms | 0.1180 ms | 500.0000 | 468.7500 | 437.5000 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

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

| Method               | Documents | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0      | Gen1      | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|-----------:|----------:|------:|--------:|----------:|----------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.488 ms** |  **1.6206 ms** | **0.0888 ms** |  **1.00** |    **0.01** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.295 ms |  1.2488 ms | 0.0685 ms |  0.84 |    0.01 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.493 ms |  1.5699 ms | 0.0861 ms |  1.00 |    0.01 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.488 ms |  0.8608 ms | 0.0472 ms |  0.87 |    0.01 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |            |           |       |         |           |           |          |           |             |
| **Count**                | **1000**      | **32.273 ms** | **17.7627 ms** | **0.9736 ms** |  **1.00** |    **0.04** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 23.970 ms |  6.7912 ms | 0.3722 ms |  0.74 |    0.02 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 29.140 ms |  7.5421 ms | 0.4134 ms |  0.90 |    0.03 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 25.267 ms |  5.4764 ms | 0.3002 ms |  0.78 |    0.02 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

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

| Method | Dim  | Mean      | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------- |----- |----------:|----------:|---------:|------:|----------:|------------:|
| **Dot**    | **384**  |  **51.64 ns** |  **6.114 ns** | **0.335 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.81 ns |  0.722 ns | 0.040 ns |  0.95 |         - |          NA |
|        |      |           |           |          |       |           |             |
| **Dot**    | **768**  | **100.10 ns** | **16.325 ns** | **0.895 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  91.06 ns |  7.684 ns | 0.421 ns |  0.91 |         - |          NA |
|        |      |           |           |          |       |           |             |
| **Dot**    | **1024** | **132.68 ns** |  **4.110 ns** | **0.225 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 125.85 ns | 22.728 ns | 1.246 ns |  0.95 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

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
| **Count**        | **200**       |  **3.013 ms** | **1.0907 ms** | **0.0598 ms** |  **1.00** |    **0.02** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.392 ms | 7.3547 ms | 0.4031 ms |  1.13 |    0.12 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.871 ms | 0.5438 ms | 0.0298 ms |  1.28 |    0.02 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  3.027 ms | 2.3265 ms | 0.1275 ms |  1.00 |    0.04 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.218 ms** | **1.6303 ms** | **0.0894 ms** |  **1.00** |    **0.02** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.499 ms | 2.3268 ms | 0.1275 ms |  1.04 |    0.02 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.998 ms | 1.8018 ms | 0.0988 ms |  1.66 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.086 ms | 1.0524 ms | 0.0577 ms |  0.98 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

## Against rapidfuzz, in this same run

Both sides on this VM in these minutes, which is what makes the ratio readable where the absolutes are not.

- `indel`
- `levenshtein`
- `metrics`
- `persistence`

### compare-indel

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 110.0 | 23.8 | 4.62x C# faster |
| 32 | 157.5 | 75.4 | 2.09x C# faster |
| 128 | 478.4 | 770.3 | 1.61x Py faster |
| 512 | 4860.3 | 7959.8 | 1.64x Py faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---:|---:|---:|:---|
| 8 | 136.0 | 18.4 | 7.41x C# faster |
| 32 | 255.4 | 132.9 | 1.92x C# faster |
| 128 | 1814.2 | 1442.8 | 1.26x C# faster |
| 512 | 15585.8 | 16659.5 | 1.07x Py faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.769 | 88.83x | 0.009 | 0.769 | 88.82x |
| accuracy_n1000_k2 | 0.001 | 0.404 | 384.86x | 0.001 | 0.404 | 384.84x |
| precision_recall_f1_macro_n1000_k2 | 0.007 | 1.398 | 197.21x | 0.007 | 1.397 | 197.22x |
| classification_report_n1000_k2 | 0.010 | 5.231 | 542.97x | 0.010 | 5.230 | 542.92x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.564 | 100.06x | 0.016 | 1.563 | 100.04x |
| balanced_accuracy_n1000_k2 | 0.007 | 0.829 | 118.75x | 0.007 | 0.829 | 118.74x |
| matthews_n1000_k2 | 0.007 | 1.558 | 223.47x | 0.007 | 1.558 | 223.47x |
| cohen_kappa_n1000_k2 | 0.007 | 0.872 | 124.89x | 0.007 | 0.872 | 124.88x |
| mse_n1000_k2 | 0.003 | 0.219 | 80.21x | 0.003 | 0.219 | 80.22x |
| mae_n1000_k2 | 0.003 | 0.218 | 79.89x | 0.003 | 0.218 | 79.89x |
| median_ae_n1000_k2 | 0.006 | 0.236 | 39.36x | 0.006 | 0.236 | 39.36x |
| r2_n1000_k2 | 0.003 | 0.274 | 99.85x | 0.003 | 0.274 | 99.85x |
| confusion_matrix_n1000_k10 | 0.009 | 0.777 | 86.64x | 0.009 | 0.777 | 86.62x |
| accuracy_n1000_k10 | 0.001 | 0.407 | 388.71x | 0.001 | 0.407 | 388.69x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.424 | 184.21x | 0.008 | 1.424 | 184.18x |
| classification_report_n1000_k10 | 0.014 | 5.535 | 388.12x | 0.014 | 5.534 | 388.07x |
| roc_auc_ovr_macro_n1000_k10 | 0.471 | 8.201 | 17.41x | 0.471 | 8.200 | 17.40x |
| balanced_accuracy_n1000_k10 | 0.008 | 0.845 | 109.27x | 0.008 | 0.845 | 109.26x |
| matthews_n1000_k10 | 0.008 | 1.598 | 207.28x | 0.008 | 1.598 | 207.29x |
| cohen_kappa_n1000_k10 | 0.008 | 0.881 | 108.28x | 0.008 | 0.881 | 108.28x |
| mse_n1000_k10 | 0.003 | 0.217 | 79.50x | 0.003 | 0.217 | 79.51x |
| mae_n1000_k10 | 0.003 | 0.218 | 79.69x | 0.003 | 0.218 | 79.68x |
| median_ae_n1000_k10 | 0.006 | 0.230 | 38.44x | 0.006 | 0.230 | 38.44x |
| r2_n1000_k10 | 0.003 | 0.274 | 100.30x | 0.003 | 0.274 | 100.31x |
| confusion_matrix_n100000_k2 | 0.990 | 10.947 | 11.06x | 0.990 | 10.946 | 11.06x |
| accuracy_n100000_k2 | 0.106 | 3.769 | 35.66x | 0.106 | 3.768 | 35.66x |
| precision_recall_f1_macro_n100000_k2 | 0.835 | 12.537 | 15.02x | 0.835 | 12.536 | 15.02x |
| classification_report_n100000_k2 | 0.780 | 26.789 | 34.35x | 0.780 | 26.789 | 34.35x |
| roc_auc_binary_n100000_k2 | 2.930 | 28.478 | 9.72x | 2.930 | 28.477 | 9.72x |
| balanced_accuracy_n100000_k2 | 0.779 | 11.026 | 14.16x | 0.779 | 11.025 | 14.16x |
| matthews_n100000_k2 | 0.784 | 22.031 | 28.09x | 0.784 | 22.030 | 28.09x |
| cohen_kappa_n100000_k2 | 0.771 | 11.062 | 14.34x | 0.771 | 11.062 | 14.34x |
| mse_n100000_k2 | 0.268 | 0.372 | 1.38x | 0.268 | 0.372 | 1.38x |
| mae_n100000_k2 | 0.269 | 0.372 | 1.38x | 0.269 | 0.372 | 1.38x |
| median_ae_n100000_k2 | 0.680 | 1.879 | 2.76x | 0.698 | 1.879 | 2.69x |
| r2_n100000_k2 | 0.260 | 0.582 | 2.24x | 0.260 | 0.582 | 2.24x |
| confusion_matrix_n100000_k10 | 0.953 | 10.942 | 11.48x | 0.953 | 10.940 | 11.48x |
| accuracy_n100000_k10 | 0.106 | 3.778 | 35.77x | 0.106 | 3.778 | 35.77x |
| precision_recall_f1_macro_n100000_k10 | 0.995 | 13.220 | 13.28x | 0.995 | 13.219 | 13.28x |
| classification_report_n100000_k10 | 0.955 | 29.667 | 31.06x | 0.955 | 29.667 | 31.06x |
| roc_auc_ovr_macro_n100000_k10 | 31.941 | 233.666 | 7.32x | 31.940 | 233.649 | 7.32x |
| balanced_accuracy_n100000_k10 | 0.954 | 11.005 | 11.54x | 0.954 | 11.004 | 11.54x |
| matthews_n100000_k10 | 0.960 | 22.855 | 23.80x | 0.960 | 22.854 | 23.80x |
| cohen_kappa_n100000_k10 | 1.147 | 11.045 | 9.63x | 1.147 | 11.045 | 9.63x |
| mse_n100000_k10 | 0.267 | 0.370 | 1.39x | 0.267 | 0.370 | 1.39x |
| mae_n100000_k10 | 0.268 | 0.370 | 1.38x | 0.268 | 0.370 | 1.38x |
| median_ae_n100000_k10 | 0.670 | 1.877 | 2.80x | 0.711 | 1.877 | 2.64x |
| r2_n100000_k10 | 0.260 | 0.578 | 2.22x | 0.260 | 0.578 | 2.22x |
| confusion_matrix_n1000000_k2 | 8.513 | 103.384 | 12.14x | 8.512 | 103.374 | 12.14x |
| accuracy_n1000000_k2 | 2.158 | 34.513 | 16.00x | 2.157 | 34.507 | 15.99x |
| precision_recall_f1_macro_n1000000_k2 | 8.602 | 113.105 | 13.15x | 8.602 | 113.093 | 13.15x |
| classification_report_n1000000_k2 | 8.571 | 220.811 | 25.76x | 8.573 | 220.807 | 25.76x |
| roc_auc_binary_n1000000_k2 | 43.992 | 315.546 | 7.17x | 43.988 | 315.545 | 7.17x |
| balanced_accuracy_n1000000_k2 | 8.556 | 103.344 | 12.08x | 8.555 | 103.331 | 12.08x |
| matthews_n1000000_k2 | 8.541 | 208.277 | 24.38x | 8.542 | 208.256 | 24.38x |
| cohen_kappa_n1000000_k2 | 8.452 | 103.556 | 12.25x | 8.453 | 103.546 | 12.25x |
| mse_n1000000_k2 | 2.682 | 2.015 | 0.75x | 2.682 | 2.014 | 0.75x |
| mae_n1000000_k2 | 2.690 | 2.023 | 0.75x | 2.690 | 2.022 | 0.75x |
| median_ae_n1000000_k2 | 6.546 | 15.801 | 2.41x | 6.644 | 15.799 | 2.38x |
| r2_n1000000_k2 | 2.601 | 3.967 | 1.53x | 2.601 | 3.966 | 1.53x |
| confusion_matrix_n1000000_k10 | 10.050 | 103.159 | 10.26x | 10.050 | 103.149 | 10.26x |
| accuracy_n1000000_k10 | 3.139 | 34.457 | 10.98x | 3.139 | 34.449 | 10.97x |
| precision_recall_f1_macro_n1000000_k10 | 10.147 | 119.886 | 11.81x | 10.146 | 119.877 | 11.82x |
| classification_report_n1000000_k10 | 10.078 | 247.751 | 24.58x | 10.076 | 247.709 | 24.58x |
| balanced_accuracy_n1000000_k10 | 9.983 | 103.617 | 10.38x | 9.982 | 103.612 | 10.38x |
| matthews_n1000000_k10 | 10.078 | 216.025 | 21.43x | 10.078 | 216.016 | 21.44x |
| cohen_kappa_n1000000_k10 | 9.770 | 103.433 | 10.59x | 9.768 | 103.417 | 10.59x |
| mse_n1000000_k10 | 5.834 | 2.028 | 0.35x | 5.833 | 2.028 | 0.35x |
| mae_n1000000_k10 | 5.827 | 2.055 | 0.35x | 5.827 | 2.055 | 0.35x |
| median_ae_n1000000_k10 | 6.608 | 15.729 | 2.38x | 6.743 | 15.729 | 2.33x |
| r2_n1000000_k10 | 2.828 | 4.027 | 1.42x | 2.827 | 4.026 | 1.42x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.75x
  mae_n1000000_k2                  0.75x
  mse_n1000000_k10                 0.35x
  mae_n1000000_k10                 0.35x

<!-- markdownlint-enable MD060 -->

### compare-persistence

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.661 | 9.870 | 2.12x | 4.956 | 9.869 | 1.99x |
| tokenizer_json_wordpiece | 11.993 | 17.340 | 1.45x | 12.330 | 17.336 | 1.41x |
| tokenizer_json_unigram | 10.820 | 42.141 | 3.89x | 11.240 | 42.133 | 3.75x |
| spiece_model | 5.133 | 30.143 | 5.87x | 5.390 | 30.142 | 5.59x |
| tfidf_save | 1.884 | 2.423 | 1.29x | 1.984 | 2.423 | 1.22x |
| tfidf_load | 4.953 | 4.306 | 0.87x | 6.235 | 4.306 | 0.69x |
| embedding_index_save | 5.267 | 4.135 | 0.79x | 5.969 | 4.134 | 0.69x |
| embedding_index_load | 4.737 | 1.634 | 0.35x | 5.156 | 1.634 | 0.32x |
| embedding_index_load_file | 5.875 | 0.892 | 0.15x | 6.459 | 0.890 | 0.14x |
| embedding_index_load_memory | 3.705 | 1.576 | 0.43x | 4.231 | 1.576 | 0.37x |
| embedding_index_view_floor | 0.000 | 0.001 | 80.37x | 0.000 | 0.001 | 80.36x |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.

<!-- markdownlint-enable MD060 -->
