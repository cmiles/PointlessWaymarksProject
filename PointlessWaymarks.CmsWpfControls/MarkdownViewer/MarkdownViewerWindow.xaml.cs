using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MarkdownViewer;

/// <summary>
///     Interaction logic for MarkdownViewerWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class MarkdownViewerWindow
{
    private MarkdownViewerWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public string MarkdownContent { get; set; } = string.Empty;
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = string.Empty;

    public static async Task<MarkdownViewerWindow> CreateInstance(string windowTitle, string markdown)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        return new MarkdownViewerWindow
        {
            MarkdownContent = markdown,
            WindowTitle = windowTitle
        };
    }
}