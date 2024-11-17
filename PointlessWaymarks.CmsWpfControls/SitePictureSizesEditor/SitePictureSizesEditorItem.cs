using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.SitePictureSizesEditor;

[NotifyPropertyChanged]
public partial class SitePictureSizesEditorItem
{
    public int MaxDimension { get; set; }
    public int Quality { get; set; }
}