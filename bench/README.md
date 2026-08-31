# Benchmarks

Three complementary tools.

## 1. Intra-C# micro-benchmarks (BenchmarkDotNet)

Rigorous per-method measurement, for optimizing the C# implementation itself:

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- --filter '*Levenshtein*'
```

### The Levenshtein corpora, and which one reaches what

There are two classes, and the difference between their corpora is the whole
point of having both.

`LevenshteinBenchmarks` draws both operands from `"abcdefghijklmnopqrstuvwxyz "`.
That is the near-duplicate matching case, and it is ASCII — so its
`Distance_CodePoint` row decodes into a sequence identical to the UTF-16 one and
measures the decode over a 27-symbol alphabet. It is not a measurement of the
code-point mode.

`LevenshteinCodePointBenchmarks` is that measurement (#208). Both operands are
drawn from U+1F300..U+1FAFF, so every character is a surrogate pair and the two
readings genuinely differ, which is the case
[decision 0002](../docs/decisions/0002-unicode-comparison-unit.md) points a
caller at. It carries a second parameter the other does not:

- `Distinct = 32` — the pattern fits the 255-symbol dense alphabet at every
  length, so the bit-parallel path applies throughout;
- `Distinct = 512` — the pattern outgrows it as `Length` rises, and the
  implementation falls back to the dynamic program.

Both are run because reporting only the first would publish the fast path's
number as though it were the mode's.

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- --filter '*LevenshteinCodePoint*'
```

### Where the bit-parallel gate belongs, and why a benchmark cannot say

`MyersGateBenchmarks` and `LcsGateBenchmarks` parameterise the differing middle
directly, so after `Affixes.Trim` the pattern is exactly `Band` long and the
dynamic program runs beside the kernel as the baseline. They answer *how much*
the kernel wins by at a given band.

Each runs that band twice, over two alphabets of 27 symbols: the Latin one every
band used before, and a CJK one above `U+00FF`. Below that boundary the kernels
index their 256-entry equality table directly; above it a pattern took the DP
until #302 and #382 gave it a side table beside the dense one. So `Dp_Cjk` is what
a refusal costs and `Kernel_Cjk` is what the side table costs, on shapes the Latin
rows measure in the same process — `BandedPair` takes the alphabet as a parameter
so that the alphabet is the only thing between the two (#383).

**A CJK row is only a CJK measurement if the band reaches the kernel**, and a
timing cannot tell you it did: both routes return the same number. The gate is 8,
so bands 4 and 6 take the DP on either alphabet by design, and
`WideAlphabetKernelTests` asserts the route for every band at or above it rather
than leaving it to be read off a ratio.

They cannot answer *where to put the gate*, and it is worth being explicit about
why, because the shape invites the mistake: below the gate the dispatch sends
both rows to the DP, so the ratio is 1 and the crossing is invisible exactly
where you want to read it. The kernels are `internal` and the constants private,
so no benchmark reaches around the dispatch either.

What answers it is sweeping the constant and reading the committed corpus end to
end — the metric that is actually reported, on the input that is actually shipped.
Edit `MyersMinPatternLength` (or `BitParallelMinPatternLength`, or
`MyersMinCodePointPatternLength`), rebuild, and run the cross-language harness of
section 3 at each value. `BitParallelLcs.MaxHeldPattern` — the longest pattern for
which the LCS kernel holds its equality table rather than letting `stackalloc` zero
one — is swept the same way, and #301 did:

```bash
dotnet build bench/Lodestar.Text.Benchmarks -c Release
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks --no-build -- compare
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks --no-build -- compare-indel
```

Three things this corpus will not tell you, all of which cost #208 time:

- **Read every bucket, not the one being tuned.** The length-32 bucket keeps
  improving as the gate falls, and the length-8 bucket falls off a cliff below 2.
  A sweep that reports only the bucket it is optimising will happily recommend 1.
- **The crossing depends on the text length, not just the pattern length.** Myers
  costs `setup + O(n)` where the DP costs `O(m·n)`, so they meet at
  `m ≈ 1 + setup/n`. A gate is one number for every `n`, so calibrate it on the
  shortest texts — the regime where being wrong is expensive.
- **The corpus had a hole between 2 and 7, and #409 filled it.** Its scattered
  length-8 bucket trims to a pattern of 0 or 1, in either alphabet — the 10% edit
  rate produces that, not the alphabet — so every conclusion in that range once
  rested on the length-32 bucket alone, whose median pattern is 16. The twenty
  banded buckets have a pattern of exactly their band, 2 through 16, and that is
  what showed the shared gate of 8 to be wrong in three of its four cases.
- **With bands, a gate question needs two runs and not a sweep.** At a gate of 2
  every band takes the kernel and at 17 every band takes the dynamic program, so one
  pair of runs prices both routes over the same pairs and the crossing is where the
  ratio reaches 1. The catch is that the two readings come from separate builds, so
  drift enters the ratio where BenchmarkDotNet's in-process baseline would not —
  about 10% here, which pins a separation of four bands and not a boundary band.
- **Never sum ns/pair across buckets to score a gate value.** The 512 bucket is
  roughly 95% of any such total and no candidate gate can touch it, so the sum
  reports that bucket's run-to-run noise as a result about the constant, and picks a
  different winner than the one bucket that can see the gate.
- **Sweep in both directions.** Each value needs its own build, so the readings are
  taken in sequence and machine drift maps onto the swept axis. Two passes in
  opposite order put that drift on both ends instead; #407's agreed to 5.3%, against
  about 12% for the gate benchmarks' short job.

### Measuring a before against an after, and reading the generated code

Two instruments, both of which failed silently before they worked, in #411.

**Nothing may compile inside the window.** Rebuilding between every measurement leaves
`VBCSCompiler` burning about a quarter of a core into the run that follows: two
executions of *identical* code differed by up to 15.2%, which was the size of the effect
being looked for. Publish each state once into its own directory, `dotnet build-server
shutdown`, then alternate only the runs — which also makes six rounds cheaper than two
were.

```bash
dotnet publish bench/Lodestar.Text.Benchmarks -c Release -o /tmp/state-before
# ... change the source, publish again to /tmp/state-after ...
dotnet build-server shutdown
/tmp/state-before/Lodestar.Text.Benchmarks compare     # alternate, several rounds
/tmp/state-after/Lodestar.Text.Benchmarks compare
```

Read each row against the spread of the runs of one state, not against a remembered
figure: that spread is the row's own noise floor, and on this workstation it is 6% to
25% depending on the bucket.

**Whether a change costs the hot path is a question about generated code**, answered by
diffing the JIT's output rather than by timing a loaded machine (#302, #411):

```bash
DOTNET_JitDisasmSummary=1 DOTNET_TieredCompilation=0 ./driver     # what got compiled
DOTNET_JitDisasm='SubsequenceLengthChars DistanceChars TrySingleWord'   DOTNET_TieredCompilation=0 ./driver > listing.txt
```

Three traps, each of which produced a diff that read as success:

- **Run the built binary, not `dotnet run`** — the launcher swallows the JIT's listing,
  and a diff of two empty captures reports "identical".
- **A method the driver never reaches is never compiled, so never listed.** Run
  `DOTNET_JitDisasmSummary=1` first and check the method you care about is in it. An
  empty capture is a failed measurement; make the script refuse it rather than diff it.
- **The driver's operands must reach the code being compared.** A pair differing in one
  position trims to a pattern of one character and takes the dynamic program, so the
  listing describes a route the change does not touch. Move both ends apart, the way
  `BandedPair` does.

## 2. net10 vs netstandard2.0 — what the broad-reach target costs

`netstandard2.0` is a contract, not a runtime: nothing executes *on* it. What can
be measured is the **netstandard2.0-compiled assembly against the net10-compiled
one, both hosted on .NET 10** — same JIT, same GC, so any difference comes from
the libraries' own conditional code paths.

`Lodestar.NetStandard.Benchmarks` links the *same* benchmark sources as the suite
above (`<Compile Include="../Lodestar.Text.Benchmarks/…" />`, never a copy) and
only swaps which assemblies it references.

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks        -- --filter '*VectorMath*' --inProcess
dotnet run -c Release --project bench/Lodestar.NetStandard.Benchmarks -- --filter '*VectorMath*'
```

### Measured

Intel i7-4770S, .NET 10.0.10, default job. **Two runs per side, and both are
shown**, interleaved net10 → netstandard2.0 → net10 → netstandard2.0 so that any
drift in machine state lands on both columns rather than on whichever one
occupies the second half of the window.

| Method | Dim | net10 | netstandard2.0 | cost |
| --- | --- | --- | --- | --- |
| `Dot` | 384 | 74.8 / 79.3 ns | 330.3 / 334.0 ns | 4.2×–4.4× |
| `Dot` | 768 | 122.9 / 128.7 ns | 648.5 / 661.1 ns | 5.1×–5.3× |
| `Dot` | 1024 | 173.9 / 177.1 ns | 896.7 / 883.8 ns | 5.0×–5.2× |
| `L2Norm` | 384 | 69.6 / 69.6 ns | 322.9 / 319.1 ns | 4.6× |
| `L2Norm` | 768 | 95.5 / 109.9 ns | 650.1 / 651.3 ns | 5.9×–6.8× |
| `L2Norm` | 1024 | 138.8 / 138.3 ns | 887.4 / 875.5 ns | 6.3× |

That is the `Vector<T>` SIMD path against the scalar fallback, which is the one
place the two builds deliberately differ — the span-based `Vector<T>` constructor
is net-only. Everything else compiles to equivalent IL, so a difference elsewhere
means something changed and is worth investigating. `L2Norm` gains more than
`Dot` because it reads one array instead of two, so the vector path is not
sharing load bandwidth with a second stream; on netstandard2.0 the two methods
cost the same, which is what two equivalent scalar loops should do.

**Read the pairs, not the means.** Every figure above was reported by
BenchmarkDotNet with an interval of about ±1 ns, and the two runs of the same
binary still differ by up to 6% (`Dot` at 384) and once by 15% (`L2Norm` at
768, 95.5 ns then 109.9). The interval describes dispersion inside one process
and says nothing about reproducibility across processes. The ratios are far more
stable than either column, which is the argument for quoting them and not the
absolute numbers.

The machine was not idle: the one-minute load average was 4.8–5.5 on 8 logical
cores at the start of all four runs, against the 1.9–2.3 floor this workstation
reaches when only its desktop client, editor and browser are up. That inflates
both columns and is the reason the pairs disagree as much as they do; it does
not favour either side, since the runs alternate.

That load is not an accident of scheduling, and the earlier figures cannot be
reproduced by re-running these commands: the editor's language servers and the
assistant session that drives the run are themselves part of it. So the table
above is internally comparable — one window, alternating sides, both columns
paying the same tax — and **not** comparable to figures taken on this machine in
a quieter state. Compare ratios across such sets, never absolutes.

### Three things keep this honest

`SetTargetFramework` on the `ProjectReference` is **not sufficient on its own**.
BenchmarkDotNet's default toolchain generates and builds its own project per run,
which re-resolves the reference and restores the net10 build — both suites then
measure the same assemblies while reporting plausible numbers. An earlier version
of this comparison showed a 4% difference for exactly that reason.

So the suite pins the in-process toolchain, removing the generated project, and
asserts what it actually loaded before running anything:

```text
// Lodestar.Text: .NETStandard,Version=v2.0
// Lodestar.Embeddings: .NETStandard,Version=v2.0
```

A mismatch exits non-zero rather than producing numbers. Sanity-check any result
against physics too: a scalar dot product over 1024 floats is latency-bound near
1000 ns, so a result close to the SIMD figure means the isolation broke again.

**And `--inProcess` on the net10 command is the third thing, not decoration.**
The netstandard2.0 project pins `InProcessEmitToolchain` because it has to; the
net10 command would otherwise use the default out-of-process toolchain, and a
ratio taken across those two harnesses mixes the target framework with the
harness that measured it. This section published such ratios until issue #87 —
4.6×, 5.2× and 5.6× — and the flag is what removes the confound.

**What the confound is worth here, measured rather than asserted.** The net10
side was run a third time in the same window, minutes after the four runs above
and on the same loaded machine, with the flag left off:

| Dim | net10, in-process | net10, out-of-process | matched pairing | mixed pairing |
| --- | --- | --- | --- | --- |
| 384 | 74.8 / 79.3 ns | 69.9 ns | 4.2×–4.4× | 4.7×–4.8× |
| 768 | 122.9 / 128.7 ns | 123.0 ns | 5.1×–5.3× | 5.3×–5.4× |
| 1024 | 173.9 / 177.1 ns | 159.6 ns | 5.0×–5.2× | 5.5×–5.6× |

The out-of-process harness reports the *same binary* as up to 10% faster on
`Dot` and 24% faster on `L2Norm` (56.1 ns against 69.6 at 384). Pairing that
column against the in-process netstandard2.0 one — the shape of the old pair of
commands — inflates the gap by up to 0.5× at 384 and 1024, while at 768 the two
pairings agree, which is why the defect was not visible from the numbers alone.

That is the whole of what this control establishes, and it is deliberately not
compared against the figures this section used to publish. Those came off this
same machine, but in a quieter state than a run driven from an editor session
can recreate — the session is part of the load it would have to remove. Two sets
of absolute figures taken under loads that differ by a factor of two support no
inference between them, in either direction. The old ones are withdrawn on the
ground that their two columns came from two harnesses, which is visible in the
commands rather than in the numbers, and not because a later run disagreed with
them.

## 3. Cross-language comparison vs Python

Same committed corpus (`bench/corpus/pairs.json`), same throughput metric
(ns/pair), same auto-scaling best-of-N methodology on both sides — so the numbers
are actually comparable.

```bash
# Python side (rapidfuzz)
. .venv-oracles/bin/activate      # built on Python 3.12+, see ../CONTRIBUTING.md
python bench/python/bench_levenshtein.py

# C# side (Lodestar.Text) — matched Stopwatch harness, not BenchmarkDotNet
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- compare
#   add --codepoint to measure the code-point mode instead of UTF-16

# side-by-side table
python bench/compare.py
python bench/compare.py --format=gfm   # a real markdown table, read natively by nightly_run.md
python bench/compare.py --bands        # the banded buckets instead of the scattered ones
```

The default table is the scattered buckets, which are the claim against rapidfuzz.
`--bands` shows the banded ones, which exist to place the bit-parallel gate and are
not such a claim — twenty band rows in the comparison table would bury the eight
that are (#409).

Results land in `bench/results/` (git-ignored: they are machine-specific and not
authoritative). Every bucket stays inside the BMP, so UTF-16 units and code points
coincide and both sides compute identical distances — and a result is keyed on its
alphabet as well as its length, two buckets answering to each length since #406. `--format=gfm` works on every mode below
the same way; the plain table stays the default because it is the one meant for
a terminal.

### Indel, over the same corpus

`Indel` is `len(a) + len(b) - 2·LCS`, so measuring it measures
`Lcs.SubsequenceLength` — which is also what `fuzz.ratio`, and therefore every
`process.extract`, runs. It gets the same treatment as Levenshtein over the same
buckets, so the two distances can be read side by side (#273):

```bash
python bench/python/bench_indel.py
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- compare-indel
python bench/compare.py indel
```

Both C# harnesses share one timing loop (`CrossLang/PairsHarness.cs`), extracted
rather than copied for the reason `Harness.cs` gives for its own extraction: a
second loop is free to drift from the first while still printing a table that
looks comparable. The same reasoning now covers the other two places the Indel
lot would have copied a Levenshtein one: the Python scripts share
`bench/python/harness.py` and differ only in which `rapidfuzz` distance they hand
it, and the two BenchmarkDotNet classes build their operands through
`ScatteredPair.Build`, seed included — comparing two distances is only meaningful
over identical inputs, and an extracted builder is what makes that true by
construction rather than by inspection.

**What this corpus reaches.** Twenty-eight buckets. Eight are *scattered*, whose
pattern after trimming is an accident of where the mutations fell: four lengths
drawn from 27 Latin symbols, every one ASCII (`U+007A` at most), and four more of
the same lengths from 27 CJK symbols, every one above `U+00FF` (#406). Twenty are
*banded* since #409, whose pattern is exactly the band named — 2 through 16, both
alphabets, 500 pairs each — and they are what can place a gate. Both alphabets carry
4–27 distinct symbols per pattern, so the dense equality table fits at every
length and the bit-parallel kernels are exercised throughout — the failure of #52
and #267, where the new path was never reached at all, does not apply here.

The wide half is what the corpus could not do until #406, and it is what makes a
refusal priceable on the input that actually ships rather than on a synthetic
band. **A wide bucket is only a wide measurement if its pairs reach the kernel**,
which a timing cannot tell you: run `BucketRouteDiagnostics`, which splits the
length-32 bucket of either alphabet on the dispatch's own criterion. It reads 833
of 1 000 CJK pairs on the kernel against 861 Latin.

Both alphabets stay inside the BMP on purpose. UTF-16 units and code points
coincide there, so the C# default and rapidfuzz measure the same quantity;
supplementary characters would break that, an emoji being one code point and two
units. The supplementary case is covered where it can be — the property tests
against the dynamic program, extended for it in #302.

`FuzzBenchmarks.Ratio` also runs this path, on one fixed pair of 43-character
sentences. That is a point, not a curve; `IndelBenchmarks` is the sweep.

### Reading the numbers

The comparison is deliberately honest about methodology: the Python side times the
**realistic per-call loop** a user writes. rapidfuzz also exposes batch APIs
(`process.cdist`) that amortise the Python→C boundary; those are faster than the
loop measured here.

Current headline (see `docs/guides/performance.md` for a captured table): C# wins
on short strings (no interpreter overhead), but rapidfuzz's **bit-parallel Myers**
core scales far better on long strings than our naive O(nm) DP. Closing that gap
is tracked in `docs/decisions/0004-levenshtein-myers-backlog.md`.

## 4. The persistence layer (issue #58)

Three vocabulary loaders and the TF-IDF round trip, on the same three axes as
above, plus one the other sections do not need: **processor time**.

The corpus is generated rather than committed — about 3 MB of vocabulary files:

```bash
python bench/corpus/generate_vocabs.py     # writes bench/corpus/vocabs/, git-ignored
```

All three `bench/corpus/generate_*.py` scripts read `tools/seeded_random.py`, so they need the same
interpreter `.venv-oracles` is built on — **3.12 or later**. Below that they stop with a sentence
naming both versions ([decision 0065](../docs/decisions/0065-the-oracle-generators-floor-is-the-ci-interpreter.md)).

Both language sides read those same files, which is what makes the comparison
mean anything; the bytes are not reproducible across machines and do not need to
be. A harness that cannot find the corpus exits non-zero naming the generator
rather than reporting numbers.

### net10 vs netstandard2.0

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks        -- --filter '*Persistence*' --inProcess
dotnet run -c Release --project bench/Lodestar.NetStandard.Benchmarks -- --filter '*Persistence*'
```

`--inProcess` puts the net10 side on the toolchain the netstandard2.0 project
pins, for the reasons section 2 sets out; this tier published a mismatched pair
until issue #88.

Intel i7-4770S, .NET 10.0.10. Loaders parse from memory here, so the figures are
parsing cost rather than disk latency. **Two runs per side, both shown**,
interleaved net10 → netstandard2.0 → net10 → netstandard2.0.

| Operation | net10 | netstandard2.0 | ratio | net10 alloc | ns2.0 alloc |
| --- | --- | --- | --- | --- | --- |
| `VocabTxt` | 6.202 / 5.818 ms | 6.054 / 5.805 ms | 0.98 / 1.00 | 3.62 MB | 3.62 MB |
| `TokenizerJsonWordPiece` | 13.565 / 13.316 ms | 12.920 / 12.886 ms | 0.95 / 0.97 | 4.74 MB | 4.74 MB |
| `TokenizerJsonUnigram` | 15.036 / 15.172 ms | 15.066 / 15.256 ms | 1.00 / 1.01 | 4.64 MB | 4.64 MB |
| `SpieceModel` | 5.711 / 5.686 ms | **10.489 / 10.419 ms** | **1.84 / 1.83** | 3.36 MB | **5.31 MB** |
| `TfidfSave` | 1.872 / 1.848 ms | 1.888 / 1.906 ms | 1.01 / 1.03 | 2.09 MB | 2.09 MB |
| `TfidfLoad` | 5.538 / 5.345 ms | 5.480 / 5.514 ms | 0.99 / 1.03 | 2.86 MB | 2.86 MB |

**This edition is not comparable to the one it replaces.** The corpus is
generated rather than committed, so re-measuring after issue #100 meant
generating it again, and the untouched rows moved with it — `SpieceModel` reads
5.7 ms here against the 4.2 ms published before, on identical code. Every figure
in this section, and in the two below it, comes from one session against one
corpus. What issue #100 was worth is measured against `main` on that same corpus
and reported in the paragraph after next, not by subtracting these numbers from
the previous edition's.

**What the read path change did to these rows.** Measured against `main`
(`cec02e1`) on this corpus, in the same session, net10:

| Operation | before | after | allocated before → after |
| --- | --- | --- | --- |
| `VocabTxt` | 5.898 / 5.683 ms | 6.202 / 5.818 ms | 4.25 → 3.62 MB |
| `TokenizerJsonWordPiece` | 14.378 / 14.156 ms | 13.565 / 13.316 ms | 7.24 → 4.74 MB |
| `TokenizerJsonUnigram` | 15.884 / 16.017 ms | 15.036 / 15.172 ms | 9.64 → 4.64 MB |
| `SpieceModel` | 5.932 / 6.009 ms | 5.711 / 5.686 ms | 4.61 → 3.36 MB |
| `TfidfSave` | 1.860 / 1.863 ms | 1.872 / 1.848 ms | 2.09 → 2.09 MB |
| `TfidfLoad` | 6.410 / 6.476 ms | 5.538 / 5.345 ms | 4.34 → 2.86 MB |

`TfidfSave` is the control and it does not move, on either axis: nothing on the
write path changed. Every loader allocates less, by 15% to 52%, which is the
counted column and does not move between runs — that is the intermediate buffers
disappearing. Time follows allocation everywhere except `VocabTxt`, which reads
3% *slower* both times on both targets. A 224 KB file is small enough that one
sized allocation buys back less than the extra work of getting to it, and 3% is
close enough to this harness's spread that it is reported rather than explained.

One caveat on the `TfidfSave` allocation figure: the benchmark pre-sizes its
destination `MemoryStream` to the artifact's exact final length, which a caller
writing to `new MemoryStream()` cannot do. That removes the buffer-doubling
garbage a realistic save would pay, so the number is friendlier than the typical
case. It is deliberate — the question on this axis is what the two *targets* cost
each other, not what a caller pays — but it is not a general "cost of Save".

Four rows are noise, which is what equivalent IL should produce, and two runs per
side say so more convincingly than one. Those four scatter between 0.98× and
1.03× and **change sign between rounds** — `VocabTxt` reads 0.98× then 1.00×,
`TfidfLoad` 0.99× then 1.03×. A single run per side cannot distinguish that from
a small consistent penalty, which is what this table used to report.

`TokenizerJsonWordPiece` is the one row that leans without changing sign: 0.95×
and 0.97×, the netstandard2.0 build faster both times by 3–5%. It leaned the same
way before issue #100 (0.95× and 0.98× on `main`, same corpus, same session), so
it is not something this work introduced, and 3–5% on a row whose two targets run
identical IL is small enough to be left as an open observation rather than
explained away.

`SpieceModel` is the exception and it is real: netstandard2.0 allocates twice per
piece where net10 allocates nothing, once for the `byte[4]` scratch buffer in
`ProtobufReader.ReadFloat` and once for the array copy in `DecodeUtf8` that
`netstandard2.0` needs because it has no span overload. Across 29 861 pieces that
is the whole 1.95 MB difference — unchanged by issue #100, which took the same
1.25 MB off both targets and left the gap between them exactly where it was. It
costs 1.83×–1.84× the time. The allocation column is counted rather than
sampled and does not move between runs at all.

**What the toolchain mismatch was worth here.** Measured in the session that
produced the previous edition of this table, and not re-measured since — the
finding is about the harness rather than about the library, and neither has moved
in that respect. Running the net10 side out-of-process in the same window — the
shape of the command pair this section used to publish — made the same binary
read up to 8% faster (`TfidfLoad` 4.625 ms against 4.920 / 5.047 in-process;
`VocabTxt` 4.092 against 4.251 / 4.308). Pairing that column against the
in-process netstandard2.0 one gave `TfidfLoad` 1.10×–1.11× and `VocabTxt`
1.03×–1.06×, where the matched pairing gave 1.01×–1.04× and 0.99×–1.01×. The
mismatch is the same size as the differences the noise rows are read for, which
is why nothing can be concluded from them under it. It is far too small to touch
`SpieceModel`.

**Conditions.** The one-minute load average was 1.4–4.1 on 8 logical cores across
the four runs above, and 1.8–3.2 across the four `main` runs they are compared
against; the editor's language servers and the session driving them are part of
that and cannot be excluded from inside it. Both columns pay it equally and the
runs alternate, so the table is internally comparable — and the before/after
table is comparable to it, having been taken in the same session on the same
corpus. Neither is comparable to the previous edition, for the reason given
above it.

### vs Python

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- compare-persistence
python bench/python/bench_persistence.py
python bench/compare.py persistence
```

Lodestar on .NET 10.0.10 against Python 3.12.3 with `tokenizers` 0.23.1,
`sentencepiece` 0.2.2 and `scikit-learn` 1.9.0. Ratios above 1 mean Lodestar is
faster.

| Operation | Lodestar | Python | wall | Lodestar cpu | Python cpu | **cpu** |
| --- | --- | --- | --- | --- | --- | --- |
| `spiece_model` | 6.163 ms | 33.475 ms | 5.43× | 6.462 ms | 33.473 ms | **5.18×** |
| `tokenizer_json_unigram` | 15.623 ms | 53.566 ms | 3.43× | 16.126 ms | 53.559 ms | **3.32×** |
| `vocab_txt` | 6.354 ms | 11.632 ms | 1.83× | 6.699 ms | 11.631 ms | **1.74×** |
| `tokenizer_json_wordpiece` | 13.328 ms | 18.494 ms | 1.39× | 13.754 ms | 18.490 ms | **1.34×** |
| `tfidf_save` | 1.925 ms | 3.232 ms | 1.68× | 1.970 ms | 3.232 ms | **1.64×** |
| `tfidf_load` | 5.525 ms | 4.205 ms | 0.76× | 5.821 ms | 4.205 ms | **0.72×** |

Both sides were run back to back on an otherwise idle machine, and the pair above
comes from one such run rather than the best figure of each operation across
several — picking per-row winners from different runs would flatter whichever
side happened to be measured last. Run-to-run spread on this harness is a few
percent, so treat differences smaller than that as noise; the loader ratios are
far larger than it.

Every ratio here is smaller than the previous edition's, and none of that is a
regression: this is the regenerated corpus of the section above, on which the
Python side is uniformly faster than it was on the old one (`vocab_txt` 11.632 ms
against 16.860). The Lodestar column moved in the other direction — the same
harness on `main`, on this corpus, in this session, read `vocab_txt` 6.799 ms,
`tokenizer_json_wordpiece` 14.269, `tfidf_load` 6.277 and `tfidf_save` 2.032, so
every row above except `tokenizer_json_unigram` is faster after issue #100 than
before it, `tfidf_load` by 12%.

### Why processor time is reported too

Elapsed time alone flatters this runtime. .NET's background collector does its
work on other threads, so an allocation-heavy operation finishes in less elapsed
time than it costs: every Lodestar row above burns 1.02–1.07 processor-seconds per
elapsed second, while CPython is strictly single-threaded and measures 1.00 on
every row. That gap is narrower than the 1.12–1.21 the previous edition reported,
and it narrowed for the reason issue #100 exists: an operation that allocates a
third less gives the background collector a third less to do.

`tfidf_load` is the one row Python still wins, and it now wins it on both columns
rather than only on processor time — 0.76× wall, 0.72× cpu. Issue #100 moved that
row toward parity rather than away from it (0.67× wall on `main`, same corpus,
same session); what is left is a corpus on which scikit-learn's loader does
better than on the previous one, plus the background-collection residue, which is
a property of the host runtime rather than something a library chooses — an
application that wants it gone sets `ConcurrentGarbageCollection=false` in its own
runtimeconfig.

### Reading the numbers

The two sides do not do the same amount of work on the loader rows, and the
comparison says so rather than hiding it. `tokenizers` and `sentencepiece` build
a whole tokenizer: the normalizer and pre-tokenizer graph, and the Rust or C++
matcher they will encode with. Lodestar's loaders build a validated dictionary and
stop — the guides tell readers to construct a tokenizer from it as a second step.
A margin in Lodestar's favour therefore reflects, in part, work it does not do.

Both are also native code behind a thin binding, not interpreted Python.

The `tfidf_save` / `tfidf_load` pair is the one comparison that is close to like
for like, and it is the one that moved most. It started at 0.60× and 0.44× — both
losses — and reached the table above through six changes, each of which was kept
only because it measured: shortest-round-trippable doubles, the idf vector as
base64, the relaxed JSON encoder, folding the ordering check into the read loop,
sizing the vocabulary buffer from the declared count, and — issue #100 — reading
the payload into one buffer sized before it is filled while decoding the base64
straight into the array that keeps it. Two further changes
were measured and **discarded** for showing no gain: disabling writer validation,
and an earlier version of that last buffer change, which paid nothing until the
idf vector stopped dominating the profile. The reasoning is in
[`docs/decisions/0011`](../docs/decisions/0011-persistence-format.md).

## 5. Classification metrics (issue #61)

`ConfusionMatrix`, `Accuracy`, `Precision`/`Recall`/`F1`, `ClassificationReport`
and `RocAuc`, against scikit-learn's equivalents, on the same three axes as
persistence — wall time, processor time, and net10 vs netstandard2.0 — plus a
fourth this branch adds: **processor time is the merge gate**, not a footnote.
Every row in the cross-language table below must be ≥ 1×, or the branch does
not merge.

Issue #93 later added `BalancedAccuracy`, `MatthewsCorrelation` and `CohenKappa`
to the same cross-language harness, taking it from six operations to **nine**.
They share this section's corpus, harnesses, methodology and gate, so the prose
below covers them; their 18 measured rows were produced in a separate window with
its own load average and are published once, in
[`docs/guides/performance.md`](../docs/guides/performance.md#balanced-accuracy-matthews-correlation-cohens-kappa-issue-93),
rather than duplicated into the table here.

The corpus is generated rather than committed, like `bench/corpus/vocabs/` —
six JSON files, about 54 MB total:

```bash
python bench/corpus/generate_metrics.py     # writes bench/corpus/metrics/, git-ignored
```

Six shapes — (1 000, 2), (1 000, 10), (100 000, 2), (100 000, 10),
(1 000 000, 2), (1 000 000, 10) samples × classes — each with `y_true`,
`y_pred`, a sample-weight column (generated but unused by the nine benchmarked
operations, on both sides), and scores for the ROC-AUC rows. The 10-class score
matrix stops at 100 000 rows: a million rows by ten classes is 200 MB of JSON,
which would measure the parser rather than the metric.

### Intra-C#, and net10 vs netstandard2.0

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks        -- --filter '*MetricsBenchmarks*' --inProcess
dotnet run -c Release --project bench/Lodestar.NetStandard.Benchmarks -- --filter '*MetricsBenchmarks*'
```

Default job on both sides. The full matrix — 3 sizes × 2 class counts ×
`MetricsBenchmarks`' 5 methods = 30 benchmarks — takes about ten minutes per run.
This tier is not the nine-operation cross-language set: `MetricsBenchmarks` times
`Matrix`, `MatrixWeighted`, `AccuracyScore`, `F1Macro` and `Report` only, and the
three issue-#93 metrics have no BenchmarkDotNet method of their own.

**`--inProcess` on the first command is not decoration.** Without it the two
commands do not measure the same way: `Lodestar.NetStandard.Benchmarks` pins
`InProcessEmitToolchain` (its `Program.cs` needs it, or BenchmarkDotNet's
generated project re-resolves the `ProjectReference` and silently restores the
net10.0 build), while the first command would use the default out-of-process
toolchain. Any figure compared across those two harnesses mixes the target
framework with the harness that measured it. This tier was published that way
before, and the flag is what removes the confound. Sections 2 and 4 above and the
batched-embedding comparison in `docs/guides/performance.md` carried the same
defect and were re-measured with the flag in place (issues #87 and #88). Every
net10-versus-netstandard2.0 figure in this repository now comes from a pair of
commands that share a toolchain, and from two runs per side rather than one.

Same isolation assertion as those sections: the netstandard2.0 run proves it
loaded the netstandard2.0 build before any number from it is trusted.

```text
// Lodestar.Metrics: .NETStandard,Version=v2.0
```

**Every figure below is two runs, not one, and the two are shown.** The runs
were interleaved (net10, netstandard2.0, net10, netstandard2.0) so that any
drift in machine load spreads across both targets instead of landing on
whichever one occupies the second half of the window. Intel i7-4770S,
.NET 10.0.10. `Matrix`, at all six shapes:

| Samples | Classes | net10 (run 1 / run 2) | netstandard2.0 (run 1 / run 2) | ratio |
| ---: | ---: | ---: | ---: | ---: |
| 1 000 | 2 | 7.167 / 7.792 µs | 7.090 / 7.003 µs | 0.90–0.99× |
| 1 000 | 10 | 7.010 / 8.620 µs | 6.951 / 7.017 µs | 0.81–0.99× |
| 100 000 | 2 | 805.1 / 800.9 µs | 795.4 / 799.2 µs | 0.99–1.00× |
| 100 000 | 10 | 916.5 / 899.5 µs | 892.1 / 894.4 µs | 0.97–0.99× |
| 1 000 000 | 2 | 8.396 / 8.181 ms | 8.158 / 8.357 ms | 0.97–1.02× |
| 1 000 000 | 10 | 9.421 / 9.139 ms | 9.118 / 9.145 ms | 0.97–1.00× |

**At n=100 000 and n=1 000 000 the two targets are at parity**, and now with
something behind the claim: all 40 measurements there — 5 methods × 4 shapes ×
2 pairings — land between 0.97× and 1.05×, while each target's own spread
between its two runs stays inside 1.05× (net10) and 1.02× (netstandard2.0).
The difference between the targets is the same size as the noise, which is the
honest way to say parity.

**At n=1 000 the ratio column is not measuring the target framework.** The
net10 side moves by up to **2.64×** between two runs of the same binary over
the same corpus — `AccuracyScore` at k=10 reads 836 ns in one run and 2 212 ns
in the next — while netstandard2.0 stays inside 1.02× across every method. The
inflation is cleanly inverse to the work per operation: `AccuracyScore` (~0.84 µs
per op) moves 2.64×, `Matrix`, `MatrixWeighted` and `F1Macro` (~7–8 µs) move
1.1×–1.3×, and `Report` (~10–16 µs) moves by 1–2%. That is the shape of a fixed
per-invocation overhead appearing in one process and not another, not of a
difference in the metric code — the same computation is happening either way.

The consequence is worth stating plainly, because this section got it wrong
before: **BenchmarkDotNet's ± margin describes dispersion *within* one process
and says nothing about reproducibility *across* processes.** An earlier version
published `AccuracyScore` at n=1 000, k=10 reading 2.59× and explained it as
the short job's noise floor — implying a longer job would settle it. A longer
job did not. The 2.59× vanished, and k=2 came back at 0.64× with a ±0.3%
margin on the net10 side, which is to say a tight interval around a figure
that a re-run then contradicted outright (1 331 ns, then 833 ns). A
`MatrixWeighted` gap of 1.06×–1.18×, apparently systematic across all six
shapes, evaporated the same way once the toolchain confound was lifted and the
runs repeated. Anything at n=1 000 in this tier needs repeated processes
before it means anything.

One small-n row *is* stable on both sides, and it says something real:
`Report` runs **1.06×–1.11× slower on netstandard2.0 at n=1 000**, consistently
across both pairings and both class counts, fading to 1.00×–1.05× at the larger
sizes. A fixed per-call cost the netstandard2.0 build pays and net10 does not,
drowned once O(samples) work dominates.

This is still **not** the `VectorMath.Dot` story from section 2, where the gap
is 4.2×–5.3×. `Lodestar.Metrics` has no `Vector<T>` SIMD path, and every
benchmark here is a scalar loop over `int[]`/`double[]`. The one piece of
target-conditional code in its dependencies is `Lodestar.Internal.Guard`, which
picks `ArgumentNullException.ThrowIfNull` on net10 and a hand-written check on
netstandard2.0 — a null test outside every loop, which cannot produce a
per-sample difference and does not.

### vs Python

```bash
. .venv-oracles/bin/activate && python bench/python/bench_metrics.py
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- compare-metrics
python bench/compare.py metrics
```

Lodestar on .NET 10.0.10 against scikit-learn 1.9.0 / NumPy 2.5.1 on Python
3.12.3. Ratios above 1 mean Lodestar is faster. The 29 measured rows are
published in
[`docs/guides/performance.md`](../docs/guides/performance.md#classification-metrics-issue-61--vs-scikit-learn),
not duplicated here.

**Merge gate: 29/29 rows at or above 1× on processor time.** Twenty-nine rows,
not twenty-nine operations: six operations over six shapes, less the seven
shape/operation pairs the two ROC-AUC rows do not cover. The three issue-#93
operations add 18 more rows, all of them ≥ 16.5× — published in
[`docs/guides/performance.md`](../docs/guides/performance.md#balanced-accuracy-matthews-correlation-cohens-kappa-issue-93),
measured in their own window, and not folded into the rows above because they do
not share its load conditions. The
narrowest margin here is 2.74× (`roc_auc_ovr_macro` at n=100 000, k=10). The design
brief named `roc_auc_binary` at a million samples — where the cost is a sort —
as the likely candidate for falling below 1×, with a radix pass over the
`double` bit patterns as the fallback if it did. It came in at 3.81×; no row
needed that change on this branch.

### Measurement conditions

Both files were produced back to back, Python first, on a machine left to
settle first — an earlier pairing was discarded outright for having started
while the five- and fifteen-minute averages were still shedding the tail of a
preceding test suite. The pair in the table above:

| Side | Started | Written | Load at start (1 / 5 / 15 min) |
| --- | --- | --- | --- |
| Python (`python-metrics.json`) | 19:09:25 | 19:12:30 | 1.52 / 3.71 / 3.94 |
| C# (`csharp-metrics.json`) | 19:13:19 | 19:16:41 | 4.20 / 4.10 / 4.05 |

The machine carries a permanent 30–40% background load from the desktop
client, an editor and a browser; a one-minute average of 1.9–2.3 is this
workstation's floor, and the Python side started below it.

**The C# side started at 4.20, and that is its predecessor's own wake** — 49
seconds after the Python run finished saturating a core. The second side of any
back-to-back pair pays this; the alternative, waiting for the tail to decay,
buys a quieter machine at the cost of the two sides no longer being back to
back. The bias it introduces runs *against* Lodestar — the C# figures are the
ones measured on the busier machine — so every ratio in the table above, and
the merge gate that reads them, is conservative rather than flattering.

Memory was never the constraint these runs looked like they might have: the
working set at n=1 000 000 is about 16 MB, and the machine had 22 GB free with
3 GB in swap. CPU contention is the only thing worth waiting out here.

### Why processor time barely differs from wall time here

Unlike the persistence comparison in section 4 — where Lodestar burns 1.12–1.21
processor-seconds per elapsed second, so the wall and cpu columns diverge and
`tfidf_load` even flips winner between them — the two columns above agree to
within about 1% on every row (up to 3.4% on the single heaviest row,
`roc_auc_ovr_macro` at n=100 000). These operations allocate little enough
(a few kilobytes at most; see `MetricsBenchmarks`'s `[MemoryDiagnoser]` output)
that .NET's background collector never gets involved, so there is no gap
between the two columns for the cpu one to correct.

### Reading the numbers

The rows at n=1 000 (70×–620×) are not telling you Lodestar is two orders of
magnitude faster at the arithmetic — a confusion matrix over 1 000 samples is
sub-microsecond work either way. They are measuring CPython's per-call
interpreter overhead, which the auto-scaling loop cannot amortise away because
each `skm.*` call re-enters the interpreter regardless of how little work is
inside it. The rows that carry the argument about the underlying computation
are the ones at n=100 000 and n=1 000 000, where the ratios settle to a still
decisive but far more modest 2.7×–43×.

None of the nine operations passes `sample_weight` on either side — the corpus
carries that column, but neither the brief's own six calls nor the three issue-#93
ones use it, so this comparison does not exercise `ConfusionMatrix`'s weighted
path at all; see `MetricsBenchmarks.MatrixWeighted` in the intra-C# tier above for
that.

`precision_recall_f1_macro` is the one operation where the two sides do not do
quite the same amount of work, though both compute the same three numbers over
the same matrix. scikit-learn's `precision_recall_fscore_support` builds the
per-class true-positive/predicted/support sums once and reads precision,
recall and F1 off that single pass. The Lodestar side builds the confusion
matrix once too, but `Precision.Score`/`Recall.Score`/`F1.Score` each walk
those sums again independently — three redundant `O(classes)` passes instead
of Python's one. At 2 or 10 classes that redundancy is a handful of additions,
several orders of magnitude below the `O(samples)` matrix construction both
sides pay first, so it is invisible in the ratios above; it is called out here
because it is real, not because it matters at this scale. `classification_report`
carries the same pattern more deeply (each of its per-class arrays, and each of
its macro/weighted averages, re-derives those sums again) for the same reason
and to the same effect: negligible at these class counts.

Everything else — `confusion_matrix`, `accuracy`, `roc_auc_binary`,
`roc_auc_ovr_macro` — is like for like: same algorithm (`BinaryRoc` mirrors
scikit-learn's `_binary_clf_curve` sort-and-accumulate exactly, and
`MultiClassRoc`'s one-vs-rest reduction is scikit-learn's own), same data,
parsed once outside the timed loop on both sides so neither pays JSON-parsing
cost inside the measurement.

## 6. Multiclass ROC-AUC, sequential against parallel (issue #86)

C# against C#: the same operation at one, two, four and eight workers. Inputs are
generated in-process from a fixed seed — there is no Python side here, so there is
no shared corpus to keep in step.

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- roc-parallel
```

The axis is **elapsed time**. Processor time rises with the worker count, which is
the point of spending cores rather than a fault in the measurement, and both are
reported side by side.

This is its own section rather than a subsection of section 5 for a reason beyond
tidiness: its `dop=1` figures look pairable with section 5's `roc_auc_ovr_macro`
row and are not. Different input, different machine load, and a sequential path
that was rewritten for this issue — the 24 measured cells and all three reasons
are in
[`docs/guides/performance.md`](../docs/guides/performance.md#multiclass-roc-auc-sequential-against-parallel-issue-86).

## 7. Persisting an embedding index (issue #62)

`EmbeddingIndex.Save` and `Load` on 10 000 vectors of 384 dimensions — 15 MB of
floats, the shape a sentence-transformer corpus actually has. The array is
generated from a fixed xorshift32 seed on both sides rather than committed: what
a float block costs to write depends on how many floats it holds, not on what
they are, and both sides reproduce the same shape from the same arithmetic
without a fixture. In Python this is a 10 000 × 384 loop of plain-int arithmetic
(`numpy.uint32`'s shifts raise on the overflow the algorithm depends on); it took
2.35 s measured once outside the timed loop, which is fine to pay a single time
at process start and did not need caching.

Measuring this shape is what found a bug: loading it used to need an explicit
`ArtifactLoadOptions { MaxArrayLength = 4_000_000 }`, because the reader applied
that 1 000 000-element default — sized for vocabularies, not vector blocks — to
the decoded float count, refusing 10 000 × 384 = 3 840 000 floats and, with it,
any index past 2 604 vectors of this dimension. It is fixed: the vector block is
bounded by `MaxTotalBytes` instead, which caps the whole payload in bytes before
parsing begins rather than the decoded element count after. `EmbeddingIndexLoad`
and `embedding_index_load` below call `Load` with the library's own defaults, and
need no options to do it.

### net10 vs netstandard2.0

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks        -- --filter '*EmbeddingIndex*' --inProcess
dotnet run -c Release --project bench/Lodestar.NetStandard.Benchmarks -- --filter '*EmbeddingIndex*'
```

Intel i7-4770S, .NET 10.0.10. **Two runs per side, both shown**, interleaved
net10 → netstandard2.0 → net10 → netstandard2.0, for the same reason section 4
gives: one run per side cannot tell a small consistent penalty from noise.

| Operation | net10 | netstandard2.0 | ratio | net10 alloc | ns2.0 alloc |
| --- | --- | --- | --- | --- | --- |
| `EmbeddingIndexSave` | 13.75 / 13.57 ms | 14.83 / 15.24 ms | **1.08 / 1.12** | 54.29 MB | 54.29 MB |
| `EmbeddingIndexLoad` | 12.96 / 12.68 ms | 14.36 / 14.41 ms | **1.11 / 1.14** | 35.35 MB | 35.35 MB |

**The two targets now diverge, and they did not before.** This table used to read
0.99×–1.01× on both rows, and the previous edition said in as many words that
there was no `SpieceModel`-shaped story here. There is one now, and issue #100
put it there. Both operations scan the whole vector block for non-finite
components — `Load` before handing the index back, `Save` before writing a file
it could not honestly write — and that scan is 3.84 million floats. It is now
vectorized through `Vector<float>`, which the netstandard2.0 build does not
compile: `Vector.IsHardwareAccelerated` and the span constructor sit behind
`#if NET5_0_OR_GREATER`, the same guard `Pooling.cs` and `VectorMath.cs` already
use. netstandard2.0 keeps the scalar loop and pays for it.

The size of the gap says the same thing twice: 1.47 ms on `Save` and 1.45 ms on
`Load`, on two operations that share nothing else. That is the scan, priced on
its own — and it matches the 18% of the load figure the scan measured at before
it was vectorized, which is what decided it was worth vectorizing at all.

Everything else about the two targets is still identical.
`Base64Numbers.WriteSingles` and `ReadSingles` use `MemoryMarshal.Cast`/`AsBytes`
on both, on purpose: `BitConverter.SingleToInt32Bits` does not exist on
netstandard2.0, so the encoding path never forks the way `ProtobufReader` does.
Allocation is identical down to the byte and does not move between runs, which
section 4 already established is what the counted (not sampled) column does.

**What the read path change was worth here.** Against `main` (`cec02e1`) in the
same session: `EmbeddingIndexLoad` 36.894 / 37.485 ms → 12.948 / 12.921 on net10
and 34.437 / 34.341 → 14.36 / 14.41 on netstandard2.0, with allocation falling
from 90 MB to 35.35 MB on both. That is **2.88× faster on net10 and 2.39× on
netstandard2.0**, for an artifact whose bytes did not change. `EmbeddingIndexSave`
falls too, 16.96 ms to 13.57 on net10, which is the vectorized scan rather than
the read path — `Save` runs the same check. Its netstandard2.0 side reads 16.88 →
15.04 ms, an 11% improvement this work does not explain: nothing on that target's
write path changed, and it is recorded here as measured rather than attributed.

**Conditions.** The one-minute load average ran 1.6–1.9 across these four runs and
1.4–3.2 across the four `main` runs they are compared against, in the same session
as the cross-language pair below — see that section's measurement-conditions note.
All of section 4, section 7 and the cross-language pair were taken in this one
session, on one regenerated corpus, and are comparable to each other; none of them
is comparable to the editions they replace.

### vs numpy — what the format choice costs

`numpy.save` writes a short header followed by the raw little-endian block. That
is precisely what a dedicated binary format for this artifact would have
produced, so this comparison measures the decision recorded in
[0011](../docs/decisions/0011-persistence-format.md) rather than illustrating it.
`faiss` was deliberately not added to make this point a second way: on a flat
index it also writes the same raw block `.npy` does, so pulling it in as a
dependency would cost a pinned package to measure the same floor twice.

```bash
python bench/python/bench_persistence.py
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- compare-persistence
python bench/compare.py persistence
```

Lodestar on .NET 10.0.10 against numpy 2.5.1 on Python 3.12.3. Ratios above 1 mean
Lodestar is faster.

| Operation | Lodestar | numpy | wall | Lodestar cpu | numpy cpu | **cpu** |
| --- | --- | --- | --- | --- | --- | --- |
| `embedding_index_save` | 12.419 ms | 15.084 ms | 1.21× | 13.349 ms | 15.083 ms | **1.13×** |
| `embedding_index_load` | 12.129 ms | 2.492 ms | 0.21× | 13.897 ms | 2.492 ms | **0.18×** |

**Read that `0.21×` as a format, not as a speed.** It puts our JSON artifact — a document to
scan and validate — against numpy's raw block, so it prices decision 0011 exactly as this
section says it does, and says nothing about how fast the two languages ingest the same bytes.
`embedding_index_ingest_npy` is the row that does: **both sides read a `.npy` and return
something searchable**, `np.load` against `NpyFile.Read` plus
[`EmbeddingIndex.FromOwnedBlock`](../docs/reference/embeddings/search/embeddingindex-fromownedblock.md).
It called `FromBlock` until #466: `np.load` returns the array it has just filled and copies it no
further, so copying the block into the index charged this side a copy numpy never pays, and priced
a route the caller need not take. Adopting is the like-for-like chain, and what
[`NpyBlock.OwnedArray`](../docs/reference/embeddings/persistence/npyblock.md) exists to allow.
For a flat cosine index the matrix *is* the index, so `np.load` alone is the honest counterpart,
and neither side normalizes — which is what a block written by an embedding pipeline already is.
It was added with the bulk ingest (#474); before that there was nothing on the C# side to pair it
with, because `Add` was the only way in.

**Its first runner reading is 0.21–0.23× wall, 0.19× cpu across three rounds** — numpy four to
five times faster, on the same 15 360 128 bytes. Taking the format advantage away made the gap
*wider* than `embedding_index_load`'s 0.24–0.27×, because that row was letting numpy be compared
against 5 MB less work. The figures, and the rows on the same run where this project is 1.2× to
6× ahead, are in
[the performance guide](../docs/guides/performance.md#against-numpy-on-the-same-format-issue-474).

**#466 took the copies between the stream and the index out and the row inverted**, from 0.19× of
numpy's cpu to **1.00–1.13×** and from 0.21–0.23× of its wall to 1.21–1.25×. cpu is the column this
harness trusts, so the honest reading is *parity to slightly ahead*, not the wall figure. How many
copies there were, and which of them paid, is decision 0057's subject rather than this section's.
The reading above stays as measured; the new one, its runner, and the anchors that make the two
windows comparable are in
[the performance guide](../docs/guides/performance.md#the-same-row-once-the-block-is-adopted-issue-466).
Two dispatches separated the causes: reading the payload straight into the `float[]` moved nothing,
and adopting the array rather than copying it into the index moved all of it — by more than the
copy it removed, because the copy came with a second 15.36 MB allocation.
[Decision 0057](../docs/decisions/0057-the-npy-read-serves-a-stream-and-a-buffer-differently.md)
has the shape the reader took; [#480](https://github.com/CyrilB1531/lodestar/issues/480) carries
the half that is still unexplained.

> **#323 changed the save path after this window.** The row above stays as measured;
> what it cannot show is that its `1.13×` inverts to `0.27×` on a newer machine,
> because `numpy.save` is bandwidth-bound where this artifact's base64 encoding is
> not. The before/after, and why the ratio is a property of the machine as much as
> of the code, are in [`docs/guides/performance.md`](../docs/guides/performance.md).

Against `main` in the same session, on the same numpy figures:
`embedding_index_save` read 14.164 ms (0.94× wall, 1.06× cpu) and
`embedding_index_load` 32.460 ms (0.08× wall, 0.07× cpu).

| | Lodestar artifact | `.npy` |
| --- | --- | --- |
| bytes on disk | 20 589 007 | 15 360 128 |

That row is the one thing in this section issue #100 did not move, and not moving
it was the point: the same index saved by the build before this work and by the
build after it produces the same file, verified byte for byte by hash, and each
build reads the other's.

Neither harness command above prints a byte count — `Harness.Measure` only
records `ms_per_op` and `cpu_ms_per_op`. The Lodestar figure is `stream.Length`
after building the same index through `BuildIndex()` and calling `Save`; the
`.npy` figure is `len(buffer.getvalue())` after `np.save` on the same
`build_vectors()` array — the same two calls each `embedding_index_save` row
above times, with the byte count kept instead of discarded.

That is a 1.34× size ratio. Base64 alone accounts for 1.333× of it (the vector
block is 15 360 000 bytes of floats, 20 480 000 as base64 text); the remaining
0.5% is the `dimension`/`normalize`/`count` fields and the 10 000 quoted `doc-N`
ids, none of which `.npy`'s header carries.

### Reading the numbers

The size ratio is what the design predicted: about 4/3, plus a fraction of a
percent for the fields a raw block does not need. It has not moved and cannot:
this is the same artifact, byte for byte, as before issue #100.

`EmbeddingIndexSave` now reads 1.21× wall and 1.13× cpu — faster than
`numpy.save` rather than 1% behind it, which is not the read path but the
non-finite scan `Save` shares with `Load` becoming a vector pass.

`EmbeddingIndexLoad` is still the row that costs. It is 4.87× slower than
`numpy.load` on wall time and 5.58× on cpu, against 13.0× and 13.7× on `main` in
this same session — a real change, and still not parity. The gap is no longer made of copies.
The read path now reads the payload into one buffer sized from the stream's own
length before it is filled, decodes the base64 straight into the `float[]` that
keeps it, and scans that array for non-finite components by vector: three passes
where it used to take five, and 35.35 MB allocated to move 15 MB of floats where
it used to take 90 MB. What remains is the format's own floor — 20 MB of base64
text has to be read and decoded where `.npy` reads 15 MB and casts it — plus a
scan `numpy.load` does not perform at all, because it does not promise what this
artifact promises about its contents. Closing the rest of that gap means not
materialising the payload, which `MaxTotalBytes` currently forbids by design; that
is a different decision from this one and has not been taken.

None of this was anticipated by size alone, and the honest reading is not that
the design was wrong to choose JSON: ADR 0011 weighed one format against two and
a fixed 33% against a decode that reads the whole payload into memory first, and
said so plainly. What the 33% figure did not say, because nothing in that
decision measured it, is what the *implementation* of that buffered decode would
cost — an order of magnitude on `Load`, most of it in copies the format never
required. Issue #100 measured that, removed it, and re-measured: the size cost
landed on prediction and stayed there, the time cost did not and has now come
down by 2.9× without the format moving. The residue above is the format's, and
this section will keep reporting it.

The figures carry their own error bars: both cross-language figures are the best
of five repeated measurements each, not a single sample, and the BenchmarkDotNet
figures above spread no more than 2.8% from their own minimum within a target —
nowhere near either the 2.9× the change is worth or the 4.9× that is left.

**Measurement conditions.** Both sides were run back to back, Python first, in
the same session as the BenchmarkDotNet runs above — this machine carries a
permanent background load from an editor, a browser and the assistant session
driving the benchmark itself, and there was no quieter window to wait for
without breaking the back-to-back pairing the comparison depends on:

| Side | Started | Written | Load at start (1 / 5 / 15 min) |
| --- | --- | --- | --- |
| Python (`python-persistence.json`) | 17:58:14 | 17:59:14 | 1.59 / 1.82 / 3.77 |
| C# (`csharp-persistence.json`) | 17:59:14 | 18:00:24 | 1.74 / 1.81 / 3.64 |
| C# on `main`, for the before/after | 18:16:38 | 18:17:44 | 1.44 / 1.91 / 2.62 |

The two starts are close on all three windows — within 0.15 on the one-minute
average, lower on five and fifteen minutes for the side measured second — so,
unlike section 5's pair, there is no direction here for the load itself to have
biased the ratio: neither side woke the other into a spike the way section 5's
C# side inherited from Python's. The C# side started the moment Python's file was
written, with no gap for this session's own overhead to open. The `main` row was
taken 16 minutes later at a comparable load, which is what makes the before/after
in this section and in section 4 a comparison rather than two tables.

### The save rows are no longer all in memory

`embedding_index_save_file` is the write counterpart of `embedding_index_load_file`,
which #336 added because the file path is the one a caller takes — every other save
row here writes to a `MemoryStream`. It uses a path of its own, so neither direction
measures a file the other just touched, and neither side flushes to the device. It
priced pre-sizing the file, which
[ADR 0052](../docs/decisions/0052-pre-sizing-the-artifact-file-buys-nothing-on-a-delayed-allocation-filesystem.md)
refused, and it outlives that question.

### Every load row here is measured on a warmed heap

`compare-persistence` runs `embedding_index_save` before `embedding_index_load`,
in one process, and the save allocates tens of megabytes. That leaves the
large-object heap grown and its **pages already committed** for the load that
follows — so the load does not pay the page commits #324 identified as the
irreducible part of its allocation phase. It is not an artefact of the ordering
being wrong; it is what any harness measuring both directions of the same format
will do unless it is built not to.

Step 1 made it visible rather than created it. Removing the save's 20 MB buffer
withdrew the subsidy, and `embedding_index_load` slowed by 1.22× on the container
and by 1.10–1.17× on the nightly runner once the runners' own difference is netted
out — with **the allocation identical to three digits on both sides**, which is
what says the load is doing the same work and paying for more of it.

So read every `embedding_index_load*` row on this page as **flattered by 8.1% — the
printed figure times 1.088 is what a load costs unsubsidised** — and do not use one
as the control for a change to the save path. Section 8's
`block_copy_floor` is what a control looks like here: a `memcpy` that allocates
nothing and therefore cannot be subsidised by anything.

**That 8.1% is measured, and it replaces an inference of "on the order of 20%" that
stood here until [#433](https://github.com/CyrilB1531/lodestar/issues/433) ran. The
two do not agree: the inference overstated the subsidy by about 2.5×.** It was read
off a band of before-and-after figures spanning two changes and two machines, which
is a wide enough thing to read a number off that it should not have been quoted to a
digit. Section 9 has the measurement and the conditions.

**The harness is not split into two processes, and that is #433's answer.** Loading in
a process that has never saved is the obvious fix and it was refused on the number.
8.1% moves the published `embedding_index_load` row from 0.25× to 0.23× — from 4.0×
behind `numpy.load` to 4.3×. Nobody reads those two differently. Against that, a split
costs a process launch per row and, worse, the back-to-back pairing that the
measurement-conditions section above argues is the only reason the C# and Python rows
are comparable: two processes started minutes apart are two machines' worth of drift
inside what is supposed to be one window. **Stating the subsidy is worth more than
removing it**, so it is stated here, in section 9, and in the performance guide, and
`heap-warmth` stays committed so the next person to doubt it can re-run it rather than
re-argue it.

## 8. Where a save's time goes (issue #429, step 0)

Not a comparison: a **profile of one operation against itself**. Five phases over
the same 10 000 × 384 index as section 7, each a strict subset of the one above
it, so the shares are readable without a second harness to reconcile.

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- save-phases
```

| phase | what it does |
| --- | --- |
| `save_total` | `EmbeddingIndex.Save` end to end |
| `write_base64_property` | the vector block alone, through `Utf8JsonWriter.WriteBase64String` |
| `write_base64_chunked` | the same block in 240 KB slices into one rented buffer |
| `base64_encode` | `Base64.EncodeToUtf8` on the block and nothing else |
| `block_copy_floor` | a `memcpy` of the same 15.36 MB, encoding nothing |

The last row is what makes the table decide anything. An encode that costs no more
than moving the same bytes is bandwidth-bound, and nothing parallelises past a
bandwidth it is already at — which is how a proposal to thread the base64 was
refused rather than tried. [ADR 0051](../docs/decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)
is that decision, and the numbers are in
[`docs/guides/performance.md`](../docs/guides/performance.md#what-a-save-actually-spends-its-time-on--step-0).

**The phases run round-robin, one round each, not one phase to completion.** This
is the whole design and it is not tidiness. A first cut ran each phase's nine runs
back to back, so a collection storm landed inside one phase's window and the
harness reported `write_base64_property` at **136.7% of `save_total`** — impossible
for a strict subset of the same work, and obvious only because the subset relation
gives the table something to contradict. Interleaving spreads that cost across
every phase instead of concentrating it in whichever one was unlucky.

Medians of nine runs after three warm-ups, with the full spread printed under the
table: on a shared machine the floor is the honest half of a row, and a phase that
allocates 20 MB per call announces itself by varying, not by being slow on average.

## 9. Is a load measured on a warmed heap (issue #433)

Not a comparison and not a profile: **one operation, two processes.**

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- heap-warmth prepare
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- heap-warmth cold
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- heap-warmth warm

python bench/python/bench_heap_warmth.py prepare
python bench/python/bench_heap_warmth.py cold
python bench/python/bench_heap_warmth.py warm
```

**Both languages, or neither.** The finding is a ratio per language and the two ratios
are then compared, so taking them on two machines compares the machines as much as the
languages. `Benchmark (on demand)` (section 10) runs all four states inside one round for
that reason, and the Python side mirrors the C# structure rather than its code — three
subcommands, the warming saves before the loop, the stream built outside the timer.

`compare-persistence` measures every save, then every load, in one process — so by
the time a load is timed the large-object heap has already been grown and its pages
committed by the saves. The question is what that is worth, and **it cannot be two
rows of one run**: a save warms the heap for everything after it, and nothing inside
a process honestly undoes that. Hence three subcommands.

`prepare` writes the artifact once. **`cold` then reads those bytes having built and
saved nothing** — that is what makes it cold, and it is why the artifact comes off
disk rather than from a save. `warm` does its saves **first** and then loads, which
is the order the harness uses; a save between two loads would add garbage competing
with the load instead of leaving a heap behind it.

Both states report **allocation per load**, and they must agree. If they do not, the
two are running different workloads and no timing comparison between them is valid —
the claim under test is that the load does identical work and pays for more of it.

**The result.** Nine alternating rounds on one hosted runner, all four states inside
each round, `Benchmark (on demand)` run 3:

| | cold | warm | warm/cold |
| --- | ---: | ---: | ---: |
| `EmbeddingIndex.Load` | 18.101 ms | 16.559 ms | **0.919** |
| `np.load` | 1.382 ms | 1.316 ms | **1.001** |

Medians of the nine round medians; the `warm/cold` column is the median of the nine
**paired** ratios, which is the statistic the alternation is for — pairing an unpaired
cold median against an unpaired warm one puts numpy at 1.05× and is an artefact.

**We have the subsidy: warm is faster in 9 rounds of 9** (sign test p = 0.004),
by 8.1%. **numpy does not have it: 4 of 9**, median ratio 1.001 — a coin toss.

**The timing is the weaker half of the evidence.** Every round, on both sides of the
C# comparison: allocation 37 069 648 bytes cold against 37 069 848 warm — 200 bytes
apart in 37 MB — and collections **4/4/4 cold against 3/3/3 warm**. Same work, same
allocation, one fewer garbage collection. That is the mechanism, and it is discrete
and reproducible where a millisecond is neither.

**On a shared machine none of this reproduces.** The same instrument on the container
put warm faster, level, then slower over three rounds with one state's median swinging
4× between them. That is why section 10 exists.

## 10. Running a diagnostic on a second machine (issue #461)

`roc-parallel` (section 6), `save-phases` (section 8), `heap-warmth` (section 9),
`pool-cost` (section 11), `ingest-phases` (section 12), `sidecar` (section 13) and
`tensor-primitives` (section 14) are
C#-only subcommands rather than `[Benchmark]` classes, so no benchmark or harness in
`bench-map.json` selects them and the nightly never runs one. That is deliberate — they
answer a question a lot asks once, not a regression worth watching every night.

`bench-map.json` does name them, in its own `diagnostics` list, and `tools/check_bench_map.py`
checks that list against what `Program.cs` dispatches. Exempt from the nightly is not exempt
from existing: a subcommand nothing names cannot be told apart from a harness somebody forgot
to map, which is the finding that file exists to raise.

The cost of that was that they ran only on a contributor's own machine, and for
some of them the machine is the finding: `save-phases` measures shares inside one
window and those transfer, but a subcommand comparing two processes has absolutes
that do not. [ADR 0051](../docs/decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)
withdrew a 1.61× taken on a shared container for exactly that reason.

**`Benchmark (on demand)`** closes it: dispatch
`.github/workflows/bench-ondemand.yml` against any branch, name the subcommand,
and read the job summary. The subcommand is free text rather than a list, because
the list would be a promise about a ref the workflow has not checked out yet — a
branch adding one runs it the day it exists, without editing the workflow.

It **prints and does not publish** — nothing there writes a page, opens a pull
request or touches the wiki. A number becomes a published figure when a person
reads it and decides, which is what `performance.md`'s name-the-machine rule is
for. The workflow records `uptime`, the core count and the CPU model beside every
run so that decision has what it needs.

## 11. What renting the payload costs against allocating it (issue #470)

One primitive, two rows, interleaved in one process:

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- pool-cost
```

Allocating a 20 589 007-byte buffer against renting and returning one, both touching every page.
**Against an uninitialized allocation**, because `Buffers.AllocateUninitialized` is what the read
path uses — comparing a zeroed `new byte[]` would charge the pool's rival for a memset the code
does not do and inflate the saving by the whole zeroing.

On a hosted runner: allocate **1.783 ms** median against rent's **0.042**, so 42× and 1.74 ms a
load. The allocation's own minimum is 0.071 ms, as cheap as the rent — **what costs is the
large-object collection it provokes**, not the allocation. [ADR 0054](../docs/decisions/0054-the-payload-buffer-is-pooled-after-all-because-the-collection-is-the-cost.md)
is what that decided, amending 0053, which had refused pooling without ever timing it.

## 12. Where the .npy ingest's time goes (issue #480)

Ten phases, interleaved, each with the collections it provoked beside its milliseconds:

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- ingest-phases
```

Issue #466 removed a whole copy of the 15.36 MB block from `NpyFile.Read(Stream)` and the
published row did not move; removing the copy into the index moved it by more than a copy is worth. Both
readings came from subtracting whole rows, which cannot say where the time went. This takes the
ingest apart instead: the staged read, the zero-copy overload beside it, the `float[]` allocation
cold and reused, the payload read into an array that already exists, the header alone, `FromBlock`
against `FromOwnedBlock`, and a bare `memcpy` of the block as the floor every share is read
against.

**`parse_header_only` is the header through the memory overload**, not through the stream reader
whose parse the ingest actually pays, so what it prices is the parse plus that overload's own
floor. It lands on top of `read_memory_view` — 0.005–0.006 ms against 0.005–0.008 — and that is
the result rather than an accident of the phase: on a block this size the header is below the
table's resolution either way.

**`from_owned_adopt` never touches the array it allocates**, so by this file's own page-touching
argument it is charged for reserving the pages and not for committing them — which is exactly why
the row reads in hundredths of a millisecond. The reading is asymmetric on purpose:
`from_block_copy` − `from_owned_adopt` is a copy *plus* the destination's first touch, not the
copy alone. The surcharge is small on this table — `from_block_copy` 0.964–1.089 ms against
`block_copy_floor`'s 0.936–0.976 into a warm target — and touching the array would change what
the row measures rather than clean the subtraction up.

**`ingest_total` and `ingest_total_last` are one measurement at two positions.** They call the
same method, first in the round and last, so any difference between them is position and nothing
else. That is the single variable [decision 0058](../docs/decisions/0058-the-npy-ingest-is-memcpy-bound-and-the-allocation-is-not-the-cost.md)
left open: its gap survived the first table, and it named a phase-reordering run as what would
settle it. The `gen` columns are the half to read first — a collection provoked by one ingest is
paid by whichever phase runs next, not by the one that allocated.

**Two independent subtractions have to agree, and that is the point of the mode.**
`read_stream_owned - stream_copy_floor` is what the read's allocation costs inside the read;
`allocate_cold - allocate_reused` prices the same allocation on its own. If they disagree, the
allocation is not what the difference between those rows is made of.

The gen columns are collections **summed over the nine runs**, not per run: a block this size
provokes at most one gen2 per run, and a column of zeroes and ones says less than a total.
[ADR 0054](../docs/decisions/0054-the-payload-buffer-is-pooled-after-all-because-the-collection-is-the-cost.md)
is why they are there at all — on the artifact buffer the time and the collection count told
different stories, and only the second one explained the first.

Nothing here is published. It prints a table and writes no page: what it measures becomes a figure
when a person reads it on a named machine and decides, which is [section 10](#10-running-a-diagnostic-on-a-second-machine-issue-461)'s
rule and not this mode's exception. The first run's table, read that way, is in
[the performance guide](../docs/guides/performance.md#where-the-ingests-time-actually-goes-issue-480),
and what it refuted is [decision 0058](../docs/decisions/0058-the-npy-ingest-is-memcpy-bound-and-the-allocation-is-not-the-cost.md).

## 13. What a binary sidecar would buy (issue #436)

Six rows, interleaved one round each:

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- sidecar
```

The artifact against a `.npy` block plus the head a sidecar would still have to write, and six
timings: the artifact load, the block read, a **floor** — the read plus one copy into a backing
store, which is what a bulk ingest would do — the rebuild through `Add` that was the only route
before issue #474, and the two rows that lot added:

- `ingest copy` is the sidecar route as it will exist: the block read, then
  `EmbeddingIndex.FromBlock`. It is the row #474's gate is read off — landing with `sidecar floor`
  is the finding, and landing near `rebuild index` instead is the refusal.
- `ingest only` is the ingest alone, on a block already in hand, so a later regression in the read
  or in the ingest can be attributed to one of them rather than to their sum.

The floor is a bound in the sense section 8's `block_copy_floor` is one: a route measured so the
method that would take it can be judged against it.

**The floor and the ingest do not allocate the same way, and the asymmetry runs in the ingest's
favour.** `sidecar floor` takes its backing store from `new float[...]`, which the CLR zero-fills,
while `EmbeddingIndex.FromBlock` allocates through the library's own uninitialized allocation and
skips the zeroing. Both are one copy, and not the same kind of one: the ingest is ahead by one
memset of the block, about 15 MB at this corpus. It is stated rather than equalised, because the
uninitialized allocation is internal to the library and this project consumes the published
packages, and because re-cutting `sidecar floor` would invalidate the 5.847 ms
[ADR 0055](../docs/decisions/0055-the-artifact-gets-a-binary-sidecar-once-a-block-can-be-ingested-whole.md)
published, which is the bar this lot is judged against. The bias runs in the ingest's favour, so a
slow `ingest copy` is not an artefact of it: landing near `rebuild index` remains the refusal it
looks like. What it does mean is that `ingest copy` coming in *below* the floor must not be read as
beating it by the whole margin — part of that gap is the memset the floor pays and the ingest does
not.

There is no row for `FromOwnedBlock`. With `AlreadyNormalized` it assigns four fields and is
constant time whatever the block's size, so its ceiling is the `read npy block` row and a row of
its own would publish noise.

On a hosted runner: the sidecar is **1.331× smaller** and its floor is **2.02× faster** than the
artifact load, while the rebuild route is **0.66×** — slower than what it would replace.
[ADR 0055](../docs/decisions/0055-the-artifact-gets-a-binary-sidecar-once-a-block-can-be-ingested-whole.md)
takes the sidecar and makes the bulk ingest its precondition.

With that ingest built (#474), the same runner puts `load / ingest` at **1.45–1.62×** across three
rounds where `load / rebuild` is 0.62–0.65× — the route stops being slower than the artifact. It
does not reach the floor: `ingest / floor` is 1.13–1.26×, and the floor is the flattered side of
that comparison per the paragraph above. Every figure, with its rounds and its spreads, is in
[the performance guide](../docs/guides/performance.md#the-bulk-ingest-that-unblocks-it-issue-474);
this section says only how to take them.

**Not on a container.** There the same rows put the floor at 0.73×, the opposite conclusion, with
the floor row spread over 12–43 ms against the runner's 4.0–8.5.

## 14. Our kNN kernel against `TensorPrimitives` (issue #437, V6)

Seven rows, interleaved, agreement checked before any of them is timed:

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- tensor-primitives
```

Issue #427's open-risks table carries *"`TensorPrimitives` makes the kNN redundant"*, and V6 of
[#437](https://github.com/CyrilB1531/lodestar/issues/437) is the only verification there that wants
a measurement rather than a reading. This is it.

**The access pattern is the whole question.** `EmbeddingIndex` normalizes on insertion, so `Search`
is a dot product over a contiguous block rather than a cosine — comparing our dot against
`TensorPrimitives.CosineSimilarity` would charge the BCL for two norms we never compute. Both
shapes are measured for that reason, and a third pair sweeps the block in **one** call rather than
10 000 calls of 384 floats, because that is what `TensorPrimitives` is designed for and the kNN
rows do not show it.

Every row calls the shipped [`VectorMath.Dot`](../docs/reference/embeddings/search/vectormath-dot.md)
rather than a copy: a kernel reproduced for a benchmark is a kernel nobody ships, and the throwaway
probe that preceded this mode had rewritten the horizontal sum.

**Agreement runs first and prints before the table.** A speed comparison between two routes that
disagree is meaningless, so it is a precondition here rather than a footnote beside the result.

The package is referenced by `bench/` and by nothing under `src/`. V6 is a question about an
incumbent, and referencing it to ask would be answering it.

The first run's answer is [decision 0060](../docs/decisions/0060-tensorprimitives-beats-our-kernel-and-the-knn-is-still-not-redundant.md):
`TensorPrimitives` is 1.09–1.23× faster on the shape `Search` runs, and the kNN is still not
redundant, because the dot is only about half a query. **The container inverted every one of those
ratios** — it reported ours 3.7× faster on the dot — which is section 10's rule holding rather than
an aside about this mode.

## 15. Against the .NET incumbents (issue #438)

Every section above answers *"should you leave Python"*. `LevenshteinIncumbentBenchmarks` and
`FuzzIncumbentBenchmarks` answer the question a .NET reader asks first — *"why this over the .NET
library that already exists"* — which
[#438](https://github.com/CyrilB1531/lodestar/issues/438) requires of every package and which
nothing here measured until they landed.

```bash
dotnet run -c Release --project bench/Lodestar.Text.Benchmarks -- \
  --filter '*IncumbentBenchmarks*' --job short
```

Incumbents are referenced by `bench/` and by nothing under `src/`, pinned exactly in
`Lodestar.Text.Benchmarks.csproj`:

| class | baseline | incumbent |
| --- | --- | --- |
| `LevenshteinIncumbentBenchmarks` | `Levenshtein.Distance` | Fastenshtein 1.0.12, Quickenshtein 1.5.1, F23.StringSimilarity 7.0.1 |
| `FuzzIncumbentBenchmarks` | `Fuzz.*` | Raffinert.FuzzySharp 6.0.0 |
| `TokenizerIncumbentBenchmarks` | `WordPieceTokenizer`, `SentencePieceTokenizer` | Microsoft.ML.Tokenizers 2.0.0 |
| `VectorizerIncumbentBenchmarks` | `TfidfVectorizer` | ML.NET 5.0.0 `FeaturizeText` |

**The values agree before the clocks run.** A speed table over two functions that return different
answers means nothing. Checked over `kitten`/`sitting`, `flaw`/`lawn`, the sentence pair the fuzzy
class uses, an empty operand and an identical pair: all four Levenshtein implementations return the
same distance on all five, and Lodestar and FuzzySharp return the same ratio on all four operations
to the last digit of the double. The tokenizers were checked the same way, over 200 documents of
the corpus: identical ids, both models, once `Microsoft.ML.Tokenizers` is told not to prepend a
beginning-of-sentence piece — which is the sort of thing an unchecked table would have silently
measured. Section 14's rule, applied here as a precondition rather than a footnote.

Four shapes, for four reasons:

- **Levenshtein** takes `Length` at 8, 64 and 512, because the bit-parallel path is what the long
  row is for and a single length would hide it. `Levenshtein.Distance`'s default overload is the
  baseline: all four libraries compare UTF-16 code units, so `TextElement.CodePoint` would be
  measuring something none of the incumbents offers.
- **The fuzzy ratios** take the *operation* as the parameter — `Ratio`, `PartialRatio`,
  `TokenSetRatio`, `WRatio` — rather than writing eight methods. With one baseline for the whole
  class BenchmarkDotNet compares `PartialRatio` against `Ratio` instead of against its counterpart,
  which is not the question; as a parameter, each pair gets its own baseline row.
- **The tokenizers** take the model as the parameter for that same reason, and encode all 5 000
  documents of `bench/corpus/vocabs/documents.json` per operation, so the number is encoding cost
  rather than model loading — both tokenizers are built once in `[GlobalSetup]`, as
  `BpeBenchmarks` does.
- **The vectorizers** are the one pair that is *not* like-for-like, and the next section is what
  that costs the reading.

### `FeaturizeText` is not our vectorizer, and the ratio alone would lie

The other two classes compare functions that return the same answer. This one does not, and #438
says so itself: `FeaturizeText` is `IDataView`-coupled and does a different job, so *"a row
treating them as equals would contradict our own argument."*

Measured on the same corpus rather than asserted:

| documents | Lodestar `TfidfVectorizer` | ML.NET `FeaturizeText` |
| ---: | --- | --- |
| 200 | 200 × 7 018 sparse, 7 996 stored values | 200 × 31 112 dense, 70 307 non-zero, 6 222 400 floats materialized |
| 1 000 | 1 000 × 21 867 sparse, 39 974 stored values | 1 000 × 81 384 dense, 351 217 non-zero, 81 384 000 floats materialized |

`FeaturizeText` adds character n-grams to the word n-grams and L2-normalizes, so its feature space
is roughly four times wider and it produces about **8.8× more non-zero features**. It is doing more
work, and a wall-clock ratio that ignores that is not a claim anyone should believe.

Divide it out and the honest number appears. At 1 000 documents the container run measures 45.6 ms
against 445.0 ms — 9.75× — for 39 974 and 351 217 non-zero features respectively: **1.14 µs and
1.27 µs per feature produced, within about 11 % of each other.**

So the advantage is not that the arithmetic is faster. It is that a sparse matrix stores 39 974
values where the dense pipeline materializes 81 million floats for the same thousand documents, and
that ours needs no `IDataView`, no schema and no pipeline object to hand back a `CsrMatrix`. That
is the argument the README makes, and it is the one this measurement supports — no more.

**Lucene.NET is deliberately not here.** #438 names it "where it overlaps", and on the vectorizer
it does not: its TF-IDF exists inside an index, reached through an `IndexSearcher`, so comparing a
library call to an indexing engine would repeat exactly the category error this section exists to
avoid. Its analysis chain does overlap our tokenizers, which is a different measurement and a
different lot.

**Where the numbers may be published.** Not from a container. Section 10's rule holds here with no
exception — [ADR 0051](../docs/decisions/0051-the-save-paths-cost-is-the-buffer-not-the-encoding.md)
withdrew a 1.61× taken on a shared container, and section 14 records the container *inverting* every
`TensorPrimitives` ratio. A container run of these two classes is a smoke test that the harness
works, and nothing else. `docs/guides/performance.md` takes them from a named machine; the nightly
publishes their ratios to `docs/guides/nightly_run.md` on its own, since all three classes are in
`bench-map.json` and are selected by any change under `src/Lodestar.Fuzzy/`,
`src/Lodestar.Text/Distances/`, `src/Lodestar.Text/Vectorization/` or
`src/Lodestar.Embeddings/Tokenization/`.
