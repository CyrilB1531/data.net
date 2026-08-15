# Lcs

Not a score at all: the raw length of the longest run two strings have in common, which is the
building block several of the measures above are made of.

The longest run the two have in common, in two senses that answer different questions:
`SubstringLength` requires it to be contiguous, `SubsequenceLength` only that it keeps its order.
The second is what `Indel` is built on, so a subsequence length and an indel distance are the same
measurement seen twice.

## Members

| Member | What it does |
| --- | --- |
| [`Lcs.SubsequenceLength`](lcs-subsequencelength.md) | Returns the length of the longest sequence of characters that appears in both inputs in the same |
| [`Lcs.SubstringLength`](lcs-substringlength.md) | Returns the length of the longest **contiguous** run of characters that appears in both inputs. |
