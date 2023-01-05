﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.WpfCommon.MarkdownDisplay;

[ObservableObject]
public partial class HelpDisplayContext
{
    [ObservableProperty] private string _helpMarkdownContent;

    public HelpDisplayContext(List<string> markdownHelp)
    {
        if (markdownHelp == null || !markdownHelp.Any()) HelpMarkdownContent = string.Empty;
        else
            HelpMarkdownContent = string.Join(Environment.NewLine + Environment.NewLine,
                markdownHelp.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}