using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.ContentIdViewer;

[NotifyPropertyChanged]
public partial class ContentIdViewerControlContext
{
    private ContentIdViewerControlContext(StatusControlContext statusContext, IContentId dbEntry)
    {
        StatusContext = statusContext;
        DbEntry = dbEntry;

        ContentIdInformation = $" Fingerprint: {dbEntry.ContentId} Db Id: {dbEntry.Id}";
    }

    public string ContentIdInformation { get; set; }
    public IContentId DbEntry { get; set; }
    public StatusControlContext StatusContext { get; set; }

    public static async Task<ContentIdViewerControlContext> CreateInstance(StatusControlContext statusContext,
        IContentId dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new ContentIdViewerControlContext(statusContext, dbEntry);

        return newContext;
    }
}