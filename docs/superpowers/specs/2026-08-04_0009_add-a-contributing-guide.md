# Design — #9: a CONTRIBUTING guide

**Date:** 2026-08-04 · **Issue:** #9 · **Branch:** `docs/9-contributing-guide` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

The conventions this project actually follows exist only in the commit history:
branch naming, one concern per branch, the oracle procedure, where suppressions
go, the provenance rule. A new contributor — or the same maintainer in three
months — has to infer them from diffs.

Two of them are worse than merely undocumented: they cost real time to
rediscover, and neither is guessable from the code.

## Decisions

### D1 — Document what exists today, and nothing else

Written against `main` as it stands. **No forward references**: nothing about a
changelog (#8), `netstandard2.0` (#1) or the comparison benchmark suite (#10),
all of which are open branches. Those sections get added by their own pull
requests.

A guide that describes a repository state that does not exist is worse than no
guide — it is a guide the first reader learns to distrust.

### D2 — Explain why `main` cannot require approving reviews

The project has one maintainer. **GitHub does not let anyone approve their own
pull request**, so a rule requiring an approving review would block every pull
request here, with nobody able to unblock it.

Protection rests on required status checks instead. This is the sort of decision
that looks like laziness unless the reasoning is written down, and the next person
to "tighten security" would otherwise lock the repository.

### D3 — The two hard-won notes get their own space

Both cost real time and are invisible from the source:

- **The oracle generator must run from a neutral working directory** with
  `PYTHONSAFEPATH` set. `nltk` refuses to import its own dependencies when they
  appear to live under the current directory, so the run fails with
  `ImportError: Blocked import of regex from current working directory` — even
  from the repository root, even with the flag set.
- **SonarLint reads neither `.editorconfig` nor a workspace
  `.vscode/settings.json`.** `sonarlint.rules` is declared application-scope in
  the extension manifest, so VS Code silently drops it from a workspace file. That
  is *why* suppressions belong in the source, and without the explanation the rule
  reads as arbitrary.

Quote the actual error text. A reader searching for their error message finds the
page; a paraphrase does not match.

### D4 — Definition of done, as a list that can be checked

Build clean under repository-wide warnings-as-errors, tests pass, new algorithms
replay an oracle corpus, lint clean, public API documented with the Python
function it matches. Each item is a command a contributor can run, not a
sentiment.

### D5 — The new file joins the gate that guards it

`CONTRIBUTING.md` is added to the markdownlint glob in `ci.yml`. A documentation
file adjacent to a gate rather than inside it will drift, and this one is about to
be the most-edited document in the repository.

### D6 — Every claim is verified before the PR

Every link target resolves; the CI job names quoted match `ci.yml` **exactly**,
because required checks are configured by name; `TreatWarningsAsErrors` is
confirmed at the repository root rather than assumed from memory.

## Out of scope

- Branch protection configuration itself (#12).
- Anything about releases, which has no procedure yet.

## What "done" means

`CONTRIBUTING.md` describing only what exists; the file inside the lint glob;
every quoted job name and link checked; markdownlint 0 issues.
