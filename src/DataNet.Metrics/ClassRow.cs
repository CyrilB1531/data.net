namespace DataNet.Metrics;

/// <summary>One class's line in a <see cref="ClassificationReport"/>.</summary>
/// <param name="Label">The label value this line scores.</param>
/// <param name="Name">The readable name supplied through <c>targetNames</c>, or null.</param>
/// <param name="Precision">Precision for this class.</param>
/// <param name="Recall">Recall for this class.</param>
/// <param name="F1">F1 for this class.</param>
/// <param name="Support">The weight of samples whose true label is this class.</param>
public sealed record ClassRow(
    int Label, string? Name, double Precision, double Recall, double F1, double Support);

/// <summary>An averaged line in a <see cref="ClassificationReport"/>.</summary>
/// <param name="Name">The average's name, as scikit-learn prints it: <c>macro avg</c>, <c>weighted avg</c>, <c>micro avg</c>.</param>
/// <param name="Precision">The averaged precision.</param>
/// <param name="Recall">The averaged recall.</param>
/// <param name="F1">The averaged F1.</param>
/// <param name="Support">The total support the average covers.</param>
public sealed record AverageRow(
    string Name, double Precision, double Recall, double F1, double Support);
