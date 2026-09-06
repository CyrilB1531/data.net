namespace Lodestar.Stats;

/// <summary>Which tail of the null distribution a test's p-value covers.</summary>
/// <remarks>
/// scipy spells this <c>alternative</c> and defaults it to <c>'two-sided'</c>
/// everywhere. It is not a presentation choice: a one-sided p-value is a
/// different number, not half of the two-sided one, once the null distribution
/// is asymmetric or discrete.
/// </remarks>
public enum Alternative
{
    /// <summary>The samples differ, in either direction. scipy's <c>'two-sided'</c>.</summary>
    TwoSided,

    /// <summary>The first sample's distribution is shifted below the second's. scipy's <c>'less'</c>.</summary>
    Less,

    /// <summary>The first sample's distribution is shifted above the second's. scipy's <c>'greater'</c>.</summary>
    Greater,
}

/// <summary>Whether an independent-samples t-test pools the two variances.</summary>
/// <remarks>
/// scipy's <c>ttest_ind</c> defaults to <c>equal_var=True</c>, which is Student's
/// test, not Welch's. Pooling is only correct when the two population variances
/// really are equal; Welch is the safer default in practice and the deliberate
/// non-default here, so the caller has to say which one they mean.
/// </remarks>
public enum Variance
{
    /// <summary>Pool the two variances — Student's t. scipy's <c>equal_var=True</c>.</summary>
    Equal,

    /// <summary>Do not pool; use the Welch-Satterthwaite degrees of freedom. scipy's <c>equal_var=False</c>.</summary>
    Welch,
}

/// <summary>Whether a discrete statistic's normal approximation gets the half-unit correction.</summary>
/// <remarks>
/// One idea, three spellings in scipy: <c>use_continuity</c> on
/// <c>mannwhitneyu</c> (default true), <c>correction</c> on <c>wilcoxon</c>
/// (default false) and <c>correction</c> on <c>chi2_contingency</c> (default
/// true, and applied to 2x2 tables only). The three defaults disagree, which is
/// exactly why this is a named argument here rather than a bool nobody reads.
/// </remarks>
public enum Continuity
{
    /// <summary>Shift the statistic half a unit toward the mean before the normal tail.</summary>
    Applied,

    /// <summary>Take the statistic as it stands.</summary>
    None,
}

/// <summary>Whether a p-value comes from the exact null distribution or its normal approximation.</summary>
/// <remarks>
/// scipy calls this <c>method</c> and defaults it to <c>'auto'</c>, which picks
/// exact for small untied samples and asymptotic otherwise. The choice changes
/// the number returned, not merely how long it takes, so it cannot be hidden.
/// </remarks>
public enum ExactMethod
{
    /// <summary>Exact when the sample is small and free of ties, asymptotic otherwise. scipy's <c>'auto'</c>.</summary>
    Auto,

    /// <summary>
    /// Enumerate the null distribution, whatever the sample. Measured: scipy
    /// computes an exact p-value on tied data too rather than refusing, so this
    /// does the same and the remarks say the number is only approximate there.
    /// </summary>
    Exact,

    /// <summary>Use the normal (or Kolmogorov) approximation, whatever the sample size.</summary>
    Asymptotic,
}

/// <summary>How the Wilcoxon signed-rank test treats pairs whose difference is zero.</summary>
/// <remarks>
/// scipy's <c>zero_method</c>, default <c>'wilcox'</c>. The three rules give
/// three different statistics on the same data, so this is part of the test's
/// definition rather than a tuning knob.
/// </remarks>
public enum ZeroMethod
{
    /// <summary>Discard the zero-difference pairs before ranking. scipy's <c>'wilcox'</c>.</summary>
    Wilcox,

    /// <summary>Rank the zeros, then drop their ranks from the sums. scipy's <c>'pratt'</c>.</summary>
    Pratt,

    /// <summary>Rank the zeros and split their ranks evenly between the two sums. scipy's <c>'zsplit'</c>.</summary>
    ZSplit,
}
