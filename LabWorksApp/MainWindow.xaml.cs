using System.Windows;
using LabWorksApp.Views;

namespace LabWorksApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnOpenLab1_Click(object sender, RoutedEventArgs e)
    {
        OpenLab1();
    }

    private void BtnOpenLab4_Click(object sender, RoutedEventArgs e)
    {
        OpenLab4();
    }

    private void BtnOpenSelectedLab_Click(object sender, RoutedEventArgs e)
    {
        int selected = ComboLabSelect.SelectedIndex;
        if (selected == 0) // Лабораторная работа №1
        {
            OpenLab1();
        }
        else if (selected == 3) // Лабораторная работа №4
        {
            OpenLab4();
        }
        else
        {
            int labNum = selected + 1;
            MessageBox.Show(
                $"Лабораторная работа №{labNum} находится в разработке.\nВ данный момент доступны:\n- Лабораторная работа №1: «Метод дихотомии (половинного деления)»\n- Лабораторная работа №4: «Алгоритмы сортировки данных».",
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
                $"Лабораторная работа №{tagStr} находится в разработке.\nДоступны Лабораторные работы №1 и №4.",
                $"Лабораторная работа №{tagStr}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void OpenLab1()
    {
        var lab1 = new Lab1Window();
        lab1.Show();
    }

    private void OpenLab4()
    {
        var lab4 = new Lab4Window();
        lab4.Show();
    }
}