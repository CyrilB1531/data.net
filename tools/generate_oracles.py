#!/usr/bin/env python3
"""Generate frozen oracle corpora for the DataNet.Text test suite.

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
import json
import math
import sys
import warnings
from importlib.metadata import version
from pathlib import Path

# CONTRIBUTING runs this with PYTHONSAFEPATH=1, which keeps the script's own
# directory off sys.path, so the shared drawer has to be pointed at by hand.
# Appended rather than prepended: nothing in tools/ may shadow an installed
# package, which is the safeguard PYTHONSAFEPATH is there to provide.
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
CAT_SENTENCE = "the cat sat on the mat"
HELLO_WORLD = "hello world"
TINY_SP_MODEL = "tiny_sp.model"
EMBEDDING_SENTENCE = "tokenization is embedding embeddings"
XLMR_FAIRSEQ_MODEL = "xlmr_fairseq.model"

# Code-point ranges per category. Surrogates (0xD800..0xDFFF) are filtered out.
RANGES = {
    "ascii": [(0x20, 0x7E)],
    "latin": [(0x20, 0x7E), (0xC0, 0x17F)],
    "bmp": [(0x20, 0x7E), (0xC0, 0x2AF), (0x370, 0x52F), (0x4E00, 0x9FFF)],
    "supplementary": [(0x1F300, 0x1FAFF), (0x10000, 0x1052F)],
}


# Twelve significant digits, not the full float64 repr, for anything a BLAS
# kernel reduced. numpy and scikit-learn sum in whatever order the SIMD kernel
# scipy-openblas selects for the host CPU chooses, so the last bits of such a
# value describe the machine that ran the generator rather than the metric —
# committing them turns the drift gate into a hardware check, which is what
# issue #97 is about.
#
# Significant digits rather than decimal places because the spread is always at
# the last bit and therefore scales with the value: it measured ~1e-13 on
# accuracy_count (~413) and ~1e-16 on the knn scores (~0.4), which is the same
# sixteenth digit in both. Twelve leaves four orders of margin above it, and
# costs at most 5e-13 against the tolerances the tests compare with — 1e-9 for
# the metrics corpus, 1e-4f for the knn one.
STABLE_DIGITS = 12


def stable(value) -> float:
    """A float the corpus can commit: rounded away from the host's last bits."""
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
    """Yield (category, a, b) tuples covering the corpus design."""
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
        # Appended last so every existing case keeps its id and value: the RNG is
        # consumed in order. These exist because the "long" family above draws from
        # BMP ranges, so its patterns contain CJK and never reach the Latin-1
        # bit-parallel path — the blocked Myers code had no coverage at all until
        # long ASCII/Latin pairs were added.
        ("long_ascii", RANGES["ascii"], 60, (80, 400), (5, 40)),
        ("long_latin", RANGES["latin"], 60, (80, 400), (5, 40)),
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
    its Rust core — not NFC normalization, not byte-level). DataNet implements the
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
    """Standard Jaro similarity over code points (matches DataNet's Jaro core).

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
    # qval=1 (textdistance default) over non-empty pairs: textdistance raises on
    # empty operands (its own edge quirk); DataNet defines those separately and
    # covers them via unit tests. Multiset (bag) semantics.
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


# --- Additional Snowball languages -----------------------------------------------
# Word lists target that language's suffix families, plus short and irregular
# words that exercise the region (RV/R1/R2) boundaries.

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
    """Freeze nltk's Snowball output for one language into an oracle payload."""
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
    """A HuggingFace WordPiece tokenizer with the pipeline DataNet reproduces."""
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
    unigram.add_special_tokens(["<unk>", "<s>", "</s>"])
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
    helpers: the proto is the format DataNet's loader claims to read, so it is the
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


# Text that names the markers themselves, which is the whole point: a piece only
# ever matches where its literal characters occur, so an input without "<" in it
# cannot tell a tokenizer that excludes the control pieces from one that does
# not. The rest is ordinary multilingual text — the vocabulary is XLM-R's, and a
# fixture that only ever saw Latin script would leave most of it unexercised.
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
    "<mask>",
    "un texte avec <s>, </s>, <pad>, <unk> et <mask> dedans",
    # Since #75 the fixture carries XLM-R's own nmt_nfkc charsmap, so these are
    # no longer inert: each one is rewritten before it is segmented. Written with
    # escapes where the character is invisible or easy to normalise by accident in
    # an editor.
    "\uff2c\uff25 \uff32\uff25\uff2e\uff21\uff32\uff24 \uff52\uff41\uff50\uff49\uff44\uff45",  # full-width LE RENARD rapide
    "\ufb01nancier, \ufb02amme et \u0153uvre",  # fi and fl ligatures
    "cafe\u0301 de\u0301ja\u0300 vu",  # decomposed accents, which nmt_nfkc recomposes
    "\u2168 siecles, \u2460\u2461\u2462 etapes",  # roman numeral IX, circled digits
    "espace\u00a0insecable et espace\u3000ideographique",
    "un\u0001texte\u0002avec\u0007des controles",
]

# The five strings a vocabulary in this layout must never segment onto.
XLMR_MARKERS = ["<s>", "<pad>", "</s>", "<unk>", "<mask>"]


def generate_xlmr_fairseq() -> dict:
    """Freeze sentencepiece's encoding of the XLM-R vocabulary in fairseq layout.

    The fixture is built by tools/fetch_xlmr_vocab.py: XLM-R's own 250 000
    pieces and scores, at the ids HuggingFace gives them, with <s>=0, <pad>=1,
    </s>=2, <unk>=3 and <mask>=250001 typed CONTROL/UNKNOWN, and the normalizer
    set to identity — the pipeline DataNet reproduces. See that script for why
    the stock sentencepiece.bpe.model cannot be replayed directly.

    This is the corpus the id-based control filter could not have passed: every
    marker sits outside 0-2 except <s>, and <mask> sits 250 000 ids away from
    where the guess looked.
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
    # Spot-checked pieces rather than all 250 002: the vocabulary itself is the
    # committed .model, and repeating it as JSON would double a 5 MB fixture to
    # prove nothing the file does not already say.
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


# --- Batch encoding and batched embedding (issue #60) ----------------------------
#
# The chain `tokenize -> insert specials -> truncate -> pad -> infer -> pool` is
# frozen in two halves, because the two admit very different standards of proof.
#
# The tokenization half is integers taken from HuggingFace `tokenizers` with the
# post-processor and padding enabled, i.e. from the library the C# reproduces. The
# replay compares them for *equality*: an id is right or it is not, and a
# tolerance would only hide an off-by-one in the template.
#
# The embedding half is computed below in float64 from the same table that
# `tools/build_tiny_models.py` bakes into `tiny_embedder.onnx`, whose only node is
# a Gather. The reference is therefore the arithmetic the model performs, worked
# out independently, and not a second copy of the C# code — which is the only
# version of it worth freezing.
#
# The C# side does not replay that half to 1e-9. ONNX Runtime hands back float32
# and the pooled vector is normalized in float32, so agreement with an exact
# reference is bounded by the float32 epsilon, near 1e-7 relative, and by nothing
# this repository can improve. Demanding 1e-9 would mean reproducing the C#
# rounding sequence in numpy, at which point the corpus is a mirror and catches
# nothing. What *is* asserted exactly lives in the C# suite, where it belongs:
# the ids and the mask above, the equality of a batched vector with the
# single-sequence vector for the same text, and the equality of the vectorized
# net10.0 result with the scalar netstandard2.0 one.

# Appended after the WordPiece vocabulary rather than placed at the front, where
# BERT keeps them. Nothing may assume `[CLS]` is id 101, or id 0, or even that the
# special tokens are contiguous with each other: the template names a token and
# the vocabulary is what answers with an id.
CLS_TOKEN = "[CLS]"
SEP_TOKEN = "[SEP]"
PAD_TOKEN = "[PAD]"
BATCH_VOCAB = [*WORDPIECE_VOCAB, CLS_TOKEN, SEP_TOKEN, PAD_TOKEN]

# Mirrors tools/build_tiny_models.py. Duplication rather than an import, because
# that script runs in a virtualenv carrying `onnx` and this one in a virtualenv
# carrying scikit-learn, and neither has the other's dependency. The table is
# frozen into the corpus and a C# test gathers a row through the ONNX model and
# compares it, so the two copies cannot drift apart in silence.
EMBEDDING_ROWS = 64
EMBEDDING_DIM = 4

BATCH_MAX_LENGTH = 8

# Chosen so the four documented edges fall out of the same batch under
# BATCH_MAX_LENGTH: nothing, one token, exactly the limit, one over it. The
# generator asserts each of those below rather than trusting this comment, so a
# vocabulary change that quietly stops exercising an edge fails here instead of
# passing a test that has become vacuous.
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
    """
    import numpy as np  # noqa: PLC0415

    table = np.zeros((EMBEDDING_ROWS, EMBEDDING_DIM), dtype=np.float64)
    for i in range(EMBEDDING_ROWS):
        for d in range(EMBEDDING_DIM):
            table[i, d] = (((7 * i + 13 * d) % 64) - 32) / 64.0
    return table


def _batch_tokenizer(vocab: dict[str, int], max_length: int | None):
    """A HuggingFace tokenizer configured the way `BatchEncoder` configures itself."""
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

    A vocabulary or template change can leave these texts encoding to lengths
    that no longer straddle the limit. The test replaying them would still pass,
    having quietly become a test of nothing.
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


# What normalization changes and `identity` hides, per the acceptance criteria of
# #75: width forms, composition, ligatures, whitespace of every flavour, control
# characters, case — plus the three rules only `custom_norm.model` performs.
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

# Each fixture carries a different charsmap, which is the point: the same
# interpreter must handle all of them. tiny_sp.model is in the list as the
# control case — no charsmap at all.
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
            # The same blob as tokenizers writes into a tokenizer.json, in the same
            # encoding, so the JSON loader can be tested against a real map without
            # a hand-pasted constant. Only for this fixture: base64 of the nmt_nfkc
            # map would add 300 KB to the corpus to say nothing new.
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


# --- Classification metrics (issue #61) --------------------------------------
#
# Fixtures target the cases where implementations actually diverge rather than
# average behaviour: a class that is never predicted, a class absent from the
# truth, a labels= subset (which drops samples and turns the report's accuracy
# row into a micro-avg row), and non-contiguous label values that catch any
# implementation assuming 0..k-1. Each fixture is emitted twice, unweighted and
# weighted, because sample_weight changes the dtype of every count upstream.

METRIC_SEED = SEED + 61
ZERO_DIVISIONS = (0, 1)
BETAS = (0.5, 2.0)
REPORT_DIGITS = (2, 3)


def _metric_fixtures() -> list[dict]:
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

    return fixtures


def _binary_average_applies(observed: list[int], pos_label: int) -> bool:
    """Mirror scikit-learn's own admissibility rule for average="binary"."""
    if len(observed) > 2:
        return False
    return pos_label in observed or len(observed) < 2


def _metric_case(fx: dict, weighted: bool) -> dict:
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
                "sklearn.metrics.precision_recall_fscore_support",
                "sklearn.metrics.fbeta_score",
                "sklearn.metrics.classification_report",
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


def main() -> None:
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
        "roc_auc.json": generate_roc_auc,
    }
    for filename, gen in generators.items():
        payload = gen()
        path = ORACLE_DIR / filename
        with path.open("w", encoding="utf-8") as f:
            json.dump(payload, f, ensure_ascii=False, indent=1)
            f.write("\n")
        print(f"{filename}: {payload['metadata']['count']} cases -> {path}")


if __name__ == "__main__":
    main()
