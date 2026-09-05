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

_As of 2026-09-05, measured at commit `65c60215bbd2020a40d02b0bc6f196c29d47d58a`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | CorpusSize | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|----------:|------:|--------:|--------:|-------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **4.904 μs** |   **0.5573 μs** | **0.0305 μs** |  **1.00** |    **0.01** |  **0.0305** |      **-** |     **2.6 KB** |        **1.00** |
| EmbedBatch         | 1          |     4.952 μs |   0.7223 μs | 0.0396 μs |  1.01 |    0.01 |  0.0305 |      - |       3 KB |        1.15 |
| EmbedBatchBucketed | 1          |     5.132 μs |   1.1645 μs | 0.0638 μs |  1.05 |    0.01 |  0.0305 |      - |       3 KB |        1.15 |
|                    |            |              |             |           |       |         |         |        |            |             |
| **UnitLoop**           | **8**          |    **76.509 μs** |  **50.8505 μs** | **2.7873 μs** |  **1.00** |    **0.04** |  **1.0986** |      **-** |   **94.76 KB** |        **1.00** |
| EmbedBatch         | 8          |    43.462 μs |  16.5236 μs | 0.9057 μs |  0.57 |    0.02 |  1.0376 |      - |   87.78 KB |        0.93 |
| EmbedBatchBucketed | 8          |    43.545 μs |   3.9979 μs | 0.2191 μs |  0.57 |    0.02 |  1.0376 |      - |   87.78 KB |        0.93 |
|                    |            |              |             |           |       |         |         |        |            |             |
| **UnitLoop**           | **32**         |   **286.432 μs** |  **13.3049 μs** | **0.7293 μs** |  **1.00** |    **0.00** |  **3.9063** |      **-** |  **334.02 KB** |        **1.00** |
| EmbedBatch         | 32         |   154.079 μs |  14.9759 μs | 0.8209 μs |  0.54 |    0.00 |  3.6621 | 0.2441 |  306.63 KB |        0.92 |
| EmbedBatchBucketed | 32         |   146.468 μs |  17.6647 μs | 0.9683 μs |  0.51 |    0.00 |  3.4180 |      - |  293.12 KB |        0.88 |
|                    |            |              |             |           |       |         |         |        |            |             |
| **UnitLoop**           | **128**        | **1,152.502 μs** | **114.0317 μs** | **6.2505 μs** |  **1.00** |    **0.01** | **15.6250** |      **-** | **1336.03 KB** |        **1.00** |
| EmbedBatch         | 128        |   590.494 μs |  68.8198 μs | 3.7722 μs |  0.51 |    0.00 | 14.6484 | 1.9531 | 1225.66 KB |        0.92 |
| EmbedBatchBucketed | 128        |   560.819 μs |  51.7100 μs | 2.8344 μs |  0.49 |    0.00 | 13.6719 | 1.9531 | 1158.15 KB |        0.87 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BkTreeBenchmarks-report-github

_As of 2026-09-03, measured at commit `3aab1281a849e2b8790dd705f6e031dc5a79eb73`._

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

| Method             | Radius | Shape     | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------- |------- |---------- |----------:|----------:|---------:|------:|--------:|-----------:|------------:|
| **LengthFilteredScan** | **1**      | **clustered** | **131.38 ms** |  **1.351 ms** | **0.074 ms** |  **1.00** |    **0.00** |   **27.25 KB** |        **1.00** |
| TreeWithinDistance | 1      | clustered |  65.92 ms | 10.207 ms | 0.560 ms |  0.50 |    0.00 |  103.68 KB |        3.80 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **1**      | **uniform**   | **138.20 ms** | **54.221 ms** | **2.972 ms** |  **1.00** |    **0.03** |   **23.86 KB** |        **1.00** |
| TreeWithinDistance | 1      | uniform   |  61.04 ms |  9.567 ms | 0.524 ms |  0.44 |    0.01 |  116.47 KB |        4.88 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **2**      | **clustered** | **199.15 ms** |  **1.807 ms** | **0.099 ms** |  **1.00** |    **0.00** |  **103.44 KB** |        **1.00** |
| TreeWithinDistance | 2      | clustered | 240.23 ms |  4.624 ms | 0.253 ms |  1.21 |    0.00 |     259 KB |        2.50 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **2**      | **uniform**   | **204.82 ms** | **17.028 ms** | **0.933 ms** |  **1.00** |    **0.01** |   **54.65 KB** |        **1.00** |
| TreeWithinDistance | 2      | uniform   | 210.37 ms | 14.949 ms | 0.819 ms |  1.03 |    0.01 |   192.8 KB |        3.53 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **3**      | **clustered** | **250.41 ms** |  **4.893 ms** | **0.268 ms** |  **1.00** |    **0.00** |   **949.9 KB** |        **1.00** |
| TreeWithinDistance | 3      | clustered | 339.76 ms | 47.366 ms | 2.596 ms |  1.36 |    0.01 | 1366.63 KB |        1.44 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **3**      | **uniform**   | **256.46 ms** |  **3.980 ms** | **0.218 ms** |  **1.00** |    **0.00** |  **741.56 KB** |        **1.00** |
| TreeWithinDistance | 3      | uniform   | 310.25 ms | 18.977 ms | 1.040 ms |  1.21 |    0.00 | 1152.95 KB |        1.55 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **4**      | **clustered** | **293.65 ms** | **18.694 ms** | **1.025 ms** |  **1.00** |    **0.00** | **5113.56 KB** |        **1.00** |
| TreeWithinDistance | 4      | clustered | 391.61 ms | 20.324 ms | 1.114 ms |  1.33 |    0.01 |  7216.2 KB |        1.41 |
|                    |        |           |           |           |          |       |         |            |             |
| **LengthFilteredScan** | **4**      | **uniform**   | **299.08 ms** | **13.015 ms** | **0.713 ms** |  **1.00** |    **0.00** | **5514.13 KB** |        **1.00** |
| TreeWithinDistance | 4      | uniform   | 395.35 ms | 39.597 ms | 2.170 ms |  1.32 |    0.01 |  7964.5 KB |        1.44 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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

| Method | length | Mean          | Error         | StdDev     | Allocated |
|------- |------- |--------------:|--------------:|-----------:|----------:|
| **Latin**  | **1000**   |      **49.88 μs** |      **1.510 μs** |   **0.083 μs** |         **-** |
| Cjk    | 1000   |      61.47 μs |      3.069 μs |   0.168 μs |         - |
| **Latin**  | **10000**  |   **5,486.90 μs** |    **659.114 μs** |  **36.128 μs** |         **-** |
| Cjk    | 10000  |   6,644.11 μs |    616.160 μs |  33.774 μs |         - |
| **Latin**  | **65536**  | **202,692.29 μs** | **13,945.696 μs** | **764.411 μs** |         **-** |

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

_As of 2026-09-05, measured at commit `65c60215bbd2020a40d02b0bc6f196c29d47d58a`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method  | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0      | Allocated | Alloc Ratio |
|-------- |---------:|----------:|---------:|------:|--------:|----------:|----------:|------------:|
| Unigram | 279.2 ms |  15.50 ms |  0.85 ms |  1.00 |    0.00 |         - |  30.32 MB |        1.00 |
| Bpe     | 512.3 ms | 341.78 ms | 18.73 ms |  1.84 |    0.06 | 1000.0000 | 112.18 MB |        3.70 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-09-05, measured at commit `65c60215bbd2020a40d02b0bc6f196c29d47d58a`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                    | Length | Mean      | Error     | StdDev   | Gen0   | Allocated |
|-------------------------- |------- |----------:|----------:|---------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |  **90.85 μs** | **30.156 μs** | **1.653 μs** | **0.2441** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **201.42 μs** |  **3.403 μs** | **0.187 μs** | **0.4883** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **450.25 μs** | **11.251 μs** | **0.617 μs** | **0.4883** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **934.39 μs** | **52.908 μs** | **2.900 μs** | **0.9766** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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
| **DpGroup**    | **cjk**      |  **19.86 μs** |  **1.220 μs** | **0.067 μs** |         **-** |
| MyersGroup | cjk      | 242.39 μs | 29.731 μs | 1.630 μs |         - |
| **DpGroup**    | **latin**    |  **10.01 μs** |  **2.930 μs** | **0.161 μs** |         **-** |
| MyersGroup | latin    | 130.84 μs |  7.952 μs | 0.436 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.DecompositionBenchmarks-report-github

_As of 2026-09-03, measured at commit `3aab1281a849e2b8790dd705f6e031dc5a79eb73`._

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

| Method                                    | Mean      | Error     | StdDev   | Ratio | RatioSD |
|------------------------------------------ |----------:|----------:|---------:|------:|--------:|
| TruncatedSvd_Rank20                       |  28.62 ms |  3.346 ms | 0.183 ms |  1.00 |    0.01 |
| Nmf_Rank20                                | 205.10 ms | 41.921 ms | 2.298 ms |  7.17 |    0.08 |
| MlNet_ProjectToPrincipalComponents_Rank20 |  24.07 ms |  1.707 ms | 0.094 ms |  0.84 |    0.01 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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

| Method         | Mean         | Error        | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|-------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     98.63 ns |     0.614 ns |  0.034 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 12,596.02 ns | 1,182.564 ns | 64.820 ns | 127.71 |    0.57 |      - |         - |          NA |
| TokenSortRatio |  1,030.34 ns |    55.320 ns |  3.032 ns |  10.45 |    0.03 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,316.62 ns |   269.808 ns | 14.789 ns |  33.63 |    0.13 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,692.34 ns |   183.814 ns | 10.075 ns |  47.58 |    0.09 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzIncumbentBenchmarks-report-github

_As of 2026-09-03, measured at commit `3aab1281a849e2b8790dd705f6e031dc5a79eb73`._

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

| Method     | Operation     | Mean         | Error        | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------- |-------------- |-------------:|-------------:|----------:|------:|-------:|----------:|------------:|
| **Lodestar**   | **Ratio**         |     **97.71 ns** |     **0.394 ns** |  **0.022 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| FuzzySharp | Ratio         |    212.02 ns |     3.875 ns |  0.212 ns |  2.17 | 0.0048 |      80 B |          NA |
|            |               |              |              |           |       |        |           |             |
| **Lodestar**   | **PartialRatio**  | **11,360.93 ns** |    **77.125 ns** |  **4.227 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| FuzzySharp | PartialRatio  |  9,703.38 ns | 1,051.427 ns | 57.632 ns |  0.85 |      - |     160 B |          NA |
|            |               |              |              |           |       |        |           |             |
| **Lodestar**   | **TokenSetRatio** |  **1,166.29 ns** |   **116.269 ns** |  **6.373 ns** |  **1.00** | **0.0858** |    **1448 B** |        **1.00** |
| FuzzySharp | TokenSetRatio |  2,097.68 ns |    71.263 ns |  3.906 ns |  1.80 | 0.1144 |    1944 B |        1.34 |
|            |               |              |              |           |       |        |           |             |
| **Lodestar**   | **WRatio**        |  **2,339.20 ns** |    **71.058 ns** |  **3.895 ns** |  **1.00** | **0.1640** |    **2760 B** |        **1.00** |
| FuzzySharp | WRatio        |  4,834.59 ns |   302.575 ns | 16.585 ns |  2.07 | 0.1831 |    3128 B |        1.13 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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
| **Distance_Utf16**             | **8**      |      **27.08 ns** |      **6.878 ns** |     **0.377 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     133.81 ns |      4.174 ns |     0.229 ns |  4.94 |    0.06 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      32.47 ns |      1.660 ns |     0.091 ns |  1.20 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.94 ns |      0.911 ns |     0.050 ns |  0.99 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **29.47 ns** |      **0.332 ns** |     **0.018 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     145.72 ns |      1.873 ns |     0.103 ns |  4.95 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.72 ns |      1.386 ns |     0.076 ns |  0.97 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      31.94 ns |      0.389 ns |     0.021 ns |  1.08 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.83 ns** |      **0.811 ns** |     **0.044 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     160.18 ns |      1.498 ns |     0.082 ns |  5.20 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.57 ns |      2.621 ns |     0.144 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      30.92 ns |      5.696 ns |     0.312 ns |  1.00 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **35.04 ns** |      **0.864 ns** |     **0.047 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     185.86 ns |      3.551 ns |     0.195 ns |  5.30 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      35.46 ns |      1.602 ns |     0.088 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      32.70 ns |      0.520 ns |     0.028 ns |  0.93 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **54.34 ns** |      **5.575 ns** |     **0.306 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     640.59 ns |    823.523 ns |    45.140 ns | 11.79 |    0.72 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      56.72 ns |      1.464 ns |     0.080 ns |  1.04 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      62.86 ns |      5.926 ns |     0.325 ns |  1.16 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **61.31 ns** |      **8.337 ns** |     **0.457 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,021.38 ns |     13.507 ns |     0.740 ns | 16.66 |    0.11 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      65.00 ns |      0.080 ns |     0.004 ns |  1.06 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      59.30 ns |      0.358 ns |     0.020 ns |  0.97 |    0.01 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **929.71 ns** |     **24.684 ns** |     **1.353 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  19,735.29 ns |  3,260.514 ns |   178.720 ns | 21.23 |    0.17 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     935.83 ns |      5.647 ns |     0.310 ns |  1.01 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     922.92 ns |     36.848 ns |     2.020 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **7,692.32 ns** |    **184.672 ns** |    **10.122 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 340,680.77 ns | 37,536.455 ns | 2,057.500 ns | 44.29 |    0.24 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   7,810.76 ns |     46.213 ns |     2.533 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   7,524.96 ns |    306.536 ns |    16.802 ns |  0.98 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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

| Method     | Band | Mean         | Error         | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|--------------:|-------------:|------:|--------:|----------:|------------:|
| **Dp**         | **8**    |    **131.40 ns** |      **2.661 ns** |     **0.146 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     55.74 ns |      0.700 ns |     0.038 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    132.90 ns |     26.941 ns |     1.477 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 8    |     97.37 ns |     16.731 ns |     0.917 ns |  0.74 |    0.01 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **12**   |    **216.28 ns** |     **13.870 ns** |     **0.760 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |     61.83 ns |      4.994 ns |     0.274 ns |  0.29 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    226.69 ns |    215.078 ns |    11.789 ns |  1.05 |    0.05 |         - |          NA |
| Kernel_Cjk | 12   |    109.71 ns |      1.082 ns |     0.059 ns |  0.51 |    0.00 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **14**   |    **286.50 ns** |    **319.825 ns** |    **17.531 ns** |  **1.00** |    **0.08** |         **-** |          **NA** |
| Kernel     | 14   |     67.95 ns |      1.052 ns |     0.058 ns |  0.24 |    0.01 |         - |          NA |
| Dp_Cjk     | 14   |    278.05 ns |    175.269 ns |     9.607 ns |  0.97 |    0.06 |         - |          NA |
| Kernel_Cjk | 14   |    114.26 ns |     13.695 ns |     0.751 ns |  0.40 |    0.02 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **16**   |    **355.51 ns** |    **138.248 ns** |     **7.578 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 16   |     74.95 ns |      3.890 ns |     0.213 ns |  0.21 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    373.36 ns |    134.067 ns |     7.349 ns |  1.05 |    0.03 |         - |          NA |
| Kernel_Cjk | 16   |    118.45 ns |      1.732 ns |     0.095 ns |  0.33 |    0.01 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **18**   |    **434.53 ns** |     **14.541 ns** |     **0.797 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 18   |     73.94 ns |      2.507 ns |     0.137 ns |  0.17 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    487.12 ns |    124.510 ns |     6.825 ns |  1.12 |    0.01 |         - |          NA |
| Kernel_Cjk | 18   |    124.39 ns |      0.742 ns |     0.041 ns |  0.29 |    0.00 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **20**   |    **753.60 ns** |    **238.204 ns** |    **13.057 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 20   |     79.67 ns |      0.908 ns |     0.050 ns |  0.11 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    772.15 ns |     15.748 ns |     0.863 ns |  1.02 |    0.02 |         - |          NA |
| Kernel_Cjk | 20   |    128.96 ns |      0.575 ns |     0.032 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **24**   |    **983.02 ns** |    **410.288 ns** |    **22.489 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 24   |     88.30 ns |      1.447 ns |     0.079 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |  1,001.04 ns |     31.255 ns |     1.713 ns |  1.02 |    0.02 |         - |          NA |
| Kernel_Cjk | 24   |    141.09 ns |      2.029 ns |     0.111 ns |  0.14 |    0.00 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **32**   |  **1,521.89 ns** |     **57.679 ns** |     **3.162 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    105.06 ns |      2.848 ns |     0.156 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,568.05 ns |    340.749 ns |    18.678 ns |  1.03 |    0.01 |         - |          NA |
| Kernel_Cjk | 32   |    165.62 ns |      2.949 ns |     0.162 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **48**   |  **3,215.82 ns** |    **142.544 ns** |     **7.813 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 48   |    130.81 ns |     22.515 ns |     1.234 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,184.97 ns |    844.431 ns |    46.286 ns |  0.99 |    0.01 |         - |          NA |
| Kernel_Cjk | 48   |    240.96 ns |      5.382 ns |     0.295 ns |  0.07 |    0.00 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **64**   |  **5,651.92 ns** |  **8,049.844 ns** |   **441.239 ns** |  **1.00** |    **0.10** |         **-** |          **NA** |
| Kernel     | 64   |    160.68 ns |      6.224 ns |     0.341 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,079.71 ns |     19.615 ns |     1.075 ns |  0.90 |    0.06 |         - |          NA |
| Kernel_Cjk | 64   |    292.85 ns |     19.977 ns |     1.095 ns |  0.05 |    0.00 |         - |          NA |
|            |      |              |               |              |       |         |           |             |
| **Dp**         | **96**   | **11,468.38 ns** |  **1,820.294 ns** |    **99.776 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 96   |    765.61 ns |     41.290 ns |     2.263 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 12,407.28 ns | 29,301.347 ns | 1,606.106 ns |  1.08 |    0.12 |         - |          NA |
| Kernel_Cjk | 96   |  1,039.72 ns |     66.910 ns |     3.668 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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
| **Distance_Utf16**             | **8**      |     **24.93 ns** |   **0.137 ns** |  **0.008 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    118.18 ns |   3.041 ns |  0.167 ns |  4.74 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     24.94 ns |   0.996 ns |  0.055 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **269.33 ns** |   **5.049 ns** |  **0.277 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    688.92 ns | 493.986 ns | 27.077 ns |  2.56 |    0.09 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    271.62 ns |   0.984 ns |  0.054 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **512**    | **14,531.78 ns** | **946.264 ns** | **51.868 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 16,977.75 ns | 157.671 ns |  8.642 ns |  1.17 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,440.99 ns | 920.715 ns | 50.467 ns |  0.99 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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

| Method             | Length | Distinct | Mean         | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |-------------:|------------:|----------:|------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |     **332.9 ns** |    **97.80 ns** |   **5.36 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     238.3 ns |    13.59 ns |   0.74 ns |  0.72 |    0.01 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **336.4 ns** |    **94.19 ns** |   **5.16 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     243.7 ns |    36.56 ns |   2.00 ns |  0.72 |    0.01 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **422.7 ns** |     **5.30 ns** |   **0.29 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     321.1 ns |    12.66 ns |   0.69 ns |  0.76 |    0.00 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **433.2 ns** |    **10.09 ns** |   **0.55 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     325.4 ns |     4.67 ns |   0.26 ns |  0.75 |    0.00 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **504.8 ns** |    **38.04 ns** |   **2.09 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     408.1 ns |     3.31 ns |   0.18 ns |  0.81 |    0.00 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **525.4 ns** |   **148.21 ns** |   **8.12 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     418.7 ns |    19.06 ns |   1.04 ns |  0.80 |    0.01 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **590.4 ns** |    **70.30 ns** |   **3.85 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,299.3 ns |    56.68 ns |   3.11 ns |  2.20 |    0.01 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **608.0 ns** |    **15.37 ns** |   **0.84 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,309.8 ns |    21.42 ns |   1.17 ns |  2.15 |    0.00 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,527.7 ns** |   **236.64 ns** |  **12.97 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,501.3 ns |    61.49 ns |   3.37 ns |  2.18 |    0.01 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,616.2 ns** |    **56.39 ns** |   **3.09 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,466.7 ns |   227.50 ns |  12.47 ns |  2.09 |    0.00 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **18,584.9 ns** | **1,606.69 ns** |  **88.07 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  65,739.9 ns |   477.93 ns |  26.20 ns |  3.54 |    0.01 |         - |          NA |
|                    |        |          |              |             |           |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **441,502.1 ns** | **7,369.56 ns** | **403.95 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  64,802.7 ns |   615.91 ns |  33.76 ns |  0.15 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinIncumbentBenchmarks-report-github

_As of 2026-09-03, measured at commit `3aab1281a849e2b8790dd705f6e031dc5a79eb73`._

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

| Method               | Length | Mean          | Error         | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |------- |--------------:|--------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| **Lodestar**             | **8**      |      **24.48 ns** |      **0.221 ns** |   **0.012 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Fastenshtein         | 8      |      74.96 ns |      4.393 ns |   0.241 ns |  3.06 |    0.01 | 0.0033 |      56 B |          NA |
| Quickenshtein        | 8      |      81.10 ns |      1.211 ns |   0.066 ns |  3.31 |    0.00 |      - |         - |          NA |
| F23_StringSimilarity | 8      |     182.92 ns |      4.110 ns |   0.225 ns |  7.47 |    0.01 | 0.0076 |     128 B |          NA |
|                      |        |               |               |            |       |         |        |           |             |
| **Lodestar**             | **64**     |     **275.71 ns** |     **10.843 ns** |   **0.594 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Fastenshtein         | 64     |   6,189.96 ns |  3,491.096 ns | 191.359 ns | 22.45 |    0.60 | 0.0153 |     280 B |          NA |
| Quickenshtein        | 64     |   1,420.40 ns |     16.402 ns |   0.899 ns |  5.15 |    0.01 |      - |         - |          NA |
| F23_StringSimilarity | 64     |  11,190.47 ns |    776.624 ns |  42.569 ns | 40.59 |    0.15 | 0.0305 |     576 B |          NA |
|                      |        |               |               |            |       |         |        |           |             |
| **Lodestar**             | **512**    |  **14,273.45 ns** |    **260.295 ns** |  **14.268 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Fastenshtein         | 512    | 427,384.33 ns | 10,695.151 ns | 586.237 ns | 29.94 |    0.04 |      - |    2072 B |          NA |
| Quickenshtein        | 512    |  37,311.35 ns |    811.268 ns |  44.468 ns |  2.61 |    0.00 |      - |         - |          NA |
| F23_StringSimilarity | 512    | 713,556.64 ns | 11,443.960 ns | 627.282 ns | 49.99 |    0.06 |      - |    4161 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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

| Method         | Samples | Classes | Mean            | Error           | StdDev       | Gen0   | Allocated |
|--------------- |-------- |-------- |----------------:|----------------:|-------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |      **7,774.0 ns** |       **504.02 ns** |     **27.63 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7,225.8 ns |       561.51 ns |     30.78 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |        917.3 ns |        23.56 ns |      1.29 ns |      - |         - |
| F1Macro        | 1000    | 2       |      7,636.7 ns |       773.41 ns |     42.39 ns | 0.0153 |     472 B |
| Report         | 1000    | 2       |     10,619.7 ns |       567.67 ns |     31.12 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7,854.1 ns** |       **310.91 ns** |     **17.04 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7,717.4 ns |       286.41 ns |     15.70 ns | 0.0610 |    1248 B |
| AccuracyScore  | 1000    | 10      |        920.2 ns |        26.33 ns |      1.44 ns |      - |         - |
| F1Macro        | 1000    | 10      |      8,219.6 ns |       450.15 ns |     24.67 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     15,596.6 ns |     1,401.07 ns |     76.80 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **867,234.2 ns** |    **67,120.82 ns** |  **3,679.12 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    811,179.4 ns |    43,693.10 ns |  2,394.97 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    163,486.8 ns |     3,646.53 ns |    199.88 ns |      - |         - |
| F1Macro        | 100000  | 2       |    825,132.7 ns |    29,711.15 ns |  1,628.57 ns |      - |     473 B |
| Report         | 100000  | 2       |    865,724.2 ns |    96,967.06 ns |  5,315.09 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **975,566.4 ns** |    **34,781.98 ns** |  **1,906.52 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    950,285.7 ns |    20,565.50 ns |  1,127.26 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    263,880.0 ns |    17,880.62 ns |    980.10 ns |      - |         - |
| F1Macro        | 100000  | 10      |    939,536.6 ns |    55,303.50 ns |  3,031.37 ns |      - |    1665 B |
| Report         | 100000  | 10      |    994,431.2 ns |    56,460.41 ns |  3,094.79 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,634,001.0 ns** |   **357,250.43 ns** | **19,582.10 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,334,313.7 ns |   764,099.46 ns | 41,882.87 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,747,005.9 ns |    45,049.98 ns |  2,469.34 ns |      - |         - |
| F1Macro        | 1000000 | 2       |  8,852,937.2 ns |   601,609.40 ns | 32,976.24 ns |      - |     484 B |
| Report         | 1000000 | 2       |  8,738,144.9 ns |   176,826.05 ns |  9,692.43 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      |  **9,970,917.8 ns** |   **215,122.38 ns** | **11,791.58 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      |  9,281,910.0 ns |   499,733.99 ns | 27,392.11 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  2,686,993.0 ns |    14,994.69 ns |    821.91 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 10,055,870.5 ns | 1,777,224.36 ns | 97,415.66 ns |      - |    1676 B |
| Report         | 1000000 | 10      |  9,467,565.6 ns |   398,588.47 ns | 21,847.98 ns |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsIncumbentBenchmarks-report-github

_As of 2026-09-03, measured at commit `3aab1281a849e2b8790dd705f6e031dc5a79eb73`._

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

| Method   | Samples | Request       | Mean         | Error        | StdDev      | Ratio  | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------- |-------- |-------------- |-------------:|-------------:|------------:|-------:|--------:|---------:|---------:|---------:|-----------:|------------:|
| **Lodestar** | **100000**  | **Bundle**        |   **8,109.6 μs** |     **84.32 μs** |     **4.62 μs** |   **1.00** |    **0.00** |        **-** |        **-** |        **-** |     **1016 B** |        **1.00** |
| MlNet    | 100000  | Bundle        |  33,839.7 μs |  1,491.17 μs |    81.74 μs |   4.17 |    0.01 | 600.0000 | 600.0000 | 600.0000 |  5089210 B |    5,009.06 |
|          |         |               |              |              |             |        |         |          |          |          |            |             |
| **Lodestar** | **100000**  | **AccuracyAlone** |     **258.0 μs** |     **20.41 μs** |     **1.12 μs** |   **1.00** |    **0.01** |        **-** |        **-** |        **-** |          **-** |          **NA** |
| MlNet    | 100000  | AccuracyAlone |  34,525.5 μs |    172.87 μs |     9.48 μs | 133.83 |    0.50 | 666.6667 | 666.6667 | 666.6667 |  5089833 B |          NA |
|          |         |               |              |              |             |        |         |          |          |          |            |             |
| **Lodestar** | **1000000** | **Bundle**        | **103,325.5 μs** | **50,635.55 μs** | **2,775.51 μs** |   **1.00** |    **0.03** |        **-** |        **-** |        **-** |          **-** |          **NA** |
| MlNet    | 1000000 | Bundle        | 220,544.2 μs |  5,120.47 μs |   280.67 μs |   2.14 |    0.05 |        - |        - |        - | 23232104 B |          NA |
|          |         |               |              |              |             |        |         |          |          |          |            |             |
| **Lodestar** | **1000000** | **AccuracyAlone** |   **2,603.8 μs** |     **67.39 μs** |     **3.69 μs** |   **1.00** |    **0.00** |        **-** |        **-** |        **-** |          **-** |          **NA** |
| MlNet    | 1000000 | AccuracyAlone | 219,803.4 μs |  3,646.07 μs |   199.85 μs |  84.42 |    0.12 |        - |        - |        - | 23231816 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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
| **Dp**         | **4**    |     **74.39 ns** |     **1.582 ns** |   **0.087 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 4    |     74.11 ns |     1.653 ns |   0.091 ns |  1.00 |    0.00 |         - |          NA |
| Dp_Cjk     | 4    |     73.48 ns |     0.206 ns |   0.011 ns |  0.99 |    0.00 |         - |          NA |
| Kernel_Cjk | 4    |     74.29 ns |    18.528 ns |   1.016 ns |  1.00 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **6**    |    **104.00 ns** |     **2.163 ns** |   **0.119 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 6    |     77.37 ns |     5.282 ns |   0.290 ns |  0.74 |    0.00 |         - |          NA |
| Dp_Cjk     | 6    |    108.62 ns |     3.954 ns |   0.217 ns |  1.04 |    0.00 |         - |          NA |
| Kernel_Cjk | 6    |    132.84 ns |     2.087 ns |   0.114 ns |  1.28 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **8**    |    **157.20 ns** |     **5.006 ns** |   **0.274 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     91.81 ns |    52.090 ns |   2.855 ns |  0.58 |    0.02 |         - |          NA |
| Dp_Cjk     | 8    |    156.47 ns |     2.670 ns |   0.146 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    182.18 ns |    57.010 ns |   3.125 ns |  1.16 |    0.02 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **10**   |    **196.56 ns** |     **4.332 ns** |   **0.237 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 10   |     94.76 ns |     3.158 ns |   0.173 ns |  0.48 |    0.00 |         - |          NA |
| Dp_Cjk     | 10   |    197.90 ns |     0.843 ns |   0.046 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 10   |    150.55 ns |     0.760 ns |   0.042 ns |  0.77 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **245.26 ns** |    **21.799 ns** |   **1.195 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 12   |    103.50 ns |    16.861 ns |   0.924 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    260.43 ns |   240.519 ns |  13.184 ns |  1.06 |    0.05 |         - |          NA |
| Kernel_Cjk | 12   |    166.90 ns |     6.082 ns |   0.333 ns |  0.68 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **418.49 ns** |   **316.703 ns** |  **17.360 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel     | 16   |    121.57 ns |     1.364 ns |   0.075 ns |  0.29 |    0.01 |         - |          NA |
| Dp_Cjk     | 16   |    408.76 ns |     4.076 ns |   0.223 ns |  0.98 |    0.03 |         - |          NA |
| Kernel_Cjk | 16   |    181.41 ns |     2.367 ns |   0.130 ns |  0.43 |    0.02 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **846.27 ns** |   **120.948 ns** |   **6.630 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 24   |    158.30 ns |     0.694 ns |   0.038 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    848.53 ns |   211.705 ns |  11.604 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 24   |    230.46 ns |    45.447 ns |   2.491 ns |  0.27 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,466.12 ns** |   **460.879 ns** |  **25.262 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 32   |    192.93 ns |     0.349 ns |   0.019 ns |  0.13 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,450.41 ns |    29.888 ns |   1.638 ns |  0.99 |    0.01 |         - |          NA |
| Kernel_Cjk | 32   |    276.07 ns |     3.937 ns |   0.216 ns |  0.19 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,349.66 ns** | **1,585.173 ns** |  **86.889 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 48   |    264.35 ns |     1.145 ns |   0.063 ns |  0.08 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,310.77 ns |   407.151 ns |  22.317 ns |  0.99 |    0.02 |         - |          NA |
| Kernel_Cjk | 48   |    356.84 ns |     6.018 ns |   0.330 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **6,191.05 ns** |   **344.323 ns** |  **18.873 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 64   |    341.62 ns |    15.102 ns |   0.828 ns |  0.06 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,232.82 ns | 1,186.270 ns |  65.023 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 64   |    438.34 ns |     6.529 ns |   0.358 ns |  0.07 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **14,295.72 ns** | **5,617.976 ns** | **307.940 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 96   |  1,265.09 ns |   573.209 ns |  31.420 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 13,923.77 ns | 2,168.822 ns | 118.880 ns |  0.97 |    0.02 |         - |          NA |
| Kernel_Cjk | 96   |  1,514.62 ns |    73.823 ns |   4.046 ns |  0.11 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-09-05, measured at commit `65c60215bbd2020a40d02b0bc6f196c29d47d58a`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                 | Mean     | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated |
|----------------------- |---------:|----------:|----------:|---------:|---------:|---------:|----------:|
| VocabTxt               | 3.111 ms | 1.4406 ms | 0.0790 ms |  46.8750 |  42.9688 |  31.2500 |   3.62 MB |
| TokenizerJsonWordPiece | 6.199 ms | 1.1376 ms | 0.0624 ms |  62.5000 |  54.6875 |  39.0625 |   5.72 MB |
| TokenizerJsonUnigram   | 7.970 ms | 0.7708 ms | 0.0422 ms |  31.2500 |  31.2500 |  31.2500 |   4.64 MB |
| SpieceModel            | 2.408 ms | 1.5272 ms | 0.0837 ms |  39.0625 |  35.1563 |  23.4375 |   3.36 MB |
| TfidfSave              | 1.405 ms | 0.8856 ms | 0.0485 ms |  21.4844 |  21.4844 |  21.4844 |   2.09 MB |
| TfidfLoad              | 3.262 ms | 4.4610 ms | 0.2445 ms |  23.4375 |  15.6250 |  15.6250 |   2.86 MB |
| EmbeddingIndexSave     | 2.898 ms | 0.8188 ms | 0.0449 ms | 281.2500 | 281.2500 | 281.2500 |  19.87 MB |
| EmbeddingIndexLoad     | 5.837 ms | 6.2853 ms | 0.3445 ms | 148.4375 | 140.6250 | 140.6250 |  15.72 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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

| Method               | Documents | Mean      | Error     | StdDev    | Ratio | Gen0      | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|----------:|----------:|------:|----------:|---------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.561 ms** | **1.3053 ms** | **0.0715 ms** |  **1.00** |  **500.0000** | **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.308 ms | 0.2339 ms | 0.0128 ms |  0.83 |  390.6250 | 187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.438 ms | 1.1347 ms | 0.0622 ms |  0.98 |  507.8125 | 171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.496 ms | 0.2035 ms | 0.0112 ms |  0.86 |  406.2500 | 156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |          |          |           |             |
| **Count**                | **1000**      | **31.093 ms** | **6.1031 ms** | **0.3345 ms** |  **1.00** | **2625.0000** | **875.0000** | **500.0000** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 24.165 ms | 0.8266 ms | 0.0453 ms |  0.78 | 1968.7500 | 781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 29.196 ms | 2.9507 ms | 0.1617 ms |  0.94 | 2562.5000 | 750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 25.070 ms | 4.6621 ms | 0.2555 ms |  0.81 | 2031.2500 | 625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.TokenizerIncumbentBenchmarks-report-github

_As of 2026-09-05, measured at commit `65c60215bbd2020a40d02b0bc6f196c29d47d58a`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method       | Model         | Mean      | Error     | StdDev   | Ratio | Gen0     | Allocated | Alloc Ratio |
|------------- |-------------- |----------:|----------:|---------:|------:|---------:|----------:|------------:|
| **Lodestar**     | **WordPiece**     |  **50.99 ms** |  **6.293 ms** | **0.345 ms** |  **1.00** | **800.0000** |  **68.25 MB** |        **1.00** |
| MlTokenizers | WordPiece     |  40.48 ms |  2.995 ms | 0.164 ms |  0.79 |        - |   3.55 MB |        0.05 |
|              |               |           |           |          |       |          |           |             |
| **Lodestar**     | **SentencePiece** | **295.44 ms** | **22.218 ms** | **1.218 ms** |  **1.00** |        **-** |  **30.33 MB** |        **1.00** |
| MlTokenizers | SentencePiece |  42.56 ms |  4.070 ms | 0.223 ms |  0.14 |        - |   3.09 MB |        0.10 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-09-05, measured at commit `65c60215bbd2020a40d02b0bc6f196c29d47d58a`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | Dim  | Mean     | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------- |----- |---------:|----------:|---------:|------:|----------:|------------:|
| **Dot**    | **384**  | **35.66 ns** |  **1.833 ns** | **0.100 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  | 30.47 ns |  0.405 ns | 0.022 ns |  0.85 |         - |          NA |
|        |      |          |           |          |       |           |             |
| **Dot**    | **768**  | **69.23 ns** |  **1.625 ns** | **0.089 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  | 58.02 ns | 20.841 ns | 1.142 ns |  0.84 |         - |          NA |
|        |      |          |           |          |       |           |             |
| **Dot**    | **1024** | **91.97 ns** |  **3.151 ns** | **0.173 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 75.12 ns |  4.596 ns | 0.252 ns |  0.82 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

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
| **Count**        | **200**       |  **2.959 ms** | **1.9173 ms** | **0.1051 ms** |  **1.00** |    **0.04** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  2.981 ms | 1.5876 ms | 0.0870 ms |  1.01 |    0.04 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.799 ms | 0.3974 ms | 0.0218 ms |  1.29 |    0.04 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.991 ms | 2.1996 ms | 0.1206 ms |  1.01 |    0.05 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.215 ms** | **0.3218 ms** | **0.0176 ms** |  **1.00** |    **0.00** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.453 ms | 0.6354 ms | 0.0348 ms |  1.03 |    0.00 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.921 ms | 0.4909 ms | 0.0269 ms |  1.65 |    0.00 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.041 ms | 0.6258 ms | 0.0343 ms |  0.98 |    0.00 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerIncumbentBenchmarks-report-github

_As of 2026-09-03, measured at commit `3aab1281a849e2b8790dd705f6e031dc5a79eb73`._

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

| Method   | Documents | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0       | Gen1       | Gen2       | Allocated | Alloc Ratio |
|--------- |---------- |-----------:|------------:|-----------:|------:|--------:|-----------:|-----------:|-----------:|----------:|------------:|
| **Lodestar** | **200**       |   **7.037 ms** |   **0.4192 ms** |  **0.0230 ms** |  **1.00** |    **0.00** |   **296.8750** |   **218.7500** |   **140.6250** |   **5.13 MB** |        **1.00** |
| MlNet    | 200       |  63.299 ms | 289.2402 ms | 15.8542 ms |  8.99 |    1.95 |  7500.0000 |  7500.0000 |  7500.0000 |  28.27 MB |        5.52 |
|          |           |            |             |            |       |         |            |            |            |           |             |
| **Lodestar** | **1000**      |  **29.487 ms** |   **5.9944 ms** |  **0.3286 ms** |  **1.00** |    **0.01** |  **2687.5000** |  **2468.7500** |  **1500.0000** |  **24.92 MB** |        **1.00** |
| MlNet    | 1000      | 373.533 ms |  58.7671 ms |  3.2212 ms | 12.67 |    0.16 | 78000.0000 | 78000.0000 | 78000.0000 |  324.7 MB |       13.03 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 116.7 | 26.7 | 4.37x C# faster |
| latin | 32 | 177.6 | 88.7 | 2.00x C# faster |
| latin | 128 | 471.6 | 907.8 | 1.92x Py faster |
| latin | 512 | 4464.3 | 7777.4 | 1.74x Py faster |
| cjk | 8 | 139.9 | 27.6 | 5.08x C# faster |
| cjk | 32 | 326.3 | 229.2 | 1.42x C# faster |
| cjk | 128 | 1902.2 | 1651.7 | 1.15x C# faster |
| cjk | 512 | 14785.2 | 10865.2 | 1.36x C# faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is Hyyro's bit-parallel LLCS above a pattern of 8 and a rolling-row dynamic program below it (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 156.3 | 18.1 | 8.62x C# faster |
| latin | 32 | 287.7 | 157.7 | 1.82x C# faster |
| latin | 128 | 1699.8 | 1545.6 | 1.10x C# faster |
| latin | 512 | 14030.8 | 16310.7 | 1.16x Py faster |
| cjk | 8 | 146.8 | 18.1 | 8.12x C# faster |
| cjk | 32 | 367.3 | 281.3 | 1.31x C# faster |
| cjk | 128 | 2856.9 | 2429.4 | 1.18x C# faster |
| cjk | 512 | 23784.4 | 19860.0 | 1.20x C# faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-09-04, measured at commit `8935765306fce8e251a4916c0ee301a911724174`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.009 | 0.951 | 101.88x | 0.009 | 0.951 | 101.87x |
| accuracy_n1000_k2 | 0.001 | 0.503 | 545.04x | 0.001 | 0.503 | 545.00x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.723 | 225.41x | 0.008 | 1.720 | 225.16x |
| classification_report_n1000_k2 | 0.010 | 6.532 | 631.60x | 0.010 | 6.532 | 631.62x |
| roc_auc_binary_n1000_k2 | 0.015 | 1.879 | 121.83x | 0.015 | 1.879 | 121.81x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.016 | 132.72x | 0.008 | 1.016 | 132.71x |
| matthews_n1000_k2 | 0.008 | 1.905 | 250.15x | 0.008 | 1.905 | 250.12x |
| cohen_kappa_n1000_k2 | 0.008 | 1.061 | 137.95x | 0.008 | 1.061 | 137.94x |
| mse_n1000_k2 | 0.002 | 0.295 | 125.63x | 0.002 | 0.295 | 125.63x |
| mae_n1000_k2 | 0.002 | 0.296 | 121.46x | 0.002 | 0.295 | 121.45x |
| median_ae_n1000_k2 | 0.006 | 0.307 | 47.64x | 0.006 | 0.307 | 47.64x |
| r2_n1000_k2 | 0.003 | 0.358 | 140.16x | 0.003 | 0.358 | 140.14x |
| confusion_matrix_n1000_k10 | 0.009 | 0.950 | 100.97x | 0.009 | 0.950 | 100.96x |
| accuracy_n1000_k10 | 0.001 | 0.511 | 548.79x | 0.001 | 0.511 | 548.79x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.753 | 210.78x | 0.008 | 1.752 | 210.76x |
| classification_report_n1000_k10 | 0.015 | 6.765 | 457.74x | 0.015 | 6.765 | 457.72x |
| roc_auc_ovr_macro_n1000_k10 | 0.546 | 9.693 | 17.75x | 0.546 | 9.692 | 17.75x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.021 | 125.61x | 0.008 | 1.021 | 125.61x |
| matthews_n1000_k10 | 0.008 | 1.942 | 239.60x | 0.008 | 1.942 | 239.58x |
| cohen_kappa_n1000_k10 | 0.008 | 1.066 | 128.23x | 0.008 | 1.066 | 128.22x |
| mse_n1000_k10 | 0.002 | 0.296 | 124.17x | 0.002 | 0.296 | 124.16x |
| mae_n1000_k10 | 0.002 | 0.294 | 121.71x | 0.002 | 0.294 | 121.71x |
| median_ae_n1000_k10 | 0.006 | 0.298 | 47.14x | 0.006 | 0.298 | 47.14x |
| r2_n1000_k10 | 0.003 | 0.351 | 137.65x | 0.003 | 0.351 | 137.65x |
| confusion_matrix_n100000_k2 | 1.003 | 10.586 | 10.55x | 1.003 | 10.585 | 10.55x |
| accuracy_n100000_k2 | 0.170 | 3.734 | 22.00x | 0.170 | 3.734 | 22.00x |
| precision_recall_f1_macro_n100000_k2 | 0.882 | 12.173 | 13.79x | 0.883 | 12.172 | 13.79x |
| classification_report_n100000_k2 | 0.873 | 26.734 | 30.64x | 0.873 | 26.731 | 30.63x |
| roc_auc_binary_n100000_k2 | 3.502 | 26.868 | 7.67x | 3.502 | 26.864 | 7.67x |
| balanced_accuracy_n100000_k2 | 0.863 | 10.830 | 12.55x | 0.863 | 10.829 | 12.55x |
| matthews_n100000_k2 | 0.862 | 21.504 | 24.96x | 0.862 | 21.502 | 24.95x |
| cohen_kappa_n100000_k2 | 0.872 | 10.869 | 12.46x | 0.872 | 10.868 | 12.46x |
| mse_n100000_k2 | 0.239 | 0.438 | 1.83x | 0.239 | 0.438 | 1.83x |
| mae_n100000_k2 | 0.239 | 0.434 | 1.81x | 0.239 | 0.434 | 1.81x |
| median_ae_n100000_k2 | 0.741 | 1.786 | 2.41x | 0.781 | 1.786 | 2.29x |
| r2_n100000_k2 | 0.234 | 0.660 | 2.82x | 0.234 | 0.660 | 2.82x |
| confusion_matrix_n100000_k10 | 0.963 | 10.751 | 11.16x | 0.963 | 10.747 | 11.16x |
| accuracy_n100000_k10 | 0.270 | 3.755 | 13.91x | 0.270 | 3.754 | 13.90x |
| precision_recall_f1_macro_n100000_k10 | 0.995 | 12.937 | 13.00x | 0.995 | 12.936 | 13.00x |
| classification_report_n100000_k10 | 0.981 | 29.352 | 29.93x | 0.981 | 29.350 | 29.93x |
| roc_auc_ovr_macro_n100000_k10 | 36.691 | 218.668 | 5.96x | 36.688 | 218.650 | 5.96x |
| balanced_accuracy_n100000_k10 | 0.972 | 10.755 | 11.07x | 0.972 | 10.755 | 11.07x |
| matthews_n100000_k10 | 0.974 | 22.386 | 23.00x | 0.973 | 22.384 | 22.99x |
| cohen_kappa_n100000_k10 | 0.987 | 10.882 | 11.03x | 0.987 | 10.880 | 11.03x |
| mse_n100000_k10 | 0.241 | 0.434 | 1.80x | 0.241 | 0.434 | 1.80x |
| mae_n100000_k10 | 0.240 | 0.426 | 1.77x | 0.240 | 0.426 | 1.77x |
| median_ae_n100000_k10 | 0.783 | 1.790 | 2.28x | 0.828 | 1.790 | 2.16x |
| r2_n100000_k10 | 0.234 | 0.683 | 2.92x | 0.233 | 0.683 | 2.92x |
| confusion_matrix_n1000000_k2 | 8.628 | 100.347 | 11.63x | 8.628 | 100.312 | 11.63x |
| accuracy_n1000000_k2 | 1.806 | 33.236 | 18.41x | 1.806 | 33.234 | 18.40x |
| precision_recall_f1_macro_n1000000_k2 | 8.874 | 108.022 | 12.17x | 8.873 | 108.013 | 12.17x |
| classification_report_n1000000_k2 | 8.795 | 211.058 | 24.00x | 8.795 | 211.027 | 23.99x |
| roc_auc_binary_n1000000_k2 | 43.347 | 286.264 | 6.60x | 43.345 | 286.261 | 6.60x |
| balanced_accuracy_n1000000_k2 | 8.745 | 100.515 | 11.49x | 8.745 | 100.505 | 11.49x |
| matthews_n1000000_k2 | 8.739 | 201.101 | 23.01x | 8.739 | 201.092 | 23.01x |
| cohen_kappa_n1000000_k2 | 8.736 | 100.361 | 11.49x | 8.735 | 100.358 | 11.49x |
| mse_n1000000_k2 | 2.400 | 1.990 | 0.83x | 2.400 | 1.990 | 0.83x |
| mae_n1000000_k2 | 2.388 | 2.049 | 0.86x | 2.388 | 2.049 | 0.86x |
| median_ae_n1000000_k2 | 7.013 | 13.948 | 1.99x | 7.075 | 13.946 | 1.97x |
| r2_n1000000_k2 | 2.332 | 3.271 | 1.40x | 2.332 | 3.271 | 1.40x |
| confusion_matrix_n1000000_k10 | 9.261 | 98.234 | 10.61x | 9.261 | 98.206 | 10.60x |
| accuracy_n1000000_k10 | 2.710 | 32.659 | 12.05x | 2.710 | 32.652 | 12.05x |
| precision_recall_f1_macro_n1000000_k10 | 9.868 | 112.881 | 11.44x | 9.867 | 112.877 | 11.44x |
| classification_report_n1000000_k10 | 9.701 | 231.952 | 23.91x | 9.701 | 231.927 | 23.91x |
| balanced_accuracy_n1000000_k10 | 9.725 | 98.283 | 10.11x | 9.724 | 98.277 | 10.11x |
| matthews_n1000000_k10 | 9.639 | 203.288 | 21.09x | 9.638 | 203.272 | 21.09x |
| cohen_kappa_n1000000_k10 | 9.557 | 96.544 | 10.10x | 9.556 | 96.536 | 10.10x |
| mse_n1000000_k10 | 2.335 | 1.955 | 0.84x | 2.335 | 1.954 | 0.84x |
| mae_n1000000_k10 | 2.323 | 1.928 | 0.83x | 2.323 | 1.928 | 0.83x |
| median_ae_n1000000_k10 | 7.261 | 13.885 | 1.91x | 7.388 | 13.884 | 1.88x |
| r2_n1000000_k10 | 2.594 | 3.363 | 1.30x | 2.594 | 3.362 | 1.30x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n1000000_k2                  0.83x
  mae_n1000000_k2                  0.86x
  mse_n1000000_k10                 0.84x
  mae_n1000000_k10                 0.83x

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-09-05, measured at commit `65c60215bbd2020a40d02b0bc6f196c29d47d58a`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 2.583 | 8.164 | 3.16x | 2.658 | 8.164 | 3.07x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 7.857 | 14.078 | 1.79x | 8.067 | 14.077 | 1.74x | 706,526 | 706,526 |
| tokenizer_json_unigram | 9.176 | 31.705 | 3.46x | 9.430 | 31.702 | 3.36x | 1,990,038 | 1,990,038 |
| spiece_model | 2.603 | 25.610 | 9.84x | 2.709 | 25.609 | 9.45x | 533,084 | 533,084 |
| tfidf_save | 1.350 | 2.291 | 1.70x | 1.399 | 2.291 | 1.64x | 581,787 | 591,922 |
| tfidf_load | 2.937 | 3.477 | 1.18x | 3.068 | 3.476 | 1.13x | 581,787 | 591,922 |
| embedding_index_save | 4.141 | 1.968 | 0.48x | 4.284 | 1.968 | 0.46x | 20,589,007 | 15,360,128 |
| embedding_index_save_file | 103.344 | 69.020 | 0.67x | 7.228 | 3.315 | 0.46x | 20,589,007 | 15,360,128 |
| embedding_index_load | 6.002 | 1.215 | 0.20x | 6.353 | 1.215 | 0.19x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 7.512 | 1.031 | 0.14x | 7.825 | 1.028 | 0.13x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 4.558 | 1.213 | 0.27x | 4.826 | 1.213 | 0.25x | 20,589,007 | 15,360,128 |
| embedding_index_ingest_npy | 1.743 | 1.215 | 0.70x | 2.023 | 1.215 | 0.60x | 15,360,128 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.000 | 73.64x | 0.000 | 0.000 | 73.64x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 366.574 | 518.448 | 1.41x | 367.017 | 518.303 | 1.41x | 15,250,490 | 14,022,374 |
| embedding_index_load_gzip | 79.070 | 78.898 | 1.00x | 79.951 | 78.894 | 0.99x | 15,250,490 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
