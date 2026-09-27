using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using LabWorksApp.Models;
using LabWorksApp.Services;
using LabWorksApp.Services.Sorting;
using LabWorksApp.Views.Components;

namespace LabWorksApp.Views;

public partial class Lab4Window : Window
{
    private readonly ArrayGeneratorService _generatorService = new();
    private readonly DataImportService _importService = new();

    private readonly ObservableCollection<ArrayItem> _arrayItems = new();
    private readonly List<SortVisualizerCard> _activeCards = new();
    private readonly DispatcherTimer _animTimer = new();

    private int _currentAnimationStep = 0;
    private bool _isPaused = false;

    // Сводная строка для таблицы результатов (по строкам к/и и t)
    public class ResultTableRow
    {
        public string MetricName { get; set; } = string.Empty;
        public string Bubble { get; set; } = "-";
        public string Shaker { get; set; } = "-";
        public string Insertion { get; set; } = "-";
        public string Quick { get; set; } = "-";
        public string Bogo { get; set; } = "-";
    }

    public Lab4Window()
    {
        InitializeComponent();

        GridArray.ItemsSource = _arrayItems;
        _arrayItems.CollectionChanged += (_, _) => UpdateArrayCountLabel();

        _animTimer.Tick += AnimTimer_Tick;
        UpdateTimerInterval();

        // Сгенерируем начальный демонстрационный массив
        GenerateDefaultArray();
    }

    private void UpdateArrayCountLabel()
    {
        TxtArrayCount.Text = $"Элементов: {_arrayItems.Count} / {ArrayGeneratorService.MaxArraySize}";
    }

    private void GenerateDefaultArray()
    {
        try
        {
            int[] defaultArray = _generatorService.Generate(-50, 50, 20);
            LoadArrayIntoGrid(defaultArray);
        }
        catch
        {
            // fallback
        }
    }

    private void LoadArrayIntoGrid(int[] numbers)
    {
        _arrayItems.Clear();
        int count = Math.Min(numbers.Length, ArrayGeneratorService.MaxArraySize);
        for (int i = 0; i < count; i++)
        {
            _arrayItems.Add(new ArrayItem { Index = i + 1, Value = numbers[i] });
        }
        StatusMessage.Text = $"Загружен массив из {count} элементов.";
    }

    private int[] GetCurrentArray()
    {
        return _arrayItems.Select(x => x.Value).ToArray();
    }

    private void BtnGenerateArray_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtRangeA.Text.Trim(), out int a))
        {
            MessageBox.Show("Граница A должна быть целым числом.", "Некорректный ввод", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtRangeB.Text.Trim(), out int b))
        {
            MessageBox.Show("Граница B должна быть целым числом.", "Некорректный ввод", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtCountN.Text.Trim(), out int n) || n <= 0)
        {
            MessageBox.Show("Количество элементов N должно быть положительным числом от 1 до 50.", "Некорректный ввод", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (n > ArrayGeneratorService.MaxArraySize)
        {
            MessageBox.Show($"По условию лабораторной работы количество элементов не должно превышать {ArrayGeneratorService.MaxArraySize}.", "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
            n = ArrayGeneratorService.MaxArraySize;
            TxtCountN.Text = n.ToString();
        }

        try
        {
            int[] arr = _generatorService.Generate(a, b, n);
            LoadArrayIntoGrid(arr);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка генерации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnStartSort_Click(object sender, RoutedEventArgs e)
    {
        var rawArray = GetCurrentArray();
        if (rawArray.Length == 0)
        {
            MessageBox.Show("Массив пуст. Сгенерируйте данные или введите их в таблицу.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (rawArray.Length > ArrayGeneratorService.MaxArraySize)
        {
            MessageBox.Show($"Количество элементов ({rawArray.Length}) превышает максимальное ограничение {ArrayGeneratorService.MaxArraySize}.", "Превышение лимита", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Выбранные алгоритмы
        var selectedAlgos = new List<ISortAlgorithm>();
        if (ChkBubble.IsChecked == true) selectedAlgos.Add(new BubbleSort());
        if (ChkShaker.IsChecked == true) selectedAlgos.Add(new ShakerSort());
        if (ChkInsertion.IsChecked == true) selectedAlgos.Add(new InsertionSort());
        if (ChkQuick.IsChecked == true) selectedAlgos.Add(new QuickSort());
        if (ChkBogo.IsChecked == true) selectedAlgos.Add(new BogoSort());

        if (selectedAlgos.Count == 0)
        {
            MessageBox.Show("Выберите хотя бы один алгоритм сортировки из списка чекбоксов.", "Алгоритмы не выбраны", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Лимит для Bogo
        long bogoLimit = 10000;
        if (ChkBogo.IsChecked == true)
        {
            if (!long.TryParse(TxtBogoLimit.Text.Trim(), out bogoLimit) || bogoLimit <= 0)
            {
                MessageBox.Show("Укажите корректный лимит итераций для BOGO сортировки (положительное число).", "Лимит BOGO", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        bool ascending = RadioAscending.IsChecked == true;

        // Остановим текущую анимацию
        _animTimer.Stop();
        _isPaused = false;
        BtnPause.Content = "⏸ Пауза";
        BtnPause.IsEnabled = true;

        PanelVisualizers.Children.Clear();
        _activeCards.Clear();

        // Инициализируем карточки визуализации для всех выбранных алгоритмов
        foreach (var algo in selectedAlgos)
        {
            var card = new SortVisualizerCard();
            card.Setup(algo, rawArray, ascending, bogoLimit);
            _activeCards.Add(card);
            PanelVisualizers.Children.Add(card);
        }

        // Поиск самого быстрого алгоритма (среди успешных)
        var successfulResults = _activeCards
            .Where(c => c.Result != null && c.Result.Succeeded)
            .Select(c => c.Result!)
            .ToList();

        SortResult? fastest = null;
        if (successfulResults.Count > 0)
        {
            fastest = successfulResults.OrderBy(r => r.ElapsedMilliseconds).First();
            fastest.IsFastest = true;

            var fastestCard = _activeCards.FirstOrDefault(c => c.Algorithm?.Type == fastest.AlgorithmType);
            fastestCard?.SetFastest(true);

            TxtFastestSummary.Text = $"Самый быстрый: {fastest.Name} ({fastest.FormattedTime})";
        }
        else
        {
            TxtFastestSummary.Text = "Самый быстрый: —";
        }

        // Формирование сводной таблицы результатов (к/и и t)
        PopulateSummaryTable();

        // Запуск одновременной пошаговой анимации
        _currentAnimationStep = 0;
        StatusMessage.Text = $"Запущена одновременная сортировка ({selectedAlgos.Count} алгоритм(ов)). Направление: {(ascending ? "по возрастанию" : "по убыванию")}.";
        _animTimer.Start();
    }

    private void PopulateSummaryTable()
    {
        var rowIterations = new ResultTableRow { MetricName = "к/и (итерации)" };
        var rowTime = new ResultTableRow { MetricName = "t (время)" };

        foreach (var card in _activeCards)
        {
            var res = card.Result;
            if (res == null) continue;

            string iterStr = res.Iterations.ToString("N0");
            string timeStr = res.FormattedTime;
            if (!res.Succeeded && !string.IsNullOrEmpty(res.Note))
            {
                timeStr += $" ({res.Note})";
            }
            if (res.IsFastest)
            {
                timeStr = "★ " + timeStr;
            }

            switch (res.AlgorithmType)
            {
                case SortAlgorithmType.Bubble:
                    rowIterations.Bubble = iterStr;
                    rowTime.Bubble = timeStr;
                    break;
                case SortAlgorithmType.Shaker:
                    rowIterations.Shaker = iterStr;
                    rowTime.Shaker = timeStr;
                    break;
                case SortAlgorithmType.Insertion:
                    rowIterations.Insertion = iterStr;
                    rowTime.Insertion = timeStr;
                    break;
                case SortAlgorithmType.Quick:
                    rowIterations.Quick = iterStr;
                    rowTime.Quick = timeStr;
                    break;
                case SortAlgorithmType.Bogo:
                    rowIterations.Bogo = iterStr;
                    rowTime.Bogo = timeStr;
                    break;
            }
        }

        GridResults.ItemsSource = new List<ResultTableRow> { rowIterations, rowTime };
    }

    private void AnimTimer_Tick(object? sender, EventArgs e)
    {
        _currentAnimationStep++;

        bool allDone = true;
        foreach (var card in _activeCards)
        {
            bool cardDone = card.RenderStep(_currentAnimationStep);
            if (!cardDone)
            {
                allDone = false;
            }
        }

        if (allDone)
        {
            _animTimer.Stop();
            BtnPause.IsEnabled = false;
            StatusMessage.Text = "Одновременная визуализация всех выбранных алгоритмов завершена.";
        }
    }

    private void BtnPauseResume_Click(object sender, RoutedEventArgs e)
    {
        if (_activeCards.Count == 0) return;

        if (_isPaused)
        {
            _animTimer.Start();
            _isPaused = false;
            BtnPause.Content = "⏸ Пауза";
            StatusMessage.Text = "Анимация продолжена.";
        }
        else
        {
            _animTimer.Stop();
            _isPaused = true;
            BtnPause.Content = "▶ Продолжить";
            StatusMessage.Text = "Анимация приостановлена.";
        }
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        _animTimer.Stop();
        _isPaused = false;
        BtnPause.IsEnabled = false;
        BtnPause.Content = "⏸ Пауза";

        _currentAnimationStep = 0;
        foreach (var card in _activeCards)
        {
            card.RenderStep(0);
        }

        StatusMessage.Text = "Визуализация сброшена к начальному состоянию массива.";
    }

    private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateTimerInterval();
    }

    private void UpdateTimerInterval()
    {
        if (SliderSpeed == null || TxtSpeedVal == null) return;

        // Значение от 1 до 100: 1 - медленно (250 мс), 100 - мгновенно (5 мс)
        double val = SliderSpeed.Value;
        double speedFactor = val / 50.0;
        TxtSpeedVal.Text = $"x{speedFactor:F1}";

        int ms = (int)Math.Max(5, 250 - (val * 2.4));
        _animTimer.Interval = TimeSpan.FromMilliseconds(ms);
    }

    private void ChkSelectAll_Checked(object sender, RoutedEventArgs e)
    {
        ChkBubble.IsChecked = true;
        ChkShaker.IsChecked = true;
        ChkInsertion.IsChecked = true;
        ChkQuick.IsChecked = true;
        ChkBogo.IsChecked = true;
    }

    private void ChkSelectAll_Unchecked(object sender, RoutedEventArgs e)
    {
        ChkBubble.IsChecked = false;
        ChkShaker.IsChecked = false;
        ChkInsertion.IsChecked = false;
        ChkQuick.IsChecked = false;
        ChkBogo.IsChecked = false;
    }

    private void MenuLoadExcel_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Выберите файл Excel или CSV с массивом",
            Filter = "Файлы данных (*.xlsx;*.csv;*.txt)|*.xlsx;*.csv;*.txt|Книги Excel (*.xlsx)|*.xlsx|Текстовые файлы (*.csv;*.txt)|*.csv;*.txt|Все файлы (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                int[] numbers = _importService.ImportFromFile(ofd.FileName);
                LoadArrayIntoGrid(numbers);
                MessageBox.Show($"Успешно импортировано {numbers.Length} элементов из файла: {ofd.SafeFileName}", "Импорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MenuLoadGoogleSheets_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new GoogleSheetsImportDialog { Owner = this };
        if (dlg.ShowDialog() == true && dlg.ResultNumbers != null)
        {
            LoadArrayIntoGrid(dlg.ResultNumbers);
            MessageBox.Show($"Успешно импортировано {dlg.ResultNumbers.Length} элементов из Google Таблиц.", "Импорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void MenuPasteClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Буфер обмена пуст.", "Вставка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var numbers = _importService.ImportFromDelimitedText(text);
            if (numbers.Length == 0)
            {
                MessageBox.Show("В буфере обмена не найдено чисел.", "Вставка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadArrayIntoGrid(numbers);
            MessageBox.Show($"Вставлено {numbers.Length} элементов из буфера обмена.", "Вставка завершена", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка вставки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuHelpTask_Click(object sender, RoutedEventArgs e)
    {
        string taskInfo =
            "ЗАДАНИЕ К ЛАБОРАТОРНОЙ РАБОТЕ №4:\n\n" +
            "1. Реализовать следующие алгоритмы сортировок:\n" +
            "   - Пузырьковая сортировка (n - 1)\n" +
            "   - Шейкерная сортировка ((n - 1) / 2)\n" +
            "   - Сортировка вставками (~ n² / 4)\n" +
            "   - Быстрая сортировка (Quick Sort)\n" +
            "   - BOGO сортировка (случайная перестановка с ограничением)\n\n" +
            "2. Требования к приложению:\n" +
            "   - Ввод данных через DataGrid (до 50 элементов)\n" +
            "   - Импорт данных из Excel (.xlsx/.csv) и Google Таблиц\n" +
            "   - Генератор массива в диапазоне [A; B] размером N\n" +
            "   - Тумблер сортировки по возрастанию и убыванию\n" +
            "   - Чекбоксы выбора нескольких или всех алгоритмов\n" +
            "   - Одновременная визуализация работы алгоритмов\n" +
            "   - Меню взаимодействия MenuStrip (Menu)\n" +
            "   - Вывод количества итераций (к/и) и точного времени (t)\n" +
            "   - Определение самого быстрого метода сортировки";

        MessageBox.Show(taskInfo, "Задание — Лабораторная работа №4", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Лабораторная работа №4: Алгоритмы сортировки данных.\nРазработано на C# WPF (.NET 10).\nОбеспечена отказоустойчивость и одновременная визуализация.", "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MenuClose_Click(object sender, RoutedEventArgs e)
    {
        _animTimer.Stop();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _animTimer.Stop();
        base.OnClosed(e);
    }
}
