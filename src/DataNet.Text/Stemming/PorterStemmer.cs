using System.Text;

namespace DataNet.Text.Stemming;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// CA1308 (normalize to uppercase): Snowball, Porter and WordPiece are *defined*
// on lowercase input — the published algorithms, the reference implementations
// and the oracle corpora this suite is checked against all lowercase first.
// ToUpperInvariant would return different stems, which is a wrong answer rather
// than a differently-cased one.
#pragma warning disable S3776, CA1308

/// <summary>
/// The Porter stemming algorithm (Martin Porter, 1980) for English.
/// </summary>
/// <remarks>
/// <para>
/// Reduces a word to a stem by stripping common morphological endings in five
/// steps. Reference behavior: the original algorithm as implemented by
/// <c>nltk.stem.porter.PorterStemmer(mode=ORIGINAL_ALGORITHM)</c>. This is an
/// original implementation of the published algorithm, not a transcription.
/// </para>
/// <para>Input is lowercased; only ASCII letters are treated as such. Thread-safe.</para>
/// </remarks>
public static class PorterStemmer
{
    /// <summary>Returns the Porter stem of <paramref name="word"/>.</summary>
    public static string Stem(string word)
    {
        Guard.NotNull(word);
        if (word.Length <= 2)
        {
            return word.ToLowerInvariant();
        }

        var w = new Worker(word.ToLowerInvariant());
        w.Step1a();
        w.Step1b();
        w.Step1c();
        w.Step2();
        w.Step3();
        w.Step4();
        w.Step5a();
        w.Step5b();
        return w.Result;
    }

    private sealed class Worker
    {
        private char[] _b;
        private int _k; // index of the last character (inclusive)

        public Worker(string word)
        {
            _b = word.ToCharArray();
            _k = _b.Length - 1;
        }

        public string Result => new(_b, 0, _k + 1);

        private bool IsConsonant(int i)
        {
            char c = _b[i];
            if (c is 'a' or 'e' or 'i' or 'o' or 'u')
            {
                return false;
            }
            if (c == 'y')
            {
                return i == 0 || !IsConsonant(i - 1);
            }
            return true;
        }

        /// <summary>Measure m of the stem <c>b[0..j]</c> (number of VC sequences).</summary>
        private int Measure(int j)
        {
            int m = 0;
            for (int i = 1; i <= j; i++)
            {
                if (!IsConsonant(i - 1) && IsConsonant(i))
                {
                    m++;
                }
            }
            return m;
        }

        private bool ContainsVowel(int j)
        {
            for (int i = 0; i <= j; i++)
            {
                if (!IsConsonant(i))
                {
                    return true;
                }
            }
            return false;
        }

        private bool EndsDoubleConsonant()
        {
            return _k >= 1 && _b[_k] == _b[_k - 1] && IsConsonant(_k);
        }

        /// <summary>True if b[i-2..i] is consonant-vowel-consonant and the last is not w/x/y.</summary>
        private bool EndsCvc(int i)
        {
            if (i < 2 || !IsConsonant(i) || IsConsonant(i - 1) || !IsConsonant(i - 2))
            {
                return false;
            }
            char c = _b[i];
            return c is not ('w' or 'x' or 'y');
        }

        private bool EndsWith(string suffix)
        {
            int len = suffix.Length;
            if (len > _k + 1)
            {
                return false;
            }
            for (int i = 0; i < len; i++)
            {
                if (_b[_k - len + 1 + i] != suffix[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Replaces the current suffix (last <paramref name="suffixLen"/> chars) with <paramref name="replacement"/>.</summary>
        private void SetSuffix(int suffixLen, string replacement)
        {
            int stemEnd = _k - suffixLen; // last index of the stem to keep
            var sb = new StringBuilder(stemEnd + 1 + replacement.Length);
            sb.Append(_b, 0, stemEnd + 1);
            sb.Append(replacement);
            _b = sb.ToString().ToCharArray();
            _k = _b.Length - 1;
        }

        private int StemEndFor(int suffixLen) => _k - suffixLen;

        public void Step1a()
        {
            if (EndsWith("sses"))
            {
                SetSuffix(4, "ss");
            }
            else if (EndsWith("ies"))
            {
                SetSuffix(3, "i");
            }
            else if (EndsWith("ss"))
            {
                // leave unchanged
            }
            else if (EndsWith("s"))
            {
                SetSuffix(1, string.Empty);
            }
        }

        public void Step1b()
        {
            bool step1bFlag = false;
            if (EndsWith("eed"))
            {
                if (Measure(StemEndFor(3)) > 0)
                {
                    SetSuffix(3, "ee"); // (m>0) EED -> EE
                }
            }
            else if (EndsWith("ed") && ContainsVowel(StemEndFor(2)))
            {
                SetSuffix(2, string.Empty);
                step1bFlag = true;
            }
            else if (EndsWith("ing") && ContainsVowel(StemEndFor(3)))
            {
                SetSuffix(3, string.Empty);
                step1bFlag = true;
            }

            if (step1bFlag)
            {
                if (EndsWith("at") || EndsWith("bl") || EndsWith("iz"))
                {
                    SetSuffix(0, "e");
                }
                else if (EndsDoubleConsonant() && _b[_k] is not ('l' or 's' or 'z'))
                {
                    _k--; // drop the doubled consonant
                }
                else if (Measure(_k) == 1 && EndsCvc(_k))
                {
                    SetSuffix(0, "e");
                }
            }
        }

        public void Step1c()
        {
            if (EndsWith("y") && ContainsVowel(StemEndFor(1)))
            {
                _b[_k] = 'i';
            }
        }

        /// <summary>If the word ends with <paramref name="suffix"/>, replace it (when measure &gt; min) and report the match.</summary>
        private bool TryReplace(string suffix, string replacement, int minMeasure)
        {
            if (!EndsWith(suffix))
            {
                return false;
            }
            if (Measure(StemEndFor(suffix.Length)) > minMeasure)
            {
                SetSuffix(suffix.Length, replacement);
            }
            return true;
        }

        public void Step2()
        {
            if (_k < 1)
            {
                return;
            }
            _ = _b[_k - 1] switch
            {
                'a' => TryReplace("ational", "ate", 0) || TryReplace("tional", "tion", 0),
                'c' => TryReplace("enci", "ence", 0) || TryReplace("anci", "ance", 0),
                'e' => TryReplace("izer", "ize", 0),
                'l' => TryReplace("bli", "ble", 0) || TryReplace("alli", "al", 0) || TryReplace("entli", "ent", 0)
                       || TryReplace("eli", "e", 0) || TryReplace("ousli", "ous", 0),
                'o' => TryReplace("ization", "ize", 0) || TryReplace("ation", "ate", 0) || TryReplace("ator", "ate", 0),
                's' => TryReplace("alism", "al", 0) || TryReplace("iveness", "ive", 0)
                       || TryReplace("fulness", "ful", 0) || TryReplace("ousness", "ous", 0),
                't' => TryReplace("aliti", "al", 0) || TryReplace("iviti", "ive", 0) || TryReplace("biliti", "ble", 0),
                'g' => TryReplace("logi", "log", 0),
                _ => false,
            };
        }

        public void Step3()
        {
            _ = _b[_k] switch
            {
                'e' => TryReplace("icate", "ic", 0) || TryReplace("ative", string.Empty, 0) || TryReplace("alize", "al", 0),
                'i' => TryReplace("iciti", "ic", 0),
                'l' => TryReplace("ical", "ic", 0) || TryReplace("ful", string.Empty, 0),
                's' => TryReplace("ness", string.Empty, 0),
                _ => false,
            };
        }

        public void Step4()
        {
            if (_k < 1)
            {
                return;
            }
            _ = _b[_k - 1] switch
            {
                'a' => TryReplace("al", string.Empty, 1),
                'c' => TryReplace("ance", string.Empty, 1) || TryReplace("ence", string.Empty, 1),
                'e' => TryReplace("er", string.Empty, 1),
                'i' => TryReplace("ic", string.Empty, 1),
                'l' => TryReplace("able", string.Empty, 1) || TryReplace("ible", string.Empty, 1),
                'n' => TryReplace("ant", string.Empty, 1) || TryReplace("ement", string.Empty, 1)
                       || TryReplace("ment", string.Empty, 1) || TryReplace("ent", string.Empty, 1),
                'o' => TryIon() || TryReplace("ou", string.Empty, 1),
                's' => TryReplace("ism", string.Empty, 1),
                't' => TryReplace("ate", string.Empty, 1) || TryReplace("iti", string.Empty, 1),
                'u' => TryReplace("ous", string.Empty, 1),
                'v' => TryReplace("ive", string.Empty, 1),
                'z' => TryReplace("ize", string.Empty, 1),
                _ => false,
            };
        }

        private bool TryIon()
        {
            if (!EndsWith("ion"))
            {
                return false;
            }
            int stemEnd = StemEndFor(3);
            if (Measure(stemEnd) > 1 && stemEnd >= 0 && _b[stemEnd] is 's' or 't')
            {
                SetSuffix(3, string.Empty);
            }
            return true;
        }

        public void Step5a()
        {
            if (EndsWith("e"))
            {
                int a = Measure(StemEndFor(1));
                if (a > 1 || (a == 1 && !EndsCvc(_k - 1)))
                {
                    SetSuffix(1, string.Empty);
                }
            }
        }

        public void Step5b()
        {
            if (_b[_k] == 'l' && EndsDoubleConsonant() && Measure(_k) > 1)
            {
                _k--;
            }
        }
    }
}
