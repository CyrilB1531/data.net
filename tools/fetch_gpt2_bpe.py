#!/usr/bin/env python3
"""Vendor the GPT-2 byte-level BPE vocabulary into tests/oracles/.

`ByteLevelBpeTests` claims byte-exact parity with HuggingFace `tokenizers` over
GPT-2's real 50 257-entry vocabulary. A self-trained toy model cannot support
that claim: it would never exercise a merge table with 50 000 ranks, and it
would not prove that DataNet reads the `merges.txt` layout a real model ships.

Only the vocabulary and the merge table are redistributed here — never the
weights, per docs/decisions/0003-provenance-and-licensing.md. `gpt2` is
MIT-licensed (https://huggingface.co/openai-community/gpt2); the attribution is
recorded in THIRD-PARTY-NOTICES.md.

    python tools/fetch_gpt2_bpe.py           # vendor
    python tools/fetch_gpt2_bpe.py --check   # verify the checked-in fixtures

Each download is checked against the SHA-256 pinned below before anything is
written. A mismatch means the upstream file changed: read the diff, update the
pin, regenerate the oracles in the same commit, and expect ids to move.
"""

from __future__ import annotations

import hashlib
import sys
import urllib.request
from pathlib import Path

ORACLE_DIR = Path(__file__).resolve().parent.parent / "tests" / "oracles"
BASE = "https://huggingface.co/openai-community/gpt2/resolve/main/"

# name in tests/oracles -> (upstream file, pinned sha256 of the upstream bytes)
FILES = {
    "gpt2_vocab.json": ("vocab.json", "196139668be63f3b5d6574427317ae82f612a97c5d1cdaf36ed2256dbf636783"),
    "gpt2_merges.txt": ("merges.txt", "1ce1664773c50f3e0cc8842619a93edc4624525b728b188a9e0be33b7726adc5"),
}


def download(name: str) -> bytes:
    with urllib.request.urlopen(BASE + name) as response:  # noqa: S310
        return response.read()


def main() -> int:
    check = "--check" in sys.argv[1:]
    failures = []
    for local, (remote, pinned) in FILES.items():
        payload = download(remote)
        digest = hashlib.sha256(payload).hexdigest()
        if digest != pinned:
            failures.append(
                f"{BASE}{remote}\n  expected sha256 {pinned}\n  got      sha256 {digest}")
            continue
        path = ORACLE_DIR / local
        if check:
            if not path.exists() or path.read_bytes() != payload:
                failures.append(f"{path} differs from the verified upstream file.")
        else:
            path.write_bytes(payload)
            print(f"{local}: {len(payload)} bytes -> {path}")
    for failure in failures:
        print(failure, file=sys.stderr)
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
