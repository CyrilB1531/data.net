namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// A <c>Split</c> step of a <c>Sequence</c> pre-tokenizer: the pattern, what to do
/// with the text around its matches, and whether the two roles are swapped.
/// </summary>
/// <param name="Pattern">The regex the step declares: <c>pattern.Regex</c> as written, or <c>pattern.String</c>'s literal escaped into the regex matching exactly it.</param>
/// <param name="Behavior">The step's <c>behavior</c>.</param>
/// <param name="Invert">Swaps match and gap; see <c>docs/equivalence.md</c>'s <c>Split(pattern, behavior=…, invert=…)</c> row for which <see cref="SplitBehavior"/> values that is a no-op for.</param>
/// <remarks>
/// The three travel together because the reference requires all three: a
/// <c>tokenizer.json</c> whose <c>Split</c> step omits <c>behavior</c> or
/// <c>invert</c> is refused with <c>missing field</c>, so none of them has a
/// default or is meaningful alone.
/// </remarks>
public sealed record BpeSplitStep(string Pattern, SplitBehavior Behavior, bool Invert);
