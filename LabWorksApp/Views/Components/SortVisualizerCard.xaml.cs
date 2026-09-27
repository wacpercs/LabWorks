using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LabWorksApp.Models;
using LabWorksApp.Services.Sorting;

namespace LabWorksApp.Views.Components;

public partial class SortVisualizerCard : UserControl
{
    private static readonly SolidColorBrush DefaultBarBrush = new(Color.FromRgb(59, 130, 246));   // Blue
    private static readonly SolidColorBrush CompareBarBrush = new(Color.FromRgb(245, 158, 11));  // Amber
    private static readonly SolidColorBrush SwapBarBrush = new(Color.FromRgb(239, 68, 68));      // Red
    private static readonly SolidColorBrush DoneBarBrush = new(Color.FromRgb(16, 185, 129));     // Green
    private static readonly SolidColorBrush TextColorBrush = new(Color.FromRgb(148, 163, 184));

    public ISortAlgorithm? Algorithm { get; private set; }
    public IReadOnlyList<SortStep>? Steps { get; private set; }
    public SortResult? Result { get; private set; }

    private int[] _lastArray = Array.Empty<int>();
    private int _lastStepIndex = -1;

    public SortVisualizerCard()
    {
        InitializeComponent();
    }

    public void Setup(ISortAlgorithm algorithm, int[] sourceArray, bool ascending, long maxIterations)
    {
        Algorithm = algorithm;
        TxtTitle.Text = algorithm.Name;

        // Генерация шагов для визуализации
        Steps = algorithm.GenerateSteps(sourceArray, ascending, maxIterations);

        // Расчёт метрик скорости (бенчмарк)
        Result = algorithm.Benchmark(sourceArray, ascending, maxIterations);

        TxtStatus.Text = "В ожидании";
        TxtStatus.Foreground = TextColorBrush;
        BadgeFastest.Visibility = Visibility.Collapsed;

        TxtIterations.Text = "Итераций: 0";
        TxtSwaps.Text = "Обменов: 0";
        TxtTime.Text = Result.FormattedTime;

        _lastArray = (int[])sourceArray.Clone();
        _lastStepIndex = -1;

        if (Steps.Count > 0)
        {
            RenderStep(0);
        }
    }

    public void SetFastest(bool isFastest)
    {
        BadgeFastest.Visibility = isFastest ? Visibility.Visible : Visibility.Collapsed;
    }

    public bool RenderStep(int stepIndex)
    {
        if (Steps == null || Steps.Count == 0) return true;

        int clampedIndex = Math.Clamp(stepIndex, 0, Steps.Count - 1);
        _lastStepIndex = clampedIndex;

        var step = Steps[clampedIndex];
        _lastArray = step.ArraySnapshot;

        TxtIterations.Text = $"Итераций: {step.Iterations}";
        TxtSwaps.Text = $"Обменов: {step.Swaps}";

        bool isFinished = step.IsDone || clampedIndex >= Steps.Count - 1;

        if (isFinished)
        {
            if (Result?.Succeeded == false)
            {
                TxtStatus.Text = "Лимит исчерпан";
                TxtStatus.Foreground = SwapBarBrush;
            }
            else
            {
                TxtStatus.Text = "Завершено";
                TxtStatus.Foreground = DoneBarBrush;
            }
        }
        else
        {
            TxtStatus.Text = "Сортировка...";
            TxtStatus.Foreground = CompareBarBrush;
        }

        DrawBars(_lastArray, step.CompareIndex1, step.CompareIndex2, step.SwapIndex1, step.SwapIndex2, isFinished);
        return isFinished;
    }

    private void CanvasBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_lastArray.Length > 0 && Steps != null && _lastStepIndex >= 0)
        {
            var step = Steps[_lastStepIndex];
            bool isFinished = step.IsDone || _lastStepIndex >= Steps.Count - 1;
            DrawBars(_lastArray, step.CompareIndex1, step.CompareIndex2, step.SwapIndex1, step.SwapIndex2, isFinished);
        }
    }

    private void DrawBars(int[] arr, int cmp1, int cmp2, int swp1, int swp2, bool isDone)
    {
        BarsCanvas.Children.Clear();

        double canvasWidth = BarsCanvas.ActualWidth;
        double canvasHeight = BarsCanvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0 || arr.Length == 0)
            return;

        int minVal = arr.Min();
        int maxVal = arr.Max();

        // Базовая нормализация для правильного отображения даже отрицательных чисел
        int effectiveMin = Math.Min(0, minVal);
        int effectiveMax = Math.Max(1, maxVal);
        int range = effectiveMax - effectiveMin;
        if (range <= 0) range = 1;

        double barSpacing = 2;
        double totalSpacing = barSpacing * (arr.Length + 1);
        double barWidth = Math.Max(2, (canvasWidth - totalSpacing) / arr.Length);

        for (int i = 0; i < arr.Length; i++)
        {
            int val = arr[i];
            double normalizedHeight = (double)(val - effectiveMin) / range;
            // Минимальная видимая высота столбца - 4 пикселя, максимальная - 90% высоты канваса
            double barHeight = Math.Max(4, normalizedHeight * (canvasHeight - 16));

            SolidColorBrush brush;
            if (isDone)
            {
                brush = DoneBarBrush;
            }
            else if (i == swp1 || i == swp2)
            {
                brush = SwapBarBrush;
            }
            else if (i == cmp1 || i == cmp2)
            {
                brush = CompareBarBrush;
            }
            else
            {
                brush = DefaultBarBrush;
            }

            var rect = new Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                Fill = brush,
                RadiusX = 2,
                RadiusY = 2
            };

            double x = barSpacing + i * (barWidth + barSpacing);
            double y = canvasHeight - barHeight - 2;

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            BarsCanvas.Children.Add(rect);

            // Если элементов немного (до 25), подписываем значения над столбцами
            if (arr.Length <= 25 && barWidth >= 14)
            {
                var label = new TextBlock
                {
                    Text = val.ToString(),
                    FontSize = Math.Clamp(barWidth * 0.5, 8, 10),
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    Width = barWidth + 4
                };
                Canvas.SetLeft(label, x - 2);
                Canvas.SetTop(label, Math.Max(0, y - 14));
                BarsCanvas.Children.Add(label);
            }
        }
    }
}
