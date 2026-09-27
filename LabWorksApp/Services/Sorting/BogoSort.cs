using System.Diagnostics;
using LabWorksApp.Models;

namespace LabWorksApp.Services.Sorting;

public class BogoSort : ISortAlgorithm
{
    public SortAlgorithmType Type => SortAlgorithmType.Bogo;
    public string Name => Type.GetDisplayName();

    private static bool IsSorted(int[] arr, bool ascending)
    {
        for (int i = 0; i < arr.Length - 1; i++)
        {
            if (ascending && arr[i] > arr[i + 1]) return false;
            if (!ascending && arr[i] < arr[i + 1]) return false;
        }
        return true;
    }

    private static void Shuffle(int[] arr, Random rnd)
    {
        int n = arr.Length;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    public IReadOnlyList<SortStep> GenerateSteps(int[] source, bool ascending, long maxIterations = 50000)
    {
        var steps = new List<SortStep>();
        var arr = (int[])source.Clone();
        int n = arr.Length;
        long iterations = 0;
        long swaps = 0;
        var rnd = new Random(42); // deterministic seed for reproducible animation

        steps.Add(new SortStep
        {
            ArraySnapshot = (int[])arr.Clone(),
            Iterations = 0,
            Swaps = 0,
            IsDone = n <= 1 || IsSorted(arr, ascending)
        });

        if (n <= 1) return steps;

        // Cap animation steps to at most 1000 so UI animation remains responsive
        long maxAnimSteps = Math.Min(maxIterations, 1000);

        while (!IsSorted(arr, ascending) && iterations < maxAnimSteps)
        {
            iterations++;
            Shuffle(arr, rnd);
            swaps += n;

            steps.Add(new SortStep
            {
                ArraySnapshot = (int[])arr.Clone(),
                Iterations = iterations,
                Swaps = swaps,
                IsDone = false
            });
        }

        bool sorted = IsSorted(arr, ascending);
        steps.Add(new SortStep
        {
            ArraySnapshot = (int[])arr.Clone(),
            Iterations = iterations,
            Swaps = swaps,
            IsDone = sorted
        });

        return steps;
    }

    public SortResult Benchmark(int[] source, bool ascending, long maxIterations = 50000)
    {
        var arr = (int[])source.Clone();
        int n = arr.Length;
        long iterations = 0;
        long swaps = 0;
        var rnd = new Random();

        var sw = Stopwatch.StartNew();

        while (!IsSorted(arr, ascending) && iterations < maxIterations)
        {
            iterations++;
            Shuffle(arr, rnd);
            swaps += n;
        }

        sw.Stop();
        bool sorted = IsSorted(arr, ascending);

        return new SortResult
        {
            AlgorithmType = Type,
            Iterations = iterations,
            Swaps = swaps,
            ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
            Succeeded = sorted,
            Note = sorted ? "Отсортировано" : $"Лимит {maxIterations} исчерпан"
        };
    }
}
