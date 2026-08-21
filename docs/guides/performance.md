# Performance

Performance is the selling point against Python, so it is measured from Lot 1 with
[BenchmarkDotNet](https://benchmarkdotnet.org/), not estimated.

## Reproduce

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- --filter '*Levenshtein*'
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- --filter '*VectorizerBenchmarks*' '*FuzzBenchmarks*'
```

## Principles applied

- **`ReadOnlySpan<char>` everywhere.** Inputs are never copied; `string` literals
  convert with no allocation.
- **`ArrayPool<int>` for the dynamic-programming matrices.** The DP row is rented
  then returned: **zero managed allocation per call**, so no GC pressure even
  under heavy load.
- **Rolling DP row on the shorter operand** → `O(min(n, m))` memory.
- **Common prefix/suffix trimming** → collapses the DP band on near-equal inputs
  (the common case in record matching).

## Levenshtein — indicative numbers

Short-job measurement (reduced iterations: means are noisy, but the allocation
column is reliable). Re-run with a full job before quoting.

| Method | Length | Mean | Allocated |
| --- | --- | ---: | ---: |
| `Distance` (UTF-16) | 8 | ~35 ns | **0 B** |
| `Distance` (code point) | 8 | ~208 ns | **0 B** |
| `Distance` (UTF-16) | 64 | ~7.0 µs | **0 B** |
| `Distance` (UTF-16) | 512 | ~21 µs | **0 B** |

**Zero allocation** at every size is the structural result. On very short inputs
the code-point mode costs ~5× the UTF-16 mode (the decode pass dominates when the
computation itself is tiny); from 64+ characters the gap closes. Hence the choice:
**UTF-16 by default**, `CodePoint` on demand.

## Compared to Python (rapidfuzz) — Levenshtein

Cross-language bench with **identical methodology on both sides** (same committed
ASCII corpus, ns/pair throughput, auto-scaling, best-of-5). See
[`bench/README.md`](https://github.com/CyrilB1531/lodestar/blob/main/bench/README.md).

Indicative measurement (rapidfuzz 3.14.5 / Python 3.12; Lodestar.Text / .NET 10 on
an Intel i7-4770S; dev machine — non-authoritative), **after** adding the blocked
(multi-word) Myers fast path:

| Length | Python (rapidfuzz) | C# (Lodestar.Text) | Ratio | C# path |
| ---: | ---: | ---: | --- | --- |
| 8 | 183 ns/pair | **36 ns/pair** | **5.1× C# faster** | DP |
| 32 | **324 ns/pair** | 453 ns/pair | 1.4× Python | Myers (single word) |
| 128 | 2 693 ns/pair | **1 777 ns/pair** | **1.5× C# faster** | Myers (blocked) |
| 512 | 21 688 ns/pair | **20 555 ns/pair** | **1.06× C# faster** | Myers (blocked) |

- **Short strings (≤ ~40)** — the typical name/identifier matching case: C# is
  ahead, largely because rapidfuzz pays per-call interop overhead there.
- **Long strings** — previously 13–31× *behind* rapidfuzz, because patterns over
  64 characters fell back to the DP. Blocked Myers closed that: the 512 bucket
  went from 684 µs to 21 µs, a 33× improvement, and now edges ahead. It was never
  a language problem, only an algorithmic one.
- **The length-32 bucket was the remaining gap**, at 1.4× behind. It is closed,
  and the cause was not the one this line assumed — see the window below.
- **Scope.** The figures above are the **UTF-16 mode**, whose bit-parallel path
  requires a Latin-1 pattern: outside that — CJK, emoji — `Distance` still uses
  the DP, and the table does not describe those inputs. The **code-point mode**
  no longer shares that restriction (#208); its own measurement is below. What
  remains unresolved is the UTF-16 path's equality table; see
  [`../decisions/0004-levenshtein-myers-backlog.md`](../decisions/0004-levenshtein-myers-backlog.md).

### The length-32 bucket, which was never the kernel (#208)

Intel i7-4770S, .NET 10.0.10, rapidfuzz 3.14.5 / Python 3.12.3; dev machine,
non-authoritative. The issue that opened this said the gap was "inside the
kernel, not in the dispatch". It was the dispatch, and the first measurement is
the one that says so: `BucketRouteDiagnostics` splits the committed length-32
bucket on the dispatch's own criterion — the shorter operand after `Affixes.Trim`,
which is the pattern Myers is handed — and times each half.

| route at the gate of 16 | pairs | median pattern | total | per pair |
| --- | ---: | ---: | ---: | ---: |
| DP (pattern < 16) | 474 | 10 | 308.3 µs | **650.4 ns** |
| Myers (pattern ≥ 16) | 526 | 22 | 134.6 µs | **255.9 ns** |

The two sum to 442.9 ns/pair against the harness's 440.6, so the split accounts
for the bucket. **47% of the pairs were carrying 70% of its cost**, and they were
the ones on the *smaller* problem: the DP was 2.5× slower than Myers on a band a
quarter the size. Nothing was wrong inside either kernel — the gate was simply
far above where the two curves actually cross.

A constant the dispatch consults cannot be swept from inside the dispatch, so it
was swept directly, rebuilding between points and reading the committed corpus
end to end. Single runs, ns/pair:

| gate | Levenshtein len 8 | Levenshtein len 32 | Indel len 8 | Indel len 32 |
| ---: | ---: | ---: | ---: | ---: |
| 16 | 31.2 | 451.7 | 37.4 | 316.4 |
| 12 | 31.4 | 311.2 | 37.2 | 217.5 |
| 10 | 31.9 | 266.0 | 37.2 | 189.8 |
| 8 | 32.0 | **239.4** | 37.9 | **177.6** |
| 6 | 31.8 | 232.4 | 37.4 | 170.0 |
| 4 | 31.6 | 233.0 | 37.6 | 164.8 |
| 2 | 31.2 | 233.1 | 37.0 | 166.9 |
| 1 | **51.2** | 236.6 | — | — |

- **8, and not lower, is a choice about text length rather than pattern length.**
  Myers costs `setup + O(n)` where the DP costs `O(m·n)`, so they cross at
  `m ≈ 1 + setup/n` — which *falls as the text grows*. There is no single right
  gate, only a right one per regime, and the gate sees only `m`. Calibrating on
  the shortest texts is calibrating on the case where being wrong costs the most,
  and the gate-of-1 row is that case: the length-8 bucket loses 64% because 32% of
  its pairs have a pattern of exactly 1 and pay for a 256-entry table to compare
  one character.
- **The corpus cannot resolve 2 to 7.** Its length-8 bucket trims to a pattern of
  0 or 1 and its length-32 bucket is the only one holding patterns in between, so
  every row above between 2 and 6 rests on one bucket. The flat region there is
  worth 6 ns and is not worth the exposure.

The second finding is in both kernels and is independent of the gate: `stackalloc`
zeroes, nothing in the assembly disables `localsinit`, and both single-word
kernels then called `Clear()` on the 256-entry equality table anyway — a second
2 KB memset on a call whose real work is `O(n)`. `Myers` already relied on that
zeroing for its probe table and said so, three constants above the line that
cleared. Removing it is worth 12% of the length-32 bucket on Levenshtein and 17%
on Indel, on top of the gate.

Both changes together, **median of 3, before and after interleaved in one
window** so machine drift lands on both columns:

| Length | Levenshtein before | after | | Indel before | after |
| ---: | ---: | ---: | --- | ---: | ---: |
| 8 | 31.9 ns | 30.7 ns | | 37.7 ns | 36.5 ns |
| 32 | 427.6 ns | **204.8 ns — 2.09×** | | 318.7 ns | **145.6 ns — 2.19×** |
| 128 | 1 722.5 ns | 1 700.8 ns | | 1 193.6 ns | 1 174.3 ns |
| 512 | 19 926.8 ns | 19 267.3 ns | | 13 562.6 ns | 13 388.5 ns |

Every bucket but 32 is within noise, which is the expected shape: 128 and 512
already took the kernel and keep their `Clear()` on the blocked path, and 8 never
reaches the gate at all.

Against rapidfuzz, **both sides re-run in one window** on a machine under load
(one-minute average 11.3 falling to 8.0 across it, so the C# column sits above
the quiet-machine medians above; the ratios are what the shared load makes
comparable):

| Length | rapidfuzz Lev | C# Lev | rapidfuzz Indel | C# Indel |
| ---: | ---: | ---: | ---: | ---: |
| 8 | 188.1 ns | **33.9 ns — 5.55×** | 130.6 ns | **39.2 ns — 3.33×** |
| 32 | 323.9 ns | **221.9 ns — 1.46×** | 206.9 ns | **158.3 ns — 1.31×** |
| 128 | 2 590.6 ns | **1 889.0 ns — 1.37×** | 637.9 ns | 1 342.3 ns — 2.10× behind |
| 512 | 20 863.7 ns | **19 896.4 ns — 1.05×** | 7 335.8 ns | 15 144.0 ns — 2.06× behind |

**The 32 bucket is ahead rather than 1.4× behind**, which is the door this lot
was opened to close. Indel's 128 and 512 are the 2.07×/2.15× the section below
already records; they take the blocked path and this lot did not touch it.

#### The gate is now two constants, because the two paths cross at different bands

`MyersMinPatternLength` was shared with the code-point path, which ADR 0004 flagged
as untested. It is: that path renames both operands through a 512-entry probe table
before the kernel sees them, so it carries the larger fixed cost and crosses later.
`LevenshteinCodePointBenchmarks` at the two candidate gates, `Length = 16` acting as
a control because both gates route it to the kernel:

| Pattern | DP (gate 16) | Kernel (gate 8) | |
| ---: | ---: | ---: | --- |
| 8 | **376.6 ns** | 419.3 ns | DP ahead by 11% |
| 10 | 428.4 ns | 434.1 ns | the tie |
| 12 | 534.9 ns | **482.6 ns** | kernel ahead by 10% |
| 16 | 550.1 ns | 532.4 ns | control — 3%, the noise floor |

So `MyersMinCodePointPatternLength` is 10 and `MyersMinPatternLength` is 8. One
constant would have to give up 11% on one path or the other, and the character
path is the one `fuzz.ratio` and every `process.extract` run.

## Compared to Python (rapidfuzz) — Indel, and therefore `fuzz.ratio` (#273)

`Indel` is `len(a) + len(b) - 2·LCS`, so what runs is
[`Lcs.SubsequenceLength`](../reference/text/distances/lcs-subsequencelength.md) —
and that is also what `fuzz.ratio`, every `process.extract` and every blocking
deduplication pass runs. Same corpus and same methodology as the Levenshtein
table above, on the same machine (Intel i7-4770S, .NET 10.0.10, rapidfuzz 3.14.5
/ Python 3.12), one-minute load average 5.2 to 5.9 across the window; dev
machine, non-authoritative, and comparable to the Levenshtein figures because it
was taken under the same conditions rather than an ideal one.

Three states, each measured in its own window with Levenshtein re-run beside it
as a control:

| Length | rapidfuzz | before | + trimming | + kernel | total |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 8 | 132 ns | 222 ns | 30 ns | 37 ns | 6.0× |
| 32 | 205 ns | 3 775 ns | 1 084 ns | **326 ns** | **11.6×** |
| 128 | 590 ns | 58 066 ns | 29 996 ns | **1 252 ns** | **46.4×** |
| 512 | 6 582 ns | 627 297 ns | 569 521 ns | **14 363 ns** | **43.7×** |

Against rapidfuzz that moves the 128 and 512 buckets from **95–98× behind** to
**2.07× and 2.15× behind**, the 32 bucket from 18.4× to 1.57×, and leaves the 8
bucket 3.66× ahead.

- **Trimming came first and is not the kernel.**
  [`Levenshtein.Distance`](../reference/text/distances/levenshtein-distance.md) already
  stripped the common prefix and suffix; `Lcs` did not. On a corpus that is a
  string and a copy with one mutation per ten, that is worth 7.5× at length 8 —
  where one position differs, so seven characters of eight never reach the band —
  and 1.10× at 512, where fifty-one scattered mutations leave almost nothing to
  strip. **It is a best case for record matching and worth nothing on unrelated
  text**, which is the shape of input the corpus does not contain.
- **The kernel is what closes the long buckets**, 46× at 128 and 44× at 512.
- **Still 2× behind at 128 and 512.** *(#320 revisits this: the gap **is**
  algorithmic, and the sentence below was wrong about where it sits — see "The
  blocked LCS kernel" further down.)* The gap is no longer algorithmic; see the
  gate table below for where it now sits.

### Where the bit-parallel gate belongs, measured (#273)

`IndelBenchmarks` cannot answer this — its operands trim down to an accidental
band — so `LcsGateBenchmarks` parameterises the differing middle directly and
runs both routes in one process with the dynamic program as baseline:

| Band | DP | Kernel | Ratio |
| ---: | ---: | ---: | ---: |
| 8 | 161 ns | 182 ns | 1.13 |
| 14 | 347 ns | 402 ns | 1.16 |
| 16 | 425 ns | **149 ns** | **0.35** |
| 32 | 1 707 ns | **185 ns** | **0.11** |
| 64 | 6 926 ns | **264 ns** | **0.04** |
| 96 | 15 836 ns | 1 504 ns | 0.09 |

- **The gate at 16 is right, and probably conservative.** The kernel's floor is
  about 149 ns and the DP already costs 161 ns at band 8, so a lower gate may win
  — but that needs measuring below 8 rather than assuming. **It was, in #208, and
  the guess was right**: the gate is 8 and the length-32 bucket halved. The window
  above has the sweep.
- **The kernel's cost is nearly flat from 16 to 64** — 149, 154, 160, 168, 185,
  234, 264 ns while the work quadruples. That is a fixed cost dominating: the
  256-entry equality table is cleared on every call. **Half of that clearing was
  redundant** and #208 removed it — `stackalloc` had already zeroed the table —
  which is worth 17% of the length-32 bucket here. #301 then stopped zeroing it at
  all for patterns of 32 or fewer, worth a further 13% of that bucket; right-sizing
  the table is still open. Band 96 makes it plain,
  crossing into the blocked path for 281 → 1 504 ns on one and a half times the
  work, the table having doubled. **Right-sizing that table to the pattern's own
  alphabet is where the remaining 2× is most likely to be**, and it is the same
  change ADR 0004 lists as lifting the Latin-1 restriction.
- **The character route carries a fixed 25–60 ns overhead** over the generic one,
  visible below the gate where both take the DP and gone by band 96 where the
  work dwarfs it. It is not a missing inlining — an `AggressiveInlining` attempt
  moved the ratios by less than the error bars — and it is not yet explained.

### Holding the equality table rather than zeroing it (#301)

Same machine and corpus, own window: Intel i7-4770S, .NET 10.0.110, one-minute
load average 3.2 to 4.6 across it; dev machine, non-authoritative.

Both single-word kernels built their 256-entry table with `stackalloc`, which
`localsinit` zeroes on every call — 2 KB of memset for a table of which at most 64
entries are used, on work that is `O(n)`. #208 removed the redundant *second*
memset; this is the one that remained, and no `Clear()` removal reaches it
(`AllowUnsafeBlocks=false` rules out `[SkipLocalsInit]`).

**The prize, sized before anything was built.** A table held in a `[ThreadStatic]`
array and restored by walking the pattern again costs `O(m)` stores rather than
`O(256)`. An isolated harness put the memset it replaces at a flat ~25 ns per call
at every pattern length — the signature of a fixed cost, and consistent with the
flatness across bands above. That harness also said the change won in **both**
kernels, which the corpus then contradicted for one of them: a 64-string working
set never evicts a held table, and a walk over the corpus does.

**Swept over the pair corpus** — longest held pattern at 0 (never held), 16, 32 and
64 — length-32 bucket, ns/pair, best of two runs interleaved across the four builds:

| longest held pattern | 0 | 16 | 32 | 64 |
| --- | ---: | ---: | ---: | ---: |
| `compare-indel` (LCS kernel) | 152.2 | 138.5 | **132.1** | 134.2 |
| `compare` (Myers) | **215.7** | 222.0 | 233.1 | 231.1 |

**So the table is held in one kernel and not in the other**, which is the same
split the two gates already carry and for a related reason. The LCS recurrence is
four operations per text character where Myers' is a dozen, so the identical fixed
cost is a far larger share of what an LCS call does — and the held table's own
costs (a thread-static access, a restore loop, and a table a corpus walk evicts
between calls where a stack frame stays hot) are what Myers cannot cover.

**Before and after**, three runs each, interleaved across the boundary:

| Length | before | after | |
| ---: | ---: | ---: | ---: |
| 8 | 38.0 ns/pair | 37.4 ns/pair | +1.6% |
| 32 | 151.2 ns/pair | **131.6 ns/pair** | **+13.0%** |
| 128 | 1 241 ns/pair | 1 219 ns/pair | +1.8% |
| 512 | 14 166 ns/pair | 14 319 ns/pair | −1.1% |

The 128 and 512 buckets take the blocked path, which this did not touch, so their
movement is the run-to-run spread — as is the 8 bucket, whose pairs trim to a
pattern below the gate. Levenshtein was re-run beside it as the control and moved
by less than that at every bucket: 32.4 → 32.5, 215.6 → 214.6, 1 800 → 1 803.

**The gate was re-swept afterwards and did not move.** A cheaper kernel should be
worth entering at a shorter pattern, and the corpus says so — the length-32 bucket
reads 132.5 ns/pair at a gate of 8, 124.1 at 6 and 120.8 at 4, with the length-8
bucket flat at 37–38 throughout. That is the same relationship #208 measured
(177.6 at 8 against 164.8 at 4) and declined to act on, for a reason this lot does
not change: below 8 the corpus has a hole, the length-8 bucket trims to a pattern
of 0 or 1, and every row therefore rests on the one bucket. `BitParallelMinPatternLength`
stays at 8 until a corpus that can answer it exists.

### CJK and emoji stop falling back to the DP (#302)

The 256-entry equality table is indexed by the character, so a pattern above U+00FF
could not be represented in it and both single-word kernels refused — the tables
above therefore did not describe those inputs. The dense table now keeps the common
characters and an open-addressed side table carries the rare ones, built only when
the pattern holds one, in a method of its own so a Latin-1 pattern is never charged
its 1.25 KB.

**What had to be established first is that the Latin-1 path — `fuzz.ratio`'s — does
not pay for the reach.** Same machine and corpus, four runs per side interleaved,
one-minute load average 6.4 to 8.2; dev machine, non-authoritative. The corpus is
ASCII throughout, so every bucket of it measures exactly that question:

| Length | path | Levenshtein | Indel |
| ---: | --- | ---: | ---: |
| 8 | single-word (changed) | +0.9% | −1.3% |
| 32 | single-word (changed) | +1.2% | −3.2% |
| 128 | blocked (**unchanged**) | −4.9% | −0.1% |
| 512 | blocked (**unchanged**) | −6.4% | −3.5% |

The last two rows are the reading that matters: they run code this change does not
touch, so their movement is the measurement's own noise floor — up to 6.4% under
this load. Every changed bucket moves less than that, which bounds the cost at about
5% and no better.

**A timing bound was the wrong instrument, though, and the exact answer is free.**
The claim being tested — that the Latin-1 callers pass an empty side table, so the
extra test folds away where the kernel is inlined — is a statement about generated
code, not about elapsed time. Read it instead of timing it:

```bash
DOTNET_JitDisasm='SubsequenceLengthChars' DOTNET_TieredCompilation=0 dotnet run -c Release
```

against a driver that runs the Latin-1 single-word path until the JIT settles, on
this branch and on the parent commit. Normalise the addresses and the two listings
are **identical, all 83 instructions**. The Latin-1 path's machine code is unchanged,
so the branch costs exactly nothing there — a claim no machine load can weaken, and
one the corpus timing could only ever have bounded.

The blocked path was widened the same way in #382, so no pattern falls back to the
dynamic program for holding a character above U+00FF.

Both halves are recorded in
[decision 0043](../decisions/0043-the-equality-table-is-sized-to-the-pattern.md),
which amends 0004's two bullets rather than editing that record.

**What the reach is worth is not measured by this corpus**, which is ASCII by
construction. It took a band of its own, below.

### What the wide path buys, and what its side table costs (#383)

Until this section the gain rested on the code-point mode's standing figure for what
falling back costs — 2.80 ms at `Length = 512` against 22 µs — which is an argument
about a different mode, not a measurement of this change. `MyersGateBenchmarks` and
`LcsGateBenchmarks` now run each band twice, over 27 Latin symbols and over 27 CJK
ones, so the alphabet is the only difference between the two readings and both are
taken in one process against the same dynamic program.

Intel i7-4770S, .NET 10.0.11, X64 RyuJIT AVX2, BenchmarkDotNet short job
(`IterationCount=3`, `WarmupCount=3`), `[MemoryDiagnoser]`; dev machine,
non-authoritative. **Three runs per class, all shown**, the first two interleaved
LCS → Myers → LCS → Myers so drift lands on every column, at a one-minute load
average of 1.22 to 4.31. The third is a control taken after the operand
construction moved to a shared base class, on a busier machine — 4.56 to 6.38 —
which is why it reads uniformly slower and why the ratio column, computed inside
each run, is the one to read across the three. Nothing on any row allocates.

| Band | DP | kernel | DP (CJK) | kernel (CJK) | CJK kernel ÷ CJK DP |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 8 | 147 / 160 / 149 ns | 72 / 74 / 70 ns | 149 / 149 / 195 ns | 118 / 140 / 125 ns | 0.79 / 0.94 / 0.64 |
| 12 | 249 / 247 / 294 ns | 75 / 81 / 78 ns | 247 / 263 / 267 ns | 142 / 150 / 156 ns | 0.57 / 0.57 / 0.58 |
| 16 | 422 / 377 / 417 ns | 96 / 87 / 93 ns | 409 / 388 / 410 ns | 147 / 146 / 165 ns | 0.36 / 0.38 / 0.40 |
| 32 | 1 558 / 1 685 / 1 652 ns | 121 / 126 / 132 ns | 1 566 / 1 592 / 1 720 ns | 211 / 239 / 235 ns | 0.13 / 0.15 / 0.14 |
| 64 | 6 245 / 6 614 / 6 671 ns | 188 / 205 / 209 ns | 6 344 / 6 378 / 6 724 ns | 350 / 380 / 389 ns | 0.06 / 0.06 / 0.06 |
| 96 | 13 962 / 15 729 / 16 768 ns | 840 / 861 / 876 ns | 14 752 / 15 079 / 15 571 ns | 1 146 / 1 251 / 1 232 ns | 0.08 / 0.08 / 0.08 |

`LcsGateBenchmarks`, and the edit-distance twin below:

| Band | DP | kernel | DP (CJK) | kernel (CJK) | CJK kernel ÷ CJK DP |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 6 | 127 / 125 / 121 ns | 121 / 122 / 128 ns | 123 / 124 / 122 ns | 119 / 124 / 122 ns | 0.97 / 1.00 / 1.00 |
| 8 | 164 / 168 / 165 ns | 111 / 107 / 106 ns | 166 / 176 / 169 ns | 168 / 162 / 161 ns | 1.02 / 0.92 / 0.95 |
| 10 | 231 / 218 / 240 ns | 113 / 115 / 127 ns | 218 / 228 / 235 ns | 183 / 181 / 194 ns | 0.84 / 0.79 / 0.83 |
| 12 | 290 / 320 / 317 ns | 120 / 148 / 135 ns | 294 / 367 / 291 ns | 202 / 223 / 186 ns | 0.69 / 0.61 / 0.64 |
| 16 | 452 / 464 / 456 ns | 139 / 141 / 138 ns | 449 / 497 / 451 ns | 216 / 233 / 215 ns | 0.48 / 0.47 / 0.48 |
| 32 | 1 905 / 2 030 / 1 700 ns | 209 / 235 / 212 ns | 1 616 / 1 951 / 1 910 ns | 299 / 308 / 301 ns | 0.19 / 0.16 / 0.16 |
| 96 | 20 090 / 20 806 / 19 478 ns | 1 441 / 1 403 / 1 468 ns | 19 502 / 20 866 / 18 742 ns | 1 750 / 1 751 / 1 772 ns | 0.09 / 0.08 / 0.09 |

- **What the refusal cost, measured rather than argued.** A CJK band of 32 takes
  about 1 650 ns on the LCS dynamic program and 230 on the kernel; at 64, 6 500
  against 370. Myers at 96 reads roughly 19 700 against 1 760. Between 7× and 18×
  wherever a band is past the crossing, and that is the whole of what #302 and #382
  buy.
- **The two `DP` columns are the same measurement twice, which is why both are
  here.** The dynamic program compares characters and should not care where in the
  BMP they sit; it does not — the two agree to under 8% on most rows, 31% at worst
  on the shortest band. That spread is this job's noise floor, measured rather than
  asserted, and it is what every reading below has to clear.
- **The side table is not free: 1.6× to 1.9× on the LCS kernel, 1.2× to 1.5× on
  Myers'.** A Latin band of 32 runs the LCS kernel in about 125 ns and a CJK one in
  230. Both are far under the 1 650 ns the DP costs, so the reach is overwhelmingly
  worth having — but the fixed cost roughly doubles, which is the probe and the
  second table, and no earlier section could have said so.
- **The gate of 8 is right for one of the two kernels on this input.** LCS is under
  the DP at band 8 in all three runs (0.79, 0.94, 0.64), so its crossing is at or
  below the gate — though that row is the table's noisiest and cannot size the win.
  Myers is not: three readings of 1.02, 0.92 and 0.95 straddle parity, and it does
  not clearly win until band 10. The constant is shared, was swept over an ASCII
  corpus in #208, and on wide input the two kernels no longer cross at the same
  place. Splitting it is its own change and needs that sweep, not this band.

### The corpus gains a wide half, and the two sides slow down differently (#406)

Every bucket was ASCII until now, so no number on this page described what either
side does above Latin-1. `bench/corpus/pairs.json` carries eight buckets since #406:
the same four lengths drawn from 27 Latin symbols, and four more drawn from 27 CJK
ones. Both alphabets stay inside the BMP on purpose — UTF-16 units and code points
coincide there, so the two sides still measure the same quantity, which a
supplementary character would break.

**The wide buckets reach the kernel, which is checked rather than assumed.**
`BucketRouteDiagnostics` splits a length-32 bucket on the dispatch's own criterion
and reads 833 of 1 000 CJK pairs on the bit-parallel route against 861 Latin. A
bucket whose pairs trimmed below the gate would have measured the dynamic program
under a wide-sounding name.

Intel i7-4770S, .NET 10.0.11, rapidfuzz 3.14.5 / Python 3.12.3; dev machine,
non-authoritative. Each pair of sides back to back, Python first, one-minute load
average 3.91 falling to 1.71 across the window.

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
| --- | ---: | ---: | ---: | :--- |
| latin | 8 | 164.7 | 29.3 | 5.62x C# faster |
| latin | 32 | 291.2 | 189.2 | 1.54x C# faster |
| latin | 128 | 2255.6 | 1778.8 | 1.27x C# faster |
| latin | 512 | 18113.7 | 22308.3 | 1.23x Py faster |
| cjk | 8 | 162.8 | 29.5 | 5.51x C# faster |
| cjk | 32 | 404.9 | 311.1 | 1.30x C# faster |
| cjk | 128 | 3333.7 | 2863.6 | 1.16x C# faster |
| cjk | 512 | 27670.7 | 26315.1 | 1.05x C# faster |

Levenshtein above, Indel below:

| alphabet | length | Python ns/pair | C# ns/pair | speedup (py/C#) |
| --- | ---: | ---: | ---: | :--- |
| latin | 8 | 115.1 | 34.3 | 3.35x C# faster |
| latin | 32 | 183.9 | 118.5 | 1.55x C# faster |
| latin | 128 | 561.5 | 883.7 | 1.57x Py faster |
| latin | 512 | 6037.7 | 10046.4 | 1.66x Py faster |
| cjk | 8 | 136.3 | 34.6 | 3.94x C# faster |
| cjk | 32 | 337.5 | 261.2 | 1.29x C# faster |
| cjk | 128 | 1896.6 | 1501.9 | 1.26x C# faster |
| cjk | 512 | 15901.1 | 11931.5 | 1.33x C# faster |

- **Read the speedup column last, and only after the two it is made of.** On Indel
  the ranking flips with the alphabet — Lodestar is 1.66× behind on Latin at 512 and
  1.33× ahead on CJK — and **it is not Lodestar that got faster.** rapidfuzz goes
  from 6 038 ns to 15 901 on the same lengths, up 163%; Lodestar goes from 10 046 to
  11 932, up 19%. The column moved because the other side moved.
- **What each side pays for is different, and both were measured.** Lodestar's cost
  is the side table #302 added, and it is the same 1.2×–1.6× the gate benchmarks
  price on synthetic bands. rapidfuzz's is not about the alphabet at all: it tracks
  **CPython's internal string kind**. A control over three 27-symbol alphabets at
  length 512 reads 19 894 ns on ASCII, 19 445 on accented Latin-1 — stored one byte
  a character, like ASCII — and 29 522 on CJK, stored two. Above U+00FF the
  interpreter widens the string and rapidfuzz pays for it.
- **Both implementations draw their line at U+00FF, for unrelated reasons.** One
  indexes a 256-entry equality table, the other switches storage kind. Nothing
  arranged that, and it is what makes the wide buckets a fair test rather than a
  flattering one: the same threshold is crossed on both sides at the same character.
- **The Latin buckets are byte-identical to the ones this page already published.**
  One seeded stream feeds the generator and the CJK draws come strictly after the
  Latin ones, so every figure above the CJK rows still measures what it always did.

### The code-point mode, on input that leaves the BMP (#208)

The table above is the UTF-16 mode over an ASCII corpus. The code-point mode is a
different question and needed its own corpus: `LevenshteinCodePointBenchmarks`
draws both operands from U+1F300..U+1FAFF, so every character is a surrogate pair
and the two readings genuinely differ — which is the case
[`../decisions/0002-unicode-comparison-unit.md`](../decisions/0002-unicode-comparison-unit.md)
points a caller at.

Intel i7-4770S, .NET 10, BenchmarkDotNet short job, `[MemoryDiagnoser]`. **Two
runs per side, both shown**, interleaved after → before → after → before so that
any drift in machine state lands on both columns. `Distinct` is how many distinct
code points the operands are drawn from.

Both operands differ at their first and last code point, so `Trim` strips nothing
and `Length` is the pattern the threshold actually sees. That is not a detail: an
earlier corpus mutated only scattered positions, and at `Length = 16` a single
mutation left a pattern of two or three symbols — the fast path was never
entered, and the row compared the DP against itself while appearing to say the
threshold cost nothing.

| Length | Distinct | before | after | change |
| ---: | ---: | ---: | ---: | --- |
| 16 | 32 | 793 / 773 ns | 509 / 516 ns | **1.53× faster** |
| 16 | 512 | 765 / 774 ns | 515 / 515 ns | **1.49× faster** |
| 24 | 32 | 1.61 / 1.55 µs | 612 / 608 ns | **2.59× faster** |
| 24 | 512 | 1.48 / 1.43 µs | 624 / 648 ns | **2.29× faster** |
| 32 | 32 | 3.50 / 3.57 µs | 725 / 725 ns | **4.87× faster** |
| 32 | 512 | 3.38 / 3.44 µs | 753 / 736 ns | **4.58× faster** |
| 40 | 32 | 4.13 / 3.92 µs | 860 / 871 ns | **4.65× faster** |
| 40 | 512 | 4.96 / 4.97 µs | 863 / 870 ns | **5.73× faster** |
| 128 | 32 | 48.4 / 50.6 µs | 3.18 / 3.14 µs | **15.7× faster** |
| 128 | 512 | 40.4 / 40.4 µs | 3.10 / 3.08 µs | **13.1× faster** |
| 512 | 32 | 777 / 818 µs | 23.2 / 23.2 µs | **34.4× faster** |
| 512 | 512 | 658 / 660 µs | 683 / 632 µs | unchanged — see below |

**Zero allocation on both sides**, at every size: the renaming borrows its two
buffers from `ArrayPool` and the probe table is `stackalloc`.

- **The last row is the ceiling, not a disappointment.** A pattern is renamed
  into a dense alphabet of 255 symbols, and one holding more falls back to the
  DP. 512 random draws from 512 symbols hold ~330 distinct, so that row measures
  the fallback — and measures what the failed attempt costs, which is the number
  worth having: within noise of doing nothing, ≈1%. At `Length = 128` the same
  512-symbol alphabet yields ~110 distinct, under the ceiling, and the row is
  12.6× faster; the ceiling is about the *pattern*, not the alphabet.
- **The gate holds at 16, and that is measured rather than inherited.**
  `MyersMinPatternLength` was tuned for the character path, and this one pays for
  a probe table on top of the kernel's equality table, so it needed its own
  answer. At a pattern of exactly 16 code points the fast path is already **1.5×**
  faster, so the threshold is not too low. Whether it is too *high* is a
  different question and is untested: below 16 both sides take the DP, and the
  constant is shared with the character path, so lowering it would change the hot
  path and wants its own measurement.
- **The UTF-16 mode on this same corpus is the DP**, because every character is a
  surrogate and the Latin-1 check refuses them: 2.80 ms at `Length = 512` against
  the code-point mode's 22 µs. That is a comparison between two different
  questions and not a reason to switch modes — but it is the measurement that
  makes the remaining backlog item concrete.

## Vectorizers and fuzzy matching

Short-job measurement, `[MemoryDiagnoser]` (dev machine — indicative).

**Vectorizers**, fit+transform over a synthetic corpus:

| Method | 200 docs | 1000 docs |
| --- | ---: | ---: |
| `CountVectorizer` | ~4.2 ms | ~9.0 ms |
| `TfidfVectorizer` | ~4.3 ms | ~9.1 ms |
| `CountVectorizer` (bigrams) | ~4.8 ms | ~14.7 ms |
| `HashingVectorizer` | ~3.6 ms | ~8.6 ms |

**Fuzzy ratios**, on a ~43-character sentence pair:

| Method | Mean | Allocated |
| --- | ---: | ---: |
| [`Fuzz.Ratio`](../reference/fuzzy/matching/fuzz-ratio.md) | ~2.5 µs | **0 B** |
| [`Fuzz.TokenSortRatio`](../reference/fuzzy/matching/fuzz-tokensortratio.md) | ~5.3 µs | 1.3 KB |
| [`Fuzz.TokenSetRatio`](../reference/fuzzy/matching/fuzz-tokensetratio.md) | ~15 µs | 5.6 KB |
| [`Fuzz.WRatio`](../reference/fuzzy/matching/fuzz-wratio.md) | ~25 µs | 7.0 KB |
| [`Fuzz.PartialRatio`](../reference/fuzzy/matching/fuzz-partialratio.md) | ~460 µs | 0 B |

> `PartialRatio` is markedly slower: the current sliding-window scan is `O(n·m²)`
> (a full Indel per window). It is correct and zero-alloc, but a bit-parallel or
> block-based optimization is a clear backlog item for long inputs.

## Batched embedding — what the number is, and what it is not

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- --filter '*BatchEmbedding*' --inProcess
```

**Read this before quoting the ratio.** The model is `tiny_embedder.onnx`: one
Gather node over a 64 × 4 table, because weights are never committed
(`CONTRIBUTING.md`) and a real encoder is a hundred megabytes. Its arithmetic is
free. So what is measured is the per-sequence cost that batching removes — graph
dispatch, thread-pool wake-up, tensor wrapping — and none of the matrix
multiplication a real encoder adds to *both* sides. This is an upper bound on the
speed-up, not the speed-up.

Full job, `[MemoryDiagnoser]`, `InProcessEmitToolchain`, Intel Core i7-4770S
(Haswell, 4 physical cores), Ubuntu 24.04, .NET 10.0.10, X64 RyuJIT AVX2. Corpus
of 1 to 61 words per text, sub-batch 8. `UnitLoop` is the baseline — one `Embed`
call per text, which is what the guide's three lines amounted to before
`EmbedBatch` existed. **Two runs, both shown**, because BenchmarkDotNet's `±`
describes dispersion inside one process and not reproducibility across processes.

| Texts | `UnitLoop` | `EmbedBatch` | ratio | `EmbedBatchBucketed` | ratio | allocated vs baseline |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 9.9 / 11.0 µs | 10.6 / 10.8 µs | 1.06 / 0.99 | 10.0 / 11.1 µs | 1.00 / 1.01 | 1.12 |
| 8 | 168 / 180 µs | 105 / 107 µs | 0.62 / 0.60 | 101 / 106 µs | 0.60 / 0.59 | 0.95 |
| 32 | 628 / 672 µs | 360 / 381 µs | 0.57 / 0.57 | 346 / 349 µs | 0.55 / 0.52 | 0.94 / 0.91 |
| 128 | 2 439 / 2 602 µs | 1 482 / 1 460 µs | 0.61 / 0.56 | 1 370 / 1 356 µs | 0.56 / 0.52 | 0.94 / 0.91 |

**Batching removes about 40 % of the wall clock** from 8 texts upward and stays
there — 0.56–0.62 across every pairing — as the per-call overhead is amortized
over the whole sub-batch. At a single text there is nothing to amortize and the
two paths are a wash: 1.06 in one run, 0.99 in the next, which is a way of
saying this benchmark cannot tell them apart there rather than that either wins.

**Bucketing is a different story, and the honest answer is smaller.** It engages
only when the corpus spans more than one sub-batch, so the rows at 1 and 8 above
run the *identical* code in both columns — they are the control, and what they
differ by is this harness's noise floor: 1–3 % on seven of the eight control
measurements taken here, with one outlier at 5.7 %. At 128 texts bucketing is
ahead by 5.8–7.5 % in all four pairings, and ahead at 32 in all four as well.
The sign is consistent where the magnitude alone would not be decisive. What is
decisive is the allocation column, which is counted rather than sampled:
1 764 KB → 1 697 KB at 128 texts, in every run. That is padding genuinely not
written. On a model doing real work that padding would be matrix multiplication
not performed, and the time would follow; this model cannot show it, so the
claim stops here.

**The two builds.** The same benchmark against the `netstandard2.0` assemblies
does **not** measure a penalty. On `EmbedBatch` the netstandard2.0 side comes in
1.4–5.7 % *ahead* of net10 in all four pairings (355 / 365 µs against 360 / 381
at 32 texts; 1 418 / 1 419 against 1 482 / 1 460 at 128), which is inside this
harness's noise but consistent in direction. This guide previously reported the
opposite — "3–5 % behind" — from a pair of commands that did not share a
BenchmarkDotNet toolchain. That comparison is withdrawn rather than reversed
(issue #87).

Withdrawn, not disproved. The figures above cannot be set against the old ones,
for a reason that has nothing to do with either harness: the old table predates
the bump of `Microsoft.ML.OnnxRuntime` to 1.28.0, and this benchmark is almost
entirely that library's dispatch cost. Add to that a machine carrying twice the
load, and no difference between the two sets is attributable to anything. What
the old pair of commands can be faulted for is visible without measuring — its
two columns came from two toolchains.

Within *this* window the harness is not the explanation either. Running the
net10 side out-of-process here, the same mismatched shape, moves this tier by
2 % at most (`EmbedBatch` 356 µs at 32 texts and 1 441 at 128, against 360 / 381
and 1 482 / 1 460 in-process) and still shows no netstandard2.0 penalty. So
unlike `VectorMath`, where the mismatch moves the ratio by up to 0.5×, on this
path the toolchain barely registers.

**No penalty is the structurally correct answer here**, and the earlier figure
should have been read as suspicious for that reason. `Pooling` guards its
`Vector<T>` branch with `accumulator.Length >= Vector<float>.Count`;
`tiny_embedder.onnx` has a hidden size of 4 (`EMBEDDING_DIM` in
`tools/build_tiny_models.py`) and `Vector<float>.Count` is 8 under AVX2, so on
net10 the guard is false and the code falls into the same scalar tail loop
netstandard2.0 runs unconditionally. The two builds execute identical pooling on
this benchmark. It cannot measure the difference between them, and now reports
that instead of a number. Where the vector path does engage, it is worth 4×–7×
(`VectorMath` over 384–1024 dimensions, section 2 of
[`bench/README.md`](https://github.com/CyrilB1531/lodestar/blob/main/bench/README.md)).
The one difference this benchmark does resolve is counted, not timed: the unit-loop path
allocates 0.6 % more on netstandard2.0 (1 887 KB against 1 875 at 128 texts),
identically in both runs, while the two batch paths allocate byte for byte the
same on both targets.

```bash
dotnet run -c Release --project bench/Lodestar.NetStandard.Benchmarks -- --filter '*BatchEmbedding*'
```

`--inProcess` on the first command, and not on the second, is the point. The
netstandard2.0 project pins `InProcessEmitToolchain` in its `Program.cs` — it
has to, or BenchmarkDotNet's generated project re-resolves the
`ProjectReference` and silently restores the net10.0 build — so the flag is what
puts the net10 side on the same toolchain. Without it the two commands measure
the same code two different ways.

**Conditions.** The four runs behind the table were taken back to back in one
window, alternating net10 and netstandard2.0, with the one-minute load average
between 5.1 and 5.9 on 8 logical cores — the editor's language servers and the
session driving the runs are part of that load and cannot be excluded from
inside it. Both columns pay it equally, so the table is internally comparable;
it is not comparable to figures taken on this machine in a quieter state, and
the ratios travel between such sets while the absolute microseconds do not.

## Clustering agreement from labels (issue #191)

Intel Core i7-4770S @ 3.10 GHz, .NET 10.0.110, Release. Median of 21 runs (n <= 10 000) and 5 runs
(n = 100 000), one process, two random labellings of `n` samples over `k` clusters.

| n | k | `FowlkesMallows` | `AdjustedMutualInformation` | `AdjustedRand` |
| ---: | ---: | ---: | ---: | ---: |
| 1 000 | 5 | 0.36 ms | 0.42 ms | 0.27 ms |
| 10 000 | 5 | 1.66 ms | 2.84 ms | 2.44 ms |
| 10 000 | 50 | 8.07 ms | 14.17 ms | 1.73 ms |
| 100 000 | 10 | 5.46 ms | 26.83 ms | 5.60 ms |
| 100 000 | 100 | 37.76 ms | **254.89 ms** | 39.63 ms |

**Fowlkes-Mallows costs about what `AdjustedRand` costs**, which is the expected result: it is a
second reader of the contingency table the other already builds, and it adds one pass over the
cells.

**Adjusted mutual information is the expensive one, and the cost grows with the number of
clusters rather than only with the samples.** The correction sums over the hypergeometric
distribution of every (class, cluster) cell the marginals allow, so the work is bounded by
`classes x clusters x n` and not by `n` alone: at 100 000 samples it is 4.8x `AdjustedRand` at 10
clusters and 6.4x at 100. That is inherent to the quantity — scikit-learn sums the same terms —
and it is the reason to reach for
[`NormalizedMutualInformation.Score`](../reference/metrics/clustering/normalizedmutualinformation-score.md)
instead when the two labellings being compared have the same number of clusters and the chance
correction is not buying anything.

Nothing here was optimised. These are the numbers as first written, published so the next change
has a baseline rather than an impression to argue against.

### The uncorrected halves (issue #213)

Same machine and method, median of 31 runs at `n = 100 000`. All three new members read
`Internal/Contingency`, the structure the table above's metrics already build.

| n | k | `RandIndex` | `MutualInformation` | [`PairConfusionMatrix.Compute`](../reference/metrics/clustering/pairconfusionmatrix-compute.md) | `AdjustedRand` (reference) |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 100 000 | 10 | 7.22 ms | 6.40 ms | 6.03 ms | 6.00 ms |
| 100 000 | 100 | 41.04 ms | 40.23 ms | 39.98 ms | 41.26 ms |

All four land in the same band as `AdjustedRand` at both cluster counts, which is what the code
says should happen: [`RandIndex.Score`](../reference/metrics/clustering/randindex-score.md) calls [`PairConfusionMatrix.Compute`](../reference/metrics/clustering/pairconfusionmatrix-compute.md) and adds two numbers,
so it cannot cost meaningfully more than the call it wraps. None of the three introduces the
`classes x clusters x n` shape `AdjustedMutualInformation` has above — there is no hypergeometric
sum here, only the pair counts and the mutual-information term the sequential agreement metrics
already pay for.

One measurement is worth naming rather than hiding: an earlier pass at 11 runs read `RandIndex` as
24.6 ms against [`PairConfusionMatrix.Compute`](../reference/metrics/clustering/pairconfusionmatrix-compute.md)'s 6.4 ms at `n = 100 000, k = 10` — four times its
own dependency, which is not a shape the code can produce. Raising the run count to 31 collapsed
the gap to the numbers above. This machine runs guarded `dotnet` commands behind a lock shared
with other sessions (see `CONTRIBUTING.md`), and a contended run landing inside a short sample is
the likely cause. Reported here because a number that does not survive more samples is exactly
what a `perf/`-style measurement is supposed to catch before it reaches a PR description, not
after.

## The sort inside the binary ROC curve (issue #206)

Intel Core i7-4770S @ 3.10 GHz, 8 logical cores, .NET 10.0.110, Release. Median of
11 runs (n = 1 000 000) and 41 runs (n = 100 000) of [`RocAuc.Score`](../reference/metrics/classification/rocauc-score.md), one process,
same binary — only `BinaryRoc.RadixThreshold` differs between the columns, so the
two rows of a pair measure nothing but the sort.

| n | equal scores | `Array.Sort` | radix | gain |
| --- | --- | --- | --- | --- |
| 100 000 | all distinct | 8.15 ms | **5.56 ms** | 1.47x |
| 100 000 | ~100 per score | 6.22 ms | **3.94 ms** | 1.58x |
| 1 000 000 | all distinct | 97.44 ms | **76.91 ms** | 1.27x |
| 1 000 000 | ~100 per score | 78.87 ms | **59.97 ms** | 1.32x |

The 97.44 ms baseline agrees with the 95.219 ms `roc_auc_binary_n1000000_k2`
below, measured a different way on the same machine.

**Why the sort at all.** Profiled on the same machine, one binary curve at
n = 1 000 000 splits 6 ms building the points, **91 ms sorting**, 3 ms
accumulating — the sort is 91% of it, and at n = 100 000 it is effectively all of
it. Nothing else in the curve is worth touching until it is.

**Why a radix rather than the alternatives.** Sorting an `int` index array against
the scores — the cheaper-items idea — was measured and rejected: 6.88 ms against
7.47 ms at n = 100 000, but 98.23 ms against 86.96 ms at a million, where the
gather costs more than the smaller items save. Eight-bit digits beat sixteen-bit
ones below ~16 000 and lose above (77.67 ms against 66.51 ms at a million).

**Why the threshold is 8 192.** The radix carries four passes and a 64 K
histogram whatever the input, so it loses on small ones: 0.85x at n = 6 000,
0.98x at 8 000, 1.21x at 10 000, 1.55x at 16 000. Below the threshold the curve
still sorts by comparison, and the extra buffers are not even rented.

**No parallelism was added.** #86 left this sort as the parallelisable remainder;
measured, it did not need to be. A parallel sort here would nest inside the
region #86 already parallelises on the multiclass path, and 1.3x sequential is
the cheaper answer.

## Classification metrics (issue #61) — vs scikit-learn

```bash
python bench/corpus/generate_metrics.py           # writes bench/corpus/metrics/, git-ignored
. .venv-oracles/bin/activate && python bench/python/bench_metrics.py
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- compare-metrics
python bench/compare.py metrics
```

Six operations — `confusion_matrix`, `accuracy`, `precision_recall_f1_macro`,
`classification_report`, `roc_auc_binary`, `roc_auc_ovr_macro` — over six shapes
(1 000 / 100 000 / 1 000 000 samples, 2 or 10 classes), on the same corpus files
on both sides. **This is the merge gate for the branch, on processor time**: every
row must be ≥ 1×, and it is.

Lodestar.Metrics on .NET 10.0.10 against scikit-learn 1.9.0 / NumPy 2.5.1 on
Python 3.12.3, Intel i7-4770S. Both sides measured back to back, Python first,
on a machine left to settle (one-minute load 1.52 at the Python start — below
this workstation's 1.9–2.3 floor, itself a permanent ~30–40 % background from
the desktop client, an editor and a browser). The C# side started 49 seconds
later, in the Python run's own wake, so its figures are the ones taken on the
busier machine and every ratio below is conservative rather than flattering;
`bench/README.md` records the full conditions.

| Operation | Lodestar ms | Python ms | wall | Lodestar cpu ms | Python cpu ms | **cpu** |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `confusion_matrix_n1000_k2` | 0.009 | 1.028 | 117.98x | 0.009 | 1.028 | **117.97x** |
| `accuracy_n1000_k2` | 0.001 | 0.546 | 618.32x | 0.001 | 0.546 | **618.33x** |
| `precision_recall_f1_macro_n1000_k2` | 0.008 | 1.793 | 226.58x | 0.008 | 1.793 | **226.58x** |
| `classification_report_n1000_k2` | 0.011 | 6.692 | 623.31x | 0.011 | 6.691 | **623.25x** |
| `roc_auc_binary_n1000_k2` | 0.029 | 2.008 | 70.12x | 0.029 | 2.008 | **70.12x** |
| `confusion_matrix_n1000_k10` | 0.009 | 1.051 | 120.64x | 0.009 | 1.051 | **120.64x** |
| `accuracy_n1000_k10` | 0.001 | 0.541 | 622.03x | 0.001 | 0.541 | **622.08x** |
| `precision_recall_f1_macro_n1000_k10` | 0.010 | 1.855 | 192.49x | 0.010 | 1.855 | **192.48x** |
| `classification_report_n1000_k10` | 0.017 | 7.011 | 422.54x | 0.017 | 7.010 | **422.53x** |
| `roc_auc_ovr_macro_n1000_k10` | 0.550 | 10.526 | 19.13x | 0.550 | 10.525 | **19.13x** |
| `confusion_matrix_n100000_k2` | 0.964 | 15.791 | 16.39x | 0.964 | 15.791 | **16.39x** |
| `accuracy_n100000_k2` | 0.190 | 5.519 | 29.01x | 0.190 | 5.518 | **29.01x** |
| `precision_recall_f1_macro_n100000_k2` | 0.844 | 17.786 | 21.07x | 0.844 | 17.785 | **21.07x** |
| `classification_report_n100000_k2` | 0.848 | 36.233 | 42.75x | 0.847 | 36.231 | **42.75x** |
| `roc_auc_binary_n100000_k2` | 7.977 | 35.024 | 4.39x | 8.092 | 35.023 | **4.33x** |
| `confusion_matrix_n100000_k10` | 1.059 | 16.109 | 15.20x | 1.059 | 16.108 | **15.21x** |
| `accuracy_n100000_k10` | 0.296 | 5.519 | 18.66x | 0.296 | 5.519 | **18.66x** |
| `precision_recall_f1_macro_n100000_k10` | 0.979 | 18.524 | 18.92x | 0.979 | 18.523 | **18.92x** |
| `classification_report_n100000_k10` | 0.979 | 40.139 | 41.00x | 0.979 | 40.137 | **41.00x** |
| `roc_auc_ovr_macro_n100000_k10` | 88.385 | 250.400 | 2.83x | 91.396 | 250.402 | **2.74x** |
| `confusion_matrix_n1000000_k2` | 8.750 | 156.920 | 17.93x | 8.749 | 156.823 | **17.92x** |
| `accuracy_n1000000_k2` | 2.045 | 51.599 | 25.23x | 2.045 | 51.596 | **25.23x** |
| `precision_recall_f1_macro_n1000000_k2` | 8.701 | 164.332 | 18.89x | 8.701 | 164.330 | **18.89x** |
| `classification_report_n1000000_k2` | 8.719 | 314.805 | 36.11x | 8.718 | 314.782 | **36.11x** |
| `roc_auc_binary_n1000000_k2` | 95.219 | 364.420 | 3.83x | 95.684 | 364.384 | **3.81x** |
| `confusion_matrix_n1000000_k10` | 9.916 | 156.707 | 15.80x | 9.915 | 156.699 | **15.80x** |
| `accuracy_n1000000_k10` | 3.122 | 51.877 | 16.61x | 3.122 | 51.874 | **16.61x** |
| `precision_recall_f1_macro_n1000000_k10` | 10.001 | 173.128 | 17.31x | 10.000 | 173.121 | **17.31x** |
| `classification_report_n1000000_k10` | 9.865 | 352.364 | 35.72x | 9.864 | 352.349 | **35.72x** |

**Gate result: 29/29 operations at or above 1× on processor time.** The
narrowest margin is **2.74×**, on `roc_auc_ovr_macro` at n=100 000, k=10 — the
row the design brief flagged as the one most likely to need a radix-sort
rewrite of `BinaryRoc`. It did not: even the heaviest sort-bound row clears the
gate by a comfortable margin, so no algorithmic change was needed on this
branch.

**Read this before quoting a single ratio.** The rows at n=1 000 (70×–620×) are
dominated by CPython's per-call interpreter overhead, not by the computation —
a confusion matrix over 1 000 samples is sub-microsecond work on either side.
The rows that carry the argument are the ones at n=100 000 and n=1 000 000,
where the ratios settle to a more modest but still decisive 2.7×–43×.

Unlike the persistence comparison, wall and processor time agree here to
within about 1% on every row (up to 3.4% on the single heaviest-cpu row): these
metrics allocate little enough per call that .NET's background collector is
never a factor, so there is no gap between the two columns to explain away.

Full breakdown, including the intra-C# and net10-vs-netstandard2.0 tiers and
where the two language sides do not do identical work, in
[`bench/README.md`](https://github.com/CyrilB1531/lodestar/blob/main/bench/README.md#5-classification-metrics-issue-61).

### Balanced accuracy, Matthews correlation, Cohen's kappa (issue #93)

Balanced accuracy, Matthews correlation and Cohen's kappa (issue #93, Tasks
3–5) add three operations — `balanced_accuracy`, `matthews`, `cohen_kappa` —
run over all six shapes above, unweighted and with default label handling on
both sides, matching scikit-learn's `balanced_accuracy_score`,
`matthews_corrcoef` and `cohen_kappa_score`. Same corpus files, same harnesses,
same methodology as the table above — **but measured in a separate window
from the original 29 rows, with its own load**: `uptime`'s one-minute average
was **19.70** just before the Python side started and **7.65** by the time
`compare.py` printed the numbers below (fifteen-minute average 13.2–14.9
throughout that window). That is nowhere near the 1.52 one-minute load the
paragraph above states for the original run, so these 18 rows should not be
read as sharing that sentence's conditions — only their own, given here.

| Operation | Lodestar ms | Python ms | wall | Lodestar cpu ms | Python cpu ms | **cpu** |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `balanced_accuracy_n1000_k2` | 0.016 | 1.194 | 76.44x | 0.011 | 1.194 | **105.24x** |
| `matthews_n1000_k2` | 0.017 | 2.216 | 134.27x | 0.012 | 2.216 | **192.06x** |
| `cohen_kappa_n1000_k2` | 0.018 | 1.240 | 67.89x | 0.012 | 1.240 | **105.23x** |
| `balanced_accuracy_n1000_k10` | 0.008 | 1.225 | 152.93x | 0.008 | 1.225 | **152.96x** |
| `matthews_n1000_k10` | 0.008 | 2.258 | 282.30x | 0.008 | 2.258 | **282.33x** |
| `cohen_kappa_n1000_k10` | 0.009 | 1.399 | 157.84x | 0.009 | 1.399 | **157.84x** |
| `balanced_accuracy_n100000_k2` | 0.887 | 17.287 | 19.49x | 0.887 | 17.282 | **19.48x** |
| `matthews_n100000_k2` | 0.884 | 34.733 | 39.28x | 0.884 | 34.712 | **39.26x** |
| `cohen_kappa_n100000_k2` | 0.880 | 18.133 | 20.61x | 0.880 | 18.103 | **20.58x** |
| `balanced_accuracy_n100000_k10` | 1.001 | 17.326 | 17.31x | 1.001 | 17.320 | **17.31x** |
| `matthews_n100000_k10` | 0.996 | 36.312 | 36.46x | 0.996 | 36.307 | **36.45x** |
| `cohen_kappa_n100000_k10` | 0.980 | 17.130 | 17.49x | 0.979 | 17.129 | **17.49x** |
| `balanced_accuracy_n1000000_k2` | 9.087 | 166.698 | 18.35x | 9.085 | 166.690 | **18.35x** |
| `matthews_n1000000_k2` | 9.003 | 350.953 | 38.98x | 9.003 | 350.762 | **38.96x** |
| `cohen_kappa_n1000000_k2` | 9.032 | 186.455 | 20.64x | 9.032 | 185.697 | **20.56x** |
| `balanced_accuracy_n1000000_k10` | 10.103 | 167.552 | 16.58x | 10.102 | 167.550 | **16.59x** |
| `matthews_n1000000_k10` | 10.262 | 340.992 | 33.23x | 10.261 | 340.854 | **33.22x** |
| `cohen_kappa_n1000000_k10` | 10.352 | 174.623 | 16.87x | 10.352 | 174.619 | **16.87x** |

**18/18 at or above 1× on processor time — the gate holds for these three
metrics too.** The two narrowest are `balanced_accuracy_n1000000_k10` at
**16.59×** and `cohen_kappa_n1000000_k10` at **16.87×**; every other row
clears 17×. As with the original 29, the busier the machine gets, the more
conservative (not flattering) a ratio above 1× is — and this window's load
average was roughly 5–13× the original run's, so these margins are, if
anything, understated relative to a quiet machine.

### Regression metrics — mse, mae, median_ae, r2 (issue #92)

The eleven regression metrics landed for issue #92 add four benchmark
operations — `mse`, `mae`, `median_ae`, `r2` — covering the four distinct cost
shapes among them: a squared mean, an absolute mean, a sort, and a two-pass
centred sum. The other seven metrics are one of those four with a different
arithmetic kernel and are not separately timed. They run over
`y_true_real`/`y_pred_real`, continuous targets drawn by a separate seeded
random generator and attached to each of the six existing corpus shapes,
independent of the classification columns those shapes already carry. The
generator inserting these draws would otherwise have shifted every
classification array after the insertion point, invalidating the 29 and 18
rows above. A before/after comparison of `y_true[:10]` on the regenerated
corpus confirmed it did not. Same corpus files, same harnesses, same
methodology as the tables above — **but measured in yet another separate
window, with its own load**: `uptime`'s one-minute average was **8.05** just
before the Python side started (five/fifteen-minute: 11.95 / 14.25) and
**6.05** by the time `compare.py` printed the numbers below (five/fifteen-minute:
7.15 / 11.07). That is well below the 16–23 one-minute load this session saw
at dispatch and while the code changes were being made, but still noticeably
busier than the 1.52 one-minute load recorded for the original 29 rows. So
these 24 rows should be read only under their own conditions, given here —
**except the six `median_ae` rows marked †**. Those come from a later
window described below, after `MedianAbsoluteError`'s unweighted path was
rewritten.

**Read the `k` suffix as a corpus file name, not as a workload.** The
regression arrays are drawn from `SeededRandom(SEED + 1_000 + n)`, which
depends on the sample count and not on the class count, so `metrics_n1000_k2`
and `metrics_n1000_k10` carry byte-identical `y_true_real`. That is deliberate
— all four operations here are single-output, and `k` is a property of the
classification columns those files also hold — but it means the 24 rows below
are **12 distinct workloads, each measured twice**. The pairs are useful for
exactly that: they bound the run-to-run spread. At n=1 000 000 the two members
agree to within 0.04× (`mse` 1.04× / 1.00×), while at n=1 000 the same
identical array gives 98.88× and 141.17× — a 43 % spread, which is what a
sub-millisecond `mse` measurement is worth on a machine at this load, and the
reason no conclusion on this page rests on an n=1 000 row.

| Operation | Lodestar ms | Python ms | wall | Lodestar cpu ms | Python cpu ms | **cpu** |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `mse_n1000_k2` | 0.005 | 0.486 | 104.89x | 0.005 | 0.458 | **98.88x** |
| `mae_n1000_k2` | 0.005 | 0.358 | 77.79x | 0.005 | 0.358 | **77.70x** |
| `median_ae_n1000_k2`† | 0.011 | 0.818 | 77.81x | 0.011 | 0.625 | **59.45x** |
| `r2_n1000_k2` | 0.008 | 0.443 | 57.72x | 0.008 | 0.442 | **57.66x** |
| `mse_n1000_k10` | 0.005 | 1.003 | 219.23x | 0.005 | 0.646 | **141.17x** |
| `mae_n1000_k10` | 0.005 | 0.541 | 119.33x | 0.005 | 0.507 | **111.80x** |
| `median_ae_n1000_k10`† | 0.011 | 0.367 | 34.83x | 0.011 | 0.367 | **34.84x** |
| `r2_n1000_k10` | 0.008 | 0.447 | 55.95x | 0.008 | 0.447 | **55.86x** |
| `mse_n100000_k2` | 0.452 | 0.645 | 1.43x | 0.452 | 0.645 | **1.43x** |
| `mae_n100000_k2` | 0.466 | 1.588 | 3.41x | 0.466 | 1.295 | **2.78x** |
| `median_ae_n100000_k2`† | 1.967 | 1.781 | 0.91x | 2.045 | 1.781 | **0.87x** |
| `r2_n100000_k2`‡ | 0.759 | 0.991 | 1.31x | 0.759 | 0.991 | **1.31x** |
| `mse_n100000_k10` | 0.455 | 0.628 | 1.38x | 0.454 | 0.628 | **1.38x** |
| `mae_n100000_k10` | 0.458 | 0.673 | 1.47x | 0.458 | 0.672 | **1.47x** |
| `median_ae_n100000_k10`† | 2.142 | 1.796 | 0.84x | 2.241 | 1.795 | **0.80x** |
| `r2_n100000_k10`‡ | 0.743 | 0.950 | 1.28x | 0.743 | 0.950 | **1.28x** |
| `mse_n1000000_k2` | 5.013 | 5.226 | 1.04x | 5.008 | 5.220 | **1.04x** |
| `mae_n1000000_k2` | 5.054 | 5.635 | 1.12x | 5.036 | 5.633 | **1.12x** |
| `median_ae_n1000000_k2`† | 18.365 | 16.375 | 0.89x | 18.708 | 16.360 | **0.87x** |
| `r2_n1000000_k2`‡ | 8.093 | 9.205 | 1.14x | 8.083 | 9.204 | **1.14x** |
| `mse_n1000000_k10` | 4.983 | 4.989 | 1.00x | 4.982 | 4.983 | **1.00x** |
| `mae_n1000000_k10` | 5.040 | 5.712 | 1.13x | 5.035 | 5.711 | **1.13x** |
| `median_ae_n1000000_k10`† | 18.094 | 16.282 | 0.90x | 18.163 | 16.259 | **0.90x** |
| `r2_n1000000_k10`‡ | 7.807 | 9.687 | 1.24x | 7.807 | 9.686 | **1.24x** |

† All six `median_ae` rows were re-measured after the quickselect rewrite
described below, in a separate window from the other eighteen rows in this
table. Every other cell is the original, unrewritten-algorithm measurement.
**They are themselves superseded**: issue #140 took a further 38% off
`median_ae` at n = 1 000 000, so every row marked † measures a partition this
package no longer ships. See "Branchless partitioning (issue #140)" below.

‡ These four `r2` rows predate issue #127: they measure the original
sequential-sum `R2`, not the Neumaier-compensated one this package ships
today, and are stale for that reason — kept here only so the pairing this
table relies on (each workload measured twice, at `k=2` and `k=10`) stays
intact. The compensated-and-optimised numbers are in
"Compensated summation (issue #127)" below, in the same window as its own
`numpy` column, which is not directly comparable to the `Python ms` column
here. `mse` and `mae` are not marked at n = 1 000 000: that section's own
numbers put them within this page's noise band there (0.6% and 1.8% away
from the figures printed above). That does not hold at n = 100 000, where
the same comparison is 5.7% and 5.4% — wider than the 1.8% this page treats
as the top of the band — so the claim below is scoped to the n = 1 000 000
rows only.

**20/24 rows at or above 1× on processor time when this table was first
measured — `median_ae` was the finding, not a fluke to rerun away.** That is
20 of 24 *rows*, which under the pairing above is 10 of 12 distinct
workloads; the four that failed were two workloads, each measured twice, and
they failed both times. All four `median_ae` rows at n=100 000 and
n=1 000 000 landed below the gate —
**0.36×**, **0.25×**, **0.19×** and **0.19×** — meaning Python was 3× to over
5× *faster* there, the only rows on this page where that was true. The cause
was the algorithm, not the run: scikit-learn's `median_absolute_error` calls
NumPy's `median`, which selects via introselect/quickselect in expected
`O(n)`; Lodestar's `MedianAbsoluteError` sorted the whole residual array,
which is `O(n log n)`, and the gap widened with `n` exactly as that
complexity difference predicted (0.36× at 100 000 rows, 0.19× at
1 000 000). `mse_n1000000_k10` was the narrowest *passing* row at **1.00×**
— a squared mean over a million rows, near enough to parity that a busier or
quieter machine could tip it either way; every other passing row cleared
1.12×.

**What changed.** `WeightedPercentile`'s unweighted branch (the follow-up
this branch was created for) no longer sorts the whole array: it selects the
one or two order statistics the median needs with a median-of-three
quickselect, falling back to `Array.Sort` on the remaining range once
partitioning has run more than a budget proportional to `log2(n)` — the same
introselect guarantee NumPy's own `median` relies on, so the worst case
stays `O(n log n)` instead of degrading to `O(n²)` on adversarial input. The
weighted branch, which genuinely needs sorted order for its cumulative-weight
walk, was not touched.

**Re-measured under load deliberately comparable to the original run, not a
quieter one.** All six `median_ae` rows above, marked †, were re-measured
after that rewrite in one pass over the full 24-operation harness, with the
same corpus and harnesses as the rest of this section — the two n=1 000
rows were already below the gate's radar (neither the original nor the
rewritten algorithm is close to failing there), but re-measuring them
alongside the four that mattered keeps every `median_ae` row honest about
which implementation it describes, rather than leaving two rows silently
mixed in with the pre-rewrite ones. Re-running on an idle machine would have
folded "the machine got quieter" into "the algorithm got faster," and a
reader could not have told the two apart — so the measurement was
deliberately taken while the one-minute load sat in the same 6–10 band as
the original run's 8.05 → 6.05, rather than waiting for a quieter machine.
`uptime`'s one-minute average was **6.62** just before the Python side
started (five/fifteen-minute: 15.34 / 14.79) and **6.52** by the time
`compare.py` printed the numbers above (five/fifteen-minute: 7.37 / 9.96).
The same fresh run put `mse_n1000000_k10` — untouched by this rewrite,
recorded above at its original **1.00×** — at **0.99×**: that row sits at
parity either way, so which side of the gate it lands on is scheduling luck
between runs, not a change in the code, and the table above keeps its
original value rather than being edited to match this aside.

**The four rows are faster but still below the gate — that is the finding,
not a reason to keep iterating on the algorithm.** In absolute terms
Lodestar's own time dropped by roughly 4×–4.8× (7.358 ms → 1.967 ms at
n=100 000, k=2; 88.792 ms → 18.365 ms at n=1 000 000, k=2), and the
processor-time ratio against scikit-learn rose from **0.36×** to **0.87×**
(n=100 000, k=2), **0.25×** to **0.80×** (n=100 000, k=10), **0.19×** to
**0.87×** (n=1 000 000, k=2) and **0.19×** to **0.90×** (n=1 000 000, k=10).
Those are four rows over two workloads, not four independent measurements —
the `k=2` and `k=10` members of each pair run on the same array — so read
them as two recoveries each confirmed twice: 0.36×/0.25× → 0.87×/0.80× at
n=100 000, and 0.19×/0.19× → 0.87×/0.90× at n=1 000 000. The agreement
within each pair is what makes the recovery credible; a 4× swing on one row
alone would not be.
NumPy's introselect and this quickselect now do the same order of work —
`O(n)` expected, `O(n log n)` worst case — so the remaining gap reads as
constant overhead (managed bounds checks, the Lomuto partition's extra
writes, no SIMD-accelerated comparison loop) rather than an algorithmic
difference, and is recorded here as measured rather than chased further.

#### Compensated summation (issue #127)

An ill-conditioned target — a large offset over a small spread — showed that
`R2`, `ExplainedVariance` and the shared kernel walk behind `mse`/`mae` and
friends were accumulating with a plain sequential sum, where numpy sums
pairwise. On such a target the two round differently: `R2`'s two passes
landed **357× outside** the oracle's `1e-9` tolerance (issue #127's fixture:
`offset = 1e9`, `spread = 1e-2`, n = 200 000). Three sites now sum with
Neumaier compensation instead — `R2`'s two passes, `ExplainedVariance`'s five
accumulations, and `Outputs.WeightedMean`, which `MeanSquaredError`,
`RootMeanSquaredError`, `MeanAbsoluteError`, `MeanAbsolutePercentageError`,
`MeanSquaredLogError`, `RootMeanSquaredLogError` and `PinballLoss` all walk
through. `median_ae` (which sorts) and `max_error` (which never sums) are
untouched and stand in as this round's control.

**The naive fix was not free, and was not accepted at its first cost.**
Measured before any recovery work, compensating cost 1.38×–1.46× the
uncompensated loop on `mse`/`mae` and 1.56×–1.63× on `r2` — which pays the
compensated sum twice per row, once per pass — against a `median_ae` control
that moved at most 1.04×. Two changes recovered it: an unweighted fast path
that skips the weight multiply and the weight sum at all three sites when no
`sampleWeight` is supplied (a sum of *n* ones is exactly *n* below 2^53, no
compensation needed), and a `Vector<double>` reduction — one Neumaier partial
sum per SIMD lane — for `R2` and `ExplainedVariance`'s single-output
unweighted case on `net10.0` (the scalar loop is unchanged on
`netstandard2.0` and for multi-output; see
[`docs/decisions/0001`](../decisions/0001-target-framework.md)). A third
lever, a branchless 2Sum in place of Neumaier's magnitude-compared branch,
was measured and **reverted**: it was measurably slower on this workload,
most visibly on `r2` (+5.2%, outside both groups' own spread in either
measurement order). No performance counters were read, so branch prediction
is offered as the likely explanation, not as something observed — what was
measured is that the "branchless is faster" hypothesis this lever tested
came out falsified, not confirmed.

**Before (the original sequential sum) against after (compensated and
optimised) — same corpus, same harness as the table above, `k=2` shape
only:**

| Operation | before (ms) | after (ms) | cost vs before | numpy (ms) |
| --- | ---: | ---: | ---: | ---: |
| `mse_n100000_k2` | 0.445 | 0.478 | 1.07x | 0.568 |
| `mse_n1000000_k2` | 4.757 | 5.042 | 1.06x | 4.743 |
| `mae_n100000_k2` | 0.449 | 0.491 | 1.09x | 0.579 |
| `mae_n1000000_k2` | 4.769 | 5.146 | 1.08x | 5.330 |
| `r2_n100000_k2` | 0.740 | 0.358 | **0.48x** | 0.867 |
| `r2_n1000000_k2` | 7.820 | 4.280 | **0.55x** | 9.193 |
| `median_ae_n100000_k2` *(control)*§ | 1.681 | 1.672 | 0.99x | 1.716 |
| `median_ae_n1000000_k2` *(control)*§ | 15.718 | 15.994 | 1.02x | 15.696 |

§ Both control rows predate issue #140, which took 38% off `median_ae` at
n = 1 000 000. They are the right control for *this* round — nothing here
touched the median — but they are not the current cost of the operation.

`r2` is this round's confirmed result: **faster than the uncompensated loop
it replaced**, not merely recovered back to it, and 2.15× faster than numpy
at n = 1 000 000 — the SIMD lane-wise reduction pays for the compensation and
then some, an asymmetry only available here because the uncompensated
baseline was itself a scalar loop with room to vectorize.
`Outputs.WeightedMean` could not take the same route: it calls a kernel
through a generic interface per element, so a lane-wise accumulator around a
scalar-called kernel would buy nothing without vectorizing all five kernels
in their own right, which was assessed and left out of scope.

Load: `uptime`'s one-minute average was 9.8 at the start of this round (just
under the wait threshold this project measures against), settling to
4.9–5.5 for the rest of it; read via `uptime` before each run, and none
exceeded 10 once the wait loop cleared.

**`mse` and `mae`'s 1.06×–1.09× are not a second confirmed result — they are
inside this round's own noise.** The control (`median_ae`, untouched by any
of this) moved 1.8% at n = 1 000 000 in this same round — within the band
this page accepts, but at the top of it. `r2`'s 45–52% swing survives that
easily; `mse` and `mae`'s cost is only three to four times the control's own
drift, which is not enough separation from the noise floor to publish as a
settled number. Read them as "roughly unchanged by the optimisation, within
this round's noise" rather than as a precise cost. A cleaner window that
measured the unweighted fast path alone, before the SIMD lever, put both
nearer 1.02×–1.07× with the control moving at most 1.4% — closer to the true
cost of that lever, but not this round's number, and not what is published
above as the current state.

#### Branchless partitioning (issue #140)

`MedianAbsoluteError` was the most expensive regression metric by a factor of
three, and the issue opened believing a sort was the reason and threads were
the answer. Instrumenting `Compute` per phase at n = 1 000 000 said otherwise:

| phase | time | share |
| --- | ---: | ---: |
| allocating the 8 MB residual column | 0.6 ms | 4% |
| filling it with abs(y − ŷ) | 2.6 ms | 16% |
| **`QuickSelect`** | **10.8 ms** | **68%** |
| validation, reduction, harness | 2.0 ms | 12% |

That killed two hypotheses in one measurement. `ArrayPool` for the column
would have bought 0.6 ms, not a third. And the fill is *cheaper* than `mse`'s
loop, because it only subtracts and takes an absolute value where `mse` adds a
compensated sum per element. Parallelism was measured separately and not
taken: partitioning `Outputs.WeightedMean` over fixed ranges reached 1.56× at
four workers and 1.65× at eight, and only with pinned pointers —
`ReadOnlySpan<double>` cannot be captured in a lambda, so a shipped version
needs either `unsafe`, which this repository sets to `false` explicitly, or
the 16 MB copy `MultiClassRoc.CopyForWorkers` makes, which costs more than the
parallelism saves.

**The diagnosis, and the experiment it had to avoid.** `QuickSelect` was
spending about 5 ns per element on sequential access, which is not bandwidth.
The suspect was the one data-dependent branch in the Lomuto partition's inner
loop, taken about half the time on unsorted residuals — the worst case for a
predictor. The natural test, timing random input against sorted input, is
**confounded**: on sorted input the median-of-three pivot lands on the true
median, so one partition pass suffices where random data needs several, and
the time would collapse because the *number of passes* collapsed. So the
experiment counted inner-loop iterations and compared nanoseconds per element
touched, on three shapes:

| shape | ns per element touched |
| --- | ---: |
| random | 4.39 |
| sorted ascending | 2.15 |
| alternating 0 / 1 | 2.65 |

Both *predictable* shapes sit together at 2.15 and 2.65 and only the coin-flip
shape costs 4.4. That is the signature of misprediction rather than of memory
or of pass count — and it also corrects the guess this experiment started
from, which expected the alternating shape to sit with random. A period-2
pattern is trivial for a modern predictor; "varies" and "hard to predict" are
not the same property.

**The change** is three lines: swap unconditionally, then advance the store
index by the comparison rather than branching on it. It is correct for the
same reason the branchy version is — when the element does not belong left,
the store index points at another element that also does not, so the two are
interchangeable and the swap is harmless. The comparison must use the value
read *before* the swap. That the JIT then emits it without a branch was
checked rather than assumed, with `DOTNET_JitDisasm` on a verbatim copy of the
loop in a scratch project — `Partition` is `internal`, so it cannot be called
from one:

```text
vucomisd xmm0, xmm1
seta     r8b
movzx    r8, r8b
add      esi, r8d      ; storeIndex += (value < pivot)
```

RyuJIT on x64. The `netstandard2.0` assembly also ships to .NET Framework,
Mono and IL2CPP, where this is unverified — the correctness of the loop does
not depend on it, only the gain does. The trade is two stores every iteration
instead of two only when the swap fires: more memory traffic bought fewer
mispredictions, and only a measurement can say whether that pays.

**Before against after**, four interleaved campaigns in one window, same
corpus on both sides, `mse` and `mae` as controls because this change cannot
touch them:

| Operation, n = 1 000 000 | before (ms) | after (ms) | change |
| --- | ---: | ---: | ---: |
| `median_ae_n1000000_k2` | 16.201 | 9.941 | **−38.6%** |
| `mse_n1000000_k2` *(control)* | 5.051 | 5.037 | −0.3% |
| `mae_n1000000_k2` *(control)* | 5.080 | 5.034 | −0.9% |

Medians of four campaigns each, run after/before/after/before inside a single
window rather than campaign by campaign — the machine drifts more between
campaigns than the effect being measured. Spread within each side was at most
0.22 ms, and the controls moved under 1%, so the window is clean. The bar was
fixed before the number was known: 20% on `median_ae` or the change reverts.
It cleared it by a factor of nearly two, taking the selection phase from
10.8 ms to roughly 4.5 and `median_ae` from three times `mse` to twice.

Machine: Intel i7-4770S, four physical cores, `uptime`'s one-minute average
7.9 at the start of the window and 5.5 at the end — a desktop session ran
throughout, which is what the interleaving and the controls are for.

Reproduce it with the operation filter the same issue added, which measures
three rows instead of seventy-eight and turns an eight-minute-twenty campaign
into a twenty-one-second one:

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- \
    compare-metrics --only median_ae,mse,mae --shapes 1000000x2
```

A filtered run writes the same `bench/results/csharp-metrics.json` holding
fewer rows, so it stamps `filtered` into the file's metadata and
`bench/compare.py` refuses it. The merge gate above needs the whole matrix, and
a three-row file would otherwise have printed as a green one.

#### Vectorized accumulation (issue #321)

`R2` and `ExplainedVariance` got a `Vector<double>` accumulation in #127, gated by
[decision 0027](../decisions/0027-r2-and-explainedvariance-vectorize-only-a-single-output.md)
on `outputCount == 1 && Vector.IsHardwareAccelerated`. The shared walk under
`Outputs.WeightedMean` — which `mse`, `mae` and, through
[`MeanSquaredError.PerOutput`](../reference/metrics/regression/meansquarederror-peroutput.md),
[`RootMeanSquaredError`](../reference/metrics/regression/rootmeansquarederror.md) all take —
did not, and stayed a scalar loop.

The table above shows the consequence without naming it: at n = 1 000 000, `r2` does
**two** passes over the data and cost less than `mse` doing one.

Same machine and method as the table above (Intel i7-4770S, .NET 10.0.10), median of
3, before and after in the same window, with `r2` re-run beside them as a control
because nothing in this change touches it:

| Operation | before | after | change |
| --- | ---: | ---: | --- |
| `mse_n1000000_k2` | 5.385 ms | **3.261 ms** | **1.65× faster** |
| `mae_n1000000_k2` | 5.419 ms | **3.393 ms** | **1.60× faster** |
| `mse_n1000000_k10` | 5.380 ms | **3.234 ms** | **1.66× faster** |
| `mae_n1000000_k10` | 5.484 ms | **3.395 ms** | **1.62× faster** |
| `r2_n1000000_k2` — control | 4.428 ms | 4.476 ms | unchanged |
| `r2_n1000000_k10` — control | 4.397 ms | 4.410 ms | unchanged |

- **The control is the point of the table.** `r2` moving by 1% across the same window
  is what says the other four rows are the change and not the machine.
- **`mse` is now cheaper than `r2`**, which is the ordering the work justifies: one
  pass against two.
- **This machine is not where the gap was found.** The
  [nightly run](nightly_run) reported `mse` and `mae` at **0.60×** against numpy on a
  Xeon 6973P-C with AVX-512, below the gate this page sets, where the rows above have
  them at 1.04× and 1.12× on Haswell — the regression is visible only where numpy's
  wider registers pay off and a scalar loop's do not. **The ratio there is not
  measured here**, and 1.65× is the floor rather than the estimate: `Vector<double>`
  holds four lanes on AVX2 and eight on AVX-512, so the runner should gain more. The
  next nightly is what confirms that, not this table.
- **Four kernels deliberately keep the scalar loop.** The Tweedie deviances reach
  `Math.Pow` and the log errors `Math.Log`, neither of which `Vector<T>` offers;
  `MeanAbsolutePercentageError` divides and clamps and was not measured. Only the two
  kernels that are arithmetic all the way down implement the lane-wise form.

## Persisting an embedding index — the save path (issue #323)

The nightly reported `embedding_index_save` at **0.27× cpu** against `numpy.save`
where [`bench/README.md`](https://github.com/CyrilB1531/lodestar/blob/main/bench/README.md)
had published **1.13×**, Lodestar ahead. Neither figure was wrong: the C# side moved
by 6% between the two machines and numpy's by a factor of four, because `numpy.save`
writes a raw block — bandwidth-bound work a newer machine speeds up almost linearly —
where this artifact base64-encodes into JSON, whose cost per byte barely moves.

Under that, `Base64Numbers.WriteSingles` allocated a full copy of the vector block
and memcpy'd into it, so that an endianness swap could run in place. That swap is a
no-op on every platform .NET runs on, and the comment beside it said so. On a
little-endian machine the bytes to encode are the ones already in the span.

Intel i7-4770S, .NET 10.0.10, median of 3, before and after in one window, on a
machine under load from a parallel session — which is why these absolutes sit above
`bench/README.md`'s. `embedding_index_load` is re-run as the control: it reads the
same artifact through the same harness and this change does not touch it.

| Operation | before | after | change |
| --- | ---: | ---: | --- |
| `embedding_index_save`, wall | 15.807 ms | **10.579 ms** | **1.49× faster** |
| `embedding_index_save`, cpu | 17.139 ms | **11.729 ms** | **1.46× faster** |
| `embedding_index_load`, wall — control | 13.715 ms | 13.506 ms | unchanged |
| `embedding_index_load`, cpu — control | 15.739 ms | 15.527 ms | unchanged |

- **One allocation and one copy, on the largest block any artifact holds.** At the
  benchmark's size that is 15 360 000 bytes allocated on the large-object heap and
  copied, per save, discarded immediately after.
- **The bytes on the wire are unchanged**, and that is now pinned rather than
  argued: the little-endian path hands `WriteBase64String` the same span the copy
  used to hold, and `Base64NumbersTests` asserts the exact base64 of a known vector,
  computed from the IEEE-754 bits rather than captured from this build.
- **This does not settle the ratio the nightly reported.** Encoding is still the
  dominant cost and still scales with the machine differently from a raw block
  write; what it removes is the part that was never encoding at all. The load
  direction, further behind and for a decided reason, is
  [#324](https://github.com/CyrilB1531/lodestar/issues/324).

## Persisting an embedding index — the load path (issue #324)

The load direction is the furthest behind Python anything here publishes, and
[ADR 0011](../decisions/0011-persistence-format.md) priced it: base64 inside JSON
against `numpy.load`'s raw block. So the obvious question was whether to pay for a
second format. **The profile says the format is not where the time goes.**

[`EmbeddingIndex.Load`](../reference/embeddings/search/embeddingindex-load.md) instrumented
phase by phase, Intel i7-4770S, .NET 10.0.10,
median of the artifact's own 20 589 007 bytes:

| phase | cost | share |
| --- | ---: | ---: |
| reading the payload into a buffer | ~4.5 ms | ~29% |
| vector block — allocation **and** base64 decode | ~10.8 ms | ~50% |
| finite scan, SIMD | ~1.6 ms | ~9% |
| 10 000 ids | ~0.7 ms | ~5% |

- **Base64 decoding is close to free.** Measured by replacing the decode with a
  `memcpy` of the same byte count and re-running: **~1.3 ms**, which is what the
  decode costs *over* moving the bytes at all. The other ~9.5 ms of that row is the
  allocation.
- **So the answer to ADR 0011's open question is that its door is not the one to
  open.** A binary format would remove 5.2 MB of base64 expansion from a path
  spending its time on allocation and page commit, not on decoding.

What the allocation was doing that it needed not: the runtime zeroes an array before
handing it over, and both large buffers here are overwritten in full immediately —
the payload by the stream, the vector block by the decoder. That is 36 MB of
large-object-heap writes per load that nothing reads.

Median of 3, before and after interleaved in one window, with `tfidf_save` as the
control since it writes and does not read:

| Operation | before | after | change |
| --- | ---: | ---: | --- |
| `embedding_index_load`, wall | 14.729 ms | **12.510 ms** | **1.18× faster** |
| `embedding_index_load`, cpu | 16.868 ms | **14.341 ms** | **1.18× faster** |
| `tfidf_load`, cpu | 7.320 ms | 7.654 ms | 0.96× |
| `tfidf_save`, cpu — control | 2.380 ms | 2.363 ms | unchanged |

- **`tfidf_load` does not gain, and is not expected to.** Its buffers are far below
  the large-object heap, where `GC.AllocateUninitializedArray` skips no zeroing at
  all. The 0.96× sits inside the spread that row shows between windows — it has read
  6.9 to 7.9 ms across this page's runs — so it is reported rather than claimed as a
  regression.
- **1.18× is well under what the allocation phase costs**, and that gap is the
  finding to carry forward: removing the zeroing returns only part of it, so most of
  that phase is the operating system committing pages on first touch, which no
  allocation strategy avoids. **Moving fewer bytes is the remaining lever**, which is
  [#336](https://github.com/CyrilB1531/lodestar/issues/336) — and its own measurement
  narrows that further.

## The blocked LCS kernel, and the call it made per character (issue #320)

The nightly put `Indel` **2.40× behind rapidfuzz at 512** while `Levenshtein` sat at
1.08×, on the same run and the same corpus. Both take the blocked bit-parallel path
and both pay the same equality table, so a cost they share cannot explain a gap only
one of them has. The asymmetry is sharper still read the other way: rapidfuzz's Indel
is **3.03×** faster than its own Levenshtein where ours was **1.37×** faster than
ours. The cheaper recurrence was not being cashed in.

`BitParallelLcs.TryBlocked` called `Advance(…)` once per text character.
`Myers.TryBlocked` writes its block loop out by hand, and says why in the file's own
header — *"It is also the hot path: helper calls here cost measurably."* The LCS
kernel, written later in #273, did not inherit that.

Two changes, both local:

- `Advance` is `AggressiveInlining`, which is what `Myers.TryBlocked` achieves by not
  having a helper at all;
- the `peqBase >= 0` test leaves the inner loop. A text character outside Latin-1
  makes every `u` zero, so `sum == value`, `difference == value`, carry and borrow
  stay clear and `v[b] = value | value`. **The whole pass is a no-op**, so the call is
  skipped rather than made with an empty row.

Intel i7-4770S, .NET 10.0.10, before and after **interleaved, four replications**,
with `Levenshtein` as the control — it takes Myers, which nothing here touches.
Baseline is post-#334, not the nightly's: that run predates both #299 and #334 and
was taken on a different machine.

| | before | after | change |
| --- | ---: | ---: | --- |
| `Indel`, length 512 | 13 926.5 ns | **12 631.4 ns** | **1.10× faster** |
| `Indel`, length 128 | 1 210.7 ns | 1 215.8 ns | unchanged |
| `Levenshtein`, 512 — control | 19 925.5 ns | 20 333.9 ns | 0.98× |
| `Levenshtein`, 128 — control | 1 781.2 ns | 1 814.1 ns | 0.98× |

- **The medians understate what the runs show.** At 512 the four before values are
  13 856.8 / 13 911.1 / 13 941.9 / 14 259.2 and the after values 12 572.2 / 12 599.0 /
  12 663.8 / **15 235.6**. Three of four sit below *every* before value with no
  overlap; the fourth is a contaminated replication, which also carries the only
  outlier at 128. It is left in the median rather than dropped.
- **The control drifts 2% the wrong way**, so if the alternation biases anything it
  biases against the change.
- **Nothing at 128, and that is the mechanism.** A 110-character pattern spans two
  64-bit blocks against eight at 512, so inlining a loop of two iterations saves
  little.
- **This does not close the gap, and the remainder is not incidental.** At 12.6 µs
  against rapidfuzz's 2.9 the kernel is still ~4.3× behind. The blocked path's rented
  table is cleared per call — 16 KB at this size, about 500 ns of 12 600, or 4% — so
  that is not where the rest is either. What remains is algorithmic and wants its own
  measurement before its own lot.

## The borrow the LCS recurrence never needed (issue #357)

[#320](https://github.com/CyrilB1531/lodestar/issues/320) took the mechanical half of
the blocked LCS kernel — a helper called once per text character — and left the
residual gap open as [#357](https://github.com/CyrilB1531/lodestar/issues/357),
recording that the table's per-call clear was only 4% of the call and that what
remained was algorithmic. It was, and it was one line.

`Advance` threaded two chains between words: the addition's carry and the
subtraction's borrow. **The borrow was provably always zero.** `u` is `v & peq`, so its
set bits are a subset of `v`'s, and subtracting a bit-subset never borrows —
`v - u` is exactly `v & ~u`. Checked against 200 000 random 64-bit draws before a line
was changed, and the property tests #273 added against the dynamic program are what
would have caught the reasoning being wrong.

**The asymmetry is the LCS recurrence's own.** `Myers` carries substitution and its
subtraction has no such shape, which is why its blocked loop legitimately keeps both
chains — and why our blocked Myers sat at parity with rapidfuzz's while our blocked
LCS did not. One serial dependency too many, in the loop that runs `text.Length ×
blocks` times.

Intel i7-4770S, four replications, before and after interleaved, `Levenshtein` as the
control since it takes Myers:

| | before | after | change |
| --- | ---: | ---: | --- |
| `Indel`, 128 | 1 293.6 ns | **906.2 ns** | **1.43× faster** |
| `Indel`, 512 | 13 811.1 ns | **8 837.0 ns** | **1.56× faster** |
| `Levenshtein`, 128 — control | 1 778.5 ns | 1 768.7 ns | 1.006× |
| `Levenshtein`, 512 — control | 20 422.0 ns | 20 218.6 ns | 1.010× |

**No overlap between the two series, on either bucket.** At 512 the before values are
12 403.5 / 12 491.5 / 15 130.7 / 15 210.7 and the after values 8 704.3 / 8 832.8 /
8 841.1 / 8 881.9 — and the after values sit inside 2% of each other where the before
ones spread over 23%. Removing a serial dependency steadies the measurement as well as
shortening it, which is what the mechanism predicts.

Unlike #320 this moves the 128 bucket too, and for the reason #320 could not: that lot
amortised a call over the blocks it covered, so two blocks gained little where eight
gained more. This removes a dependency from the loop itself, so it pays wherever the
loop runs.

**`UInt128` for the carry was tried and is slower — do not retry it.** What remains
between this kernel and rapidfuzz is about 1.8 cycles per block update (6.69 against
4.92 on an i7-4770S at length 512), and the shape of the loop suggests the carry: C++
reaches `_addcarry_u64` and emits one `ADC` where this reconstructs the carry with two
comparisons and an `or`. Writing it as `UInt128 wide = (UInt128)value + u + carry`,
which reads better and should let the JIT emit `ADD`/`ADC`, measured **0.914× at 512
and 0.957× at 128** — four replications interleaved, control steady, no overlap
between the series. The conversions and the shift cost more than the comparisons they
replace. The gap is what the JIT will not emit, not something the source is holding
wrong.

Against rapidfuzz 3.14.5, both sides re-run in one window after the change:

| length | rapidfuzz | Lodestar | |
| ---: | ---: | ---: | --- |
| 8 | 128.5 ns | **37.2 ns** | **3.45× C# faster** |
| 32 | 203.2 ns | **129.3 ns** | **1.57× C# faster** |
| 128 | 585.1 ns | 895.4 ns | 1.53× Python faster |
| 512 | 6 504.9 ns | 8 969.7 ns | **1.38× Python faster** |

The two long buckets were 2.03× and about 2.1× behind before this lot. **What is left
is no longer a factor of two.** Whether the remainder is worth a third lot is a
question for a measurement, not for this page.

## Compressing an index (issue #378)

The artifact is base64 inside JSON, which spends eight bits to carry six, so it is
about 1.33x the raw block. Deflate takes that back almost exactly — base64 is the
one expansion a general-purpose coder undoes perfectly. The question was never
whether the size comes back. **It was what the time costs**, on a path
[#323](https://github.com/CyrilB1531/lodestar/issues/323),
[#324](https://github.com/CyrilB1531/lodestar/issues/324),
[#336](https://github.com/CyrilB1531/lodestar/issues/336) and
[#377](https://github.com/CyrilB1531/lodestar/issues/377) spent four lots making
fast.

A synthetic 4 000 × 384 index through the real `Save` and `Load`, Intel i7-4770S,
.NET 10.0.10 — warmed, median of 7, the five modes interleaved in one window so the
rows are comparable to each other:

| | bytes | × size | save | × save | load | × load |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| plain | 8 231 006 | 1.000 | 10.2 ms | 1.00 | 7.9 ms | 1.00 |
| gzip `Fastest` | 6 257 079 | 0.760 | 270.8 ms | **26.67×** | 56.5 ms | 7.19× |
| gzip `Optimal` | 6 151 764 | 0.747 | 382.1 ms | **37.62×** | 46.6 ms | 5.92× |
| brotli `Fastest` | 6 074 449 | **0.738** | 37.4 ms | 3.68× | 40.8 ms | 5.19× |
| brotli `Optimal` | 6 069 780 | 0.737 | 122.1 ms | 12.02× | 38.3 ms | 4.87× |

- **The size claim holds exactly.** 0.747 × 1.333 = 0.996 of the raw block, which is
  the floor #378 predicted from the other direction. Level 9 gives the same bytes as
  level 6; there is nothing to tune.
- **Deflate is dominated on all three axes.** brotli `Fastest` is smaller than gzip
  at any level, seven times cheaper to write and cheaper to read. `BrotliStream` does
  not exist on `netstandard2.0`, though, which is why the documented recipe is gzip:
  a recipe that works on one of two target frameworks is not one this project can
  publish.
- **And the price is the whole answer.** The cheapest compression available
  multiplies the load by 5.19 — spending, several times over, what four lots
  returned — to buy 26% of a disk.

So **the library does not compress, and the caller can.** Wrapping the stream works
on both sides today and costs no API:
[the embeddings guide](embeddings.md#compressing-the-artifact) has the recipe,
[ADR 0044](../decisions/0044-compression-belongs-to-the-caller.md) the decision and
its loser. `bench/compare-persistence` now carries `embedding_index_save_gzip` and
`embedding_index_load_gzip` beside the plain rows, against numpy's
`savez_compressed`, so the trade is re-measured rather than remembered.

**The corpus is harsher than the synthetic index, and says so.** The nightly's own
rows, 10 000 × 384 on a hosted runner — ratios only, per that page's warning:

| operation | C# cpu | bytes | Python cpu | Python bytes |
| --- | ---: | ---: | ---: | ---: |
| `embedding_index_save` | 5.949 ms | 20 589 007 | 1.337 ms | 15 360 128 |
| `embedding_index_save_gzip` | 456.995 ms | 15 251 458 | 638.992 ms | 14 022 374 |
| `embedding_index_load` | 5.519 ms | 20 589 007 | 1.327 ms | 15 360 128 |
| `embedding_index_load_gzip` | 81.774 ms | 15 251 458 | 72.368 ms | 14 022 374 |

**0.741× the size for 76.8× the save and 14.8× the load**, against 37.62× and 5.92×
for `Optimal` on the 8 MB synthetic index above. The price grows with the artifact,
which is the opposite of what would make it worth paying — the indexes big enough for
26% of a disk to matter are the ones where compressing costs the most.

Two things the pair says that the plain rows cannot:

- **Compressed, we are ahead of numpy on the write and level with it on the read** —
  1.40× on `savez_compressed`, 0.88× coming back. The deflate coder is the same on
  both sides, so what is being compared is what each side hands it.
- **Compression closes most of the format gap.** Uncompressed, the artifact is 1.34×
  numpy's block, the expansion [ADR 0011](../decisions/0011-persistence-format.md)
  priced. Compressed, it is 1.09×. The base64 is nearly all of the difference, and a
  binary format would buy back an eighth of what a general-purpose coder already does.

**Bytes are a published number now.** `Harness.Measure` recorded time only, so
[#100](https://github.com/CyrilB1531/lodestar/issues/100)'s size figures were taken
by hand and could not be re-checked. Every persistence row carries `artifact_bytes`
on both sides, and the comparison prints it next to the time — which is the only way
a compressed row reads honestly.

## Multiclass ROC-AUC, sequential against parallel (issue #86)

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- roc-parallel
```

### Read the axis before reading the table

**The axis here is elapsed time, and processor time rises.** That is what
spending cores means rather than a fault in the measurement, so both columns are
printed side by side for every cell and neither is dropped. Concretely, on
`ovr_n100000_k10` at eight workers: processor time goes from 75.993 ms to
142.224 ms while elapsed time falls from 75.991 ms to 26.648 ms. The work got
larger and the wait got shorter. A caller who is paying for CPU seconds should
read the second column and may well conclude the default is right for them.

**The table immediately above this one is on a different axis, and the two must
not be paired.** Issue #61's comparison against scikit-learn is a *processor
time* gate — every row at or above 1×, which is the property that survives being
run on a busy machine — and it stays one. This table asks a different question of
the same code: how long the caller waits. Setting a processor-time ratio beside
an elapsed-time ratio and reading down the page is the easiest available way to
mislead a reader, so the axes are named at both tables rather than inferred from
the column headers.

**One pair of numbers is on the same axis, and it is the one to watch.** Issue #61's
table reports `roc_auc_ovr_macro_n100000_k10` at **88.385 ms wall** (91.396 ms
processor); the table further down this page reports the same operation at `dop=1`
as **75.991 / 75.779 ms elapsed**. Both are elapsed milliseconds, so the axis
warning just given does not cover them — and they are still not the same
measurement. Three things differ, and all three matter:

- **Different input.** Issue #61 scores the committed corpus file
  `bench/corpus/metrics/metrics_n100000_k10.json`; this bench generates a seeded
  separable problem in process (`Random(86)`, rows normalised). How separable the
  problem is decides how many equal scores `BinaryRoc` groups into one threshold,
  which changes the work per curve.
- **Different machine load.** Issue #61's pass was taken at a one-minute load of
  1.52. These figures were taken between 2.31 and 4.01 — the Conditions section
  below gives the three readings.
- **Different code on the sequential path.** The sequential multiclass drivers were
  rewritten for this issue: the per-curve buffers now come from one pooled
  `BinaryRoc.Scratch` reused across every class, where Issue #61's build allocated
  two fresh arrays per class inside `BinaryRoc.Score`. Issue #61's 88.385 ms and
  this table's 75.991 ms were not produced by the same implementation, and nothing
  here measures how much of the gap that accounts for.

So the arithmetic a reader is most likely to reach for is the one nobody should
publish: dividing this table's best `ovr_n100000_k10` cell (26.648 ms elapsed) into
Issue #61's Python column for that row (250.400 ms wall) yields a tidy-looking
"9.4× faster than scikit-learn" that no single run measured — two windows, two
inputs, two builds. A cross-language figure for the parallel path would have to be
measured as one pass with both sides on the same input, and it has not been.

### Conditions

Intel i7-4770S, **4 physical cores / 8 logical threads**, Ubuntu 24.04,
.NET 10, one sitting. Inputs generated in-process from a fixed seed (`Random(86)`,
rows normalised to sum to 1) — no shared corpus, because both sides of this
comparison are C#. Each cell is the best of five repeats over a 0.5 s minimum,
with wall and processor time taken from the same repeat.

**Two back-to-back passes over all 24 cells, both published.** One-minute load
average, from `uptime`: **2.31 before the first pass, 3.32 between them, 4.01
after the second.** The rise across the window is the runs themselves — this
workstation does not reach that on its own — and it is why both passes are shown
instead of a mean, following `bench/README.md`'s "read the pairs, not the means"
rule. The two passes are unusually close here: the sequential baselines of the
two heaviest cells agree to 0.28% and 0.45%, against the up-to-15% dispersion
that same section documents for this machine. The widest disagreement anywhere in
the 24 pairs is 6.45%, on `ovo_n1000_k10` at four workers, where the whole cell
is under 0.3 ms.

Because the load climbed from 2.31 to 4.01 while these figures were taken, the
table is comparable **to itself** — one window, all four worker counts of a shape
measured next to each other — and **not** to the scikit-learn table above, which
was taken at a one-minute load of 1.52. Those absolute milliseconds and these
were not measured on the same machine state.

### The 24 cells

Elapsed and processor milliseconds per operation, as `pass 1 / pass 2`. The
`cpu ÷ elapsed` column is what the harness prints as `(…x cores)` — how many
cores' worth of work the call consumed. The speed-up column is elapsed time
divided into the same pass's own `dop=1` row.

| Operation | dop | elapsed ms | processor ms | cpu ÷ elapsed | speed-up vs dop=1 |
| --- | ---: | ---: | ---: | ---: | ---: |
| `ovr_n1000_k10` | 1 | 0.466 / 0.470 | 0.467 / 0.470 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovr_n1000_k10` | 2 | 0.294 / 0.301 | 0.647 / 0.672 | 2.20 / 2.23 | 1.59 / 1.56 |
| `ovr_n1000_k10` | 4 | **0.240 / 0.238** | 0.958 / 0.944 | 3.98 / 3.96 | **1.94 / 1.97** |
| `ovr_n1000_k10` | 8 | 0.249 / 0.244 | 1.066 / 1.036 | 4.28 / 4.24 | 1.87 / 1.93 |
| `ovo_n1000_k10` | 1 | 0.745 / 0.741 | 0.745 / 0.741 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovo_n1000_k10` | 2 | 0.438 / 0.439 | 0.957 / 0.943 | 2.19 / 2.15 | 1.70 / 1.69 |
| `ovo_n1000_k10` | 4 | **0.297 / 0.279** | 1.215 / 1.169 | 4.09 / 4.19 | **2.51 / 2.66** |
| `ovo_n1000_k10` | 8 | 0.423 / 0.421 | 1.627 / 1.603 | 3.84 / 3.81 | 1.76 / 1.76 |
| `ovr_n100000_k5` | 1 | 36.770 / 36.633 | 36.770 / 36.635 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovr_n100000_k5` | 2 | 23.535 / 23.444 | 38.852 / 38.905 | 1.65 / 1.66 | 1.56 / 1.56 |
| `ovr_n100000_k5` | 4 | 17.777 / 17.656 | 45.709 / 46.358 | 2.57 / 2.63 | 2.07 / 2.07 |
| `ovr_n100000_k5` | 8 | **14.380 / 14.405** | 56.961 / 57.354 | 3.96 / 3.98 | **2.56 / 2.54** |
| `ovo_n100000_k5` | 1 | 55.121 / 54.865 | 55.117 / 54.860 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovo_n100000_k5` | 2 | 29.305 / 29.674 | 56.743 / 57.214 | 1.94 / 1.93 | 1.88 / 1.85 |
| `ovo_n100000_k5` | 4 | 24.177 / 24.151 | 60.830 / 60.723 | 2.52 / 2.51 | 2.28 / 2.27 |
| `ovo_n100000_k5` | 8 | **17.331 / 17.535** | 91.430 / 91.555 | 5.28 / 5.22 | **3.18 / 3.13** |
| `ovr_n100000_k10` | 1 | 75.991 / 75.779 | 75.993 / 75.785 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovr_n100000_k10` | 2 | 40.242 / 40.163 | 77.532 / 77.425 | 1.93 / 1.93 | 1.89 / 1.89 |
| `ovr_n100000_k10` | 4 | 34.997 / 35.644 | 94.273 / 91.616 | 2.69 / 2.57 | 2.17 / 2.13 |
| `ovr_n100000_k10` | 8 | **26.648 / 26.379** | 142.224 / 140.712 | 5.34 / 5.33 | **2.85 / 2.87** |
| `ovo_n100000_k10` | 1 | 127.375 / 126.810 | 127.377 / 126.801 | 1.00 / 1.00 | 1.00 / 1.00 |
| `ovo_n100000_k10` | 2 | 64.317 / 64.250 | 123.433 / 123.341 | 1.92 / 1.92 | 1.98 / 1.97 |
| `ovo_n100000_k10` | 4 | **37.162 / 36.875** | 133.183 / 131.681 | 3.58 / 3.57 | **3.43 / 3.44** |
| `ovo_n100000_k10` | 8 | 37.284 / 37.457 | 187.662 / 184.325 | 5.03 / 4.92 | 3.42 / 3.39 |

Bold marks the fastest worker count for each shape in elapsed time. Nothing was
skipped: one-vs-one at n=100 000, k=10 is 45 pairs and 90 curves, the heaviest
cell in the matrix, and a single sequential call is about 127 ms — well inside the
bench's 60-second patience for that cell.

### What the numbers say

**At k=10 and n=100 000 the opt-in is worth 2.85×–2.87× on one-vs-rest and
3.43×–3.44× on one-vs-one**, on four physical cores. One-vs-rest wants all eight
logical threads to get there (26.6 / 26.4 ms); one-vs-one gets there at four
(37.2 / 36.9 ms) and eight buys it nothing.

**At k=5 the ceiling is lower, and part of it is arithmetic.** One-vs-rest tops
out at 2.56× / 2.54×: five classes are five independent units of work, so the
per-index loop is clamped to five workers however many the caller asks for, and
five pieces cannot be spread evenly over four cores — one core does two while
three do one. One-vs-one at the same shape has ten pairs to hand out and reaches
3.18× / 3.13×, the best ratio in the table at n=100 000.

**At n=1000 the opt-in is a gain, not a cost — which is not what was expected.**
The design brief assumed the small-input row would be a dispatch overhead to
justify. It is a doubling: one-vs-rest 0.466 / 0.470 ms → 0.240 / 0.238 at four
workers (1.94× / 1.97×), one-vs-one 0.745 / 0.741 → 0.297 / 0.279 (2.51× /
2.66×). Ten classes are ten independent sorts even when each sorts only a
thousand values, and the copy the parallel path pays for is 1000 × 10 doubles —
80 KB, which fits in L2. This is why the option has no internal size threshold:
a crossover constant calibrated for "small inputs" would have thrown this away.

**`dop=8` loses to `dop=4` on three of the six shapes, in both passes.**
`ovr_n1000_k10` (0.249 / 0.244 against 0.240 / 0.238), `ovo_n1000_k10` (0.423 /
0.421 against 0.297 / 0.279 — 42% / 51% slower) and `ovo_n100000_k10` (37.284 /
37.457 against 37.162 / 36.875). This machine has **4 physical cores and 8
logical threads**: past four workers the extra ones share execution units with a
sibling rather than adding any, while still adding scheduling and cache
pressure. The `ovo_n1000_k10` row is the clearest case, and it is a large
regression, not a rounding error.

The practical consequence is worth stating plainly, because
`MaxDegreeOfParallelism`'s own documentation offers `Environment.ProcessorCount`
as the way to ask for every core: **on a hyperthreaded machine that property can
be the wrong number, and slower than half of it.** `ProcessorCount` is 8 here.
Four is the better setting on half these shapes and never much worse on the rest.
There is no recommended value in the library because there is no value that is
right on every machine — measure your shape on your hardware, which is what this
bench mode exists for.

**Where the ceiling comes from.** Nothing here approaches 4× on four cores, and
three things account for it, in decreasing order of confidence. `ValidateRowSums`
walks all `samples × classes` scores on the calling thread before any dispatch —
it stays sequential so its message can name the *first* bad row — and no worker
count shortens it. The parallel path copies its inputs, `samples × classes × 8`
bytes for the transposed score matrix, about 8 MB at n=100 000, k=10. And the
per-index loop can only be as parallel as it has indices, which is what caps
one-vs-rest at k=5. This run does not time those three separately, so the split
between them is not quantified here; what is measured is the total, and it is in
the table.

The reasoning behind the opt-in default, the absent `-1` sentinel and the absent
threshold is in
[`../decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md`](../decisions/0018-multiclass-roc-auc-parallelism-is-opt-in.md).
