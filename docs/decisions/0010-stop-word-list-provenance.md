# 0010 — Stop-word lists come from Snowball, not from the `nltk` corpus

**Status:** accepted · **Date:** 2026-08-05

## Context

`DataNet.Text` ships one stop-word list: scikit-learn's 318-word English set,
for `stop_words="english"` parity. Six Snowball stemmers now cover English,
French, German, Italian, Portuguese and Spanish, so the missing lists are the
obvious follow-up ([issue #13](https://github.com/CyrilB1531/data.net/issues/13)).

Two sources are candidates, and they are not interchangeable.

**`nltk`** is the parity reference for the stemmers ([`0008`](0008-italian-enza-nltk-divergence.md)),
which makes `nltk.corpus.stopwords` the natural target. Its `README` states the
lists were obtained from PostgreSQL's copy of the Snowball stop words, with
Romanian from `arlc.ro` and later community edits (English augmented, German
corrected, Kazakh, Nepali, Azerbaijani, Greek and Indonesian added). But the
`nltk_data` repository classifies the `stopwords` package under **"Unclarified,
Unknown, Ambiguous, or Citation-Only"** in `DATASET-LICENSES.md`: the package
carries no `license` attribute in `index.xml`, and its `webpage` attribute reads
`ftp://ftp.cs.cornell.edu/pub/smart/english.stop and http://snowball.tartarus.org/
and others`. `LICENSE-OVERVIEW.md` is explicit that the repository-wide
Apache-2.0 licence governs the repository, **not** the individual data packages,
and warns against redistribution of the unclarified ones. So the Apache-2.0
licence on `nltk` — recorded in `THIRD-PARTY-NOTICES.md` for oracle generation —
does not extend to this corpus.

**Snowball** publishes its own stop-word lists at
`https://snowballstem.org/algorithms/<language>/stop.txt`, and the site's licence
page reads: "Except where explicitly noted, all the software given out on this
Snowball site is covered by the 3-clause BSD License", © 2001 Dr Martin Porter,
© 2002 Richard Boulton. BSD-3-Clause is compatible with Apache-2.0 and needs only
that the copyright notice and licence terms travel with the redistribution.

This is also the upstream of the `nltk` lists, so the two are close but not
equal. Measured, word for word:

| Language | Snowball | `nltk` | Only in Snowball | Only in `nltk` |
| --- | ---: | ---: | ---: | ---: |
| French | 154 | 157 | 13 | 16 |
| German | 231 | 232 | 4 | 5 |
| Italian | 279 | 279 | 0 | 0 |
| Portuguese | 203 | 207 | 0 | 4 |
| Spanish | 308 | 313 | 2 | 7 |
| English | 174 | 198 | 15 | 39 |

The gap is a snapshot gap, not a disagreement: `nltk` froze PostgreSQL's copy —
which is itself a download of these same files — and Snowball has since added
words (`ceci`, `cela`, `quel`, `sans` in French), while `nltk` accepted edits of
its own (`dass` in German, `estar`/`haver`/`ser`/`é` in Portuguese, the
`sentir` forms in Spanish). English is the outlier: `nltk`'s list was
deliberately augmented with contractions and negation forms
([`nltk_data` issue #22](https://github.com/nltk/nltk_data/issues/22)), and its
recorded origin includes the Cornell SMART list, whose terms are not stated.

## Decision

- **Ship the Snowball lists**, for the five non-English languages that already
  have a Snowball stemmer: French, German, Italian, Portuguese, Spanish. They
  are BSD-3-Clause, from a named copyright holder, and are the upstream that the
  alternative merely mirrors.
- **Do not vendor the `nltk` corpus**, in whole or in part — not the augmented
  English list, not the corrected German one, not the Romanian list from
  `arlc.ro`. This is a licensing conclusion, not a quality judgement.
- **`StopWords.English` stays scikit-learn's 318-word list.** It is BSD-3-Clause
  and it is what `stop_words="english"` gives a migrating user; replacing it with
  Snowball's 174-word English list would break a documented parity guarantee to
  gain nothing. Snowball English is therefore not shipped: the language is
  already served.
- **Pin the source.** The five files were retrieved on 2026-08-05 and their
  SHA-256 recorded in `tools/fetch_stopwords.py`, which regenerates
  `StopWords.Snowball.cs` from them. Vendoring is reproducible and auditable
  rather than typed in by hand.
- **Attribute.** Snowball is added to `NOTICE` and to the shipped-components
  table of `THIRD-PARTY-NOTICES.md` — it is the first *resource* the packages
  redistribute, as opposed to a dependency they reference.

## Consequences

- For French, German, Portuguese and Spanish, `DataNet.Text` removes a slightly
  different set of tokens than `nltk` would. `docs/equivalence.md` records this
  as a known divergence, with the per-language counts above. Italian is
  identical. This is the one place where the library deliberately does *not*
  match `nltk`, and [`0008`](0008-italian-enza-nltk-divergence.md) is the reason
  it needs saying out loud: parity is the contract everywhere else.
- A caller who needs exactly `nltk`'s behaviour keeps a clean route —
  `StopWords` accepts any `IReadOnlyCollection<string>`, so the corpus can be
  loaded from the caller's own machine, under whatever terms the caller accepts.
  That decision is theirs to make; redistributing it is what we cannot do.
- The remaining languages of the `nltk` corpus (Arabic, Russian, Dutch, …) stay
  out until either a stemmer justifies them or a clean source is found. Adding a
  language means finding it on snowballstem.org, not copying it from `nltk`.
- If Snowball updates a list, the SHA-256 check in `tools/fetch_stopwords.py`
  fails loudly instead of silently producing a different library. Refreshing is
  then a deliberate act: new checksum, regenerated file, updated counts in the
  tests.
