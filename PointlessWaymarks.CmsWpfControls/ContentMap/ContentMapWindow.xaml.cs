using System.Windows;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.ContentMap;

[NotifyPropertyChanged]
public  partial class ContentMapWindow : Window
{
    private ContentMapWindow(ContentMapContext toLoad)
    {
        InitializeComponent();

        ListContext = toLoad;

        DataContext = this;
    }

    public ContentMapContext ListContext { get; set; }
    public string WindowTitle { get; set; } = "Photo List";

    public static async Task<ContentMapWindow> CreateInstance(ContentMapListLoader toLoad)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new ContentMapWindow(await ContentMapContext.CreateInstance(null, toLoad, true));

        return window;
    }
}