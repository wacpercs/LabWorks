using System.Diagnostics;
using LabWorksApp.Models;

namespace LabWorksApp.Services.Mathematics;

public class DichotomyMinimizer
{
    public const int MaxIterations = 1000;

    /// <summary>
    /// Поиск локального минимума функции f(x) на интервале [a, b] методом дихотомии.
    /// </summary>
    public DichotomyResult FindMinimum(Func<double, double> f, double a, double b, double epsilon)
    {
        if (epsilon <= 0)
            throw new ArgumentException("Погрешность epsilon должна быть строго больше нуля.");

        if (a > b)
            (a, b) = (b, a);

        if (Math.Abs(b - a) < 1e-15)
            throw new ArgumentException("Границы интервала [a, b] не должны совпадать.");

        var steps = new List<DichotomyStep>();
        double delta = epsilon / 3.0;
        if (delta <= 0 || delta >= (b - a) / 2.0)
            delta = (b - a) * 0.001;

        var sw = Stopwatch.StartNew();
        int step = 0;

        while ((b - a) >= epsilon && step < MaxIterations)
        {
            step++;
            double xm = (a + b) / 2.0;
            double x1 = xm - delta;
            double x2 = xm + delta;

            double f1 = f(x1);
            double f2 = f(x2);

            steps.Add(new DichotomyStep
            {
                StepNumber = step,
                A = a,
                B = b,
                X1 = x1,
                X2 = x2,
                F1 = f1,
                F2 = f2
            });

            if (f1 < f2)
            {
                b = x2;
            }
            else
            {
                a = x1;
            }
        }

        sw.Stop();

        double bestX = (a + b) / 2.0;
        double bestY = f(bestX);

        return new DichotomyResult
        {
            BestX = bestX,
            BestY = bestY,
            IterationsCount = step,
            AchievedPrecision = b - a,
            IsSuccess = (b - a) < epsilon || step < MaxIterations,
            Message = $"Локальный минимум успешно найден за {step} итераций.",
            Steps = steps,
            ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds
        };
    }
}
