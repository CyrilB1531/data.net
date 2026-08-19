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

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | CorpusSize | Mean         | Error      | StdDev    | Ratio | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|-----------:|----------:|------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **6.270 μs** |  **0.2552 μs** | **0.0140 μs** |  **1.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.441 μs |  0.0571 μs | 0.0031 μs |  1.03 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.437 μs |  0.7059 μs | 0.0387 μs |  1.03 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **8**          |   **113.714 μs** | **21.9279 μs** | **1.2019 μs** |  **1.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    75.783 μs |  4.4600 μs | 0.2445 μs |  0.67 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    77.949 μs |  7.1818 μs | 0.3937 μs |  0.69 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **32**         |   **414.111 μs** | **55.4243 μs** | **3.0380 μs** |  **1.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   264.595 μs | 17.7809 μs | 0.9746 μs |  0.64 |  26.8555 |  1.4648 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   254.103 μs | 11.2195 μs | 0.6150 μs |  0.61 |  25.8789 |  1.4648 |   427.8 KB |        0.91 |
|                    |            |              |            |           |       |          |         |            |             |
| **UnitLoop**           | **128**        | **1,663.463 μs** | **37.3132 μs** | **2.0453 μs** |  **1.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        | 1,068.483 μs | 11.7142 μs | 0.6421 μs |  0.64 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        | 1,021.019 μs | 43.2040 μs | 2.3682 μs |  0.61 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |
````

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method  | Mean     | Error    | StdDev  | Ratio | Gen0       | Allocated | Alloc Ratio |
|-------- |---------:|---------:|--------:|------:|-----------:|----------:|------------:|
| Unigram | 622.0 ms | 71.41 ms | 3.91 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 576.4 ms | 40.57 ms | 2.22 ms |  0.93 |  7000.0000 | 112.18 MB |        0.22 |
````

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Length | Mean       | Error     | StdDev   | Gen0   | Allocated |
|-------------------------- |------- |-----------:|----------:|---------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    |   **102.9 μs** |   **5.37 μs** |  **0.29 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **218.3 μs** |   **6.71 μs** |  **0.37 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **482.1 μs** |  **77.54 μs** |  **4.25 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,007.2 μs** | **189.16 μs** | **10.37 μs** | **7.8125** | **157.03 KB** |
````

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean        | Error        | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |------------:|-------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |    134.3 ns |      3.71 ns |     0.20 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 50,204.3 ns | 22,933.09 ns | 1,257.04 ns | 373.89 |    8.12 |      - |         - |          NA |
| TokenSortRatio |  1,116.3 ns |    340.20 ns |    18.65 ns |   8.31 |    0.12 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,613.5 ns |     52.08 ns |     2.85 ns |  26.91 |    0.04 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,867.0 ns |    172.35 ns |     9.45 ns |  36.25 |    0.08 | 0.4272 |    7200 B |          NA |
````

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Length | Mean          | Error          | StdDev        | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|---------------:|--------------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **26.62 ns** |       **1.637 ns** |      **0.090 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     132.25 ns |      43.939 ns |      2.408 ns |  4.97 |    0.08 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      28.05 ns |       1.020 ns |      0.056 ns |  1.05 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      27.20 ns |       0.627 ns |      0.034 ns |  1.02 |    0.00 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.60 ns** |       **5.719 ns** |      **0.313 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     147.83 ns |      24.103 ns |      1.321 ns |  5.17 |    0.06 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.67 ns |       0.475 ns |      0.026 ns |  1.00 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.09 ns |       0.509 ns |      0.028 ns |  0.98 |    0.01 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.66 ns** |       **1.752 ns** |      **0.096 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     165.49 ns |      13.245 ns |      0.726 ns |  5.40 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.92 ns |       3.025 ns |      0.166 ns |  1.04 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      31.18 ns |       0.504 ns |      0.028 ns |  1.02 |    0.00 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **86.36 ns** |       **5.456 ns** |      **0.299 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     189.57 ns |      26.810 ns |      1.470 ns |  2.20 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      86.42 ns |       0.243 ns |      0.013 ns |  1.00 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      92.40 ns |      41.705 ns |      2.286 ns |  1.07 |    0.02 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **94.96 ns** |       **0.810 ns** |      **0.044 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     673.96 ns |     108.982 ns |      5.974 ns |  7.10 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |     102.21 ns |       1.867 ns |      0.102 ns |  1.08 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      94.92 ns |       3.788 ns |      0.208 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **32**     |     **111.34 ns** |       **7.195 ns** |      **0.394 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,024.48 ns |     127.513 ns |      6.989 ns |  9.20 |    0.06 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |     108.86 ns |      21.329 ns |      1.169 ns |  0.98 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |     110.61 ns |       7.400 ns |      0.406 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,334.58 ns** |     **171.316 ns** |      **9.390 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  21,502.33 ns |     781.975 ns |     42.863 ns | 16.11 |    0.10 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,416.96 ns |     241.438 ns |     13.234 ns |  1.06 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,382.27 ns |      11.923 ns |      0.654 ns |  1.04 |    0.01 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **512**    |  **11,730.40 ns** |   **1,732.832 ns** |     **94.982 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 335,284.86 ns | 362,204.508 ns | 19,853.651 ns | 28.58 |    1.48 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |  11,698.03 ns |     272.064 ns |     14.913 ns |  1.00 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |  11,423.22 ns |      48.940 ns |      2.683 ns |  0.97 |    0.01 |         - |          NA |
````

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Band | Mean        | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| **Dp**     | **8**    |    **131.2 ns** |      **8.07 ns** |     **0.44 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |    129.2 ns |     30.15 ns |     1.65 ns |  0.99 |    0.01 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **12**   |    **233.1 ns** |     **37.84 ns** |     **2.07 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 12   |    243.3 ns |     38.45 ns |     2.11 ns |  1.04 |    0.01 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **14**   |    **283.1 ns** |    **303.45 ns** |    **16.63 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| Kernel | 14   |    282.8 ns |    322.42 ns |    17.67 ns |  1.00 |    0.07 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **16**   |    **351.4 ns** |      **0.97 ns** |     **0.05 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 16   |    116.7 ns |      0.79 ns |     0.04 ns |  0.33 |    0.00 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **18**   |    **434.5 ns** |     **14.01 ns** |     **0.77 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 18   |    118.8 ns |      4.09 ns |     0.22 ns |  0.27 |    0.00 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **20**   |    **766.8 ns** |     **63.63 ns** |     **3.49 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 20   |    121.6 ns |     12.53 ns |     0.69 ns |  0.16 |    0.00 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **24**   |    **987.9 ns** |    **248.86 ns** |    **13.64 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 24   |    128.1 ns |      2.11 ns |     0.12 ns |  0.13 |    0.00 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **32**   |  **1,733.2 ns** |     **55.81 ns** |     **3.06 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 32   |    140.0 ns |     28.77 ns |     1.58 ns |  0.08 |    0.00 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **48**   |  **3,627.3 ns** |    **528.44 ns** |    **28.97 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel | 48   |    169.2 ns |      2.88 ns |     0.16 ns |  0.05 |    0.00 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **64**   |  **5,538.1 ns** |  **1,423.45 ns** |    **78.02 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 64   |    199.7 ns |     27.67 ns |     1.52 ns |  0.04 |    0.00 |         - |          NA |
|        |      |             |              |             |       |         |           |             |
| **Dp**     | **96**   | **12,357.9 ns** | **23,741.22 ns** | **1,301.34 ns** |  **1.01** |    **0.13** |         **-** |          **NA** |
| Kernel | 96   |  1,159.9 ns |     16.76 ns |     0.92 ns |  0.09 |    0.01 |         - |          NA |
````

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Length | Mean         | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **26.59 ns** |   **6.204 ns** |  **0.340 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    119.61 ns |   3.009 ns |  0.165 ns |  4.50 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.18 ns |   0.167 ns |  0.009 ns |  0.95 |    0.01 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **301.76 ns** |   **7.411 ns** |  **0.406 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    695.26 ns |  16.681 ns |  0.914 ns |  2.30 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    303.48 ns |   5.013 ns |  0.275 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **512**    | **14,186.74 ns** | **204.801 ns** | **11.226 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 16,631.67 ns | 399.872 ns | 21.918 ns |  1.17 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 13,759.21 ns | 508.074 ns | 27.849 ns |  0.97 |    0.00 |         - |          NA |
````

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Length | Distinct | Mean           | Error         | StdDev      | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |---------------:|--------------:|------------:|-------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |       **382.0 ns** |      **56.35 ns** |     **3.09 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,539.7 ns |      19.66 ns |     1.08 ns |   4.03 |    0.03 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **378.6 ns** |      **17.58 ns** |     **0.96 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,533.2 ns |      11.04 ns |     0.61 ns |   4.05 |    0.01 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **473.3 ns** |      **15.91 ns** |     **0.87 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,540.8 ns |     162.34 ns |     8.90 ns |   7.48 |    0.02 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **448.6 ns** |       **3.02 ns** |     **0.17 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,573.6 ns |     245.80 ns |    13.47 ns |   7.97 |    0.03 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **550.1 ns** |     **138.66 ns** |     **7.60 ns** |   **1.00** |    **0.02** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     6,653.9 ns |     362.03 ns |    19.84 ns |  12.10 |    0.15 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **545.3 ns** |       **1.23 ns** |     **0.07 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,520.6 ns |   1,545.59 ns |    84.72 ns |  11.96 |    0.13 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **650.4 ns** |      **50.75 ns** |     **2.78 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |    10,176.1 ns |     174.35 ns |     9.56 ns |  15.65 |    0.06 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **630.6 ns** |       **6.85 ns** |     **0.38 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |    10,485.8 ns |     793.98 ns |    43.52 ns |  16.63 |    0.06 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,299.6 ns** |      **28.94 ns** |     **1.59 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   138,502.0 ns |   6,626.40 ns |   363.22 ns |  60.23 |    0.14 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,337.2 ns** |      **91.38 ns** |     **5.01 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   106,885.8 ns |   2,871.88 ns |   157.42 ns |  45.73 |    0.10 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **18,111.5 ns** |     **508.35 ns** |    **27.86 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,361,247.4 ns |  26,608.08 ns | 1,458.48 ns | 130.37 |    0.19 |         - |          NA |
|                    |        |          |                |               |             |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **437,948.5 ns** | **176,672.57 ns** | **9,684.02 ns** |   **1.00** |    **0.03** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,772,317.5 ns |  36,302.96 ns | 1,989.89 ns |   4.05 |    0.08 |         - |          NA |
````

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Samples | Classes | Mean           | Error         | StdDev       | Gen0   | Allocated |
|--------------- |-------- |-------- |---------------:|--------------:|-------------:|-------:|----------:|
| **Matrix**         | **1000**    | **2**       |     **7,805.7 ns** |   **1,249.06 ns** |     **68.47 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     7,396.9 ns |     470.92 ns |     25.81 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |       919.7 ns |      39.56 ns |      2.17 ns |      - |         - |
| F1Macro        | 1000    | 2       |     7,342.8 ns |     214.26 ns |     11.74 ns | 0.0229 |     472 B |
| Report         | 1000    | 2       |    10,652.8 ns |     518.44 ns |     28.42 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **7,852.3 ns** |     **313.32 ns** |     **17.17 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     7,495.8 ns |     269.19 ns |     14.75 ns | 0.0610 |    1248 B |
| AccuracyScore  | 1000    | 10      |       919.1 ns |      17.30 ns |      0.95 ns |      - |         - |
| F1Macro        | 1000    | 10      |     8,192.4 ns |     162.98 ns |      8.93 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |    15,758.6 ns |   3,482.38 ns |    190.88 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **868,489.3 ns** |  **29,322.04 ns** |  **1,607.24 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   818,023.5 ns | 112,879.33 ns |  6,187.30 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   165,595.8 ns |   3,616.50 ns |    198.23 ns |      - |         - |
| F1Macro        | 100000  | 2       |   861,524.4 ns |  58,936.43 ns |  3,230.50 ns |      - |     473 B |
| Report         | 100000  | 2       |   888,949.8 ns |  37,300.26 ns |  2,044.55 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **984,521.5 ns** |  **11,980.49 ns** |    **656.69 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   941,404.0 ns |  50,407.18 ns |  2,762.99 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   273,491.9 ns |   8,919.92 ns |    488.93 ns |      - |         - |
| F1Macro        | 100000  | 10      |   995,298.1 ns |  29,293.76 ns |  1,605.69 ns |      - |    1665 B |
| Report         | 100000  | 10      |   987,389.8 ns |  56,400.87 ns |  3,091.52 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **8,705,368.3 ns** |  **86,274.70 ns** |  **4,729.01 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       | 8,365,643.3 ns | 511,656.81 ns | 28,045.64 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       | 1,747,426.4 ns |  12,243.19 ns |    671.09 ns |      - |         - |
| F1Macro        | 1000000 | 2       | 8,450,137.5 ns |  87,191.48 ns |  4,779.26 ns |      - |     484 B |
| Report         | 1000000 | 2       | 8,409,762.5 ns | 208,734.13 ns | 11,441.42 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **9,922,978.4 ns** | **640,930.40 ns** | **35,131.56 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 9,392,700.5 ns | 136,649.86 ns |  7,490.24 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      | 2,692,796.0 ns |  39,737.90 ns |  2,178.17 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 9,758,491.8 ns | 137,980.89 ns |  7,563.20 ns |      - |    1676 B |
| Report         | 1000000 | 10      | 9,858,382.3 ns | 814,218.18 ns | 44,630.05 ns |      - |   15892 B |
````

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | Mean      | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated |
|----------------------- |----------:|----------:|----------:|---------:|---------:|---------:|----------:|
| VocabTxt               |  4.203 ms | 3.0722 ms | 0.1684 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.833 ms | 2.8534 ms | 0.1564 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.980 ms | 3.2711 ms | 0.1793 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.816 ms | 4.0154 ms | 0.2201 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.946 ms | 0.5975 ms | 0.0328 ms |  33.2031 |  27.3438 |  27.3438 |   2.09 MB |
| TfidfLoad              |  4.511 ms | 1.1961 ms | 0.0656 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  9.103 ms | 2.8954 ms | 0.1587 ms | 515.6250 | 515.6250 | 515.6250 |  54.29 MB |
| EmbeddingIndexLoad     |  7.715 ms | 2.5990 ms | 0.1425 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |
````

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Documents | Mean      | Error     | StdDev    | Ratio | Gen0      | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|----------:|----------:|------:|----------:|---------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.887 ms** | **0.3956 ms** | **0.0217 ms** |  **1.00** |  **500.0000** | **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.529 ms | 0.3753 ms | 0.0206 ms |  0.83 |  390.6250 | 187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.862 ms | 1.3304 ms | 0.0729 ms |  1.00 |  500.0000 | 156.2500 |  62.5000 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.956 ms | 1.4833 ms | 0.0813 ms |  0.88 |  406.2500 | 156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |           |          |          |           |             |
| **Count**                | **1000**      | **31.565 ms** | **3.8077 ms** | **0.2087 ms** |  **1.00** | **2625.0000** | **875.0000** | **500.0000** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 24.619 ms | 2.2204 ms | 0.1217 ms |  0.78 | 1968.7500 | 781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 29.538 ms | 3.2534 ms | 0.1783 ms |  0.94 | 2562.5000 | 750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 25.440 ms | 0.1927 ms | 0.0106 ms |  0.81 | 2031.2500 | 625.0000 | 250.0000 |  31.83 MB |        0.82 |
````

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Dim  | Mean      | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|------- |----- |----------:|---------:|---------:|------:|----------:|------------:|
| **Dot**    | **384**  |  **51.16 ns** | **3.241 ns** | **0.178 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  48.56 ns | 0.356 ns | 0.020 ns |  0.95 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  |  **94.08 ns** | **5.784 ns** | **0.317 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  92.77 ns | 5.276 ns | 0.289 ns |  0.99 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **124.27 ns** | **5.363 ns** | **0.294 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 122.67 ns | 1.022 ns | 0.056 ns |  0.99 |         - |          NA |
````

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Documents | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|------------- |---------- |----------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| **Count**        | **200**       |  **2.992 ms** | **1.2752 ms** | **0.0699 ms** |  **1.00** |    **0.03** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  3.009 ms | 0.6978 ms | 0.0382 ms |  1.01 |    0.02 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.795 ms | 1.0096 ms | 0.0553 ms |  1.27 |    0.03 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.915 ms | 0.3934 ms | 0.0216 ms |  0.97 |    0.02 |  97.6563 |  31.2500 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.594 ms** | **0.8008 ms** | **0.0439 ms** |  **1.00** |    **0.01** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.695 ms | 0.6657 ms | 0.0365 ms |  1.01 |    0.01 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 12.350 ms | 2.4828 ms | 0.1361 ms |  1.63 |    0.02 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.351 ms | 1.3680 ms | 0.0750 ms |  0.97 |    0.01 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |
````

## Against rapidfuzz, in this same run

Both sides on this VM in these minutes, which is what makes the ratio readable where the absolutes are not.

- `indel`
- `levenshtein`
- `metrics`
- `persistence`

### compare-indel

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
Python: rapidfuzz 3.14.5 (py 3.12.13)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)

  length |   Python ns/pair |     C# ns/pair |  speedup (py/C#)
---------+------------------+----------------+-----------------
       8 |            117.4 |           28.3 |   4.15x C# faster
      32 |            178.0 |          255.9 |   1.44x Py faster
     128 |            483.6 |          954.9 |   1.97x Py faster
     512 |           4479.5 |        11024.7 |   2.46x Py faster

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is a rolling-row dynamic program (#273).
````

### compare-levenshtein

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
Python: rapidfuzz 3.14.5 (py 3.12.13)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)

  length |   Python ns/pair |     C# ns/pair |  speedup (py/C#)
---------+------------------+----------------+-----------------
       8 |            155.4 |           18.4 |   8.46x C# faster
      32 |            284.7 |          363.8 |   1.28x Py faster
     128 |           1688.0 |         1342.8 |   1.26x C# faster
     512 |          14065.8 |        14685.3 |   1.04x Py faster

Note: Python times the realistic per-call loop; rapidfuzz's C core uses the bit-parallel Myers algorithm, so it scales better on long strings.
````

### compare-metrics

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
Python: {'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.13)
C#:     Lodestar on .NET 10.0.11

operation                             C# ms      Py ms    wall |     C# cpu     Py cpu     cpu
confusion_matrix_n1000_k2             0.009      0.980 103.67x |      0.009      0.980 103.68x
accuracy_n1000_k2                     0.001      0.524 507.03x |      0.001      0.524 507.00x
precision_recall_f1_macro_n1000_k2      0.008      1.756 221.30x |      0.008      1.755 221.30x
classification_report_n1000_k2        0.011      6.716 618.34x |      0.011      6.715 618.30x
roc_auc_binary_n1000_k2               0.016      1.923 121.27x |      0.016      1.923 121.27x
balanced_accuracy_n1000_k2            0.008      1.049 134.97x |      0.008      1.049 134.99x
matthews_n1000_k2                     0.008      1.965 251.65x |      0.008      1.965 251.60x
cohen_kappa_n1000_k2                  0.008      1.091 139.86x |      0.008      1.091 139.87x
mse_n1000_k2                          0.005      0.304  60.11x |      0.005      0.304  60.10x
mae_n1000_k2                          0.005      0.309  61.43x |      0.005      0.309  61.43x
median_ae_n1000_k2                    0.007      0.318  47.33x |      0.007      0.318  47.33x
r2_n1000_k2                           0.002      0.371 149.90x |      0.002      0.371 149.88x
confusion_matrix_n1000_k10            0.010      0.983 102.70x |      0.010      0.983 102.71x
accuracy_n1000_k10                    0.001      0.531 471.95x |      0.001      0.531 471.99x
precision_recall_f1_macro_n1000_k10      0.009      1.788 210.29x |      0.009      1.787 210.28x
classification_report_n1000_k10       0.016      6.992 443.25x |      0.016      6.991 443.26x
roc_auc_ovr_macro_n1000_k10           0.543     10.049  18.49x |      0.543     10.047  18.49x
balanced_accuracy_n1000_k10           0.008      1.062 127.81x |      0.008      1.062 127.81x
matthews_n1000_k10                    0.008      2.033 243.76x |      0.008      2.032 243.71x
cohen_kappa_n1000_k10                 0.009      1.112 128.25x |      0.009      1.111 128.25x
mse_n1000_k10                         0.005      0.309  61.10x |      0.005      0.309  61.11x
mae_n1000_k10                         0.005      0.305  60.56x |      0.005      0.305  60.56x
median_ae_n1000_k10                   0.007      0.319  47.58x |      0.007      0.319  47.58x
r2_n1000_k10                          0.002      0.369 149.56x |      0.002      0.369 149.59x
confusion_matrix_n100000_k2           1.011     11.222  11.10x |      1.011     11.220  11.10x
accuracy_n100000_k2                   0.185      3.910  21.08x |      0.185      3.909  21.08x
precision_recall_f1_macro_n100000_k2      0.882     12.754  14.45x |      0.882     12.753  14.45x
classification_report_n100000_k2      0.877     27.712  31.60x |      0.877     27.709  31.60x
roc_auc_binary_n100000_k2             3.734     27.845   7.46x |      3.734     27.840   7.46x
balanced_accuracy_n100000_k2          0.872     11.333  12.99x |      0.872     11.331  12.99x
matthews_n100000_k2                   0.881     22.307  25.33x |      0.881     22.303  25.33x
cohen_kappa_n100000_k2                0.874     11.303  12.93x |      0.874     11.301  12.93x
mse_n100000_k2                        0.507      0.437   0.86x |      0.507      0.437   0.86x
mae_n100000_k2                        0.505      0.429   0.85x |      0.505      0.429   0.85x
median_ae_n100000_k2                  0.772      1.813   2.35x |      0.791      1.813   2.29x
r2_n100000_k2                         0.235      0.665   2.83x |      0.235      0.665   2.83x
confusion_matrix_n100000_k10          0.968     11.054  11.42x |      0.968     11.050  11.42x
accuracy_n100000_k10                  0.273      3.850  14.10x |      0.273      3.849  14.10x
precision_recall_f1_macro_n100000_k10      0.995     13.385  13.46x |      0.995     13.383  13.46x
classification_report_n100000_k10      0.982     30.410  30.98x |      0.981     30.408  30.99x
roc_auc_ovr_macro_n100000_k10        39.361    227.116   5.77x |     39.357    227.092   5.77x
balanced_accuracy_n100000_k10         0.977     11.215  11.48x |      0.977     11.214  11.48x
matthews_n100000_k10                  0.982     23.113  23.55x |      0.982     23.110  23.54x
cohen_kappa_n100000_k10               0.991     11.417  11.52x |      0.991     11.415  11.52x
mse_n100000_k10                       0.507      0.441   0.87x |      0.507      0.441   0.87x
mae_n100000_k10                       0.505      0.433   0.86x |      0.505      0.433   0.86x
median_ae_n100000_k10                 0.781      1.848   2.37x |      0.828      1.848   2.23x
r2_n100000_k10                        0.235      0.711   3.02x |      0.235      0.711   3.02x
confusion_matrix_n1000000_k2          8.683    101.925  11.74x |      8.682    101.911  11.74x
accuracy_n1000000_k2                  1.944     34.006  17.49x |      1.944     33.997  17.49x
precision_recall_f1_macro_n1000000_k2      8.884    110.298  12.42x |      8.885    110.275  12.41x
classification_report_n1000000_k2      8.771    213.620  24.35x |      8.771    213.587  24.35x
roc_auc_binary_n1000000_k2           53.923    302.484   5.61x |     53.918    302.357   5.61x
balanced_accuracy_n1000000_k2         8.777    102.381  11.66x |      8.777    102.358  11.66x
matthews_n1000000_k2                  8.843    204.972  23.18x |      8.843    204.944  23.18x
cohen_kappa_n1000000_k2               8.776    101.726  11.59x |      8.774    101.710  11.59x
mse_n1000000_k2                       5.098      2.913   0.57x |      5.098      2.913   0.57x
mae_n1000000_k2                       5.082      2.852   0.56x |      5.081      2.851   0.56x
median_ae_n1000000_k2                 7.112     15.059   2.12x |      7.198     15.056   2.09x
r2_n1000000_k2                        2.374      5.149   2.17x |      2.374      5.148   2.17x
confusion_matrix_n1000000_k10         9.712    102.552  10.56x |      9.712    102.510  10.56x
accuracy_n1000000_k10                 2.799     34.083  12.18x |      2.799     34.080  12.18x
precision_recall_f1_macro_n1000000_k10      9.951    116.610  11.72x |      9.951    116.585  11.72x
classification_report_n1000000_k10      9.832    239.634  24.37x |      9.831    239.610  24.37x
balanced_accuracy_n1000000_k10        9.787    102.370  10.46x |      9.787    102.357  10.46x
matthews_n1000000_k10                 9.870    212.526  21.53x |      9.869    212.462  21.53x
cohen_kappa_n1000000_k10              9.770    102.841  10.53x |      9.769    102.815  10.52x
mse_n1000000_k10                      5.099      2.844   0.56x |      5.099      2.844   0.56x
mae_n1000000_k10                      5.076      2.684   0.53x |      5.075      2.684   0.53x
median_ae_n1000000_k10                6.937     14.910   2.15x |      6.982     14.907   2.14x
r2_n1000000_k10                       2.797      4.745   1.70x |      2.796      4.744   1.70x

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mse_n100000_k2                   0.86x
  mae_n100000_k2                   0.85x
  mse_n100000_k10                  0.87x
  mae_n100000_k10                  0.86x
  mse_n1000000_k2                  0.57x
  mae_n1000000_k2                  0.56x
  mse_n1000000_k10                 0.56x
  mae_n1000000_k10                 0.53x
````

### compare-persistence

_As of 2026-08-19, measured at commit `2575ca720e04f12ed02151cbec70e0afe07550f6`._

````text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.13)
C#:     Lodestar on .NET 10.0.11

operation                       C# ms    Py ms    wall |   C# cpu   Py cpu     cpu
vocab_txt                       4.255   10.676   2.51x |    4.506   10.675   2.37x
tokenizer_json_wordpiece       12.646   17.763   1.40x |   13.132   17.761   1.35x
tokenizer_json_unigram         12.705   45.373   3.57x |   13.266   45.131   3.40x
spiece_model                    5.003   30.138   6.02x |    5.245   30.135   5.75x
tfidf_save                      1.824    2.513   1.38x |    1.913    2.513   1.31x
tfidf_load                      3.977    4.219   1.06x |    4.231    4.219   1.00x
embedding_index_save            8.779    5.506   0.63x |    9.417    5.506   0.58x
embedding_index_load            7.654    1.665   0.22x |    8.582    1.665   0.19x

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
````
