# Vendored skills

[obra/superpowers](https://github.com/obra/superpowers), **v6.3.0**, upstream commit
[`b36e082`](https://github.com/obra/superpowers/commit/b36e0829c6d0140e93cfef2ca599b1b07d4a7797).
MIT, © 2025 Jesse Vincent — `LICENSE` beside this file is upstream's, unmodified, which is what
MIT asks of a redistribution.

## Why they are here rather than installed

[`CLAUDE.md`](../../CLAUDE.md) requires design specs and implementation plans under
`docs/superpowers/`, and these skills are what writes them. They were not obtainable in every
environment the project is worked from: `superpowers` is not in the plugin catalogue reachable from
a hosted session, and `~/.claude/plugins/synced` is a sync target rather than an install directory.
So a session without them reproduced the format by reading a neighbouring file, which is how two
plans came to fail `writing-plans`' own self-review — coarse steps where it asks for 2–5 minute TDD
ones, and prose where its **No Placeholders** rule requires code.
[#454](https://github.com/CyrilB1531/lodestar/issues/454) is the record.

Vendored, they are available to any session working in this repository, hosted ones included, and
they are pinned: an update is a deliberate commit rather than a drift under a running session.

## What was taken, and what was not

**Taken:** `skills/` in full — 14 skills, and the `scripts/` each carries as part of itself
(`brainstorming`'s browser companion, `subagent-driven-development`'s workspace helpers,
`systematic-debugging`'s polluter finder, `writing-skills`' graph renderer).

**Left upstream, and why each:**

- **`hooks/`.** Wiring a hook changes how every session in this repository behaves before anyone
  asks it to, which is a decision of its own and not part of making a documented format reachable.
- **`brainstorming/scripts/`, the visual companion.** 1 319 lines including a 723-line local HTTP
  server, and the skill's own text calls it optional — offered "just-in-time … if no visual
  question ever arises, never offer it". It also cannot work from a hosted session, which has no
  browser tab to open. It is where SonarCloud's code scanning raised three high-severity alerts on
  [#455](https://github.com/CyrilB1531/lodestar/pull/455), and removing it is the fix; excluding
  `.claude/**` from analysis was tried first and is the wrong shape, because it silences a detector
  rather than removing what it detected. **Anyone wanting the companion has it in their own
  install**, which is where a browser feature belongs.

`subagent-driven-development/scripts/` **stays**: its SKILL.md instructs `scripts/sdd-workspace`
and `scripts/task-brief` by name, so those three helpers are load-bearing rather than optional.

## One difference from the installed plugin

Vendored as repository skills they carry no `superpowers:` namespace. A plan whose header reads
`REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development` names the plugin form; here the
skill answers to `subagent-driven-development`. Existing plans keep the namespaced spelling because
they were written against the plugin and rewriting history to match a packaging choice would be the
wrong repair.

## What CI does with them

`sonar.exclusions` and `sonar.coverage.exclusions` name `.claude/**`, and that was established by
experiment rather than assumed. Three runs on [#455](https://github.com/CyrilB1531/lodestar/pull/455):

| `.claude/**` excluded | companion present | quality gate | code scanning |
| :---: | :---: | --- | --- |
| no | yes | FAILED | 9 alerts |
| yes | yes | PASSED | 9 alerts |
| no | no | FAILED | clean |

The gate follows the exclusion and the security alerts follow the companion — two independent
things, and an earlier commit here claimed "nothing is excluded from anything", which the third
run disproved. **The gate's condition is not one better code would satisfy**: it wants coverage on
new code, and a dependency's shell helpers cannot be unit-tested into this project's coverage
report.

**What the exclusion does not silence is the part that matters.** SonarCloud also exports to GitHub
code scanning, and that channel found the 723-line HTTP server regardless. Security findings still
arrive.

`check_machine_paths` and `check_comment_length` are **not** excluded either — they see every
tracked file and pass over this tree, which is worth knowing before taking an upstream version
that would not.

## Updating

Re-clone at a chosen tag, copy `skills/` and `LICENSE` over, and update the version and commit at
the top of this file in the same commit. If the upstream `skills/` gains a directory, take it or
say here why not — a silent omission is the failure this file exists to prevent.
