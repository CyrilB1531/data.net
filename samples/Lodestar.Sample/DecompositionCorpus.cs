using Lodestar.Abstractions;

namespace Lodestar.Sample;

/// <summary>The one term-document matrix every decomposition sample factorizes.</summary>
/// <remarks>
/// A helper, not a sample: it names no public type of its own. Three files used to carry
/// a copy of these arrays under a comment claiming they were identical — duplication only
/// SonarCloud can see, and an invariant nothing enforced. Now the SVD and the NMF really
/// do factorize the same block, so their answers can be read against each other.
/// </remarks>
internal static class DecompositionCorpus
{
    // Five documents over six terms, built by hand so the shape is readable: terms 0-2
    // belong to one subject and terms 3-5 to another, with document 2 straddling both.
    private static readonly double[] Values =
        [2.0, 1.0, 3.0, 1.0, 2.0, 1.0, 1.0, 1.0, 2.0, 1.0, 3.0, 2.0, 1.0, 2.0];
    private static readonly int[] Columns = [0, 1, 0, 2, 1, 2, 3, 4, 3, 5, 0, 2, 4, 5];
    private static readonly int[] Rows = [0, 2, 4, 8, 10, 14];

    /// <summary>A fresh matrix over the shared arrays: five documents, six terms.</summary>
    public static CsrMatrix Documents() => new(5, 6, Values, Columns, Rows);
}
