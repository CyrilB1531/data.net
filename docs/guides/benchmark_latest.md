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

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | CorpusSize | Mean         | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|-----------:|------:|--------:|--------:|-------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **6.785 μs** |   **0.2345 μs** |  **0.0129 μs** |  **1.00** |    **0.00** |  **0.1068** |      **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.485 μs |   0.1870 μs |  0.0103 μs |  0.96 |    0.00 |  0.1221 |      - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.745 μs |   0.1141 μs |  0.0063 μs |  0.99 |    0.00 |  0.1221 |      - |    3.16 KB |        1.14 |
|                    |            |              |             |            |       |         |         |        |            |             |
| **UnitLoop**           | **8**          |   **118.275 μs** |   **4.5115 μs** |  **0.2473 μs** |  **1.00** |    **0.00** |  **5.3711** | **0.1221** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    76.678 μs |  53.5995 μs |  2.9380 μs |  0.65 |    0.02 |  5.1270 | 0.2441 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    76.655 μs |   5.3339 μs |  0.2924 μs |  0.65 |    0.00 |  5.1270 | 0.2441 |  127.31 KB |        0.95 |
|                    |            |              |             |            |       |         |         |        |            |             |
| **UnitLoop**           | **32**         |   **427.317 μs** |  **45.6494 μs** |  **2.5022 μs** |  **1.00** |    **0.01** | **19.0430** | **0.4883** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   257.695 μs |   9.7403 μs |  0.5339 μs |  0.60 |    0.00 | 17.5781 | 0.9766 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   248.229 μs |   7.1151 μs |  0.3900 μs |  0.58 |    0.00 | 17.0898 | 0.9766 |   427.8 KB |        0.91 |
|                    |            |              |             |            |       |         |         |        |            |             |
| **UnitLoop**           | **128**        | **1,720.020 μs** |  **77.0050 μs** |  **4.2209 μs** |  **1.00** |    **0.00** | **76.1719** | **3.9063** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        | 1,041.946 μs |  32.2891 μs |  1.7699 μs |  0.61 |    0.00 | 70.3125 | 9.7656 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   987.384 μs | 459.1923 μs | 25.1699 μs |  0.57 |    0.01 | 68.3594 | 9.7656 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | length | Mean          | Error         | StdDev     | Allocated |
|------- |------- |--------------:|--------------:|-----------:|----------:|
| **Latin**  | **1000**   |      **55.01 μs** |      **0.538 μs** |   **0.029 μs** |         **-** |
| Cjk    | 1000   |      61.65 μs |      0.593 μs |   0.032 μs |         - |
| **Latin**  | **10000**  |   **5,556.50 μs** |    **149.100 μs** |   **8.173 μs** |         **-** |
| Cjk    | 10000  |   8,270.07 μs |  1,441.151 μs |  78.994 μs |         - |
| **Latin**  | **65536**  | **197,960.19 μs** | **10,062.359 μs** | **551.552 μs** |         **-** |

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

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method  | Mean     | Error    | StdDev  | Ratio | Gen0       | Allocated | Alloc Ratio |
|-------- |---------:|---------:|--------:|------:|-----------:|----------:|------------:|
| Unigram | 601.4 ms | 40.09 ms | 2.20 ms |  1.00 | 21000.0000 | 519.51 MB |        1.00 |
| Bpe     | 535.1 ms | 18.72 ms | 1.03 ms |  0.89 |  4000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                    | Length | Mean       | Error    | StdDev  | Gen0   | Allocated |
|-------------------------- |------- |-----------:|---------:|--------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |   **114.1 μs** | **15.26 μs** | **0.84 μs** | **0.7324** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **242.5 μs** | **13.32 μs** | **0.73 μs** | **1.4648** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **524.4 μs** | **14.09 μs** | **0.77 μs** | **2.9297** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,115.9 μs** | **83.80 μs** | **4.59 μs** | **5.8594** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Alphabet | Mean       | Error      | StdDev    | Allocated |
|----------- |--------- |-----------:|-----------:|----------:|----------:|
| **DpGroup**    | **cjk**      |  **17.170 μs** |  **0.0677 μs** | **0.0037 μs** |         **-** |
| MyersGroup | cjk      | 240.261 μs | 15.1871 μs | 0.8325 μs |         - |
| **DpGroup**    | **latin**    |   **9.462 μs** |  **0.5874 μs** | **0.0322 μs** |         **-** |
| MyersGroup | latin    | 133.365 μs |  4.8179 μs | 0.2641 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method         | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|-------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     91.02 ns |     8.869 ns |   0.486 ns |   1.00 |    0.01 |      - |         - |          NA |
| PartialRatio   | 12,556.91 ns | 6,881.303 ns | 377.187 ns | 137.97 |    3.65 |      - |         - |          NA |
| TokenSortRatio |  1,068.79 ns |   180.002 ns |   9.867 ns |  11.74 |    0.11 | 0.0515 |    1312 B |          NA |
| TokenSetRatio  |  3,432.67 ns |   282.242 ns |  15.471 ns |  37.72 |    0.23 | 0.2289 |    5760 B |          NA |
| WRatio         |  4,704.60 ns |   170.477 ns |   9.344 ns |  51.69 |    0.25 | 0.2823 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                     | Length | Mean          | Error         | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|--------------:|-------------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **27.90 ns** |      **0.173 ns** |     **0.009 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     137.14 ns |     11.119 ns |     0.609 ns |  4.91 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      27.66 ns |      2.020 ns |     0.111 ns |  0.99 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.10 ns |      0.112 ns |     0.006 ns |  0.94 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **27.89 ns** |      **0.958 ns** |     **0.052 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     146.99 ns |      3.742 ns |     0.205 ns |  5.27 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      29.20 ns |      4.830 ns |     0.265 ns |  1.05 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.44 ns |      0.916 ns |     0.050 ns |  1.02 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.13 ns** |      **0.800 ns** |     **0.044 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     155.50 ns |      7.559 ns |     0.414 ns |  5.16 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.75 ns |      3.332 ns |     0.183 ns |  1.05 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      28.90 ns |      1.827 ns |     0.100 ns |  0.96 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **32.97 ns** |      **0.683 ns** |     **0.037 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     216.15 ns |     12.905 ns |     0.707 ns |  6.56 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      35.93 ns |      1.824 ns |     0.100 ns |  1.09 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      31.52 ns |      0.842 ns |     0.046 ns |  0.96 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **50.31 ns** |      **2.776 ns** |     **0.152 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     638.42 ns |     68.208 ns |     3.739 ns | 12.69 |    0.07 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      59.70 ns |      2.532 ns |     0.139 ns |  1.19 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      55.17 ns |      2.104 ns |     0.115 ns |  1.10 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **61.57 ns** |      **2.036 ns** |     **0.112 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |     786.53 ns |    726.713 ns |    39.834 ns | 12.77 |    0.56 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      65.39 ns |      3.344 ns |     0.183 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      57.66 ns |      1.510 ns |     0.083 ns |  0.94 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **812.74 ns** |     **13.243 ns** |     **0.726 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  18,691.12 ns |    813.769 ns |    44.605 ns | 23.00 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     888.45 ns |      3.381 ns |     0.185 ns |  1.09 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     881.52 ns |      9.358 ns |     0.513 ns |  1.08 |    0.00 |         - |          NA |
|                            |        |               |               |              |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **7,505.99 ns** |    **110.673 ns** |     **6.066 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 323,596.48 ns | 40,418.776 ns | 2,215.489 ns | 43.11 |    0.26 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   7,477.75 ns |    289.251 ns |    15.855 ns |  1.00 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   7,492.70 ns |     75.206 ns |     4.122 ns |  1.00 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**         | **8**    |    **122.44 ns** |     **1.046 ns** |   **0.057 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     55.85 ns |     0.511 ns |   0.028 ns |  0.46 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    126.88 ns |    34.047 ns |   1.866 ns |  1.04 |    0.01 |         - |          NA |
| Kernel_Cjk | 8    |     93.30 ns |     2.313 ns |   0.127 ns |  0.76 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **210.14 ns** |     **8.159 ns** |   **0.447 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |     63.33 ns |     1.346 ns |   0.074 ns |  0.30 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    210.89 ns |    18.113 ns |   0.993 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 12   |    110.69 ns |    28.597 ns |   1.567 ns |  0.53 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **14**   |    **298.07 ns** |     **1.962 ns** |   **0.108 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 14   |     67.30 ns |     0.756 ns |   0.041 ns |  0.23 |    0.00 |         - |          NA |
| Dp_Cjk     | 14   |    267.06 ns |     5.632 ns |   0.309 ns |  0.90 |    0.00 |         - |          NA |
| Kernel_Cjk | 14   |    111.09 ns |     1.353 ns |   0.074 ns |  0.37 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **379.20 ns** |     **2.701 ns** |   **0.148 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 16   |     71.16 ns |     1.994 ns |   0.109 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    380.45 ns |    47.112 ns |   2.582 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 16   |    119.34 ns |    22.366 ns |   1.226 ns |  0.31 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **18**   |    **462.84 ns** |    **22.217 ns** |   **1.218 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 18   |     75.17 ns |     1.532 ns |   0.084 ns |  0.16 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    461.27 ns |     3.995 ns |   0.219 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 18   |    122.75 ns |     6.551 ns |   0.359 ns |  0.27 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **20**   |    **558.71 ns** |     **3.894 ns** |   **0.213 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 20   |     78.55 ns |     2.524 ns |   0.138 ns |  0.14 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    558.36 ns |    11.941 ns |   0.655 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 20   |    128.72 ns |     2.487 ns |   0.136 ns |  0.23 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **740.12 ns** |   **161.509 ns** |   **8.853 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 24   |     86.94 ns |     0.654 ns |   0.036 ns |  0.12 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    799.58 ns |   699.153 ns |  38.323 ns |  1.08 |    0.05 |         - |          NA |
| Kernel_Cjk | 24   |    143.10 ns |     6.612 ns |   0.362 ns |  0.19 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,193.21 ns** |    **46.273 ns** |   **2.536 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    103.55 ns |     0.137 ns |   0.008 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,213.40 ns |   524.337 ns |  28.741 ns |  1.02 |    0.02 |         - |          NA |
| Kernel_Cjk | 32   |    167.81 ns |    21.522 ns |   1.180 ns |  0.14 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **2,794.28 ns** |     **9.694 ns** |   **0.531 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 48   |    125.46 ns |     3.115 ns |   0.171 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  2,794.16 ns |   102.148 ns |   5.599 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 48   |    236.90 ns |     2.389 ns |   0.131 ns |  0.08 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **4,880.58 ns** | **7,458.806 ns** | **408.842 ns** |  **1.00** |    **0.11** |         **-** |          **NA** |
| Kernel     | 64   |    153.00 ns |     2.188 ns |   0.120 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,010.41 ns | 2,493.261 ns | 136.664 ns |  1.03 |    0.08 |         - |          NA |
| Kernel_Cjk | 64   |    286.52 ns |    18.506 ns |   1.014 ns |  0.06 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **11,067.26 ns** | **4,159.950 ns** | **228.021 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 96   |    682.98 ns |    34.199 ns |   1.875 ns |  0.06 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 10,080.22 ns | 1,810.605 ns |  99.245 ns |  0.91 |    0.02 |         - |          NA |
| Kernel_Cjk | 96   |    985.35 ns |     2.356 ns |   0.129 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                     | Length | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **26.93 ns** |     **0.732 ns** |   **0.040 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    137.34 ns |    10.263 ns |   0.563 ns |  5.10 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.74 ns |     1.289 ns |   0.071 ns |  0.96 |    0.00 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **263.86 ns** |    **11.486 ns** |   **0.630 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    717.67 ns |    12.554 ns |   0.688 ns |  2.72 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    269.50 ns |    31.653 ns |   1.735 ns |  1.02 |    0.01 |         - |          NA |
|                            |        |              |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    | **15,527.28 ns** | **1,929.034 ns** | **105.737 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 18,165.53 ns |   369.510 ns |  20.254 ns |  1.17 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 15,139.77 ns |   464.794 ns |  25.477 ns |  0.98 |    0.01 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | Length | Distinct | Mean         | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |-------------:|-------------:|----------:|------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |     **363.7 ns** |      **9.02 ns** |   **0.49 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     221.7 ns |      4.37 ns |   0.24 ns |  0.61 |    0.00 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **355.1 ns** |      **6.77 ns** |   **0.37 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     223.9 ns |      2.10 ns |   0.11 ns |  0.63 |    0.00 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **465.9 ns** |      **2.13 ns** |   **0.12 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     306.5 ns |      2.34 ns |   0.13 ns |  0.66 |    0.00 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **453.4 ns** |     **33.37 ns** |   **1.83 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     313.5 ns |      6.73 ns |   0.37 ns |  0.69 |    0.00 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **542.8 ns** |     **10.42 ns** |   **0.57 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     411.3 ns |      2.68 ns |   0.15 ns |  0.76 |    0.00 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **550.5 ns** |      **9.28 ns** |   **0.51 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     397.3 ns |     17.25 ns |   0.95 ns |  0.72 |    0.00 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **633.8 ns** |     **20.12 ns** |   **1.10 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,383.6 ns |     14.61 ns |   0.80 ns |  2.18 |    0.00 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **659.2 ns** |      **1.69 ns** |   **0.09 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,324.3 ns |    141.98 ns |   7.78 ns |  2.01 |    0.01 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,742.3 ns** |     **45.41 ns** |   **2.49 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,561.9 ns |    150.82 ns |   8.27 ns |  2.03 |    0.00 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,809.6 ns** |     **35.30 ns** |   **1.93 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,557.6 ns |    277.61 ns |  15.22 ns |  1.98 |    0.00 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **19,919.4 ns** |    **738.02 ns** |  **40.45 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  68,020.9 ns | 14,726.80 ns | 807.23 ns |  3.41 |    0.04 |         - |          NA |
|                    |        |          |              |              |           |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **507,750.7 ns** | **12,811.42 ns** | **702.24 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  62,553.0 ns |    105.90 ns |   5.80 ns |  0.12 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method         | Samples | Classes | Mean         | Error       | StdDev     | Gen0   | Allocated |
|--------------- |-------- |-------- |-------------:|------------:|-----------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |     **6.635 μs** |   **0.1985 μs** |  **0.0109 μs** | **0.0076** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     6.455 μs |   0.2572 μs |  0.0141 μs | 0.0076 |     312 B |
| AccuracyScore  | 1000    | 2       |     1.133 μs |   0.0045 μs |  0.0002 μs |      - |         - |
| F1Macro        | 1000    | 2       |     6.567 μs |   0.2006 μs |  0.0110 μs | 0.0153 |     472 B |
| Report         | 1000    | 2       |     9.555 μs |   0.0922 μs |  0.0051 μs | 0.2594 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **6.803 μs** |   **0.0796 μs** |  **0.0044 μs** | **0.0458** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     6.620 μs |   0.3001 μs |  0.0164 μs | 0.0458 |    1248 B |
| AccuracyScore  | 1000    | 10      |     1.133 μs |   0.0341 μs |  0.0019 μs |      - |         - |
| F1Macro        | 1000    | 10      |     6.821 μs |   0.1305 μs |  0.0072 μs | 0.0610 |    1664 B |
| Report         | 1000    | 10      |    14.396 μs |   0.2260 μs |  0.0124 μs | 0.6104 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **760.847 μs** |  **36.1390 μs** |  **1.9809 μs** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   728.818 μs |  21.4795 μs |  1.1774 μs |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   170.353 μs |  25.3851 μs |  1.3914 μs |      - |         - |
| F1Macro        | 100000  | 2       |   739.163 μs |  37.6811 μs |  2.0654 μs |      - |     473 B |
| Report         | 100000  | 2       |   771.007 μs |  91.1608 μs |  4.9968 μs |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **889.305 μs** |  **37.0550 μs** |  **2.0311 μs** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   885.894 μs |  44.8458 μs |  2.4582 μs |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   274.694 μs |  10.6281 μs |  0.5826 μs |      - |         - |
| F1Macro        | 100000  | 10      |   881.760 μs |  30.6264 μs |  1.6787 μs |      - |    1665 B |
| Report         | 100000  | 10      |   873.074 μs |  25.4110 μs |  1.3929 μs |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **7,345.797 μs** | **281.1026 μs** | **15.4082 μs** |      **-** |     **318 B** |
| MatrixWeighted | 1000000 | 2       | 7,577.554 μs | 478.6193 μs | 26.2347 μs |      - |     318 B |
| AccuracyScore  | 1000000 | 2       | 1,973.377 μs |  72.4291 μs |  3.9701 μs |      - |         - |
| F1Macro        | 1000000 | 2       | 7,724.142 μs | 249.1775 μs | 13.6583 μs |      - |     478 B |
| Report         | 1000000 | 2       | 7,524.268 μs | 153.5010 μs |  8.4139 μs |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **8,792.776 μs** | **293.8876 μs** | **16.1090 μs** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 8,719.573 μs | 389.7039 μs | 21.3610 μs |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      | 2,996.003 μs |  60.8285 μs |  3.3342 μs |      - |         - |
| F1Macro        | 1000000 | 10      | 9,144.508 μs | 656.9726 μs | 36.0109 μs |      - |    1676 B |
| Report         | 1000000 | 10      | 9,313.685 μs | 198.9309 μs | 10.9041 μs |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method     | Band | Mean         | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |----- |-------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| **Dp**         | **4**    |     **78.67 ns** |   **1.035 ns** |  **0.057 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 4    |     75.93 ns |   3.649 ns |  0.200 ns |  0.97 |    0.00 |         - |          NA |
| Dp_Cjk     | 4    |     76.73 ns |   0.405 ns |  0.022 ns |  0.98 |    0.00 |         - |          NA |
| Kernel_Cjk | 4    |     75.69 ns |   2.904 ns |  0.159 ns |  0.96 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **6**    |    **105.78 ns** |   **1.510 ns** |  **0.083 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 6    |     76.71 ns |   0.478 ns |  0.026 ns |  0.73 |    0.00 |         - |          NA |
| Dp_Cjk     | 6    |    105.44 ns |   1.396 ns |  0.077 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 6    |    128.27 ns |   0.892 ns |  0.049 ns |  1.21 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **8**    |    **148.73 ns** |  **11.171 ns** |  **0.612 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 8    |     85.72 ns |   1.868 ns |  0.102 ns |  0.58 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    149.89 ns |   0.243 ns |  0.013 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    179.12 ns |  17.290 ns |  0.948 ns |  1.20 |    0.01 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **10**   |    **196.20 ns** |   **5.054 ns** |  **0.277 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 10   |     93.69 ns |   1.333 ns |  0.073 ns |  0.48 |    0.00 |         - |          NA |
| Dp_Cjk     | 10   |    198.02 ns |   3.495 ns |  0.192 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 10   |    138.30 ns |   2.517 ns |  0.138 ns |  0.70 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **12**   |    **269.71 ns** |  **10.151 ns** |  **0.556 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |    104.59 ns |  28.954 ns |  1.587 ns |  0.39 |    0.01 |         - |          NA |
| Dp_Cjk     | 12   |    268.74 ns |   1.760 ns |  0.096 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 12   |    150.53 ns |  11.847 ns |  0.649 ns |  0.56 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **16**   |    **412.21 ns** |   **5.332 ns** |  **0.292 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 16   |    119.33 ns |   0.604 ns |  0.033 ns |  0.29 |    0.00 |         - |          NA |
| Dp_Cjk     | 16   |    412.19 ns |   2.669 ns |  0.146 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 16   |    168.94 ns |   9.077 ns |  0.498 ns |  0.41 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **24**   |    **810.02 ns** |  **17.839 ns** |  **0.978 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 24   |    155.44 ns |  20.922 ns |  1.147 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    820.97 ns | 302.173 ns | 16.563 ns |  1.01 |    0.02 |         - |          NA |
| Kernel_Cjk | 24   |    216.07 ns |   5.992 ns |  0.328 ns |  0.27 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **32**   |  **1,419.52 ns** |   **8.162 ns** |  **0.447 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    192.19 ns |   1.759 ns |  0.096 ns |  0.14 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,425.37 ns |  35.521 ns |  1.947 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 32   |    257.55 ns |   6.287 ns |  0.345 ns |  0.18 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **48**   |  **3,118.79 ns** | **172.915 ns** |  **9.478 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 48   |    261.39 ns |   3.823 ns |  0.210 ns |  0.08 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,109.40 ns |  46.539 ns |  2.551 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 48   |    339.61 ns |   1.996 ns |  0.109 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **64**   |  **5,470.97 ns** | **145.119 ns** |  **7.954 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 64   |    332.09 ns |  10.999 ns |  0.603 ns |  0.06 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  5,460.83 ns |  46.868 ns |  2.569 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 64   |    435.38 ns |   3.491 ns |  0.191 ns |  0.08 |    0.00 |         - |          NA |
|            |      |              |            |           |       |         |           |             |
| **Dp**         | **96**   | **12,212.16 ns** | **276.297 ns** | **15.145 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 96   |  1,310.41 ns |  16.653 ns |  0.913 ns |  0.11 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 12,237.11 ns | 427.949 ns | 23.457 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 96   |  1,699.20 ns |  43.354 ns |  2.376 ns |  0.14 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method                 | Mean      | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated |
|----------------------- |----------:|----------:|----------:|---------:|---------:|---------:|----------:|
| VocabTxt               |  3.842 ms | 0.0966 ms | 0.0053 ms |  85.9375 |  78.1250 |  31.2500 |   3.62 MB |
| TokenizerJsonWordPiece | 11.472 ms | 2.9466 ms | 0.1615 ms | 125.0000 | 109.3750 |  31.2500 |   5.72 MB |
| TokenizerJsonUnigram   | 11.884 ms | 0.3498 ms | 0.0192 ms |  78.1250 |  62.5000 |  31.2500 |   4.64 MB |
| SpieceModel            |  4.073 ms | 0.9082 ms | 0.0498 ms |  78.1250 |  70.3125 |  23.4375 |   3.36 MB |
| TfidfSave              |  1.944 ms | 0.4206 ms | 0.0231 ms |  33.2031 |  29.2969 |  29.2969 |   2.09 MB |
| TfidfLoad              |  4.300 ms | 0.3515 ms | 0.0193 ms |  62.5000 |  54.6875 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  5.517 ms | 0.6180 ms | 0.0339 ms | 273.4375 | 273.4375 | 273.4375 |  19.87 MB |
| EmbeddingIndexLoad     | 10.096 ms | 5.1618 ms | 0.2829 ms | 484.3750 | 453.1250 | 453.1250 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method               | Documents | Mean      | Error     | StdDev    | Ratio | Gen0      | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|----------:|----------:|------:|----------:|---------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.700 ms** | **0.4437 ms** | **0.0243 ms** |  **1.00** |  **343.7500** | **156.2500** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.297 ms | 0.5794 ms | 0.0318 ms |  0.82 |  257.8125 | 125.0000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.852 ms | 1.3158 ms | 0.0721 ms |  1.02 |  359.3750 | 140.6250 |  62.5000 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.528 ms | 0.0510 ms | 0.0028 ms |  0.85 |  265.6250 |  93.7500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |          |          |           |             |
| **Count**                | **1000**      | **31.984 ms** | **0.9332 ms** | **0.0512 ms** |  **1.00** | **1687.5000** | **812.5000** | **562.5000** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 25.089 ms | 0.7729 ms | 0.0424 ms |  0.78 | 1406.2500 | 562.5000 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 30.456 ms | 2.7674 ms | 0.1517 ms |  0.95 | 1968.7500 | 593.7500 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 25.722 ms | 0.6029 ms | 0.0330 ms |  0.80 | 1343.7500 | 343.7500 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method | Dim  | Mean      | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|------- |----- |----------:|---------:|---------:|------:|----------:|------------:|
| **Dot**    | **384**  |  **54.97 ns** | **0.270 ns** | **0.015 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  52.03 ns | 0.859 ns | 0.047 ns |  0.95 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  | **103.43 ns** | **1.053 ns** | **0.058 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  | 107.37 ns | 1.088 ns | 0.060 ns |  1.04 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **139.75 ns** | **2.932 ns** | **0.161 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 136.69 ns | 8.288 ns | 0.454 ns |  0.98 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method       | Documents | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|------------- |---------- |----------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **Count**        | **200**       |  **3.022 ms** | **4.5751 ms** | **0.2508 ms** |  **1.00** |    **0.10** |  **62.5000** |  **23.4375** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.246 ms | 5.3065 ms | 0.2909 ms |  1.08 |    0.11 |  62.5000 |  23.4375 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.786 ms | 0.1958 ms | 0.0107 ms |  1.26 |    0.09 | 109.3750 |  62.5000 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.974 ms | 2.0463 ms | 0.1122 ms |  0.99 |    0.08 |  62.5000 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.167 ms** | **0.7291 ms** | **0.0400 ms** |  **1.00** |    **0.01** | **351.5625** | **210.9375** |  **70.3125** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.453 ms | 1.3872 ms | 0.0760 ms |  1.04 |    0.01 | 343.7500 | 218.7500 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 12.086 ms | 1.1205 ms | 0.0614 ms |  1.69 |    0.01 | 640.6250 | 265.6250 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.209 ms | 0.1759 ms | 0.0096 ms |  1.01 |    0.00 | 351.5625 | 140.6250 |  70.3125 |   7.85 MB |        1.00 |

<!-- markdownlint-enable MD060 -->

### compare-indel

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 102.4 | 21.8 | 4.69x C# faster |
| latin | 32 | 147.9 | 86.7 | 1.71x C# faster |
| latin | 128 | 467.2 | 801.7 | 1.72x Py faster |
| latin | 512 | 4731.8 | 7440.6 | 1.57x Py faster |
| cjk | 8 | 113.3 | 21.8 | 5.18x C# faster |
| cjk | 32 | 263.1 | 214.2 | 1.23x C# faster |
| cjk | 128 | 1792.0 | 1585.3 | 1.13x C# faster |
| cjk | 512 | 16300.6 | 10808.4 | 1.51x C# faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is Hyyro's bit-parallel LLCS above a pattern of 8 and a rolling-row dynamic program below it (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 131.6 | 19.2 | 6.86x C# faster |
| latin | 32 | 229.6 | 154.9 | 1.48x C# faster |
| latin | 128 | 1809.0 | 1543.2 | 1.17x C# faster |
| latin | 512 | 15214.5 | 16134.8 | 1.06x Py faster |
| cjk | 8 | 130.9 | 19.2 | 6.83x C# faster |
| cjk | 32 | 339.1 | 290.0 | 1.17x C# faster |
| cjk | 128 | 2978.7 | 2531.9 | 1.18x C# faster |
| cjk | 512 | 25920.2 | 19808.3 | 1.31x C# faster |

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.

<!-- markdownlint-enable MD060 -->

### compare-metrics

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| confusion_matrix_n1000_k2 | 0.007 | 0.811 | 111.52x | 0.007 | 0.811 | 111.51x |
| accuracy_n1000_k2 | 0.001 | 0.425 | 372.56x | 0.001 | 0.425 | 372.52x |
| precision_recall_f1_macro_n1000_k2 | 0.007 | 1.472 | 220.87x | 0.007 | 1.472 | 220.88x |
| classification_report_n1000_k2 | 0.010 | 5.619 | 582.96x | 0.010 | 5.619 | 582.93x |
| roc_auc_binary_n1000_k2 | 0.015 | 1.627 | 107.49x | 0.015 | 1.626 | 107.48x |
| balanced_accuracy_n1000_k2 | 0.007 | 0.871 | 133.02x | 0.007 | 0.871 | 133.02x |
| matthews_n1000_k2 | 0.007 | 1.646 | 250.37x | 0.007 | 1.646 | 250.36x |
| cohen_kappa_n1000_k2 | 0.007 | 0.905 | 137.57x | 0.007 | 0.905 | 137.59x |
| mse_n1000_k2 | 0.007 | 0.232 | 32.96x | 0.007 | 0.232 | 32.95x |
| mae_n1000_k2 | 0.007 | 0.231 | 32.76x | 0.007 | 0.231 | 32.76x |
| median_ae_n1000_k2 | 0.007 | 0.246 | 36.13x | 0.007 | 0.246 | 36.13x |
| r2_n1000_k2 | 0.003 | 0.281 | 82.21x | 0.003 | 0.281 | 82.20x |
| confusion_matrix_n1000_k10 | 0.007 | 0.804 | 109.46x | 0.007 | 0.804 | 109.47x |
| accuracy_n1000_k10 | 0.001 | 0.428 | 375.17x | 0.001 | 0.428 | 375.13x |
| precision_recall_f1_macro_n1000_k10 | 0.007 | 1.491 | 207.73x | 0.007 | 1.491 | 207.72x |
| classification_report_n1000_k10 | 0.015 | 5.804 | 396.09x | 0.015 | 5.804 | 396.09x |
| roc_auc_ovr_macro_n1000_k10 | 0.521 | 8.316 | 15.96x | 0.521 | 8.316 | 15.96x |
| balanced_accuracy_n1000_k10 | 0.007 | 0.870 | 124.71x | 0.007 | 0.870 | 124.71x |
| matthews_n1000_k10 | 0.007 | 1.674 | 240.07x | 0.007 | 1.674 | 240.05x |
| cohen_kappa_n1000_k10 | 0.007 | 0.914 | 123.99x | 0.007 | 0.914 | 123.99x |
| mse_n1000_k10 | 0.007 | 0.231 | 32.84x | 0.007 | 0.231 | 32.84x |
| mae_n1000_k10 | 0.007 | 0.231 | 32.71x | 0.007 | 0.230 | 32.71x |
| median_ae_n1000_k10 | 0.007 | 0.243 | 35.99x | 0.007 | 0.243 | 35.99x |
| r2_n1000_k10 | 0.003 | 0.280 | 82.09x | 0.003 | 0.280 | 82.10x |
| confusion_matrix_n100000_k2 | 0.829 | 10.913 | 13.16x | 0.829 | 10.912 | 13.16x |
| accuracy_n100000_k2 | 0.180 | 3.764 | 20.88x | 0.180 | 3.763 | 20.88x |
| precision_recall_f1_macro_n100000_k2 | 0.744 | 12.380 | 16.65x | 0.744 | 12.380 | 16.65x |
| classification_report_n100000_k2 | 0.753 | 26.734 | 35.49x | 0.753 | 26.734 | 35.49x |
| roc_auc_binary_n100000_k2 | 4.079 | 25.349 | 6.22x | 4.079 | 25.349 | 6.21x |
| balanced_accuracy_n100000_k2 | 0.746 | 10.964 | 14.69x | 0.746 | 10.964 | 14.69x |
| matthews_n100000_k2 | 0.753 | 21.924 | 29.13x | 0.753 | 21.923 | 29.13x |
| cohen_kappa_n100000_k2 | 0.745 | 10.989 | 14.74x | 0.745 | 10.989 | 14.74x |
| mse_n100000_k2 | 0.281 | 0.514 | 1.83x | 0.281 | 0.514 | 1.83x |
| mae_n100000_k2 | 0.268 | 0.505 | 1.88x | 0.268 | 0.505 | 1.88x |
| median_ae_n100000_k2 | 0.761 | 1.669 | 2.19x | 0.777 | 1.669 | 2.15x |
| r2_n100000_k2 | 0.336 | 0.854 | 2.54x | 0.336 | 0.854 | 2.54x |
| confusion_matrix_n100000_k10 | 0.903 | 10.863 | 12.03x | 0.903 | 10.863 | 12.03x |
| accuracy_n100000_k10 | 0.276 | 3.764 | 13.62x | 0.276 | 3.764 | 13.62x |
| precision_recall_f1_macro_n100000_k10 | 0.862 | 13.020 | 15.11x | 0.862 | 13.019 | 15.11x |
| classification_report_n100000_k10 | 0.867 | 29.265 | 33.75x | 0.867 | 29.263 | 33.76x |
| roc_auc_ovr_macro_n100000_k10 | 45.793 | 205.712 | 4.49x | 45.787 | 205.694 | 4.49x |
| balanced_accuracy_n100000_k10 | 0.855 | 10.969 | 12.82x | 0.855 | 10.969 | 12.82x |
| matthews_n100000_k10 | 0.853 | 22.536 | 26.41x | 0.853 | 22.536 | 26.41x |
| cohen_kappa_n100000_k10 | 0.984 | 11.003 | 11.18x | 0.984 | 11.003 | 11.19x |
| mse_n100000_k10 | 0.281 | 0.514 | 1.83x | 0.281 | 0.514 | 1.83x |
| mae_n100000_k10 | 0.268 | 0.506 | 1.89x | 0.268 | 0.506 | 1.89x |
| median_ae_n100000_k10 | 0.821 | 1.669 | 2.03x | 0.887 | 1.669 | 1.88x |
| r2_n100000_k10 | 0.336 | 0.852 | 2.53x | 0.336 | 0.851 | 2.53x |
| confusion_matrix_n1000000_k2 | 7.833 | 104.503 | 13.34x | 7.833 | 104.488 | 13.34x |
| accuracy_n1000000_k2 | 1.930 | 34.402 | 17.82x | 1.930 | 34.402 | 17.82x |
| precision_recall_f1_macro_n1000000_k2 | 7.536 | 112.327 | 14.91x | 7.536 | 112.315 | 14.90x |
| classification_report_n1000000_k2 | 7.453 | 223.095 | 29.93x | 7.452 | 223.065 | 29.93x |
| roc_auc_binary_n1000000_k2 | 63.750 | 288.226 | 4.52x | 63.749 | 288.178 | 4.52x |
| balanced_accuracy_n1000000_k2 | 7.446 | 104.649 | 14.05x | 7.446 | 104.640 | 14.05x |
| matthews_n1000000_k2 | 7.444 | 210.737 | 28.31x | 7.444 | 210.727 | 28.31x |
| cohen_kappa_n1000000_k2 | 7.458 | 104.812 | 14.05x | 7.458 | 104.810 | 14.05x |
| mse_n1000000_k2 | 2.626 | 3.401 | 1.30x | 2.625 | 3.401 | 1.30x |
| mae_n1000000_k2 | 2.667 | 3.095 | 1.16x | 2.667 | 3.095 | 1.16x |
| median_ae_n1000000_k2 | 7.506 | 14.364 | 1.91x | 7.543 | 14.362 | 1.90x |
| r2_n1000000_k2 | 3.294 | 6.400 | 1.94x | 3.293 | 6.399 | 1.94x |
| confusion_matrix_n1000000_k10 | 9.067 | 104.381 | 11.51x | 9.066 | 104.343 | 11.51x |
| accuracy_n1000000_k10 | 2.990 | 34.358 | 11.49x | 2.990 | 34.353 | 11.49x |
| precision_recall_f1_macro_n1000000_k10 | 8.682 | 118.604 | 13.66x | 8.682 | 118.596 | 13.66x |
| classification_report_n1000000_k10 | 8.600 | 250.319 | 29.11x | 8.599 | 250.317 | 29.11x |
| balanced_accuracy_n1000000_k10 | 8.567 | 104.559 | 12.20x | 8.567 | 104.556 | 12.20x |
| matthews_n1000000_k10 | 8.554 | 216.820 | 25.35x | 8.554 | 216.792 | 25.34x |
| cohen_kappa_n1000000_k10 | 8.554 | 104.742 | 12.25x | 8.554 | 104.737 | 12.24x |
| mse_n1000000_k10 | 2.832 | 3.515 | 1.24x | 2.832 | 3.515 | 1.24x |
| mae_n1000000_k10 | 2.813 | 3.047 | 1.08x | 2.813 | 3.047 | 1.08x |
| median_ae_n1000000_k10 | 7.828 | 14.728 | 1.88x | 7.945 | 14.727 | 1.85x |
| r2_n1000000_k10 | 3.859 | 6.416 | 1.66x | 3.858 | 6.416 | 1.66x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

<!-- markdownlint-enable MD060 -->

### compare-persistence

_As of 2026-08-29, measured at commit `5d7a580b6fa8b8891f7c3ddfba22bf3fe791d7a2`._

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 3.976 | 9.892 | 2.49x | 4.283 | 9.891 | 2.31x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 11.403 | 15.477 | 1.36x | 11.741 | 15.477 | 1.32x | 706,526 | 706,526 |
| tokenizer_json_unigram | 11.678 | 41.488 | 3.55x | 12.060 | 41.483 | 3.44x | 1,990,038 | 1,990,038 |
| spiece_model | 4.646 | 28.771 | 6.19x | 4.945 | 28.770 | 5.82x | 533,084 | 533,084 |
| tfidf_save | 1.860 | 2.496 | 1.34x | 1.882 | 2.496 | 1.33x | 581,787 | 591,922 |
| tfidf_load | 4.359 | 3.890 | 0.89x | 4.607 | 3.890 | 0.84x | 581,787 | 591,922 |
| embedding_index_save | 5.254 | 2.527 | 0.48x | 5.452 | 2.527 | 0.46x | 20,589,007 | 15,360,128 |
| embedding_index_save_file | 50.362 | 36.496 | 0.72x | 10.704 | 5.280 | 0.49x | 20,589,007 | 15,360,128 |
| embedding_index_load | 9.543 | 1.631 | 0.17x | 10.291 | 1.631 | 0.16x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 10.366 | 1.148 | 0.11x | 11.168 | 1.142 | 0.10x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 5.599 | 1.662 | 0.30x | 5.991 | 1.662 | 0.28x | 20,589,007 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 115.06x | 0.000 | 0.001 | 115.07x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 424.529 | 562.664 | 1.33x | 425.739 | 562.652 | 1.32x | 15,250,490 | 14,022,374 |
| embedding_index_load_gzip | 83.508 | 71.661 | 0.86x | 84.952 | 71.661 | 0.84x | 15,250,490 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
