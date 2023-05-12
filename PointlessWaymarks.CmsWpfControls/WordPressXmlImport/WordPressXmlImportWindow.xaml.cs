#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport;

/// <summary>
///     Interaction logic for WordPressXmlImportWindow.xaml
/// </summary>
[ObservableObject]
#pragma warning disable MVVMTK0033
public partial class WordPressXmlImportWindow
#pragma warning restore MVVMTK0033
{
    [ObservableProperty] private WordPressXmlImportContext _importContext;
    [ObservableProperty] private StatusControlContext _statusContext;

    private WordPressXmlImportWindow()
    {
        InitializeComponent();

        _statusContext = new StatusControlContext();

        DataContext = this;

        _importContext = new WordPressXmlImportContext(StatusContext);
    }

    public static async Task<WordPressXmlImportWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        return new WordPressXmlImportWindow();
    }
}