﻿namespace PointlessWaymarks.CmsWpfControls.HtmlViewer;

public partial class HtmlViewerWindow
{
    public HtmlViewerWindow(string htmlString)
    {
        InitializeComponent();

        DataContext = new HtmlViewerContext {HtmlString = htmlString};
    }
}