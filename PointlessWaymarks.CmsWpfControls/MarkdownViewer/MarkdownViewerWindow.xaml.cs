﻿using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.MarkdownViewer;

/// <summary>
///     Interaction logic for MarkdownViewerWindow.xaml
/// </summary>
[ObservableObject]
public partial class MarkdownViewerWindow
{
    [ObservableProperty] private string _markdownContent;
    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private StatusControlContext _statusContext;

    public MarkdownViewerWindow(string windowTitle, string markdown)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext(); 
        MarkdownContent = markdown;
        WindowTitle = windowTitle;

        DataContext = this;
    }
}