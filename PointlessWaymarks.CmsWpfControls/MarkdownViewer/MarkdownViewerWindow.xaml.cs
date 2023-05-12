using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MarkdownViewer;

/// <summary>
///     Interaction logic for MarkdownViewerWindow.xaml
/// </summary>
[ObservableObject]
#pragma warning disable MVVMTK0033
public partial class MarkdownViewerWindow
#pragma warning restore MVVMTK0033
{
    [ObservableProperty] private string _markdownContent = string.Empty;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _windowTitle = string.Empty;

    private MarkdownViewerWindow()
    {
        InitializeComponent();
        _statusContext = new StatusControlContext();
        DataContext = this;
    }

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