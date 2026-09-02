namespace Lodestar.Text.Indexing;

/// <summary>One hit from a <see cref="BkTree"/> query: the indexed item and its distance
/// to the query.</summary>
/// <remarks>
/// <see cref="Distance"/> is the value the tree's own metric returned, not a normalized
/// score — a caller comparing it against a similarity in <c>[0, 100]</c> is comparing two
/// different quantities.
/// </remarks>
public readonly record struct BkTreeMatch(string Item, int Distance);
