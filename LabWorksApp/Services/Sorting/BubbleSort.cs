using System.Diagnostics;
using LabWorksApp.Models;

namespace LabWorksApp.Services.Sorting;

public class BubbleSort : ISortAlgorithm
{
    public SortAlgorithmType Type => SortAlgorithmType.Bubble;
    public string Name => Type.GetDisplayName();

    public IReadOnlyList<SortStep> GenerateSteps(int[] source, bool ascending, long maxIterations = 50000)
    {
        var steps = new List<SortStep>();
        var arr = (int[])source.Clone();
        int n = arr.Length;
        long iterations = 0;
        long swaps = 0;

        steps.Add(new SortStep
        {
            ArraySnapshot = (int[])arr.Clone(),
            Iterations = 0,
            Swaps = 0,
            IsDone = n <= 1
        });

        if (n <= 1) return steps;

        for (int i = 0; i < n - 1; i++)
        {
            bool swapped = false;
            for (int j = 0; j < n - 1 - i; j++)
            {
                iterations++;
                bool needSwap = ascending ? (arr[j] > arr[j + 1]) : (arr[j] < arr[j + 1]);

                steps.Add(new SortStep
                {
                    ArraySnapshot = (int[])arr.Clone(),
                    CompareIndex1 = j,
                    CompareIndex2 = j + 1,
                    Iterations = iterations,
                    Swaps = swaps,
                    IsDone = false
                });

                if (needSwap)
                {
                    (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                    swaps++;
                    swapped = true;

                    steps.Add(new SortStep
                    {
                        ArraySnapshot = (int[])arr.Clone(),
                        SwapIndex1 = j,
                        SwapIndex2 = j + 1,
                        Iterations = iterations,
                        Swaps = swaps,
                        IsDone = false
                    });
                }
            }
            if (!swapped) break;
        }

        steps.Add(new SortStep
        {
            ArraySnapshot = (int[])arr.Clone(),
            Iterations = iterations,
            Swaps = swaps,
            IsDone = true
        });

        return steps;
    }

    public SortResult Benchmark(int[] source, bool ascending, long maxIterations = 50000)
    {
        var arr = (int[])source.Clone();
        int n = arr.Length;
        long iterations = 0;
        long swaps = 0;

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < n - 1; i++)
        {
            bool swapped = false;
            for (int j = 0; j < n - 1 - i; j++)
            {
                iterations++;
                bool needSwap = ascending ? (arr[j] > arr[j + 1]) : (arr[j] < arr[j + 1]);
                if (needSwap)
                {
                    (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                    swaps++;
                    swapped = true;
                }
            }
            if (!swapped) break;
        }

        sw.Stop();

        return new SortResult
        {
            AlgorithmType = Type,
            Iterations = iterations,
            Swaps = swaps,
            ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
            Succeeded = true
        };
    }
}
