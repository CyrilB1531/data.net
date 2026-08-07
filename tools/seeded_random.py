#!/usr/bin/env python3
"""The single place this repository draws pseudorandom numbers.

Every corpus and oracle here is built from a fixed seed so that regenerating
produces identical bytes: CI's `Oracles are reproducible` job diffs them, and
both language sides of the benchmarks read the same corpus files. The numbers
are test data and protect nothing, so `random` is the correct module here and
`secrets` would be the wrong one.

Sonar's S2245 cannot know that, and fires once per call site — some thirty
lines across four generators. Routing the draws through this wrapper puts the
waiver in one file instead of scattering pragmas over the generators, where
they drift away from the code they were meant to cover. Each method delegates
to `random.Random` unchanged, which is what keeps every existing seed's
sequence, and therefore every committed oracle, intact.
"""

from __future__ import annotations

import random
from collections.abc import Sequence


class SeededRandom:
    """A seeded `random.Random`, one delegating method at a time."""

    def __init__(self, seed: int) -> None:
        self._rng = random.Random(seed)  # NOSONAR S2245

    def random(self) -> float:
        return self._rng.random()  # NOSONAR S2245

    def randint(self, a: int, b: int) -> int:
        return self._rng.randint(a, b)  # NOSONAR S2245

    def randrange(self, stop: int) -> int:
        return self._rng.randrange(stop)  # NOSONAR S2245

    def choice[T](self, seq: Sequence[T]) -> T:
        return self._rng.choice(seq)  # NOSONAR S2245

    def uniform(self, a: float, b: float) -> float:
        return self._rng.uniform(a, b)  # NOSONAR S2245

    def gauss(self, mu: float, sigma: float) -> float:
        return self._rng.gauss(mu, sigma)  # NOSONAR S2245
