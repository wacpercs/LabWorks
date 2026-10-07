using LabWorksApp.Services.Mathematics;

namespace LabWorksApp.Tests;

public class Lab1MathTests
{
    [Theory]
    [InlineData("x^2 - 4*x + 3", 2.0, -1.0)]
    [InlineData("x^2 - 4*x + 3", 0.0, 3.0)]
    [InlineData("2*x + 5", 3.0, 11.0)]
    [InlineData("-x^2 + 4", 2.0, 0.0)]
    [InlineData("(x - 2)^2 + 1", 2.0, 1.0)]
    [InlineData("2x - 3", 4.0, 5.0)]
    public void MathParser_EvaluatesCorrectly(string expr, double x, double expected)
    {
        var f = MathExpressionParser.Parse(expr);
        double result = f(x);
        Assert.Equal(expected, result, 4);
    }

    [Fact]
    public void MathParser_EvaluatesTrigonometricAndMathFunctions()
    {
        var fSin = MathExpressionParser.Parse("sin(x)");
        Assert.Equal(0.0, fSin(0.0), 4);
        Assert.Equal(1.0, fSin(Math.PI / 2.0), 4);

        var fSqrt = MathExpressionParser.Parse("sqrt(x) + 2");
        Assert.Equal(5.0, fSqrt(9.0), 4);

        var fExp = MathExpressionParser.Parse("exp(x)");
        Assert.Equal(1.0, fExp(0.0), 4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("(x + 2")]
    [InlineData("x + 2)")]
    [InlineData("unknown_func(x)")]
    [InlineData("x @ 5")]
    public void MathParser_ThrowsOnInvalidInput(string expr)
    {
        Assert.Throws<ArgumentException>(() => MathExpressionParser.Parse(expr));
    }

    [Fact]
    public void DichotomyMinimizer_FindsQuadraticMinimum_Correctly()
    {
        var minimizer = new DichotomyMinimizer();
        // f(x) = x^2 - 4x + 3, минимум в x = 2, f(2) = -1
        var f = MathExpressionParser.Parse("x^2 - 4*x + 3");
        double a = 0.0;
        double b = 5.0;
        double eps = 0.001;

        var result = minimizer.FindMinimum(f, a, b, eps);

        Assert.True(result.IsSuccess);
        Assert.InRange(result.BestX, 1.99, 2.01);
        Assert.InRange(result.BestY, -1.01, -0.99);
        Assert.True(result.Steps.Count > 0);
        Assert.True(result.AchievedPrecision < eps);
    }

    [Fact]
    public void DichotomyMinimizer_FindsShiftedMinimum()
    {
        var minimizer = new DichotomyMinimizer();
        // f(x) = 2*(x + 3)^2 + 5, минимум в x = -3, f(-3) = 5
        var f = MathExpressionParser.Parse("2*(x + 3)^2 + 5");
        double a = -6.0;
        double b = 0.0;
        double eps = 0.0001;

        var result = minimizer.FindMinimum(f, a, b, eps);

        Assert.True(result.IsSuccess);
        Assert.InRange(result.BestX, -3.001, -2.999);
        Assert.InRange(result.BestY, 4.999, 5.001);
    }

    [Fact]
    public void DichotomyMinimizer_ThrowsOnInvalidParameters()
    {
        var minimizer = new DichotomyMinimizer();
        var f = (Func<double, double>)(x => x * x);

        Assert.Throws<ArgumentException>(() => minimizer.FindMinimum(f, 0, 5, -0.01));
        Assert.Throws<ArgumentException>(() => minimizer.FindMinimum(f, 2, 2, 0.01));
    }

    [Fact]
    public void DichotomyRootFinder_FindsRoot_WhenSignChanges()
    {
        var rootFinder = new DichotomyRootFinder();
        // f(x) = x^2 - 4, корень на [0; 3] в x = 2
        var f = MathExpressionParser.Parse("x^2 - 4");
        double a = 0.0;
        double b = 3.0;
        double eps = 0.001;

        var result = rootFinder.FindRoot(f, a, b, eps);

        Assert.True(result.IsSuccess);
        Assert.InRange(result.BestX, 1.99, 2.01);
        Assert.InRange(Math.Abs(result.BestY), 0.0, 0.01);
        Assert.True(result.Steps.Count > 0);
    }

    [Fact]
    public void DichotomyRootFinder_ThrowsWhenNoSignChange()
    {
        var rootFinder = new DichotomyRootFinder();
        // f(x) = x^2 + 1 > 0 всюду
        var f = MathExpressionParser.Parse("x^2 + 1");
        double a = -5.0;
        double b = 5.0;

        Assert.Throws<InvalidOperationException>(() => rootFinder.FindRoot(f, a, b, 0.001));
    }
}
