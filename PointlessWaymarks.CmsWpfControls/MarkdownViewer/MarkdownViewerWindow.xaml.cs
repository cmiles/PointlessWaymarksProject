using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.MarkdownViewer;

/// <summary>
///     Interaction logic for MarkdownViewerWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class MarkdownViewerWindow
{
    private MarkdownViewerWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string MarkdownContent { get; set; } = string.Empty;
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = string.Empty;

    public static async Task<MarkdownViewerWindow> CreateInstance(string windowTitle, string markdown)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        return new MarkdownViewerWindow
        {
            MarkdownContent = markdown,
            WindowTitle = windowTitle,
            StatusContext = await StatusControlContext.CreateInstance()
        };
    }
}