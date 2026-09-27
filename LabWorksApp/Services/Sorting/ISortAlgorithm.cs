using LabWorksApp.Models;

namespace LabWorksApp.Services.Sorting;

public interface ISortAlgorithm
{
    SortAlgorithmType Type { get; }
    string Name { get; }
    IReadOnlyList<SortStep> GenerateSteps(int[] source, bool ascending, long maxIterations = 50000);
    SortResult Benchmark(int[] source, bool ascending, long maxIterations = 50000);
}
