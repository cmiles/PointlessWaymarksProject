using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PointList;

[NotifyPropertyChanged]
public partial class PointListListItem : IContentListItem, IContentListSmallImage
{
    private protected PointListListItem(PointContentActions itemActions, PointContent dbEntry)
    {
        DbEntry = dbEntry;
        ItemActions = itemActions;
    }

    public PointContent DbEntry { get; set; }
    public PointContentActions ItemActions { get; set; }
    public CurrentSelectedTextTracker? SelectedTextTracker { get; set; } = new();
    public bool ShowType { get; set; }

    public string? SmallImageUrl { get; set; }

    public IContentCommon Content()
    {
        return DbEntry;
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

    public async Task ViewHistory()
    {
        await ItemActions.ViewHistory(DbEntry);
    }

    public async Task ViewOnSite()
    {
        await ItemActions.ViewOnSite(DbEntry);
    }

    public static Task<PointListListItem> CreateInstance(PointContentActions itemActions)
    {
        return Task.FromResult(new PointListListItem(itemActions, PointContent.CreateInstance()));
    }
}