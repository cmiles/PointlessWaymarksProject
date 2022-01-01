using System.Windows;
using System.Windows.Controls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;


namespace PointlessWaymarks.CmsWpfControls.Utility;

[ObservableObject]
public partial class CurrentSelectedTextTracker
{
    [ObservableProperty] private string _currentSelectedText;
    [ObservableProperty] private RelayCommand<RoutedEventArgs> _selectedTextChangedCommand;

    public CurrentSelectedTextTracker()
    {
        SelectedTextChangedCommand = new RelayCommand<RoutedEventArgs>(SelectedTextChanged);
    }

    private void SelectedTextChanged(RoutedEventArgs obj)
    {
        var source = obj.Source as TextBox;
        CurrentSelectedText = source?.SelectedText;
    }
}