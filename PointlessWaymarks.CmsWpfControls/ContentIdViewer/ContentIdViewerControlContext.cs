using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ContentIdViewer;

public partial class ContentIdViewerControlContext : ObservableObject
{
    [ObservableProperty] private string _contentIdInformation = string.Empty;
    [ObservableProperty] private IContentId _dbEntry;
    [ObservableProperty] private StatusControlContext _statusContext;

    private ContentIdViewerControlContext(StatusControlContext statusContext, IContentId dbEntry)
    {
        _statusContext = statusContext;
        _dbEntry = dbEntry;

        ContentIdInformation = $" Fingerprint: {dbEntry.ContentId} Db Id: {dbEntry.Id}";
    }

    public static async Task<ContentIdViewerControlContext> CreateInstance(StatusControlContext statusContext,
        IContentId dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newContext = new ContentIdViewerControlContext(statusContext, dbEntry);
        
        return newContext;
    }
}