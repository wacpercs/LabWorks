using System.Diagnostics;
using LabWorksApp.Models;

namespace LabWorksApp.Services.Sorting;

public class ShakerSort : ISortAlgorithm
{
    public SortAlgorithmType Type => SortAlgorithmType.Shaker;
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

        int left = 0;
        int right = n - 1;

        while (left < right)
        {
            bool swapped = false;

            // Forward pass
            for (int i = left; i < right; i++)
            {
                iterations++;
                bool needSwap = ascending ? (arr[i] > arr[i + 1]) : (arr[i] < arr[i + 1]);

                steps.Add(new SortStep
                {
                    ArraySnapshot = (int[])arr.Clone(),
                    CompareIndex1 = i,
                    CompareIndex2 = i + 1,
                    Iterations = iterations,
                    Swaps = swaps,
                    IsDone = false
                });

                if (needSwap)
                {
                    (arr[i], arr[i + 1]) = (arr[i + 1], arr[i]);
                    swaps++;
                    swapped = true;

                    steps.Add(new SortStep
                    {
                        ArraySnapshot = (int[])arr.Clone(),
                        SwapIndex1 = i,
                        SwapIndex2 = i + 1,
                        Iterations = iterations,
                        Swaps = swaps,
                        IsDone = false
                    });
                }
            }
            right--;

            if (!swapped) break;
            swapped = false;

            // Backward pass
            for (int i = right; i > left; i--)
            {
                iterations++;
                bool needSwap = ascending ? (arr[i - 1] > arr[i]) : (arr[i - 1] < arr[i]);

                steps.Add(new SortStep
                {
                    ArraySnapshot = (int[])arr.Clone(),
                    CompareIndex1 = i - 1,
                    CompareIndex2 = i,
                    Iterations = iterations,
                    Swaps = swaps,
                    IsDone = false
                });

                if (needSwap)
                {
                    (arr[i - 1], arr[i]) = (arr[i], arr[i - 1]);
                    swaps++;
                    swapped = true;

                    steps.Add(new SortStep
                    {
                        ArraySnapshot = (int[])arr.Clone(),
                        SwapIndex1 = i - 1,
                        SwapIndex2 = i,
                        Iterations = iterations,
                        Swaps = swaps,
                        IsDone = false
                    });
                }
            }
            left++;

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

        int left = 0;
        int right = n - 1;

        while (left < right)
        {
            bool swapped = false;

            for (int i = left; i < right; i++)
            {
                iterations++;
                bool needSwap = ascending ? (arr[i] > arr[i + 1]) : (arr[i] < arr[i + 1]);
                if (needSwap)
                {
                    (arr[i], arr[i + 1]) = (arr[i + 1], arr[i]);
                    swaps++;
                    swapped = true;
                }
            }
            right--;

            if (!swapped) break;
            swapped = false;

            for (int i = right; i > left; i--)
            {
                iterations++;
                bool needSwap = ascending ? (arr[i - 1] > arr[i]) : (arr[i - 1] < arr[i]);
                if (needSwap)
                {
                    (arr[i - 1], arr[i]) = (arr[i], arr[i - 1]);
                    swaps++;
                    swapped = true;
                }
            }
            left++;

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
