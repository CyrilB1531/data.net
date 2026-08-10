using System.Text;

namespace DataNet.Text.Stemming;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// SonarLint S3267: the suffix scans early-return and mutate in place, which Where cannot express — and they run per token.
// CA1845 (use span-based string.Concat): that overload does not exist on
// netstandard2.0. The Substring form is what makes this file compile there.
#pragma warning disable CA1845
// CA1308 (normalize to uppercase): Snowball, Porter and WordPiece are *defined*
// on lowercase input — the published algorithms, the reference implementations
// and the oracle corpora this suite is checked against all lowercase first.
// ToUpperInvariant would return different stems, which is a wrong answer rather
// than a differently-cased one.
#pragma warning disable S3776, S3267, CA1308

/// <summary>
/// The French Snowball stemming algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Reference behavior: <c>nltk.stem.snowball.SnowballStemmer("french")</c>. An
/// original implementation of the published Snowball algorithm, using the RV/R1/R2
/// regions and the standard step ordering. Input is lowercased. Thread-safe.
/// </para>
/// </remarks>
public static class FrenchSnowballStemmer
{
    /// <summary>Returns the French Snowball stem of <paramref name="word"/>.</summary>
    public static string Stem(string word)
    {
        Guard.NotNull(word);
        // Compose accents (NFC) so 'è' etc. are single code points, as the rules expect.
        string s = word.ToLowerInvariant().Normalize(NormalizationForm.FormC);
        if (s.Length < 2)
        {
            return s;
        }
        return new Worker(s).Run();
    }

    private sealed class Worker
    {
        private string _s;
        private readonly int _rv;
        private readonly int _r1;
        private readonly int _r2;
        private bool _step1RemovedMent;

        public Worker(string s)
        {
            _s = MarkNonVowels(s);
            _r1 = Region(_s, 0);
            _r2 = Region(_s, _r1);
            _rv = ComputeRv(_s);
        }

        public string Run()
        {
            // Control flow is driven by whether a step actually ALTERED the word,
            // not merely matched a suffix (Snowball semantics).
            string before = _s;
            Step1();
            bool step1 = _s != before;

            bool step2 = false;
            if (!step1 || _step1RemovedMent)
            {
                before = _s;
                Step2a();
                step2 = _s != before;
                if (!step2)
                {
                    before = _s;
                    Step2b();
                    step2 = _s != before;
                }
            }

            if (step1 || step2)
            {
                Step3();
            }
            else
            {
                Step4();
            }

            Step5();
            Step6();
            return Unmark(_s);
        }

        private static bool IsVowel(char c) =>
            c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y'
            or 'â' or 'à' or 'ë' or 'é' or 'ê' or 'è' or 'ï' or 'î' or 'ô' or 'û' or 'ù';

        private static string MarkNonVowels(string s)
        {
            char[] a = s.ToCharArray();
            for (int i = 0; i < a.Length; i++)
            {
                char c = a[i];
                bool prevVowel = i > 0 && IsVowel(s[i - 1]);
                bool nextVowel = i + 1 < a.Length && IsVowel(s[i + 1]);
                if (c == 'u' && i > 0 && s[i - 1] == 'q')
                {
                    a[i] = 'U';
                }
                else if ((c == 'u' || c == 'i') && prevVowel && nextVowel)
                {
                    a[i] = char.ToUpperInvariant(c);
                }
                else if (c == 'y' && (prevVowel || nextVowel))
                {
                    a[i] = 'Y';
                }
            }
            return new string(a);
        }

        private static string Unmark(string s) => s.Replace('I', 'i').Replace('U', 'u').Replace('Y', 'y');

        private static int Region(string s, int from)
        {
            int i = from;
            while (i < s.Length && !IsVowel(s[i]))
            {
                i++;
            }
            while (i < s.Length && IsVowel(s[i]))
            {
                i++;
            }
            return i < s.Length ? i + 1 : s.Length;
        }

        private static int ComputeRv(string s)
        {
            int n = s.Length;
            if (n < 3)
            {
                return n;
            }
            if ((IsVowel(s[0]) && IsVowel(s[1]))
                || s.StartsWith("par", StringComparison.Ordinal)
                || s.StartsWith("col", StringComparison.Ordinal)
                || s.StartsWith("tap", StringComparison.Ordinal))
            {
                return 3;
            }
            for (int i = 1; i < n; i++)
            {
                if (IsVowel(s[i]))
                {
                    return i + 1;
                }
            }
            return n;
        }

        private bool InRv(int suffixLen) => _s.Length - suffixLen >= _rv;
        private bool InR1(int suffixLen) => _s.Length - suffixLen >= _r1;
        private bool InR2(int suffixLen) => _s.Length - suffixLen >= _r2;
        private bool Ends(string suffix) => _s.EndsWith(suffix, StringComparison.Ordinal);
        private void Delete(int len) => _s = _s[..^len];
        private void Replace(int suffixLen, string repl) => _s = _s.Substring(0, _s.Length - suffixLen) + repl;

        private void Step1()
        {
            // (a) delete if in R2
            foreach (string suf in new[] { "ances", "iqUes", "ismes", "ables", "istes", "ance", "iqUe", "isme", "able", "iste", "eux" })
            {
                if (Ends(suf))
                {
                    if (InR2(suf.Length))
                    {
                        Delete(suf.Length);
                    }
                    return;
                }
            }

            // atrice(s)/ateur(s)/ation(s)
            foreach (string suf in new[] { "atrices", "ateurs", "ations", "atrice", "ateur", "ation" })
            {
                if (Ends(suf))
                {
                    if (InR2(suf.Length))
                    {
                        Delete(suf.Length);
                        if (Ends("ic"))
                        {
                            if (InR2(2))
                            {
                                Delete(2);
                            }
                            else
                            {
                                Replace(2, "iqU");
                            }
                        }
                    }
                    return;
                }
            }

            foreach (string suf in new[] { "logies", "logie" })
            {
                if (Ends(suf))
                {
                    if (InR2(suf.Length))
                    {
                        Replace(suf.Length, "log");
                    }
                    return;
                }
            }
            foreach (string suf in new[] { "usions", "utions", "usion", "ution" })
            {
                if (Ends(suf))
                {
                    if (InR2(suf.Length))
                    {
                        Replace(suf.Length, "u");
                    }
                    return;
                }
            }
            foreach (string suf in new[] { "ences", "ence" })
            {
                if (Ends(suf))
                {
                    if (InR2(suf.Length))
                    {
                        Replace(suf.Length, "ent");
                    }
                    return;
                }
            }

            foreach (string suf in new[] { "ements", "ement" })
            {
                if (Ends(suf))
                {
                    if (InRv(suf.Length))
                    {
                        Delete(suf.Length);
                        if (Ends("iv") && InR2(2))
                        {
                            Delete(2);
                            if (Ends("at") && InR2(2))
                            {
                                Delete(2);
                            }
                        }
                        else if (Ends("eus"))
                        {
                            if (InR2(3))
                            {
                                Delete(3);
                            }
                            else if (InR1(3))
                            {
                                Replace(3, "eux");
                            }
                        }
                        else if ((Ends("abl") || Ends("iqU")) && InR2(3))
                        {
                            Delete(3);
                        }
                        else if ((Ends("ièr") || Ends("Ièr")) && InRv(3))
                        {
                            Replace(3, "i");
                        }
                    }
                    return;
                }
            }

            foreach (string suf in new[] { "ités", "ité" })
            {
                if (Ends(suf))
                {
                    if (InR2(suf.Length))
                    {
                        Delete(suf.Length);
                        if (Ends("abil"))
                        {
                            if (InR2(4))
                            {
                                Delete(4);
                            }
                            else
                            {
                                Replace(4, "abl");
                            }
                        }
                        else if (Ends("ic"))
                        {
                            if (InR2(2))
                            {
                                Delete(2);
                            }
                            else
                            {
                                Replace(2, "iqU");
                            }
                        }
                        else if (Ends("iv") && InR2(2))
                        {
                            Delete(2);
                        }
                    }
                    return;
                }
            }

            foreach (string suf in new[] { "ives", "ive" })
            {
                if (Ends(suf))
                {
                    if (InR2(suf.Length))
                    {
                        Delete(suf.Length);
                        if (Ends("at") && InR2(2))
                        {
                            Delete(2);
                            if (Ends("ic"))
                            {
                                if (InR2(2))
                                {
                                    Delete(2);
                                }
                                else
                                {
                                    Replace(2, "iqU");
                                }
                            }
                        }
                    }
                    return;
                }
            }

            foreach (string suf in new[] { "eaux" })
            {
                if (Ends(suf))
                {
                    Replace(suf.Length, "eau");
                    return;
                }
            }
            if (Ends("aux"))
            {
                if (InR1(3))
                {
                    Replace(3, "al");
                }
                return;
            }
            foreach (string suf in new[] { "euses", "euse" })
            {
                if (Ends(suf))
                {
                    if (InR2(suf.Length))
                    {
                        Delete(suf.Length);
                    }
                    else if (InR1(suf.Length))
                    {
                        Replace(suf.Length, "eux");
                    }
                    return;
                }
            }
            foreach (string suf in new[] { "issements", "issement" })
            {
                if (Ends(suf))
                {
                    if (InR1(suf.Length) && _s.Length - suf.Length - 1 >= 0 && !IsVowel(_s[_s.Length - suf.Length - 1]))
                    {
                        Delete(suf.Length);
                    }
                    return;
                }
            }
            if (Ends("amment"))
            {
                if (InRv(6))
                {
                    Replace(6, "ant");
                    _step1RemovedMent = true;
                }
                return;
            }
            if (Ends("emment"))
            {
                if (InRv(6))
                {
                    Replace(6, "ent");
                    _step1RemovedMent = true;
                }
                return;
            }
            foreach (string suf in new[] { "ments", "ment" })
            {
                if (Ends(suf))
                {
                    int before = _s.Length - suf.Length - 1;
                    if (before >= 0 && IsVowel(_s[before]) && before >= _rv)
                    {
                        Delete(suf.Length);
                        _step1RemovedMent = true;
                    }
                    return;
                }
            }

        }

        private void Step2a()
        {
            foreach (string suf in new[]
            {
                "issaIent", "issantes", "iraIent", "issement", "issements", "issante",
                "issants", "issions", "irions", "issais", "issait", "issant", "issent",
                "issiez", "issons", "irais", "irait", "irent", "iriez", "irons", "iront",
                "isses", "issez", "îmes", "îtes", "irai", "iras", "irez", "isse", "ies",
                "ira", "ît", "ie", "ir", "is", "it", "i",
            })
            {
                if (Ends(suf) && InRv(suf.Length))
                {
                    int before = _s.Length - suf.Length - 1;
                    if (before >= 0 && !IsVowel(_s[before]))
                    {
                        Delete(suf.Length);
                        return;
                    }
                    return;
                }
            }
        }

        private void Step2b()
        {
            foreach (string suf in new[] { "eraIent", "erions", "èrent", "erais", "erait", "eriez", "erons", "eront", "erai", "eras", "erez", "ées", "era", "iez", "ée", "és", "er", "ez", "é" })
            {
                if (Ends(suf) && InRv(suf.Length))
                {
                    Delete(suf.Length);
                    return;
                }
            }
            foreach (string suf in new[]
            {
                "assions", "assiez", "assent", "asses", "antes", "aIent", "âmes", "âtes",
                "ante", "ants", "asse", "ait", "ais", "ant", "ât", "ai", "as", "a",
            })
            {
                if (Ends(suf) && InRv(suf.Length))
                {
                    Delete(suf.Length);
                    if (Ends("e") && InRv(1))
                    {
                        Delete(1);
                    }
                    return;
                }
            }
            foreach (string suf in new[] { "ions" })
            {
                if (Ends(suf) && InR2(suf.Length))
                {
                    Delete(suf.Length);
                    return;
                }
            }
        }

        private void Step3()
        {
            if (_s.EndsWith('Y'))
            {
                _s = _s.Substring(0, _s.Length - 1) + "i";
            }
            else if (_s.EndsWith('ç'))
            {
                _s = _s.Substring(0, _s.Length - 1) + "c";
            }
        }

        private void Step4()
        {
            if (Ends("s"))
            {
                int before = _s.Length - 2;
                if (before >= 0 && _s[before] is not ('a' or 'i' or 'o' or 'u' or 'è' or 's'))
                {
                    Delete(1);
                }
            }

            foreach (string suf in new[] { "ion" })
            {
                if (Ends(suf) && InR2(suf.Length))
                {
                    int before = _s.Length - suf.Length - 1;
                    if (before >= 0 && _s[before] is 's' or 't')
                    {
                        Delete(suf.Length);
                        return;
                    }
                }
            }
            foreach (string suf in new[] { "Ière", "ière", "Ier", "ier" })
            {
                if (Ends(suf) && InRv(suf.Length))
                {
                    Replace(suf.Length, "i");
                    return;
                }
            }
            if (Ends("e"))
            {
                if (InRv(1))
                {
                    Delete(1);
                }
                return;
            }
            if (Ends("ë"))
            {
                int before = _s.Length - 2;
                if (before >= 1 && _s[before] == 'u' && _s[before - 1] == 'g')
                {
                    Delete(1);
                }
            }
        }

        private void Step5()
        {
            if (Ends("enn") || Ends("onn") || Ends("ett") || Ends("ell") || Ends("eill"))
            {
                Delete(1);
            }
        }

        private void Step6()
        {
            for (int i = _s.Length - 1; i >= 0; i--)
            {
                char c = _s[i];
                if (IsVowel(c))
                {
                    if (c == 'é' || c == 'è')
                    {
                        _s = _s[..i] + 'e' + _s[(i + 1)..];
                    }
                    break;
                }
            }
        }
    }
}
