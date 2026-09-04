# Nightly benchmark run

<!-- nightly-baseline: a7a9382eb7cd998f2b635d32314a57766e27d971 -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `a7a9382eb7cd998f2b635d32314a57766e27d971`
- Previous run: `a7a9382eb7cd998f2b635d32314a57766e27d971`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
- `BpeBenchmarks`
- `BpeScalingBenchmarks`
- `PersistenceBenchmarks`
- `TokenizerIncumbentBenchmarks`

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

| Method             | CorpusSize | Mean         | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|-----------:|------:|--------:|--------:|-------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **5.900 μs** |   **1.1794 μs** |  **0.0646 μs** |  **1.00** |    **0.01** |  **0.1526** |      **-** |     **2.6 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.090 μs |   0.8694 μs |  0.0477 μs |  1.03 |    0.01 |  0.1831 |      - |       3 KB |        1.15 |
| EmbedBatchBucketed | 1          |     6.360 μs |   2.8839 μs |  0.1581 μs |  1.08 |    0.03 |  0.1831 |      - |       3 KB |        1.15 |
|                    |            |              |             |            |       |         |         |        |            |             |
| **UnitLoop**           | **8**          |    **92.845 μs** |   **0.6689 μs** |  **0.0367 μs** |  **1.00** |    **0.00** |  **5.7373** | **0.1221** |   **94.76 KB** |        **1.00** |
| EmbedBatch         | 8          |    56.393 μs |   8.8313 μs |  0.4841 μs |  0.61 |    0.00 |  5.3711 | 0.2441 |   87.78 KB |        0.93 |
| EmbedBatchBucketed | 8          |    54.282 μs |   9.7518 μs |  0.5345 μs |  0.58 |    0.00 |  5.3711 | 0.2441 |   87.78 KB |        0.93 |
|                    |            |              |             |            |       |         |         |        |            |             |
| **UnitLoop**           | **32**         |   **346.652 μs** |  **74.3435 μs** |  **4.0750 μs** |  **1.00** |    **0.01** | **20.0195** | **0.4883** |  **334.02 KB** |        **1.00** |
| EmbedBatch         | 32         |   198.902 μs |  38.6985 μs |  2.1212 μs |  0.57 |    0.01 | 18.5547 | 1.2207 |  306.63 KB |        0.92 |
| EmbedBatchBucketed | 32         |   181.785 μs |  12.3097 μs |  0.6747 μs |  0.52 |    0.01 | 17.8223 | 0.9766 |  293.12 KB |        0.88 |
|                    |            |              |             |            |       |         |         |        |            |             |
| **UnitLoop**           | **128**        | **1,378.246 μs** | **210.3567 μs** | **11.5304 μs** |  **1.00** |    **0.01** | **80.0781** | **3.9063** | **1336.03 KB** |        **1.00** |
| EmbedBatch         | 128        |   787.776 μs |  28.1197 μs |  1.5413 μs |  0.57 |    0.00 | 74.2188 | 9.7656 | 1225.67 KB |        0.92 |
| EmbedBatchBucketed | 128        |   720.931 μs |  53.1760 μs |  2.9148 μs |  0.52 |    0.00 | 70.3125 | 9.7656 | 1158.15 KB |        0.87 |

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

| Method  | Mean     | Error    | StdDev  | Ratio | Gen0      | Allocated | Alloc Ratio |
|-------- |---------:|---------:|--------:|------:|----------:|----------:|------------:|
| Unigram | 313.9 ms |  3.86 ms | 0.21 ms |  1.00 | 1500.0000 |  30.32 MB |        1.00 |
| Bpe     | 538.1 ms | 67.98 ms | 3.73 ms |  1.71 | 7000.0000 | 112.18 MB |        3.70 |

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

| Method                    | Length | Mean     | Error    | StdDev  | Gen0   | Gen1   | Allocated |
|-------------------------- |------- |---------:|---------:|--------:|-------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    | **102.8 μs** |  **2.89 μs** | **0.16 μs** | **1.2207** |      **-** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **227.0 μs** | **31.31 μs** | **1.72 μs** | **2.4414** |      **-** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **479.3 μs** | **10.41 μs** | **0.57 μs** | **4.3945** |      **-** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **974.4 μs** | **13.15 μs** | **0.72 μs** | **8.7891** | **0.9766** | **157.03 KB** |

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
| VocabTxt               |  4.378 ms | 2.1995 ms | 0.1206 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.069 ms | 2.3548 ms | 0.1291 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.423 ms | 0.9545 ms | 0.0523 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.783 ms | 3.0369 ms | 0.1665 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.871 ms | 0.3543 ms | 0.0194 ms |  29.2969 |  23.4375 |  23.4375 |   2.09 MB |
| TfidfLoad              |  4.371 ms | 0.6614 ms | 0.0363 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  4.092 ms | 0.6556 ms | 0.0359 ms | 273.4375 | 273.4375 | 273.4375 |  19.87 MB |
| EmbeddingIndexLoad     |  5.658 ms | 0.8578 ms | 0.0470 ms | 203.1250 | 171.8750 | 140.6250 |  15.72 MB |

<!-- markdownlint-enable MD060 -->

### Lodestar.Text.Benchmarks.TokenizerIncumbentBenchmarks-report-github

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

| Method       | Model         | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0      | Gen1     | Allocated | Alloc Ratio |
|------------- |-------------- |----------:|----------:|---------:|------:|--------:|----------:|---------:|----------:|------------:|
| **Lodestar**     | **WordPiece**     |  **65.56 ms** | **26.671 ms** | **1.462 ms** |  **1.00** |    **0.03** | **4250.0000** | **125.0000** |  **68.25 MB** |        **1.00** |
| MlTokenizers | WordPiece     |  56.29 ms | 27.372 ms | 1.500 ms |  0.86 |    0.03 |  200.0000 |        - |   3.55 MB |        0.05 |
|              |               |           |           |          |       |         |           |          |           |             |
| **Lodestar**     | **SentencePiece** | **348.51 ms** | **18.455 ms** | **1.012 ms** |  **1.00** |    **0.00** | **1000.0000** |        **-** |  **30.33 MB** |        **1.00** |
| MlTokenizers | SentencePiece |  50.98 ms |  1.597 ms | 0.088 ms |  0.15 |    0.00 |  100.0000 |        - |   3.09 MB |        0.10 |

<!-- markdownlint-enable MD060 -->
