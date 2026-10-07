using System.Diagnostics;
using LabWorksApp.Models;

namespace LabWorksApp.Services.Mathematics;

public class DichotomyRootFinder
{
    public const int MaxIterations = 1000;

    /// <summary>
    /// Поиск корня f(x) = 0 на интервале [a, b] методом половинного деления (с контролем смены знака).
    /// </summary>
    public DichotomyResult FindRoot(Func<double, double> f, double a, double b, double epsilon)
    {
        if (epsilon <= 0)
            throw new ArgumentException("Погрешность epsilon должна быть строго больше нуля.");

        if (a > b)
            (a, b) = (b, a);

        double fa = f(a);
        double fb = f(b);

        if (Math.Abs(fa) < 1e-12)
        {
            return new DichotomyResult
            {
                BestX = a,
                BestY = fa,
                IterationsCount = 0,
                AchievedPrecision = 0,
                IsSuccess = true,
                Message = "Левая граница 'a' уже является точным корнем."
            };
        }

        if (Math.Abs(fb) < 1e-12)
        {
            return new DichotomyResult
            {
                BestX = b,
                BestY = fb,
                IterationsCount = 0,
                AchievedPrecision = 0,
                IsSuccess = true,
                Message = "Правая граница 'b' уже является точным корнем."
            };
        }

        // Если знаки одинаковые, ищем отрезок перемены знака внутри [a, b]
        if (fa * fb > 0)
        {
            bool foundSubinterval = false;
            int nSub = 100;
            double h = (b - a) / nSub;
            double curX = a;
            double curF = fa;

            for (int i = 0; i < nSub; i++)
            {
                double nextX = curX + h;
                double nextF = f(nextX);
                if (curF * nextF <= 0)
                {
                    a = curX;
                    b = nextX;
                    fa = curF;
                    fb = nextF;
                    foundSubinterval = true;
                    break;
                }
                curX = nextX;
                curF = nextF;
            }

            if (!foundSubinterval)
            {
                throw new InvalidOperationException($"На заданном интервале функция не меняет знак (f(a) = {fa:F3}, f(b) = {fb:F3}). Метод половинного деления требует смены знака f(a)*f(b) <= 0.");
            }
        }

        var steps = new List<DichotomyStep>();
        var sw = Stopwatch.StartNew();
        int step = 0;
        double c = (a + b) / 2.0;

        while ((b - a) / 2.0 >= epsilon && step < MaxIterations)
        {
            step++;
            c = (a + b) / 2.0;
            double fc = f(c);

            steps.Add(new DichotomyStep
            {
                StepNumber = step,
                A = a,
                B = b,
                X1 = c,
                X2 = double.NaN,
                F1 = fc,
                F2 = double.NaN
            });

            if (Math.Abs(fc) < 1e-14)
                break;

            if (fa * fc <= 0)
            {
                b = c;
                fb = fc;
            }
            else
            {
                a = c;
                fa = fc;
            }
        }

        sw.Stop();
        c = (a + b) / 2.0;
        double rootY = f(c);

        return new DichotomyResult
        {
            BestX = c,
            BestY = rootY,
            IterationsCount = step,
            AchievedPrecision = (b - a) / 2.0,
            IsSuccess = true,
            Message = $"Корень успешно найден: x = {c:F6}, f(x) = {rootY:E3} (за {step} шагов).",
            Steps = steps,
            ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds
        };
    }
}
