namespace Lodestar.Text.Keywords;

/// <summary>One extracted phrase and the score that ranked it.</summary>
/// <param name="Phrase">The phrase, as the extractor assembled it.</param>
/// <param name="Score">Higher is better. The scale is the extractor's, and is not comparable across extractors.</param>
public readonly record struct KeywordMatch(string Phrase, double Score);
