#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport;

[NotifyPropertyChanged]
public partial class WordPressXmlImportListItem : ISelectedTextTracker
{
    public string Category { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    public CurrentSelectedTextTracker? SelectedTextTracker { get; set; } = new();
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string WordPressType { get; set; } = string.Empty;
}