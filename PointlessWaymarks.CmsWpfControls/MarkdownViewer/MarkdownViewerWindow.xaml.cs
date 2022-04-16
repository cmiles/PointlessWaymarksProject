using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.MarkdownViewer;

/// <summary>
///     Interaction logic for MarkdownViewerWindow.xaml
/// </summary>
[ObservableObject]
public partial class MarkdownViewerWindow
{
    [ObservableProperty] private string _markdownContent;
    [ObservableProperty] private string _windowTitle;

    public MarkdownViewerWindow(string windowTitle, string markdown)
    {
        InitializeComponent();
        MarkdownContent = markdown;
        WindowTitle = windowTitle;

        DataContext = this;
    }
}