using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport;

/// <summary>
///     Interaction logic for WordPressXmlImportWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class WordPressXmlImportWindow
{
    private WordPressXmlImportWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
        ImportContext = new WordPressXmlImportContext(StatusContext);
        WindowTitle = $"WordPress Import - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public WordPressXmlImportContext ImportContext { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }

    public static async Task<WordPressXmlImportWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        return new WordPressXmlImportWindow();
    }
}