using Lodestar.Text.Phonetics;

namespace Lodestar.Sample;

/// <summary>The one encoder here that also decides whether two codes match.</summary>
internal static class MatchRatingApproachSample
{
    public static void Run()
    {
        Console.WriteLine($"  MatchRatingApproach.Codex(Byrne)     = {MatchRatingApproach.Codex("Byrne")}");
        Console.WriteLine($"  MatchRatingApproach.Compare(Byrne, Boern) = {MatchRatingApproach.Compare("Byrne", "Boern")}");
        Console.WriteLine($"  MatchRatingApproach.Compare(Tim, Timothy) = {MatchRatingApproach.Compare("Tim", "Timothy")}");
    }
}
