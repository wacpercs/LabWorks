using System.Diagnostics;
using LabWorksApp.Models;

namespace LabWorksApp.Services.Sorting;

public class InsertionSort : ISortAlgorithm
{
    public SortAlgorithmType Type => SortAlgorithmType.Insertion;
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

        for (int i = 1; i < n; i++)
        {
            int j = i;
            while (j > 0)
            {
                iterations++;
                bool needSwap = ascending ? (arr[j] < arr[j - 1]) : (arr[j] > arr[j - 1]);

                steps.Add(new SortStep
                {
                    ArraySnapshot = (int[])arr.Clone(),
                    CompareIndex1 = j - 1,
                    CompareIndex2 = j,
                    Iterations = iterations,
                    Swaps = swaps,
                    IsDone = false
                });

                if (needSwap)
                {
                    (arr[j], arr[j - 1]) = (arr[j - 1], arr[j]);
                    swaps++;

                    steps.Add(new SortStep
                    {
                        ArraySnapshot = (int[])arr.Clone(),
                        SwapIndex1 = j - 1,
                        SwapIndex2 = j,
                        Iterations = iterations,
                        Swaps = swaps,
                        IsDone = false
                    });
                    j--;
                }
                else
                {
                    break;
                }
            }
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

        for (int i = 1; i < n; i++)
        {
            int j = i;
            while (j > 0)
            {
                iterations++;
                bool needSwap = ascending ? (arr[j] < arr[j - 1]) : (arr[j] > arr[j - 1]);
                if (needSwap)
                {
                    (arr[j], arr[j - 1]) = (arr[j - 1], arr[j]);
                    swaps++;
                    j--;
                }
                else
                {
                    break;
                }
            }
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
