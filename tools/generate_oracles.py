#!/usr/bin/env python3
"""Generate frozen oracle corpora for the Lodestar.Text test suite.

Per section 4 of the project brief, correctness is proven by replaying reference
values captured from the canonical Python libraries — never by trusting that the
C# "compiles and passes". This script produces those references as versioned
JSON under ``tests/oracles/``. Python is thus a *development* dependency only;
the committed JSON is what the C# suite consumes at test time.

Design rules:
  * Deterministic. A fixed seed and no wall-clock timestamps, so regenerating on
    another machine yields a byte-identical file (clean diffs, real review).
  * Code-point semantics. rapidfuzz operates on Python ``str`` (code points), so
    the C# side must compare with ``TextElement.CodePoint`` to match. Lone
    surrogates are never emitted (they cannot round-trip through JSON).
  * Broad coverage. Empty / identical / ASCII typos / accents / BMP mix / CJK /
    supplementary-plane emoji / long strings.

Usage:
    python -m venv .venv-oracles && . .venv-oracles/bin/activate
    pip install rapidfuzz jellyfish
    python tools/generate_oracles.py
"""

from __future__ import annotations

import base64
import contextlib
import json
import math
import os
import sys
import tempfile
import warnings
from importlib.metadata import version
from pathlib import Path

# PYTHONSAFEPATH=1 (CONTRIBUTING.md) keeps this script's own directory off
# sys.path, so seeded_random is appended -- never prepended, so nothing here shadows an installed package.
sys.path.append(str(Path(__file__).resolve().parent))

from seeded_random import SeededRandom  # noqa: E402

from difflib import SequenceMatcher

import jellyfish
import numpy as np
import textdistance as td
from rapidfuzz.distance import DamerauLevenshtein, Indel, Levenshtein, OSA
from sklearn import metrics as skm
from sklearn.feature_extraction.text import CountVectorizer as SkCountVectorizer
from sklearn.feature_extraction.text import TfidfVectorizer as SkTfidfVectorizer

SEED = 20260801
ORACLE_DIR = Path(__file__).resolve().parent.parent / "tests" / "oracles"

# Fixture strings reused across several corpora.
QUICK_FOX = "the quick brown fox"
METS = "new york mets"
UNK_TOKEN = "[UNK]"
# WordPiece's spelling; the sentencepiece-based families (Unigram, XLM-R) spell
# the same concept in angle brackets below.
UNK_TOKEN_LOWER = "<unk>"
CAT_SENTENCE = "the cat sat on the mat"
HELLO_WORLD = "hello world"
END_OF_TEXT = "<|endoftext|>"
TINY_SP_MODEL = "tiny_sp.model"
EMBEDDING_SENTENCE = "tokenization is embedding embeddings"
XLMR_FAIRSEQ_MODEL = "xlmr_fairseq.model"
# XLM-R's mask marker, and the added token issue #104 was opened for: roberta-base
# declares lstrip on this one.
MASK_TOKEN = "<mask>"

# Named rather than repeated per corpus: three metadata blocks carry it, which is
# python:S1192's threshold -- confirmed CRITICAL by the analyzer at that count.
BYTE_LEVEL_NO_MERGES = (
    "hand-built: the byte-level alphabet with no merges, defined in tools/generate_oracles.py")

# Ordered: whitespace first (" a" vs "a " must stay distinct), then multi-byte
# scripts, then the special-token strings written out literally.
BPE_TEXTS = [
    "",
    " ",
    "   ",
    "Hello, world!",
    " leading space",
    "trailing space ",
    "double  space",
    "a\tb\nc\r\nd",
    "Il était une fois, à Paris — déjà vu.",
    "naïve café résumé",
    "東京都から来ました",
    "中文分词测试",
    "emoji 👋🏽 family 👨‍👩‍👧‍👦 flag 🇫🇷",
    "<|endoftext|> is written here literally",
    "[UNK] [CLS] [SEP] as text",
    "123 4567 89.01 -42",
    "https://example.com/path?q=1&r=2",
    "snake_case camelCase kebab-case SCREAMING_CASE",
    "the quick brown fox jumps over the lazy dog",
    "tokenization is embedding embeddings",
]

# Code-point ranges per category. Surrogates (0xD800..0xDFFF) are filtered out.
RANGES = {
    "ascii": [(0x20, 0x7E)],
    "latin": [(0x20, 0x7E), (0xC0, 0x17F)],
    "bmp": [(0x20, 0x7E), (0xC0, 0x2AF), (0x370, 0x52F), (0x4E00, 0x9FFF)],
    "supplementary": [(0x1F300, 0x1FAFF), (0x10000, 0x1052F)],
}


# Not the full float64 repr; see stable()'s docstring for why twelve.
STABLE_DIGITS = 12


def stable(value) -> float:
    """A float the corpus can commit: rounded away from the host's last bits.

    numpy and scikit-learn sum in whatever order the SIMD kernel scipy-openblas
    selects for the host CPU, so the last bits of anything a BLAS kernel reduced
    describe the machine that ran the generator rather than the metric --
    committing them turns the drift gate into a hardware check (issue #97).

    Significant digits, not decimal places, because the spread is always at the
    last bit and scales with the value: measured ~1e-13 on accuracy_count (~413)
    and ~1e-16 on the knn scores (~0.4), the same sixteenth digit in both. Twelve
    leaves four orders of margin above it, costing at most 5e-13 against the
    tolerances the tests compare with -- 1e-9 for the metrics corpus, 1e-4f for
    the knn one.
    """
    return float(f"{float(value):.{STABLE_DIGITS}g}")


def rand_string(rng: SeededRandom, length: int, ranges) -> str:
    out = []
    for _ in range(length):
        lo, hi = rng.choice(ranges)
        cp = rng.randint(lo, hi)
        while 0xD800 <= cp <= 0xDFFF:
            cp = rng.randint(lo, hi)
        out.append(chr(cp))
    return "".join(out)


def mutate(rng: SeededRandom, s: str, edits: int, ranges) -> str:
    """Apply `edits` random insert/delete/substitute operations to `s`."""
    chars = list(s)
    for _ in range(edits):
        op = rng.choice(("ins", "del", "sub")) if chars else "ins"
        lo, hi = rng.choice(ranges)
        cp = rng.randint(lo, hi)
        while 0xD800 <= cp <= 0xDFFF:
            cp = rng.randint(lo, hi)
        if op == "ins":
            chars.insert(rng.randint(0, len(chars)), chr(cp))
        elif op == "del":
            del chars[rng.randint(0, len(chars) - 1)]
        else:
            chars[rng.randint(0, len(chars) - 1)] = chr(cp)
    return "".join(chars)


def build_pairs(rng: SeededRandom):
    """Yield (category, a, b) tuples covering the corpus design.

    long_ascii/long_latin/long_supplementary are appended last so every existing
    case keeps its id and value -- the RNG is consumed in order.

    The first two exist because "long" draws from BMP ranges, so its patterns
    contain CJK and never reach the Latin-1 bit-parallel path: the blocked Myers
    code had no coverage at all until long ASCII/Latin pairs were added.

    long_supplementary is the same hole one plane up (#208). "supplementary"
    draws 2-10 characters, and the fast path opens at 16, so measured over the
    1425 cases before it: 283 reached the Myers gate, 194 of those held a
    character above U+00FF, and *none* held a supplementary one. Surrogate
    decoding is what the code-point mode is for, and it was the one part of that
    path with no case long enough to exercise it.
    """
    # Deterministic edge cases first.
    edge = [
        ("", ""),
        ("a", ""),
        ("", "a"),
        ("abc", "abc"),
        ("kitten", "sitting"),
        ("flaw", "lawn"),
        ("Levenshtein", "Levenstein"),
        ("café", "cafe"),
        ("Straße", "Strasse"),
        ("naïve", "naive"),
        ("😀", "😀"),
        ("😀", "😁"),
        ("a😀b", "ab"),
        ("👨‍👩‍👧", "👨‍👩‍👦"),  # ZWJ sequences: differ by code points
        ("中文测试", "中文考试"),
        # Transposition-focused: exercise OSA vs unrestricted Damerau-Levenshtein.
        ("ab", "ba"),
        ("abcd", "acbd"),
        ("CA", "ABC"),   # OSA gives 3, unrestricted Damerau gives 2
        ("ca", "abc"),
        ("a cat", "an act"),
        ("converse", "conserve"),
    ]
    for a, b in edge:
        yield "edge", a, b

    # Randomized families.
    plans = [
        ("ascii", RANGES["ascii"], 200, (3, 12), (0, 5)),
        ("latin", RANGES["latin"], 200, (3, 14), (0, 6)),
        ("bmp", RANGES["bmp"], 200, (3, 16), (0, 7)),
        ("supplementary", RANGES["supplementary"], 150, (2, 10), (0, 6)),
        ("long", RANGES["bmp"], 60, (120, 400), (5, 40)),
        # See this function's docstring for why these three are appended last.
        ("long_ascii", RANGES["ascii"], 60, (80, 400), (5, 40)),
        ("long_latin", RANGES["latin"], 60, (80, 400), (5, 40)),
        # 20, not the 80 its neighbours use: at 80 every case cleared the 64-code-point
        # word and the single-word kernel got none. Measured on the first attempt.
        ("long_supplementary", RANGES["supplementary"], 60, (20, 400), (5, 40)),
    ]
    for name, ranges, count, (lo_len, hi_len), (lo_edit, hi_edit) in plans:
        for _ in range(count):
            length = rng.randint(lo_len, hi_len)
            a = rand_string(rng, length, ranges)
            b = mutate(rng, a, rng.randint(lo_edit, hi_edit), ranges)
            # Half the time, compare against a fully independent string too.
            yield name, a, b
            if rng.random() < 0.5:
                yield name, a, rand_string(rng, rng.randint(lo_len, hi_len), ranges)


def generate_levenshtein() -> dict:
    rng = SeededRandom(SEED)
    cases = []
    for idx, (category, a, b) in enumerate(build_pairs(rng)):
        cases.append(
            {
                "id": idx,
                "category": category,
                "a": a,
                "b": b,
                "distance": Levenshtein.distance(a, b),
                "normalized_distance": Levenshtein.normalized_distance(a, b),
                "normalized_similarity": Levenshtein.normalized_similarity(a, b),
            }
        )
    return {
        "metadata": {
            "algorithm": "Levenshtein",
            "library": "rapidfuzz",
            "library_version": version("rapidfuzz"),
            "reference_calls": [
                "rapidfuzz.distance.Levenshtein.distance",
                "rapidfuzz.distance.Levenshtein.normalized_distance",
                "rapidfuzz.distance.Levenshtein.normalized_similarity",
            ],
            "weights": [1, 1, 1],
            "semantics": "code_point",
            "seed": SEED,
            "count": len(cases),
        },
        "cases": cases,
    }


def _edit_distance_corpus(module, algorithm: str, library: str, calls: list[str]) -> dict:
    """Build an oracle for any rapidfuzz edit-distance module (Levenshtein/OSA/DL)."""
    rng = SeededRandom(SEED)
    cases = []
    for idx, (category, a, b) in enumerate(build_pairs(rng)):
        cases.append(
            {
                "id": idx,
                "category": category,
                "a": a,
                "b": b,
                "distance": module.distance(a, b),
                "normalized_distance": module.normalized_distance(a, b),
                "normalized_similarity": module.normalized_similarity(a, b),
            }
        )
    return {
        "metadata": {
            "algorithm": algorithm,
            "library": library,
            "library_version": version(library),
            "reference_calls": calls,
            "semantics": "code_point",
            "seed": SEED,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_osa() -> dict:
    return _edit_distance_corpus(
        OSA, "OSA", "rapidfuzz",
        ["rapidfuzz.distance.OSA.distance",
         "rapidfuzz.distance.OSA.normalized_distance",
         "rapidfuzz.distance.OSA.normalized_similarity"],
    )


def generate_damerau() -> dict:
    return _edit_distance_corpus(
        DamerauLevenshtein, "DamerauLevenshtein", "rapidfuzz",
        ["rapidfuzz.distance.DamerauLevenshtein.distance",
         "rapidfuzz.distance.DamerauLevenshtein.normalized_distance",
         "rapidfuzz.distance.DamerauLevenshtein.normalized_similarity"],
    )


def _hamming_reference(a: str, b: str) -> int:
    """Standard Hamming distance over code points: positional mismatches over the
    common prefix, plus the length difference.

    Note: jellyfish.hamming_distance matches this for all normal inputs but
    diverges on ~5% of degenerate combining-mark strings (an unexplained quirk of
    its Rust core — not NFC normalization, not byte-level). Lodestar implements the
    standard definition; see docs/decisions/0005-hamming-jellyfish-divergence.md.
    """
    m = min(len(a), len(b))
    return sum(1 for i in range(m) if a[i] != b[i]) + abs(len(a) - len(b))


def generate_hamming() -> dict:
    rng = SeededRandom(SEED)
    diverge = 0
    cases = []
    for idx, (category, a, b) in enumerate(build_pairs(rng)):
        ref = _hamming_reference(a, b)
        if jellyfish.hamming_distance(a, b) != ref:
            diverge += 1
        cases.append({"id": idx, "category": category, "a": a, "b": b, "distance": ref})
    return {
        "metadata": {
            "algorithm": "Hamming",
            "library": "reference-standard",
            "reference_calls": ["standard code-point Hamming (see decision 0005)"],
            "jellyfish_version": version("jellyfish"),
            "jellyfish_divergences": diverge,
            "semantics": "code_point",
            "seed": SEED,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_indel() -> dict:
    return _edit_distance_corpus(
        Indel, "Indel", "rapidfuzz",
        ["rapidfuzz.distance.Indel.distance",
         "rapidfuzz.distance.Indel.normalized_distance",
         "rapidfuzz.distance.Indel.normalized_similarity"],
    )


def _jaro_reference(a: str, b: str) -> float:  # NOSONAR S3776
    """Standard Jaro similarity over code points (matches Lodestar's Jaro core).

    Cognitive complexity is deliberately left above the threshold. This is a
    transcription of the published Jaro algorithm — match window, then
    transposition count — and its C# counterpart, Jaro.SimilarityCore, carries the
    same suppression for the same reason: decomposing it would break the
    one-to-one mapping with the reference that makes any divergence auditable.

    The argument is stronger here than in the C#. This function GENERATES the
    reference data every other component is validated against, so "the tests still
    pass" would be circular: the tests compare against exactly this output.

    jellyfish agrees for normal inputs but diverges on the same degenerate
    combining-mark / emoji strings as its Hamming (decision 0005). We therefore
    generate from this reference and record the jellyfish divergence count.
    """
    l1, l2 = len(a), len(b)
    if l1 == 0 or l2 == 0:
        return 0.0
    window = max(0, max(l1, l2) // 2 - 1)
    m1 = [False] * l1
    m2 = [False] * l2
    matches = 0
    for i in range(l1):
        lo = max(0, i - window)
        hi = min(i + window + 1, l2)
        for j in range(lo, hi):
            if not m2[j] and a[i] == b[j]:
                m1[i] = m2[j] = True
                matches += 1
                break
    if matches == 0:
        return 0.0
    t = 0
    k = 0
    for i in range(l1):
        if m1[i]:
            while not m2[k]:
                k += 1
            if a[i] != b[k]:
                t += 1
            k += 1
    t //= 2
    m = matches
    return (m / l1 + m / l2 + (m - t) / m) / 3.0


def _jaro_winkler_reference(a: str, b: str, p: float = 0.1) -> float:
    jaro = _jaro_reference(a, b)
    if jaro <= 0.7:
        return jaro
    limit = min(min(len(a), len(b)), 4)
    prefix = 0
    while prefix < limit and a[prefix] == b[prefix]:
        prefix += 1
    return jaro + prefix * p * (1.0 - jaro)


def _similarity_reference_corpus(reference, jelly, algorithm: str) -> dict:
    rng = SeededRandom(SEED)
    diverge = 0
    cases = []
    for idx, (category, a, b) in enumerate(build_pairs(rng)):
        ref = reference(a, b)
        if abs(jelly(a, b) - ref) > 1e-9:
            diverge += 1
        cases.append({"id": idx, "category": category, "a": a, "b": b, "similarity": ref})
    return {
        "metadata": {
            "algorithm": algorithm,
            "library": "reference-standard",
            "reference_calls": [f"standard {algorithm} over code points (see decision 0005)"],
            "jellyfish_version": version("jellyfish"),
            "jellyfish_divergences": diverge,
            "semantics": "code_point",
            "seed": SEED,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_jaro() -> dict:
    return _similarity_reference_corpus(_jaro_reference, jellyfish.jaro_similarity, "Jaro")


def generate_jaro_winkler() -> dict:
    return _similarity_reference_corpus(
        _jaro_winkler_reference, jellyfish.jaro_winkler_similarity, "JaroWinkler")


def generate_lcs() -> dict:
    rng = SeededRandom(SEED)
    cases = []
    for idx, (category, a, b) in enumerate(build_pairs(rng)):
        subsequence = (len(a) + len(b) - Indel.distance(a, b)) // 2
        substring = SequenceMatcher(None, a, b, autojunk=False).find_longest_match(0, len(a), 0, len(b)).size
        cases.append({
            "id": idx, "category": category, "a": a, "b": b,
            "subsequence": subsequence, "substring": substring,
        })
    return {
        "metadata": {
            "algorithm": "Lcs",
            "library": "rapidfuzz+difflib",
            "reference_calls": [
                "subsequence: (len(a)+len(b)-rapidfuzz.distance.Indel.distance)//2",
                "substring: difflib.SequenceMatcher(autojunk=False).find_longest_match(...).size",
            ],
            "semantics": "code_point",
            "seed": SEED,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_ratcliff() -> dict:
    rng = SeededRandom(SEED)
    cases = []
    for idx, (category, a, b) in enumerate(build_pairs(rng)):
        similarity = SequenceMatcher(None, a, b, autojunk=False).ratio()
        cases.append({"id": idx, "category": category, "a": a, "b": b, "similarity": similarity})
    return {
        "metadata": {
            "algorithm": "RatcliffObershelp",
            "library": "difflib",
            "reference_calls": ["difflib.SequenceMatcher(None, a, b, autojunk=False).ratio()"],
            "autojunk": False,
            "semantics": "code_point",
            "seed": SEED,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_set_similarity() -> dict:
    """qval=1 (textdistance default), multiset (bag) semantics, over non-empty pairs.

    textdistance raises on empty operands, which is its own edge quirk; Lodestar
    defines those separately and covers them via unit tests.
    """
    rng = SeededRandom(SEED)
    cases = []
    for idx, (category, a, b) in enumerate(build_pairs(rng)):
        if a == "" or b == "":
            continue
        cases.append({
            "id": idx, "category": category, "a": a, "b": b,
            "jaccard": td.Jaccard(qval=1).normalized_similarity(a, b),
            "dice": td.Sorensen(qval=1).normalized_similarity(a, b),
            "overlap": td.Overlap(qval=1).normalized_similarity(a, b),
            "tversky": td.Tversky(qval=1).normalized_similarity(a, b),
            "cosine": td.Cosine(qval=1).normalized_similarity(a, b),
        })
    return {
        "metadata": {
            "algorithm": "SetSimilarity",
            "library": "textdistance",
            "library_version": version("textdistance"),
            "reference_calls": [
                "textdistance.{Jaccard,Sorensen,Overlap,Tversky,Cosine}(qval=1).normalized_similarity",
            ],
            "qval": 1,
            "semantics": "code_point",
            "seed": SEED,
            "count": len(cases),
        },
        "cases": cases,
    }


CURATED_WORDS = [
    "Robert", "Rupert", "Rubin", "Ashcraft", "Ashcroft", "Tymczak", "Pfister",
    "Honeyman", "Washington", "Lee", "Gutierrez", "Jackson", "VanDeusen", "Deusen",
    "Knuth", "Euler", "Gauss", "Kant", "Lloyd", "Bob", "a", "MacDonald", "Christina",
    "Catherine", "Smith", "Smyth", "Schmidt", "Jefferson", "Adams", "Wojcik",
    "Nguyen", "Johnson", "Williams", "Brown", "Garcia", "Martinez", "Anderson",
    "Thompson", "Phillip", "Xavier", "Yvonne", "Zachary", "Quinn", "Wright",
    "Knight", "Gnome", "Psalm", "Thomas", "Theodore", "Czar", "Pizza", "Aegean",
]


def phonetic_words(rng: SeededRandom):
    for w in CURATED_WORDS:
        yield w
    # Random pronounceable-ish alphabetic words, deterministic.
    letters = "abcdefghijklmnopqrstuvwxyz"
    for _ in range(350):
        length = rng.randint(2, 11)
        w = "".join(rng.choice(letters) for _ in range(length))
        # Occasionally capitalize to exercise case handling.
        yield w.capitalize() if rng.random() < 0.5 else w


METAPHONE_WORDS = [
    "Thomas", "Theodore", "Catherine", "Christina", "Christopher", "Character",
    "Chemistry", "School", "Schmidt", "Knight", "Knife", "Knuth", "Gnome", "Sign",
    "Design", "Gnat", "Wright", "Write", "Wrong", "Psalm", "Pneumonia", "Phone",
    "Phoenix", "Philip", "Elephant", "Rough", "Though", "Through", "Laugh", "Ghost",
    "Judge", "Bridge", "Edge", "Dodge", "Special", "Social", "Musician", "Nation",
    "Action", "Mission", "Passion", "Ocean", "Ancient", "Efficient", "Thumb",
    "Climb", "Lamb", "Comb", "Dumb", "Xavier", "Xylophone", "Box", "Fox", "Exam",
    "Cent", "City", "Cycle", "Cat", "Cool", "Music", "Quick", "Queen", "Square",
    "Zero", "Zone", "Buzz", "Vision", "Version", "Washington", "Jackson", "Jefferson",
    "Robert", "Rupert", "Rubin", "Ashcraft", "Ashcroft", "Tymczak", "Pfister",
    "Honeyman", "Gutierrez", "MacDonald", "Anderson", "Williams", "Thompson",
    "Nicholas", "Vaughan", "Hugh", "Leigh", "Callaghan", "Gough", "Naughton",
    "Aegean", "Caesar", "Scene", "Science", "Scissors", "Fascinate", "Discipline",
    "Yellow", "Yes", "Young", "Beyond", "Layer", "Player", "Day", "Boy", "Guy",
    "Whale", "White", "Where", "Which", "Whisper", "Hour", "Honest", "Heir", "Herb",
    "Ghana", "Spaghetti", "Bologna", "Lasagna", "Champagne", "Foreign", "Reign",
]


def generate_metaphone() -> dict:
    cases = []
    for idx, word in enumerate(METAPHONE_WORDS):
        cases.append({"id": idx, "word": word, "metaphone": jellyfish.metaphone(word)})
    return {
        "metadata": {
            "algorithm": "Metaphone",
            "library": "jellyfish",
            "library_version": version("jellyfish"),
            "reference_calls": ["jellyfish.metaphone"],
            "corpus": "real English words/names (see decision 0007)",
            "seed": SEED,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_phonetics() -> dict:
    rng = SeededRandom(SEED)
    cases = []
    for idx, word in enumerate(phonetic_words(rng)):
        cases.append({
            "id": idx,
            "word": word,
            "soundex": jellyfish.soundex(word),
            "metaphone": jellyfish.metaphone(word),
            "nysiis": jellyfish.nysiis(word),
        })
    return {
        "metadata": {
            "algorithm": "Phonetics",
            "library": "jellyfish",
            "library_version": version("jellyfish"),
            "reference_calls": ["jellyfish.soundex", "jellyfish.metaphone", "jellyfish.nysiis"],
            "seed": SEED,
            "count": len(cases),
        },
        "cases": cases,
    }


CORPUS_A = [
    CAT_SENTENCE,
    "a cat and a dog",
    "the dog barked loudly",
    "cats and dogs are friends",
    QUICK_FOX,
]
CORPUS_ACCENTS = ["Café crème", "Cafe creme", "Élève à l'école", "eleve a l ecole"]


def _build_count_vectorizer(cfg: dict):
    return SkCountVectorizer(
        analyzer=cfg.get("analyzer", "word"),
        ngram_range=(cfg.get("ngram_min", 1), cfg.get("ngram_max", 1)),
        min_df=cfg.get("min_df", 1),
        max_df=cfg.get("max_df", 1.0),
        binary=cfg.get("binary", False),
        lowercase=cfg.get("lowercase", True),
        strip_accents="unicode" if cfg.get("strip_accents", False) else None,
        stop_words=cfg.get("stop_words", None),
    )


COUNT_CASES = [
    {"config": {}, "docs": CORPUS_A},
    {"config": {"ngram_min": 1, "ngram_max": 2}, "docs": CORPUS_A},
    {"config": {"min_df": 2}, "docs": CORPUS_A},
    {"config": {"max_df": 0.5}, "docs": CORPUS_A},
    {"config": {"binary": True}, "docs": CORPUS_A},
    {"config": {"stop_words": ["the", "a", "and"]}, "docs": CORPUS_A},
    {"config": {"stop_words": "english"}, "docs": CORPUS_A},
    {"config": {"lowercase": False}, "docs": CORPUS_A},
    {"config": {"strip_accents": True}, "docs": CORPUS_ACCENTS},
    {"config": {"analyzer": "char", "ngram_min": 2, "ngram_max": 3}, "docs": CORPUS_A[:3]},
    {"config": {"analyzer": "char_wb", "ngram_min": 2, "ngram_max": 3}, "docs": CORPUS_A[:3]},
]


def generate_countvectorizer() -> dict:
    cases = []
    for idx, case in enumerate(COUNT_CASES):
        cv = _build_count_vectorizer(case["config"])
        x = cv.fit_transform(case["docs"])
        cases.append({
            "id": idx,
            "config": case["config"],
            "docs": case["docs"],
            "feature_names": cv.get_feature_names_out().tolist(),
            "matrix": x.toarray().astype(int).tolist(),
        })
    return {
        "metadata": {
            "algorithm": "CountVectorizer",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": ["sklearn.feature_extraction.text.CountVectorizer"],
            "count": len(cases),
        },
        "cases": cases,
    }


TFIDF_CASES = [
    {"config": {}, "docs": CORPUS_A},
    {"config": {"sublinear_tf": True}, "docs": CORPUS_A},
    {"config": {"smooth_idf": False}, "docs": CORPUS_A},
    {"config": {"norm": None}, "docs": CORPUS_A},
    {"config": {"use_idf": False}, "docs": CORPUS_A},
    {"config": {"ngram_min": 1, "ngram_max": 2}, "docs": CORPUS_A},
    {"config": {"norm": "l1"}, "docs": CORPUS_A},
]


def generate_tfidfvectorizer() -> dict:
    cases = []
    for idx, case in enumerate(TFIDF_CASES):
        cfg = case["config"]
        tv = SkTfidfVectorizer(
            ngram_range=(cfg.get("ngram_min", 1), cfg.get("ngram_max", 1)),
            use_idf=cfg.get("use_idf", True),
            smooth_idf=cfg.get("smooth_idf", True),
            sublinear_tf=cfg.get("sublinear_tf", False),
            norm=cfg.get("norm", "l2") if "norm" in cfg else "l2",
        )
        x = tv.fit_transform(case["docs"])
        cases.append({
            "id": idx,
            "config": cfg,
            "docs": case["docs"],
            "feature_names": tv.get_feature_names_out().tolist(),
            "idf": tv.idf_.tolist() if cfg.get("use_idf", True) else None,
            "matrix": x.toarray().tolist(),
        })
    return {
        "metadata": {
            "algorithm": "TfidfVectorizer",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": ["sklearn.feature_extraction.text.TfidfVectorizer"],
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_hashingvectorizer() -> dict:
    from sklearn.feature_extraction.text import HashingVectorizer as SkHV
    from sklearn.utils.murmurhash import murmurhash3_32

    hashes = {t: int(murmurhash3_32(t.encode("utf-8"), seed=0)) for t in ["cat", "the", "dog", "hello", "a", "mat"]}
    configs = [
        {"n_features": 16, "alternate_sign": True, "norm": None},
        {"n_features": 16, "alternate_sign": True, "norm": "l2"},
        {"n_features": 16, "alternate_sign": False, "norm": None},
        {"n_features": 8, "ngram_min": 1, "ngram_max": 2, "norm": None},
    ]
    cases = []
    for idx, cfg in enumerate(configs):
        hv = SkHV(
            n_features=cfg["n_features"],
            alternate_sign=cfg.get("alternate_sign", True),
            norm=cfg.get("norm", "l2"),
            ngram_range=(cfg.get("ngram_min", 1), cfg.get("ngram_max", 1)),
        )
        x = hv.fit_transform(CORPUS_A)
        cases.append({"id": idx, "config": cfg, "docs": CORPUS_A, "matrix": x.toarray().tolist()})
    return {
        "metadata": {
            "algorithm": "HashingVectorizer",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": ["sklearn.feature_extraction.text.HashingVectorizer", "sklearn.utils.murmurhash.murmurhash3_32"],
            "murmur3": hashes,
            "count": len(cases),
        },
        "cases": cases,
    }


PORTER_WORDS = [
    # step 1a
    "caresses", "ponies", "ties", "caress", "cats",
    # step 1b
    "feed", "agreed", "plastered", "bled", "motoring", "sing", "conflated",
    "troubled", "sized", "hopping", "tanned", "falling", "hissing", "fizzed",
    "failing", "filing",
    # step 1c
    "happy", "sky",
    # step 2
    "relational", "conditional", "rational", "valenci", "hesitanci", "digitizer",
    "conformabli", "radicalli", "differentli", "vileli", "analogousli",
    "vietnamization", "predication", "operator", "feudalism", "decisiveness",
    "hopefulness", "callousness", "formaliti", "sensitiviti", "sensibiliti",
    # step 3
    "triplicate", "formative", "formalize", "electriciti", "electrical",
    "hopeful", "goodness",
    # step 4
    "revival", "allowance", "inference", "airliner", "gyroscopic", "adjustable",
    "defensible", "irritant", "replacement", "adjustment", "dependent",
    "adoption", "homologous", "communism", "activate", "angulariti",
    "homologou", "effective", "bowdlerize",
    # step 5
    "probate", "rate", "cease", "controll", "roll",
    # common words
    "running", "runner", "easily", "fairly", "national", "generalization",
    "organization", "happiness", "argument", "arguing", "meetings",
]


def generate_porter() -> dict:
    from nltk.stem.porter import PorterStemmer  # noqa: PLC0415 (lazy: sandbox import guard)

    stemmer = PorterStemmer(mode=PorterStemmer.ORIGINAL_ALGORITHM)
    cases = [{"id": i, "word": w, "stem": stemmer.stem(w)} for i, w in enumerate(PORTER_WORDS)]
    return {
        "metadata": {
            "algorithm": "PorterStemmer",
            "library": "nltk",
            "library_version": version("nltk"),
            "mode": "ORIGINAL_ALGORITHM",
            "reference_calls": ["nltk.stem.porter.PorterStemmer(mode=ORIGINAL_ALGORITHM)"],
            "count": len(cases),
        },
        "cases": cases,
    }


SNOWBALL_EN_WORDS = PORTER_WORDS + [
    "generous", "generously", "generation", "communism", "communication", "arsenic",
    "national", "nationally", "rationalization", "sensational", "consciously",
    "beautiful", "beautifully", "happily", "quickly", "slowly", "friendly",
    "management", "development", "government", "measurement", "achievement",
    "creation", "relation", "position", "decision", "television", "discussion",
    "activity", "sensitivity", "productivity", "ability", "possibility",
    "organize", "realize", "recognize", "characterize", "modernize",
    "connected", "connecting", "connection", "connects", "connect",
    "studies", "studied", "studying", "study", "cries", "cried", "crying",
    "agreement", "agreed", "agreeing", "agrees", "agree",
    "controlling", "controlled", "controls", "control", "rolling", "rolled",
    "flying", "denying", "trying", "buying", "playing", "enjoying",
    "hopeful", "careful", "wonderful", "powerful", "successful",
    "goodness", "happiness", "kindness", "darkness", "weakness",
    "faithfully", "hopefully", "carefully", "exactly", "absolutely",
    "biology", "psychology", "technology", "apology", "analogy",
    "universities", "abilities", "cities", "parties", "countries",
    "running", "runner", "runs", "swimmer", "swimming", "beginner", "beginning",
    "european", "america", "france", "england", "computer", "internet",
    "walking", "talked", "jumped", "wanted", "needed", "worked", "looked",
]


def generate_snowball_en() -> dict:
    from nltk.stem.snowball import SnowballStemmer  # noqa: PLC0415

    stemmer = SnowballStemmer("english")
    seen = set()
    words = [w for w in SNOWBALL_EN_WORDS if not (w in seen or seen.add(w))]
    cases = [{"id": i, "word": w, "stem": stemmer.stem(w)} for i, w in enumerate(words)]
    return {
        "metadata": {
            "algorithm": "EnglishSnowballStemmer",
            "library": "nltk",
            "library_version": version("nltk"),
            "reference_calls": ["nltk.stem.snowball.SnowballStemmer('english')"],
            "count": len(cases),
        },
        "cases": cases,
    }


SNOWBALL_FR_WORDS = [
    "continuellement", "amoureusement", "national", "nationale", "nationaux", "finalement",
    "rapidement", "organisation", "organiser", "organisé", "développement", "information",
    "maison", "maisons", "cheval", "chevaux", "journal", "journaux", "heureuse", "heureux",
    "finir", "finissait", "finissant", "mangé", "mangée", "mangées", "manger", "mangez",
    "parlions", "parlait", "parlerons", "chanter", "chantez", "chantait", "chanteront",
    "beauté", "activité", "possibilité", "capacité", "réalité", "société", "qualité",
    "important", "importante", "importants", "différent", "différence", "présidence",
    "gentiment", "vraiment", "seulement", "notamment", "évidemment", "constamment",
    "production", "création", "administration", "communication", "génération",
    "technologie", "psychologie", "biologie", "logique", "musique", "physique",
    "grandeur", "chaleur", "couleur", "douleur", "bonheur", "malheur",
    "premier", "première", "dernier", "dernière", "policier", "policière",
    "voiture", "nature", "culture", "structure", "aventure", "peinture",
    "national", "rationnel", "personnel", "naturel", "culturel", "actuel",
    "grandir", "choisir", "réussir", "réfléchir", "établir", "accomplir",
    "aimer", "aimé", "aimait", "aimeront", "aimerais", "donner", "donné", "donnait",
    "petit", "petite", "petits", "petites", "grand", "grande", "grands",
    "rouge", "rouges", "jaune", "jaunes", "libre", "libres", "riche", "riches",
    "connaissance", "puissance", "naissance", "croissance", "assurance",
    "facilement", "difficilement", "heureusement", "malheureusement", "certainement",
    "utiliser", "utilisé", "utilisation", "réalisation", "réaliser", "réalisé",
    "gouvernement", "changement", "mouvement", "sentiment", "moment", "document",
    "belle", "belles", "vieille", "nouvelle", "nouvelles", "ancienne", "ancien",
    "manière", "matière", "lumière", "rivière", "prière", "carrière",
]


def generate_snowball_fr() -> dict:
    from nltk.stem.snowball import SnowballStemmer  # noqa: PLC0415

    stemmer = SnowballStemmer("french")
    seen = set()
    words = [w for w in SNOWBALL_FR_WORDS if not (w in seen or seen.add(w))]
    cases = [{"id": i, "word": w, "stem": stemmer.stem(w)} for i, w in enumerate(words)]
    return {
        "metadata": {
            "algorithm": "FrenchSnowballStemmer",
            "library": "nltk",
            "library_version": version("nltk"),
            "reference_calls": ["nltk.stem.snowball.SnowballStemmer('french')"],
            "count": len(cases),
        },
        "cases": cases,
    }


# --- Additional Snowball languages, see _snowball_corpus's docstring ----------

SNOWBALL_ES_WORDS = [
    # step 0: attached object pronouns
    "damelo", "dámelo", "haciéndola", "haciendolo", "vámonos", "escribirle", "decirles",
    "comprarlo", "cantándome", "construyendolo", "dárselo", "mostrárselas",
    # step 1: nominal / adjectival suffixes
    "esperanza", "esperanzas", "musico", "musica", "musicos", "musicas",
    "realismo", "realismos", "amable", "amables", "posible", "posibles",
    "artista", "artistas", "hermoso", "hermosa", "hermosos", "hermosas",
    "conocimiento", "conocimientos", "sentimiento", "sentimientos",
    "computadora", "computador", "generación", "generaciones", "trabajador", "trabajadores",
    "importante", "importantes", "distancia", "distancias",
    "biología", "biologías", "solución", "soluciones", "revolución", "revoluciones",
    "existencia", "existencias", "paciencia",
    "rapidamente", "rápidamente", "claramente", "efectivamente", "activamente",
    "realmente", "generalmente", "posiblemente", "amablemente",
    "ciudad", "ciudades", "capacidad", "capacidades", "actividad", "actividades",
    "activa", "activo", "activas", "activos", "creativo", "creativa",
    # step 2: verb endings
    "cantar", "canto", "cantas", "cantamos", "cantaron", "cantaban", "cantaría",
    "cantarían", "cantaremos", "cantase", "cantaste", "cantando",
    "comer", "comes", "comemos", "comieron", "comería", "comiendo", "comido",
    "vivir", "vives", "vivimos", "vivieron", "viviría", "viviendo", "vivido",
    "construyendo", "construyeron", "leyendo", "leyeron", "oyendo",
    "distinguen", "distinguir", "sigue", "siguen", "pague", "paguen",
    # short / residual
    "casa", "casas", "libro", "libros", "papel", "papeles", "sol", "mar",
    "país", "países", "café", "bebé", "and", "yo", "el", "la",
]


SNOWBALL_PT_WORDS = [
    "esperança", "esperanças", "musico", "musica", "musicos", "musicas",
    "realismo", "amável", "amáveis", "possível", "possíveis",
    "artista", "artistas", "formoso", "formosa", "formosos", "formosas",
    "conhecimento", "conhecimentos", "sentimento", "sentimentos",
    "computador", "computadores", "geração", "gerações", "trabalhador", "trabalhadores",
    "importante", "importantes", "distância", "distâncias",
    "biologia", "biologias", "solução", "soluções", "revolução", "revoluções",
    "existência", "existências", "paciência",
    "rapidamente", "claramente", "efetivamente", "ativamente",
    "realmente", "geralmente", "possivelmente",
    "cidade", "cidades", "capacidade", "capacidades", "atividade", "atividades",
    "ativa", "ativo", "ativas", "ativos", "criativo", "criativa",
    "cantar", "canto", "cantas", "cantamos", "cantaram", "cantava", "cantaria",
    "cantariam", "cantaremos", "cantasse", "cantaste", "cantando",
    "comer", "comes", "comemos", "comeram", "comeria", "comendo", "comido",
    "partir", "partes", "partimos", "partiram", "partiria", "partindo", "partido",
    "casa", "casas", "livro", "livros", "papel", "papéis", "sol", "mar",
    "país", "países", "café", "bebê", "coração", "corações",
    "nação", "nações", "irmã", "irmãs", "logia", "logias",
]


SNOWBALL_IT_WORDS = [
    "speranza", "speranze", "musico", "musica", "musici", "musiche",
    "realismo", "realismi", "amabile", "amabili", "possibile", "possibili",
    "artista", "artisti", "formoso", "formosa", "formosi", "formose",
    "conoscimento", "sentimento", "sentimenti",
    "computatore", "computatori", "generazione", "generazioni",
    "lavoratore", "lavoratori", "importante", "importanti", "distanza", "distanze",
    "biologia", "biologie", "soluzione", "soluzioni", "rivoluzione", "rivoluzioni",
    "esistenza", "esistenze", "pazienza",
    "rapidamente", "chiaramente", "effettivamente", "attivamente",
    "realmente", "generalmente", "possibilmente",
    "citta", "città", "capacita", "capacità", "attivita", "attività",
    "attiva", "attivo", "attive", "attivi", "creativo", "creativa",
    "cantare", "canto", "canti", "cantiamo", "cantarono", "cantava", "canterebbe",
    "cantando", "cantato", "cantata", "cantate", "cantati",
    "credere", "credi", "crediamo", "credono", "credendo", "creduto",
    "finire", "finisci", "finiamo", "finirono", "finendo", "finito",
    "casa", "case", "libro", "libri", "carta", "carte", "sole", "mare",
    "paese", "paesi", "caffè", "abbandonare", "abbandonato",
]


SNOWBALL_DE_WORDS = [
    # German preprocessing: sharp s, u/y between vowels, umlauts
    "straße", "strasse", "größe", "grosse", "fuß", "füße",
    "kraut", "kräuter", "haus", "häuser", "baum", "bäume",
    # -heit / -keit / -ung / -nis / -isch / -lich / -ig / -end
    "schönheit", "schönheiten", "freiheit", "möglichkeit", "möglichkeiten",
    "wohnung", "wohnungen", "zeitung", "zeitungen", "rechnung",
    "ergebnis", "ergebnisse", "geheimnis", "kenntnis",
    "praktisch", "praktische", "politisch", "politischen",
    "freundlich", "freundliche", "freundlichen", "wirklich", "wirkliche",
    "wichtig", "wichtige", "wichtigen", "richtig", "richtiges",
    "lachend", "singend", "arbeitend",
    # inflectional endings -e -en -es -em -er -ern -est
    "kinder", "kindern", "kindes", "kinde", "kind",
    "guten", "gutes", "gutem", "guter", "gute", "gut",
    "schnellsten", "schnellste", "schnellst", "schnell",
    "männer", "männern", "frauen", "frau", "mann",
    # verbs
    "arbeiten", "arbeitet", "arbeitete", "gearbeitet", "arbeite",
    "spielen", "spielt", "spielte", "gespielt",
    "laufen", "läuft", "lief", "gelaufen",
    "sprechen", "spricht", "sprach", "gesprochen",
    # short / residual
    "der", "die", "das", "und", "ist", "ein", "eine", "einen",
]


def _snowball_corpus(language: str, algorithm: str, words: list[str]) -> dict:
    """Freeze nltk's Snowball output for one language into an oracle payload.

    Each word list targets that language's own suffix families, plus short and
    irregular words that exercise its region (RV/R1/R2) boundaries.
    """
    from nltk.stem.snowball import SnowballStemmer  # noqa: PLC0415

    stemmer = SnowballStemmer(language)
    seen = set()
    unique = [w for w in words if not (w in seen or seen.add(w))]
    cases = [{"id": i, "word": w, "stem": stemmer.stem(w)} for i, w in enumerate(unique)]
    return {
        "metadata": {
            "algorithm": algorithm,
            "library": "nltk",
            "library_version": version("nltk"),
            "reference_calls": [f"nltk.stem.snowball.SnowballStemmer('{language}')"],
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_snowball_es() -> dict:
    return _snowball_corpus("spanish", "SpanishSnowballStemmer", SNOWBALL_ES_WORDS)


def generate_snowball_pt() -> dict:
    return _snowball_corpus("portuguese", "PortugueseSnowballStemmer", SNOWBALL_PT_WORDS)


def generate_snowball_it() -> dict:
    return _snowball_corpus("italian", "ItalianSnowballStemmer", SNOWBALL_IT_WORDS)


def generate_snowball_de() -> dict:
    return _snowball_corpus("german", "GermanSnowballStemmer", SNOWBALL_DE_WORDS)


WORDPIECE_VOCAB = [
    UNK_TOKEN, "the", "cat", "dog", "play", "un", "love", "run", "quick", "brown",
    "fox", "jump", "hello", "world", "token", "embed", "semantic", "search", "is",
    "are", "and", "this", "big", "a", ".", "!", "?",
    "##s", "##ing", "##ed", "##er", "##aff", "##able", "##ly", "##ner", "##ning",
    "##ization", "##ize", "##ding", "##dings", "##ger", "##gest", "##a", "##b", "##c",
]
WORDPIECE_TEXTS = [
    "the cats playing",
    "unaffable",
    "unknownxyz",
    "quick brown fox jumps.",
    EMBEDDING_SENTENCE,
    "hello world!",
    "the dog runs and the cat plays",
    "bigger biggest",
    "lovely love loved lover",
]


def generate_wordpiece() -> dict:
    from tokenizers import Tokenizer  # noqa: PLC0415
    from tokenizers.models import WordPiece
    from tokenizers.pre_tokenizers import Whitespace

    vocab = {tok: i for i, tok in enumerate(WORDPIECE_VOCAB)}
    wp = WordPiece(vocab, unk_token=UNK_TOKEN, max_input_chars_per_word=100)
    tokenizer = Tokenizer(wp)
    tokenizer.pre_tokenizer = Whitespace()

    cases = []
    for i, text in enumerate(WORDPIECE_TEXTS):
        enc = tokenizer.encode(text)
        cases.append({"id": i, "text": text, "tokens": enc.tokens, "ids": enc.ids})
    return {
        "metadata": {
            "algorithm": "WordPiece",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "vocab": vocab,
            "unk_token": UNK_TOKEN,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_pooling() -> dict:
    rng = SeededRandom(SEED)
    cases = []
    for cid, (seq, dim) in enumerate([(4, 6), (5, 8), (3, 4), (6, 5)]):
        emb = [[rng.uniform(-1, 1) for _ in range(dim)] for _ in range(seq)]
        mask = [1 if rng.random() < 0.7 or t == 0 else 0 for t in range(seq)]
        active = sum(mask) or 1
        pooled = [sum(emb[t][d] for t in range(seq) if mask[t]) / active for d in range(dim)]
        norm = sum(v * v for v in pooled) ** 0.5
        normalized = [v / norm for v in pooled] if norm > 0 else pooled
        cases.append({
            "id": cid, "seq": seq, "dim": dim,
            "embeddings": emb, "mask": mask, "pooled_normalized": normalized,
        })
    return {
        "metadata": {
            "algorithm": "MeanPooling",
            "library": "reference",
            "reference_calls": ["mean pool with attention mask + L2 normalize (sentence-transformers recipe)"],
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_knn() -> dict:
    import numpy as np  # noqa: PLC0415

    rng = SeededRandom(SEED)
    n_items, dim, n_queries, k = 60, 16, 6, 5
    corpus = [[rng.uniform(-1, 1) for _ in range(dim)] for _ in range(n_items)]
    queries = [[rng.uniform(-1, 1) for _ in range(dim)] for _ in range(n_queries)]

    c = np.array(corpus, dtype=np.float64)
    c = c / np.linalg.norm(c, axis=1, keepdims=True)
    cases = []
    for i, raw in enumerate(queries):
        q = np.array(raw, dtype=np.float64)
        q = q / np.linalg.norm(q)
        sims = c @ q
        order = np.argsort(-sims, kind="stable")[:k]
        results = [{"index": int(j), "score": stable(sims[j])} for j in order]
        cases.append({"id": i, "query": raw, "k": k, "results": results})

    return {
        "metadata": {
            "algorithm": "CosineKnn",
            "library": "numpy",
            "library_version": version("numpy"),
            "reference_calls": ["brute-force cosine similarity + argsort"],
            "dim": dim,
            "count": len(cases),
        },
        "corpus": corpus,
        "cases": cases,
    }


FUZZ_PAIRS = [
    ("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear"),
    (METS, METS),
    (METS, "the wonderful new york mets"),
    ("mariners vs angels", "los angeles angels of anaheim at seattle mariners"),
    (HELLO_WORLD, "world hello"),
    ("a", "ab"), ("", ""), ("abc", "abcd"),
    ("Hello", "hello"), ("New York!", "york new"),
    (QUICK_FOX, "the brown quick fox"),
    ("apple", "apple pie"), ("apple pie", "apple"),
    ("data science", "science of data"), ("machine learning", "learning machine models"),
    ("kitten", "sitting"), ("levenshtein", "levenstein"),
    ("this is a test", "this is a test!"),
    ("one two three four", "four three two one"),
    ("café", "cafe"), ("naïve", "naive"),
    ("abcdefgh", "abcdefgh"), ("abcdefgh", "hgfedcba"),
    ("python programming", "programming in python"),
    ("the cat", "cat"), ("supercalifragilistic", "super"),
    ("john smith", "smith, john"), ("jonathan", "john"),
    ("123 main st", "123 main street"), ("dr smith", "doctor smith"),
]


def generate_fuzz() -> dict:
    from rapidfuzz import fuzz  # noqa: PLC0415

    cases = []
    for i, (a, b) in enumerate(FUZZ_PAIRS):
        cases.append({
            "id": i, "a": a, "b": b,
            "ratio": fuzz.ratio(a, b),
            "partial_ratio": fuzz.partial_ratio(a, b),
            "token_sort_ratio": fuzz.token_sort_ratio(a, b),
            "token_set_ratio": fuzz.token_set_ratio(a, b),
            "wratio": fuzz.WRatio(a, b),
        })
    return {
        "metadata": {
            "algorithm": "Fuzz",
            "library": "rapidfuzz",
            "library_version": version("rapidfuzz"),
            "reference_calls": ["rapidfuzz.fuzz.{ratio,partial_ratio,token_sort_ratio,token_set_ratio,WRatio}"],
            "count": len(cases),
        },
        "cases": cases,
    }


PROCESS_CHOICES = [
    METS, "new york yankees", "boston red sox", "atlanta braves",
    "new york knicks", "brooklyn nets", "los angeles lakers", "chicago bulls",
]
PROCESS_CASES = [
    {"query": "new york", "limit": 5, "cutoff": 0.0},
    {"query": "new york", "limit": 3, "cutoff": 0.0},
    {"query": METS, "limit": 5, "cutoff": 80.0},
    {"query": "brooklyn", "limit": 2, "cutoff": 0.0},
    {"query": "lakers", "limit": 5, "cutoff": 50.0},
]


def generate_process() -> dict:
    from rapidfuzz import process  # noqa: PLC0415

    cases = []
    for i, case in enumerate(PROCESS_CASES):
        res = process.extract(case["query"], PROCESS_CHOICES, limit=case["limit"], score_cutoff=case["cutoff"])
        cases.append({
            "id": i, "query": case["query"], "limit": case["limit"], "cutoff": case["cutoff"],
            "results": [{"choice": c, "score": s, "index": idx} for (c, s, idx) in res],
        })
    return {
        "metadata": {
            "algorithm": "Process",
            "library": "rapidfuzz",
            "library_version": version("rapidfuzz"),
            "reference_calls": ["rapidfuzz.process.extract (default scorer WRatio)"],
            "choices": PROCESS_CHOICES,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_sentencepiece() -> dict:
    import sentencepiece as spm  # noqa: PLC0415

    sp = spm.SentencePieceProcessor(model_file=str(ORACLE_DIR / TINY_SP_MODEL))
    vocab = [{"piece": sp.id_to_piece(i), "score": sp.get_score(i), "id": i} for i in range(sp.get_piece_size())]
    texts = [
        QUICK_FOX, "tokenization", HELLO_WORLD,
        "machine learning and data science", CAT_SENTENCE,
        "natural language processing", "xyzabc", "a b c",
        "unigram models find the best segmentation", "programming",
    ]
    cases = [
        {"id": k, "text": t, "pieces": sp.encode(t, out_type=str), "ids": sp.encode(t, out_type=int)}
        for k, t in enumerate(texts)
    ]
    return {
        "metadata": {
            "algorithm": "SentencePiece",
            "library": "sentencepiece",
            "library_version": version("sentencepiece"),
            "model": "tiny_sp.model (self-trained unigram, identity normalizer)",
            "unk_id": sp.unk_id(),
            "vocab": vocab,
            "count": len(cases),
        },
        "cases": cases,
    }


LOADER_TEXTS = [
    QUICK_FOX, "tokenization", HELLO_WORLD,
    "machine learning and data science", CAT_SENTENCE,
    "natural language processing", "xyzabc", "a b c",
    "unigram models find the best segmentation", "programming",
]


def _wordpiece_tokenizer(vocab: dict[str, int], lowercase: bool):
    """A HuggingFace WordPiece tokenizer with the pipeline Lodestar reproduces."""
    from tokenizers import Tokenizer  # noqa: PLC0415
    from tokenizers.models import WordPiece  # noqa: PLC0415
    from tokenizers.normalizers import Lowercase  # noqa: PLC0415
    from tokenizers.pre_tokenizers import Whitespace  # noqa: PLC0415

    tokenizer = Tokenizer(WordPiece(vocab, unk_token=UNK_TOKEN, max_input_chars_per_word=100))
    tokenizer.pre_tokenizer = Whitespace()
    if lowercase:
        tokenizer.normalizer = Lowercase()
    return tokenizer


def generate_vocab_txt() -> dict:
    """Freeze a vocab.txt and what transformers' loader makes of it.

    The file content is embedded so the C# side replays the exact bytes rather
    than a second fixture that could drift away from this one.
    """
    tokens = list(WORDPIECE_VOCAB)
    # transformers reads the file in text mode and does token.rstrip("\n"); the
    # trailing newline of the last line therefore adds no entry.
    content = "".join(f"{token}\n" for token in tokens)
    vocab = {token: index for index, token in enumerate(tokens)}

    tokenizer = _wordpiece_tokenizer(vocab, lowercase=False)
    cases = []
    for i, text in enumerate(WORDPIECE_TEXTS):
        enc = tokenizer.encode(text)
        cases.append({"id": i, "text": text, "tokens": enc.tokens, "ids": enc.ids})

    return {
        "metadata": {
            "algorithm": "VocabTxtLoader",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "reference_calls": [
                "transformers.BertTokenizer vocab.txt loading: rstrip('\\n') then vocab[token] = index",
                "tokenizers.Tokenizer(WordPiece(vocab, unk_token)).encode",
            ],
            "vocab_txt": content,
            "vocab": vocab,
            "unk_token": UNK_TOKEN,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_tokenizer_json() -> dict:
    """Freeze two tokenizer.json documents — WordPiece and Unigram — and their encodings."""
    import json as _json  # noqa: PLC0415
    from tokenizers import Tokenizer  # noqa: PLC0415
    from tokenizers.models import Unigram  # noqa: PLC0415
    from tokenizers.pre_tokenizers import Metaspace  # noqa: PLC0415
    from sentencepiece import sentencepiece_model_pb2 as model_pb2  # noqa: PLC0415

    wordpiece_vocab = {token: index for index, token in enumerate(WORDPIECE_VOCAB)}
    wordpiece = _wordpiece_tokenizer(wordpiece_vocab, lowercase=True)
    wordpiece_cases = []
    for i, text in enumerate(WORDPIECE_TEXTS):
        enc = wordpiece.encode(text)
        wordpiece_cases.append({"id": i, "model": "WordPiece", "text": text, "tokens": enc.tokens, "ids": enc.ids})

    proto = model_pb2.ModelProto()
    proto.ParseFromString((ORACLE_DIR / TINY_SP_MODEL).read_bytes())
    unigram_pieces = [(p.piece, p.score) for p in proto.pieces]
    unigram = Tokenizer(Unigram(unigram_pieces, unk_id=proto.trainer_spec.unk_id, byte_fallback=False))
    unigram.pre_tokenizer = Metaspace()
    unigram.add_special_tokens([UNK_TOKEN_LOWER, "<s>", "</s>"])
    unigram_cases = []
    for i, text in enumerate(LOADER_TEXTS):
        enc = unigram.encode(text)
        unigram_cases.append({"id": i, "model": "Unigram", "text": text, "tokens": enc.tokens, "ids": enc.ids})

    return {
        "metadata": {
            "algorithm": "TokenizerJsonLoader",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "reference_calls": [
                "tokenizers.Tokenizer.from_file('tokenizer.json') then .encode",
            ],
            "wordpiece_tokenizer_json": _json.loads(wordpiece.to_str()),
            "wordpiece_vocab": wordpiece_vocab,
            "wordpiece_unk_token": UNK_TOKEN,
            "wordpiece_lowercase": True,
            "unigram_tokenizer_json": _json.loads(unigram.to_str()),
            "unigram_unk_id": proto.trainer_spec.unk_id,
            "count": len(wordpiece_cases) + len(unigram_cases),
        },
        "cases": wordpiece_cases + unigram_cases,
    }


def generate_spiece_model() -> dict:
    """Freeze what sentencepiece's own parser reads out of tests/oracles/tiny_sp.model.

    Piece *types* come from the protobuf rather than from the IsControl/IsUnknown
    helpers: the proto is the format Lodestar's loader claims to read, so it is the
    right reference for it.
    """
    import sentencepiece as spm  # noqa: PLC0415
    from sentencepiece import sentencepiece_model_pb2 as model_pb2  # noqa: PLC0415

    proto = model_pb2.ModelProto()
    proto.ParseFromString((ORACLE_DIR / TINY_SP_MODEL).read_bytes())
    pieces = [
        {"piece": p.piece, "score": p.score, "type": int(p.type), "id": i}
        for i, p in enumerate(proto.pieces)
    ]

    sp = spm.SentencePieceProcessor(model_file=str(ORACLE_DIR / TINY_SP_MODEL))
    cases = [
        {"id": k, "text": t, "pieces": sp.encode(t, out_type=str), "ids": sp.encode(t, out_type=int)}
        for k, t in enumerate(LOADER_TEXTS)
    ]

    return {
        "metadata": {
            "algorithm": "SentencePieceModelLoader",
            "library": "sentencepiece",
            "library_version": version("sentencepiece"),
            "model": "tiny_sp.model (self-trained unigram, identity normalizer)",
            "reference_calls": [
                "sentencepiece_model_pb2.ModelProto().ParseFromString(open('spiece.model','rb').read())",
                "sentencepiece.SentencePieceProcessor(model_file=…).encode",
            ],
            "normalizer_name": proto.normalizer_spec.name,
            "add_dummy_prefix": proto.normalizer_spec.add_dummy_prefix,
            "remove_extra_whitespaces": proto.normalizer_spec.remove_extra_whitespaces,
            "escape_whitespaces": proto.normalizer_spec.escape_whitespaces,
            "unk_id": proto.trainer_spec.unk_id,
            "bos_id": proto.trainer_spec.bos_id,
            "eos_id": proto.trainer_spec.eos_id,
            "pad_id": proto.trainer_spec.pad_id,
            "pieces": pieces,
            "count": len(cases),
        },
        "cases": cases,
    }


# Names the markers literally, plus ordinary multilingual text; see
# generate_xlmr_fairseq's docstring.
XLMR_TEXTS = [
    "le renard brun rapide saute par-dessus le chien paresseux",
    "el zorro marron rapido salta sobre el perro perezoso",
    "der schnelle braune Fuchs springt uber den faulen Hund",
    "быстрая коричневая лиса прыгает через ленивую собаку",
    "速い茶色のキツネが怠け者の犬を飛び越える",
    "a <unk> b",
    "le chat <mask> sur le tapis",
    "<s> hello </s>",
    "<pad><pad> padding",
    MASK_TOKEN,
    "un texte avec <s>, </s>, <pad>, <unk> et <mask> dedans",
    # Since #75 these are rewritten by XLM-R's own nmt_nfkc charsmap before
    # segmentation; escaped so no editor can normalise them by accident.
    "\uff2c\uff25 \uff32\uff25\uff2e\uff21\uff32\uff24 \uff52\uff41\uff50\uff49\uff44\uff45",  # full-width LE RENARD rapide
    "\ufb01nancier, \ufb02amme et \u0153uvre",  # fi and fl ligatures
    "cafe\u0301 de\u0301ja\u0300 vu",  # decomposed accents, which nmt_nfkc recomposes
    "\u2168 siecles, \u2460\u2461\u2462 etapes",  # roman numeral IX, circled digits
    "espace\u00a0insecable et espace\u3000ideographique",
    "un\u0001texte\u0002avec\u0007des controles",
]

# The five strings a vocabulary in this layout must never segment onto.
XLMR_MARKERS = ["<s>", "<pad>", "</s>", UNK_TOKEN_LOWER, MASK_TOKEN]


def generate_xlmr_fairseq() -> dict:
    """Freeze sentencepiece's encoding of the XLM-R vocabulary in fairseq layout.

    The fixture is built by tools/fetch_xlmr_vocab.py: XLM-R's own 250 000
    pieces and scores, at the ids HuggingFace gives them, with <s>=0, <pad>=1,
    </s>=2, <unk>=3 and <mask>=250001 typed CONTROL/UNKNOWN, and the normalizer
    set to identity — the pipeline Lodestar reproduces. See that script for why
    the stock sentencepiece.bpe.model cannot be replayed directly.

    This is the corpus the id-based control filter could not have passed: every
    marker sits outside 0-2 except <s>, and <mask> sits 250 000 ids away from
    where the guess looked.

    XLMR_TEXTS names the marker strings literally -- a piece only ever matches
    where its literal characters occur, so an input without "<" in it cannot tell
    a tokenizer that excludes the control pieces from one that does not -- plus
    ordinary multilingual text over XLM-R's own vocabulary, so a fixture that only
    ever saw Latin script does not leave most of it unexercised.
    """
    import sentencepiece as spm  # noqa: PLC0415
    from sentencepiece import sentencepiece_model_pb2 as model_pb2  # noqa: PLC0415

    path = ORACLE_DIR / XLMR_FAIRSEQ_MODEL
    proto = model_pb2.ModelProto()
    proto.ParseFromString(path.read_bytes())
    sp = spm.SentencePieceProcessor(model_file=str(path))

    markers = [
        {
            "piece": piece,
            "id": next(i for i, p in enumerate(proto.pieces) if p.piece == piece),
            "type": int(next(p.type for p in proto.pieces if p.piece == piece)),
        }
        for piece in XLMR_MARKERS
    ]
    # Spot-checked rather than all 250 002: the vocabulary is the committed
    # .model, and repeating it as JSON would double a 5 MB fixture for nothing.
    sampled = [
        {"id": i, "piece": sp.id_to_piece(i), "score": sp.get_score(i), "type": int(proto.pieces[i].type)}
        for i in (0, 1, 2, 3, 4, 5, 1000, 100_000, 250_000, 250_001)
    ]
    cases = [
        {"id": k, "text": t, "pieces": sp.encode(t, out_type=str), "ids": sp.encode(t, out_type=int)}
        for k, t in enumerate(XLMR_TEXTS)
    ]

    return {
        "metadata": {
            "algorithm": "SentencePieceTokenizer",
            "library": "sentencepiece",
            "library_version": version("sentencepiece"),
            "model": (
                "xlmr_fairseq.model (xlm-roberta-base vocabulary, fairseq layout, "
                "identity normalizer — see tools/fetch_xlmr_vocab.py)"
            ),
            "reference_calls": [
                "sentencepiece.SentencePieceProcessor(model_file='xlmr_fairseq.model').encode",
            ],
            "normalizer_name": proto.normalizer_spec.name,
            "vocab_size": len(proto.pieces),
            "unk_id": proto.trainer_spec.unk_id,
            "bos_id": proto.trainer_spec.bos_id,
            "eos_id": proto.trainer_spec.eos_id,
            "pad_id": proto.trainer_spec.pad_id,
            "markers": markers,
            "sampled_pieces": sampled,
            "count": len(cases),
        },
        "cases": cases,
    }


# --- Batch encoding and batched embedding (issue #60) --------------------------
# See generate_batch_encoding's docstring for why the chain is frozen in two halves.

# Appended after the WordPiece vocabulary; see _batch_tokenizer's docstring for why.
CLS_TOKEN = "[CLS]"
SEP_TOKEN = "[SEP]"
PAD_TOKEN = "[PAD]"
BATCH_VOCAB = [*WORDPIECE_VOCAB, CLS_TOKEN, SEP_TOKEN, PAD_TOKEN]

# Mirrors tools/build_tiny_models.py; see _batch_embedding_table's docstring for
# why it is duplicated rather than imported.
EMBEDDING_ROWS = 64
EMBEDDING_DIM = 4

BATCH_MAX_LENGTH = 8

# The four documented edges (see _assert_batch_edges) fall out of one batch
# under this limit.
BATCH_EDGE_TEXTS = [
    "",
    "the",
    "quick brown fox jumps.",
    EMBEDDING_SENTENCE,
]

BATCH_MIXED_TEXTS = [
    "hello world!",
    "the",
    "the dog runs and the cat plays",
    "unaffable",
    "the cats playing",
    EMBEDDING_SENTENCE,
    "",
    "lovely love loved lover",
]

BATCH_UNKNOWN_TEXTS = [
    "unknownxyz",
    "the unknownxyz cat",
    "zzz qqq",
]


def _batch_embedding_table():
    """The synthetic embedding matrix `tiny_embedder.onnx` gathers from.

    Every entry is a multiple of 1/64 with magnitude below 1/2, so a sum of a few
    dozen rows is exact in float32 and the only inexactness in the whole pipeline
    is the final division and the normalization.

    EMBEDDING_ROWS/EMBEDDING_DIM mirror tools/build_tiny_models.py, duplicated
    rather than imported: that script runs in a virtualenv carrying `onnx`, this
    one in a virtualenv carrying scikit-learn, and neither has the other's
    dependency. The table is frozen into the corpus and a C# test gathers a row
    through the ONNX model and compares it, so the two copies cannot drift apart
    in silence.
    """
    import numpy as np  # noqa: PLC0415

    table = np.zeros((EMBEDDING_ROWS, EMBEDDING_DIM), dtype=np.float64)
    for i in range(EMBEDDING_ROWS):
        for d in range(EMBEDDING_DIM):
            table[i, d] = (((7 * i + 13 * d) % 64) - 32) / 64.0
    return table


def _batch_tokenizer(vocab: dict[str, int], max_length: int | None):
    """A HuggingFace tokenizer configured the way `BatchEncoder` configures itself.

    CLS_TOKEN/SEP_TOKEN/PAD_TOKEN are appended after the WordPiece vocabulary
    rather than placed at the front, where BERT keeps them: nothing may assume
    [CLS] is id 101, or id 0, or that the special tokens are contiguous with each
    other -- the template names a token and the vocabulary is what answers with
    an id.
    """
    from tokenizers.processors import TemplateProcessing  # noqa: PLC0415

    tokenizer = _wordpiece_tokenizer(vocab, lowercase=False)
    tokenizer.post_processor = TemplateProcessing(
        single=f"{CLS_TOKEN} $A {SEP_TOKEN}",
        special_tokens=[(CLS_TOKEN, vocab[CLS_TOKEN]), (SEP_TOKEN, vocab[SEP_TOKEN])],
    )
    # padding="longest": to the longest row of this batch, never to max_length.
    tokenizer.enable_padding(pad_id=vocab[PAD_TOKEN], pad_token=PAD_TOKEN)
    if max_length is None:
        tokenizer.no_truncation()
    else:
        tokenizer.enable_truncation(max_length=max_length)
    return tokenizer


def _batch_reference(ids, mask, table):
    """Mean-pool the gathered rows behind the mask, then L2-normalize.

    The sentence-transformers recipe, in float64: `sum(E[ids] * mask) /
    clamp(sum(mask), min=1e-9)`, scaled to unit length.
    """
    import numpy as np  # noqa: PLC0415

    gathered = table[np.array(ids, dtype=np.int64)]
    weights = np.array(mask, dtype=np.float64)[:, :, None]
    active = np.maximum(np.array(mask, dtype=np.float64).sum(axis=1), 1e-9)[:, None]
    pooled = (gathered * weights).sum(axis=1) / active
    norm = np.sqrt((pooled * pooled).sum(axis=1))[:, None]
    normalized = np.divide(pooled, norm, out=pooled.copy(), where=norm > 0)
    return pooled.tolist(), normalized.tolist()


def _batch_case(cid: int, name: str, texts: list[str], vocab: dict[str, int],
                max_length: int | None, table) -> dict:
    encodings = _batch_tokenizer(vocab, max_length).encode_batch(texts)
    ids = [enc.ids for enc in encodings]
    mask = [enc.attention_mask for enc in encodings]
    pooled, normalized = _batch_reference(ids, mask, table)
    return {
        "id": cid,
        "name": name,
        "texts": texts,
        "max_length": max_length,
        "sequence_length": len(ids[0]),
        "input_ids": ids,
        "attention_mask": mask,
        "pooled": pooled,
        "pooled_normalized": normalized,
    }


def _assert_batch_edges(case: dict) -> None:
    """Fail generation if the edge fixture has stopped exercising its four edges.

    BATCH_EDGE_TEXTS is chosen so the four edges checked below -- nothing, one
    token, exactly BATCH_MAX_LENGTH, one over it -- fall out of the same batch.
    A vocabulary or template change can leave these texts encoding to lengths
    that no longer straddle the limit. The test replaying them would still pass,
    having quietly become a test of nothing, which is why each edge is asserted
    here rather than left to this docstring.
    """
    lengths = [sum(row) for row in case["attention_mask"]]
    limit = case["max_length"]
    template_tokens = 2  # [CLS] and [SEP]
    if lengths[0] != template_tokens:
        raise AssertionError(f"the empty text should encode to the template alone, got {lengths[0]}")
    if lengths[1] != template_tokens + 1:
        raise AssertionError(f"'the' should encode to one token plus the template, got {lengths[1]}")
    if lengths[2] != limit:
        raise AssertionError(f"the exactly-at-the-limit text encodes to {lengths[2]}, not {limit}")
    if lengths[3] != limit:
        raise AssertionError(f"the over-the-limit text should truncate to {limit}, got {lengths[3]}")
    untruncated = _batch_tokenizer(
        {tok: i for i, tok in enumerate(BATCH_VOCAB)}, None).encode(case["texts"][3])
    if len(untruncated.ids) != limit + 1:
        raise AssertionError(
            f"the over-the-limit text encodes to {len(untruncated.ids)} tokens; "
            f"it must be exactly one over {limit} for the fixture to test the boundary")


def generate_batch_encoding() -> dict:
    """Freeze `tokenize -> insert specials -> truncate -> pad -> infer -> pool`, in two halves.

    Tokenization is integers taken from HuggingFace `tokenizers` with the
    post-processor and padding enabled -- the library the C# reproduces -- and
    replayed for exact equality: an id is right or it is not, and a tolerance
    would only hide an off-by-one in the template.

    The embedding half is float64 arithmetic over the same table
    `tools/build_tiny_models.py` bakes into `tiny_embedder.onnx` (a lone Gather
    node), worked out independently rather than as a second copy of the C# code
    -- the only version of it worth freezing. ONNX Runtime hands back float32
    and normalizes in float32, so agreement with this exact reference is bounded
    by the float32 epsilon (~1e-7 relative) and by nothing this repository can
    improve; demanding 1e-9 would mean reproducing the C# rounding sequence in
    numpy, at which point the corpus mirrors the code and catches nothing. What
    *is* asserted exactly lives in the C# suite: the ids and mask here, a
    batched vector against the single-sequence one for the same text, and the
    vectorized net10.0 result against the scalar netstandard2.0 one.
    """
    vocab = {tok: i for i, tok in enumerate(BATCH_VOCAB)}
    table = _batch_embedding_table()

    cases = [
        _batch_case(0, "mixed_lengths", BATCH_MIXED_TEXTS, vocab, None, table),
        _batch_case(1, "edges", BATCH_EDGE_TEXTS, vocab, BATCH_MAX_LENGTH, table),
        _batch_case(2, "unknown_tokens", BATCH_UNKNOWN_TEXTS, vocab, None, table),
        _batch_case(3, "single_text", [CAT_SENTENCE], vocab, None, table),
        # Every row the same length, so the batch is already rectangular and no
        # padding is written at all — the control the padded cases are read against.
        _batch_case(4, "no_padding_needed", ["the cat", "the dog", "the fox"], vocab, None, table),
        _batch_case(5, "truncated", BATCH_MIXED_TEXTS, vocab, BATCH_MAX_LENGTH, table),
    ]
    _assert_batch_edges(cases[1])

    if max(max(row) for case in cases for row in case["input_ids"]) >= EMBEDDING_ROWS:
        raise AssertionError(
            f"an id falls outside the {EMBEDDING_ROWS}-row table tiny_embedder.onnx gathers from")

    return {
        "metadata": {
            "algorithm": "BatchEncoding",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "reference_calls": [
                "tokenizers.Tokenizer.encode_batch, TemplateProcessing(single='[CLS] $A [SEP]'), "
                "enable_padding(longest), enable_truncation(max_length)",
                "mean pool with attention mask + L2 normalize (sentence-transformers recipe)",
            ],
            "vocab": vocab,
            "unk_token": UNK_TOKEN,
            "template": {"prefix": [CLS_TOKEN], "suffix": [SEP_TOKEN], "pad": PAD_TOKEN},
            "embedding_rows": EMBEDDING_ROWS,
            "embedding_dim": EMBEDDING_DIM,
            "embedding_table": table.tolist(),
            "count": len(cases),
        },
        "cases": cases,
    }


# What normalization changes and identity hides (#75); see generate_normalizer's
# docstring.
NORMALIZER_TEXTS = [
    "",
    "already normal text",
    "\uff2c\uff25 \uff32\uff25\uff2e\uff21\uff32\uff24",       # full-width letters
    "\uff11\uff12\uff13",                                       # full-width digits
    "\uff71\uff92\uff98\uff76",                                # half-width katakana
    "cafe\u0301",                                                # decomposed
    "caf\u00e9",                                                 # composed
    "\ufb01nancier \ufb02amme \ufb03n",                           # ligatures
    "\u2168 \u2460\u2461 \u3231",                                # roman numeral, circled, squared
    "a\u00a0b",                                                  # non-breaking space
    "a\u3000b",                                                  # ideographic space
    "a\tb",                                                      # tab
    "a\nb",                                                      # newline
    "a\u200bb",                                                  # zero-width space
    "\ufeffbom",                                                 # byte-order mark
    "a\u0001b\u0007c",                                           # control characters
    "  spaced   out  ",
    "MiXeD CaSe TeXt",                                          # only the _cf rules fold this
    "\u00df \u2460 \u00a4",                                       # the custom rules: sharp s, circled one, currency sign
    "\u2581 already escaped",                                    # the meta symbol itself
    "\u0130stanbul",                                             # dotted capital I
    "\u1e9b\u0323",                                              # long s with dot, plus dot below
]

# Each carries a different charsmap (or none); see generate_normalizer's docstring.
NORMALIZER_FIXTURES = [
    ("xlmr_fairseq.model", "xlm-roberta-base, nmt_nfkc (the spm_train default)"),
    ("nmt_nfkc_cf.model", "self-trained, nmt_nfkc_cf (case folding)"),
    ("custom_norm.model", "self-trained, three hand-written rules from a normalization_rule_tsv"),
    (TINY_SP_MODEL, "self-trained, identity: no charsmap at all"),
]


def generate_normalizer() -> dict:
    """Freeze what each fixture's precompiled_charsmap does to text.

    Two references per case, because they answer different questions:

    * ``normalized`` is the charsmap alone. It is produced from a copy of the
      model with add_dummy_prefix, remove_extra_whitespaces and
      escape_whitespaces turned off, so nothing but the map speaks — that is
      exactly the boundary of ``PrecompiledNormalizer.Normalize``.
    * ``pieces``/``ids`` are the whole pipeline on the stock flags, which is what
      ``SentencePieceTokenizer.Encode`` reproduces.

    A test that only replayed the second could pass with the normalization and
    the whitespace handling wrong in compensating ways.

    NORMALIZER_TEXTS covers what normalization changes and identity hides, per
    the acceptance criteria of #75: width forms, composition, ligatures,
    whitespace of every flavour, control characters, case -- plus the three
    rules only custom_norm.model performs.

    NORMALIZER_FIXTURES: each entry carries a different charsmap, which is the
    point -- the same interpreter must handle all of them. tiny_sp.model is the
    control case, with no charsmap at all.

    custom_norm.model alone also carries charsmap_base64, the same blob
    `tokenizers` writes into a tokenizer.json, in the same encoding, so the JSON
    loader can be tested against a real map without a hand-pasted constant.
    Base64 of the much larger nmt_nfkc map would add 300 KB to the corpus to say
    nothing new.
    """
    import sentencepiece as spm  # noqa: PLC0415
    from sentencepiece import sentencepiece_model_pb2 as model_pb2  # noqa: PLC0415

    models = []
    cases = []
    for filename, description in NORMALIZER_FIXTURES:
        path = ORACLE_DIR / filename
        proto = model_pb2.ModelProto()
        proto.ParseFromString(path.read_bytes())

        bare = model_pb2.ModelProto()
        bare.CopyFrom(proto)
        bare.normalizer_spec.add_dummy_prefix = False
        bare.normalizer_spec.remove_extra_whitespaces = False
        bare.normalizer_spec.escape_whitespaces = False

        stock = spm.SentencePieceProcessor(model_file=str(path))
        charsmap_only = spm.SentencePieceProcessor(model_proto=bare.SerializeToString())

        entry = {
            "model": filename,
            "description": description,
            "normalizer_name": proto.normalizer_spec.name,
            "charsmap_bytes": len(proto.normalizer_spec.precompiled_charsmap),
            "vocab_size": len(proto.pieces),
        }
        if filename == "custom_norm.model":
            # Only for this fixture, so the JSON loader has a real map to test
            # against; see this function's docstring for why not the others.
            entry["charsmap_base64"] = base64.b64encode(proto.normalizer_spec.precompiled_charsmap).decode("ascii")
        models.append(entry)
        for text in NORMALIZER_TEXTS:
            cases.append({
                "id": len(cases),
                "model": filename,
                "text": text,
                "normalized": charsmap_only.normalize(text),
                "pieces": stock.encode(text, out_type=str),
                "ids": stock.encode(text, out_type=int),
            })

    return {
        "metadata": {
            "algorithm": "PrecompiledNormalizer",
            "library": "sentencepiece",
            "library_version": version("sentencepiece"),
            "reference_calls": [
                "sentencepiece.SentencePieceProcessor(model_file=…).normalize  (charsmap only)",
                "sentencepiece.SentencePieceProcessor(model_file=…).encode     (whole pipeline)",
            ],
            "models": models,
            "count": len(cases),
        },
        "cases": cases,
    }


# --- Classification metrics (issue #61), see _metric_fixtures's docstring -----

METRIC_SEED = SEED + 61
ZERO_DIVISIONS = (0, 1)
BETAS = (0.5, 2.0)
REPORT_DIGITS = (2, 3)


def _finite_or_name(value: float) -> float | str:
    """Encode a non-finite oracle value as a string, and round a finite one.

    JSON has no literal for NaN or infinity, and this repository's loader reads
    strict JSON. Every other oracle value in the corpus is a plain number; these
    are the first that cannot be.

    A finite value goes through stable() for the reason STABLE_DIGITS gives:
    balanced accuracy, Matthews correlation and Cohen's kappa all come out of
    np.dot/np.outer/xp.mean reductions, so their last bits describe this host's
    BLAS kernel rather than the metric. The three name strings are returned
    untouched — "NaN" must stay "NaN", not become a rounded number.
    """
    if math.isnan(value):
        return "NaN"
    if math.isinf(value):
        return "Infinity" if value > 0 else "-Infinity"
    return stable(value)


def _metric_fixtures() -> list[dict]:
    """Fixtures chosen for where implementations diverge, not average behaviour.

    A class never predicted, a class absent from the truth, a labels= subset
    (which drops samples and turns the report's accuracy row into a micro-avg
    row), and non-contiguous label values that catch any implementation
    assuming 0..k-1. Each fixture is emitted twice, unweighted and weighted,
    because sample_weight changes the dtype of every count upstream.

    class_only_in_pred is small enough to move balanced accuracy off the naive
    per-sample average: it is scored only over the classes present in y_true
    (0.75), not over every class either array mentions (0.5), and the adjusted
    form follows the same restriction.
    """
    rng = SeededRandom(METRIC_SEED)
    fixtures: list[dict] = []

    def noisy(truth: list[int], classes: list[int], flip: float) -> list[int]:
        return [
            t if rng.random() >= flip else rng.choice([c for c in classes if c != t])
            for t in truth
        ]

    def add(name, y_true, y_pred, labels=None, target_names=None, pos_label=1):
        fixtures.append({
            "name": name,
            "y_true": [int(v) for v in y_true],
            "y_pred": [int(v) for v in y_pred],
            "labels": labels,
            "target_names": target_names,
            "pos_label": pos_label,
            "sample_weight": [round(rng.uniform(0.1, 3.0), 3) for _ in y_true],
        })

    balanced = [rng.randint(0, 1) for _ in range(200)]
    add("binary_balanced", balanced, noisy(balanced, [0, 1], 0.2),
        target_names=["negative", "positive"])

    imbalanced = [0] * 190 + [1] * 10
    add("binary_imbalanced", imbalanced, noisy(imbalanced, [0, 1], 0.3))

    three = [rng.randint(0, 2) for _ in range(300)]
    add("multiclass_3", three, noisy(three, [0, 1, 2], 0.35),
        target_names=["alpha", "beta", "gamma"])

    ten = [rng.randint(0, 9) for _ in range(500)]
    add("multiclass_10", ten, noisy(ten, list(range(10)), 0.5))

    # Class 2 is in y_true and never predicted: its precision divides by zero.
    add("class_never_predicted", [0, 0, 1, 1, 2, 2, 1, 0], [0, 1, 1, 1, 0, 1, 1, 0])

    # Class 3 is predicted and absent from y_true: its recall divides by zero.
    add("class_absent_from_truth", [0, 0, 1, 1, 0, 1], [0, 3, 1, 3, 0, 1])

    perfect = [rng.randint(0, 2) for _ in range(50)]
    add("perfect", perfect, list(perfect))
    add("all_wrong", perfect, [(v + 1) % 3 for v in perfect])

    add("single_sample", [1], [1])
    add("single_class", [1, 1, 1, 1], [1, 1, 1, 1])

    subset = [rng.randint(0, 3) for _ in range(120)]
    add("labels_subset", subset, noisy(subset, [0, 1, 2, 3], 0.4), labels=[0, 2])

    sparse = [rng.choice([-1, 5, 42]) for _ in range(120)]
    add("non_contiguous_labels", sparse, noisy(sparse, [-1, 5, 42], 0.4), pos_label=5)

    # A class predicted but never true; see this function's docstring for why.
    add("class_only_in_pred", [0, 0, 1], [0, 2, 1])

    return fixtures


def _binary_average_applies(observed: list[int], pos_label: int) -> bool:
    """Mirror scikit-learn's own admissibility rule for average="binary"."""
    if len(observed) > 2:
        return False
    return pos_label in observed or len(observed) < 2


def _metric_case(fx: dict, weighted: bool) -> dict:
    """One fixture's full metric surface, unweighted or weighted.

    ``case["normalized"]`` passes labels=labels so every mode's matrix keeps the
    same shape and label ordering as ``case["confusion_matrix"]`` above it,
    rather than falling back to the full observed label set for
    labels_subset-style fixtures. Each entry goes through stable(), not bare
    float(): normalize= divides by a row, column or grand sum that numpy
    reduced, so it carries the same host-dependent last bits as every other
    reduced value here. confusion_matrix's own nan_to_num keeps every entry
    finite, so none needs _finite_or_name's string encoding.
    """
    y_true, y_pred = fx["y_true"], fx["y_pred"]
    labels, pos_label = fx["labels"], fx["pos_label"]
    sw = fx["sample_weight"] if weighted else None
    observed = sorted(set(y_true) | set(y_pred))
    effective = labels if labels is not None else observed
    averages = ["micro", "macro", "weighted"]
    if _binary_average_applies(observed, pos_label):
        averages.append("binary")

    cm = skm.confusion_matrix(y_true, y_pred, labels=labels, sample_weight=sw)
    case = {
        "fixture": fx["name"],
        "weighted": weighted,
        "y_true": y_true,
        "y_pred": y_pred,
        "sample_weight": sw,
        "labels": labels,
        "target_names": fx["target_names"],
        "pos_label": pos_label,
        "expected_labels": [int(v) for v in effective],
        "confusion_matrix": [[stable(v) for v in row] for row in cm.tolist()],
        "accuracy": stable(skm.accuracy_score(y_true, y_pred, sample_weight=sw)),
        "accuracy_count": stable(
            skm.accuracy_score(y_true, y_pred, normalize=False, sample_weight=sw)),
        "averaged": {},
        "per_class": {},
        "fbeta": {},
        "reports": {},
    }

    for zd in ZERO_DIVISIONS:
        for avg in averages:
            p, r, f, _ = skm.precision_recall_fscore_support(
                y_true, y_pred, labels=labels, average=avg, pos_label=pos_label,
                sample_weight=sw, zero_division=zd)
            case["averaged"][f"{avg}|{zd}"] = {
                "precision": stable(p), "recall": stable(r), "f1": stable(f)}
            for beta in BETAS:
                case["fbeta"][f"{beta}|{avg}|{zd}"] = stable(skm.fbeta_score(
                    y_true, y_pred, beta=beta, labels=labels, average=avg,
                    pos_label=pos_label, sample_weight=sw, zero_division=zd))
        p, r, f, s = skm.precision_recall_fscore_support(
            y_true, y_pred, labels=labels, average=None, sample_weight=sw,
            zero_division=zd)
        case["per_class"][str(zd)] = {
            "precision": [stable(v) for v in p],
            "recall": [stable(v) for v in r],
            "f1": [stable(v) for v in f],
            "support": [stable(v) for v in s],
        }

    for digits in REPORT_DIGITS:
        case["reports"][str(digits)] = skm.classification_report(
            y_true, y_pred, labels=labels, target_names=fx["target_names"],
            digits=digits, sample_weight=sw, zero_division=0)

    case["balanced_accuracy"] = _finite_or_name(
        skm.balanced_accuracy_score(y_true, y_pred, sample_weight=sw))
    case["balanced_accuracy_adjusted"] = _finite_or_name(
        skm.balanced_accuracy_score(y_true, y_pred, sample_weight=sw, adjusted=True))
    case["matthews"] = _finite_or_name(skm.matthews_corrcoef(y_true, y_pred, sample_weight=sw))
    for suffix, w in (("", None), ("_linear", "linear"), ("_quadratic", "quadratic")):
        case["kappa" + suffix] = _finite_or_name(
            skm.cohen_kappa_score(y_true, y_pred, weights=w, sample_weight=sw))
    # See this function's docstring for labels=labels and stable() here.
    case["normalized"] = {
        mode: [[stable(x) for x in row]
               for row in skm.confusion_matrix(
                   y_true, y_pred, labels=labels, sample_weight=sw, normalize=mode)]
        for mode in ("true", "pred", "all")
    }
    return case


def generate_classification_metrics() -> dict:
    with warnings.catch_warnings():
        # scikit-learn warns on every undefined metric; the corpus records the
        # value it returns, which is the thing under test.
        warnings.simplefilter("ignore")
        cases = [
            _metric_case(fx, weighted)
            for fx in _metric_fixtures()
            for weighted in (False, True)
        ]
    return {
        "metadata": {
            "algorithm": "ClassificationMetrics",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.accuracy_score",
                "sklearn.metrics.confusion_matrix",
                "sklearn.metrics.confusion_matrix(normalize=...)",
                "sklearn.metrics.precision_recall_fscore_support",
                "sklearn.metrics.fbeta_score",
                "sklearn.metrics.classification_report",
                "sklearn.metrics.balanced_accuracy_score",
                "sklearn.metrics.balanced_accuracy_score(adjusted=True)",
                "sklearn.metrics.matthews_corrcoef",
                "sklearn.metrics.cohen_kappa_score(weights=None|'linear'|'quadratic')",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }


# --- Clustering agreement (issue #172) ---------------------------------------


def _clustering_fixtures() -> list[tuple[str, list[int], list[int]]]:
    """Partitions whose agreement is the thing under test, degenerate ones included.

    The last four are the cases the reference answers surprisingly: an empty
    input and a single sample are perfect agreement rather than an error, and a
    single cluster on one side splits homogeneity from completeness.
    """
    return [
        ("identical", [0, 0, 1, 1, 2, 2], [0, 0, 1, 1, 2, 2]),
        ("renamed", [0, 0, 1, 1, 2, 2], [2, 2, 0, 0, 1, 1]),
        ("one moved", [0, 0, 1, 1, 2, 2], [0, 0, 1, 2, 2, 2]),
        ("independent", [0, 0, 1, 1], [0, 1, 0, 1]),
        ("split in two", [0, 0, 0, 1, 1, 1], [0, 1, 0, 1, 0, 1]),
        ("merged", [0, 0, 1, 1, 2, 2], [0, 0, 0, 0, 1, 1]),
        ("unbalanced", [0] * 8 + [1, 2], [0] * 7 + [1, 1, 2]),
        ("negative labels", [-1, -1, 3, 3], [7, 7, -2, -2]),
        ("one cluster predicted", [0, 0, 1, 1], [0, 0, 0, 0]),
        ("one class in truth", [0, 0, 0, 0], [0, 0, 1, 1]),
        ("every sample alone", [0, 0, 1, 1], [0, 1, 2, 3]),
        ("single sample", [0], [0]),
        ("empty", [], []),
    ]


def generate_clustering_agreement() -> dict:
    from sklearn import metrics as skmetrics

    cases = []
    with warnings.catch_warnings():
        # An undefined case warns and still returns the value under test.
        warnings.simplefilter("ignore")
        for name, true, pred in _clustering_fixtures():
            homogeneity, completeness, v_measure = (
                skmetrics.homogeneity_completeness_v_measure(true, pred))
            cases.append({
                "name": name,
                "labels_true": true,
                "labels_pred": pred,
                "adjusted_rand": skmetrics.adjusted_rand_score(true, pred),
                "normalized_mutual_information": skmetrics.normalized_mutual_info_score(true, pred),
                "fowlkes_mallows": skmetrics.fowlkes_mallows_score(true, pred),
                # scikit-learn 1.9.0 raises on an empty input here -- log(0) inside
                # mutual_info_score -- where every other metric in this corpus returns.
                "mutual_information": skmetrics.mutual_info_score(true, pred) if true else None,
                "rand": skmetrics.rand_score(true, pred),
                "pair_confusion": [
                    int(v) for v in
                    skmetrics.cluster.pair_confusion_matrix(true, pred).ravel()
                ] if true else [0, 0, 0, 0],
                "adjusted_mutual_information": skmetrics.adjusted_mutual_info_score(true, pred),
                "homogeneity": homogeneity,
                "completeness": completeness,
                "v_measure": v_measure,
            })

    return {
        "metadata": {
            "algorithm": "ClusteringAgreement",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.adjusted_rand_score",
                "sklearn.metrics.normalized_mutual_info_score",
                "sklearn.metrics.homogeneity_completeness_v_measure",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }


# --- Ranking, ordered list (issue #173, first lot) ---------------------------


def _ranking_fixtures() -> list[dict]:
    """Relevance and score pairs whose *ties* are the thing under test.

    A tie is where a plausible implementation agrees on the easy cases and
    disagrees where it matters: scikit-learn averages the discounted gain over
    the permutations of equal scores, which is a different number from ranking
    them arbitrarily -- 0.807 against 0.614 on the all-tied case, measured.
    """
    return [
        {"name": "perfectly ordered", "true": [3.0, 2.0, 1.0, 0.0], "score": [0.9, 0.5, 0.4, 0.1]},
        {"name": "reversed", "true": [3.0, 2.0, 1.0, 0.0], "score": [0.1, 0.4, 0.5, 0.9]},
        {"name": "every score tied", "true": [3.0, 2.0, 1.0, 0.0], "score": [0.5, 0.5, 0.5, 0.5]},
        {"name": "two tied among distinct", "true": [3.0, 2.0, 1.0, 0.0], "score": [0.9, 0.5, 0.5, 0.1]},
        {"name": "a tie across the k boundary", "true": [3.0, 2.0, 1.0, 0.0], "score": [0.9, 0.5, 0.5, 0.2]},
        {"name": "all-zero relevance", "true": [0.0, 0.0, 0.0, 0.0], "score": [0.9, 0.5, 0.4, 0.1]},
        {"name": "one relevant document", "true": [0.0, 0.0, 1.0, 0.0], "score": [0.9, 0.5, 0.4, 0.1]},
        {"name": "six documents", "true": [2.0, 0.0, 3.0, 1.0, 0.0, 2.0],
         "score": [0.8, 0.7, 0.6, 0.5, 0.4, 0.3]},
    ]


def generate_ranking() -> dict:
    import math

    import numpy as np
    from sklearn.metrics import dcg_score, ndcg_score

    cases = []
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        for fixture in _ranking_fixtures():
            true = np.array([fixture["true"]])
            score = np.array([fixture["score"]])
            cases.append({
                "name": fixture["name"],
                "y_true": fixture["true"],
                "y_score": fixture["score"],
                "dcg": float(dcg_score(true, score)),
                # ignore_ties goes through a bare np.argsort, an unstable quicksort, so
                # on a tied row this value is numpy's order rather than a defined one.
                "dcg_ignore_ties": float(dcg_score(true, score, ignore_ties=True)),
                "dcg_log_e": float(dcg_score(true, score, log_base=math.e)),
                "dcg_at_2": float(dcg_score(true, score, k=2)),
                "ndcg": float(ndcg_score(true, score)),
                "ndcg_ignore_ties": float(ndcg_score(true, score, ignore_ties=True)),
                "ndcg_at_2": float(ndcg_score(true, score, k=2)),
                "ndcg_at_99": float(ndcg_score(true, score, k=99)),
            })

    return {
        "metadata": {
            "algorithm": "Ranking",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.dcg_score",
                "sklearn.metrics.dcg_score(k=..., log_base=..., ignore_ties=...)",
                "sklearn.metrics.ndcg_score",
                "sklearn.metrics.ndcg_score(k=..., ignore_ties=...)",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }


# Three corpora open with the pair their own page works through, and the name is
# what ties a failing case back to the prose that explains it.
WORKED_CASE = "the worked case"
WORKED_CASE_WEIGHTED = "the worked case, weighted"


def _label_ranking_fixtures() -> list[dict]:
    """Rows where a plausible implementation and the reference part company."""
    wide = [0] * 20
    for j in (0, 9, 19):
        wide[j] = 1
    return [
        {"name": WORKED_CASE, "true": [[1, 0, 0], [0, 0, 1]],
         "score": [[0.75, 0.5, 1.0], [1.0, 0.2, 0.1]], "weight": None},
        {"name": WORKED_CASE_WEIGHTED, "true": [[1, 0, 0], [0, 0, 1]],
         "score": [[0.75, 0.5, 1.0], [1.0, 0.2, 0.1]], "weight": [1.0, 2.0]},
        {"name": "every label relevant", "true": [[1, 1, 1]],
         "score": [[0.7, 0.2, 0.1]], "weight": None},
        {"name": "no label relevant", "true": [[0, 0, 0]],
         "score": [[0.7, 0.2, 0.1]], "weight": None},
        {"name": "an empty row beside a scoring one", "true": [[0, 0, 0], [1, 0, 0]],
         "score": [[0.7, 0.2, 0.1], [0.7, 0.2, 0.1]], "weight": None},
        {"name": "every score equal, two of three relevant", "true": [[1, 1, 0]],
         "score": [[0.5, 0.5, 0.5]], "weight": None},
        {"name": "negative scores", "true": [[1, 0, 0]],
         "score": [[-0.7, -0.2, -0.1]], "weight": None},
        {"name": "relevant on top", "true": [[1, 1, 0, 0]],
         "score": [[0.9, 0.8, 0.2, 0.1]], "weight": None},
        {"name": "relevant at the bottom", "true": [[0, 0, 1, 1]],
         "score": [[0.9, 0.8, 0.2, 0.1]], "weight": None},
        # 20 columns: the width at which lot 1's Array.Sort stopped being stable. The
        # tie order is unobservable here, and a case is worth more than the claim.
        {"name": "twenty columns, every score tied", "true": [wide],
         "score": [[0.5] * 20], "weight": None},
        {"name": "twenty columns, strictly ordered", "true": [wide],
         "score": [[(20 - j) / 20 for j in range(20)]], "weight": None},
        # The relevant labels are at indices 0 and 2 but score 0.2 and 0.8, so their
        # index order and score order disagree -- the only shape a pairing bug shows in.
        {"name": "relevant labels out of score order", "true": [[1, 0, 1]],
         "score": [[0.2, 0.9, 0.8]], "weight": None},
    ]


def generate_label_ranking() -> dict:
    import numpy as np
    from sklearn.metrics import coverage_error, label_ranking_loss
    from sklearn.metrics import label_ranking_average_precision_score as lrap

    cases = []
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        for fixture in _label_ranking_fixtures():
            true = np.array(fixture["true"])
            score = np.array(fixture["score"])
            kw = {} if fixture["weight"] is None else {
                "sample_weight": np.array(fixture["weight"])}
            cases.append({
                "name": fixture["name"],
                "y_true": [v for row in fixture["true"] for v in row],
                "y_score": [v for row in fixture["score"] for v in row],
                "label_count": true.shape[1],
                "sample_weight": fixture["weight"],
                "lrap": float(lrap(true, score, **kw)),
                "coverage": float(coverage_error(true, score, **kw)),
                "ranking_loss": float(label_ranking_loss(true, score, **kw)),
            })

    return {
        "metadata": {
            "algorithm": "LabelRanking",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.label_ranking_average_precision_score",
                "sklearn.metrics.coverage_error",
                "sklearn.metrics.label_ranking_loss",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }


def _average_precision_binary_fixtures() -> list[dict]:
    """Binary cases, chosen where the step sum and the trapezoid part company."""
    return [
        {"name": WORKED_CASE, "true": [0, 0, 1, 1],
         "score": [0.1, 0.4, 0.35, 0.8], "pos_label": 1, "weight": None},
        {"name": "the worked case, pos_label 0", "true": [0, 0, 1, 1],
         "score": [0.1, 0.4, 0.35, 0.8], "pos_label": 0, "weight": None},
        {"name": WORKED_CASE_WEIGHTED, "true": [0, 0, 1, 1],
         "score": [0.1, 0.4, 0.35, 0.8], "pos_label": 1, "weight": [1.0, 2.0, 3.0, 4.0]},
        # Every score tied: the sum takes one step of the full recall at the group's
        # precision, where the trapezoid interpolates a diagonal that is not there.
        {"name": "every score tied", "true": [0, 1, 0, 1],
         "score": [0.5, 0.5, 0.5, 0.5], "pos_label": 1, "weight": None},
        {"name": "perfectly ranked", "true": [0, 0, 1, 1],
         "score": [0.1, 0.2, 0.3, 0.4], "pos_label": 1, "weight": None},
        {"name": "perfectly inverted", "true": [1, 1, 0, 0],
         "score": [0.1, 0.2, 0.3, 0.4], "pos_label": 1, "weight": None},
        {"name": "one positive, ranked last", "true": [0, 0, 0, 1],
         "score": [0.9, 0.8, 0.7, 0.1], "pos_label": 1, "weight": None},
        # scikit-learn warns here and returns a value rather than refusing.
        {"name": "no positive sample", "true": [0, 0, 0, 0],
         "score": [0.1, 0.4, 0.35, 0.8], "pos_label": 1, "weight": None},
        {"name": "every sample positive", "true": [1, 1, 1, 1],
         "score": [0.1, 0.4, 0.35, 0.8], "pos_label": 1, "weight": None},
        {"name": "negative scores", "true": [0, 0, 1, 1],
         "score": [-0.9, -0.6, -0.65, -0.2], "pos_label": 1, "weight": None},
        {"name": "labels are -1 and 1", "true": [-1, -1, 1, 1],
         "score": [0.1, 0.4, 0.35, 0.8], "pos_label": 1, "weight": None},
        {"name": "a tie spanning both classes", "true": [1, 0, 1, 0, 1],
         "score": [0.9, 0.5, 0.5, 0.5, 0.1], "pos_label": 1, "weight": None},
    ]


def generate_average_precision() -> dict:
    """average_precision_score: the binary sum, and the label matrix it averages over."""
    import numpy as np
    from sklearn.metrics import average_precision_score, auc, precision_recall_curve

    binary = []
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        for fixture in _average_precision_binary_fixtures():
            true = np.array(fixture["true"])
            score = np.array(fixture["score"])
            kw = {"pos_label": fixture["pos_label"]}
            if fixture["weight"] is not None:
                kw["sample_weight"] = np.array(fixture["weight"])

            # The trapezoid is carried beside the sum so the corpus itself records the
            # difference the metric exists to avoid; nothing in C# reproduces this column.
            precision, recall, _ = precision_recall_curve(true, score, **kw)
            binary.append({
                "name": fixture["name"],
                "y_true": fixture["true"],
                "y_score": fixture["score"],
                "pos_label": fixture["pos_label"],
                "sample_weight": fixture["weight"],
                "average_precision": float(average_precision_score(true, score, **kw)),
                "trapezoid": float(auc(recall, precision)),
            })

    multilabel = []
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        for fixture in _label_ranking_fixtures():
            true = np.array(fixture["true"])
            score = np.array(fixture["score"])
            kw = {} if fixture["weight"] is None else {
                "sample_weight": np.array(fixture["weight"])}
            multilabel.append({
                "name": fixture["name"],
                "y_true": [int(v) for row in fixture["true"] for v in row],
                "y_score": [v for row in fixture["score"] for v in row],
                "label_count": int(true.shape[1]),
                "sample_weight": fixture["weight"],
                "macro": float(average_precision_score(true, score, average="macro", **kw)),
                "micro": float(average_precision_score(true, score, average="micro", **kw)),
                "weighted": float(average_precision_score(true, score, average="weighted", **kw)),
                "per_label": [float(v) for v in np.atleast_1d(
                    average_precision_score(true, score, average=None, **kw))],
            })

    return {
        "metadata": {
            "algorithm": "AveragePrecision",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.average_precision_score",
                "sklearn.metrics.auc",
                "sklearn.metrics.precision_recall_curve",
            ],
            "count": len(binary) + len(multilabel),
        },
        "binary_cases": binary,
        "multilabel_cases": multilabel,
    }


def _deviance_fixtures() -> list[dict]:
    """Pairs chosen so every Tweedie regime is reached, and each one's domain edge."""
    return [
        {"name": WORKED_CASE, "true": [1.0, 2.0, 3.0, 4.0],
         "pred": [1.5, 2.5, 2.0, 4.5], "weight": None},
        {"name": WORKED_CASE_WEIGHTED, "true": [1.0, 2.0, 3.0, 4.0],
         "pred": [1.5, 2.5, 2.0, 4.5], "weight": [1.0, 2.0, 3.0, 4.0]},
        {"name": "a perfect prediction", "true": [1.5, 2.5, 2.0],
         "pred": [1.5, 2.5, 2.0], "weight": None},
        # y_true at zero is the boundary between the [1, 2) regime, which allows it,
        # and the >= 2 regimes, which do not -- so it is scored only where it is legal.
        {"name": "a zero truth", "true": [0.0, 2.0, 3.0],
         "pred": [1.0, 2.0, 3.0], "weight": None},
        {"name": "far apart", "true": [1.0, 10.0, 2.0],
         "pred": [8.0, 1.0, 9.0], "weight": None},
        # long-comment: the exclusion below is a reproducibility claim, and a reader
        # who does not know why will delete the flag and re-break the drift gate.
        # At power -2 this pair's deviance is a sum of three terms an order of
        # magnitude larger than the result, so its last bits follow the machine's
        # reduction order -- measured, a CI runner and this one disagree by one ulp
        # on it. Freezing either answer makes the gate a lottery. Only this fixture
        # is affected, and the other five still cover the negative regimes.
        {"name": "small values", "true": [0.01, 0.5, 0.25],
         "pred": [0.02, 0.4, 0.3], "weight": None, "skip_negative_powers": True},
    ]


def _tweedie_powers() -> list[float]:
    """One power per regime, plus the two the named deviances are."""
    return [-2.0, -1.0, 0.0, 1.0, 1.5, 2.0, 3.0]


def _tweedie_admits(power: float, true, pred) -> bool:
    """Whether the regime admits this pair, which is what the C# side refuses on."""
    if power < 0:
        return min(pred) > 0
    if power == 0:
        return True
    if power < 2:
        return min(true) >= 0 and min(pred) > 0
    return min(true) > 0 and min(pred) > 0


def _tweedie_row(fixture: dict, true, pred, kw: dict) -> list[dict]:
    """One entry per power the fixture is legal at, with its D2 where that is defined."""
    from sklearn.metrics import d2_tweedie_score, mean_tweedie_deviance

    # A D2 needs two samples and a truth that varies; where it does not, the
    # reference divides by zero and the C# side refuses instead.
    scored = len(true) >= 2 and len(set(fixture["true"])) > 1

    rows = []
    for power in _tweedie_powers():
        if power < 0 and fixture.get("skip_negative_powers"):
            continue
        if not _tweedie_admits(power, fixture["true"], fixture["pred"]):
            continue
        entry = {
            "power": power,
            "deviance": float(mean_tweedie_deviance(true, pred, power=power, **kw)),
        }
        if scored:
            entry["d2"] = float(d2_tweedie_score(true, pred, power=power, **kw))
        rows.append(entry)
    return rows


def _deviance_case(fixture: dict) -> dict:
    """Every number one fixture contributes, across the powers its values allow."""
    import numpy as np
    from sklearn.metrics import (
        d2_absolute_error_score,
        d2_pinball_score,
        mean_gamma_deviance,
        mean_poisson_deviance,
    )

    true = np.array(fixture["true"])
    pred = np.array(fixture["pred"])
    kw = {} if fixture["weight"] is None else {"sample_weight": np.array(fixture["weight"])}

    case = {
        "name": fixture["name"],
        "y_true": fixture["true"],
        "y_pred": fixture["pred"],
        "sample_weight": fixture["weight"],
        "tweedie": _tweedie_row(fixture, true, pred, kw),
        "d2_absolute_error": float(d2_absolute_error_score(true, pred, **kw)),
        "pinball": [
            {"alpha": alpha, "d2": float(d2_pinball_score(true, pred, alpha=alpha, **kw))}
            for alpha in (0.1, 0.25, 0.5, 0.75, 0.9)
        ],
    }
    if _tweedie_admits(1.0, fixture["true"], fixture["pred"]):
        case["poisson"] = float(mean_poisson_deviance(true, pred, **kw))
    if _tweedie_admits(2.0, fixture["true"], fixture["pred"]):
        case["gamma"] = float(mean_gamma_deviance(true, pred, **kw))
    return case


def _deviance_multioutput() -> dict:
    """Two outputs, the shape only the two pinball D2 scores accept."""
    import numpy as np
    from sklearn.metrics import d2_absolute_error_score, d2_pinball_score

    true = [[0.5, 1.0], [1.0, 1.0], [7.0, -6.0]]
    pred = [[0.0, 2.0], [-1.0, 2.0], [8.0, -5.0]]
    mt = np.array(true)
    mp = np.array(pred)
    return {
        "y_true": [v for row in true for v in row],
        "y_pred": [v for row in pred for v in row],
        "output_count": 2,
        "uniform_average": float(d2_absolute_error_score(mt, mp)),
        "raw_values": [float(v) for v in d2_absolute_error_score(mt, mp, multioutput="raw_values")],
        "pinball_uniform_average": float(d2_pinball_score(mt, mp, alpha=0.75)),
        "pinball_raw_values": [
            float(v) for v in d2_pinball_score(mt, mp, alpha=0.75, multioutput="raw_values")],
    }


def generate_regression_deviance() -> dict:
    """The three GLM deviances and the three D2 scores -- regression lot 2 (#202)."""
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        cases = [_deviance_case(fixture) for fixture in _deviance_fixtures()]
        multioutput = _deviance_multioutput()

    return {
        "metadata": {
            "algorithm": "RegressionDeviance",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.mean_tweedie_deviance",
                "sklearn.metrics.mean_poisson_deviance",
                "sklearn.metrics.mean_gamma_deviance",
                "sklearn.metrics.d2_tweedie_score",
                "sklearn.metrics.d2_pinball_score",
                "sklearn.metrics.d2_absolute_error_score",
            ],
            "count": len(cases),
        },
        "cases": cases,
        "multioutput": multioutput,
    }


def _internal_validity_fixtures() -> list[dict]:
    """Clusterings chosen where a plausible implementation and the reference part company."""
    two_by_two = [[1.0, 2.0], [1.5, 1.8], [5.0, 8.0], [8.0, 8.0], [1.0, 0.6], [9.0, 11.0]]
    return [
        {"name": WORKED_CASE, "features": two_by_two, "labels": [0, 0, 1, 1, 0, 1]},
        # Every cluster but one holds a single sample: the widest label count either
        # metric admits, n - 1, and the one where a singleton's zero spread shows.
        {"name": "one cluster of two, the rest singletons", "features": two_by_two,
         "labels": [0, 1, 2, 3, 4, 4]},
        {"name": "a singleton beside a large cluster", "features": two_by_two,
         "labels": [0, 0, 0, 0, 0, 1]},
        {"name": "three clusters", "features": two_by_two, "labels": [0, 1, 2, 0, 1, 2]},
        # No spread at all: Calinski-Harabasz answers 1 rather than dividing by zero,
        # and Davies-Bouldin 0 because the centroids coincide.
        {"name": "four identical points", "features": [[1.0, 1.0]] * 4, "labels": [0, 0, 1, 1]},
        {"name": "two points, far apart, duplicated",
         "features": [[0.0, 0.0], [0.0, 0.0], [10.0, 10.0], [10.0, 10.0]], "labels": [0, 0, 1, 1]},
        # One feature, and five: the shape is a flat span either way.
        {"name": "one feature", "features": [[1.0], [1.2], [8.0], [8.4], [1.1], [9.0]],
         "labels": [0, 0, 1, 1, 0, 1]},
        {"name": "five features",
         "features": [[1.0, 2.0, 3.0, 4.0, 5.0], [1.1, 2.1, 3.1, 4.1, 5.1],
                      [9.0, 8.0, 7.0, 6.0, 5.0], [9.1, 8.1, 7.1, 6.1, 5.1]],
         "labels": [0, 0, 1, 1]},
    ]


def generate_internal_validity() -> dict:
    """Calinski-Harabasz and Davies-Bouldin, which score a clustering with no reference (#192)."""
    import numpy as np
    from sklearn.metrics import calinski_harabasz_score, davies_bouldin_score

    cases = []
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        for fixture in _internal_validity_fixtures():
            features = np.array(fixture["features"])
            labels = np.array(fixture["labels"])
            cases.append({
                "name": fixture["name"],
                "features": [v for row in fixture["features"] for v in row],
                "feature_count": int(features.shape[1]),
                "labels": fixture["labels"],
                "calinski_harabasz": float(calinski_harabasz_score(features, labels)),
                "davies_bouldin": float(davies_bouldin_score(features, labels)),
            })

    return {
        "metadata": {
            "algorithm": "InternalValidity",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.calinski_harabasz_score",
                "sklearn.metrics.davies_bouldin_score",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }


def _top_k_fixtures() -> list[dict]:
    """Multiclass score matrices, where k is the question rather than ties.

    Every weight vector puts a value other than 1 on a sample that HITS. A
    vector weighting only the misses cannot separate "sums the weights" from
    "counts the samples": both give the unweighted count, which is what a first
    measurement of normalize=False reported before the fixture was fixed (#216).
    """
    return [
        {"name": "three classes", "true": [0, 1, 2, 2],
         "score": [[0.7, 0.2, 0.1], [0.3, 0.5, 0.2], [0.2, 0.3, 0.5], [0.5, 0.3, 0.2]],
         "weight": [5.0, 1.0, 1.0, 1.0]},
        {"name": "every prediction wrong at k=1", "true": [1, 2, 0],
         "score": [[0.6, 0.3, 0.1], [0.5, 0.4, 0.1], [0.2, 0.7, 0.1]],
         "weight": [2.0, 3.0, 4.0]},
        # scikit-learn infers the label set from y_true and refuses a wider score row,
        # so every class appears here; our own surface takes the count as a parameter.
        {"name": "ties in the score row", "true": [0, 1, 2],
         "score": [[0.5, 0.5, 0.0], [0.4, 0.4, 0.2], [0.1, 0.2, 0.7]],
         "weight": [1.0, 4.0, 1.0]},
        # A negative weight is accepted and takes the fraction out of [0, 1],
        # which the reference does too rather than refusing it.
        {"name": "a negative weight", "true": [0, 1, 2, 2],
         "score": [[0.7, 0.2, 0.1], [0.3, 0.5, 0.2], [0.2, 0.3, 0.5], [0.5, 0.3, 0.2]],
         "weight": [-1.0, 1.0, 1.0, 1.0]},
    ]


def _ranking_weighted_fixtures() -> list[dict]:
    """Multi-row queries, because a weight over one row cancels.

    Every fixture in _ranking_fixtures is a single query, so sample_weight
    there multiplies the numerator and the denominator alike and no vector can
    change a value. Weights need at least two rows that score differently,
    which is what these are.
    """
    return [
        # The corpus' perfect and reversed rows together: the pair whose mean the
        # weights move furthest, and whose unweighted mean is already asserted.
        {"name": "perfect and reversed", "true": [[3.0, 2.0, 1.0, 0.0], [3.0, 2.0, 1.0, 0.0]],
         "score": [[0.9, 0.5, 0.4, 0.1], [0.1, 0.4, 0.5, 0.9]], "weight": [1.0, 3.0]},
        {"name": "perfect and reversed, weight on the good row",
         "true": [[3.0, 2.0, 1.0, 0.0], [3.0, 2.0, 1.0, 0.0]],
         "score": [[0.9, 0.5, 0.4, 0.1], [0.1, 0.4, 0.5, 0.9]], "weight": [3.0, 1.0]},
        # Equal weights must give the unweighted mean back, which is the check
        # that the weighting is a mean rather than a sum.
        {"name": "equal weights are the plain mean",
         "true": [[3.0, 2.0, 1.0, 0.0], [3.0, 2.0, 1.0, 0.0]],
         "score": [[0.9, 0.5, 0.4, 0.1], [0.1, 0.4, 0.5, 0.9]], "weight": [2.0, 2.0]},
        # A row whose relevance is all zero scores 0 for ndcg and 0 for dcg, and
        # still carries its weight into the denominator.
        {"name": "a nothing-relevant row carries its weight",
         "true": [[3.0, 2.0, 1.0, 0.0], [0.0, 0.0, 0.0, 0.0]],
         "score": [[0.9, 0.5, 0.4, 0.1], [0.9, 0.5, 0.4, 0.1]], "weight": [1.0, 4.0]},
        # Accepted on both sides, and it takes the result outside the range the
        # page promises -- recorded rather than smoothed.
        {"name": "a negative weight", "true": [[3.0, 2.0, 1.0, 0.0], [3.0, 2.0, 1.0, 0.0]],
         "score": [[0.9, 0.5, 0.4, 0.1], [0.1, 0.4, 0.5, 0.9]], "weight": [-1.0, 2.0]},
        {"name": "three rows", "true": [[3.0, 2.0, 1.0], [1.0, 2.0, 3.0], [0.0, 1.0, 0.0]],
         "score": [[0.9, 0.5, 0.1], [0.9, 0.5, 0.1], [0.5, 0.5, 0.5]], "weight": [1.0, 2.0, 5.0]},
    ]


def generate_ranking_weighted() -> dict:
    """dcg_score and ndcg_score with a sample_weight, which needs several rows."""
    import math

    import numpy as np
    from sklearn.metrics import dcg_score, ndcg_score

    cases = []
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        for fixture in _ranking_weighted_fixtures():
            true = np.array(fixture["true"])
            score = np.array(fixture["score"])
            weight = np.array(fixture["weight"])
            cases.append({
                "name": fixture["name"],
                "y_true": [v for row in fixture["true"] for v in row],
                "y_score": [v for row in fixture["score"] for v in row],
                "label_count": true.shape[1],
                "sample_weight": fixture["weight"],
                "dcg": float(dcg_score(true, score)),
                "dcg_weighted": float(dcg_score(true, score, sample_weight=weight)),
                "dcg_weighted_log_e": float(
                    dcg_score(true, score, log_base=math.e, sample_weight=weight)),
                "dcg_weighted_at_2": float(dcg_score(true, score, k=2, sample_weight=weight)),
                "ndcg": float(ndcg_score(true, score)),
                "ndcg_weighted": float(ndcg_score(true, score, sample_weight=weight)),
                "ndcg_weighted_at_2": float(ndcg_score(true, score, k=2, sample_weight=weight)),
                "ndcg_weighted_ignore_ties": float(
                    ndcg_score(true, score, ignore_ties=True, sample_weight=weight)),
            })

    return {
        "metadata": {
            "algorithm": "RankingWeighted",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.dcg_score(sample_weight=...)",
                "sklearn.metrics.ndcg_score(sample_weight=...)",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_top_k_accuracy() -> dict:
    import numpy as np
    from sklearn.metrics import top_k_accuracy_score

    cases = []
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        for fixture in _top_k_fixtures():
            true = np.array(fixture["true"])
            score = np.array(fixture["score"])
            classes = score.shape[1]
            entry = {
                "name": fixture["name"],
                "y_true": fixture["true"],
                "y_score": [value for row in fixture["score"] for value in row],
                "class_count": classes,
            }
            weight = np.array(fixture["weight"])
            entry["sample_weight"] = fixture["weight"]
            for k in (1, 2, classes):
                entry[f"top_{k}"] = float(top_k_accuracy_score(true, score, k=k))
                entry[f"top_{k}_count"] = float(
                    top_k_accuracy_score(true, score, k=k, normalize=False))
                entry[f"top_{k}_weighted"] = float(
                    top_k_accuracy_score(true, score, k=k, sample_weight=weight))
                # normalize=False sums the weights of the hits rather than counting
                # them, and never divides -- so it alone survives a zero-sum vector.
                entry[f"top_{k}_weighted_count"] = float(
                    top_k_accuracy_score(true, score, k=k, normalize=False,
                                         sample_weight=weight))
            cases.append(entry)

    return {
        "metadata": {
            "algorithm": "TopKAccuracy",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.top_k_accuracy_score",
                "sklearn.metrics.top_k_accuracy_score(normalize=False)",
                "sklearn.metrics.top_k_accuracy_score(sample_weight=...)",
                "sklearn.metrics.top_k_accuracy_score(normalize=False, sample_weight=...)",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }


# --- Silhouette (issue #172, second lot) --------------------------------------


def _silhouette_fixtures() -> list[dict]:
    """Feature matrices whose clustering is the thing under test.

    Two well-separated blobs, two that overlap, a cluster holding one sample, and
    a one-dimensional case -- the shapes whose per-sample values differ in kind
    rather than in scale.
    """
    return [
        {
            "name": "two separated blobs",
            "features": [[0.0, 0.0], [0.1, 0.1], [5.0, 5.0], [5.1, 5.2], [5.0, 4.9]],
            "labels": [0, 0, 1, 1, 1],
        },
        {
            "name": "two overlapping blobs",
            "features": [[0.0, 0.0], [1.0, 0.5], [1.2, 0.4], [2.0, 1.0], [0.4, 1.4]],
            "labels": [0, 0, 1, 1, 0],
        },
        {
            "name": "a singleton cluster",
            "features": [[0.0, 0.0], [0.1, 0.1], [5.0, 5.0], [5.1, 5.2], [9.0, 9.0]],
            "labels": [0, 0, 1, 1, 2],
        },
        {
            "name": "one dimension",
            "features": [[0.0], [1.0], [10.0], [11.0], [12.0]],
            "labels": [0, 0, 1, 1, 1],
        },
        {
            "name": "three clusters",
            "features": [[0.0, 0.0], [0.2, 0.1], [4.0, 4.0], [4.2, 3.9], [8.0, 0.0], [8.1, 0.2]],
            "labels": [0, 0, 1, 1, 2, 2],
        },
        {
            "name": "coincident samples",
            "features": [[1.0, 1.0], [1.0, 1.0], [1.0, 1.0], [1.0, 1.0]],
            "labels": [0, 0, 1, 1],
        },
        {
            "name": "a misplaced sample",
            "features": [[0.0, 0.0], [0.2, 0.1], [4.0, 4.0], [4.2, 3.9], [0.1, 0.3]],
            "labels": [0, 0, 1, 1, 1],
        },
    ]


def generate_silhouette() -> dict:
    import numpy as np
    from sklearn.metrics import pairwise_distances, silhouette_samples, silhouette_score

    cases = []
    for fixture in _silhouette_fixtures():
        features = np.array(fixture["features"], dtype=float)
        labels = fixture["labels"]
        distances = pairwise_distances(features, metric="euclidean")
        cases.append({
            "name": fixture["name"],
            "features": [value for row in fixture["features"] for value in row],
            "feature_count": features.shape[1],
            "labels": labels,
            "distances": [float(value) for row in distances for value in row],
            # random_state is inert without sample_size, and named anyway: S6709 asks
            # every caller of a seeded API to say which seed, and silence is not an answer.
            "score": float(silhouette_score(features, labels, random_state=0)),
            "score_precomputed": float(
                silhouette_score(distances, labels, metric="precomputed", random_state=0)),
            "per_sample": [float(value) for value in silhouette_samples(features, labels)],
        })

    return {
        "metadata": {
            "algorithm": "Silhouette",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.silhouette_score",
                "sklearn.metrics.silhouette_score(metric='precomputed')",
                "sklearn.metrics.silhouette_samples",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }


# --- ROC-AUC (issue #61) ------------------------------------------------------


def _softmax(row: list[float]) -> list[float]:
    top = max(row)
    exps = [math.exp(v - top) for v in row]
    total = sum(exps)
    return [v / total for v in exps]


def _roc_fixtures() -> list[dict]:
    rng = SeededRandom(METRIC_SEED + 1)
    fixtures: list[dict] = []

    def weights(n: int) -> list[float]:
        return [round(rng.uniform(0.1, 3.0), 3) for _ in range(n)]

    def informative(truth: list[int]) -> list[float]:
        # Overlapping but separable: an AUC around 0.8 rather than 0.5 or 1.0.
        return [round(rng.random() * 0.6 + 0.4 * t, 12) for t in truth]

    balanced = [rng.randint(0, 1) for _ in range(300)]
    fixtures.append({"name": "binary_balanced", "kind": "binary", "y_true": balanced,
                     "scores": informative(balanced), "class_count": 2,
                     "sample_weight": weights(len(balanced))})

    imbalanced = [0] * 280 + [1] * 20
    fixtures.append({"name": "binary_imbalanced", "kind": "binary", "y_true": imbalanced,
                     "scores": informative(imbalanced), "class_count": 2,
                     "sample_weight": weights(len(imbalanced))})

    tied = [rng.randint(0, 1) for _ in range(200)]
    fixtures.append({"name": "binary_heavy_ties", "kind": "binary", "y_true": tied,
                     # One decimal: many samples share a score, which is where a
                     # rank-based shortcut and a real ROC curve part company.
                     "scores": [round(v, 1) for v in informative(tied)], "class_count": 2,
                     "sample_weight": weights(len(tied))})

    for k, size in ((3, 240), (5, 400)):
        truth = [rng.randint(0, k - 1) for _ in range(size)]
        rows = []
        for t in truth:
            logits = [rng.gauss(0.0, 1.0) for _ in range(k)]
            logits[t] += 1.5
            rows.append([round(v, 12) for v in _softmax(logits)])
        fixtures.append({"name": f"multiclass_{k}", "kind": "multiclass", "y_true": truth,
                         "scores": rows, "class_count": k,
                         "sample_weight": weights(size)})

    return fixtures


def _roc_case(fx: dict, weighted: bool) -> dict:
    sw = fx["sample_weight"] if weighted else None
    y_true = fx["y_true"]
    case = {
        "fixture": fx["name"],
        "kind": fx["kind"],
        "weighted": weighted,
        "y_true": y_true,
        "scores": fx["scores"],
        "class_count": fx["class_count"],
        "sample_weight": sw,
        "values": {},
    }
    if fx["kind"] == "binary":
        case["values"]["binary"] = stable(
            skm.roc_auc_score(y_true, fx["scores"], sample_weight=sw))
        return case

    scores = np.array(fx["scores"], dtype=float)
    classes = list(range(fx["class_count"]))
    for strategy in ("ovr", "ovo"):
        # scikit-learn refuses sample_weight for one-vs-one, and so do we.
        if strategy == "ovo" and weighted:
            continue
        for average in ("macro", "weighted"):
            case["values"][f"{strategy}|{average}"] = stable(skm.roc_auc_score(
                y_true, scores, multi_class=strategy, average=average,
                labels=classes, sample_weight=sw))
    return case


def generate_roc_auc() -> dict:
    cases = [
        _roc_case(fx, weighted)
        for fx in _roc_fixtures()
        for weighted in (False, True)
    ]
    return {
        "metadata": {
            "algorithm": "RocAuc",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": ["sklearn.metrics.roc_auc_score"],
            "count": len(cases),
        },
        "cases": cases,
    }


# --- BPE and byte-level BPE tokenizers (issue #59) ---------------------------

GPT2_VOCAB = "gpt2_vocab.json"
GPT2_MERGES = "gpt2_merges.txt"


def _gpt2_tokenizer(pattern: str | None = None, add_prefix_space: bool = False):
    """GPT-2's byte-level BPE, optionally with another model's split pattern.

    `pattern=None` is stock GPT-2: `ByteLevel` does its own splitting. A pattern
    reproduces the Llama-3 / Qwen2 shape, where a `Split` runs first and
    `ByteLevel` is reduced to the byte mapping.

    `add_prefix_space` is HuggingFace's own `ByteLevel` default (True), left off
    here because every corpus that predates issue #59's final review was
    generated without it; `generate_bpe_added_tokens` is the one that turns it on.
    """
    from tokenizers import Regex, Tokenizer  # noqa: PLC0415
    from tokenizers.decoders import ByteLevel as ByteLevelDecoder  # noqa: PLC0415
    from tokenizers.models import BPE  # noqa: PLC0415
    from tokenizers.pre_tokenizers import ByteLevel, Sequence, Split  # noqa: PLC0415

    tokenizer = Tokenizer(BPE.from_file(
        str(ORACLE_DIR / GPT2_VOCAB), str(ORACLE_DIR / GPT2_MERGES)))
    if pattern is None:
        tokenizer.pre_tokenizer = ByteLevel(add_prefix_space=add_prefix_space)
    else:
        tokenizer.pre_tokenizer = Sequence([
            Split(Regex(pattern), behavior="isolated"),
            ByteLevel(add_prefix_space=add_prefix_space, use_regex=False),
        ])
    tokenizer.decoder = ByteLevelDecoder()
    return tokenizer


def generate_bytelevel_bpe() -> dict:
    from tokenizers.pre_tokenizers import ByteLevel  # noqa: PLC0415

    tokenizer = _gpt2_tokenizer()
    # The published bytes_to_unicode construction: the three printable ranges map
    # to themselves, and the 68 bytes left over take 256, 257, ... in byte order.
    printable = (list(range(0x21, 0x7F)) + list(range(0xA1, 0xAD)) + list(range(0xAE, 0x100)))
    table = [None] * 256
    for byte in printable:
        table[byte] = chr(byte)
    spare = 0
    for byte in range(256):
        if table[byte] is None:
            table[byte] = chr(256 + spare)
            spare += 1
    # Coerced to sets: ByteLevel.alphabet() returns a list here, so a bare
    # set(table) == ByteLevel.alphabet() is False regardless of contents.
    assert set(table) == set(ByteLevel.alphabet()), "derived alphabet disagrees with tokenizers"

    # ignore_merges: emits a piece that is itself a vocab entry whole, rather
    # than merged up to. Set on the deserialized JSON -- the flag lives on the model.
    import json as _json  # noqa: PLC0415
    from tokenizers import Tokenizer as _Tokenizer  # noqa: PLC0415

    spec = _json.loads(tokenizer.to_str())
    spec["model"]["ignore_merges"] = True
    ignoring = _Tokenizer.from_str(_json.dumps(spec))

    cases = []
    for i, text in enumerate(BPE_TEXTS):
        enc = tokenizer.encode(text)
        enc_ignoring = ignoring.encode(text)
        cases.append({
            "id": i,
            "text": text,
            "tokens": enc.tokens,
            "ids": enc.ids,
            "decoded": tokenizer.decode(enc.ids, skip_special_tokens=False),
            "decoded_skip_specials": tokenizer.decode(enc.ids, skip_special_tokens=True),
            "tokens_ignore_merges": enc_ignoring.tokens,
            "ids_ignore_merges": enc_ignoring.ids,
        })
    return {
        "metadata": {
            "algorithm": "ByteLevelBPE",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "gpt2 (vendored by tools/fetch_gpt2_bpe.py)",
            "alphabet": table,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_bpe() -> dict:
    """Classic character-level BPE over the small self-trained model."""
    from tokenizers import Tokenizer  # noqa: PLC0415

    tokenizer = Tokenizer.from_file(str(ORACLE_DIR / "tiny_bpe.json"))
    cases = []
    for i, text in enumerate(BPE_TEXTS):
        enc = tokenizer.encode(text)
        cases.append({"id": i, "text": text, "tokens": enc.tokens, "ids": enc.ids})
    return {
        "metadata": {
            "algorithm": "BPE",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "tiny_bpe.json (self-trained, end_of_word_suffix </w>)",
            "count": len(cases),
        },
        "cases": cases,
    }


# GPT-2's own vocabulary cannot exercise ignore_merges; see generate_orphan_bpe's
# docstring for why this corpus exists instead.
ORPHAN_BPE_TEXTS = [
    "abc",       # the orphan itself: ['ab', 'c'] normally, ['abc'] with the flag
    "x abc y",   # the orphan as one piece among ordinary ones
    "x y",       # no piece here is the orphan
    "ab c",      # "ab" is a legitimately reachable entry, not the orphan
    "",
]


def generate_orphan_bpe() -> dict:
    """Classic BPE over a model with one vocabulary entry the merge table cannot reach.

    The vendored GPT-2 model cannot exercise ignore_merges: checked over all
    50 257 of its vocabulary entries, none diverges, because a natively-trained
    merge table always retraces to its own entries (see the ignore_merges
    task's amended plan for the argument in full). The flag only rescues
    *orphaned* entries -- present in a model's vocabulary but unreachable by
    replaying its merges -- which is what a tiktoken-to-tokenizer.json
    conversion produces and what training never does. orphan_bpe_model.json
    (tools/build_tiny_models.py) holds exactly one such entry, on purpose, so
    this corpus is the only one in the suite that can prove the flag does
    anything.
    """
    import json as _json  # noqa: PLC0415
    from tokenizers import Tokenizer  # noqa: PLC0415

    tokenizer = Tokenizer.from_file(str(ORACLE_DIR / "orphan_bpe_model.json"))
    spec = _json.loads(tokenizer.to_str())
    spec["model"]["ignore_merges"] = True
    ignoring = Tokenizer.from_str(_json.dumps(spec))

    cases = []
    for i, text in enumerate(ORPHAN_BPE_TEXTS):
        enc = tokenizer.encode(text)
        enc_ignoring = ignoring.encode(text)
        cases.append({
            "id": i,
            "text": text,
            "tokens": enc.tokens,
            "ids": enc.ids,
            "tokens_ignore_merges": enc_ignoring.tokens,
            "ids_ignore_merges": enc_ignoring.ids,
        })
    return {
        "metadata": {
            "algorithm": "BPE",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "orphan_bpe_model.json (hand-constructed, one orphaned entry)",
            "count": len(cases),
        },
        "cases": cases,
    }


# long-comment: kept at length because the material imposes it -- two mirror URLs and
# what they agree on are the evidence that stands in for a gated original nobody can open
# Transcribe each of these from the model's own tokenizer.json rather than from
# memory: they differ from GPT-2 in newline handling and in the case-insensitive
# contraction group, and from each other only in a quantifier on \p{N}.
#
# Provenance:
#   gpt2   - tests/oracles/gpt2_vocab.json / gpt2_merges.txt (vendored by
#            tools/fetch_gpt2_bpe.py); ByteLevel does its own splitting, so
#            this is the pattern GPT-2's own pre-tokenizer is equivalent to.
#   qwen2  - https://huggingface.co/Qwen/Qwen2-0.5B/resolve/main/tokenizer.json
#            (Apache-2.0, ungated), `pre_tokenizer.pretokenizers[0].pattern.Regex`.
#   llama3 - meta-llama/Meta-Llama-3-8B is gated and returns HTTP 401 without
#            an authorized token, so this was read from two independent
#            mirrors instead and cross-checked byte-for-byte:
#              https://huggingface.co/NousResearch/Meta-Llama-3-8B/resolve/main/tokenizer.json
#              https://huggingface.co/unsloth/llama-3-8b/resolve/main/tokenizer.json
#            Both carry the same `pre_tokenizer.pretokenizers[0].pattern.Regex`,
#            the same `model.ignore_merges = true`, and the same 128 000-entry
#            vocabulary, which is what stands in for reading the gated
#            original. It differs from qwen2 in exactly one place: `\p{N}{1,3}`
#            where qwen2 has `\p{N}`.
BPE_PATTERNS = {
    "gpt2": r"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
    "llama3": r"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+",
    "qwen2": r"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+",
}


def generate_bpe_pretokenize() -> dict:
    """Prove the split, not the vocabulary.

    The Llama-3 and Qwen2 rows of the parity table are claimed at the split
    level only (ADR 0017). Running their patterns over GPT-2's vocabulary is
    what proves the C# regex behaves as HuggingFace's does, without vendoring a
    second and third 150 000-entry vocabulary to prove a merge loop the GPT-2
    corpus already proves.
    """
    cases = []
    case_id = 0
    for name, pattern in BPE_PATTERNS.items():
        tokenizer = _gpt2_tokenizer(None if name == "gpt2" else pattern)
        for text in BPE_TEXTS:
            pieces = [piece for piece, _ in tokenizer.pre_tokenizer.pre_tokenize_str(text)]
            cases.append({"id": case_id, "pattern": name, "text": text, "pieces": pieces})
            case_id += 1
    return {
        "metadata": {
            "algorithm": "BPE pre-tokenization",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "patterns": BPE_PATTERNS,
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_bpe_tokenizer_json() -> dict:
    """The tokenizer.json shapes TokenizerJsonLoader.LoadBpe must read.

    Each case carries the file itself, so the C# side parses the exact bytes
    HuggingFace was handed rather than a second fixture that could drift.
    """
    from tokenizers import Tokenizer  # noqa: PLC0415

    text = "Hello, world! déjà 東京 👋"
    cases = []
    for i, (name, tokenizer) in enumerate([
        ("bytelevel", _gpt2_tokenizer()),
        ("split_sequence", _gpt2_tokenizer(BPE_PATTERNS["qwen2"])),
        ("classic", Tokenizer.from_file(str(ORACLE_DIR / "tiny_bpe.json"))),
    ]):
        enc = tokenizer.encode(text)
        cases.append({
            "id": i,
            "name": name,
            "tokenizer_json": tokenizer.to_str(),
            "text": text,
            "tokens": enc.tokens,
            "ids": enc.ids,
        })
    return {
        "metadata": {
            "algorithm": "BPE tokenizer.json",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "count": len(cases),
        },
        "cases": cases,
    }


# Combining sequence, singleton (U+212B -> U+00C5) and two compatibility characters
# only the K forms touch -- as \u escapes, so none arrives already normalized.
BPE_NORMALIZER_TEXTS = [
    "école",            # e + COMBINING ACUTE
    "école",             # the precomposed form of the same word
    "Ångstrom unit",     # ANGSTROM SIGN
    "ﬁve o￦clock",  # the fi ligature, and a fullwidth macron
    "① ② café",
    HELLO_WORLD,              # unchanged by every form: the control
]

# Code points chosen to expose a disagreement between .NET's Unicode tables
# and Rust's crate, if the four forms have one -- see the spec's D5.
UNICODE_FORM_PROBES = BPE_NORMALIZER_TEXTS + [
    "ẛ̣",   # LATIN SMALL LETTER LONG S WITH DOT ABOVE + DOT BELOW
    "İ",         # LATIN CAPITAL LETTER I WITH DOT ABOVE
    "Ω",         # OHM SIGN
    "̈́",         # COMBINING GREEK DIALYTIKA TONOS, a singleton decomposition
    "가한",   # Hangul syllables, algorithmic composition
    "豈",         # a CJK compatibility ideograph
    "ᾂ",         # a Greek letter with three stacked marks
    "ﷺ",         # ARABIC LIGATURE SALLALLAHOU..., expands to 18 characters
]


def generate_unicode_forms() -> dict:
    """What tokenizers' four normalization forms produce, character for character.

    The C# side asserts String.Normalize gives the same answer. Nothing in this
    corpus involves BPE: it isolates the one question the tokenizer corpus cannot
    answer, which is whether the two runtimes' Unicode tables agree at all.
    """
    from tokenizers import normalizers  # noqa: PLC0415

    forms = [("NFC", normalizers.NFC()), ("NFKC", normalizers.NFKC()),
             ("NFD", normalizers.NFD()), ("NFKD", normalizers.NFKD())]
    cases = []
    for text in UNICODE_FORM_PROBES:
        for name, normalizer in forms:
            cases.append({
                "id": len(cases),
                "form": name,
                "text": text,
                "normalized": normalizer.normalize_str(text),
            })
    return {
        "metadata": {
            "algorithm": "Unicode normalization forms",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "count": len(cases),
        },
        "cases": cases,
    }


def _small_bytelevel_bpe_tokenizer(add_prefix_space: bool = False):
    """A byte-level BPE with full coverage and no merges.

    Every one of the 256 alphabet characters is its own token, so any input
    encodes with no unknown id -- all this corpus needs, since its subject is
    the normalizer, not the size of the vocabulary the merges reach. Reuses the
    bytes_to_unicode-derived alphabet generate_bytelevel_bpe already proves
    against ByteLevel.alphabet(), rather than deriving it a second time.
    """
    from tokenizers import Tokenizer, decoders, models, pre_tokenizers  # noqa: PLC0415

    vocab = {c: i for i, c in enumerate(sorted(pre_tokenizers.ByteLevel.alphabet()))}
    tokenizer = Tokenizer(models.BPE(vocab, []))
    tokenizer.pre_tokenizer = pre_tokenizers.ByteLevel(add_prefix_space=add_prefix_space)
    tokenizer.decoder = decoders.ByteLevel()
    return tokenizer


def generate_bpe_normalizer() -> dict:
    """A BPE pipeline with a normalizer, which LoadBpe refused wholesale.

    Ten pipelines. Seven cover D1-D4 of the spec: one per form, a Sequence of
    two, an empty Sequence -- the deepseek shape, which does nothing and was
    refused for nothing -- and a normalizer beside both halves of the added
    token table, the gpt-neox shape (23 of its 25 entries are normalized:
    true). Three more, added by the branch review of #121: two where a raw
    and a normalized added token compete for the same span (finding 1), and
    one measuring add_prefix_space against a normalizer for the first time
    (finding 2) -- see the comments at each for what they pin down.

    Only "nfc" -- the form four of the five surveyed models actually declare --
    runs on real GPT-2, so the corpus still proves the case that exists in the
    wild against a real 50 257-entry vocabulary. Every other pipeline runs on
    _small_bytelevel_bpe_tokenizer(): each case already carries its own whole
    tokenizer.json (so the C# side parses the exact bytes HuggingFace was
    handed), and ten copies of GPT-2's 1.8 MB model would have made this
    corpus over 100 MB for a question that has nothing to do with vocabulary
    size.
    """
    from tokenizers import AddedToken, normalizers  # noqa: PLC0415

    pipelines = [
        ("nfc", normalizers.NFC(), False, True),
        ("nfkc", normalizers.NFKC(), False, False),
        ("nfd", normalizers.NFD(), False, False),
        ("nfkd", normalizers.NFKD(), False, False),
        ("sequence", normalizers.Sequence([normalizers.NFD(), normalizers.NFC()]), False, False),
        ("empty_sequence", normalizers.Sequence([]), False, False),
        ("added_tokens", normalizers.NFC(), True, False),
    ]

    cases = []
    for i, (name, normalizer, with_added, real_gpt2) in enumerate(pipelines):
        tokenizer = _gpt2_tokenizer() if real_gpt2 else _small_bytelevel_bpe_tokenizer()
        tokenizer.normalizer = normalizer
        if with_added:
            # One of each half. The normalized entry is written decomposed, so it
            # can only match once its own content has been normalized too.
            tokenizer.add_tokens([AddedToken("café", normalized=True)])
            tokenizer.add_special_tokens([AddedToken(END_OF_TEXT, special=True, normalized=False)])
        texts = BPE_NORMALIZER_TEXTS + (["a café<|endoftext|>b", "café tail"] if with_added else [])
        text_cases = []
        for text in texts:
            enc = tokenizer.encode(text)
            text_cases.append({
                "text": text,
                "tokens": enc.tokens,
                "ids": enc.ids,
                "decoded": tokenizer.decode(enc.ids, skip_special_tokens=False),
            })
        cases.append({
            "id": i,
            "name": name,
            "tokenizer_json": tokenizer.to_str(),
            "texts": text_cases,
        })

    # Branch review of #121, finding 1: a raw entry now beats an earlier or
    # longer normalized one for the same span, HuggingFace's own precedence -- see spec D2.
    precedence_pipelines = [
        # "ab" (normalized) starts earlier but loses: the raw pass claims "b"
        # first, leaving no gap that still contains "ab".
        ("precedence_raw_beats_normalized", [("ab", True), ("b", False)], ["xaby"]),
        # "abc" (normalized, earlier and longer) still loses: the raw pass
        # claims "cy" first, removing the 'c' it needed.
        ("precedence_raw_beats_earlier_longer_normalized", [("abc", True), ("cy", False)], ["abcy"]),
    ]
    for name, tokens, texts in precedence_pipelines:
        tokenizer = _small_bytelevel_bpe_tokenizer()
        for content, normalized in tokens:
            tokenizer.add_tokens([AddedToken(content, normalized=normalized)])
        text_cases = []
        for text in texts:
            enc = tokenizer.encode(text)
            text_cases.append({
                "text": text,
                "tokens": enc.tokens,
                "ids": enc.ids,
                "decoded": tokenizer.decode(enc.ids, skip_special_tokens=False),
            })
        cases.append({
            "id": len(cases),
            "name": name,
            "tokenizer_json": tokenizer.to_str(),
            "texts": text_cases,
        })

    # Branch review of #121, finding 2: normalization runs before
    # add_prefix_space is decided -- U+3000 only becomes a literal space post-NFKC; see spec D2.
    prefix_space_tokenizer = _small_bytelevel_bpe_tokenizer(add_prefix_space=True)
    prefix_space_tokenizer.normalizer = normalizers.NFKC()
    prefix_space_texts = [
        "　café",  # IDEOGRAPHIC SPACE + café: a leading space only after NFKC
        " café",       # control: already begins with a literal space
    ]
    prefix_space_text_cases = []
    for text in prefix_space_texts:
        enc = prefix_space_tokenizer.encode(text)
        prefix_space_text_cases.append({
            "text": text,
            "tokens": enc.tokens,
            "ids": enc.ids,
            "decoded": prefix_space_tokenizer.decode(enc.ids, skip_special_tokens=False),
        })
    cases.append({
        "id": len(cases),
        "name": "add_prefix_space_after_normalize",
        "tokenizer_json": prefix_space_tokenizer.to_str(),
        "texts": prefix_space_text_cases,
    })

    return {
        "metadata": {
            "algorithm": "BPE normalizer",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "count": len(cases),
        },
        "cases": cases,
    }


# Two CJK texts, an emoji sequence and two controls: a byte-level token is a
# fragment of a multi-byte character far more often than not.
BYTELEVEL_STREAM_TEXTS = [
    "東京 \U0001f44b",          # 東京 + waving hand
    "日本語のテキスト",  # a Japanese sentence
    "\U0001f1eb\U0001f1f7 emoji",       # a regional-indicator pair
    "déjà vu",                # Latin-1: no fragment, the control
    HELLO_WORLD,                        # ASCII: the other control
]


def generate_bytelevel_decode_stream() -> dict:
    """Each id of a text decoded on its own, which is how a stream is consumed.

    tokenizers substitutes U+FFFD for a byte sequence that is not well-formed
    UTF-8; Lodestar threw until issue #149. The `replacement_count` per case is
    carried so a corpus that stopped exercising the substitution would be noticed
    rather than pass silently.
    """
    tokenizer = _gpt2_tokenizer()

    cases = []
    for text in BYTELEVEL_STREAM_TEXTS:
        enc = tokenizer.encode(text)
        per_id = [tokenizer.decode([i]) for i in enc.ids]
        cases.append({
            "id": len(cases),
            "text": text,
            "ids": enc.ids,
            "tokens": enc.tokens,
            "per_id_decoded": per_id,
            "replacement_count": sum(1 for s in per_id if "�" in s),
            "decoded": tokenizer.decode(enc.ids),
        })
    return {
        "metadata": {
            "algorithm": "byte-level decode, one id at a time",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "count": len(cases),
        },
        "cases": cases,
    }


# The two settings the rest of the BPE corpus structurally cannot see, exercised
# together because they interact; see generate_bpe_added_tokens's docstring.
BPE_ADDED_TOKEN_TEXTS = BPE_TEXTS + [
    "hi<|endoftext|>bye",             # a segment after an added token, no space of its own
    END_OF_TEXT,                      # nothing but the token
    "<|endoftext|>tail",              # an empty segment before it
    " <|endoftext|> ",                # segments that already start with a space
    "x<|endoftext|> y<|endoftext|>z",  # several, mixed
]


def generate_bpe_added_tokens() -> dict:
    """GPT-2 with its own <|endoftext|> registered, and add_prefix_space on.

    The corpus carries the whole `tokenizer.json` once, in the metadata: the C#
    side parses the exact bytes HuggingFace was handed, as
    `generate_bpe_tokenizer_json` does, rather than a second fixture that could
    drift from it.

    BPE_ADDED_TOKEN_TEXTS exercises two settings the rest of the BPE corpus
    structurally cannot see, together because they interact:

    1. `<|endoftext|>` is id 50256 in GPT-2's own model.vocab and is also
       listed in added_tokens. Every other BPE fixture here either registers
       no added token at all (`BPE.from_file` does not) or never names one in
       its text, so a loader that reads added_tokens as "the entries
       model.vocab lacks" passes the whole corpus while dropping every
       special token there is.
    2. `add_prefix_space` is HuggingFace's ByteLevel default and nothing on
       this branch had ever generated a corpus with it on. It applies per
       added-token-delimited segment, not once to the whole input, and only
       when the segment does not already start with a space -- observable
       only when a text starts with a space, or when an added token sits
       between two segments, hence the extra texts below.
    """
    from tokenizers import AddedToken  # noqa: PLC0415

    tokenizer = _gpt2_tokenizer(add_prefix_space=True)
    tokenizer.add_special_tokens([AddedToken(END_OF_TEXT, special=True)])

    cases = []
    for i, text in enumerate(BPE_ADDED_TOKEN_TEXTS):
        enc = tokenizer.encode(text)
        cases.append({
            "id": i,
            "text": text,
            "tokens": enc.tokens,
            "ids": enc.ids,
            "decoded": tokenizer.decode(enc.ids, skip_special_tokens=False),
            "decoded_skip_specials": tokenizer.decode(enc.ids, skip_special_tokens=True),
        })
    return {
        "metadata": {
            "algorithm": "ByteLevelBPE with added tokens and add_prefix_space",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "gpt2 (vendored by tools/fetch_gpt2_bpe.py), <|endoftext|> added as special",
            "tokenizer_json": tokenizer.to_str(),
            "count": len(cases),
        },
        "cases": cases,
    }


# --- Regression metrics (issue #92) ------------------------------------------

REGRESSION_SEED = METRIC_SEED + 2


def _regression_fixtures() -> list[dict]:
    """Fixtures chosen so that each degenerate branch has one case of its own.

    The ordinary ones exist to prove the arithmetic; the degenerate ones exist
    because every implementation agrees on the ordinary ones.

    uniform_fractional_weights carries a uniform *fractional* weight, which is
    where the weighted percentile's tolerance shows and nowhere else. 0.1 is
    not representable in binary, so the cumulative sum overshoots half the
    total by units in the last place; scikit-learn averages anyway (its test
    is `fraction_above > eps`, not `> 0`) and returns 4.5 on these residuals,
    where an exact test returns 4.0. Every other weighted fixture here is
    exactly representable -- 1, 2, 3, 7 -- so none of them can tell the two
    rules apart, and the residuals are distinct so the two averaged order
    statistics differ.
    """
    rng = SeededRandom(REGRESSION_SEED)

    def weights(n: int) -> list[float]:
        return [round(rng.uniform(0.1, 3.0), 3) for _ in range(n)]

    def noisy(truth: list[float], spread: float) -> list[float]:
        return [round(t + rng.gauss(0.0, spread), 12) for t in truth]

    fixtures: list[dict] = []

    # Ordinary single output, positive targets: the only shape where the log
    # family is defined, so it carries msle/rmsle as well as everything else.
    positive = [round(rng.uniform(0.5, 40.0), 12) for _ in range(200)]
    fixtures.append({"name": "positive_single", "output_count": 1,
                     "y_true": positive, "y_pred": noisy(positive, 2.0),
                     "sample_weight": weights(len(positive))})

    # Targets straddling zero: mape's clamp never fires (no exact zero) but the
    # values get small, and msle is undefined, so this case proves the split.
    signed = [round(rng.uniform(-20.0, 20.0), 12) for _ in range(200)]
    fixtures.append({"name": "signed_single", "output_count": 1,
                     "y_true": signed, "y_pred": noisy(signed, 3.0),
                     "sample_weight": weights(len(signed))})

    # Three outputs, deliberately unequal in variance so variance_weighted and
    # uniform_average cannot coincide.
    rows_true: list[float] = []
    rows_pred: list[float] = []
    for _ in range(150):
        base = [round(rng.uniform(0.5, 5.0), 12),
                round(rng.uniform(0.5, 60.0), 12),
                round(rng.uniform(0.5, 400.0), 12)]
        rows_true.extend(base)
        rows_pred.extend(round(b + rng.gauss(0.0, 0.05 * b), 12) for b in base)
    fixtures.append({"name": "positive_three_outputs", "output_count": 3,
                     "y_true": rows_true, "y_pred": rows_pred,
                     "sample_weight": weights(150)})

    # --- the degenerate cases, one fixture each ---

    # A truth with zero variance: force_finite decides, zeroDivision does not.
    fixtures.append({"name": "constant_truth_perfect", "output_count": 1,
                     "y_true": [2.0, 2.0, 2.0], "y_pred": [2.0, 2.0, 2.0],
                     "sample_weight": [1.0, 2.0, 3.0]})
    fixtures.append({"name": "constant_truth_imperfect", "output_count": 1,
                     "y_true": [2.0, 2.0, 2.0], "y_pred": [1.0, 2.0, 3.0],
                     "sample_weight": [1.0, 2.0, 3.0]})

    # One sample: r2 is nan under either force_finite, which is zeroDivision's
    # territory and nothing else's.
    fixtures.append({"name": "single_sample", "output_count": 1,
                     "y_true": [3.0], "y_pred": [5.0], "sample_weight": [2.0]})

    # An exact zero in the truth: mape's epsilon clamp, and nothing else's.
    fixtures.append({"name": "zero_in_truth", "output_count": 1,
                     "y_true": [0.0, 4.0, -2.0], "y_pred": [1.0, 5.0, -1.0],
                     "sample_weight": [1.0, 1.0, 2.0]})

    # An even sample count with a lopsided weight, where the averaged weighted
    # percentile and a plain median part company.
    fixtures.append({"name": "lopsided_weights", "output_count": 1,
                     "y_true": [0.0, 2.0, 4.0, 10.0], "y_pred": [0.0, 0.0, 0.0, 0.0],
                     "sample_weight": [1.0, 1.0, 1.0, 7.0]})

    # See this function's docstring for why 0.1 needs a fractional weight.
    fixtures.append({"name": "uniform_fractional_weights", "output_count": 1,
                     "y_true": [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0],
                     "y_pred": [1.0] * 10,
                     "sample_weight": [0.1] * 10})

    return fixtures


def _regression_call(fn, y_true, y_pred, sw, **fixed):
    """Binds one scikit-learn metric to this case, leaving `multioutput` free.

    `multioutput=None` means "do not pass it", which is what uniform_average is
    on every one of these functions. `fixed` carries whatever else the metric
    takes for the whole case — `alpha` for the pinball loss, `force_finite` for
    R2 and explained variance — so that one binder serves all three families.
    """
    def call(mo):
        kw = dict(fixed)
        if mo is not None:
            kw["multioutput"] = mo
        return fn(y_true, y_pred, sample_weight=sw, **kw)
    return call


def _regression_emit(values: dict, key: str, call, mo, is_vector: bool) -> None:
    result = call(mo)
    if is_vector:
        # raw_values on a 1-D input returns a 0-d array in some paths and a
        # 1-element one in others, so it is widened before being walked.
        values[key] = [_finite_or_name(float(x)) for x in np.atleast_1d(result)]
    else:
        values[key] = _finite_or_name(float(result))


def _regression_shapes(values: dict, key: str, call, ow,
                       suffix: str = "", variance_weighted: bool = False) -> None:
    """Records one metric under every multioutput shape scikit-learn allows it.

    Writing the four shapes here rather than four times per family is what keeps
    `_regression_case` under S3776's limit: each copy carried its own
    `if ow is not None` guard, and four guarded copies in four loops is most of
    what the rule was counting.

    `suffix` exists because R2 and explained variance key on `metric|shape|flag`
    while everything else keys on `metric|shape`, and the flag trails the shape.
    """
    shapes = [("uniform", None, False), ("raw", "raw_values", True)]
    if variance_weighted:
        shapes.append(("variance_weighted", "variance_weighted", False))
    if ow is not None:
        shapes.append(("weights", ow, False))

    for shape, mo, is_vector in shapes:
        _regression_emit(values, f"{key}|{shape}{suffix}", call, mo, is_vector)


_REGRESSION_PLAIN = {
    "mse": skm.mean_squared_error,
    "rmse": skm.root_mean_squared_error,
    "mae": skm.mean_absolute_error,
    "median_ae": skm.median_absolute_error,
    "mape": skm.mean_absolute_percentage_error,
}

# Defined only where every target is above -1, which is why they are their own
# list rather than more entries above.
_REGRESSION_LOG = (
    ("msle", skm.mean_squared_log_error),
    ("rmsle", skm.root_mean_squared_log_error),
)

# The two that take force_finite, and the only two scikit-learn accepts
# variance_weighted for.
_REGRESSION_SCORED = (
    ("r2", skm.r2_score),
    ("ev", skm.explained_variance_score),
)


def _regression_arrays(fx: dict, k: int):
    """The fixture's flat lists as scikit-learn wants to see them.

    Single-output targets are handed over 1-D rather than as an n x 1 matrix,
    because that is the shape a caller writes and the shape `max_error` accepts.
    """
    n = len(fx["y_true"]) // k
    y_true = np.asarray(fx["y_true"], dtype=float).reshape(n, k)
    y_pred = np.asarray(fx["y_pred"], dtype=float).reshape(n, k)
    return (y_true.ravel(), y_pred.ravel()) if k == 1 else (y_true, y_pred)


def _regression_log_defined(y_true, y_pred) -> bool:
    """Whether the log family is defined here: every target strictly above -1."""
    return float(np.min(y_true)) > -1.0 and float(np.min(y_pred)) > -1.0


def _regression_case(fx: dict, weighted: bool) -> dict:
    sw = fx["sample_weight"] if weighted else None
    k = fx["output_count"]
    y_true, y_pred = _regression_arrays(fx, k)

    values: dict = {}

    # Output weights are fixed rather than drawn, so that a reader can check the
    # reduction by hand: 0.3/0.7 on two outputs, 0.2/0.3/0.5 on three.
    ow = {1: None, 2: [0.3, 0.7], 3: [0.2, 0.3, 0.5]}[k]

    for name, fn in _REGRESSION_PLAIN.items():
        _regression_shapes(values, name, _regression_call(fn, y_true, y_pred, sw), ow)

    if _regression_log_defined(y_true, y_pred):
        for name, fn in _REGRESSION_LOG:
            _regression_shapes(values, name, _regression_call(fn, y_true, y_pred, sw), ow)

    # max_error takes neither sample_weight nor multioutput, and refuses 2-D
    # input outright with "Multioutput not supported in max_error".
    if k == 1 and not weighted:
        values["max_error|uniform"] = _finite_or_name(float(skm.max_error(y_true, y_pred)))

    for alpha in (0.5, 0.9):
        _regression_shapes(
            values, f"pinball{alpha}",
            _regression_call(skm.mean_pinball_loss, y_true, y_pred, sw, alpha=alpha), ow)

    for name, fn in _REGRESSION_SCORED:
        for ff in (True, False):
            _regression_shapes(
                values, name,
                _regression_call(fn, y_true, y_pred, sw, force_finite=ff), ow,
                suffix="|force_finite" if ff else "|raw_infinity",
                variance_weighted=True)

    return {
        "fixture": fx["name"],
        "weighted": weighted,
        "output_count": k,
        "y_true": fx["y_true"],
        "y_pred": fx["y_pred"],
        "sample_weight": sw,
        "values": values,
    }


def generate_regression() -> dict:
    with warnings.catch_warnings():
        # scikit-learn warns on every undefined metric; the corpus records the
        # value it returns, which is the thing under test.
        warnings.simplefilter("ignore")
        cases = [
            _regression_case(fx, weighted)
            for fx in _regression_fixtures()
            for weighted in (False, True)
        ]
    return {
        "metadata": {
            "algorithm": "Regression",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.mean_squared_error",
                "sklearn.metrics.root_mean_squared_error",
                "sklearn.metrics.mean_absolute_error",
                "sklearn.metrics.median_absolute_error",
                "sklearn.metrics.mean_absolute_percentage_error",
                "sklearn.metrics.mean_squared_log_error",
                "sklearn.metrics.root_mean_squared_log_error",
                "sklearn.metrics.max_error",
                "sklearn.metrics.r2_score",
                "sklearn.metrics.explained_variance_score",
                "sklearn.metrics.mean_pinball_loss",
            ],
            "count": len(cases),
        },
        "cases": cases,
    }


# --- The conditioning the ordinary regression corpus cannot reach (issue #127) ---
# See generate_regression_conditioning's docstring.

CONDITIONING_SAMPLES = 200_000
CONDITIONING_OFFSET = 1e9
CONDITIONING_SPREAD = 1e-2
# 1e-6, not the ramp's own 5e-8 step, so quantizing it carries the ill
# conditioning; see generate_regression_conditioning's docstring for the ULP math.
CONDITIONING_PERTURBATION = 1e-6
PROBE_INDICES = [0, 1, CONDITIONING_SAMPLES // 2, CONDITIONING_SAMPLES - 2, CONDITIONING_SAMPLES - 1]


def _conditioning_arrays() -> tuple[list[float], list[float]]:
    """The closed form both sides build, and nothing but it."""
    step = CONDITIONING_SPREAD / CONDITIONING_SAMPLES
    y_true = [CONDITIONING_OFFSET + i * step for i in range(CONDITIONING_SAMPLES)]
    y_pred = [y_true[i] + ((i % 7) - 3) * CONDITIONING_PERTURBATION
              for i in range(CONDITIONING_SAMPLES)]
    return y_true, y_pred


def _bits(value: float) -> str:
    """The double's raw IEEE-754 bits, so a probe compares the number and not its spelling."""
    import struct  # noqa: PLC0415

    return f"{struct.unpack('<Q', struct.pack('<d', value))[0]:016x}"


def generate_regression_conditioning() -> dict:
    """scikit-learn's answers on a target no committed array could carry.

    regression.json stores its arrays in full and caps at 450 values, over
    targets in [0.5, 40] -- a range where a sequential sum and numpy's pairwise
    one agree to far more digits than the corpus compares at. The defect #127
    fixes needs the opposite: many samples, and a large offset over a small
    spread, so that the low-order bits of every term fall off the end of the
    accumulator.

    Storing 200 000 samples as JSON would be megabytes, so this case carries
    the closed form instead. The C# side rebuilds the same arrays from the
    same expression, in the same order -- both languages evaluate IEEE-754
    doubles, so the two constructions are identical value for value.
    PROBE_INDICES is how that stops being a matter of faith: the raw bits at
    those positions are recorded and compared before anything is scored.

    CONDITIONING_PERTURBATION is 1e-6, not the ramp's own 5e-8 step: the ULP at
    1e9 is 2^-23, about 1.19e-7 (checked with math.ulp(1e9)), so a
    perturbation below half of that rounds straight back onto the target.
    Measured: with 1e-8 every residual is exactly zero, mse is 0 and r2 is 1,
    and a fixture built that way passes while proving nothing. The ramp's step
    stays below the ULP on purpose -- quantizing it is the ill conditioning
    this case exists to carry.
    """
    y_true, y_pred = _conditioning_arrays()
    yt = np.asarray(y_true)
    yp = np.asarray(y_pred)

    values = {
        "r2": stable(float(skm.r2_score(yt, yp))),
        "explained_variance": stable(float(skm.explained_variance_score(yt, yp))),
        "mse": stable(float(skm.mean_squared_error(yt, yp))),
        "mae": stable(float(skm.mean_absolute_error(yt, yp))),
    }
    return {
        "metadata": {
            "algorithm": "Regression under ill conditioning",
            "library": "scikit-learn",
            "library_version": version("scikit-learn"),
            "reference_calls": [
                "sklearn.metrics.r2_score",
                "sklearn.metrics.explained_variance_score",
                "sklearn.metrics.mean_squared_error",
                "sklearn.metrics.mean_absolute_error",
            ],
            "samples": CONDITIONING_SAMPLES,
            "offset": CONDITIONING_OFFSET,
            "spread": CONDITIONING_SPREAD,
            "perturbation": CONDITIONING_PERTURBATION,
            "construction": (
                "step = spread / samples; y_true[i] = offset + i * step; "
                "y_pred[i] = y_true[i] + ((i % 7) - 3) * perturbation"
            ),
            "probe_indices": PROBE_INDICES,
            "probe_bits_y_true": [_bits(y_true[i]) for i in PROBE_INDICES],
            "probe_bits_y_pred": [_bits(y_pred[i]) for i in PROBE_INDICES],
            "count": len(values),
        },
        "values": values,
    }


# Content/id matching flags over a byte-level model (issue #104); see
# generate_bpe_added_token_flags's docstring.
BPE_FLAG_TEXTS = [
    # lstrip, on <mask> -- roberta-base's own shape.
    "a <mask> b",
    "a<mask>b",
    "a  <mask>  b",
    "<mask> a",
    "a <mask>",
    "a\t<mask>",
    # A no-break space, written as an escape so no editor can flatten it to U+0020.
    "a\u00a0<mask>",
    "a. <mask>",
    # rstrip, on <pad>, the mirror of the same shapes.
    "a <pad> b",
    "a<pad>b",
    "a  <pad>  b",
    "<pad> a",
    "a <pad>",
    "a <pad>\tb",
    "a <pad>. b",
    # single_word, on <m>: a letter, a digit or '_' on either side blocks it,
    # punctuation and the ends of the text do not.
    "a <m> b",
    ".<m>.",
    "-<m>-",
    "1<m>1",
    "_<m>_",
    "é<m>é",
    "<m>",
    "a<m>b",
    # Two matches, one space apart: the probe for AddedTokenScanner's own
    # left-strip-stops-at-the-previous-match choice (see the docstring).
    "<pad> <mask>",
    "<mask> <mask>",
    "a <pad> <mask> b",
]


def generate_bpe_added_token_flags() -> dict:
    """GPT-2 with one added token per matching flag.

    Shaped after `generate_bpe_added_tokens`: the whole `tokenizer.json` rides in
    the metadata, so the C# side parses the exact bytes HuggingFace was handed.

    BPE_FLAG_TEXTS carries one token per flag, because a flag only shows in the
    pieces around the match: lstrip and rstrip make the space beside a match
    disappear -- the id is unchanged, what is gone is the 'Ġ' the whitespace
    would have produced -- and single_word makes a match not happen at all,
    leaving the marker's own characters to the merge loop.

    add_prefix_space is off here, unlike `generate_bpe_added_tokens`'s
    tokenizer: a prefix space is added per segment and would put a 'Ġ' beside
    every match, which is the very piece the strips are read from.
    bpe_added_tokens.json is where that setting is measured; this corpus keeps
    it out of the way.

    <m> is the one entry left non-special, so decoded_skip_specials shows the
    two halves of the table apart: special is what a decoder drops, and it
    decides nothing about where an entry matches.
    """
    from tokenizers import AddedToken  # noqa: PLC0415

    tokenizer = _gpt2_tokenizer()
    tokenizer.add_tokens([
        AddedToken(MASK_TOKEN, lstrip=True, special=True),
        AddedToken("<pad>", rstrip=True, special=True),
        AddedToken("<m>", single_word=True),
    ])

    cases = []
    for i, text in enumerate(BPE_FLAG_TEXTS):
        enc = tokenizer.encode(text)
        cases.append({
            "id": i,
            "text": text,
            "tokens": enc.tokens,
            "ids": enc.ids,
            "decoded": tokenizer.decode(enc.ids, skip_special_tokens=False),
            "decoded_skip_specials": tokenizer.decode(enc.ids, skip_special_tokens=True),
        })
    return {
        "metadata": {
            "algorithm": "ByteLevelBPE with added-token matching flags",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "gpt2 (vendored by tools/fetch_gpt2_bpe.py), <mask> lstrip, <pad> rstrip, <m> single_word",
            "tokenizer_json": tokenizer.to_str(),
            "count": len(cases),
        },
        "cases": cases,
    }


# --- The BPE settings that change nothing, and the one with no default (issue
# #118); see generate_bpe_no_op_settings's docstring. ---------------------------

# 'a</w>' etc. exist so no symbol is dropped for want of a vocabulary entry
# under the `</w>` contrast; see this section's docstring for why.
NO_OP_VOCAB = {"a": 0, "b": 1, "c": 2, "ab": 3, "a</w>": 4, "b</w>": 5, "c</w>": 6}
NO_OP_MERGES = [("a", "b")]
NO_OP_TEXTS = ["ab", "abc", "ab c", "a b", "c"]

# Byte-level, for `add_prefix_space`: 'Ġ' is the space, and 'ab' is reachable by
# the one merge, so the flag shows up as a leading 'Ġ' the model has an entry for.
NO_OP_BYTE_LEVEL_VOCAB = {"a": 0, "b": 1, "Ġ": 2, "ab": 3}
NO_OP_BYTE_LEVEL_MERGES = [("a", "b")]
NO_OP_BYTE_LEVEL_TEXTS = ["ab", "a b", " a"]


def _no_op_bpe(**settings):
    """The classic model every no-op case and its baseline are measured on."""
    from tokenizers import Tokenizer, models, pre_tokenizers  # noqa: PLC0415

    tokenizer = Tokenizer(models.BPE(
        vocab=dict(NO_OP_VOCAB), merges=list(NO_OP_MERGES), unk_token=None, **settings))
    tokenizer.pre_tokenizer = pre_tokenizers.Whitespace()
    return tokenizer


def _no_op_byte_level_bpe(add_prefix_space: bool):
    """The byte-level counterpart, whose `add_prefix_space` is declared either way."""
    from tokenizers import Tokenizer, decoders, models, pre_tokenizers  # noqa: PLC0415

    tokenizer = Tokenizer(models.BPE(
        vocab=dict(NO_OP_BYTE_LEVEL_VOCAB), merges=list(NO_OP_BYTE_LEVEL_MERGES), unk_token=None))
    tokenizer.pre_tokenizer = pre_tokenizers.ByteLevel(add_prefix_space=add_prefix_space)
    tokenizer.decoder = decoders.ByteLevel()
    return tokenizer


def _no_op_models() -> list[tuple[str, str, object, list[str]]]:
    """Every model the corpus carries: its name, what it declares, and its texts."""
    return [
        ("baseline", "no end_of_word_suffix, no continuing_subword_prefix, no dropout",
         _no_op_bpe(), NO_OP_TEXTS),
        ("end_of_word_suffix_empty", 'end_of_word_suffix: ""',
         _no_op_bpe(end_of_word_suffix=""), NO_OP_TEXTS),
        ("end_of_word_suffix_marker", 'end_of_word_suffix: "</w>"',
         _no_op_bpe(end_of_word_suffix="</w>"), NO_OP_TEXTS),
        ("continuing_subword_prefix_empty", 'continuing_subword_prefix: ""',
         _no_op_bpe(continuing_subword_prefix=""), NO_OP_TEXTS),
        ("dropout_zero", "dropout: 0.0",
         _no_op_bpe(dropout=0.0), NO_OP_TEXTS),
        ("byte_level_add_prefix_space_true", "ByteLevel add_prefix_space: true",
         _no_op_byte_level_bpe(add_prefix_space=True), NO_OP_BYTE_LEVEL_TEXTS),
        ("byte_level_add_prefix_space_false", "ByteLevel add_prefix_space: false",
         _no_op_byte_level_bpe(add_prefix_space=False), NO_OP_BYTE_LEVEL_TEXTS),
    ]


def _byte_level_documents_without_add_prefix_space() -> list[tuple[str, dict]]:
    """The same byte-level file with `add_prefix_space` removed, once per position.

    The three positions a `ByteLevel` block can appear in: the top-level
    `pre_tokenizer`, the second step of a `Sequence`, and the `decoder`.
    """
    base = json.loads(_no_op_byte_level_bpe(add_prefix_space=True).to_str())
    stripped = {k: v for k, v in base["pre_tokenizer"].items() if k != "add_prefix_space"}

    top_level = json.loads(json.dumps(base))
    top_level["pre_tokenizer"] = stripped

    sequence = json.loads(json.dumps(base))
    sequence["pre_tokenizer"] = {
        "type": "Sequence",
        "pretokenizers": [
            {"type": "Split", "pattern": {"Regex": " "}, "behavior": "Isolated", "invert": False},
            dict(stripped, use_regex=False),
        ],
    }

    decoder = json.loads(json.dumps(base))
    decoder["decoder"] = {k: v for k, v in base["decoder"].items() if k != "add_prefix_space"}

    return [("pre_tokenizer", top_level), ("sequence_step", sequence), ("decoder", decoder)]


def _add_prefix_space_refusals() -> list[dict]:
    """What `tokenizers` answers when a `ByteLevel` block omits `add_prefix_space`.

    These shapes cannot be cases: the reference refuses to build them, so there is
    no token stream to record. Recording the refusal instead keeps "the reference
    refuses this too" a measurement rather than a claim in a commit message.
    """
    from tokenizers import Tokenizer  # noqa: PLC0415

    refusals = []
    for position, doc in _byte_level_documents_without_add_prefix_space():
        document = json.dumps(doc)
        try:
            Tokenizer.from_str(document)
        except Exception as exc:  # noqa: BLE001 - the refusal IS the measurement
            refusals.append({"position": position, "document": document, "error": str(exc)})
        else:
            raise AssertionError(
                f"tokenizers accepted a ByteLevel {position} declaring no add_prefix_space; "
                "issue #118's refusal rests on it refusing one")
    return refusals


def generate_bpe_no_op_settings() -> dict:
    """Each BPE setting that changes nothing, beside the baseline that proves it.

    `LoadBpe` used to refuse `continuing_subword_prefix: ""` and `dropout: 0.0`,
    and to crash on `end_of_word_suffix: ""`. Accepting them rests on a claim --
    that each of those values is a no-op -- which a load test cannot make: a
    file that loads without throwing proves only that nothing was thrown. So
    each setting is recorded here against a baseline built from the same
    vocabulary and merges with the setting absent, and the equality of the two
    token streams is the evidence.

    The models are hand-built rather than vendored: the claim is about a value
    in the file, not about a particular model, and a 7-entry vocabulary keeps
    the whole corpus readable where GPT-2's would add two megabytes of noise.

    `end_of_word_suffix: "</w>"` is here as a contrast, not as a no-op: the
    same vocabulary tokenizes differently under it, which is what makes the
    empty case's equality a measurement rather than a property of a model too
    small to notice. NO_OP_VOCAB's 'a</w>', 'b</w>' and 'c</w>' entries exist
    so no symbol is ever dropped for want of a vocabulary entry under that
    contrast -- the model declares no unk_token, and a dropped symbol would
    make the contrast a test of that instead.
    """
    carried = _no_op_models()
    cases = []
    for name, _, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({
                "id": len(cases),
                "model": name,
                "text": text,
                "tokens": enc.tokens,
                "ids": enc.ids,
            })
    return {
        "metadata": {
            "algorithm": "BPE model settings that change nothing, and the ByteLevel field with no default",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "hand-built: a 7-entry classic BPE and a 4-entry byte-level BPE, both defined in tools/generate_oracles.py",
            "models": {
                name: {"declares": declares, "tokenizer_json": tokenizer.to_str()}
                for name, declares, tokenizer, _ in carried
            },
            # Read as: this model's token streams equal that model's, text for text.
            "no_op_pairs": [
                {"case": "end_of_word_suffix_empty", "baseline": "baseline"},
                {"case": "continuing_subword_prefix_empty", "baseline": "baseline"},
                {"case": "dropout_zero", "baseline": "baseline"},
            ],
            # And as: these two differ somewhere, which is what makes the equalities
            # above worth asserting.
            "contrast_pairs": [
                {"case": "end_of_word_suffix_marker", "baseline": "baseline"},
                {"case": "byte_level_add_prefix_space_true",
                 "baseline": "byte_level_add_prefix_space_false"},
            ],
            "add_prefix_space_refusals": _add_prefix_space_refusals(),
            "count": len(cases),
        },
        "cases": cases,
    }


# The same four flags over a WordPiece model that has a normalizer, named cases;
# see generate_wordpiece_added_tokens's docstring for why each one is here.
WORDPIECE_ADDED_TOKEN_TEXTS = [
    ("raw_entry_matches_its_own_casing", "the [CLS] cat"),
    ("raw_entry_ignores_the_lowercased_spelling", "the [cls] cat"),
    ("normalized_entry_matches_the_declared_casing", "the <MASK> cat"),
    ("normalized_entry_matches_the_lowercased_spelling", "the <mask> cat"),
    ("special_and_normalized_matches_the_declared_casing", "the [SEP] cat"),
    ("special_and_normalized_matches_the_lowercased_spelling", "the [sep] cat"),
    ("the_raw_pass_wins_over_a_normalized_match_further_left", "the A<R> cat"),
    ("a_normalized_match_stands_where_no_raw_one_does", "the a<r> cat"),
    ("lstrip_absorbs_the_space", "the <L> cat"),
    ("lstrip_absorbs_every_contiguous_space", "the  <L>  cat"),
    ("lstrip_absorbs_a_tab", "the\t<L>"),
    ("lstrip_absorbs_a_no_break_space", "the\u00a0<L>"),
    ("lstrip_stops_at_punctuation", "the. <L>"),
    ("lstrip_with_nothing_to_absorb", "the<L>cat"),
    ("lstrip_at_the_start_of_the_text", "<L> the"),
    ("lstrip_at_the_end_of_the_text", "the <L>"),
    ("rstrip_absorbs_the_space", "the <W> cat"),
    ("rstrip_absorbs_every_contiguous_space", "the  <W>  cat"),
    ("rstrip_with_nothing_to_absorb", "the<W>cat"),
    ("rstrip_at_the_start_of_the_text", "<W> the"),
    ("rstrip_at_the_end_of_the_text", "the <W>"),
    ("single_word_between_spaces", "the <S> cat"),
    ("single_word_between_full_stops", ".<S>."),
    ("single_word_between_hyphens", "-<S>-"),
    ("single_word_blocked_by_digits", "1<S>1"),
    ("single_word_blocked_by_underscores", "_<S>_"),
    ("single_word_blocked_by_accented_letters", "é<S>é"),
    ("single_word_blocked_by_letters", "the<S>cat"),
    ("single_word_is_the_whole_text", "<S>"),
]


def generate_wordpiece_added_tokens() -> dict:
    """A lowercasing WordPiece model with an added_tokens table that uses the flags.

    No other committed WordPiece corpus adds a token at all, so this is the only
    replayed evidence that the table is read and applied. The whole
    `tokenizer.json` rides in the metadata, as the BPE flag corpus does.

    This is the same four flags as the BPE corpus, over a model that has a
    normalizer, which is what BPE structurally cannot show: `normalized`
    decides which of two passes an entry runs in, and only a tokenizer that
    normalizes anything can tell the passes apart. WORDPIECE_ADDED_TOKEN_TEXTS
    is named cases, because each one is here for a reason:

    * a raw entry ([CLS], normalized=false) is matched against the
      un-lowercased text and emits its own casing;
    * a normalized entry (<MASK>) has its own content lowercased and is
      matched against the lowercased text, so both spellings match and both
      emit <mask>;
    * [SEP] is special *and* normalized, which every file add_special_tokens
      wrote makes look impossible -- it sets normalized = !special. It is the
      case that proves the discriminator is `normalized`, not `special`;
    * <R> (raw) and A<R> (normalized) overlap, with the normalized one
      starting further left. HuggingFace splits on the raw trie first and
      runs the normalized one over what is left, so the raw entry wins -- an
      outcome a single merged leftmost-wins scan cannot produce.

    Plus the strip and single_word shapes from the BPE corpus, with lstrip on
    a raw entry and rstrip on a normalized one so both passes carry a strip.

    Note for anyone regenerating: `tokenizers` refuses a tokenizer.json that
    omits `normalized`, so every entry states it. The absent-field default is
    therefore a decision this library makes rather than a behaviour it
    measured, and a C# unit test covers it instead.
    """
    from tokenizers import AddedToken  # noqa: PLC0415

    vocab = {token: index for index, token in enumerate(WORDPIECE_VOCAB)}
    tokenizer = _wordpiece_tokenizer(vocab, lowercase=True)
    tokenizer.add_tokens([
        AddedToken("[CLS]", special=True),
        AddedToken("<MASK>"),
        AddedToken("[SEP]", special=True, normalized=True),
        AddedToken("<R>", special=True),
        AddedToken("A<R>"),
        AddedToken("<L>", lstrip=True, special=True),
        AddedToken("<W>", rstrip=True),
        AddedToken("<S>", single_word=True),
    ])

    cases = []
    for i, (name, text) in enumerate(WORDPIECE_ADDED_TOKEN_TEXTS):
        enc = tokenizer.encode(text)
        cases.append({
            "id": i,
            "name": name,
            "text": text,
            "tokens": enc.tokens,
            "ids": enc.ids,
        })
    return {
        "metadata": {
            "algorithm": "WordPiece with added tokens and a Lowercase normalizer",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "reference_calls": [
                "tokenizers.Tokenizer(WordPiece(...)) with normalizers.Lowercase, then .add_tokens and .encode",
            ],
            "tokenizer_json": tokenizer.to_str(),
            "unk_token": UNK_TOKEN,
            "count": len(cases),
        },
        "cases": cases,
    }


# --- fuse_unk (issue #119) ----------------------------------------------------

# Z is in none of these vocabularies, which is what makes it a run when repeated.
_FUSE_VOCAB = {UNK_TOKEN: 0, "a": 1, "b": 2, "ab": 3}
_FUSE_MERGES = [("a", "b")]

# A merge whose LEFT side is the unknown token; see _fuse_unk_models's docstring.
_FUSE_MERGE_VOCAB = {UNK_TOKEN: 0, "a": 1, UNK_TOKEN + "a": 2}
_FUSE_MERGE_MERGES = [(UNK_TOKEN, "a")]

# The unknown token is ALSO a covered character; see _fuse_unk_models's docstring.
_FUSE_COVERED_UNK_VOCAB = {"q": 0, "a": 1}

# D7, the end-of-word suffix's own trap; see _fuse_unk_models's docstring.
_FUSE_EOW_SUFFIX = "</w>"
_FUSE_EOW_VOCAB = {UNK_TOKEN: 0, "a": 1, "a" + _FUSE_EOW_SUFFIX: 2, "Z": 3}


def _fuse_unk_model(vocab, merges, fuse, *, unk=UNK_TOKEN, byte_level=False, eow=None):
    """One tokenizer, built rather than trained, so the file is byte-stable.

    Every classic model declares Whitespace. A model declaring no pre-tokenizer
    at all does not split at all, which Lodestar reads as `NoPreTokenizer` since
    issue #122 and `bpe_no_split.json` measures; declaring the pre-tokenizer
    explicitly keeps this corpus about fuse_unk rather than about that split.
    """
    from tokenizers import Tokenizer, models, pre_tokenizers  # noqa: PLC0415

    # tokenizers 0.23.1 raises TypeError on unk_token=None or
    # end_of_word_suffix=None: both are omitted rather than passed empty.
    kwargs = {"fuse_unk": fuse}
    if unk is not None:
        kwargs["unk_token"] = unk
    if eow is not None:
        kwargs["end_of_word_suffix"] = eow
    tokenizer = Tokenizer(models.BPE(dict(vocab), list(merges), **kwargs))
    tokenizer.pre_tokenizer = (pre_tokenizers.ByteLevel(add_prefix_space=False) if byte_level
                               else pre_tokenizers.Whitespace())
    return tokenizer


def _fuse_unk_models() -> list[tuple]:
    """(name, declares, fuse, tokenizer, texts) for every shape the spec decided on.

    _FUSE_MERGE_VOCAB states a merge whose LEFT side is the unknown token.
    Training never produces this; it is stated so that "does fusing happen
    before or after merging" has an answer a test can read. Fused, "ZZa"
    merges to [UNK]a; unfused it cannot, because the second [UNK] sits between
    the first and the a.

    _FUSE_COVERED_UNK_VOCAB makes the unknown token ALSO a covered character.
    This is the only shape that tells "the previous symbol was substituted"
    from "the previous id equals the unknown id", and the two disagree: "qZ"
    does not fuse, "ZZ" does. It uses a letter, not punctuation: the
    pre-tokenizer isolates punctuation from letters, so "?" and "Z" would land
    in different pieces and never meet, and the trap needs both characters
    inside one piece.

    _FUSE_EOW_VOCAB is D7's trap: the end-of-word suffix is appended to a
    piece's last code point BEFORE the vocabulary lookup, and only at the last
    position -- there is no fallback to the bare form there. "a" is covered
    both bare (the form looked up everywhere but the last position) and
    suffixed (the form looked up at the last position), so a run ending on a
    covered character still resolves and does not fuse. "Z" is the
    distinguishing case: covered bare, but "Z</w>" is deliberately absent, so a
    "Z" in last position is uncovered and substituted even though the same
    character is a real token everywhere else in the piece -- an
    implementation that fell back to the bare lookup at the last position
    would resolve it instead and never show the difference. Y is covered in
    neither form, so a run it starts still extends across a "Z" the suffix
    turns uncovered.

    plain_texts covers a run in the middle, at each end, the whole text, a
    single unknown (where the two flags must agree), two runs split by a
    covered character, an alternation (no run at all), and an astral run.
    split_texts covers the same idea across a piece boundary: without a
    pre-tokenizer the space is itself uncovered, so "aZ Za" is one run of
    three; with Whitespace it is two pieces and must not fuse across.
    across_split gets only split_texts, never plain_texts: none of the plain
    ones contains a space, so Whitespace makes each one a single piece and the
    run fuses inside it exactly as it does with no pre-tokenizer, which would
    report `differs=True` for a reason that has nothing to do with boundaries.
    """
    from tokenizers import pre_tokenizers  # noqa: PLC0415

    byte_vocab = {c: i for i, c in enumerate(sorted(pre_tokenizers.ByteLevel.alphabet()))}

    plain_texts = ["aZZZa", "ZZa", "aZZ", "ZZZ", "aZa", "ZZaZZ", "ZaZaZ", "a\U0001F600\U0001F601a"]
    split_texts = ["aZ Za", "Z a Z"]

    models_out = []
    for fuse in (False, True):
        suffix = "fused" if fuse else "unfused"
        models_out.append((
            f"in_piece_{suffix}", "a run inside a single piece", fuse,
            _fuse_unk_model(_FUSE_VOCAB, _FUSE_MERGES, fuse), plain_texts))
        # Only the texts that HAVE a boundary; see this function's docstring.
        models_out.append((
            f"across_split_{suffix}", "a run interrupted by a piece boundary", fuse,
            _fuse_unk_model(_FUSE_VOCAB, _FUSE_MERGES, fuse), split_texts))
        models_out.append((
            f"unk_merge_{suffix}", "a merge whose left side is the unknown token", fuse,
            _fuse_unk_model(_FUSE_MERGE_VOCAB, _FUSE_MERGE_MERGES, fuse), ["ZZa", "Za", "ZZZa"]))
        models_out.append((
            f"covered_unk_{suffix}", "an unknown token that is also a covered character", fuse,
            _fuse_unk_model(_FUSE_COVERED_UNK_VOCAB, [], fuse, unk="q"),
            ["qZ", "Zq", "qq", "ZZ", "qZa", "aqZ"]))
        models_out.append((
            f"no_unk_{suffix}", "fuse_unk with no unknown token declared", fuse,
            _fuse_unk_model(_FUSE_VOCAB, _FUSE_MERGES, fuse, unk=None), ["aZZa", "ZZZ"]))
        models_out.append((
            f"byte_level_{suffix}", "byte-level, where no character is uncovered", fuse,
            _fuse_unk_model(byte_vocab, [], fuse, unk=None, byte_level=True),
            ["a\U0001F600b", "ab"]))
        models_out.append((
            f"end_of_word_{suffix}",
            "a character covered bare but not suffixed, where the end-of-word suffix decides"
            " the last-position lookup rather than the character's own coverage (D7)", fuse,
            _fuse_unk_model(_FUSE_EOW_VOCAB, [], fuse, eow=_FUSE_EOW_SUFFIX),
            ["aYZ", "aZZ", "Za", "aZ"]))
    return models_out


def generate_bpe_fuse_unk() -> dict:
    """Every fuse_unk shape, each recorded with the flag off and on."""
    carried = _fuse_unk_models()
    cases = []
    for name, _, _, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({
                "id": len(cases),
                "model": name,
                "text": text,
                "tokens": enc.tokens,
                "ids": enc.ids,
            })

    # Read as: turning the flag on changes this model's stream, or does not.
    # The `differs` column is measured below rather than asserted here.
    streams = {}
    for case in cases:
        streams.setdefault(case["model"], {})[case["text"]] = case["tokens"]
    pairs = []
    for name, _, fuse, _, _ in carried:
        if not fuse:
            continue
        unfused = name.replace("_fused", "_unfused")
        differs = any(streams[name][t] != streams[unfused][t] for t in streams[name])
        pairs.append({"fused": name, "unfused": unfused, "differs": differs})

    return {
        "metadata": {
            "algorithm": "BPE fuse_unk",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "hand-built: six classic BPE shapes and one byte-level, all defined in tools/generate_oracles.py",
            "models": {
                name: {"declares": declares, "fuse_unk": fuse, "tokenizer_json": tokenizer.to_str()}
                for name, declares, fuse, tokenizer, _ in carried
            },
            "fuse_pairs": pairs,
            "count": len(cases),
        },
        "cases": cases,
    }


# --- added tokens are not vocabulary entries (issue #130) ---------------------

# Q is the added token and is deliberately absent from this vocabulary; Z is
# absent from everything, so it is uncovered in every model here.
_COVERAGE_VOCAB = {UNK_TOKEN: 0, "a": 1, "b": 2, "ab": 3}
_COVERAGE_MERGES = [("a", "b")]

# The ignore_merges pair; see _added_coverage_models's docstring for "!!".
_IGNORE_MERGES_VOCAB_WITHOUT_BANG = {UNK_TOKEN: 0, "a": 1, "!": 2}
_IGNORE_MERGES_VOCAB_WITH_BANG = {UNK_TOKEN: 0, "a": 1, "!": 2, "!!": 3}


def _added_coverage_model(single_word):
    """One tokenizer whose added token is not in model.vocab."""
    from tokenizers import AddedToken, Tokenizer, models, pre_tokenizers  # noqa: PLC0415

    tokenizer = Tokenizer(models.BPE(
        dict(_COVERAGE_VOCAB), list(_COVERAGE_MERGES), unk_token=UNK_TOKEN))
    tokenizer.pre_tokenizer = pre_tokenizers.Whitespace()
    tokenizer.add_tokens([AddedToken("Q", single_word=single_word, normalized=False)])
    return tokenizer


def _added_coverage_ignore_merges_model(vocab):
    """One tokenizer with ignore_merges on: "!!" is an added token, in model.vocab or not."""
    from tokenizers import AddedToken, Tokenizer, models, pre_tokenizers  # noqa: PLC0415

    model = models.BPE(dict(vocab), [], unk_token=UNK_TOKEN)
    model.ignore_merges = True
    tokenizer = Tokenizer(model)
    tokenizer.pre_tokenizer = pre_tokenizers.Whitespace()
    tokenizer.add_tokens([AddedToken("!!", single_word=True, normalized=False)])
    return tokenizer


def _added_coverage_models() -> list[tuple]:
    """(name, declares, single_word, tokenizer, texts).

    texts: aQa and ZQZ put the added token inside a word, where single_word
    decides whether the scanner may match it. Q and "a Q a" put it on its own,
    where the scanner matches under either flag and both sides already agree.
    aQ ends a piece with it, and QQ doubles it.

    ignore_merges_texts: "!!" is the added token. a!! is split by Whitespace
    into "a" / "!!"; single_word declines the scanner on "a" (a word
    character), so "!!" reaches the ignore_merges shortcut while still being
    an added token's exact content. One vocabulary omits "!!" from
    model.vocab, the control carries it too so the shortcut can fire from
    there instead of from the fold. Bare "!!" reaches the scanner directly
    under either vocabulary, and is carried as the non-discriminating control.
    """
    texts = ["aQa", "ZQZ", "QQ", "Q", "a Q a", "aQ"]
    ignore_merges_texts = ["a!!", "!!"]
    return [
        ("single_word", "an added token absent from model.vocab, matched only on its own",
         True, _added_coverage_model(True), texts),
        ("any_position", "the same added token, matchable inside a word",
         False, _added_coverage_model(False), texts),
        ("ignore_merges_added_token_only",
         "ignore_merges on; the added token '!!' is absent from model.vocab, "
         "so the whole-piece shortcut cannot see it",
         True, _added_coverage_ignore_merges_model(_IGNORE_MERGES_VOCAB_WITHOUT_BANG),
         ignore_merges_texts),
        ("ignore_merges_added_token_in_vocab",
         "the control: '!!' is in model.vocab too, so the shortcut fires from there",
         True, _added_coverage_ignore_merges_model(_IGNORE_MERGES_VOCAB_WITH_BANG),
         ignore_merges_texts),
    ]


@contextlib.contextmanager
def _rust_stderr_captured():
    """Hold file descriptor 2 open to a temp file, yielding the file itself.

    A Rust panic is printed by the default panic hook, which writes to fd 2
    directly and before pyo3 turns the panic into a PanicException. Nothing
    above that layer can see it: contextlib.redirect_stderr rebinds
    sys.stderr, which the hook never consults. Only dup2 reaches it.
    """
    saved = os.dup(2)
    try:
        with tempfile.TemporaryFile(mode="w+b") as sink:
            os.dup2(sink.fileno(), 2)
            try:
                yield sink
            finally:
                os.dup2(saved, 2)
    finally:
        os.close(saved)


def _load_recording_panic(document: str):
    """(tokenizer, error) -- the reference's verdict on one document, panic and all.

    The error is None when it loaded. Whatever the panic hook wrote to fd 2 is
    dropped when a refusal came with it, and re-emitted when none did: the known
    panic is expected, and suppressing it must not hide the next one.
    """
    from tokenizers import Tokenizer  # noqa: PLC0415

    with _rust_stderr_captured() as sink:
        try:
            loaded, error = Tokenizer.from_str(document), None
        except BaseException as exc:  # noqa: BLE001 - the refusal IS the measurement
            # BaseException: a Rust panic surfaces as unimportable
            # pyo3_runtime.PanicException; Ctrl-C/SystemExit are re-raised (S5754).
            if isinstance(exc, (KeyboardInterrupt, SystemExit)):
                raise
            loaded, error = None, f"{type(exc).__name__}: {exc}"
        sink.seek(0)
        printed = sink.read().decode("utf-8", "replace")

    if printed and error is None:
        sys.stderr.write(printed)
    return loaded, error


def _added_coverage_refusals() -> list[dict]:
    """Three shapes the reference refuses, recorded with what it said and when.

    Two never produce a tokenizer at all — they fail while the document is
    read. The third produces a working one that answers token_to_id and
    encodes covered text, and refuses only an input needing a substitution.
    None of the three can be an ordinary case, because two have no token
    stream and the third's is beside the point; recording the refusal is what
    makes "the reference refuses this too" a measurement rather than a claim.

    unk_only_in_added_tokens: the unknown token exists only in added_tokens.
    This one LOADS -- the reference defers the check to encode, and raises
    only on text that needs a substitution. "ab" encodes fine; "aZb" does not.

    merge_names_an_added_token: a merge names a token that only added_tokens
    declares. Refused while the document is read.

    merge_result_missing: a merge whose two sides are present but whose
    result is not. The reference PANICS while reading rather than raising;
    the panic is recorded as what it is, and Lodestar refuses in its own words
    (D6). Its stderr is captured rather than let through: the hook prints on
    every run, and a line that is always there is one nobody reads (#214).

    The reference does not refuse all three at the same moment: two fail
    while the document is being read, the first loads and fails from encode
    -- and only on text that needs a substitution, which is why each shape
    carries the text that provokes it.
    """
    def document(vocab, merges, unk, added):
        return json.dumps({
            "version": "1.0", "truncation": None, "padding": None,
            "added_tokens": added,
            "normalizer": None, "pre_tokenizer": {"type": "Whitespace"},
            "post_processor": None, "decoder": None,
            "model": {
                "type": "BPE", "dropout": None, "unk_token": unk,
                "continuing_subword_prefix": None, "end_of_word_suffix": None,
                "fuse_unk": False, "byte_fallback": False, "ignore_merges": False,
                "vocab": vocab, "merges": merges,
            },
        })

    added_q = [{"id": 2, "content": "Q", "single_word": True, "lstrip": False,
                "rstrip": False, "normalized": False, "special": False}]

    # (shape, document, the text that provokes the failure if loading did not)
    # -- see this function's docstring for what each shape is.
    shapes = [
        ("unk_only_in_added_tokens",
         document({"a": 0, "b": 1}, [], UNK_TOKEN_LOWER,
                  [{"id": 2, "content": UNK_TOKEN_LOWER, "single_word": False, "lstrip": False,
                    "rstrip": False, "normalized": False, "special": True}]),
         "aZb"),
        ("merge_names_an_added_token",
         document({UNK_TOKEN: 0, "a": 1, "Qa": 3}, [["Q", "a"]], UNK_TOKEN, added_q),
         "Qa"),
        ("merge_result_missing",
         document({"a": 0, "b": 1}, [["a", "b"]], None, []),
         "ab"),
    ]

    refusals = []
    for shape, doc, provoking_text in shapes:
        tokenizer, refused_at_load = _load_recording_panic(doc)
        if refused_at_load is not None:
            refusals.append({"shape": shape, "document": doc, "raised_by": "load",
                             "text": None, "error": refused_at_load})
            continue
        try:
            tokenizer.encode(provoking_text)
        except BaseException as exc:  # noqa: BLE001
            # Same reasoning as _load_recording_panic's handler.
            if isinstance(exc, (KeyboardInterrupt, SystemExit)):
                raise
            refusals.append({"shape": shape, "document": doc, "raised_by": "encode",
                             "text": provoking_text, "error": f"{type(exc).__name__}: {exc}"})
        else:
            raise AssertionError(
                f"tokenizers accepted {shape} and encoded {provoking_text!r} without complaint; "
                "issue #130's refusal rests on it refusing one")
    return refusals


def generate_bpe_added_token_coverage() -> dict:
    """An added token absent from model.vocab, with the scanner allowed and denied."""
    carried = _added_coverage_models()
    cases = []
    for name, _, _, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({"id": len(cases), "model": name, "text": text,
                          "tokens": enc.tokens, "ids": enc.ids})

    return {
        "metadata": {
            "algorithm": "BPE added-token coverage",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "hand-built: one 4-entry classic BPE, twice, defined in tools/generate_oracles.py",
            "models": {
                name: {"declares": declares, "single_word": single_word,
                       "tokenizer_json": tokenizer.to_str()}
                for name, declares, single_word, tokenizer, _ in carried
            },
            "refusals": _added_coverage_refusals(),
            "count": len(cases),
        },
        "cases": cases,
    }


# --- continuing_subword_prefix (issue #120) -----------------------------------

_PREFIX = "##"
_PREFIX_EOW = "</w>"


def _prefix_model(vocab, merges, *, prefix=_PREFIX, eow=None, unk=None):
    """One tokenizer, built rather than trained, so the file is byte-stable."""
    from tokenizers import Tokenizer, models, pre_tokenizers  # noqa: PLC0415

    kwargs = {}
    if prefix is not None:
        kwargs["continuing_subword_prefix"] = prefix
    if eow is not None:
        kwargs["end_of_word_suffix"] = eow
    if unk is not None:
        kwargs["unk_token"] = unk
    tokenizer = Tokenizer(models.BPE(dict(vocab), list(merges), **kwargs))
    tokenizer.pre_tokenizer = pre_tokenizers.Whitespace()
    return tokenizer


def _prefix_models() -> list[tuple]:
    """(name, declares, tokenizer, texts), one per case nothing else distinguishes."""
    bare = {"a": 0, "b": 1, _PREFIX + "a": 2, _PREFIX + "b": 3}

    return [
        ("base", "a prefix, and a piece long enough to need it",
         _prefix_model(bare, []), ["ab", "a", "b"]),
        # Two words is the only shape that tells per-piece from per-text: the
        # first symbol of the SECOND word is bare.
        ("two_pieces", "two pieces, so the second one's first symbol is bare",
         _prefix_model(bare, []), ["ab ab", "a b", "ab a"]),
        # b exists bare and ##b does not. There is no fallback, so the character
        # is dropped -- or substituted where an unknown token exists.
        ("no_prefixed_form", "a character whose prefixed form is absent, and no unknown token",
         _prefix_model({"a": 0, "b": 1}, []), ["ab", "a", "ba"]),
        ("no_prefixed_form_unk", "the same, with an unknown token to substitute",
         _prefix_model({"a": 0, "b": 1, UNK_TOKEN: 2}, [], unk=UNK_TOKEN), ["ab", "a", "ba"]),
        # The merge result is the stripped concatenation. Only `ab` is present,
        # so a plain concatenation would look for `a##b` and fail.
        ("merge_stripped_result", "a merge whose stripped result alone is in the vocabulary",
         _prefix_model({"a": 0, "b": 1, _PREFIX + "b": 2, "ab": 3}, [("a", _PREFIX + "b")]),
         ["ab", "aba"]),
        # Both sides prefixed: the left keeps its prefix, only the right loses
        # one. "Both lose it" would need `bc` and is the plausible wrong reading.
        ("merge_both_prefixed", "a merge whose two sides both carry the prefix",
         _prefix_model({"a": 0, _PREFIX + "b": 1, _PREFIX + "c": 2, _PREFIX + "bc": 3},
                       [(_PREFIX + "b", _PREFIX + "c")]),
         ["abc", "ab"]),
        # ("a", "##b</w>") must give "ab</w>": strip the prefix, keep the suffix.
        # Stripping both, or neither, looks for a token that is absent.
        ("merge_suffixed_right", "a merge whose right side carries the prefix and the suffix at once",
         _prefix_model({"a": 0, _PREFIX + "b" + _PREFIX_EOW: 1, "ab" + _PREFIX_EOW: 2,
                        "a" + _PREFIX_EOW: 3},
                       [("a", _PREFIX + "b" + _PREFIX_EOW)], eow=_PREFIX_EOW),
         ["ab", "a"]),
        # Prefix and suffix compose, prefix then character then suffix.
        ("prefix_and_suffix", "a prefix and an end-of-word suffix on the same symbol",
         _prefix_model({"a": 0, "b": 1, _PREFIX + "b": 2, "b" + _PREFIX_EOW: 3,
                        _PREFIX + "b" + _PREFIX_EOW: 4, "a" + _PREFIX_EOW: 5},
                       [], eow=_PREFIX_EOW),
         ["ab", "a", "b"]),
        # An empty prefix must give the same stream as none at all. This is the
        # untouched path's own regression proof.
        ("empty_prefix", "an empty prefix, which prefixes nothing",
         _prefix_model(bare, [], prefix=""), ["ab", "a b"]),
        ("no_prefix", "no prefix declared, the baseline the empty one must equal",
         _prefix_model(bare, [], prefix=None), ["ab", "a b"]),
    ]


def _prefix_refusals() -> list[dict]:
    """The shape the reference refuses to build, with what it said.

    A merge whose two sides carry the prefix but whose CONCATENATED form is in
    the vocabulary instead of the stripped one. There is no token stream to
    record -- the reference never produces a tokenizer -- so recording the
    refusal is what makes "the reference refuses this too" a measurement.
    """
    from tokenizers import Tokenizer  # noqa: PLC0415

    document = json.dumps({
        "version": "1.0", "truncation": None, "padding": None, "added_tokens": [],
        "normalizer": None, "pre_tokenizer": {"type": "Whitespace"},
        "post_processor": None, "decoder": None,
        "model": {
            "type": "BPE", "dropout": None, "unk_token": None,
            "continuing_subword_prefix": _PREFIX, "end_of_word_suffix": None,
            "fuse_unk": False, "byte_fallback": False, "ignore_merges": False,
            "vocab": {"a": 0, _PREFIX + "b": 1, _PREFIX + "c": 2, _PREFIX + "b" + _PREFIX + "c": 3},
            "merges": [[_PREFIX + "b", _PREFIX + "c"]],
        },
    })

    try:
        Tokenizer.from_str(document)
    except BaseException as exc:  # noqa: BLE001 - the refusal IS the measurement
        # Same reasoning as _load_recording_panic's handler.
        if isinstance(exc, (KeyboardInterrupt, SystemExit)):
            raise
        return [{"shape": "merge_result_not_stripped", "document": document,
                 "error": f"{type(exc).__name__}: {exc}"}]
    raise AssertionError(
        "tokenizers accepted a merge whose result is the concatenated form; "
        "issue #120's stripped-result rule rests on it refusing one")


def generate_bpe_continuing_prefix() -> dict:
    """Every continuing_subword_prefix shape, and the one the reference refuses."""
    carried = _prefix_models()
    cases = []
    for name, _, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({"id": len(cases), "model": name, "text": text,
                          "tokens": enc.tokens, "ids": enc.ids})

    return {
        "metadata": {
            "algorithm": "BPE continuing_subword_prefix",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "hand-built: ten classic BPE shapes, defined in tools/generate_oracles.py",
            "models": {
                name: {"declares": declares, "tokenizer_json": tokenizer.to_str()}
                for name, declares, tokenizer, _ in carried
            },
            "refusals": _prefix_refusals(),
            "count": len(cases),
        },
        "cases": cases,
    }


# --- Split + ByteLevel Sequence, both patterns (issue #143) -------------------

# Llama-3's own Split pattern; mirrors BpePatterns.Llama3 in C#, repeated
# rather than imported so the generator does not depend on the library under test.
_SEQ_SPLIT = (
    r"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}"
    r"| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+"
)


def _sequence_split_model(use_regex):
    """A byte-level BPE behind Sequence[Split(Llama-3), ByteLevel].

    add_prefix_space is off throughout, deliberately: it prepends a space to
    every piece the Split step produces, so with it on each case here would
    measure that rule on top of this one and none would discriminate. It is
    bpe_prefix_space.json's subject instead. ADR 0022 section 10 recorded the
    same reasoning when bpe_added_token_flags.json was generated with it off.

    The merges exist so the split is observable in the tokens and not only in the
    pieces: a merge never crosses a piece boundary, so "'ai" can only be reached
    when the apostrophe and the letters share a piece.

    They cover 'a/'ai and 'h/'hu, so of the five texts whose PIECES differ
    between the two models, two also differ in TOKENS -- "j'ai vu l'ami d'Anne"
    and "aujourd'hui". "C'est l'été", "O'Brien and D'Angelo" and "rock'n'roll"
    differ in pieces alone, there being no merge starting 'e, 'B, 'n or 'r.

    That is deliberate rather than a gap. The pieces are the evidence, and all
    five carry it; the tokens exist only to prove the pieces reach the merge
    loop, which two texts establish as well as five would. Merges added for 'e,
    'B, 'n and 'r would exist solely to make an established proof redundant.
    """
    from tokenizers import Tokenizer, models, pre_tokenizers, decoders, Regex  # noqa: PLC0415

    vocab = {c: i for i, c in enumerate(sorted(pre_tokenizers.ByteLevel.alphabet()))}
    merges = []
    for left, right in (("'", "a"), ("'a", "i"), ("a", "i"), ("'", "h"), ("'h", "u")):
        merged = left + right
        if merged not in vocab:
            vocab[merged] = len(vocab)
        merges.append((left, right))

    tokenizer = Tokenizer(models.BPE(vocab, merges))
    tokenizer.pre_tokenizer = pre_tokenizers.Sequence([
        pre_tokenizers.Split(Regex(_SEQ_SPLIT), behavior="isolated"),
        pre_tokenizers.ByteLevel(add_prefix_space=False, use_regex=use_regex),
    ])
    tokenizer.decoder = decoders.ByteLevel()
    return tokenizer


def _sequence_split_models() -> list[tuple]:
    """(name, declares, tokenizer, texts) — one per side of the divergence.

    texts opens with the divergence itself, on five shapes of one cause rather
    than five spellings of one shape: elision before a vowel, before an h, an
    accented letter after the apostrophe, a capitalised name, and twice inside
    one word.

    "it's fine", "don't" and "the 'quoted' word" are cases that must NOT move:
    without them the corpus proves something changed, not that the right thing
    changed -- a fix that split on every apostrophe would pass the group above
    and fail here. "hello123 don't" is a fourth must-not-move case covering a
    reason the other three do not: Llama-3's pattern already parts letters
    from digits and already isolates 't, so the second pass changes nothing
    here even though it changes every elision above.
    """
    texts = [
        "j'ai vu l'ami d'Anne",
        "aujourd'hui",
        "C'est l'été",
        "O'Brien and D'Angelo",
        "rock'n'roll",
        "it's fine",
        "don't",
        "the 'quoted' word",
        "hello123 don't",
    ]
    return [
        ("use_regex_on",
         "a Sequence whose ByteLevel step splits again, which is the default",
         _sequence_split_model(True), texts),
        ("use_regex_off",
         "the same Sequence with the second split turned off",
         _sequence_split_model(False), texts),
    ]


def generate_bpe_sequence_split() -> dict:
    """Both patterns of a Split + ByteLevel Sequence, pieces as well as tokens."""
    carried = _sequence_split_models()
    cases = []
    for name, _, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({
                "id": len(cases),
                "model": name,
                "text": text,
                # The pre-tokenizer's own output, which is where the defect is.
                "pieces": [p for p, _span in tokenizer.pre_tokenizer.pre_tokenize_str(text)],
                "tokens": enc.tokens,
                "ids": enc.ids,
            })

    return {
        "metadata": {
            "algorithm": "BPE Sequence[Split, ByteLevel] pre-tokenization",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "hand-built: the byte-level alphabet plus five merges, defined in tools/generate_oracles.py",
            "models": {
                name: {"declares": declares, "tokenizer_json": tokenizer.to_str()}
                for name, declares, tokenizer, _ in carried
            },
            "count": len(cases),
        },
        "cases": cases,
    }


# --- Split behavior and invert (issue #145) -----------------------------------

# Named in the Python constructor's spelling; see _split_behavior_models's
# docstring, and D6.
_SPLIT_BEHAVIORS = ["isolated", "removed", "merged_with_previous",
                    "merged_with_next", "contiguous"]

# Two patterns because one cannot separate every behavior; see
# _split_behavior_models's docstring.
_SPLIT_PATTERN = r"\w+"
_SPLIT_ADJACENT_PATTERN = "X"


def _split_behavior_model(pattern, behavior, invert):
    """One byte-level BPE behind Sequence[Split(pattern), ByteLevel].

    add_prefix_space is off throughout, deliberately: it prepends a space to
    every piece the Split step produces, so with it on each case here would
    measure that rule on top of this one. It is bpe_prefix_space.json's
    subject instead. ADR 0022 section 10 recorded the same reasoning.

    use_regex is off on the ByteLevel step so the Split step's arrangement
    reaches the model untouched -- with it on, GPT-2's pattern would re-split
    every piece and hide the behavior being measured.
    """
    from tokenizers import Tokenizer, models, pre_tokenizers, decoders, Regex  # noqa: PLC0415

    vocab = {c: i for i, c in enumerate(sorted(pre_tokenizers.ByteLevel.alphabet()))}
    tokenizer = Tokenizer(models.BPE(vocab, []))
    tokenizer.pre_tokenizer = pre_tokenizers.Sequence([
        pre_tokenizers.Split(Regex(pattern), behavior=behavior, invert=invert),
        pre_tokenizers.ByteLevel(add_prefix_space=False, use_regex=False),
    ])
    tokenizer.decoder = decoders.ByteLevel()
    return tokenizer


def _split_behavior_texts():
    """The texts, boundaries as well as examples.

    They do NOT make all twenty models differ, and could not: invert is a no-op
    for isolated and contiguous, and it exchanges the two merge directions, so
    twelve of the pairs are equal by the reference's own rules rather than by a
    weakness here. What the set has to do is separate the five behaviors, which
    needs the adjacency case above for isolated against contiguous.
    """
    return [
        # The spec's D2 row: separates removed, merged_with_previous,
        # merged_with_next and their inversions from each other.
        "ab cd!",
        # D7's boundary rows, which are where an off-by-one in the segmentation
        # lands: fully matched, not matched at all, and gaps on both ends.
        "abc",
        "  ",
        " ab ",
        "",
        # Adjacent matches under the second pattern -- the only shape that
        # tells isolated from contiguous (D4).
        "aXXb",
    ]


def _split_behavior_models() -> list[tuple]:
    """(name, declares, pattern, behavior, invert, tokenizer, texts).

    Twenty models: five behaviors x invert x two patterns. _SPLIT_BEHAVIORS is
    named in the Python constructor's spelling; the file it serializes to uses
    PascalCase, which is what the C# loader reads (measured, spec D6).

    Two patterns, because one cannot separate every behavior. _SPLIT_PATTERN
    ("\\w+") leaves gaps in most texts, which is what tells removed and the two
    merge directions apart -- but it is greedy, so it never produces two
    ADJACENT matches, and isolated and contiguous differ nowhere else (spec
    D4). _SPLIT_ADJACENT_PATTERN ("X") over "aXXb" is that shape, and it is
    the only reason the second pattern exists.
    """
    carried = []
    for behavior in _SPLIT_BEHAVIORS:
        for invert in (False, True):
            for pattern, tag in ((_SPLIT_PATTERN, ""), (_SPLIT_ADJACENT_PATTERN, "_adjacent")):
                name = f"{behavior}{'_inverted' if invert else ''}{tag}"
                carried.append((
                    name,
                    f"behavior {behavior}, invert {invert}, pattern {pattern!r}",
                    pattern, behavior, invert,
                    _split_behavior_model(pattern, behavior, invert),
                    _split_behavior_texts(),
                ))
    return carried


def _split_behavior_refusals() -> list[dict]:
    """The three Split-step shapes the reference refuses to build."""
    from tokenizers import Tokenizer  # noqa: PLC0415

    def document(step):
        return json.dumps({
            "version": "1.0", "truncation": None, "padding": None, "added_tokens": [],
            "normalizer": None,
            "pre_tokenizer": {"type": "Sequence", "pretokenizers": [
                step, {"type": "ByteLevel", "add_prefix_space": False,
                       "trim_offsets": True, "use_regex": False}]},
            "post_processor": None, "decoder": None,
            "model": {"type": "BPE", "dropout": None, "unk_token": None,
                      "continuing_subword_prefix": None, "end_of_word_suffix": None,
                      "fuse_unk": False, "byte_fallback": False, "ignore_merges": False,
                      "vocab": {"a": 0}, "merges": []},
        })

    full = {"type": "Split", "pattern": {"Regex": _SPLIT_PATTERN},
            "behavior": "Isolated", "invert": False}
    shapes = [
        ("behavior_absent", {k: v for k, v in full.items() if k != "behavior"}),
        ("invert_absent", {k: v for k, v in full.items() if k != "invert"}),
        ("behavior_unknown", {**full, "behavior": "Nonsense"}),
    ]

    refusals = []
    for shape, step in shapes:
        doc = document(step)
        try:
            Tokenizer.from_str(doc)
        except BaseException as exc:  # noqa: BLE001 - the refusal IS the measurement
            # Same reasoning as _load_recording_panic's handler.
            if isinstance(exc, (KeyboardInterrupt, SystemExit)):
                raise
            refusals.append({"shape": shape, "document": doc,
                             "error": f"{type(exc).__name__}: {exc}"})
            continue
        raise AssertionError(
            f"tokenizers accepted {shape}; issue #145's refusal of it rests on the reference refusing it")
    return refusals


# --- a merge pair listed twice (issue #160) -----------------------------------

# See _duplicate_merge_models's docstring for the a+b duplicate this backs.
_DUPLICATE_VOCAB = {"a": 0, "b": 1, "c": 2, "d": 3, "ab": 4, "bc": 5, "cd": 6}


def _duplicate_merge_document(merges) -> str:
    """A tokenizer.json written by hand, so a duplicate survives into the file.

    Round-tripping through Tokenizer.to_str() cannot be used here: the
    reference collapses a repeated pair while serializing, so the document it
    writes for the duplicate is byte-identical to the one it writes for
    last_kept. A corpus built that way hands the loader under test a file with
    no duplicate in it and passes while measuring nothing.
    """
    return json.dumps({
        "version": "1.0", "truncation": None, "padding": None, "added_tokens": [],
        "normalizer": None, "pre_tokenizer": {"type": "Whitespace"},
        "post_processor": None, "decoder": None,
        "model": {
            "type": "BPE", "dropout": None, "unk_token": None,
            "continuing_subword_prefix": None, "end_of_word_suffix": None,
            "fuse_unk": False, "byte_fallback": False, "ignore_merges": False,
            "vocab": dict(_DUPLICATE_VOCAB), "merges": [list(pair) for pair in merges],
        },
    })


def _duplicate_merge_models() -> list[tuple]:
    """(name, declares, document, tokenizer, texts) -- the duplicate and both readings.

    _DUPLICATE_VOCAB: a+b is listed at rank 0 AND rank 3. Keeping the first
    makes it merge before b+c; keeping the last makes it merge after. Nothing
    else distinguishes the two readings, and no committed corpus contained a
    duplicated pair before this.
    """
    from tokenizers import Tokenizer  # noqa: PLC0415

    first, second, third = ("a", "b"), ("b", "c"), ("c", "d")
    shapes = [
        ("duplicate", "a+b listed at rank 0 and again at rank 3",
         [first, second, third, first]),
        ("first_kept", "the same table with only the rank-0 occurrence",
         [first, second, third]),
        ("last_kept", "the same table with only the rank-3 occurrence",
         [second, third, first]),
    ]
    carried = []
    for name, declares, merges in shapes:
        document = _duplicate_merge_document(merges)
        carried.append((name, declares, document, Tokenizer.from_str(document),
                        ["abcd", "abc", "ab"]))
    return carried


def generate_bpe_duplicate_merge() -> dict:
    """Which occurrence of a repeated merge pair the reference keeps."""
    carried = _duplicate_merge_models()
    cases = []
    for name, _declares, _document, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({"id": len(cases), "model": name, "text": text,
                          "tokens": enc.tokens, "ids": enc.ids})

    return {
        "metadata": {
            "algorithm": "BPE merge table with a duplicated pair",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "hand-built: seven entries and four merges, defined in tools/generate_oracles.py",
            # tokenizer_json is written here rather than by Tokenizer.to_str(),
            # which collapses the duplicate and would make this corpus vacuous.
            "models": {
                name: {"declares": declares, "tokenizer_json": document}
                for name, declares, document, _, _ in carried
            },
            # Read as: the duplicate's stream equals one of these two, and which
            # one it is is the whole measurement.
            "candidates": ["first_kept", "last_kept"],
            "count": len(cases),
        },
        "cases": cases,
    }


def generate_bpe_split_behavior() -> dict:
    """Every Split behavior and invert, pieces as well as tokens."""
    carried = _split_behavior_models()
    cases = []
    for name, _declares, _pattern, _behavior, _invert, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({
                "id": len(cases),
                "model": name,
                "text": text,
                # The pre-tokenizer's own output, which is where the behavior is.
                "pieces": [p for p, _span in tokenizer.pre_tokenizer.pre_tokenize_str(text)],
                "tokens": enc.tokens,
                "ids": enc.ids,
            })

    return {
        "metadata": {
            "algorithm": "BPE Sequence Split step behavior and invert",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": BYTE_LEVEL_NO_MERGES,
            "models": {
                name: {"declares": declares, "pattern": pattern, "behavior": behavior,
                       "invert": invert, "tokenizer_json": tokenizer.to_str()}
                for name, declares, pattern, behavior, invert, tokenizer, _ in carried
            },
            "refusals": _split_behavior_refusals(),
            "count": len(cases),
        },
        "cases": cases,
    }


# --- a pre-tokenizer that does not split (issue #122) -------------------------

# fuse_unk is on so the unsplit side comes out SHORTER than the split one --
# measured, 3 tokens against 4; with it off the pair still differs, 5 against 4.
_NO_SPLIT_VOCAB = {UNK_TOKEN: 0, "a": 1, "b": 2, "ab": 3}


def _no_split_classic(pre_tokenizer):
    """A classic BPE, built rather than trained, so the file is byte-stable."""
    from tokenizers import Tokenizer, models, pre_tokenizers  # noqa: PLC0415

    tokenizer = Tokenizer(models.BPE(
        dict(_NO_SPLIT_VOCAB), [("a", "b")], unk_token=UNK_TOKEN, fuse_unk=True))
    if pre_tokenizer is not None:
        tokenizer.pre_tokenizer = pre_tokenizer
    return tokenizer


def _no_split_byte_level(use_regex, add_prefix_space=False, added=None):
    """A byte-level BPE whose alphabet covers every byte, so nothing is unknown.

    The one merge spans a piece boundary on purpose: use_regex cuts "hello
    world" between the o and the space, so only the unsplit model can apply it.
    Without it both models emit one token per character and measure nothing.
    """
    from tokenizers import Tokenizer, models, pre_tokenizers, decoders  # noqa: PLC0415

    vocab = {c: i for i, c in enumerate(sorted(pre_tokenizers.ByteLevel.alphabet()))}
    vocab["oĠ"] = len(vocab)
    tokenizer = Tokenizer(models.BPE(vocab, [("o", "Ġ")]))
    tokenizer.pre_tokenizer = pre_tokenizers.ByteLevel(
        add_prefix_space=add_prefix_space, use_regex=use_regex)
    tokenizer.decoder = decoders.ByteLevel()
    if added:
        tokenizer.add_tokens(added)
    return tokenizer


def _no_split_models() -> list[tuple]:
    """(name, declares, tokenizer, texts) -- one per thing no other model shows."""
    from tokenizers import AddedToken, pre_tokenizers  # noqa: PLC0415

    fuse_texts = ["aZ Za", "ab", "Z Z"]
    byte_texts = [HELLO_WORLD, "  leading and trailing  ", "hello world  again", "café \U0001f600"]
    added_texts = ["o o<sep>o o", "o o"]
    return [
        ("absent", "no pre_tokenizer at all -- the shape #122 found Lodestar mis-loading",
         _no_split_classic(None), fuse_texts),
        ("whitespace", "the classic Whitespace split, for the row above to differ from",
         _no_split_classic(pre_tokenizers.Whitespace()), fuse_texts),
        ("byte_level_no_regex", "ByteLevel with use_regex off -- refused before #122",
         _no_split_byte_level(False), byte_texts),
        ("byte_level_regex", "the same with it on, so the pair shows what the flag does",
         _no_split_byte_level(True), byte_texts),
        ("no_regex_prefix_space", "no split and add_prefix_space on -- one space, at the front",
         _no_split_byte_level(False, add_prefix_space=True), byte_texts),
        ("no_regex_added_token", "no split, with an added token the text spans",
         _no_split_byte_level(False, added=[AddedToken("<sep>", special=True)]),
         added_texts),
        ("regex_added_token", "the split counterpart the row above is measured against",
         _no_split_byte_level(True, added=[AddedToken("<sep>", special=True)]),
         added_texts),
    ]


def generate_bpe_no_split() -> dict:
    """What a pre-tokenizer that does not split produces, and what it decodes to."""
    carried = _no_split_models()
    cases = []
    for name, _declares, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({
                "id": len(cases), "model": name, "text": text,
                "tokens": enc.tokens, "ids": enc.ids,
                # D5 is about the input coming back; a token list proves itself.
                "decoded": tokenizer.decode(enc.ids, skip_special_tokens=False),
            })

    return {
        "metadata": {
            "algorithm": "BPE with a pre-tokenizer that does not split",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": "hand-built: a 4-entry classic BPE and a byte-level one, defined in tools/generate_oracles.py",
            "models": {
                name: {"declares": declares, "tokenizer_json": tokenizer.to_str()}
                for name, declares, tokenizer, _ in carried
            },
            "count": len(cases),
        },
        "cases": cases,
    }


# --- add_prefix_space per Split piece (issue #122) ----------------------------

# A Regex, not a bare string: the loader reads both spellings since #167, but
# swapping this one would re-serialize the models and move a frozen corpus.
_PREFIX_SPACE_SPLIT = r"\|"


def _prefix_space_model(pre_split, add_prefix_space, use_regex):
    """A byte-level BPE with no merges, so every piece is one token per character.

    No merges on purpose: this corpus is about where a space is inserted, and a
    merge would fold that evidence into a token whose spelling hides it.
    """
    from tokenizers import Regex, Tokenizer, models, pre_tokenizers, decoders  # noqa: PLC0415

    vocab = {c: i for i, c in enumerate(sorted(pre_tokenizers.ByteLevel.alphabet()))}
    tokenizer = Tokenizer(models.BPE(vocab, []))
    byte_level = pre_tokenizers.ByteLevel(
        add_prefix_space=add_prefix_space, use_regex=use_regex)
    tokenizer.pre_tokenizer = pre_tokenizers.Sequence([
        pre_tokenizers.Split(Regex(_PREFIX_SPACE_SPLIT), behavior="isolated", invert=False),
        byte_level,
    ]) if pre_split else byte_level
    tokenizer.decoder = decoders.ByteLevel()
    return tokenizer


def _prefix_space_models() -> list[tuple]:
    """(name, declares, tokenizer, texts) -- one per thing no other model shows."""
    # The last text has no "|": the four models with the space on agree on what it
    # DECODES to; their pieces still differ, on use_regex rather than on the split.
    texts = ["ab|cd", "a b|c d", "ab| cd", " ab|cd", "a| |b", "a|b|c|d", "no split here"]
    return [
        ("presplit_aps", "Sequence[Split, ByteLevel(aps on, use_regex off)] -- Llama-3's shape",
         _prefix_space_model(True, add_prefix_space=True, use_regex=False), texts),
        ("presplit_aps_regex", "the same with use_regex on, so both patterns and the space are measured together",
         _prefix_space_model(True, add_prefix_space=True, use_regex=True), texts),
        ("presplit_no_aps", "the same with aps off -- the control, and what every shipped model declares",
         _prefix_space_model(True, add_prefix_space=False, use_regex=False), texts),
        ("bare_aps", "a bare ByteLevel with aps on -- GPT-2's shape, which must not move",
         _prefix_space_model(False, add_prefix_space=True, use_regex=True), texts),
        ("no_split_aps", "a bare ByteLevel, aps on and use_regex off -- the no-split mode's boundary",
         _prefix_space_model(False, add_prefix_space=True, use_regex=False), texts),
    ]


def generate_bpe_prefix_space() -> dict:
    """Where add_prefix_space lands, per piece rather than per text."""
    carried = _prefix_space_models()
    cases = []
    for name, _declares, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({
                "id": len(cases), "model": name, "text": text,
                # The pre-tokenizer's own output, which is where the space lands.
                "pieces": [p for p, _span in tokenizer.pre_tokenizer.pre_tokenize_str(text)],
                "tokens": enc.tokens, "ids": enc.ids,
                # The divergence survives Decode, which is how a user meets it.
                "decoded": tokenizer.decode(enc.ids, skip_special_tokens=False),
            })

    return {
        "metadata": {
            "algorithm": "BPE add_prefix_space placement",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": BYTE_LEVEL_NO_MERGES,
            "models": {
                name: {"declares": declares, "tokenizer_json": tokenizer.to_str()}
                for name, declares, tokenizer, _ in carried
            },
            "count": len(cases),
        },
        "cases": cases,
    }


# --- a Split step whose pattern is a literal (issue #167) ---------------------

# Three of these prove the escape happened -- measured, dropping Regex.Escape
# reddens backslash_d, metachar_dot and pipe; ab, the emoji and "" cannot.
_SPLIT_LITERALS = {
    "backslash_d": ("\\d", ["a\\db 7", "\\d\\d", "7\\d7"]),
    "metachar_dot": ("a.c", ["abc a.c", "a.c.a", "aXc"]),
    "pipe": ("|", ["ab|cd", "|ab", "a||b"]),
    "multi_char": ("ab", ["xabyab", "ab", "aab"]),
    "astral": ("\U0001f600", ["a\U0001f600b", "\U0001f600", "a\U0001f601b"]),
    # Carried as a model rather than a divergence because the two agree:
    # measured, BpePreTokenizer over "" gives ["a", "b", "c"] for "abc" too.
    "empty": ("", ["abc", "", "a"]),
}


def _split_literal_model(pattern):
    """A byte-level BPE with no merges, so every piece is one token per character.

    The pattern is handed in already wrapped, as a str for the literal spelling
    and a Regex for the escaped one -- which is the difference under test.
    """
    from tokenizers import Tokenizer, models, pre_tokenizers, decoders  # noqa: PLC0415

    vocab = {c: i for i, c in enumerate(sorted(pre_tokenizers.ByteLevel.alphabet()))}
    tokenizer = Tokenizer(models.BPE(vocab, []))
    tokenizer.pre_tokenizer = pre_tokenizers.Sequence([
        pre_tokenizers.Split(pattern, behavior="isolated", invert=False),
        pre_tokenizers.ByteLevel(add_prefix_space=False, use_regex=False),
    ])
    tokenizer.decoder = decoders.ByteLevel()
    return tokenizer


def _split_literal_models() -> list[tuple]:
    """(name, declares, literal, tokenizer, texts) -- each literal beside its escaped twin."""
    import re  # noqa: PLC0415
    from tokenizers import Regex  # noqa: PLC0415

    carried = []
    for name, (literal, texts) in _SPLIT_LITERALS.items():
        carried.append((
            f"{name}_literal", f"Split pattern spelled {{'String': {literal!r}}}",
            literal, _split_literal_model(literal), texts))
        carried.append((
            f"{name}_escaped", f"the same literal as {{'Regex': {re.escape(literal)!r}}}",
            None, _split_literal_model(Regex(re.escape(literal))), texts))
    return carried


def _split_literal_refusals() -> list[dict]:
    """The two pattern shapes #167 decides to refuse: neither spelling, and both.

    Not a measurement of either side. tokenizers builds neither shape, so there
    is no reference error to capture, which is why both are carried as shapes
    and not as recorded errors. The loader refuses each of them since 01c0de1 --
    the both case on the two keys being present rather than on both values being
    readable -- and the tests assert on those messages, not on anything here.
    """
    shapes = [
        ("pattern_empty", {}),
        ("pattern_both", {"Regex": "a", "String": "a"}),
    ]
    return [{"shape": shape, "pattern": pattern} for shape, pattern in shapes]


def generate_bpe_split_literal() -> dict:
    """What a literal Split pattern produces, beside its escaped-regex twin."""
    carried = _split_literal_models()
    cases = []
    for name, _declares, _literal, tokenizer, texts in carried:
        for text in texts:
            enc = tokenizer.encode(text)
            cases.append({
                "id": len(cases), "model": name, "text": text,
                # The pre-tokenizer's own output, which is where the pattern acts.
                "pieces": [p for p, _span in tokenizer.pre_tokenizer.pre_tokenize_str(text)],
                "tokens": enc.tokens, "ids": enc.ids,
            })

    return {
        "metadata": {
            "algorithm": "BPE Sequence Split step with a literal pattern",
            "library": "tokenizers",
            "library_version": version("tokenizers"),
            "model": BYTE_LEVEL_NO_MERGES,
            "models": {
                name: {"declares": declares, "literal": literal,
                       "tokenizer_json": tokenizer.to_str()}
                for name, declares, literal, tokenizer, _ in carried
            },
            # Lodestar's refusals, not the reference's: tokenizers builds neither
            # shape, so there is no error to capture -- only the pattern node.
            "refusals": _split_literal_refusals(),
            "count": len(cases),
        },
        "cases": cases,
    }


def main() -> None:
    """Write every oracle deterministically, byte for byte.

    ``newline="\\n"``: these files are committed and CI's "Oracles are
    reproducible" job compares them with a raw ``git diff``, not a text-mode
    read. A contributor with core.autocrlf=false or unset who regenerates on
    Windows would have the platform default translate every "\\n" to "\\r\\n"
    on write, and that CRLF would reach the repository as-is and make the diff
    nonempty forever, even though nothing semantic changed. (core.autocrlf=true
    or =input is unaffected: git normalises CRLF back to LF on add/commit
    regardless of what Python wrote to disk -- verified against all three
    settings by committing an LF file, rewriting it as CRLF, and re-running
    ``git diff --quiet``: true and input exit 0, false exits 1.)

    ``allow_nan=False``: Python would otherwise write a bare NaN or Infinity,
    which is not JSON and which System.Text.Json refuses at load time -- a
    failure that would surface in CI as a broken test run rather than here as
    a broken generation. Non-finite oracle values are encoded deliberately, as
    the strings below.
    """
    ORACLE_DIR.mkdir(parents=True, exist_ok=True)
    generators = {
        "levenshtein.json": generate_levenshtein,
        "osa.json": generate_osa,
        "damerau.json": generate_damerau,
        "hamming.json": generate_hamming,
        "indel.json": generate_indel,
        "jaro.json": generate_jaro,
        "jaro_winkler.json": generate_jaro_winkler,
        "lcs.json": generate_lcs,
        "ratcliff.json": generate_ratcliff,
        "set_similarity.json": generate_set_similarity,
        "phonetics.json": generate_phonetics,
        "metaphone.json": generate_metaphone,
        "countvectorizer.json": generate_countvectorizer,
        "tfidfvectorizer.json": generate_tfidfvectorizer,
        "hashingvectorizer.json": generate_hashingvectorizer,
        "porter.json": generate_porter,
        "snowball_en.json": generate_snowball_en,
        "snowball_fr.json": generate_snowball_fr,
        "snowball_es.json": generate_snowball_es,
        "snowball_pt.json": generate_snowball_pt,
        "snowball_it.json": generate_snowball_it,
        "snowball_de.json": generate_snowball_de,
        "wordpiece.json": generate_wordpiece,
        "batch_encoding.json": generate_batch_encoding,
        "pooling.json": generate_pooling,
        "knn.json": generate_knn,
        "sentencepiece.json": generate_sentencepiece,
        "vocab_txt.json": generate_vocab_txt,
        "tokenizer_json.json": generate_tokenizer_json,
        "spiece_model.json": generate_spiece_model,
        "xlmr_fairseq.json": generate_xlmr_fairseq,
        "normalizer.json": generate_normalizer,
        "fuzz.json": generate_fuzz,
        "process.json": generate_process,
        "classification_metrics.json": generate_classification_metrics,
        "clustering_agreement.json": generate_clustering_agreement,
        "silhouette.json": generate_silhouette,
        "internal_validity.json": generate_internal_validity,
        "ranking.json": generate_ranking,
        "ranking_weighted.json": generate_ranking_weighted,
        "label_ranking.json": generate_label_ranking,
        "average_precision.json": generate_average_precision,
        "top_k_accuracy.json": generate_top_k_accuracy,
        "roc_auc.json": generate_roc_auc,
        "regression.json": generate_regression,
        "regression_conditioning.json": generate_regression_conditioning,
        "regression_deviance.json": generate_regression_deviance,
        "bpe.json": generate_bpe,
        "orphan_bpe.json": generate_orphan_bpe,
        "bytelevel_bpe.json": generate_bytelevel_bpe,
        "bpe_pretokenize.json": generate_bpe_pretokenize,
        "bpe_tokenizer_json.json": generate_bpe_tokenizer_json,
        "bpe_normalizer.json": generate_bpe_normalizer,
        "bytelevel_decode_stream.json": generate_bytelevel_decode_stream,
        "unicode_forms.json": generate_unicode_forms,
        "bpe_added_tokens.json": generate_bpe_added_tokens,
        "bpe_added_token_flags.json": generate_bpe_added_token_flags,
        "bpe_no_op_settings.json": generate_bpe_no_op_settings,
        "bpe_fuse_unk.json": generate_bpe_fuse_unk,
        "bpe_added_token_coverage.json": generate_bpe_added_token_coverage,
        "bpe_continuing_prefix.json": generate_bpe_continuing_prefix,
        "bpe_sequence_split.json": generate_bpe_sequence_split,
        "bpe_split_behavior.json": generate_bpe_split_behavior,
        "bpe_split_literal.json": generate_bpe_split_literal,
        "bpe_duplicate_merge.json": generate_bpe_duplicate_merge,
        "bpe_no_split.json": generate_bpe_no_split,
        "wordpiece_added_tokens.json": generate_wordpiece_added_tokens,
        "bpe_prefix_space.json": generate_bpe_prefix_space,
    }
    for filename, gen in generators.items():
        payload = gen()
        path = ORACLE_DIR / filename
        # newline="\n": see this function's docstring for why.
        with path.open("w", encoding="utf-8", newline="\n") as f:
            # allow_nan=False: see this function's docstring for why.
            json.dump(payload, f, ensure_ascii=False, indent=1, allow_nan=False)
            f.write("\n")
        print(f"{filename}: {payload['metadata']['count']} cases -> {path}")


if __name__ == "__main__":
    main()
