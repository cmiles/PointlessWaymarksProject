#nullable enable
using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport;

/// <summary>
///     Interaction logic for WordPressXmlImportWindow.xaml
/// </summary>
[ObservableObject]
public partial class WordPressXmlImportWindow
{
    [ObservableProperty] private WordPressXmlImportContext _importContext;
    [ObservableProperty] private StatusControlContext _statusContext;

    public WordPressXmlImportWindow()
    {
        InitializeComponent();

        _statusContext = new StatusControlContext();

        DataContext = this;

        _importContext = new WordPressXmlImportContext(StatusContext);
    }
}