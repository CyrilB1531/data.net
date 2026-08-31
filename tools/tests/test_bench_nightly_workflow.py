"""The nightly's permissions are keys GitHub accepts, and the publish runs no branch code.

Issue #464 was first fixed by asking for `workflows: write`. There is no such
permission scope: GitHub refused the whole file with "Unexpected value 'workflows'",
which takes a workflow out of service rather than failing one job. Decision 0067 has
the rest. The first test below is what would have caught it before the merge.
"""

from __future__ import annotations

import pathlib
import re

import pytest
import yaml

WORKFLOWS = pathlib.Path(__file__).resolve().parents[2] / ".github" / "workflows"
NIGHTLY = WORKFLOWS / "bench-nightly.yml"

# GitHub's `permissions:` scopes. A key outside this set is not a narrower grant --
# it is a parse error that disables the workflow entirely.
SCOPES = frozenset(
    {
        "actions",
        "attestations",
        "checks",
        "contents",
        "deployments",
        "discussions",
        "id-token",
        "issues",
        "models",
        "packages",
        "pages",
        "pull-requests",
        "repository-projects",
        "security-events",
        "statuses",
    }
)

# What "runs repository code" looks like in a `run:` block. `git` and `gh` are the
# runner's own binaries and are what the publish is allowed to call.
REPOSITORY_CODE = re.compile(r"(?<![\w/-])(python3?|dotnet|pip|npx|node)\s", re.MULTILINE)

PINNED = re.compile(r"^[\w.-]+/[\w.-]+(/[\w.-]+)*@[0-9a-f]{40}$")


def _blocks(workflow):
    """Every permissions block in the file, workflow-level and per job."""
    blocks = []
    if isinstance(workflow.get("permissions"), dict):
        blocks.append(("<workflow>", workflow["permissions"]))
    for name, job in workflow["jobs"].items():
        if isinstance(job.get("permissions"), dict):
            blocks.append((name, job["permissions"]))
    return blocks


@pytest.fixture(scope="module")
def workflow():
    return yaml.safe_load(NIGHTLY.read_text(encoding="utf-8"))


@pytest.mark.parametrize("path", sorted(WORKFLOWS.glob("*.yml")), ids=lambda p: p.name)
def test_every_permission_key_is_a_scope_github_knows(path):
    parsed = yaml.safe_load(path.read_text(encoding="utf-8"))
    for where, block in _blocks(parsed):
        unknown = sorted(set(block) - SCOPES)
        assert not unknown, f"{path.name}: {where} asks for {unknown}, which GitHub refuses"


def test_the_publish_job_runs_no_repository_code(workflow):
    for step in workflow["jobs"]["publish"]["steps"]:
        script = step.get("run")
        if script is None:
            continue
        found = REPOSITORY_CODE.search(script)
        assert found is None, f"publish: step calls {found.group(1)!r}"


def test_the_publish_job_uses_only_pinned_actions(workflow):
    for step in workflow["jobs"]["publish"]["steps"]:
        uses = step.get("uses")
        if uses is None:
            continue
        assert PINNED.match(uses), f"publish: {uses} is not pinned to a commit"


def test_the_publish_waits_for_the_measurement(workflow):
    assert workflow["jobs"]["publish"]["needs"] == "measure"


def test_the_publish_stands_down_when_the_workflows_differ(workflow):
    # The guard is what keeps a ref that touches .github/workflows/ from spending a
    # nightly window on a push GITHUB_TOKEN can never make (#464).
    scripts = "\n".join(
        step.get("run", "") for step in workflow["jobs"]["publish"]["steps"]
    )
    assert "git diff --quiet FETCH_HEAD HEAD -- .github/workflows/" in scripts
    assert "GITHUB_STEP_SUMMARY" in scripts


def test_the_measurement_job_cannot_open_a_pull_request(workflow):
    # It runs the measured branch's own code; the token it holds is the smallest that
    # still lets it publish the wiki.
    assert workflow["jobs"]["measure"]["permissions"] == {"contents": "write"}
