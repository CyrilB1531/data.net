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

**Left upstream:** the repository's own `hooks/`. Wiring a hook changes how every session in this
repository behaves before anyone asks it to, which is a decision of its own and not part of making
a documented format reachable.

## One difference from the installed plugin

Vendored as repository skills they carry no `superpowers:` namespace. A plan whose header reads
`REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development` names the plugin form; here the
skill answers to `subagent-driven-development`. Existing plans keep the namespaced spelling because
they were written against the plugin and rewriting history to match a packaging choice would be the
wrong repair.

## Updating

Re-clone at a chosen tag, copy `skills/` and `LICENSE` over, and update the version and commit at
the top of this file in the same commit. If the upstream `skills/` gains a directory, take it or
say here why not — a silent omission is the failure this file exists to prevent.
