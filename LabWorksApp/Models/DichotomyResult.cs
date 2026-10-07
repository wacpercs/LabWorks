namespace LabWorksApp.Models;

public class DichotomyResult
{
    public double BestX { get; set; }
    public double BestY { get; set; }
    public int IterationsCount { get; set; }
    public double AchievedPrecision { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<DichotomyStep> Steps { get; set; } = Array.Empty<DichotomyStep>();
    public double ElapsedMilliseconds { get; set; }

    public string FormattedBestX => BestX.ToString("F6");
    public string FormattedBestY => BestY.ToString("F6");
    public string FormattedPrecision => AchievedPrecision.ToString("E4");
    public string FormattedTime => ElapsedMilliseconds < 1.0
        ? $"{ElapsedMilliseconds * 1000.0:F1} мкс"
        : $"{ElapsedMilliseconds:F3} мс";
}
