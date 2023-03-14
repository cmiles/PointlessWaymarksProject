using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace PointlessWaymarks.CmsWpfControls.Utility;

public partial class CurrentSelectedTextTracker : ObservableObject
{
    [ObservableProperty] private string? _currentSelectedText;
    [ObservableProperty] private RelayCommand<RoutedEventArgs> _selectedTextChangedCommand;

    public CurrentSelectedTextTracker()
    {
        _selectedTextChangedCommand = new RelayCommand<RoutedEventArgs>(SelectedTextChanged);
    }

    private void SelectedTextChanged(RoutedEventArgs? obj)
    {
        if (obj == null) return;

        var source = obj.Source as TextBox;
        CurrentSelectedText = source?.SelectedText;
    }
}