namespace Lodestar.Text.Keywords;

/// <summary>
/// The undirected co-occurrence graph TextRank ranks, and the power iteration that ranks it.
/// </summary>
/// <remarks>
/// The window runs over the RAW token stream: a stop word is a null that occupies a
/// position and forms no node, so two words separated by one are not adjacent. Nodes of
/// zero weighted degree are then deleted in one pass, without which the transition matrix
/// is substochastic and its dominant eigenvector is a different vector. Both measured
/// against summa, whose pipeline does the same two things.
/// </remarks>
internal sealed class WordGraph
{
    private readonly List<string> _nodes = [];
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
    private double[][] _weights;

    public WordGraph(IReadOnlyList<string?> stream, int window)
    {
        Guard.NotNull(stream);
        Guard.NotLessThan(window, 1);

        foreach (string token in stream.OfType<string>().Where(t => !_index.ContainsKey(t)))
        {
            _index[token] = _nodes.Count;
            _nodes.Add(token);
        }

        _weights = BuildWeights(stream, window);
        RemoveUnreachable();
    }

    /// <summary>The ranked words, in first-occurrence order, with the unreachable ones gone.</summary>
    public IReadOnlyList<string> Nodes => _nodes;

    /// <summary>How many undirected edges survive, which is what a window bug changes first.</summary>
    public int EdgeCount
    {
        get
        {
            int edges = 0;
            for (int i = 0; i < _nodes.Count; i++)
            {
                for (int j = i + 1; j < _nodes.Count; j++)
                {
                    if (!IsZero(_weights[i][j]))
                    {
                        edges++;
                    }
                }
            }

            return edges;
        }
    }

    /// <summary>The dominant left eigenvector of <c>d·A + (1 − d)/n</c>, L2-normalised.</summary>
    /// <param name="damping">The probability of following an edge rather than teleporting.</param>
    /// <param name="tolerance">The largest per-component change that counts as converged.</param>
    /// <param name="maxIterations">The most iterations to run before giving up.</param>
    /// <exception cref="InvalidOperationException">The iteration did not converge within <paramref name="maxIterations"/>.</exception>
    public double[] Rank(double damping, double tolerance, int maxIterations)
    {
        int n = _nodes.Count;
        if (n == 0)
        {
            return [];
        }

        double[][] m = BuildTransitionMatrix(damping, n);
        double[] x = InitialVector(n);
        double[] next = new double[n];

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            double delta = Iterate(m, x, next, n);
            (x, next) = (next, x);
            if (delta < tolerance)
            {
                // The transition matrix is non-negative and x starts non-negative, so every
                // iterate stays non-negative; the abs guards only against a stray -0.0.
                MakeNonNegative(x, n);
                return x;
            }
        }

        throw new InvalidOperationException(
            $"The power iteration did not converge to {tolerance} within {maxIterations} iterations.");
    }

    // Weights are sums of exact 1.0 increments, well under a double's 53-bit mantissa, so
    // testing against zero is exact rather than an approximation that S1244 wants ranged.
#pragma warning disable S1244
    private static bool IsZero(double weight) => weight == 0;
#pragma warning restore S1244

    private static double[][] CreateMatrix(int n)
    {
        var matrix = new double[n][];
        for (int i = 0; i < n; i++)
        {
            matrix[i] = new double[n];
        }

        return matrix;
    }

    private static double[] InitialVector(int n)
    {
        double[] x = new double[n];
        double initial = 1.0 / Math.Sqrt(n);
        for (int i = 0; i < n; i++)
        {
            x[i] = initial;
        }

        return x;
    }

    private static void MakeNonNegative(double[] x, int n)
    {
        for (int i = 0; i < n; i++)
        {
            x[i] = Math.Abs(x[i]);
        }
    }

    // One power-iteration step, x·M renormalised to unit L2 norm, returning the largest
    // per-component change so the caller can test convergence without a second pass.
    private static double Iterate(double[][] m, double[] x, double[] next, int n)
    {
        for (int j = 0; j < n; j++)
        {
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                sum += x[i] * m[i][j];
            }

            next[j] = sum;
        }

        double norm = 0;
        for (int j = 0; j < n; j++)
        {
            norm += next[j] * next[j];
        }

        norm = Math.Sqrt(norm);

        double delta = 0;
        for (int j = 0; j < n; j++)
        {
            next[j] /= norm;
            double componentDelta = Math.Abs(next[j] - x[j]);
            if (componentDelta > delta)
            {
                delta = componentDelta;
            }
        }

        return delta;
    }

    private double[][] BuildWeights(IReadOnlyList<string?> stream, int window)
    {
        double[][] weights = CreateMatrix(_nodes.Count);
        for (int i = 0; i < stream.Count; i++)
        {
            if (stream[i] is string left)
            {
                AddEdgesFrom(stream, weights, i, left, window);
            }
        }

        return weights;
    }

    // Pairs token i with i+1 .. i+window-1, summa's own window semantics: only tokens
    // that far apart in the RAW stream — nulls included — ever share an edge.
    private void AddEdgesFrom(IReadOnlyList<string?> stream, double[][] weights, int i, string left, int window)
    {
        int end = Math.Min(i + window, stream.Count);
        int a = _index[left];
        for (int j = i + 1; j < end; j++)
        {
            if (stream[j] is not string right)
            {
                continue;
            }

            int b = _index[right];
            if (a == b)
            {
                continue;
            }

            weights[a][b] += 1;
            weights[b][a] += 1;
        }
    }

    // The row-stochastic transition matrix d·A + (1 − d)/n, built once per Rank call so
    // Rank itself only ever multiplies and renormalises.
    private double[][] BuildTransitionMatrix(double damping, int n)
    {
        double[][] m = CreateMatrix(n);
        double teleport = (1 - damping) / n;
        for (int i = 0; i < n; i++)
        {
            double degree = 0;
            for (int j = 0; j < n; j++)
            {
                degree += _weights[i][j];
            }

            for (int j = 0; j < n; j++)
            {
                m[i][j] = teleport + (IsZero(degree) ? 0 : (damping * _weights[i][j] / degree));
            }
        }

        return m;
    }

    // One pass is enough: a node of zero weighted degree has no edges, so removing it
    // lowers nobody else's degree and can isolate no one. summa's is one `for` too.
    private void RemoveUnreachable()
    {
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            double degree = 0;
            for (int j = 0; j < _nodes.Count; j++)
            {
                degree += _weights[i][j];
            }

            if (IsZero(degree))
            {
                // Descending, so Drop never invalidates an index still to visit.
                Drop(i);
            }
        }
    }

    private void Drop(int index)
    {
        int n = _nodes.Count;
        double[][] trimmed = CreateMatrix(n - 1);
        for (int i = 0, a = 0; i < n; i++)
        {
            if (i == index)
            {
                continue;
            }

            for (int j = 0, b = 0; j < n; j++)
            {
                if (j == index)
                {
                    continue;
                }

                trimmed[a][b] = _weights[i][j];
                b++;
            }

            a++;
        }

        _nodes.RemoveAt(index);
        _weights = trimmed;
        _index.Clear();
        for (int i = 0; i < _nodes.Count; i++)
        {
            _index[_nodes[i]] = i;
        }
    }
}
