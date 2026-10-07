using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LabWorksApp.Models;
using LabWorksApp.Services.Mathematics;

namespace LabWorksApp.Views;

public partial class Lab1Window : Window
{
    private readonly DichotomyMinimizer _minimizer = new();
    private readonly DichotomyRootFinder _rootFinder = new();

    private List<DichotomyStep> _currentSteps = new();
    private Func<double, double>? _currentFunction;

    public Lab1Window()
    {
        InitializeComponent();
        Loaded += (_, _) => BtnPlot_Click(this, new RoutedEventArgs());
    }

    private bool TryParseInputs(out Func<double, double>? func, out double a, out double b, out double eps, out string formula)
    {
        func = null;
        a = 0;
        b = 0;
        eps = 0.001;
        formula = TxtFormula.Text.Trim();

        if (string.IsNullOrWhiteSpace(formula))
        {
            MessageBox.Show("Формула функции f(x) не может быть пустой.", "Некорректный ввод", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        try
        {
            func = MathExpressionParser.Parse(formula);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка в формуле f(x):\n{ex.Message}", "Синтаксическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (!double.TryParse(TxtA.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out a))
        {
            MessageBox.Show("Левая граница 'a' должна быть вещественным числом.", "Некорректный ввод", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!double.TryParse(TxtB.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out b))
        {
            MessageBox.Show("Правая граница 'b' должна быть вещественным числом.", "Некорректный ввод", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (Math.Abs(b - a) < 1e-12)
        {
            MessageBox.Show("Границы интервала [a; b] не должны совпадать.", "Некорректный интервал", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (a > b)
        {
            (a, b) = (b, a);
            TxtA.Text = a.ToString("G4");
            TxtB.Text = b.ToString("G4");
        }

        if (!double.TryParse(TxtEpsilon.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out eps) || eps <= 0)
        {
            MessageBox.Show("Точность 'e' (погрешность) должна быть строго положительным числом (например, 0.001).", "Некорректная погрешность", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private void BtnPlot_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseInputs(out var func, out double a, out double b, out _, out _))
            return;

        _currentFunction = func;
        PlotControl.Plot(func!, a, b);
        StatusMessage.Text = $"График функции f(x) успешно построен на интервале [{a:G4}; {b:G4}].";
    }

    private void BtnCalculate_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseInputs(out var func, out double a, out double b, out double eps, out _))
            return;

        _currentFunction = func;
        _currentSteps.Clear();

        double? minX = null;
        double? minY = null;
        double? rootX = null;
        double? rootY = null;
        double totalMs = 0;

        bool calcMin = RadioMin.IsChecked == true || RadioBoth.IsChecked == true;
        bool calcRoot = RadioRoot.IsChecked == true || RadioBoth.IsChecked == true;

        DichotomyResult? minResult = null;
        DichotomyResult? rootResult = null;

        // 1. Поиск минимума
        if (calcMin)
        {
            try
            {
                minResult = _minimizer.FindMinimum(func!, a, b, eps);
                minX = minResult.BestX;
                minY = minResult.BestY;
                totalMs += minResult.ElapsedMilliseconds;

                TxtResultMinX.Text = minResult.FormattedBestX;
                TxtResultMinY.Text = minResult.FormattedBestY;
                TxtResultMinIter.Text = minResult.IterationsCount.ToString();
                TxtResultMinPrec.Text = minResult.FormattedPrecision;

                _currentSteps.AddRange(minResult.Steps);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске минимума: {ex.Message}", "Ошибка расчета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            TxtResultMinX.Text = "—";
            TxtResultMinY.Text = "—";
            TxtResultMinIter.Text = "—";
            TxtResultMinPrec.Text = "—";
        }

        // 2. Поиск корня
        if (calcRoot)
        {
            try
            {
                rootResult = _rootFinder.FindRoot(func!, a, b, eps);
                rootX = rootResult.BestX;
                rootY = rootResult.BestY;
                totalMs += rootResult.ElapsedMilliseconds;

                TxtResultRootX.Text = rootResult.FormattedBestX;
                TxtResultRootY.Text = rootResult.FormattedBestY;
                TxtResultRootIter.Text = rootResult.IterationsCount.ToString();

                if (!calcMin)
                {
                    _currentSteps.AddRange(rootResult.Steps);
                }
            }
            catch (Exception ex)
            {
                if (RadioRoot.IsChecked == true)
                {
                    MessageBox.Show(ex.Message, "Поиск корня", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                TxtResultRootX.Text = "Не найден";
                TxtResultRootY.Text = "—";
                TxtResultRootIter.Text = "—";
            }
        }
        else
        {
            TxtResultRootX.Text = "—";
            TxtResultRootY.Text = "—";
            TxtResultRootIter.Text = "—";
        }

        TxtResultTime.Text = totalMs < 1.0 ? $"{totalMs * 1000.0:F1} мкс" : $"{totalMs:F3} мс";

        string notes = "";
        if (minResult != null) notes += minResult.Message + " ";
        if (rootResult != null && rootResult.IsSuccess) notes += rootResult.Message;
        TxtResultNote.Text = string.IsNullOrWhiteSpace(notes) ? "Расчет завершен." : notes;

        // Обновляем график с маркерными точками
        PlotControl.Plot(func!, a, b, minX, minY, rootX, rootY);

        // Обновляем DataGrid протокола
        GridSteps.ItemsSource = null;
        GridSteps.ItemsSource = _currentSteps;
        TxtStepsCount.Text = $"Шагов: {_currentSteps.Count}";

        StatusMessage.Text = $"Расчет завершен. Выполнено итераций: {_currentSteps.Count}. Время: {TxtResultTime.Text}.";
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        TxtFormula.Text = "";
        TxtA.Text = "0";
        TxtB.Text = "1";
        TxtEpsilon.Text = "0.001";

        TxtResultMinX.Text = "—";
        TxtResultMinY.Text = "—";
        TxtResultMinIter.Text = "—";
        TxtResultMinPrec.Text = "—";

        TxtResultRootX.Text = "—";
        TxtResultRootY.Text = "—";
        TxtResultRootIter.Text = "—";
        TxtResultTime.Text = "—";
        TxtResultNote.Text = "Поля очищены.";

        PlotControl.Clear();
        _currentSteps.Clear();
        GridSteps.ItemsSource = null;
        TxtStepsCount.Text = "Шагов: 0";
        StatusMessage.Text = "Форма очищена.";
    }

    private void SetExample(string formula, double a, double b, double eps = 0.001)
    {
        TxtFormula.Text = formula;
        TxtA.Text = a.ToString(CultureInfo.InvariantCulture);
        TxtB.Text = b.ToString(CultureInfo.InvariantCulture);
        TxtEpsilon.Text = eps.ToString(CultureInfo.InvariantCulture);

        BtnCalculate_Click(this, new RoutedEventArgs());
    }

    private void MenuExample1_Click(object sender, RoutedEventArgs e) => SetExample("x^2 - 4*x + 3", 0, 5);
    private void MenuExample2_Click(object sender, RoutedEventArgs e) => SetExample("2*x^2 + 3*x - 5", -3, 2);
    private void MenuExample3_Click(object sender, RoutedEventArgs e) => SetExample("(x - 2)^2 + 1", 0, 4);
    private void MenuExample4_Click(object sender, RoutedEventArgs e) => SetExample("x^3 - 3*x", 0, 2);
    private void MenuExample5_Click(object sender, RoutedEventArgs e) => SetExample("sin(x) + x^2 / 4", -3, 2);

    private void ComboPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboPresets.SelectedItem is ComboBoxItem item && item.Content is string formula)
        {
            TxtFormula.Text = formula;
            if (formula.Contains("x^2 - 4*x + 3")) { TxtA.Text = "0"; TxtB.Text = "5"; }
            else if (formula.Contains("2*x^2 + 3*x - 5")) { TxtA.Text = "-3"; TxtB.Text = "2"; }
            else if (formula.Contains("(x - 2)^2 + 1")) { TxtA.Text = "0"; TxtB.Text = "4"; }
            else if (formula.Contains("x^3 - 3*x")) { TxtA.Text = "0"; TxtB.Text = "2"; }
            else if (formula.Contains("x^2 - 4")) { TxtA.Text = "0"; TxtB.Text = "4"; }
        }
    }

    private void MenuExport_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSteps.Count == 0)
        {
            MessageBox.Show("Таблица итераций пуста. Сначала выполните расчет.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Title = "Сохранить протокол итераций",
            Filter = "Текстовые файлы (*.csv;*.txt)|*.csv;*.txt|Все файлы (*.*)|*.*",
            FileName = "dichotomy_iterations.csv"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                var lines = new List<string> { "Step;A;B;X1;X2;F1;F2;IntervalLength" };
                foreach (var s in _currentSteps)
                {
                    lines.Add($"{s.StepNumber};{s.FormattedA};{s.FormattedB};{s.FormattedX1};{s.FormattedX2};{s.FormattedF1};{s.FormattedF2};{s.FormattedLength}");
                }
                File.WriteAllLines(sfd.FileName, lines);
                MessageBox.Show($"Протокол успешно экспортирован ({_currentSteps.Count} шагов) в файл:\n{sfd.FileName}", "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MenuHelpTask_Click(object sender, RoutedEventArgs e)
    {
        string task =
            "ЗАДАНИЕ К ЛАБОРАТОРНОЙ РАБОТЕ №1:\n\n" +
            "Используя метод половинного деления (дихотомии) найти локальный минимум заданной точности e функции f(x) на интервале [a, b].\n\n" +
            "ТРЕБОВАНИЯ:\n" +
            "1. Ввод входных данных с использованием стандартных компонентов WPF (a, b, e, f(x)).\n" +
            "2. Вывести график функции и найденную точку минимума.\n" +
            "3. Элементы взаимодействия (кнопки «Рассчитать», «Очистить») размещать в MenuStrip (WPF Menu).\n" +
            "4. Обеспечить отказоустойчивость для входных данных с оповещением пользователя.";

        MessageBox.Show(task, "Задание — Лабораторная работа №1", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MenuHelpTheory_Click(object sender, RoutedEventArgs e)
    {
        string theory =
            "МЕТОД ДИХОТОМИИ (ОДНОМЕРНАЯ ОПТИМИЗАЦИЯ):\n\n" +
            "1. На каждом шаге середина интервала делится двумя пробными точками:\n" +
            "   x1 = (a + b)/2 - delta\n" +
            "   x2 = (a + b)/2 + delta (где delta < e/2)\n\n" +
            "2. Вычисляются значения функции f(x1) и f(x2).\n" +
            "   Если f(x1) < f(x2), то минимум лежит левее x2 -> b = x2.\n" +
            "   Иначе минимум лежит правее x1 -> a = x1.\n\n" +
            "3. Процесс повторяется, пока длина интервала (b - a) не станет меньше заданной погрешности e.";

        MessageBox.Show(theory, "Теория — Метод половинного деления", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MenuClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
