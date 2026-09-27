using System.Windows;
using LabWorksApp.Services;

namespace LabWorksApp.Views;

public partial class GoogleSheetsImportDialog : Window
{
    private readonly DataImportService _importService = new();

    public int[]? ResultNumbers { get; private set; }

    public GoogleSheetsImportDialog()
    {
        InitializeComponent();
    }

    private async void BtnDownloadUrl_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;
        string url = TxtSheetUrl.Text.Trim();

        if (string.IsNullOrWhiteSpace(url) || url.Contains("..."))
        {
            ShowError("Пожалуйста, введите корректный URL Google Таблицы.");
            return;
        }

        try
        {
            IsEnabled = false;
            Cursor = System.Windows.Input.Cursors.Wait;

            var numbers = await _importService.ImportFromGoogleSheetsAsync(url);
            if (numbers.Length == 0)
            {
                ShowError("Не удалось обнаружить числа в таблице.");
                return;
            }

            ResultNumbers = numbers;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsEnabled = true;
            Cursor = System.Windows.Input.Cursors.Arrow;
        }
    }

    private void BtnApplyDirectText_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;
        string text = TxtDirectData.Text.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            ShowError("Вставьте числовые данные или разделительный текст из таблицы.");
            return;
        }

        try
        {
            var numbers = _importService.ImportFromDelimitedText(text);
            if (numbers.Length == 0)
            {
                ShowError("В тексте не найдено корректных целых чисел.");
                return;
            }

            ResultNumbers = numbers;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка обработки текста: {ex.Message}");
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string msg)
    {
        TxtError.Text = msg;
        TxtError.Visibility = Visibility.Visible;
    }
}
