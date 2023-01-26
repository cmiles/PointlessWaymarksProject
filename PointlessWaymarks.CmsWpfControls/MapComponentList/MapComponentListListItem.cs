using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList;

public partial class MapComponentListListItem : ObservableObject, IContentListItem
{
    [ObservableProperty] private MapComponent _dbEntry;
    [ObservableProperty] private MapComponentContentActions _itemActions;
    [ObservableProperty] private CurrentSelectedTextTracker _selectedTextTracker = new();
    [ObservableProperty] private bool _showType;

    public IContentCommon Content()
    {
        if (DbEntry == null) return null;

        return new ContentCommonShell
        {
            Summary = DbEntry.Summary,
            Title = DbEntry.Title,
            ContentId = DbEntry.ContentId,
            ContentVersion = DbEntry.ContentVersion,
            Id = DbEntry.Id,
            CreatedBy = DbEntry.CreatedBy,
            CreatedOn = DbEntry.CreatedOn,
            LastUpdatedBy = DbEntry.LastUpdatedBy,
            LastUpdatedOn = DbEntry.LastUpdatedOn
        };
    }

    public Guid? ContentId()
    {
        return DbEntry?.ContentId;
    }

    public string DefaultBracketCode()
    {
        return ItemActions.DefaultBracketCode(DbEntry);
    }

    public async Task DefaultBracketCodeToClipboard()
    {
        await ItemActions.DefaultBracketCodeToClipboard(DbEntry);
    }

    public async Task Delete()
    {
        await ItemActions.Delete(DbEntry);
    }

    public async Task Edit()
    {
        await ItemActions.Edit(DbEntry);
    }

    public async Task ExtractNewLinks()
    {
        await ItemActions.ExtractNewLinks(DbEntry);
    }

    public async Task GenerateHtml()
    {
        await ItemActions.GenerateHtml(DbEntry);
    }

    public async Task ViewOnSite()
    {
        await ItemActions.ViewOnSite(DbEntry);
    }

    public async Task ViewHistory()
    {
        await ItemActions.ViewHistory(DbEntry);
    }
}