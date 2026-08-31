"""`workflows: write` stays in a job that runs no repository code.

Issue #464: pushing the nightly's results branch off a measured ref that touched
`.github/workflows/` is a workflow write, so the publish needs that permission.
The job that runs the measured branch's own benchmarks must not hold it.
"""

from __future__ import annotations

import pathlib
import re

import pytest
import yaml

WORKFLOW = (
    pathlib.Path(__file__).resolve().parents[2]
    / ".github"
    / "workflows"
    / "bench-nightly.yml"
)

# What "runs repository code" looks like in a `run:` block. `git` and `gh` are the
# runner's own binaries and are what the publish is allowed to call.
REPOSITORY_CODE = re.compile(r"(?<![\w/-])(python3?|dotnet|pip|npx|node)\s", re.MULTILINE)

PINNED = re.compile(r"^[\w.-]+/[\w.-]+(/[\w.-]+)*@[0-9a-f]{40}$")


@pytest.fixture(scope="module")
def workflow():
    return yaml.safe_load(WORKFLOW.read_text(encoding="utf-8"))


def _elevated(workflow):
    return {
        name
        for name, job in workflow["jobs"].items()
        if (job.get("permissions") or {}).get("workflows") == "write"
    }


def test_exactly_one_job_may_write_workflows(workflow):
    assert _elevated(workflow) == {"publish"}


def test_the_job_that_runs_the_benchmarks_is_not_that_job(workflow):
    assert "workflows" not in (workflow["jobs"]["measure"].get("permissions") or {})


def test_the_elevated_job_runs_no_repository_code(workflow):
    for name in _elevated(workflow):
        for step in workflow["jobs"][name]["steps"]:
            script = step.get("run")
            if script is None:
                continue
            found = REPOSITORY_CODE.search(script)
            assert found is None, f"{name}: step calls {found.group(1)!r}"


def test_the_elevated_job_uses_only_pinned_actions(workflow):
    for name in _elevated(workflow):
        for step in workflow["jobs"][name]["steps"]:
            uses = step.get("uses")
            if uses is None:
                continue
            assert PINNED.match(uses), f"{name}: {uses} is not pinned to a commit"


def test_the_publish_waits_for_the_measurement(workflow):
    assert workflow["jobs"]["publish"]["needs"] == "measure"


def test_no_permission_is_declared_on_the_workflow_itself(workflow):
    # A workflow-level block would grant workflows: write to `measure` as well, which
    # is the escalation this split exists to avoid.
    assert "permissions" not in workflow
