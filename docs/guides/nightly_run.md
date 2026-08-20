# Nightly benchmark run

<!-- nightly-baseline: 5c7184c12dfc09032f4dfc96d2f733cce63a5ac7 -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `5c7184c12dfc09032f4dfc96d2f733cce63a5ac7`
- Previous run: `5c7184c12dfc09032f4dfc96d2f733cce63a5ac7`
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
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3
```

<!-- markdownlint-disable MD060 -->

| Method             | CorpusSize | Mean         | Error       | StdDev     | Ratio | Gen0    | Gen1   | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|-----------:|------:|--------:|-------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **6.677 μs** |   **0.9141 μs** |  **0.0501 μs** |  **1.00** |  **0.1068** |      **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.736 μs |   0.5170 μs |  0.0283 μs |  1.01 |  0.1221 |      - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.863 μs |   0.4324 μs |  0.0237 μs |  1.03 |  0.1221 |      - |    3.16 KB |        1.14 |
|                    |            |              |             |            |       |         |        |            |             |
| **UnitLoop**           | **8**          |   **115.288 μs** |  **21.3035 μs** |  **1.1677 μs** |  **1.00** |  **5.3711** | **0.1221** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    69.291 μs |   4.7477 μs |  0.2602 μs |  0.60 |  5.1270 | 0.2441 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    71.788 μs |   8.2604 μs |  0.4528 μs |  0.62 |  5.1270 | 0.2441 |  127.31 KB |        0.95 |
|                    |            |              |             |            |       |         |        |            |             |
| **UnitLoop**           | **32**         |   **415.255 μs** |  **86.6798 μs** |  **4.7512 μs** |  **1.00** | **19.0430** | **0.4883** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   243.361 μs |  28.7706 μs |  1.5770 μs |  0.59 | 17.8223 | 1.2207 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   235.871 μs |  22.8282 μs |  1.2513 μs |  0.57 | 17.3340 | 1.2207 |   427.8 KB |        0.91 |
|                    |            |              |             |            |       |         |        |            |             |
| **UnitLoop**           | **128**        | **1,659.318 μs** | **152.9292 μs** |  **8.3826 μs** |  **1.00** | **76.1719** | **3.9063** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        |   993.954 μs |  98.9489 μs |  5.4237 μs |  0.60 | 70.3125 | 9.7656 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   937.272 μs | 231.2097 μs | 12.6734 μs |  0.56 | 68.3594 | 9.7656 |  1696.9 KB |        0.91 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

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
| Unigram | 571.0 ms | 60.35 ms | 3.31 ms |  1.00 | 21000.0000 | 519.51 MB |        1.00 |
| Bpe     | 525.7 ms | 32.29 ms | 1.77 ms |  0.92 |  4000.0000 | 112.18 MB |        0.22 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BpeScalingBenchmarks-report-github

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
| **BpeOnOnePathologicalToken** | **512**    |   **106.9 μs** | **20.90 μs** | **1.15 μs** | **0.7324** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   |   **249.4 μs** |  **7.74 μs** | **0.42 μs** | **1.4648** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   |   **519.3 μs** | **31.40 μs** | **1.72 μs** | **2.9297** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **1,066.3 μs** | **45.32 μs** | **2.48 μs** | **5.8594** | **157.03 KB** |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.BucketRouteDiagnostics-report-github

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

| Method     | Mean      | Error    | StdDev   | Allocated |
|----------- |----------:|---------:|---------:|----------:|
| DpGroup    |  12.77 μs | 4.896 μs | 0.268 μs |         - |
| MyersGroup | 130.52 μs | 0.946 μs | 0.052 μs |         - |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.FuzzBenchmarks-report-github

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

| Method         | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Ratio          |     90.29 ns |   0.260 ns |  0.014 ns |   1.00 |    0.00 |      - |         - |          NA |
| PartialRatio   | 17,613.98 ns | 523.877 ns | 28.715 ns | 195.08 |    0.28 |      - |         - |          NA |
| TokenSortRatio |  1,062.45 ns |   8.324 ns |  0.456 ns |  11.77 |    0.00 | 0.0515 |    1312 B |          NA |
| TokenSetRatio  |  3,248.47 ns | 291.918 ns | 16.001 ns |  35.98 |    0.15 | 0.2289 |    5760 B |          NA |
| WRatio         |  4,651.75 ns | 367.258 ns | 20.131 ns |  51.52 |    0.19 | 0.2823 |    7200 B |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.IndelBenchmarks-report-github

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

| Method                     | Length | Mean          | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------- |--------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |      **27.90 ns** |     **1.693 ns** |   **0.093 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |     142.36 ns |     5.207 ns |   0.285 ns |  5.10 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |      27.53 ns |     3.446 ns |   0.189 ns |  0.99 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 8      |      26.60 ns |     1.469 ns |   0.081 ns |  0.95 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **12**     |      **27.69 ns** |     **0.330 ns** |   **0.018 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 12     |     146.16 ns |     6.140 ns |   0.337 ns |  5.28 |    0.01 |         - |          NA |
| NormalizedSimilarity_Utf16 | 12     |      28.24 ns |     1.399 ns |   0.077 ns |  1.02 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 12     |      26.90 ns |     0.484 ns |   0.027 ns |  0.97 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **16**     |      **30.06 ns** |     **2.621 ns** |   **0.144 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_CodePoint         | 16     |     155.52 ns |     5.548 ns |   0.304 ns |  5.17 |    0.02 |         - |          NA |
| NormalizedSimilarity_Utf16 | 16     |      31.23 ns |     3.221 ns |   0.177 ns |  1.04 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 16     |      29.19 ns |     0.255 ns |   0.014 ns |  0.97 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **20**     |      **89.44 ns** |     **2.356 ns** |   **0.129 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 20     |     213.77 ns |     4.627 ns |   0.254 ns |  2.39 |    0.00 |         - |          NA |
| NormalizedSimilarity_Utf16 | 20     |      91.98 ns |     1.615 ns |   0.089 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 20     |      93.49 ns |     3.236 ns |   0.177 ns |  1.05 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **24**     |      **48.04 ns** |     **1.029 ns** |   **0.056 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 24     |     656.75 ns |    24.525 ns |   1.344 ns | 13.67 |    0.03 |         - |          NA |
| NormalizedSimilarity_Utf16 | 24     |      56.42 ns |     9.009 ns |   0.494 ns |  1.17 |    0.01 |         - |          NA |
| SubsequenceLength_Utf16    | 24     |      48.83 ns |     0.284 ns |   0.016 ns |  1.02 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **32**     |      **59.41 ns** |     **1.395 ns** |   **0.076 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 32     |     777.95 ns |   736.448 ns |  40.367 ns | 13.10 |    0.59 |         - |          NA |
| NormalizedSimilarity_Utf16 | 32     |      73.68 ns |     0.624 ns |   0.034 ns |  1.24 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 32     |      57.21 ns |     0.611 ns |   0.034 ns |  0.96 |    0.00 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **128**    |   **1,195.46 ns** |    **79.622 ns** |   **4.364 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 128    |  18,261.06 ns |   276.124 ns |  15.135 ns | 15.28 |    0.05 |         - |          NA |
| NormalizedSimilarity_Utf16 | 128    |   1,229.73 ns |    31.216 ns |   1.711 ns |  1.03 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 128    |   1,149.39 ns |   123.369 ns |   6.762 ns |  0.96 |    0.01 |         - |          NA |
|                            |        |               |              |            |       |         |           |             |
| **Distance_Utf16**             | **512**    |  **12,823.86 ns** |   **280.736 ns** |  **15.388 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 342,741.23 ns | 8,604.548 ns | 471.644 ns | 26.73 |    0.04 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    |  12,565.99 ns |    77.749 ns |   4.262 ns |  0.98 |    0.00 |         - |          NA |
| SubsequenceLength_Utf16    | 512    |  12,261.78 ns |   690.846 ns |  37.868 ns |  0.96 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LcsGateBenchmarks-report-github

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

| Method | Band | Mean         | Error        | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------- |----- |-------------:|-------------:|-----------:|------:|--------:|----------:|------------:|
| **Dp**     | **8**    |    **123.02 ns** |     **0.488 ns** |   **0.027 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 8    |     55.30 ns |     0.985 ns |   0.054 ns |  0.45 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **12**   |    **232.07 ns** |     **7.186 ns** |   **0.394 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 12   |     62.72 ns |     0.099 ns |   0.005 ns |  0.27 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **14**   |    **266.77 ns** |     **1.475 ns** |   **0.081 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 14   |     66.00 ns |     0.450 ns |   0.025 ns |  0.25 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **16**   |    **378.91 ns** |     **6.275 ns** |   **0.344 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 16   |     70.38 ns |     0.609 ns |   0.033 ns |  0.19 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **18**   |    **461.67 ns** |     **0.831 ns** |   **0.046 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 18   |     74.59 ns |     2.142 ns |   0.117 ns |  0.16 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **20**   |    **557.65 ns** |     **2.678 ns** |   **0.147 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 20   |     78.18 ns |     0.619 ns |   0.034 ns |  0.14 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **24**   |    **802.54 ns** | **1,060.473 ns** |  **58.128 ns** |  **1.00** |    **0.09** |         **-** |          **NA** |
| Kernel | 24   |     88.66 ns |     2.108 ns |   0.116 ns |  0.11 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **32**   |  **1,330.86 ns** | **1,912.620 ns** | **104.837 ns** |  **1.00** |    **0.10** |         **-** |          **NA** |
| Kernel | 32   |    105.72 ns |     4.355 ns |   0.239 ns |  0.08 |    0.01 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **48**   |  **2,603.91 ns** |    **52.753 ns** |   **2.892 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Kernel | 48   |    122.74 ns |     3.700 ns |   0.203 ns |  0.05 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **64**   |  **5,059.74 ns** | **3,484.978 ns** | **191.023 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Kernel | 64   |    145.42 ns |     0.801 ns |   0.044 ns |  0.03 |    0.00 |         - |          NA |
|        |      |              |              |            |       |         |           |             |
| **Dp**     | **96**   | **11,180.56 ns** | **3,295.856 ns** | **180.657 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Kernel | 96   |    958.99 ns |     8.087 ns |   0.443 ns |  0.09 |    0.00 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinBenchmarks-report-github

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

| Method                     | Length | Mean         | Error      | StdDev    | Ratio | Allocated | Alloc Ratio |
|--------------------------- |------- |-------------:|-----------:|----------:|------:|----------:|------------:|
| **Distance_Utf16**             | **8**      |     **27.19 ns** |   **0.996 ns** |  **0.055 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 8      |    137.60 ns |   4.023 ns |  0.221 ns |  5.06 |         - |          NA |
| NormalizedSimilarity_Utf16 | 8      |     25.64 ns |   0.152 ns |  0.008 ns |  0.94 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **64**     |    **264.58 ns** |   **4.753 ns** |  **0.261 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 64     |    703.61 ns |  36.498 ns |  2.001 ns |  2.66 |         - |          NA |
| NormalizedSimilarity_Utf16 | 64     |    265.78 ns |  10.983 ns |  0.602 ns |  1.00 |         - |          NA |
|                            |        |              |            |           |       |           |             |
| **Distance_Utf16**             | **512**    | **14,512.03 ns** |  **23.855 ns** |  **1.308 ns** |  **1.00** |         **-** |          **NA** |
| Distance_CodePoint         | 512    | 17,642.21 ns | 188.878 ns | 10.353 ns |  1.22 |         - |          NA |
| NormalizedSimilarity_Utf16 | 512    | 15,269.21 ns |  37.258 ns |  2.042 ns |  1.05 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.LevenshteinCodePointBenchmarks-report-github

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

| Method             | Length | Distinct | Mean           | Error         | StdDev       | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------- |------- |--------- |---------------:|--------------:|-------------:|-------:|--------:|----------:|------------:|
| **Distance_CodePoint** | **16**     | **32**       |       **378.6 ns** |      **10.88 ns** |      **0.60 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 32       |     1,399.4 ns |      13.91 ns |      0.76 ns |   3.70 |    0.01 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **16**     | **512**      |       **365.5 ns** |      **56.30 ns** |      **3.09 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 16     | 512      |     1,414.6 ns |      15.33 ns |      0.84 ns |   3.87 |    0.03 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **32**       |       **464.8 ns** |      **45.21 ns** |      **2.48 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 32       |     3,329.0 ns |     180.29 ns |      9.88 ns |   7.16 |    0.04 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **24**     | **512**      |       **458.6 ns** |       **3.10 ns** |      **0.17 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 24     | 512      |     3,620.7 ns |   2,960.16 ns |    162.26 ns |   7.90 |    0.31 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **32**       |       **552.7 ns** |     **117.55 ns** |      **6.44 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 32       |     5,899.9 ns |   1,245.88 ns |     68.29 ns |  10.68 |    0.15 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **32**     | **512**      |       **561.9 ns** |       **4.78 ns** |      **0.26 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 32     | 512      |     6,136.2 ns |   1,003.71 ns |     55.02 ns |  10.92 |    0.08 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **32**       |       **644.5 ns** |      **68.46 ns** |      **3.75 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 32       |     9,065.1 ns |   1,000.25 ns |     54.83 ns |  14.06 |    0.10 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **40**     | **512**      |       **637.8 ns** |       **0.53 ns** |      **0.03 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 40     | 512      |     9,509.9 ns |   8,762.61 ns |    480.31 ns |  14.91 |    0.65 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **32**       |     **2,369.5 ns** |     **361.61 ns** |     **19.82 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 32       |   149,268.8 ns |   2,077.50 ns |    113.87 ns |  63.00 |    0.46 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **128**    | **512**      |     **2,368.5 ns** |     **164.37 ns** |      **9.01 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 128    | 512      |   116,739.8 ns | 137,837.61 ns |  7,555.34 ns |  49.29 |    2.77 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **32**       |    **19,381.6 ns** |     **366.90 ns** |     **20.11 ns** |   **1.00** |    **0.00** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 32       | 2,559,824.9 ns | 439,282.14 ns | 24,078.54 ns | 132.07 |    1.08 |         - |          NA |
|                    |        |          |                |               |              |        |         |           |             |
| **Distance_CodePoint** | **512**    | **512**      |   **518,154.2 ns** |  **87,678.38 ns** |  **4,805.95 ns** |   **1.00** |    **0.01** |         **-** |          **NA** |
| Distance_Utf16     | 512    | 512      | 1,892,592.8 ns |  44,654.62 ns |  2,447.67 ns |   3.65 |    0.03 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MetricsBenchmarks-report-github

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
| **Matrix**         | **1000**    | **2**       |     **6.378 μs** |   **0.3796 μs** |  **0.0208 μs** | **0.0076** |     **312 B** |
| MatrixWeighted | 1000    | 2       |     6.571 μs |   0.2823 μs |  0.0155 μs | 0.0076 |     312 B |
| AccuracyScore  | 1000    | 2       |     1.133 μs |   0.0014 μs |  0.0001 μs |      - |         - |
| F1Macro        | 1000    | 2       |     6.579 μs |   0.1225 μs |  0.0067 μs | 0.0153 |     472 B |
| Report         | 1000    | 2       |     9.592 μs |   0.1027 μs |  0.0056 μs | 0.2594 |    6520 B |
| **Matrix**         | **1000**    | **10**      |     **6.500 μs** |   **0.0820 μs** |  **0.0045 μs** | **0.0458** |    **1248 B** |
| MatrixWeighted | 1000    | 10      |     6.745 μs |   0.1630 μs |  0.0089 μs | 0.0458 |    1248 B |
| AccuracyScore  | 1000    | 10      |     1.131 μs |   0.0064 μs |  0.0003 μs |      - |         - |
| F1Macro        | 1000    | 10      |     6.823 μs |   0.1609 μs |  0.0088 μs | 0.0610 |    1664 B |
| Report         | 1000    | 10      |    14.303 μs |   0.8502 μs |  0.0466 μs | 0.6104 |   15496 B |
| **Matrix**         | **100000**  | **2**       |   **788.857 μs** |  **28.9564 μs** |  **1.5872 μs** |      **-** |     **313 B** |
| MatrixWeighted | 100000  | 2       |   755.950 μs |  27.7845 μs |  1.5230 μs |      - |     313 B |
| AccuracyScore  | 100000  | 2       |   180.748 μs |   9.2550 μs |  0.5073 μs |      - |         - |
| F1Macro        | 100000  | 2       |   773.134 μs |  51.0107 μs |  2.7961 μs |      - |     473 B |
| Report         | 100000  | 2       |   754.683 μs |  34.4701 μs |  1.8894 μs |      - |    6545 B |
| **Matrix**         | **100000**  | **10**      |   **886.585 μs** |  **73.9552 μs** |  **4.0537 μs** |      **-** |    **1249 B** |
| MatrixWeighted | 100000  | 10      |   872.791 μs |  47.7538 μs |  2.6175 μs |      - |    1249 B |
| AccuracyScore  | 100000  | 10      |   274.698 μs |   7.2301 μs |  0.3963 μs |      - |         - |
| F1Macro        | 100000  | 10      |   832.664 μs |  27.5345 μs |  1.5093 μs |      - |    1665 B |
| Report         | 100000  | 10      |   872.759 μs |  38.6607 μs |  2.1191 μs |      - |   15841 B |
| **Matrix**         | **1000000** | **2**       | **7,593.906 μs** | **175.1747 μs** |  **9.6019 μs** |      **-** |     **318 B** |
| MatrixWeighted | 1000000 | 2       | 7,558.556 μs | 401.4044 μs | 22.0023 μs |      - |     318 B |
| AccuracyScore  | 1000000 | 2       | 1,969.234 μs |  22.8773 μs |  1.2540 μs |      - |         - |
| F1Macro        | 1000000 | 2       | 7,436.041 μs |  52.0164 μs |  2.8512 μs |      - |     478 B |
| Report         | 1000000 | 2       | 7,872.285 μs | 122.7577 μs |  6.7288 μs |      - |    6572 B |
| **Matrix**         | **1000000** | **10**      | **8,792.429 μs** | **490.3771 μs** | **26.8792 μs** |      **-** |    **1260 B** |
| MatrixWeighted | 1000000 | 10      | 8,717.503 μs |  57.5740 μs |  3.1558 μs |      - |    1260 B |
| AccuracyScore  | 1000000 | 10      | 2,985.182 μs | 116.1795 μs |  6.3682 μs |      - |         - |
| F1Macro        | 1000000 | 10      | 9,235.663 μs | 107.0299 μs |  5.8667 μs |      - |    1676 B |
| Report         | 1000000 | 10      | 8,867.352 μs | 543.0713 μs | 29.7676 μs |      - |   15892 B |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.MyersGateBenchmarks-report-github

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

| Method | Band | Mean         | Error      | StdDev    | Ratio | Allocated | Alloc Ratio |
|------- |----- |-------------:|-----------:|----------:|------:|----------:|------------:|
| **Dp**     | **4**    |     **79.57 ns** |   **3.719 ns** |  **0.204 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 4    |     76.16 ns |   1.235 ns |  0.068 ns |  0.96 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **6**    |    **105.69 ns** |   **1.196 ns** |  **0.066 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 6    |    104.44 ns |   0.837 ns |  0.046 ns |  0.99 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **8**    |    **148.51 ns** |   **0.978 ns** |  **0.054 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 8    |     86.22 ns |   0.343 ns |  0.019 ns |  0.58 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **10**   |    **197.75 ns** |   **1.398 ns** |  **0.077 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 10   |     94.05 ns |   2.007 ns |  0.110 ns |  0.48 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **12**   |    **269.32 ns** |   **2.713 ns** |  **0.149 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 12   |    103.00 ns |   0.041 ns |  0.002 ns |  0.38 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **16**   |    **413.81 ns** |  **12.225 ns** |  **0.670 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 16   |    122.50 ns |  23.650 ns |  1.296 ns |  0.30 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **24**   |    **812.83 ns** |   **9.305 ns** |  **0.510 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 24   |    154.98 ns |   1.313 ns |  0.072 ns |  0.19 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **32**   |  **1,421.31 ns** |   **5.593 ns** |  **0.307 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 32   |    189.68 ns |   1.650 ns |  0.090 ns |  0.13 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **48**   |  **3,109.58 ns** |  **84.841 ns** |  **4.650 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 48   |    259.88 ns |  13.125 ns |  0.719 ns |  0.08 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **64**   |  **5,463.95 ns** | **439.507 ns** | **24.091 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 64   |    327.78 ns |   0.258 ns |  0.014 ns |  0.06 |         - |          NA |
|        |      |              |            |           |       |           |             |
| **Dp**     | **96**   | **12,220.84 ns** | **348.002 ns** | **19.075 ns** |  **1.00** |         **-** |          **NA** |
| Kernel | 96   |  1,117.85 ns |  67.765 ns |  3.714 ns |  0.09 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

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
| VocabTxt               |  3.860 ms | 0.2099 ms | 0.0115 ms |  85.9375 |  78.1250 |  31.2500 |   3.62 MB |
| TokenizerJsonWordPiece | 11.320 ms | 1.2991 ms | 0.0712 ms | 125.0000 | 109.3750 |  31.2500 |   5.72 MB |
| TokenizerJsonUnigram   | 11.722 ms | 0.4666 ms | 0.0256 ms |  78.1250 |  62.5000 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.711 ms | 0.3932 ms | 0.0216 ms |  89.8438 |  85.9375 |  35.1563 |   3.36 MB |
| TfidfSave              |  1.889 ms | 0.2518 ms | 0.0138 ms |  35.1563 |  31.2500 |  31.2500 |   2.09 MB |
| TfidfLoad              |  4.284 ms | 0.3683 ms | 0.0202 ms |  62.5000 |  54.6875 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     | 10.318 ms | 0.6454 ms | 0.0354 ms | 453.1250 | 453.1250 | 453.1250 |  39.64 MB |
| EmbeddingIndexLoad     |  9.709 ms | 1.7092 ms | 0.0937 ms | 468.7500 | 437.5000 | 437.5000 |  35.35 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.StopWordBenchmarks-report-github

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

| Method               | Documents | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |---------- |----------:|----------:|----------:|------:|--------:|----------:|---------:|---------:|----------:|------------:|
| **Count**                | **200**       |  **7.447 ms** | **1.9277 ms** | **0.1057 ms** |  **1.00** |    **0.02** |  **343.7500** | **156.2500** |  **62.5000** |   **7.92 MB** |        **1.00** |
| CountWithStopWords   | 200       |  6.159 ms | 0.5724 ms | 0.0314 ms |  0.83 |    0.01 |  257.8125 | 125.0000 |        - |   6.34 MB |        0.80 |
| Hashing              | 200       |  7.431 ms | 0.6762 ms | 0.0371 ms |  1.00 |    0.01 |  359.3750 | 140.6250 |  62.5000 |   8.08 MB |        1.02 |
| HashingWithStopWords | 200       |  6.312 ms | 0.4312 ms | 0.0236 ms |  0.85 |    0.01 |  265.6250 |  93.7500 |        - |   6.52 MB |        0.82 |
|                      |           |           |           |           |       |         |           |          |          |           |             |
| **Count**                | **1000**      | **30.384 ms** | **2.7512 ms** | **0.1508 ms** |  **1.00** |    **0.01** | **1781.2500** | **812.5000** | **593.7500** |  **38.64 MB** |        **1.00** |
| CountWithStopWords   | 1000      | 23.523 ms | 2.7820 ms | 0.1525 ms |  0.77 |    0.01 | 1406.2500 | 562.5000 | 250.0000 |  30.99 MB |        0.80 |
| Hashing              | 1000      | 29.813 ms | 8.9395 ms | 0.4900 ms |  0.98 |    0.01 | 1968.7500 | 593.7500 | 593.7500 |  39.55 MB |        1.02 |
| HashingWithStopWords | 1000      | 24.956 ms | 0.5092 ms | 0.0279 ms |  0.82 |    0.00 | 1343.7500 | 343.7500 | 250.0000 |  31.83 MB |        0.82 |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorMathBenchmarks-report-github

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
| **Dot**    | **384**  |  **51.60 ns** | **0.594 ns** | **0.033 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 384  |  51.98 ns | 0.148 ns | 0.008 ns |  1.01 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **768**  | **103.66 ns** | **4.006 ns** | **0.220 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 768  | 100.40 ns | 0.532 ns | 0.029 ns |  0.97 |         - |          NA |
|        |      |           |          |          |       |           |             |
| **Dot**    | **1024** | **140.35 ns** | **0.622 ns** | **0.034 ns** |  **1.00** |         **-** |          **NA** |
| L2Norm | 1024 | 136.45 ns | 1.340 ns | 0.073 ns |  0.97 |         - |          NA |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.VectorizerBenchmarks-report-github

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
| **Count**        | **200**       |  **3.209 ms** | **5.6100 ms** | **0.3075 ms** |  **1.01** |    **0.12** |  **62.5000** |  **23.4375** |        **-** |    **1.6 MB** |        **1.00** |
| Tfidf        | 200       |  2.917 ms | 1.3934 ms | 0.0764 ms |  0.91 |    0.08 |  62.5000 |  23.4375 |        - |   1.63 MB |        1.02 |
| CountBigrams | 200       |  3.746 ms | 0.2016 ms | 0.0110 ms |  1.17 |    0.10 | 109.3750 |  62.5000 |        - |   2.78 MB |        1.74 |
| Hashing      | 200       |  2.822 ms | 0.1020 ms | 0.0056 ms |  0.89 |    0.08 |  66.4063 |  31.2500 |        - |    1.6 MB |        1.00 |
|              |           |           |           |           |       |         |          |          |          |           |             |
| **Count**        | **1000**      |  **6.975 ms** | **0.7841 ms** | **0.0430 ms** |  **1.00** |    **0.01** | **343.7500** | **203.1250** |  **62.5000** |   **7.83 MB** |        **1.00** |
| Tfidf        | 1000      |  7.215 ms | 0.6305 ms | 0.0346 ms |  1.03 |    0.01 | 343.7500 | 218.7500 |  93.7500 |   7.97 MB |        1.02 |
| CountBigrams | 1000      | 11.572 ms | 2.5574 ms | 0.1402 ms |  1.66 |    0.02 | 640.6250 | 265.6250 | 265.6250 |  13.42 MB |        1.71 |
| Hashing      | 1000      |  7.013 ms | 0.6580 ms | 0.0361 ms |  1.01 |    0.01 | 351.5625 | 140.6250 |  70.3125 |   7.85 MB |        1.00 |

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
| 8 | 102.0 | 21.8 | 4.68x C# faster |
| 32 | 148.3 | 91.4 | 1.62x C# faster |
| 128 | 470.0 | 942.5 | 2.01x Py faster |
| 512 | 4752.7 | 11020.5 | 2.32x Py faster |

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
| 8 | 131.5 | 19.0 | 6.92x C# faster |
| 32 | 230.2 | 157.3 | 1.46x C# faster |
| 128 | 1818.5 | 1311.9 | 1.39x C# faster |
| 512 | 15183.0 | 15016.7 | 1.01x C# faster |

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
| confusion_matrix_n1000_k2 | 0.007 | 0.808 | 107.79x | 0.007 | 0.808 | 107.80x |
| accuracy_n1000_k2 | 0.001 | 0.429 | 375.83x | 0.001 | 0.429 | 375.81x |
| precision_recall_f1_macro_n1000_k2 | 0.007 | 1.471 | 220.37x | 0.007 | 1.471 | 220.36x |
| classification_report_n1000_k2 | 0.010 | 5.678 | 588.65x | 0.010 | 5.677 | 588.59x |
| roc_auc_binary_n1000_k2 | 0.015 | 1.651 | 108.06x | 0.015 | 1.651 | 108.05x |
| balanced_accuracy_n1000_k2 | 0.007 | 0.880 | 134.22x | 0.007 | 0.879 | 134.21x |
| matthews_n1000_k2 | 0.007 | 1.671 | 254.23x | 0.007 | 1.671 | 254.20x |
| cohen_kappa_n1000_k2 | 0.007 | 0.932 | 142.26x | 0.007 | 0.932 | 142.25x |
| mse_n1000_k2 | 0.003 | 0.236 | 88.08x | 0.003 | 0.236 | 88.07x |
| mae_n1000_k2 | 0.003 | 0.235 | 86.55x | 0.003 | 0.235 | 86.55x |
| median_ae_n1000_k2 | 0.007 | 0.250 | 38.29x | 0.007 | 0.250 | 38.29x |
| r2_n1000_k2 | 0.003 | 0.287 | 83.88x | 0.003 | 0.287 | 83.89x |
| confusion_matrix_n1000_k10 | 0.008 | 0.829 | 108.39x | 0.008 | 0.829 | 108.38x |
| accuracy_n1000_k10 | 0.001 | 0.437 | 383.45x | 0.001 | 0.437 | 383.44x |
| precision_recall_f1_macro_n1000_k10 | 0.007 | 1.530 | 214.59x | 0.007 | 1.530 | 214.57x |
| classification_report_n1000_k10 | 0.014 | 5.902 | 416.34x | 0.014 | 5.902 | 416.32x |
| roc_auc_ovr_macro_n1000_k10 | 0.521 | 8.445 | 16.21x | 0.521 | 8.444 | 16.21x |
| balanced_accuracy_n1000_k10 | 0.007 | 0.896 | 128.11x | 0.007 | 0.895 | 128.10x |
| matthews_n1000_k10 | 0.007 | 1.720 | 246.78x | 0.007 | 1.720 | 246.78x |
| cohen_kappa_n1000_k10 | 0.007 | 0.942 | 128.10x | 0.007 | 0.942 | 128.09x |
| mse_n1000_k10 | 0.003 | 0.235 | 87.94x | 0.003 | 0.235 | 87.93x |
| mae_n1000_k10 | 0.003 | 0.235 | 86.39x | 0.003 | 0.235 | 86.39x |
| median_ae_n1000_k10 | 0.006 | 0.250 | 38.52x | 0.006 | 0.250 | 38.53x |
| r2_n1000_k10 | 0.003 | 0.285 | 83.36x | 0.003 | 0.285 | 83.36x |
| confusion_matrix_n100000_k2 | 0.833 | 10.895 | 13.08x | 0.833 | 10.895 | 13.08x |
| accuracy_n100000_k2 | 0.169 | 3.757 | 22.25x | 0.169 | 3.756 | 22.24x |
| precision_recall_f1_macro_n100000_k2 | 0.745 | 12.347 | 16.58x | 0.745 | 12.346 | 16.57x |
| classification_report_n100000_k2 | 0.752 | 26.702 | 35.50x | 0.752 | 26.699 | 35.50x |
| roc_auc_binary_n100000_k2 | 4.119 | 25.297 | 6.14x | 4.119 | 25.294 | 6.14x |
| balanced_accuracy_n100000_k2 | 0.749 | 10.909 | 14.57x | 0.749 | 10.909 | 14.57x |
| matthews_n100000_k2 | 0.748 | 21.809 | 29.15x | 0.748 | 21.808 | 29.15x |
| cohen_kappa_n100000_k2 | 0.743 | 10.959 | 14.74x | 0.743 | 10.958 | 14.74x |
| mse_n100000_k2 | 0.286 | 0.532 | 1.86x | 0.286 | 0.532 | 1.86x |
| mae_n100000_k2 | 0.284 | 0.524 | 1.85x | 0.284 | 0.524 | 1.85x |
| median_ae_n100000_k2 | 0.748 | 1.688 | 2.26x | 0.761 | 1.688 | 2.22x |
| r2_n100000_k2 | 0.334 | 0.869 | 2.60x | 0.334 | 0.869 | 2.60x |
| confusion_matrix_n100000_k10 | 0.848 | 10.862 | 12.80x | 0.848 | 10.859 | 12.80x |
| accuracy_n100000_k10 | 0.261 | 3.764 | 14.44x | 0.261 | 3.764 | 14.44x |
| precision_recall_f1_macro_n100000_k10 | 0.862 | 13.440 | 15.58x | 0.862 | 13.439 | 15.58x |
| classification_report_n100000_k10 | 0.864 | 30.021 | 34.74x | 0.864 | 30.020 | 34.74x |
| roc_auc_ovr_macro_n100000_k10 | 46.578 | 216.383 | 4.65x | 46.581 | 216.367 | 4.64x |
| balanced_accuracy_n100000_k10 | 0.853 | 10.981 | 12.87x | 0.853 | 10.980 | 12.87x |
| matthews_n100000_k10 | 0.849 | 22.609 | 26.62x | 0.849 | 22.607 | 26.62x |
| cohen_kappa_n100000_k10 | 1.006 | 11.012 | 10.95x | 1.005 | 11.011 | 10.95x |
| mse_n100000_k10 | 0.285 | 0.531 | 1.86x | 0.285 | 0.531 | 1.86x |
| mae_n100000_k10 | 0.284 | 0.525 | 1.85x | 0.284 | 0.525 | 1.85x |
| median_ae_n100000_k10 | 0.809 | 1.687 | 2.08x | 0.875 | 1.687 | 1.93x |
| r2_n100000_k10 | 0.335 | 0.868 | 2.59x | 0.335 | 0.868 | 2.59x |
| confusion_matrix_n1000000_k2 | 7.475 | 102.971 | 13.77x | 7.475 | 102.954 | 13.77x |
| accuracy_n1000000_k2 | 1.843 | 33.942 | 18.42x | 1.843 | 33.942 | 18.42x |
| precision_recall_f1_macro_n1000000_k2 | 7.532 | 110.905 | 14.73x | 7.531 | 110.901 | 14.73x |
| classification_report_n1000000_k2 | 7.457 | 217.806 | 29.21x | 7.457 | 217.796 | 29.21x |
| roc_auc_binary_n1000000_k2 | 66.889 | 283.063 | 4.23x | 66.880 | 283.017 | 4.23x |
| balanced_accuracy_n1000000_k2 | 7.430 | 102.078 | 13.74x | 7.430 | 102.066 | 13.74x |
| matthews_n1000000_k2 | 7.430 | 206.836 | 27.84x | 7.430 | 206.816 | 27.84x |
| cohen_kappa_n1000000_k2 | 7.460 | 102.506 | 13.74x | 7.459 | 102.497 | 13.74x |
| mse_n1000000_k2 | 2.856 | 2.908 | 1.02x | 2.856 | 2.908 | 1.02x |
| mae_n1000000_k2 | 2.824 | 2.862 | 1.01x | 2.824 | 2.862 | 1.01x |
| median_ae_n1000000_k2 | 7.598 | 13.671 | 1.80x | 7.639 | 13.670 | 1.79x |
| r2_n1000000_k2 | 3.317 | 5.656 | 1.71x | 3.317 | 5.656 | 1.71x |
| confusion_matrix_n1000000_k10 | 8.540 | 101.986 | 11.94x | 8.540 | 101.954 | 11.94x |
| accuracy_n1000000_k10 | 2.802 | 33.683 | 12.02x | 2.802 | 33.670 | 12.02x |
| precision_recall_f1_macro_n1000000_k10 | 8.637 | 116.341 | 13.47x | 8.636 | 116.333 | 13.47x |
| classification_report_n1000000_k10 | 8.550 | 238.555 | 27.90x | 8.550 | 238.547 | 27.90x |
| balanced_accuracy_n1000000_k10 | 8.562 | 102.199 | 11.94x | 8.561 | 102.192 | 11.94x |
| matthews_n1000000_k10 | 8.550 | 212.973 | 24.91x | 8.549 | 212.958 | 24.91x |
| cohen_kappa_n1000000_k10 | 8.534 | 102.997 | 12.07x | 8.534 | 102.988 | 12.07x |
| mse_n1000000_k10 | 2.862 | 3.044 | 1.06x | 2.862 | 3.044 | 1.06x |
| mae_n1000000_k10 | 2.837 | 2.930 | 1.03x | 2.837 | 2.930 | 1.03x |
| median_ae_n1000000_k10 | 7.392 | 13.935 | 1.89x | 7.429 | 13.934 | 1.88x |
| r2_n1000000_k10 | 3.354 | 5.955 | 1.78x | 3.354 | 5.954 | 1.78x |

ratio > 1 means Lodestar is faster. cpu is the merge gate for this branch
(docs/guides/performance.md): every operation, every size, must be >= 1x.

<!-- markdownlint-enable MD060 -->

### compare-persistence

```text
Python: {'tokenizers': '0.23.1', 'sentencepiece': '0.2.2', 'scikit-learn': '1.9.0', 'numpy': '2.5.1'} (py 3.12.14)
C#:     Lodestar on .NET 10.0.11
```

<!-- markdownlint-disable MD060 -->

| operation | C# ms | Py ms | wall | C# cpu | Py cpu | cpu |
|:---|---:|---:|---:|---:|---:|---:|
| vocab_txt | 3.768 | 10.645 | 2.82x | 4.002 | 10.644 | 2.66x |
| tokenizer_json_wordpiece | 9.369 | 15.784 | 1.68x | 9.787 | 15.783 | 1.61x |
| tokenizer_json_unigram | 11.181 | 40.686 | 3.64x | 12.022 | 40.681 | 3.38x |
| spiece_model | 3.758 | 28.498 | 7.58x | 3.916 | 28.468 | 7.27x |
| tfidf_save | 1.927 | 2.542 | 1.32x | 1.945 | 2.541 | 1.31x |
| tfidf_load | 4.066 | 3.838 | 0.94x | 4.289 | 3.838 | 0.89x |
| embedding_index_save | 10.261 | 5.792 | 0.56x | 10.845 | 5.791 | 0.53x |
| embedding_index_load | 8.867 | 1.503 | 0.17x | 9.718 | 1.501 | 0.15x |
| embedding_index_load_file | 9.707 | 1.113 | 0.11x | 10.555 | 1.113 | 0.11x |

ratio > 1 means Lodestar is faster. cpu is the honest one: elapsed time
hides work .NET does on background GC threads; CPython is single-threaded.

<!-- markdownlint-enable MD060 -->
