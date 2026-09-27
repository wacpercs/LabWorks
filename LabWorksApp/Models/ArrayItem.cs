using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LabWorksApp.Models;

public class ArrayItem : INotifyPropertyChanged
{
    private int _index;
    private int _value;

    public int Index
    {
        get => _index;
        set { _index = value; OnPropertyChanged(); }
    }

    public int Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
