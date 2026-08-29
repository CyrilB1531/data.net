#!/usr/bin/env python3
"""np.load in a process that has called np.save, against one that has not.

The C# counterpart is HeapWarmthBench, and this mirrors its shape rather than its
code: three subcommands, because the variable under test is the process. `prepare`
writes the .npy once, `cold` loads those bytes having built and saved nothing, and
`warm` does its saves first and then loads.

The question is #433's second half. If numpy has no such asymmetry and C# does, the
published embedding_index_load ratio flatters us, and #324's "furthest behind"
framing is understated rather than overstated. Either ordering is a result here.
"""

from __future__ import annotations

import io
import resource
import statistics
import sys
import tempfile
import time
from pathlib import Path

sys.path.append(str(Path(__file__).resolve().parent))

import numpy as np  # noqa: E402

from bench_persistence import build_vectors  # noqa: E402

# Timed runs. Odd, so the median is a run rather than a mean of two.
REPEATS = 9

# Untimed runs first, matching the C# side's JIT warm-up so both discard the same count.
WARMUP = 2

# Saves the warm state makes before it loads anything, as the harness does.
WARMING_SAVES = 12

DEFAULT_PATH = Path(tempfile.gettempdir()) / "lodestar-heap-warmth.npy"


def prepare(path: Path) -> None:
    np.save(path, build_vectors())
    print(f"prepared        {path} ({path.stat().st_size:,} bytes)")


def measure(warm: bool, path: Path) -> None:
    # Read, never save: this is the only allocation the cold process makes before the
    # loop, and the warm one makes it too, so it cancels.
    payload = path.read_bytes()

    # Before the loop, not inside it. A save between two loads competes with the load
    # rather than warming for it -- which is what made the C# side's first cut report
    # cold as faster than warm, stably, over three rounds.
    if warm:
        vectors = build_vectors()
        for _ in range(WARMING_SAVES):
            np.save(tempfile.TemporaryFile(), vectors)

    # Wrapped once, rewound per run. Building the stream inside the loop would copy the
    # payload into the timed region, which the C# side does not do -- it loads from a
    # byte[] it read before the loop.
    stream = io.BytesIO(payload)

    samples = []
    array_bytes = 0
    for run in range(WARMUP + REPEATS):
        stream.seek(0)
        start = time.perf_counter()
        loaded = np.load(stream)
        elapsed = (time.perf_counter() - start) * 1000.0
        array_bytes = loaded.nbytes
        del loaded
        if run >= WARMUP:
            samples.append(elapsed)

    samples.sort()
    print(f"state           {'warm' if warm else 'cold'}")
    print(
        f"load ms         median {statistics.median(samples):.3f}"
        f"  min {samples[0]:.3f}  max {samples[-1]:.3f}"
    )
    # Both states must agree on this or they are two workloads, not one workload on two
    # heaps, and no timing comparison between them is valid.
    print(f"array bytes     {array_bytes:,}")
    print(f"peak rss        {resource.getrusage(resource.RUSAGE_SELF).ru_maxrss:,} KiB")


def main() -> None:
    state = sys.argv[1] if len(sys.argv) > 1 else "cold"
    path = Path(sys.argv[2]) if len(sys.argv) > 2 else DEFAULT_PATH

    if state == "prepare":
        prepare(path)
        return

    measure(state == "warm", path)


if __name__ == "__main__":
    main()
