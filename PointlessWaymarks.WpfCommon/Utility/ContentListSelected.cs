using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.Utility;

public class ContentListSelected<T> : INotifyPropertyChanged where T : ISelectedTextTracker
{
    private ObservableCollection<CommandBinding>? _listBoxAppCommandBindings;
    private T? _selected;
    private List<T>? _selectedItems = [];

    private ContentListSelected(StatusControlContext? statusContext = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
    }

    public ObservableCollection<CommandBinding>? ListBoxAppCommandBindings
    {
        get => _listBoxAppCommandBindings;
        set
        {
            if (Equals(value, _listBoxAppCommandBindings)) return;
            _listBoxAppCommandBindings = value;
            OnPropertyChanged();
        }
    }

    public T? Selected
    {
        get => _selected;
        set
        {
            if (Equals(value, _selected)) return;
            _selected = value;
            OnPropertyChanged();
        }
    }

    public List<T> SelectedItems
    {
        get => _selectedItems ?? [];
        set
        {
            if (Equals(value, _selectedItems)) return;
            _selectedItems = value;
            OnPropertyChanged();
        }
    }

    public StatusControlContext StatusContext { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static async Task<ContentListSelected<T>> CreateInstance(StatusControlContext? statusContext = null)
    {
        var newControl = new ContentListSelected<T>(statusContext);
        await newControl.LoadData();
        return newControl;
    }

    private void ExecuteListBoxItemCopy(object? sender, ExecutedRoutedEventArgs e)
    {
        if (Selected == null) return;
        StatusContext.ContextDispatcher.Invoke(() =>
        {
            Clipboard.SetText(Selected.SelectedTextTracker?.CurrentSelectedText ?? string.Empty);
        });
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        ListBoxAppCommandBindings = new ObservableCollection<CommandBinding>(
            [new(ApplicationCommands.Copy, ExecuteListBoxItemCopy)]);
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}