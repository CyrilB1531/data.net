using System.Text;

namespace DataNet.Text.Stemming;

/// <summary>
/// The English Snowball ("Porter2") stemming algorithm.
/// </summary>
/// <remarks>
/// <para>
/// A refinement of <see cref="PorterStemmer"/> with cleaner handling of regions
/// R1/R2 and numerous special cases. Reference behavior:
/// <c>nltk.stem.snowball.SnowballStemmer("english")</c>. This is an original
/// implementation of the published Snowball algorithm.
/// </para>
/// <para>English-oriented; input is lowercased. Thread-safe.</para>
/// </remarks>
public static class EnglishSnowballStemmer
{
    private static readonly Dictionary<string, string> Exceptions1 = new(StringComparer.Ordinal)
    {
        ["skis"] = "ski",
        ["skies"] = "sky",
        ["dying"] = "die",
        ["lying"] = "lie",
        ["tying"] = "tie",
        ["idly"] = "idl",
        ["gently"] = "gentl",
        ["ugly"] = "ugli",
        ["early"] = "earli",
        ["only"] = "onli",
        ["singly"] = "singl",
        ["sky"] = "sky",
        ["news"] = "news",
        ["howe"] = "howe",
        ["atlas"] = "atlas",
        ["cosmos"] = "cosmos",
        ["bias"] = "bias",
        ["andes"] = "andes",
    };

    private static readonly HashSet<string> Exceptions2 = new(StringComparer.Ordinal)
    {
        "inning", "outing", "canning", "herring", "earring", "proceed", "exceed", "succeed",
    };

    /// <summary>Returns the English Snowball stem of <paramref name="word"/>.</summary>
    public static string Stem(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        string s = word.ToLowerInvariant();
        if (s.Length <= 2)
        {
            return s;
        }
        if (Exceptions1.TryGetValue(s, out string? mapped))
        {
            return mapped;
        }

        var w = new Worker(s);
        return w.Stem();
    }

    private sealed class Worker
    {
        private string _s;
        private readonly int _r1;
        private readonly int _r2;

        public Worker(string s)
        {
            // Remove a leading apostrophe, then mark consonantal y as 'Y'.
            if (s.StartsWith('\''))
            {
                s = s[1..];
            }
            var sb = new StringBuilder(s);
            if (sb.Length > 0 && sb[0] == 'y')
            {
                sb[0] = 'Y';
            }
            for (int i = 1; i < sb.Length; i++)
            {
                if (sb[i] == 'y' && IsVowel(sb[i - 1]))
                {
                    sb[i] = 'Y';
                }
            }
            _s = sb.ToString();

            _r1 = ComputeR1(_s);
            _r2 = Region(_s, _r1);
        }

        public string Stem()
        {
            Step0();
            Step1a();
            if (Exceptions2.Contains(_s))
            {
                return _s;
            }
            Step1b();
            Step1c();
            Step2();
            Step3();
            Step4();
            Step5();
            return _s.Replace('Y', 'y');
        }

        private static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';

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

        private static int ComputeR1(string s)
        {
            if (s.StartsWith("gener", StringComparison.Ordinal) || s.StartsWith("arsen", StringComparison.Ordinal))
            {
                return 5;
            }
            if (s.StartsWith("commun", StringComparison.Ordinal))
            {
                return 6;
            }
            return Region(s, 0);
        }

        private bool InR1(int suffixLen) => _s.Length - suffixLen >= _r1;

        private bool InR2(int suffixLen) => _s.Length - suffixLen >= _r2;

        private bool Ends(string suffix) => _s.EndsWith(suffix, StringComparison.Ordinal);

        private void Replace(string suffix, string replacement) =>
            _s = string.Concat(_s.AsSpan(0, _s.Length - suffix.Length), replacement);

        private bool ContainsVowelBefore(int suffixLen)
        {
            int end = _s.Length - suffixLen;
            for (int i = 0; i < end; i++)
            {
                if (IsVowel(_s[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>True if the word (optionally minus a trailing char count) ends in a short syllable.</summary>
        private bool EndsShortSyllable(int len)
        {
            if (len == 2)
            {
                return IsVowel(_s[0]) && !IsVowel(_s[1]);
            }
            if (len >= 3)
            {
                char c1 = _s[len - 3];
                char c2 = _s[len - 2];
                char c3 = _s[len - 1];
                return !IsVowel(c1) && IsVowel(c2) && !IsVowel(c3) && c3 is not ('w' or 'x' or 'Y');
            }
            return false;
        }

        private bool IsShortWord() => EndsShortSyllable(_s.Length) && _r1 >= _s.Length;

        private void Step0()
        {
            if (Ends("'s'"))
            {
                _s = _s[..^3];
            }
            else if (Ends("'s"))
            {
                _s = _s[..^2];
            }
            else if (Ends("'"))
            {
                _s = _s[..^1];
            }
        }

        private void Step1a()
        {
            if (Ends("sses"))
            {
                Replace("sses", "ss");
            }
            else if (Ends("ied") || Ends("ies"))
            {
                Replace(_s[^3..], _s.Length - 3 > 1 ? "i" : "ie");
            }
            else if (Ends("us") || Ends("ss"))
            {
                // unchanged
            }
            else if (Ends("s"))
            {
                // delete if there is a vowel before the char preceding the final s
                for (int i = 0; i < _s.Length - 2; i++)
                {
                    if (IsVowel(_s[i]))
                    {
                        _s = _s[..^1];
                        break;
                    }
                }
            }
        }

        private void Step1b()
        {
            if (Ends("eedly"))
            {
                if (InR1(5))
                {
                    Replace("eedly", "ee");
                }
                return;
            }
            if (Ends("eed"))
            {
                if (InR1(3))
                {
                    Replace("eed", "ee");
                }
                return;
            }

            string? suffix = Ends("ingly") ? "ingly" : Ends("edly") ? "edly" : Ends("ing") ? "ing" : Ends("ed") ? "ed" : null;
            if (suffix is null || !ContainsVowelBefore(suffix.Length))
            {
                return;
            }

            Replace(suffix, string.Empty);
            if (Ends("at") || Ends("bl") || Ends("iz"))
            {
                _s += "e";
            }
            else if (EndsDouble())
            {
                _s = _s[..^1];
            }
            else if (IsShortWord())
            {
                _s += "e";
            }
        }

        private bool EndsDouble()
        {
            if (_s.Length < 2 || _s[^1] != _s[^2])
            {
                return false;
            }
            return _s[^1] is 'b' or 'd' or 'f' or 'g' or 'm' or 'n' or 'p' or 'r' or 't';
        }

        private void Step1c()
        {
            if (_s.Length > 2 && (_s[^1] == 'y' || _s[^1] == 'Y') && !IsVowel(_s[^2]))
            {
                _s = string.Concat(_s.AsSpan(0, _s.Length - 1), "i");
            }
        }

        private void Step2()
        {
            foreach ((string suffix, string replacement) in Step2Rules)
            {
                if (!Ends(suffix))
                {
                    continue;
                }
                if (!InR1(suffix.Length))
                {
                    return;
                }
                switch (suffix)
                {
                    case "ogi":
                        if (_s.Length >= 4 && _s[^4] == 'l')
                        {
                            Replace(suffix, replacement);
                        }
                        return;
                    case "li":
                        if (_s.Length >= 3 && IsLiEnding(_s[^3]))
                        {
                            _s = _s[..^2];
                        }
                        return;
                    default:
                        Replace(suffix, replacement);
                        return;
                }
            }
        }

        private static bool IsLiEnding(char c) => c is 'c' or 'd' or 'e' or 'g' or 'h' or 'k' or 'm' or 'n' or 'r' or 't';

        private void Step3()
        {
            foreach ((string suffix, string replacement) in Step3Rules)
            {
                if (!Ends(suffix))
                {
                    continue;
                }
                if (!InR1(suffix.Length))
                {
                    return;
                }
                if (suffix == "ative")
                {
                    if (InR2(suffix.Length))
                    {
                        Replace(suffix, replacement);
                    }
                    return;
                }
                Replace(suffix, replacement);
                return;
            }
        }

        private void Step4()
        {
            foreach (string suffix in Step4Suffixes)
            {
                if (!Ends(suffix))
                {
                    continue;
                }
                if (!InR2(suffix.Length))
                {
                    return;
                }
                if (suffix == "ion")
                {
                    if (_s.Length >= 4 && _s[^4] is 's' or 't')
                    {
                        Replace(suffix, string.Empty);
                    }
                    return;
                }
                Replace(suffix, string.Empty);
                return;
            }
        }

        private void Step5()
        {
            if (_s.EndsWith('e'))
            {
                if (InR2(1) || (InR1(1) && !EndsShortSyllable(_s.Length - 1)))
                {
                    _s = _s[..^1];
                }
            }
            else if (_s.EndsWith('l') && InR2(1) && _s.Length >= 2 && _s[^2] == 'l')
            {
                _s = _s[..^1];
            }
        }

        // Ordered longest-first within the algorithm's groups.
        private static readonly (string Suffix, string Replacement)[] Step2Rules =
        {
            ("ational", "ate"), ("fulness", "ful"), ("iveness", "ive"), ("ousness", "ous"),
            ("ization", "ize"), ("tional", "tion"), ("biliti", "ble"), ("lessli", "less"),
            ("ousli", "ous"), ("fulli", "ful"), ("entli", "ent"), ("aliti", "al"),
            ("iviti", "ive"), ("ation", "ate"), ("alism", "al"), ("enci", "ence"),
            ("anci", "ance"), ("abli", "able"), ("izer", "ize"), ("ator", "ate"),
            ("alli", "al"), ("bli", "ble"), ("ogi", "og"), ("li", ""),
        };

        private static readonly (string Suffix, string Replacement)[] Step3Rules =
        {
            ("ational", "ate"), ("tional", "tion"), ("alize", "al"), ("icate", "ic"),
            ("iciti", "ic"), ("ative", ""), ("ical", "ic"), ("ness", ""), ("ful", ""),
        };

        private static readonly string[] Step4Suffixes =
        {
            "ement", "ance", "ence", "able", "ible", "ment", "ant", "ent", "ism",
            "ate", "iti", "ous", "ive", "ize", "ion", "al", "er", "ic",
        };
    }
}
