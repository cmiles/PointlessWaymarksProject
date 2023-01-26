using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ContentIdViewer;

public partial class ContentIdViewerControlContext : ObservableObject
{
    [ObservableProperty] private string _contentIdInformation;
    [ObservableProperty] private IContentId _dbEntry;
    [ObservableProperty] private StatusControlContext _statusContext;

    private ContentIdViewerControlContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();
    }

    public static async Task<ContentIdViewerControlContext> CreateInstance(StatusControlContext statusContext,
        IContentId dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new ContentIdViewerControlContext(statusContext);
        await newContext.LoadData(dbEntry);
        return newContext;
    }

    public async Task LoadData(IContentId dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DbEntry = dbEntry;

        if (dbEntry == null)
        {
            ContentIdInformation = "Id: (Db Entry Is Null) Fingerprint: (Db Entry is Null)";
            return;
        }

        ContentIdInformation = $" Fingerprint: {dbEntry.ContentId} Db Id: {dbEntry.Id}";
    }
}