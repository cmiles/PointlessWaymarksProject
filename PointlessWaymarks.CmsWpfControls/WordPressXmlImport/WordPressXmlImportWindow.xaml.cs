#nullable enable
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport;

/// <summary>
///     Interaction logic for WordPressXmlImportWindow.xaml
/// </summary>
public partial class WordPressXmlImportWindow : INotifyPropertyChanged
{
    private WordPressXmlImportContext _importContext;
    private StatusControlContext _statusContext;

    public WordPressXmlImportWindow()
    {
        InitializeComponent();

        _statusContext = new StatusControlContext();

        DataContext = this;

        _importContext = new WordPressXmlImportContext(StatusContext);
    }

    public WordPressXmlImportContext ImportContext
    {
        get => _importContext;
        set
        {
            if (Equals(value, _importContext)) return;
            _importContext = value;
            OnPropertyChanged();
        }
    }

    public StatusControlContext StatusContext
    {
        get => _statusContext;
        set
        {
            if (Equals(value, _statusContext)) return;
            _statusContext = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}