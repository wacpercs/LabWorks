namespace LabWorksApp.Models;

public class SortResult
{
    public SortAlgorithmType AlgorithmType { get; set; }
    public string Name => AlgorithmType.GetDisplayName();
    public string Formula => AlgorithmType.GetTheoreticalFormula();
    public long Iterations { get; set; }
    public long Swaps { get; set; }
    public double ElapsedMilliseconds { get; set; }
    public double ElapsedMicroseconds => ElapsedMilliseconds * 1000.0;
    public bool IsFastest { get; set; }
    public bool Succeeded { get; set; } = true;
    public string Note { get; set; } = string.Empty;

    public string FormattedTime => ElapsedMilliseconds < 1.0
        ? $"{ElapsedMicroseconds:F1} мкс ({ElapsedMilliseconds:F4} мс)"
        : $"{ElapsedMilliseconds:F3} мс";
}
