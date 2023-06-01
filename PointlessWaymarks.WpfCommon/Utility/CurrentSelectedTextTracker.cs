using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.Utility;

[NotifyPropertyChanged]
public partial class CurrentSelectedTextTracker
{
    public CurrentSelectedTextTracker()
    {
        SelectedTextChangedCommand = new RelayCommand<RoutedEventArgs>(SelectedTextChanged);
    }

    public string? CurrentSelectedText { get; set; }
    public RelayCommand<RoutedEventArgs> SelectedTextChangedCommand { get; set; }

    private void SelectedTextChanged(RoutedEventArgs? obj)
    {
        if (obj == null) return;

        var source = obj.Source as TextBox;
        CurrentSelectedText = source?.SelectedText;
    }
}