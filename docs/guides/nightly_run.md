# Nightly benchmark run

<!-- nightly-baseline: 2483f1a00691271083f00baf3835de96bf0a4076 -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `2483f1a00691271083f00baf3835de96bf0a4076`
- Previous run: `2483f1a00691271083f00baf3835de96bf0a4076`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
- `BlockedTableBenchmarks`
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
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | CorpusSize | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|----------:|------:|--------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **6.190 μs** |   **2.0988 μs** | **0.1150 μs** |  **1.00** |    **0.02** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.349 μs |   0.5078 μs | 0.0278 μs |  1.03 |    0.02 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.229 μs |   0.6632 μs | 0.0364 μs |  1.01 |    0.02 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **8**          |   **110.988 μs** |   **4.8151 μs** | **0.2639 μs** |  **1.00** |    **0.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    70.553 μs |  10.3044 μs | 0.5648 μs |  0.64 |    0.00 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    70.583 μs |  15.7002 μs | 0.8606 μs |  0.64 |    0.01 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **32**         |   **402.575 μs** |  **81.8012 μs** | **4.4838 μs** |  **1.00** |    **0.01** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   251.828 μs |  17.3156 μs | 0.9491 μs |  0.63 |    0.01 |  26.8555 |  1.4648 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   244.866 μs |   6.7106 μs | 0.3678 μs |  0.61 |    0.01 |  26.1230 |  1.7090 |   427.8 KB |        0.91 |
|                    |            |              |             |           |       |         |          |         |            |             |
| **UnitLoop**           | **128**        | **1,619.658 μs** | **114.0138 μs** | **6.2495 μs** |  **1.00** |    **0.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        | 1,025.353 μs | 141.2212 μs | 7.7408 μs |  0.63 |    0.00 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   975.284 μs |  62.1032 μs | 3.4041 μs |  0.60 |    0.00 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BlockedTableBenchmarks-report-github

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
| **Latin**  | **1000**   |      **51.93 μs** |     **0.631 μs** |   **0.035 μs** |         **-** |
| Cjk    | 1000   |      56.06 μs |     2.402 μs |   0.132 μs |         - |
| **Latin**  | **10000**  |   **5,514.80 μs** | **1,122.506 μs** |  **61.528 μs** |         **-** |
| Cjk    | 10000  |   6,234.71 μs |   289.373 μs |  15.862 μs |         - |
| **Latin**  | **65536**  | **202,152.38 μs** | **5,826.234 μs** | **319.356 μs** |         **-** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

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
| Unigram | 599.5 ms | 16.27 ms | 0.89 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 555.7 ms | 31.29 ms | 1.72 ms |  0.93 |  7000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

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
| **BpeOnOnePathologicalToken** | **512**    | **104.0 μs** |   **4.04 μs** | **0.22 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **217.5 μs** |   **3.52 μs** | **0.19 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **479.6 μs** |  **25.81 μs** | **1.41 μs** | **4.3945** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **994.5 μs** | **103.44 μs** | **5.67 μs** | **7.8125** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

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
| **DpGroup**    | **cjk**      |  **19.22 μs** |  **0.286 μs** | **0.016 μs** |         **-** |
| MyersGroup | cjk      | 242.40 μs | 46.931 μs | 2.572 μs |         - |
| **DpGroup**    | **latin**    |  **10.04 μs** |  **0.204 μs** | **0.011 μs** |         **-** |
| MyersGroup | latin    | 130.48 μs |  9.857 μs | 0.540 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

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

| Method         | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|-------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     97.01 ns |     0.674 ns |   0.037 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 12,698.16 ns | 2,318.995 ns | 127.112 ns | 130.89 |    1.14 |      - |         - |          NA |
| TokenSortRatio |  1,104.29 ns |    42.034 ns |   2.304 ns |  11.38 |    0.02 | 0.0782 |    1312 B |          NA |
| TokenSetRatio  |  3,479.54 ns |   301.808 ns |  16.543 ns |  35.87 |    0.15 | 0.3433 |    5760 B |          NA |
| WRatio         |  4,791.86 ns |   490.386 ns |  26.880 ns |  49.39 |    0.24 | 0.4272 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

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

| Method                     | Length | Mean          | Error          | StdDev        | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|---------------:|--------------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **26.82 ns** |       **0.908 ns** |      **0.050 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     139.01 ns |       2.200 ns |      0.121 ns |  5.18 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      28.59 ns |       1.494 ns |      0.082 ns |  1.07 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.60 ns |       0.236 ns |      0.013 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **28.24 ns** |       **0.970 ns** |      **0.053 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     151.79 ns |       4.726 ns |      0.259 ns |  5.38 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      31.46 ns |       4.971 ns |      0.272 ns |  1.11 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      28.26 ns |       0.395 ns |      0.022 ns |  1.00 |    0.00 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.45 ns** |       **0.309 ns** |      **0.017 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     158.86 ns |      26.774 ns |      1.468 ns |  5.22 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.01 ns |       1.434 ns |      0.079 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      30.90 ns |       1.664 ns |      0.091 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **35.48 ns** |       **0.433 ns** |      **0.024 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     187.52 ns |       9.148 ns |      0.501 ns |  5.29 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      35.62 ns |       0.754 ns |      0.041 ns |  1.00 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      32.89 ns |       6.060 ns |      0.332 ns |  0.93 |    0.01 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **54.28 ns** |       **0.758 ns** |      **0.042 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     691.70 ns |      19.576 ns |      1.073 ns | 12.74 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      60.07 ns |       5.135 ns |      0.281 ns |  1.11 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      57.60 ns |       9.816 ns |      0.538 ns |  1.06 |    0.01 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **60.98 ns** |       **0.210 ns** |      **0.011 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |   1,016.35 ns |      27.286 ns |      1.496 ns | 16.67 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      64.80 ns |       0.719 ns |      0.039 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      59.99 ns |       0.643 ns |      0.035 ns |  0.98 |    0.00 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **128**    |     **945.97 ns** |      **41.754 ns** |      **2.289 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  21,637.00 ns |     163.167 ns |      8.944 ns | 22.87 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |     933.14 ns |       5.889 ns |      0.323 ns |  0.99 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |     932.86 ns |      56.990 ns |      3.124 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |               |                |               |       |         |           |             |
| **Distance_Utf16**             | **512**    |   **7,427.45 ns** |     **334.748 ns** |     **18.349 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 349,853.88 ns | 306,000.540 ns | 16,772.922 ns | 47.10 |    1.96 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |   7,868.57 ns |     313.913 ns |     17.207 ns |  1.06 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |   7,716.02 ns |     462.443 ns |     25.348 ns |  1.04 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

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
| **Dp**         | **8**    |    **130.99 ns** |     **1.801 ns** |   **0.099 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     55.53 ns |     0.605 ns |   0.033 ns |  0.42 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    131.89 ns |     1.513 ns |   0.083 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |     98.12 ns |     2.589 ns |   0.142 ns |  0.75 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **229.66 ns** |    **63.815 ns** |   **3.498 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 12   |     61.84 ns |     0.385 ns |   0.021 ns |  0.27 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    228.99 ns |   145.818 ns |   7.993 ns |  1.00 |    0.03 |         - |          NA |
| Kernel_Cjk | 12   |    110.56 ns |     3.692 ns |   0.202 ns |  0.48 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **14**   |    **268.92 ns** |    **27.689 ns** |   **1.518 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 14   |     67.66 ns |     1.049 ns |   0.058 ns |  0.25 |    0.00 |         - |          NA |
| Dp_Cjk     | 14   |    274.39 ns |   206.129 ns |  11.299 ns |  1.02 |    0.04 |         - |          NA |
| Kernel_Cjk | 14   |    112.64 ns |     0.696 ns |   0.038 ns |  0.42 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **364.28 ns** |   **196.082 ns** |  **10.748 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Kernel     | 16   |     70.56 ns |     0.679 ns |   0.037 ns |  0.19 |    0.01 |         - |          NA |
| Dp_Cjk     | 16   |    352.91 ns |    10.398 ns |   0.570 ns |  0.97 |    0.03 |         - |          NA |
| Kernel_Cjk | 16   |    119.13 ns |    11.912 ns |   0.653 ns |  0.33 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **18**   |    **437.96 ns** |    **94.620 ns** |   **5.186 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 18   |     74.53 ns |     2.588 ns |   0.142 ns |  0.17 |    0.00 |         - |          NA |
| Dp_Cjk     | 18   |    439.43 ns |    99.689 ns |   5.464 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 18   |    124.45 ns |     7.506 ns |   0.411 ns |  0.28 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **20**   |    **752.98 ns** |   **239.756 ns** |  **13.142 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 20   |     79.99 ns |     1.832 ns |   0.100 ns |  0.11 |    0.00 |         - |          NA |
| Dp_Cjk     | 20   |    771.44 ns |    32.166 ns |   1.763 ns |  1.02 |    0.02 |         - |          NA |
| Kernel_Cjk | 20   |    128.45 ns |     8.078 ns |   0.443 ns |  0.17 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **991.65 ns** |    **85.534 ns** |   **4.688 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 24   |     86.42 ns |     1.880 ns |   0.103 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    991.36 ns |   169.160 ns |   9.272 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 24   |    143.07 ns |    53.774 ns |   2.948 ns |  0.14 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,564.79 ns** |   **262.436 ns** |  **14.385 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 32   |    105.78 ns |     4.422 ns |   0.242 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,549.68 ns |   576.425 ns |  31.596 ns |  0.99 |    0.02 |         - |          NA |
| Kernel_Cjk | 32   |    167.28 ns |     1.249 ns |   0.068 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,623.65 ns** |   **844.006 ns** |  **46.263 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel     | 48   |    130.58 ns |     0.542 ns |   0.030 ns |  0.04 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,595.50 ns |   480.501 ns |  26.338 ns |  0.99 |    0.01 |         - |          NA |
| Kernel_Cjk | 48   |    241.04 ns |    45.553 ns |   2.497 ns |  0.07 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **5,504.47 ns** |   **428.336 ns** |  **23.479 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Kernel     | 64   |    156.61 ns |     4.899 ns |   0.269 ns |  0.03 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,138.15 ns | 3,297.313 ns | 180.737 ns |  1.12 |    0.03 |         - |          NA |
| Kernel_Cjk | 64   |    294.52 ns |    11.010 ns |   0.603 ns |  0.05 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **11,408.98 ns** |   **230.253 ns** |  **12.621 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 96   |    770.44 ns |    48.829 ns |   2.676 ns |  0.07 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 12,376.70 ns |   405.605 ns |  22.233 ns |  1.08 |    0.00 |         - |          NA |
| Kernel_Cjk | 96   |  1,014.14 ns |   259.469 ns |  14.222 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

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
| **Distance_Utf16**             | **8**      |     **25.52 ns** |   **1.187 ns** |  **0.065 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    120.03 ns |  15.064 ns |  0.826 ns |  4.70 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.19 ns |   0.225 ns |  0.012 ns |  0.99 |    0.00 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **64**     |    **269.79 ns** |   **3.329 ns** |  **0.182 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    671.73 ns |  20.788 ns |  1.139 ns |  2.49 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    273.14 ns |  14.825 ns |  0.813 ns |  1.01 |    0.00 |         - |          NA |
|                            |        |              |            |           |       |         |           |             |
| **Distance_Utf16**             | **512**    | **14,618.33 ns** | **101.858 ns** |  **5.583 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 17,056.56 ns | 646.999 ns | 35.464 ns |  1.17 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 14,086.92 ns | 310.819 ns | 17.037 ns |  0.96 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

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
| **Distance_CodePoint** | **16**     | **32**       |     **340.9 ns** |      **1.15 ns** |     **0.06 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     252.3 ns |      1.44 ns |     0.08 ns |  0.74 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |     **335.3 ns** |     **51.11 ns** |     **2.80 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     242.3 ns |      1.56 ns |     0.09 ns |  0.72 |    0.01 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |     **413.9 ns** |     **19.83 ns** |     **1.09 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     320.8 ns |      1.51 ns |     0.08 ns |  0.78 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |     **418.0 ns** |     **51.05 ns** |     **2.80 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     324.4 ns |      6.67 ns |     0.37 ns |  0.78 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |     **507.4 ns** |      **5.37 ns** |     **0.29 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     412.3 ns |      6.25 ns |     0.34 ns |  0.81 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |     **513.7 ns** |     **58.04 ns** |     **3.18 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     417.6 ns |     24.65 ns |     1.35 ns |  0.81 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |     **590.7 ns** |      **8.14 ns** |     **0.45 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |   1,316.0 ns |    228.25 ns |    12.51 ns |  2.23 |    0.02 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |     **610.8 ns** |      **4.95 ns** |     **0.27 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |   1,300.9 ns |     16.18 ns |     0.89 ns |  2.13 |    0.00 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |   **2,534.9 ns** |    **273.11 ns** |    **14.97 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   5,421.0 ns |     34.40 ns |     1.89 ns |  2.14 |    0.01 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |   **2,569.4 ns** |    **456.82 ns** |    **25.04 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   5,429.2 ns |     98.61 ns |     5.41 ns |  2.11 |    0.02 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |  **21,592.8 ns** |    **833.48 ns** |    **45.69 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       |  64,208.4 ns |    865.38 ns |    47.43 ns |  2.97 |    0.01 |         - |          NA |
|                    |        |          |              |              |             |       |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      | **442,253.4 ns** | **25,571.19 ns** | **1,401.64 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      |  58,658.4 ns |  1,227.29 ns |    67.27 ns |  0.13 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

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
| **Matrix**         | **1000**    | **2**       |      **7,809.8 ns** |     **1,541.49 ns** |     **84.49 ns** | **0.0153** |     **312 B** |
| MatrixWeighted | 1000    | 2       |      7,243.4 ns |     1,139.22 ns |     62.44 ns | 0.0153 |     312 B |
| AccuracyScore  | 1000    | 2       |        922.7 ns |        91.19 ns |      5.00 ns |      - |         - |
| F1Macro        | 1000    | 2       |      7,661.0 ns |       490.11 ns |     26.86 ns | 0.0153 |     472 B |
| Report         | 1000    | 2       |     10,266.7 ns |       245.12 ns |     13.44 ns | 0.3815 |    6520 B |
| **Matrix**         | **1000**    | **10**      |      **7,865.0 ns** |       **417.15 ns** |     **22.87 ns** | **0.0610** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |      7,966.2 ns |       504.88 ns |     27.67 ns | 0.0610 |    1248 B |
| AccuracyScore  | 1000    | 10      |        920.9 ns |        28.51 ns |      1.56 ns |      - |         - |
| F1Macro        | 1000    | 10      |      8,053.3 ns |       428.42 ns |     23.48 ns | 0.0916 |    1664 B |
| Report         | 1000    | 10      |     16,043.5 ns |     1,232.84 ns |     67.58 ns | 0.9155 |   15496 B |
| **Matrix**         | **100000**  | **2**       |    **880,874.1 ns** |     **9,475.88 ns** |    **519.40 ns** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |    825,557.9 ns |    29,993.70 ns |  1,644.06 ns |      - |     313 B |
| AccuracyScore  | 100000  | 2       |    163,487.1 ns |     3,946.96 ns |    216.35 ns |      - |         - |
| F1Macro        | 100000  | 2       |    877,166.6 ns |    20,558.55 ns |  1,126.88 ns |      - |     473 B |
| Report         | 100000  | 2       |    887,015.4 ns |    43,440.53 ns |  2,381.12 ns |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |    **986,424.8 ns** |   **105,949.97 ns** |  **5,807.48 ns** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |    948,073.2 ns |    72,774.51 ns |  3,989.02 ns |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |    274,693.4 ns |    19,808.84 ns |  1,085.79 ns |      - |         - |
| F1Macro        | 100000  | 10      |    981,168.1 ns |    37,104.75 ns |  2,033.84 ns |      - |    1665 B |
| Report         | 100000  | 10      |    995,617.6 ns |    39,733.15 ns |  2,177.91 ns |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       |  **8,653,845.8 ns** |   **276,744.87 ns** | **15,169.32 ns** |      **-** |     **324 B** |
| MatrixWeighted | 1000000 | 2       |  8,301,638.4 ns |   476,993.80 ns | 26,145.64 ns |      - |     324 B |
| AccuracyScore  | 1000000 | 2       |  1,747,087.6 ns |     1,386.31 ns |     75.99 ns |      - |         - |
| F1Macro        | 1000000 | 2       |  8,329,075.9 ns |   973,695.21 ns | 53,371.52 ns |      - |     484 B |
| Report         | 1000000 | 2       |  8,652,480.8 ns |   242,634.57 ns | 13,299.62 ns |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **10,111,704.0 ns** | **1,442,678.11 ns** | **79,078.06 ns** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      |  9,336,022.1 ns |   281,068.56 ns | 15,406.32 ns |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      |  2,686,067.3 ns |    57,151.19 ns |  3,132.65 ns |      - |         - |
| F1Macro        | 1000000 | 10      | 10,695,158.4 ns | 1,497,209.31 ns | 82,067.09 ns |      - |    1676 B |
| Report         | 1000000 | 10      |  9,670,517.7 ns |   322,890.63 ns | 17,698.72 ns |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

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
| **Dp**         | **4**    |     **75.58 ns** |     **2.082 ns** |   **0.114 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 4    |     73.54 ns |     0.742 ns |   0.041 ns |  0.97 |    0.00 |         - |          NA |
| Dp_Cjk     | 4    |     74.28 ns |     0.404 ns |   0.022 ns |  0.98 |    0.00 |         - |          NA |
| Kernel_Cjk | 4    |     73.83 ns |     0.635 ns |   0.035 ns |  0.98 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **6**    |    **104.13 ns** |     **1.654 ns** |   **0.091 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 6    |     77.85 ns |     1.370 ns |   0.075 ns |  0.75 |    0.00 |         - |          NA |
| Dp_Cjk     | 6    |    104.87 ns |     5.800 ns |   0.318 ns |  1.01 |    0.00 |         - |          NA |
| Kernel_Cjk | 6    |    129.61 ns |    27.720 ns |   1.519 ns |  1.24 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **8**    |    **156.70 ns** |    **10.762 ns** |   **0.590 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 8    |     88.50 ns |     1.206 ns |   0.066 ns |  0.56 |    0.00 |         - |          NA |
| Dp_Cjk     | 8    |    154.94 ns |     1.829 ns |   0.100 ns |  0.99 |    0.00 |         - |          NA |
| Kernel_Cjk | 8    |    203.18 ns |     2.844 ns |   0.156 ns |  1.30 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **10**   |    **196.61 ns** |     **3.403 ns** |   **0.187 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 10   |     95.88 ns |     1.505 ns |   0.082 ns |  0.49 |    0.00 |         - |          NA |
| Dp_Cjk     | 10   |    197.07 ns |     1.865 ns |   0.102 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 10   |    151.17 ns |     7.700 ns |   0.422 ns |  0.77 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **12**   |    **244.75 ns** |     **2.770 ns** |   **0.152 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 12   |    104.44 ns |     1.827 ns |   0.100 ns |  0.43 |    0.00 |         - |          NA |
| Dp_Cjk     | 12   |    248.05 ns |    43.058 ns |   2.360 ns |  1.01 |    0.01 |         - |          NA |
| Kernel_Cjk | 12   |    164.37 ns |     7.688 ns |   0.421 ns |  0.67 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **16**   |    **418.38 ns** |   **208.697 ns** |  **11.439 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 16   |    122.30 ns |    18.251 ns |   1.000 ns |  0.29 |    0.01 |         - |          NA |
| Dp_Cjk     | 16   |    413.77 ns |    60.673 ns |   3.326 ns |  0.99 |    0.02 |         - |          NA |
| Kernel_Cjk | 16   |    182.36 ns |     3.364 ns |   0.184 ns |  0.44 |    0.01 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **24**   |    **842.45 ns** |    **17.654 ns** |   **0.968 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 24   |    158.21 ns |     1.332 ns |   0.073 ns |  0.19 |    0.00 |         - |          NA |
| Dp_Cjk     | 24   |    846.52 ns |   164.169 ns |   8.999 ns |  1.00 |    0.01 |         - |          NA |
| Kernel_Cjk | 24   |    228.25 ns |     2.026 ns |   0.111 ns |  0.27 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **32**   |  **1,450.80 ns** |    **18.281 ns** |   **1.002 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 32   |    193.02 ns |    17.519 ns |   0.960 ns |  0.13 |    0.00 |         - |          NA |
| Dp_Cjk     | 32   |  1,446.34 ns |    22.676 ns |   1.243 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 32   |    271.25 ns |     3.246 ns |   0.178 ns |  0.19 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **48**   |  **3,301.63 ns** |    **72.516 ns** |   **3.975 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 48   |    267.44 ns |     9.750 ns |   0.534 ns |  0.08 |    0.00 |         - |          NA |
| Dp_Cjk     | 48   |  3,301.62 ns |    63.312 ns |   3.470 ns |  1.00 |    0.00 |         - |          NA |
| Kernel_Cjk | 48   |    354.69 ns |     6.606 ns |   0.362 ns |  0.11 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **64**   |  **6,316.42 ns** | **2,401.949 ns** | **131.659 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| Kernel     | 64   |    340.75 ns |     3.980 ns |   0.218 ns |  0.05 |    0.00 |         - |          NA |
| Dp_Cjk     | 64   |  6,202.51 ns |   385.705 ns |  21.142 ns |  0.98 |    0.02 |         - |          NA |
| Kernel_Cjk | 64   |    648.86 ns |    90.704 ns |   4.972 ns |  0.10 |    0.00 |         - |          NA |
|            |      |              |              |            |       |         |           |             |
| **Dp**         | **96**   | **13,949.47 ns** |   **192.725 ns** |  **10.564 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel     | 96   |  1,240.38 ns |   480.458 ns |  26.336 ns |  0.09 |    0.00 |         - |          NA |
| Dp_Cjk     | 96   | 14,652.87 ns |   850.373 ns |  46.612 ns |  1.05 |    0.00 |         - |          NA |
| Kernel_Cjk | 96   |  1,472.75 ns |     4.450 ns |   0.244 ns |  0.11 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

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
| VocabTxt               |  4.075 ms | 1.9486 ms | 0.1068 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.808 ms | 3.9492 ms | 0.2165 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.773 ms | 0.5510 ms | 0.0302 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.769 ms | 4.2484 ms | 0.2329 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.843 ms | 0.2028 ms | 0.0111 ms |  33.2031 |  27.3438 |  27.3438 |   2.09 MB |
| TfidfLoad              |  4.375 ms | 0.8205 ms | 0.0450 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  6.837 ms | 5.3548 ms | 0.2935 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  5.799 ms | 1.1955 ms | 0.0655 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

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

| Method               | Documents | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1      | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|----------:|----------:|------:|--------:|----------:|----------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.318 ms** | **0.5733 ms** | **0.0314 ms** |  **1.00** |    **0.01** |  **500.0000** |  **234.3750** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.176 ms | 0.0411 ms | 0.0023 ms |  0.84 |    0.00 |  390.6250 |  187.5000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.489 ms | 2.2474 ms | 0.1232 ms |  1.02 |    0.02 |  507.8125 |  171.8750 |  70.3125 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.441 ms | 0.3180 ms | 0.0174 ms |  0.88 |    0.00 |  406.2500 |  156.2500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |         |           |           |          |           |             |
| **Count**                | **1000**      | **30.308 ms** | **2.0761 ms** | **0.1138 ms** |  **1.00** |    **0.00** | **2718.7500** | **1031.2500** | **531.2500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 23.852 ms | 2.0932 ms | 0.1147 ms |  0.79 |    0.00 | 1968.7500 |  781.2500 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 28.934 ms | 1.7554 ms | 0.0962 ms |  0.95 |    0.00 | 2562.5000 |  750.0000 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 24.562 ms | 1.7682 ms | 0.0969 ms |  0.81 |    0.00 | 2031.2500 |  625.0000 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

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
| **Dot**    | **384**  |  **50.93 ns** | **1.958 ns** | **0.107 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  51.49 ns | 3.063 ns | 0.168 ns |  1.01 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  |  **95.02 ns** | **1.714 ns** | **0.094 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  |  92.43 ns | 0.181 ns | 0.010 ns |  0.97 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **123.97 ns** | **2.596 ns** | **0.142 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 122.65 ns | 0.738 ns | 0.040 ns |  0.99 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

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
| **Count**        | **200**       |  **2.889 ms** | **1.3229 ms** | **0.0725 ms** |  **1.00** |    **0.03** |  **93.7500** |  **39.0625** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  2.946 ms | 2.0449 ms | 0.1121 ms |  1.02 |    0.04 | 101.5625 |  39.0625 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.762 ms | 0.0468 ms | 0.0026 ms |  1.30 |    0.03 | 171.8750 | 109.3750 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.971 ms | 1.5502 ms | 0.0850 ms |  1.03 |    0.03 |  93.7500 |  23.4375 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **7.135 ms** | **3.1660 ms** | **0.1735 ms** |  **1.00** |    **0.03** | **484.3750** | **343.7500** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.144 ms | 1.1195 ms | 0.0614 ms |  1.00 |    0.02 | 484.3750 | 312.5000 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.530 ms | 0.7178 ms | 0.0393 ms |  1.62 |    0.03 | 906.2500 | 375.0000 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  6.862 ms | 0.7555 ms | 0.0414 ms |  0.96 |    0.02 | 492.1875 | 156.2500 |  70.3125 |   7.85 MB |        1.00 |

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

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 115.6 | 25.8 | 4.48x C# faster |
| latin | 32 | 179.8 | 88.1 | 2.04x C# faster |
| latin | 128 | 472.9 | 912.4 | 1.93x Py faster |
| latin | 512 | 4451.1 | 7820.9 | 1.76x Py faster |
| cjk | 8 | 138.8 | 27.5 | 5.05x C# faster |
| cjk | 32 | 331.1 | 224.4 | 1.48x C# faster |
| cjk | 128 | 1905.8 | 1670.3 | 1.14x C# faster |
| cjk | 512 | 14893.6 | 10783.9 | 1.38x C# faster |

Note: Indel is len(a)+len(b)-2*LCS on both sides, so this compares the subsequence kernels. Lodestar's is Hyyro's bit-parallel LLCS above a pattern of 8 and a rolling-row dynamic program below it (#273).

<!-- markdownlint-enable MD060 -->

### compare-levenshtein

```text
Python: rapidfuzz 3.14.5 (py 3.12.14)
C#:     Lodestar.Text on .NET 10.0.11 (mode Utf16Unit)
```

<!-- markdownlint-disable MD060 -->

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
|---|---:|---:|---:|:---|
| latin | 8 | 158.6 | 17.5 | 9.07x C# faster |
| latin | 32 | 289.3 | 158.2 | 1.83x C# faster |
| latin | 128 | 1697.4 | 1497.4 | 1.13x C# faster |
| latin | 512 | 14090.4 | 15029.9 | 1.07x Py faster |
| cjk | 8 | 149.3 | 17.5 | 8.52x C# faster |
| cjk | 32 | 369.3 | 288.3 | 1.28x C# faster |
| cjk | 128 | 2873.1 | 2336.9 | 1.23x C# faster |
| cjk | 512 | 23651.2 | 18300.0 | 1.29x C# faster |

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
| confusion_matrix_n1000_k2 | 0.009 | 0.962 | 104.13x | 0.009 | 0.962 | 104.12x |
| accuracy_n1000_k2 | 0.001 | 0.513 | 499.08x | 0.001 | 0.513 | 499.08x |
| precision_recall_f1_macro_n1000_k2 | 0.008 | 1.724 | 222.70x | 0.008 | 1.724 | 222.70x |
| classification_report_n1000_k2 | 0.010 | 6.594 | 633.67x | 0.010 | 6.593 | 633.64x |
| roc_auc_binary_n1000_k2 | 0.016 | 1.893 | 118.20x | 0.016 | 1.893 | 118.21x |
| balanced_accuracy_n1000_k2 | 0.008 | 1.026 | 133.63x | 0.008 | 1.026 | 133.64x |
| matthews_n1000_k2 | 0.008 | 1.927 | 253.18x | 0.008 | 1.927 | 253.17x |
| cohen_kappa_n1000_k2 | 0.008 | 1.074 | 140.14x | 0.008 | 1.074 | 140.14x |
| mse_n1000_k2 | 0.002 | 0.302 | 125.12x | 0.002 | 0.302 | 125.11x |
| mae_n1000_k2 | 0.002 | 0.301 | 124.48x | 0.002 | 0.301 | 124.47x |
| median_ae_n1000_k2 | 0.006 | 0.314 | 55.46x | 0.006 | 0.314 | 55.46x |
| r2_n1000_k2 | 0.003 | 0.366 | 142.19x | 0.003 | 0.366 | 142.18x |
| confusion_matrix_n1000_k10 | 0.009 | 0.968 | 101.94x | 0.009 | 0.968 | 101.94x |
| accuracy_n1000_k10 | 0.001 | 0.513 | 455.14x | 0.001 | 0.513 | 455.12x |
| precision_recall_f1_macro_n1000_k10 | 0.008 | 1.761 | 211.23x | 0.008 | 1.761 | 211.24x |
| classification_report_n1000_k10 | 0.015 | 6.895 | 461.23x | 0.015 | 6.895 | 461.23x |
| roc_auc_ovr_macro_n1000_k10 | 0.542 | 9.784 | 18.04x | 0.542 | 9.782 | 18.04x |
| balanced_accuracy_n1000_k10 | 0.008 | 1.036 | 123.56x | 0.008 | 1.035 | 123.56x |
| matthews_n1000_k10 | 0.008 | 1.987 | 241.88x | 0.008 | 1.987 | 241.89x |
| cohen_kappa_n1000_k10 | 0.009 | 1.079 | 124.63x | 0.009 | 1.079 | 124.62x |
| mse_n1000_k10 | 0.002 | 0.302 | 124.90x | 0.002 | 0.302 | 124.90x |
| mae_n1000_k10 | 0.002 | 0.301 | 124.18x | 0.002 | 0.301 | 124.19x |
| median_ae_n1000_k10 | 0.006 | 0.312 | 55.47x | 0.006 | 0.312 | 55.47x |
| r2_n1000_k10 | 0.003 | 0.363 | 141.22x | 0.003 | 0.363 | 141.22x |
| confusion_matrix_n100000_k2 | 0.987 | 10.695 | 10.83x | 0.987 | 10.694 | 10.83x |
| accuracy_n100000_k2 | 0.186 | 3.740 | 20.15x | 0.186 | 3.740 | 20.16x |
| precision_recall_f1_macro_n100000_k2 | 0.863 | 12.235 | 14.18x | 0.863 | 12.234 | 14.18x |
| classification_report_n100000_k2 | 0.865 | 26.733 | 30.90x | 0.865 | 26.732 | 30.90x |
| roc_auc_binary_n100000_k2 | 3.596 | 26.466 | 7.36x | 3.595 | 26.464 | 7.36x |
| balanced_accuracy_n100000_k2 | 0.865 | 10.759 | 12.43x | 0.865 | 10.758 | 12.43x |
| matthews_n100000_k2 | 0.858 | 21.408 | 24.95x | 0.858 | 21.406 | 24.95x |
| cohen_kappa_n100000_k2 | 0.864 | 10.794 | 12.49x | 0.864 | 10.793 | 12.49x |
| mse_n100000_k2 | 0.238 | 0.461 | 1.94x | 0.238 | 0.461 | 1.94x |
| mae_n100000_k2 | 0.238 | 0.453 | 1.91x | 0.238 | 0.453 | 1.91x |
| median_ae_n100000_k2 | 0.684 | 1.791 | 2.62x | 0.708 | 1.791 | 2.53x |
| r2_n100000_k2 | 0.235 | 0.701 | 2.99x | 0.235 | 0.701 | 2.99x |
| confusion_matrix_n100000_k10 | 0.939 | 10.676 | 11.37x | 0.939 | 10.676 | 11.37x |
| accuracy_n100000_k10 | 0.272 | 3.740 | 13.74x | 0.272 | 3.739 | 13.74x |
| precision_recall_f1_macro_n100000_k10 | 0.991 | 12.892 | 13.01x | 0.991 | 12.891 | 13.00x |
| classification_report_n100000_k10 | 0.984 | 29.399 | 29.88x | 0.984 | 29.396 | 29.87x |
| roc_auc_ovr_macro_n100000_k10 | 34.645 | 213.385 | 6.16x | 34.644 | 213.363 | 6.16x |
| balanced_accuracy_n100000_k10 | 0.979 | 10.761 | 10.99x | 0.979 | 10.760 | 10.99x |
| matthews_n100000_k10 | 0.971 | 22.154 | 22.82x | 0.971 | 22.153 | 22.82x |
| cohen_kappa_n100000_k10 | 0.997 | 10.911 | 10.94x | 0.997 | 10.910 | 10.94x |
| mse_n100000_k10 | 0.238 | 0.460 | 1.93x | 0.238 | 0.460 | 1.93x |
| mae_n100000_k10 | 0.238 | 0.452 | 1.90x | 0.238 | 0.452 | 1.90x |
| median_ae_n100000_k10 | 0.717 | 1.799 | 2.51x | 0.773 | 1.798 | 2.33x |
| r2_n100000_k10 | 0.235 | 0.698 | 2.97x | 0.235 | 0.698 | 2.97x |
| confusion_matrix_n1000000_k2 | 8.298 | 100.408 | 12.10x | 8.298 | 100.396 | 12.10x |
| accuracy_n1000000_k2 | 1.953 | 33.170 | 16.98x | 1.953 | 33.162 | 16.98x |
| precision_recall_f1_macro_n1000000_k2 | 8.674 | 107.828 | 12.43x | 8.673 | 107.822 | 12.43x |
| classification_report_n1000000_k2 | 8.613 | 210.681 | 24.46x | 8.613 | 210.663 | 24.46x |
| roc_auc_binary_n1000000_k2 | 42.770 | 289.328 | 6.76x | 42.766 | 289.330 | 6.77x |
| balanced_accuracy_n1000000_k2 | 8.674 | 100.325 | 11.57x | 8.674 | 100.319 | 11.57x |
| matthews_n1000000_k2 | 8.609 | 201.271 | 23.38x | 8.609 | 201.257 | 23.38x |
| cohen_kappa_n1000000_k2 | 8.678 | 100.503 | 11.58x | 8.678 | 100.500 | 11.58x |
| mse_n1000000_k2 | 2.379 | 2.384 | 1.00x | 2.379 | 2.383 | 1.00x |
| mae_n1000000_k2 | 2.379 | 2.353 | 0.99x | 2.379 | 2.352 | 0.99x |
| median_ae_n1000000_k2 | 6.341 | 14.381 | 2.27x | 6.443 | 14.381 | 2.23x |
| r2_n1000000_k2 | 2.347 | 4.130 | 1.76x | 2.347 | 4.130 | 1.76x |
| confusion_matrix_n1000000_k10 | 9.368 | 100.371 | 10.71x | 9.368 | 100.342 | 10.71x |
| accuracy_n1000000_k10 | 2.797 | 33.072 | 11.82x | 2.797 | 33.065 | 11.82x |
| precision_recall_f1_macro_n1000000_k10 | 9.769 | 114.310 | 11.70x | 9.769 | 114.306 | 11.70x |
| classification_report_n1000000_k10 | 9.745 | 234.996 | 24.12x | 9.744 | 234.970 | 24.11x |
| balanced_accuracy_n1000000_k10 | 9.824 | 100.122 | 10.19x | 9.824 | 100.113 | 10.19x |
| matthews_n1000000_k10 | 9.724 | 208.479 | 21.44x | 9.723 | 208.468 | 21.44x |
| cohen_kappa_n1000000_k10 | 9.821 | 100.094 | 10.19x | 9.819 | 100.087 | 10.19x |
| mse_n1000000_k10 | 2.376 | 2.269 | 0.95x | 2.376 | 2.269 | 0.95x |
| mae_n1000000_k10 | 2.379 | 2.348 | 0.99x | 2.379 | 2.348 | 0.99x |
| median_ae_n1000000_k10 | 6.388 | 14.450 | 2.26x | 6.496 | 14.449 | 2.22x |
| r2_n1000000_k10 | 2.656 | 4.018 | 1.51x | 2.656 | 4.017 | 1.51x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

BELOW GATE on processor time:
  mae_n1000000_k2                  0.99x
  mse_n1000000_k10                 0.95x
  mae_n1000000_k10                 0.99x

<!-- markdownlint-enable MD060 -->

### compare-persistence

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu | C# bytes | Py bytes |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| vocab_txt | 4.930 | 9.750 | 1.98x | 5.151 | 9.750 | 1.89x | 228,891 | 228,891 |
| tokenizer_json_wordpiece | 12.203 | 15.878 | 1.30x | 12.638 | 15.877 | 1.26x | 706,526 | 706,526 |
| tokenizer_json_unigram | 12.816 | 34.999 | 2.73x | 13.087 | 34.996 | 2.67x | 1,990,038 | 1,990,038 |
| spiece_model | 4.589 | 27.865 | 6.07x | 4.789 | 27.864 | 5.82x | 533,084 | 533,084 |
| tfidf_save | 1.781 | 2.548 | 1.43x | 1.846 | 2.548 | 1.38x | 581,787 | 591,922 |
| tfidf_load | 5.283 | 3.996 | 0.76x | 5.560 | 3.996 | 0.72x | 581,787 | 591,922 |
| embedding_index_save | 6.828 | 1.463 | 0.21x | 7.369 | 1.463 | 0.20x | 20,589,007 | 15,360,128 |
| embedding_index_load | 5.899 | 1.527 | 0.26x | 6.564 | 1.527 | 0.23x | 20,589,007 | 15,360,128 |
| embedding_index_load_file | 6.475 | 0.906 | 0.14x | 7.180 | 0.905 | 0.13x | 20,589,007 | 15,360,128 |
| embedding_index_load_memory | 4.082 | 1.512 | 0.37x | 4.428 | 1.512 | 0.34x | 20,589,007 | 15,360,128 |
| embedding_index_view_floor | 0.000 | 0.001 | 82.32x | 0.000 | 0.001 | 82.33x | 20,589,007 | 15,360,128 |
| embedding_index_save_gzip | 411.230 | 554.575 | 1.35x | 412.486 | 554.550 | 1.34x | 15,251,458 | 14,022,374 |
| embedding_index_load_gzip | 77.969 | 66.361 | 0.85x | 79.221 | 66.357 | 0.84x | 15,251,458 | 14,022,374 |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.
bytes is what the row wrote or read; a results file from before #378
carries none, and the two columns then disappear rather than read zero.

<!-- markdownlint-enable MD060 -->
