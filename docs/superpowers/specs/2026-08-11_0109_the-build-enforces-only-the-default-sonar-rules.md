# 0109 — The build enforces only the Sonar rules the analyzer enables by default

**Issue:** [#109](https://github.com/CyrilB1531/data.net/issues/109) · **Date:** 2026-08-11 ·
**Branch:** `chore/109-sonar-rule-parity` · **Checkout:** `/home/cyril/Documents/devs/data.net`

## Context

[#84](https://github.com/CyrilB1531/data.net/issues/84) put `SonarAnalyzer.CSharp` in the build so that
"a finding is a compile error on the machine that wrote the code"
([ADR 0015](../../decisions/0015-sonar-rules-in-the-build.md)).
[#107](https://github.com/CyrilB1531/data.net/issues/107) closed the half of the gap that belonged to the
.NET code-quality rules, by setting `AnalysisMode=All`
([ADR 0019](../../decisions/0019-the-net-analysers-run-in-the-build-too.md)).

The half left open is SonarAnalyzer's own: the package ships a large fraction of its rules **disabled**,
the server's quality profile enables some of them, and nothing local closes that difference. The loop for
those rules is still push, wait three minutes, read the gate.

It is not hypothetical. Pull request #126 — merged on 2026-08-11, the day this was written — failed its
quality gate on a single `csharpsquid:S1192` in
`src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`, while `dotnet build DataNet.slnx -c Release
--no-incremental` reported `0 Avertissement(s) 0 Erreur(s)` and the `Build, test, pack` job passed. The
fix was one constant. The issue records two more of the same shape on #86, `csharpsquid:S107` and
`csharpsquid:S2245`.

## Measurements taken before deciding

Every line below was measured on this checkout on 2026-08-11, against
`SonarAnalyzer.CSharp` **10.20.0.135146** — the version pinned in the root `Directory.Build.props`.

| Question | How it was measured | Answer |
| --- | --- | --- |
| Is the server profile readable without a token? | `GET api/qualityprofiles/search?organization=cyrilb1531&project=CyrilB1531_data.net`, anonymous | **Yes.** C# profile is `Sonar way`, key `AZF_RqJ__mc37gztrQ3P`, **380 active rules** |
| Which rules does the package ship disabled? | `-p:ErrorLog=<path>%2Cversion=2` on one project, then read `runs[0].tool.driver.rules[]` | **450** `Sxxxx` rules declared, **138** carry `defaultConfiguration.enabled: false` |
| Are the issue's three examples among them? | the same table | `S107` **disabled**, `S1192` **disabled**, `S3776` **disabled**, `S2245` **enabled** |
| Does a severity entry wake a rule in the build? | throwaway `.editorconfig` with `dotnet_diagnostic.S1192.severity = warning`, `dotnet build src/DataNet.Embeddings` | **Yes.** `TokenizerJsonLoader.cs(769,34): warning S1192 … 'add_prefix_space' 4 times`, and nothing after the fix |
| Is a container runtime available? | `command -v docker`, `podman --version` | both present |
| Does a config file exist to extend? | `ls .globalconfig .editorconfig` | neither exists today |

Two of these deserve to be read carefully rather than skimmed.

**`S2245` is enabled by default in 10.20.0.135146.** The issue's second piece of evidence says the build
never mentioned two `new Random` sites on #86. Against the version pinned today, that rule is *not* in the
disabled set, so either the analyser version has moved since #86 or something else hid those findings —
`bench/`'s own `NoWarn`, or the `#pragma` five bench files already carry. **The plan measures this before
it asserts anything**, and the acceptance criterion that names `S2245` is verified against what is
measured, not against what the issue assumed.

**The delta needs no reflection and no second tool.** The SARIF v2 error log the compiler already knows how
to write names every rule the analysers declare *and* whether each is enabled by default. Deriving the
delta is therefore an offline read of a build artefact intersected with one anonymous HTTP response.

## Decisions

### D1 — a generated `.globalconfig`, declared by nothing, not an `.editorconfig`

The probe above used an `.editorconfig` because it was the fastest thing to throw away. The committed
mechanism is a `.globalconfig` at the repository root.

The reason for the global config over the editor config: it can carry **only** analyzer severities, so it
cannot be confused with the formatting conventions `dotnet format` enforces — and `CONTRIBUTING.md` already
warns readers away from `.editorconfig`, for SonarLint reasons that remain true.

**Amended after Task 3, 2026-08-12.** This decision originally added a
`<GlobalAnalyzerConfigFiles Include="$(MSBuildThisFileDirectory).globalconfig" />` item to the root
`Directory.Build.props`, on the premise that a `.globalconfig` is picked up automatically only from the
project's own directory. That premise is false for this SDK, and the item it justified **breaks the
feature**. Measured twice, independently, on SDK 10.0.110: `Microsoft.Managed.Core.targets` globs every
*ancestor* directory of every compiled file for a file literally named `.globalconfig` whenever
`DiscoverGlobalAnalyzerConfigFiles` is not `false` — which nothing here sets — so the repository root is
already in scope for every project, `samples/` included, with no wiring at all. Registering the same path a
second time makes MSBuild log `MultipleGlobalAnalyzerKeys` and **drop the severity of every rule in the
file**: the `S107` canary that failed the build under discovery alone went quiet the moment the explicit
item was added.

So nothing declares the file. What sits in `Directory.Build.props` is a comment saying that, saying why an
`Include` must not be added back, and saying what whoever disables discovery would have to write instead.
The lesson is the same one the measurement section already carries: a mechanism that is asserted rather
than demonstrated can be exactly backwards, and only the canary tells the difference.

### D2 — the file carries the delta only, at `warning`

One entry per rule that is **active on the server and shipped disabled** by the package:

```ini
dotnet_diagnostic.S1192.severity = warning
```

Not `error`. `TreatWarningsAsErrors` in the root `Directory.Build.props` is already the lever that decides
whether a finding stops the build, and a second lever saying the same thing would be one to forget when the
first one changes.

Not the whole profile either. Writing all 380 active rules would restate what already works and bury the
lines that carry information. Every line in this file answers "why is this one here": because the server
wants it and the package does not give it.

Rules the package enables but the profile does not carry are left alone. Turning them off would silence
warnings that are useful locally, and the build is allowed to be stricter than the gate — it is only not
allowed to be *looser*.

### D3 — the generator resolves the project's profile, not the organization's default

`tools/generate_sonar_globalconfig.py`, run offline apart from one HTTP call:

- **Input A** — the active rules of the profile associated with `CyrilB1531_data.net` for language `cs`,
  through `api/qualityprofiles/search?organization=…&project=…` then `api/rules/search?activation=true`.
  Resolved *per project* rather than by taking the organization's default `Sonar way`, so the day a custom
  profile is attached the generator follows it instead of quietly describing a profile nobody uses.
- **Input B** — `runs[0].tool.driver.rules[]` from a SARIF v2 error log produced by a build of this
  repository, which is where `enabled: false` comes from.
- **Output** — `.globalconfig`, deterministic, sorted by rule id, with a header naming the profile key, the
  analyzer version and the two commands that produced it. **No timestamp**: a generated file that changes
  every day cannot be drift-checked.

### D4 — drift is checked on every pull request, and a network failure is not drift

The same script gains `--check`: it regenerates into memory and compares against the committed file,
changing nothing. The `Lint` job runs it on every pull request, which is the cost accepted here — one call
to `sonarcloud.io` per run — in exchange for learning about a profile change on the pull request that first
meets it rather than a week later.

The two failure modes get two exit codes and two messages. A profile that moved says so and prints the
diff; an API that could not be reached says *that*, and says it loudly. A check that goes green because the
server was silent would be worse than no check, and this repository has been bitten by exactly that shape
before — a `--filter` matching no test, a drift job comparing nothing.

### D5 — the generator gets a pytest suite, which is new infrastructure and is stated as such

`tools/` has no Python test suite today. This adds one, `tools/tests/`, with `pytest` in
`tools/requirements.txt` (and the hashed lock), a CI step that runs it, and frozen fixtures: a trimmed SARIF
rule table, a trimmed `api/rules/search` response. What it proves is what a drift check cannot — that the
parsing, the intersection and the output format are *right*, not merely stable. A generator that is
consistently wrong passes a drift check every time.

The fixtures are trimmed by hand from real responses and committed, so the tests need no network.

### D6 — the acceptance criteria are asserted on the file, and demonstrated once on the build

The issue's first two criteria are "re-introducing an 8-parameter method fails `dotnet build`" and the same
for an unsuppressed `new Random`. They are covered twice, deliberately:

- **Asserted, permanently**: a test in `tools/tests/` that the generated `.globalconfig` raises `S107`, and
  `S2245` too if the plan's first task finds it disabled in the pinned analyser after all. Where a criterion
  turns out to be about a rule that is *already* enabled, the test asserts that instead, and the plan says
  which of the two it measured. Offline either way, and it fails the day a regenerated file loses a rule the
  criteria named.
- **Demonstrated, once**: the plan compiles a method with eight parameters and records the build output in
  its report. End-to-end proof that the wiring reaches the compiler, without leaving behind a project whose
  job is to not compile.

A permanent canary project was considered and rejected: code that must fail to build is code that will one
day fail to build for the wrong reason, and the failure would look identical.

### D7 — a local SonarQube container covers what no .NET build can, and says what it does not reproduce

`tools/sonarqube-local/compose.yaml`, pinned by image digest, plus a `CONTRIBUTING.md` section beside the
definition of done: start the container, wait for it, create a token, run `dotnet-sonarscanner` against
`localhost:9000`, with the cost stated — image size, RAM, first-start time.

What it covers that nothing else local does: **the Python rules on `tools/`**, which no .NET build can ever
reach; **duplication**; and **coverage**, from the OpenCover reports CI already produces.

What it does **not** reproduce, and the documentation says so in the same breath:

- the custom `No new issue` quality gate — the Community edition ships `Sonar way` and the gate's seven
  conditions are not there;
- any notion of *new code*, because branch and pull-request analysis are commercial features, so the local
  verdict is over the whole project rather than over the diff;
- the exact analyser versions the server runs, which move independently of the image tag.

It is a finder, not an oracle. A finding it reports is real; a clean run does not promise a green gate.

### D8 — existing findings are measured before they are treated

Enabling the delta will surface findings on code that is already committed. #107 met 655 of them and
decided per rule; the same discipline applies, and the count is unknown until the plan's first task runs.
Each finding is then fixed, or suppressed with a reason at the call site, or exempted for a whole area in
that area's `Directory.Build.props` with a comment naming the rule — the three routes `CONTRIBUTING.md`
already defines. Nothing is exempted for being noisy.

The four unsuppressed `new Random` sites the issue names —
`tests/DataNet.Embeddings.Tests/BpeTokenizerTests.cs:404,467` and
`tests/DataNet.Text.Tests/Distances/LevenshteinPropertyTests.cs:32,61` — are in scope here rather than
deferred, since S2245 is enabled in the pinned analyser and they would otherwise be a build failure the
moment anything else changes in those files.

### D9 — `CONTRIBUTING.md` says which tool it is talking about

The paragraph that reads "Do not reach for `.editorconfig` … for SonarLint rules" is correct about
SonarLint and wrong as a blanket statement: the build's Roslyn pass reads exactly that kind of file, and
this change relies on it. The paragraph gains the distinction, and points at the `.globalconfig` as the
lever that does work for the build.

## Evidence

- The generator: the pytest suite of D5, plus the drift check of D4.
- The wiring: the demonstration of D6, with build output in the plan's report.
- The container: a recorded run against this repository, with the finding count it produces and how long it
  took, so `CONTRIBUTING.md`'s "what it costs" is measured rather than guessed.
- The delta itself: the plan reports how many rules it contains and how many findings they produce, per
  rule, before deciding anything about them.

## Documentation

- `CONTRIBUTING.md` — the pre-push scanner section (D7), and the `.editorconfig` correction (D9).
- An ADR only if a decision here diverges from what ADR 0015 and ADR 0019 already record. Extending the
  same idea with a second configuration file is a continuation of both, not a divergence, so the default is
  **no ADR** — and if the measurement of D8 makes us disable a rule the server enables, that *is* a
  divergence and gets one.
- `.globalconfig` is generated, so its header documents itself; `tools/README.md` gains the generator and
  the container the way it lists the other scripts.

## Out of scope

- Raising `AnalysisLevel` past `10.0` or bumping `SonarAnalyzer.CSharp` — each surfaces new rules and is
  its own change, per ADR 0015.
- The rules SonarCloud computes that no analyser here runs: duplication and coverage stay server-side, and
  the container of D7 is the only local answer to them.
- Making the pre-push container run in CI. CI already has the real server.

## Risks

- **The API shape can change.** The generator depends on two SonarCloud endpoints that carry no
  compatibility promise. The drift check is what would notice, and it fails loudly rather than silently
  (D4).
- **The delta can be large.** If the 138 disabled rules intersect the profile widely, D8's cleanup could
  dwarf the mechanism. The plan measures first and the scope is re-cut on the number, rather than the
  cleanup being discovered halfway through.
- **The container is heavy.** SonarQube Community needs a gigabyte-scale image and `vm.max_map_count`
  tuning on Linux; if the recorded run shows it costs more than it saves, D7 shrinks to a documented
  command with its caveats rather than a committed compose file — and the documentation says which.
