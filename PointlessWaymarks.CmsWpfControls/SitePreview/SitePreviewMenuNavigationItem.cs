using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.SitePreview;

[NotifyPropertyChanged]
public partial class SitePreviewMenuNavigationItem
{
    public SitePreviewMenuNavigationItem(string displayText, string url)
    {
        DisplayText = displayText;
        Url = url;
    }

    public string DisplayText { get; set; }
    public string Url { get; set; }
}