namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// A <c>Split</c> step of a <c>Sequence</c> pre-tokenizer: the pattern, what to do
/// with the text around its matches, and whether the two roles are swapped.
/// </summary>
/// <param name="Pattern">The regex the step declares, from <c>pattern.Regex</c>.</param>
/// <param name="Behavior">The step's <c>behavior</c>.</param>
/// <param name="Invert">
/// The step's <c>invert</c>. It swaps the roles of match and gap, and nothing else:
/// measured against <c>tokenizers</c> 0.23.1, it is a no-op for
/// <see cref="SplitBehavior.Isolated"/> and <see cref="SplitBehavior.Contiguous"/>
/// and exchanges <see cref="SplitBehavior.MergedWithPrevious"/> with
/// <see cref="SplitBehavior.MergedWithNext"/>.
/// </param>
/// <remarks>
/// The three travel together because the reference requires all three: a
/// <c>tokenizer.json</c> whose <c>Split</c> step omits <c>behavior</c> or
/// <c>invert</c> is refused with <c>missing field</c>, so there is no default and
/// no state in which one of them is meaningful without the others.
/// </remarks>
public sealed record BpeSplitStep(string Pattern, SplitBehavior Behavior, bool Invert);
