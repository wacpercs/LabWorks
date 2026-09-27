using System.Diagnostics;
using LabWorksApp.Models;

namespace LabWorksApp.Services.Sorting;

public class QuickSort : ISortAlgorithm
{
    public SortAlgorithmType Type => SortAlgorithmType.Quick;
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

        void QuickSortRecursive(int low, int high)
        {
            if (low >= high) return;

            int pivot = arr[high];
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                iterations++;
                bool condition = ascending ? (arr[j] <= pivot) : (arr[j] >= pivot);

                steps.Add(new SortStep
                {
                    ArraySnapshot = (int[])arr.Clone(),
                    CompareIndex1 = j,
                    CompareIndex2 = high,
                    Iterations = iterations,
                    Swaps = swaps,
                    IsDone = false
                });

                if (condition)
                {
                    i++;
                    if (i != j)
                    {
                        (arr[i], arr[j]) = (arr[j], arr[i]);
                        swaps++;

                        steps.Add(new SortStep
                        {
                            ArraySnapshot = (int[])arr.Clone(),
                            SwapIndex1 = i,
                            SwapIndex2 = j,
                            Iterations = iterations,
                            Swaps = swaps,
                            IsDone = false
                        });
                    }
                }
            }

            if (i + 1 != high)
            {
                (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);
                swaps++;

                steps.Add(new SortStep
                {
                    ArraySnapshot = (int[])arr.Clone(),
                    SwapIndex1 = i + 1,
                    SwapIndex2 = high,
                    Iterations = iterations,
                    Swaps = swaps,
                    IsDone = false
                });
            }

            int pIndex = i + 1;
            QuickSortRecursive(low, pIndex - 1);
            QuickSortRecursive(pIndex + 1, high);
        }

        QuickSortRecursive(0, n - 1);

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

        void QuickSortRecursive(int low, int high)
        {
            if (low >= high) return;

            int pivot = arr[high];
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                iterations++;
                bool condition = ascending ? (arr[j] <= pivot) : (arr[j] >= pivot);
                if (condition)
                {
                    i++;
                    if (i != j)
                    {
                        (arr[i], arr[j]) = (arr[j], arr[i]);
                        swaps++;
                    }
                }
            }

            if (i + 1 != high)
            {
                (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);
                swaps++;
            }

            int pIndex = i + 1;
            QuickSortRecursive(low, pIndex - 1);
            QuickSortRecursive(pIndex + 1, high);
        }

        QuickSortRecursive(0, n - 1);
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
