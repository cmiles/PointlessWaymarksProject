using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MarkdownViewer;

/// <summary>
///     Interaction logic for MarkdownViewerWindow.xaml
/// </summary>
[ObservableObject]
public partial class MarkdownViewerWindow
{
    [ObservableProperty] private string _markdownContent;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _windowTitle;

    private MarkdownViewerWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
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