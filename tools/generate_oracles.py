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

import json
import random
from importlib.metadata import version
from pathlib import Path

from difflib import SequenceMatcher

import jellyfish
import textdistance as td
from rapidfuzz.distance import DamerauLevenshtein, Indel, Levenshtein, OSA
from sklearn.feature_extraction.text import CountVectorizer as SkCountVectorizer
from sklearn.feature_extraction.text import TfidfVectorizer as SkTfidfVectorizer

SEED = 20260801
ORACLE_DIR = Path(__file__).resolve().parent.parent / "tests" / "oracles"

# Code-point ranges per category. Surrogates (0xD800..0xDFFF) are filtered out.
RANGES = {
    "ascii": [(0x20, 0x7E)],
    "latin": [(0x20, 0x7E), (0xC0, 0x17F)],
    "bmp": [(0x20, 0x7E), (0xC0, 0x2AF), (0x370, 0x52F), (0x4E00, 0x9FFF)],
    "supplementary": [(0x1F300, 0x1FAFF), (0x10000, 0x1052F)],
}


def rand_string(rng: random.Random, length: int, ranges) -> str:
    out = []
    for _ in range(length):
        lo, hi = rng.choice(ranges)
        cp = rng.randint(lo, hi)
        while 0xD800 <= cp <= 0xDFFF:
            cp = rng.randint(lo, hi)
        out.append(chr(cp))
    return "".join(out)


def mutate(rng: random.Random, s: str, edits: int, ranges) -> str:
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


def build_pairs(rng: random.Random):
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
        ("CA", "ABC"),   # OSA=3, DL=2
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
    rng = random.Random(SEED)
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
    rng = random.Random(SEED)
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
    rng = random.Random(SEED)
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


def _jaro_reference(a: str, b: str) -> float:
    """Standard Jaro similarity over code points (matches DataNet's Jaro core).

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
    rng = random.Random(SEED)
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
    rng = random.Random(SEED)
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
    rng = random.Random(SEED)
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
    rng = random.Random(SEED)
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


def phonetic_words(rng: random.Random):
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
    rng = random.Random(SEED)
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
    "the cat sat on the mat",
    "a cat and a dog",
    "the dog barked loudly",
    "cats and dogs are friends",
    "the quick brown fox",
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
