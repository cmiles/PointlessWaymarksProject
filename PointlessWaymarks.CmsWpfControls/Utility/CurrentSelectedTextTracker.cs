using System.Windows;
using System.Windows.Controls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Commands;

namespace PointlessWaymarks.CmsWpfControls.Utility;

[ObservableObject]
public partial class CurrentSelectedTextTracker
{
    [ObservableProperty] private string _currentSelectedText;
    [ObservableProperty] private Command<RoutedEventArgs> _selectedTextChangedCommand;

    public CurrentSelectedTextTracker()
    {
        SelectedTextChangedCommand = new Command<RoutedEventArgs>(SelectedTextChanged);
    }

    private void SelectedTextChanged(RoutedEventArgs obj)
    {
        var source = obj.Source as TextBox;
        CurrentSelectedText = source?.SelectedText;
    }
}