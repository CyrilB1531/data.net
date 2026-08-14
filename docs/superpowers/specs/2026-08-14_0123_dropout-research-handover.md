# 0123 — research handover: is `dropout` ever reproduced?

**Issue:** [#123](https://github.com/CyrilB1531/data.net/issues/123) · **Date:** 2026-08-14 ·
**Status:** research only. No spec, no plan, no code. Everything below was measured; nothing was decided.

This is a handover, not a design. It exists so the lot that takes #123 does not re-run the measurements.

## What the issue asks, and what is answered

#123 names four questions. Three are answered below; the fourth is a decision nobody has taken.

| question | answer |
| --- | --- |
| load-time or encode-time? | **encode-time**, measured |
| does any shipped model declare it? | **none of 23 read**, measured |
| what does a user with such a file do instead? | not decided |
| where does the decision live? | not decided |

## D1 — dropout is encode-time, and non-deterministic per call

One tokenizer, vocabulary `{a, b, c, ab, abc}`, merges `(a,b)` then `(ab,c)`, `dropout=0.5`,
`Whitespace` pre-tokenizer. Twelve `encode("abc")` calls on the **same** object:

```
distinct results: ('a', 'b', 'c'), ('ab', 'c'), ('abc',)
```

At `dropout=0.0` the same loop gives `('abc',)` twelve times out of twelve.

So the refusal cannot rest on "a load-time setting we cannot represent". The randomness fires per
`encode` call, which makes refusing it a choice about behaviour rather than an impossibility.

## D2 — `tokenizers` 0.23.1 exposes no seed, anywhere

```
tokenizers 0.23.1
seed-ish attrs on tokenizers: []
seed-ish attrs on models.BPE: []
python random.seed reproduces? False
 first: [('a','b','c'), ('ab','c'), ('ab','c'), ('a','b','c'), ('a','b','c'), ('abc',), ('a','b','c'), ('a','b','c')]
second: [('a','b','c'), ('a','b','c'), ('a','b','c'), ('a','b','c'), ('a','b','c'), ('a','b','c'), ('a','b','c'), ('abc',)]
```

Both runs ran `random.seed(42)` first. The sequences differ, because the randomness is Rust's
`thread_rng` and Python's seed does not reach it.

**This is the finding that matters, and it is stronger than the argument the code currently makes.**
`TokenizerJsonLoader`'s message says dropout "drops merges at random during tokenization, which no
deterministic tokenizer reproduces". True, but it is not the reason. DataNet *could* implement
BPE-dropout with its own seeded RNG. What it could never do is **prove parity**, because this repository
proves every algorithm by replaying values frozen from the reference — and a reference that cannot be
pinned cannot be frozen. Dropout is the one setting where the oracle method is structurally unavailable.

Shipping it would mean shipping a parity claim no corpus could ever check, which is what `CLAUDE.md` and
[ADR 0003](../../decisions/0003-provenance-and-licensing.md) exist to prevent.

## D3 — no model that could be read declares a non-null dropout

Fetched on 2026-08-14 from `https://huggingface.co/<repo>/raw/main/tokenizer.json`, falling back to
`/resolve/main/` for the two whose file is LFS-backed (`/raw/` returns the pointer, which is why a first
pass recorded them as unparseable).

| group | read | of which BPE | non-null `dropout` |
| --- | ---: | ---: | ---: |
| the fifteen #121 surveyed, for comparability | **15** | 13 | **0** |
| chosen to find a positive if one exists | 8 | 6 | **0** |
| **total** | **23** | **19** | **0** |

The fifteen: `EleutherAI/gpt-neox-20b`, `EleutherAI/pythia-160m`, `Qwen/Qwen2-0.5B`,
`allenai/OLMo-1B-hf`, `deepseek-ai/deepseek-coder-1.3b-base`, `gpt2`, `roberta-base`,
`facebook/bart-large`, `bigscience/bloom-560m`, `tiiuae/falcon-7b`, `microsoft/phi-2`,
`bigcode/starcoder2-3b`, `HuggingFaceTB/SmolLM2-135M`, `stabilityai/stablelm-2-1_6b`,
`Salesforce/codegen-350M-mono`.

The eight: `facebook/xglm-564M`, `RWKV/rwkv-4-169m-pile`, `EleutherAI/gpt-j-6b`, `bigcode/santacoder`,
`codeparrot/codeparrot-small`, `facebook/nllb-200-distilled-600M`, and Llama-3 through both ungated
mirrors [ADR 0017 §5](../../decisions/0017-bpe-parity-scope.md) established —
`NousResearch/Meta-Llama-3-8B` and `unsloth/llama-3-8b`.

**Eight could not be read, and why is itself a result:**

- **five return 404** — `Helsinki-NLP/opus-mt-en-de`, `facebook/m2m100_418M`, `google/mt5-small`,
  `replit/replit-code-v1-3b`, `Salesforce/codet5p-220m`. These are the NMT and multilingual models
  chosen *because* subword regularization comes from that lineage, and they ship no `tokenizer.json` at
  all. **They cannot declare BPE-dropout, because they do not use the format that has the field.**
- **three return 401**, gated — `cerebras/Cerebras-GPT-111M`, `databricks/dolly-v2-3b`,
  `mosaicml/mpt-7b`.

**State the limit honestly wherever this lands.** HuggingFace has no way to search *inside*
`tokenizer.json`, so this is a sample chosen by family, not a scan. A zero over it evidences the
**convention**, not the absence.

## What is NOT decided

- **Keep refusing, or reproduce under a caller-supplied seed.** D2 is the argument for refusing, and it
  is a strong one — but it is an argument, not a decision, and #123 says explicitly that if the answer
  goes the other way the issue closes and a fresh one carries the implementation.
- **What a user with such a file is told to do.** The obvious answer is that dropout is a *training-time*
  augmentation and inference wants determinism, so setting the field to `null` loads the file and changes
  nothing about what the model was trained to produce. [ADR 0017 §3](../../decisions/0017-bpe-parity-scope.md)
  is the standard to match: it names Llama-2 and Mistral v0.1 and says where to go instead.
- **Whether a distributional comparison counts as proof.** Comparing output *distributions* over many
  encodes instead of exact outputs is the obvious escape hatch from D2. Whoever takes this should name it
  and rule on it rather than leave it unconsidered — it proves a distribution rather than a behaviour,
  needs a large sample for a tight bound, and would land in the one CI job already known to be flaky.
- **Where the decision lives.** `../data.net/.next-adr` reported **0034** free on 2026-08-14.

## How to redo the survey in ten lines

No tool was committed — #123 expects no new behaviour, and a survey script is not part of a written
decision. The method is:

```python
import json, urllib.request
for repo in REPOS:                      # try /raw/main first; /resolve/main for LFS-backed files
    url = f"https://huggingface.co/{repo}/raw/main/tokenizer.json"
    doc = json.loads(urllib.request.urlopen(url, timeout=120).read())
    print(repo, (doc.get("model") or {}).get("type"), (doc.get("model") or {}).get("dropout"))
```

A 404 means the repository ships no `tokenizer.json`; a 401 means it is gated; an unparseable body from
`/raw/` means the file is LFS-backed and needs `/resolve/`.
