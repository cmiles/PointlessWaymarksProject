using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LinkList;

public partial class LinkListListItem : ObservableObject, IContentListItem
{
    [ObservableProperty] private LinkContent _dbEntry;
    [ObservableProperty] private LinkContentActions _itemActions;
    [ObservableProperty] private string _linkContentString = string.Empty;
    [ObservableProperty] private CurrentSelectedTextTracker _selectedTextTracker = new();
    [ObservableProperty] private bool _showType;

    public LinkListListItem(LinkContentActions itemActions)
    {
        _dbEntry = LinkContent.CreateInstance();
        _itemActions = itemActions;
    }

    public IContentCommon Content()
    {
        return new ContentCommonShell
        {
            Summary =
                string.Join(Environment.NewLine,
                    (new List<string?>
                    {
                        DbEntry.Title,
                        DbEntry.Site,
                        DbEntry.Url,
                        DbEntry.Author,
                        DbEntry.Description,
                        DbEntry.Comments
                    }).Where(x => !string.IsNullOrWhiteSpace(x))),
            Title = DbEntry.Title,
            ContentId = DbEntry.ContentId,
            ContentVersion = DbEntry.ContentVersion,
            Id = DbEntry.Id,
            CreatedBy = DbEntry.CreatedBy,
            CreatedOn = DbEntry.CreatedOn,
            LastUpdatedBy = DbEntry.LastUpdatedBy,
            LastUpdatedOn = DbEntry.LastUpdatedOn,
            Tags = DbEntry.Tags
        };
    }

    public Guid? ContentId()
    {
        return DbEntry.ContentId;
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