# 0011 — Persisted artifacts are versioned JSON, written with `System.Text.Json`

**Status:** accepted · **Date:** 2026-08-05

## Context

There was no I/O in `src/` at all
([issue #58](https://github.com/CyrilB1531/data.net/issues/58)). Two things
followed, both pushing work onto the caller that the library is better placed to
do: a fitted `TfidfVectorizer` died with the process, and a pretrained
vocabulary had to be parsed by hand — for SentencePiece, that meant a protobuf.

Choosing a format is the decision the rest depends on, so it is made once, here,
and applies to fitted models and to loaded vocabularies alike.

Three constraints shaped it:

- **No `BinaryFormatter`, no polymorphic deserialization.** A loaded file is
  untrusted input. This also rules out the Python habit these APIs mirror:
  `pickle.load` executes arbitrary code by design.
- **`DataNet.Text` ships with no external dependencies**, and says so in the
  README. `Newtonsoft.Json`, `protobuf-net` and `MessagePack` are excluded.
- **The round trip must be bit-exact.** A reloaded model that produces scores
  "close to" the original is worse than one that fails, because nothing surfaces
  it.

## Decision

**JSON, one document per artifact, written and read with `System.Text.Json`.**

`System.Text.Json` is taken as a package on `netstandard2.0` and is in-box from
`net8.0` onwards. It is the single, deliberate exception to the no-dependency
rule, and it is confined to the two packages that ship artifacts —
`DataNet.Fuzzy` has no I/O and does not take it.

### Why not hand-roll the reader

Hand-rolling was the alternative that preserved the rule literally. It was
declined: a JSON reader for untrusted input is not a hundred lines, it is a
correct UTF-8 decoder, a correct number parser, depth accounting and a
surrogate-pair story — every one of them a place to be subtly wrong, on the
exact code path that reads hostile files.

The rule that matters is *native code where .NET has a gap*. Reading JSON is not
a gap; it has been in the box for four major versions. What is being avoided by
this exception is not a dependency but a hand-written parser with a security
boundary running through it. The minimal protobuf reader in
`SentencePieceModelLoader` is the counter-example that shows the line is real:
four wire types against a frozen format, small enough to audit, and there is no
in-box alternative at all.

### Why not a binary format as well

The issue allows a compact format only if a benchmark shows JSON is a real
bottleneck. That benchmark now exists — `bench/README.md` §4 — and it did show
one, so this section records what was measured and what was done about it.

Against scikit-learn plus `pickle`, on a `TfidfVectorizer` fitted to 5 000
documents, the first measurement had DataNet **losing both directions**: saving
took 1.66× what `pickle.dumps` took, loading 2.25× what `pickle.loads` took.

The answer was not a second format. Profiling the load path put the cost
somewhere more specific than "JSON":

| Phase of a 30 000-feature load | Cost |
| --- | --- |
| Parsing 30 000 idf values written as JSON numbers | 1.95 ms |
| Materialising the 30 000 vocabulary strings | 0.50 ms |
| Tokenising the document at all | 3.80 ms |

The numeric vector was four times more expensive than the vocabulary it sat
beside, and it inflated the token count that made tokenisation itself expensive.
So **only the idf vector left readable JSON** (see below); everything a human
reads stayed exactly as it was. That, with the encoder and buffer changes
recorded below, moved both directions to parity or better without a second
format existing anywhere in the codebase.

Two formats to keep bit-exact remains the cost this avoids. The door is not
closed — it is simply not paid for yet.

### The header

Every artifact opens with the same two properties:

```json
{"$schema": "datanet/tfidf-vectorizer", "version": 1, "…": "…"}
```

`$schema` names the artifact kind, so loading a `count-vectorizer` file as a
TF-IDF model fails with a message that says so, rather than by missing property.
`version` is numbered **per artifact**: adding a field to the TF-IDF artifact
does not invalidate a saved `HashingVectorizer`. Only artifacts DataNet writes
carry a header — a pretrained vocabulary is read from a foreign format and never
written back, so the loaders have none to check. A persisted artifact outlives
the library version that wrote it, so the number is present from the first
commit rather than added when it is first needed — by then it is too late.

**Unknown properties are rejected.** The tempting default is to ignore them, on
the grounds of forward compatibility. That gets it backwards: a file written by
a newer build carries fields this build does not apply, and silently ignoring
them produces a model that is quietly not the one that was saved. Failing names
the property.

### Doubles

A single double — an idf option, a threshold — is written as a JSON number. From
`net8.0` that is the shortest form that still round-trips, which .NET Core 3.0
made exact and which is about a quarter shorter than `"G17"`. A
`netstandard2.0` build may run on .NET Framework, where that guarantee does not
hold, so it keeps `"G17"`: exact everywhere, at the cost of longer numbers. The
two builds write different bytes for the same model; both read back identically,
and each is byte-reproducible against itself, which is what the artifact contract
promises.

Non-finite values are refused on write. JSON has no representation for `NaN` or
infinity, and a model containing one is broken before it reaches the file.

Raw bits, unlike JSON numbers, carry `NaN` and infinity perfectly well — so the
idf vector below is checked on **both** sides rather than relying on the format
to make them unrepresentable. An infinite idf weight turns every later
`Transform` into `NaN` scores, silently and a long way from the file that caused
it.

### The idf vector is base64, and the vocabulary is not

The idf vector is written as one base64 string of raw little-endian IEEE-754
bits. The vocabulary beside it stays a plain JSON array of strings.

That asymmetry is the point. Debuggability is what this format buys, and it is
bought for the parts a person actually reads: which features were learned, what
options were fitted, which schema and version. Nobody reads thirty thousand
floats by eye. Measured, that vector was the single most expensive thing in the
file — 1.95 ms to parse against 0.50 ms for the whole vocabulary — and it made
the artifact a quarter larger, which slowed the tokeniser down again on the way
past.

Three things improve rather than degrade by moving it:

- **Exactness.** Raw bits round-trip by construction. No decimal formatter is
  involved, so the `"G17"` question above simply does not arise for the vector
  that has thirty thousand chances to get it wrong.
- **Size.** 782 KB to 589 KB on a 30 000-feature artifact.
- **Speed.** Saving fell from 9.05 ms to 1.95 ms, loading from 12.31 ms to
  5.85 ms.

The security line holds: decoding is `Utf8JsonReader.GetBytesFromBase64`, the
same library that parses the rest of the file, not a hand-written decoder. The
idf vector's decoded length is checked against `MaxArrayLength` and refused
unless it is a whole number of 64-bit values. That element-count bound is
specific to a vocabulary-scale array: `EmbeddingIndex`'s vector block reuses the
same base64 helper but is bounded by `MaxTotalBytes` before parsing instead,
because a million floats is a small index and a million tokens is a large
vocabulary.

### Escaping

Artifacts are written with `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`.

The default encoder escapes every non-ASCII character as `\uXXXX` — six bytes
where UTF-8 needs two — and escapes characters JSON never required, which is why
a token pattern came out of an early build as `\b\w\w+\b`. A vectorizer's
vocabulary is precisely where non-ASCII lives: this library ships Snowball
stop-word lists for French, German, Italian, Portuguese and Spanish, 258 of
whose entries are accented, so this is the ordinary case and not an edge one.

"Unsafe" names an HTML-injection concern. The default encoder exists so that JSON
can be dropped into a `<script>` block without escaping it again; an artifact is
read back by this library's own parser and is never embedded in a page, so that
protection buys nothing here while costing size on every accented token. The
escaping JSON itself requires — quotes, backslashes, control characters — is
still applied.

Measured on a 7 200-feature accented vocabulary: 9 201 `\uXXXX` sequences fell to
zero, the artifact shrank 18%, saving got 2.09× faster and loading 1.84×.

### Bounds

`ArtifactLoadOptions` caps vocabulary size (1 000 000), token length (1024),
JSON depth (32), total bytes (256 MiB) and array length (1 000 000). Every count
read from a file sizes a buffer, so every count is checked before it is used.
Exceeding a limit raises `InvalidDataException` naming both the limit and the
offending value; a JSON syntax error is restated as the same exception type, so
callers do not have to catch two depending on whether the file broke the grammar
or the schema.

`featureCount` is written **before** the array it describes, so a reader can size
from a value it has already checked rather than growing a list at the file's
discretion.

The type is declared separately in `DataNet.Text.Persistence` and
`DataNet.Embeddings.Persistence` rather than shared. Sharing would need either a
package reference between the two — which would make `DataNet.Embeddings` depend
on a *published* `DataNet.Text` that does not yet contain the type — or one
public type compiled into both assemblies, which is an ambiguous reference for
anyone consuming both packages. Two small records is the cheaper of the three.

### API shape

`Save(Stream)`, `Save(string path)`, `static Load(Stream, ArtifactLoadOptions?)`,
`static Load(string path, ArtifactLoadOptions?)`, and native async counterparts.

`Load` is static because a half-constructed object waiting to be filled in is not
a thing this library should hand out. A stream passed in is **never disposed** —
the caller owns it; the `path` overloads own the `FileStream` they open. Output
is UTF-8 without a byte-order mark.

## Consequences

- `DataNet.Text` and `DataNet.Embeddings` declare `System.Text.Json` in their
  `netstandard2.0` dependency group. `tools/check_nuspec_dependencies.py`
  asserts exactly that, so the edge cannot appear or vanish unnoticed, and
  `THIRD-PARTY-NOTICES.md` records it.
- The `net10.0` packages remain dependency-free, so the README's claim stays true
  where most consumers read it — but it now needs the qualifier, and has it.
- `CsrMatrix`'s public constructor validates its arrays. Deserialization turns a
  malformed structure from a caller-discipline problem into an out-of-bounds
  read, so the boundary is where the arrays enter the type.
- A second artifact type is cheap to add: `EmbeddingIndex` persistence
  is a body writer and a body reader on top of this header.
