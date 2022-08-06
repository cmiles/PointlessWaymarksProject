using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


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