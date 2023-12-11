namespace PointlessWaymarks.CmsWpfControls.ContentMap;

public class ContentMapMapSelection : EventArgs
{
    public ContentMapMapSelection(Guid selectedContentId)
    {
        SelectedContentId = selectedContentId;
    }

    public Guid SelectedContentId { get; set; }
}