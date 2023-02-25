using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.MarkdownDisplay;

public partial class HelpDisplayContext : ObservableObject
{
    [ObservableProperty] private string _helpMarkdownContent = string.Empty;

    public HelpDisplayContext(List<string> markdownHelp)
    {
        if (!markdownHelp.Any()) HelpMarkdownContent = string.Empty;
        else
            HelpMarkdownContent = string.Join(Environment.NewLine + Environment.NewLine,
                markdownHelp.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}