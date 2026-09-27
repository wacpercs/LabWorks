namespace LabWorksApp.Models;

public enum SortAlgorithmType
{
    Bubble,
    Shaker,
    Insertion,
    Quick,
    Bogo
}

public static class SortAlgorithmExtensions
{
    public static string GetDisplayName(this SortAlgorithmType type) => type switch
    {
        SortAlgorithmType.Bubble => "Пузырьковая (Bubble)",
        SortAlgorithmType.Shaker => "Шейкерная (Shaker)",
        SortAlgorithmType.Insertion => "Вставками (Insertion)",
        SortAlgorithmType.Quick => "Быстрая (Quick)",
        SortAlgorithmType.Bogo => "BOGO (Случайная)",
        _ => type.ToString()
    };

    public static string GetTheoreticalFormula(this SortAlgorithmType type) => type switch
    {
        SortAlgorithmType.Bubble => "n - 1",
        SortAlgorithmType.Shaker => "(n - 1) / 2",
        SortAlgorithmType.Insertion => "~ n² / 4",
        SortAlgorithmType.Quick => "O(n log n)",
        SortAlgorithmType.Bogo => "O((n + 1)!)",
        _ => ""
    };
}
