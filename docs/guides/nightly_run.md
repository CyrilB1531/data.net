# Nightly benchmark run

<!-- nightly-baseline: 65c60215bbd2020a40d02b0bc6f196c29d47d58a -->

> **Generated. Do not edit.** Produced by `.github/workflows/bench-nightly.yml`; every edit is
> overwritten by the next run. The curated figures, measured on a named machine, are in
> [performance](performance). The last known reading for a method quiet tonight is in
> [benchmark_latest](benchmark_latest).

**Read the ratios, not the means.** These run on a GitHub hosted runner: a shared VM whose
hardware differs from night to night and whose neighbours are unknown. An absolute figure here
is not comparable to the performance page, and not reliably comparable to yesterday's. A ratio
against a baseline measured in the same run, on the same VM, in the same minute, is.

## This run

- Commit: `65c60215bbd2020a40d02b0bc6f196c29d47d58a`
- Previous run: `65c60215bbd2020a40d02b0bc6f196c29d47d58a`
- Runner: Linux / X64 (GitHub hosted)

## Classes re-run

Selected by `tools/select_benchmarks.py` from the sources that changed since the previous run:

- `BatchEmbeddingBenchmarks`
- `BpeBenchmarks`
- `BpeScalingBenchmarks`
- `PersistenceBenchmarks`
- `TokenizerIncumbentBenchmarks`
- `VectorMathBenchmarks`

### Lodestar.Text.Benchmarks.BatchEmbeddingBenchmarks-report-github

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

### Lodestar.Text.Benchmarks.BpeBenchmarks-report-github

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

### Lodestar.Text.Benchmarks.PersistenceBenchmarks-report-github

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

### Lodestar.Text.Benchmarks.TokenizerIncumbentBenchmarks-report-github

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

## Against rapidfuzz, in this same run

Both sides on this VM in these minutes, which is what makes the ratio readable where the absolutes are not.

- `persistence`

### compare-persistence

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
