namespace DataNet.Text.Benchmarks;

/// <summary>
/// Locates the generated benchmark corpus, and refuses to let a run proceed
/// without it.
/// </summary>
/// <remarks>
/// Producing numbers from a missing or half-written corpus is the failure this
/// prevents: both language sides must read the same files for the comparison to
/// mean anything, so an absent corpus is an error rather than a reason to skip.
/// </remarks>
public static class BenchCorpus
{
    /// <summary>Every file <c>generate_vocabs.py</c> writes.</summary>
    public static readonly string[] RequiredFiles =
    [
        "vocab_30k.txt",
        "tokenizer_30k_wordpiece.json",
        "tokenizer_30k_unigram.json",
        "spiece_30k.model",
        "documents.json",
    ];

    /// <summary>The repository root, found by walking up from the working directory.</summary>
    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (File.Exists(System.IO.Path.Combine(dir.FullName, "DataNet.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            "Could not find the repository root (no DataNet.slnx in any parent of " +
            $"'{Directory.GetCurrentDirectory()}'). Run from inside the repository.");
    }

    /// <summary>The corpus directory, verified complete.</summary>
    public static string VocabsDirectory()
    {
        string dir = System.IO.Path.Combine(RepoRoot(), "bench", "corpus", "vocabs");
        foreach (string file in RequiredFiles)
        {
            if (!File.Exists(System.IO.Path.Combine(dir, file)))
            {
                throw new InvalidOperationException(
                    $"The benchmark corpus is missing '{file}' in '{dir}'. " +
                    "Generate it first: python bench/corpus/generate_vocabs.py");
            }
        }
        return dir;
    }

    /// <summary>Full path to one corpus file.</summary>
    public static string Path(string fileName) =>
        System.IO.Path.Combine(VocabsDirectory(), fileName);

    /// <summary>Bytes of one corpus file.</summary>
    public static byte[] Read(string fileName) => File.ReadAllBytes(Path(fileName));
}
