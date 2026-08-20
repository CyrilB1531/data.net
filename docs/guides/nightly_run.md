# Nightly benchmark run

<!-- nightly-baseline: 6cadb6a76b4e172c36be7cdb3f541d742e24373a -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `6cadb6a76b4e172c36be7cdb3f541d742e24373a`
- Previous run: `6cadb6a76b4e172c36be7cdb3f541d742e24373a`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
- `BpeBenchmarks`
- `BpeScalingBenchmarks`
- `PersistenceBenchmarks`

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

| Method             | CorpusSize | Mean         | Error       | StdDev    | Ratio | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------- |----------- |-------------:|------------:|----------:|------:|---------:|--------:|-----------:|------------:|
| **UnitLoop**           | **1**          |     **6.147 μs** |   **0.4325 μs** | **0.0237 μs** |  **1.00** |   **0.1678** |       **-** |    **2.76 KB** |        **1.00** |
| EmbedBatch         | 1          |     6.426 μs |   1.1243 μs | 0.0616 μs |  1.05 |   0.1907 |       - |    3.16 KB |        1.14 |
| EmbedBatchBucketed | 1          |     6.512 μs |   0.3421 μs | 0.0187 μs |  1.06 |   0.1907 |       - |    3.16 KB |        1.14 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **8**          |   **111.178 μs** |   **1.2248 μs** | **0.0671 μs** |  **1.00** |   **8.1787** |  **0.2441** |  **134.29 KB** |        **1.00** |
| EmbedBatch         | 8          |    74.442 μs |  14.5132 μs | 0.7955 μs |  0.67 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
| EmbedBatchBucketed | 8          |    73.575 μs |   5.0715 μs | 0.2780 μs |  0.66 |   7.6904 |  0.3662 |  127.31 KB |        0.95 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **32**         |   **412.764 μs** |  **26.4858 μs** | **1.4518 μs** |  **1.00** |  **28.3203** |  **0.9766** |  **468.71 KB** |        **1.00** |
| EmbedBatch         | 32         |   257.827 μs |  59.9502 μs | 3.2861 μs |  0.62 |  26.8555 |  1.4648 |  441.32 KB |        0.94 |
| EmbedBatchBucketed | 32         |   250.795 μs |  24.6911 μs | 1.3534 μs |  0.61 |  25.8789 |  1.4648 |   427.8 KB |        0.91 |
|                    |            |              |             |           |       |          |         |            |             |
| **UnitLoop**           | **128**        | **1,662.869 μs** | **140.3652 μs** | **7.6939 μs** |  **1.00** | **113.2813** |  **5.8594** | **1874.78 KB** |        **1.00** |
| EmbedBatch         | 128        | 1,023.228 μs |  71.7800 μs | 3.9345 μs |  0.62 | 107.4219 | 15.6250 | 1764.42 KB |        0.94 |
| EmbedBatchBucketed | 128        |   946.209 μs |  47.8914 μs | 2.6251 μs |  0.57 | 103.5156 | 15.6250 |  1696.9 KB |        0.91 |

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
| Unigram | 607.0 ms | 49.79 ms | 2.73 ms |  1.00 | 32000.0000 | 519.51 MB |        1.00 |
| Bpe     | 559.4 ms | 46.95 ms | 2.57 ms |  0.92 |  7000.0000 | 112.18 MB |        0.22 |

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

| Method                    | Length | Mean     | Error    | StdDev  | Gen0   | Allocated |
|-------------------------- |------- |---------:|---------:|--------:|-------:|----------:|
| **BpeOnOnePathologicalToken** | **512**    | **103.7 μs** |  **2.49 μs** | **0.14 μs** | **1.2207** |  **20.38 KB** |
| **BpeOnOnePathologicalToken** | **1024**   | **221.4 μs** | **32.67 μs** | **1.79 μs** | **2.4414** |  **39.93 KB** |
| **BpeOnOnePathologicalToken** | **2048**   | **483.9 μs** | **27.12 μs** | **1.49 μs** | **3.9063** |  **78.98 KB** |
| **BpeOnOnePathologicalToken** | **4096**   | **987.9 μs** | **41.70 μs** | **2.29 μs** | **7.8125** | **157.03 KB** |

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
| VocabTxt               |  4.408 ms | 2.4360 ms | 0.1335 ms | 117.1875 | 109.3750 |  39.0625 |   3.62 MB |
| TokenizerJsonWordPiece | 11.636 ms | 2.3406 ms | 0.1283 ms | 187.5000 | 171.8750 |  46.8750 |   5.72 MB |
| TokenizerJsonUnigram   | 12.475 ms | 0.9455 ms | 0.0518 ms |  93.7500 |  78.1250 |  31.2500 |   4.64 MB |
| SpieceModel            |  3.898 ms | 4.2486 ms | 0.2329 ms | 109.3750 | 101.5625 |  31.2500 |   3.36 MB |
| TfidfSave              |  1.907 ms | 0.2954 ms | 0.0162 ms |  31.2500 |  25.3906 |  25.3906 |   2.09 MB |
| TfidfLoad              |  4.436 ms | 0.5713 ms | 0.0313 ms |  85.9375 |  78.1250 |  23.4375 |   2.86 MB |
| EmbeddingIndexSave     |  6.866 ms | 0.8194 ms | 0.0449 ms | 476.5625 | 476.5625 | 476.5625 |  39.64 MB |
| EmbeddingIndexLoad     |  6.026 ms | 1.6341 ms | 0.0896 ms | 531.2500 | 500.0000 | 468.7500 |  35.35 MB |

<!-- markdownlint-enable MD060 -->
