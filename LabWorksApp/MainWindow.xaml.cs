using System.Windows;
using LabWorksApp.Views;

namespace LabWorksApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnOpenLab4_Click(object sender, RoutedEventArgs e)
    {
        OpenLab4();
    }

    private void BtnOpenSelectedLab_Click(object sender, RoutedEventArgs e)
    {
        int selected = ComboLabSelect.SelectedIndex;
        if (selected == 3) // Лабораторная работа №4
        {
            OpenLab4();
        }
        else
        {
            int labNum = selected + 1;
            MessageBox.Show(
                $"Лабораторная работа №{labNum} находится в разработке.\nВ данный момент полностью готова и доступна Лабораторная работа №4: «Алгоритмы сортировки данных».",
                $"Лабораторная работа №{labNum}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void BtnPlaceholder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is string tagStr)
        {
            MessageBox.Show(
                $"Лабораторная работа №{tagStr} находится в разработке.\nОткройте Лабораторную работу №4.",
                $"Лабораторная работа №{tagStr}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void OpenLab4()
    {
        var lab4 = new Lab4Window();
        lab4.Show();
    }
}