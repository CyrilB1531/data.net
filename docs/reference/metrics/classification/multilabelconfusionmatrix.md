# MultilabelConfusionMatrix

One 2×2 [`ConfusionMatrix`](confusionmatrix.md) per label — each label counted against everything
else, which is what a per-class score is built from before it is divided into one.

**A stack of the existing type, not a new one.** Each entry *is* a two-label matrix, so everything
that reads one reads these: [`Recall.Score`](recall-score.md) over the entry for class 1 gives class
1's recall, and a test asserts exactly that. Wrapping the array in a type of its own would have
bought a name and cost that.

Each entry's labels are `0` and `1` in that order, which puts the cells where the reference puts
them: `[0, 0]` true negative, `[0, 1]` false positive, `[1, 0]` false negative, `[1, 1]` true
positive.

## Per label, or per sample

`samplewise` turns the count on its side: one matrix per **row**, counting that row's labels rather
than each label's samples. On two samples over three labels it returns two matrices instead of
three.

The reference offers it on a label matrix only, and refuses it on single-label input with
"Samplewise metrics are not available outside of multilabel classification". Here that refusal is
structural rather than checked — `samplewise` is a parameter of the matrix overload and does not
exist on the other, so the call a caller must not make cannot be written.

## Members

| Member | What it does |
| --- | --- |
| [`MultilabelConfusionMatrix.Compute`](multilabelconfusionmatrix-compute.md) | The stack, from labels or from a matrix. |
