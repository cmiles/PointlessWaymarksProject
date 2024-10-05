using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.SnippetList;

/// <summary>
///     Interaction logic for SnippetListWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class SnippetListWindow
{
    private SnippetListWindow(SnippetListContext toLoad)
    {
        InitializeComponent();
        ListContext = toLoad;
        DataContext = this;
        WindowTitle = $"Snippet List - {UserSettingsSingleton.CurrentSettings().SiteName}";
    }

    public SnippetListContext ListContext { get; set; }
    public string WindowTitle { get; set; }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed.
    /// </summary>
    /// <returns></returns>
    public static async Task<SnippetListWindow> CreateInstance(SnippetListContext? toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var window = new SnippetListWindow(toLoad ?? await SnippetListContext.CreateInstance(null));

        return window;
    }
}