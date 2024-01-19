namespace PointlessWaymarks.CmsWpfControls.ContentMap;

public class ContentMapMapSelection(Guid selectedContentId) : EventArgs
{
    public Guid SelectedContentId { get; set; } = selectedContentId;
}