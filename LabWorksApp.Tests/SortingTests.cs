using LabWorksApp.Models;
using LabWorksApp.Services;
using LabWorksApp.Services.Sorting;

namespace LabWorksApp.Tests;

public class SortingTests
{
    private static readonly int[] TestArray = { 42, -15, 0, 88, 3, -15, 12, 99, 1 };

    [Theory]
    [InlineData(SortAlgorithmType.Bubble)]
    [InlineData(SortAlgorithmType.Shaker)]
    [InlineData(SortAlgorithmType.Insertion)]
    [InlineData(SortAlgorithmType.Quick)]
    public void ComparisonAlgorithms_SortAscending_Correctly(SortAlgorithmType type)
    {
        ISortAlgorithm algo = GetAlgorithm(type);
        var expected = (int[])TestArray.Clone();
        Array.Sort(expected);

        // Benchmark test
        var result = algo.Benchmark(TestArray, ascending: true);
        Assert.True(result.Succeeded);

        // Steps test
        var steps = algo.GenerateSteps(TestArray, ascending: true);
        Assert.NotEmpty(steps);
        var finalSnapshot = steps[^1].ArraySnapshot;
        Assert.Equal(expected, finalSnapshot);
    }

    [Theory]
    [InlineData(SortAlgorithmType.Bubble)]
    [InlineData(SortAlgorithmType.Shaker)]
    [InlineData(SortAlgorithmType.Insertion)]
    [InlineData(SortAlgorithmType.Quick)]
    public void ComparisonAlgorithms_SortDescending_Correctly(SortAlgorithmType type)
    {
        ISortAlgorithm algo = GetAlgorithm(type);
        var expected = (int[])TestArray.Clone();
        Array.Sort(expected);
        Array.Reverse(expected);

        var result = algo.Benchmark(TestArray, ascending: false);
        Assert.True(result.Succeeded);

        var steps = algo.GenerateSteps(TestArray, ascending: false);
        Assert.NotEmpty(steps);
        var finalSnapshot = steps[^1].ArraySnapshot;
        Assert.Equal(expected, finalSnapshot);
    }

    [Fact]
    public void BogoSort_SortsSmallArray_Ascending()
    {
        var smallArray = new[] { 5, 2, 8, 1 };
        var bogo = new BogoSort();
        var result = bogo.Benchmark(smallArray, ascending: true, maxIterations: 50000);
        Assert.True(result.Succeeded);

        var expected = new[] { 1, 2, 5, 8 };
        var steps = bogo.GenerateSteps(smallArray, ascending: true, maxIterations: 1000);
        Assert.True(steps.Count > 0);
    }

    [Fact]
    public void BogoSort_RespectsMaxIterations()
    {
        var largeUnsorted = Enumerable.Range(1, 20).Reverse().ToArray();
        var bogo = new BogoSort();
        long limit = 50;

        var result = bogo.Benchmark(largeUnsorted, ascending: true, maxIterations: limit);
        Assert.Equal(limit, result.Iterations);
        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(SortAlgorithmType.Bubble)]
    [InlineData(SortAlgorithmType.Shaker)]
    [InlineData(SortAlgorithmType.Insertion)]
    [InlineData(SortAlgorithmType.Quick)]
    [InlineData(SortAlgorithmType.Bogo)]
    public void AllAlgorithms_HandleSingleElement(SortAlgorithmType type)
    {
        ISortAlgorithm algo = GetAlgorithm(type);
        var single = new[] { 42 };

        var result = algo.Benchmark(single, ascending: true);
        Assert.True(result.Succeeded);

        var steps = algo.GenerateSteps(single, ascending: true);
        Assert.NotEmpty(steps);
        Assert.True(steps[^1].IsDone);
        Assert.Equal(single, steps[^1].ArraySnapshot);
    }

    private static ISortAlgorithm GetAlgorithm(SortAlgorithmType type) => type switch
    {
        SortAlgorithmType.Bubble => new BubbleSort(),
        SortAlgorithmType.Shaker => new ShakerSort(),
        SortAlgorithmType.Insertion => new InsertionSort(),
        SortAlgorithmType.Quick => new QuickSort(),
        SortAlgorithmType.Bogo => new BogoSort(),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
