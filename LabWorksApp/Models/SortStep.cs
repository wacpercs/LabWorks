namespace LabWorksApp.Models;

public class SortStep
{
    public int[] ArraySnapshot { get; set; } = Array.Empty<int>();
    public int CompareIndex1 { get; set; } = -1;
    public int CompareIndex2 { get; set; } = -1;
    public int SwapIndex1 { get; set; } = -1;
    public int SwapIndex2 { get; set; } = -1;
    public long Iterations { get; set; }
    public long Swaps { get; set; }
    public bool IsDone { get; set; }
}
