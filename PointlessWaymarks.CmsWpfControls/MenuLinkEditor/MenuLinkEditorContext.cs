using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.MenuLinkEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class MenuLinkEditorContext
{
    private MenuLinkEditorContext(StatusControlContext statusContext, ObservableCollection<MenuLinkListItem> items,
        bool loadInBackground = true)
    {
        StatusContext = statusContext;
        CommonCommands = new CmsCommonCommands(StatusContext);

        BuildCommands();

        HelpMarkdown = MenuLinksHelpMarkdown.HelpBlock;

        Items = items;

        if (loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public CmsCommonCommands CommonCommands { get; set; }
    public List<MenuLinkEditorContentTypeSearchListChoice> ContentTypeRssChoices { get; set; } = [];
    public List<MenuLinkEditorContentTypeSearchListChoice> ContentTypeSearchListChoices { get; set; } = [];
    public string HelpMarkdown { get; set; }
    public ObservableCollection<MenuLinkListItem> Items { get; set; }
    public List<MenuLinkListItem>? SelectedItems { get; set; }
    public StatusControlContext StatusContext { get; set; }

    [BlockingCommand]
    private async Task AddItem()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newItem = await MenuLinkListItem.CreateInstance(new MenuLink());
        newItem.UserOrder = Items.Count;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(newItem);
    }

    public static async Task<MenuLinkEditorContext> CreateInstance(StatusControlContext? statusContext,
        bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        var factoryItems = new ObservableCollection<MenuLinkListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new MenuLinkEditorContext(factoryStatusContext, factoryItems, loadInBackground);

        return toReturn;
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    private async Task DeleteItems()
    {
        var selected = SelectedListItems();

        await ThreadSwitcher.ResumeForegroundAsync();

        selected.ForEach(x => Items.Remove(x));

        await RenumberItems();
    }

    [BlockingCommand]
    public async Task InsertIndexTag(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, BracketCodeSpecialPages.IndexBracketCode);
    }

    private async Task InsertIntoLinkTag(MenuLinkListItem? listItem, string toInsert)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            await StatusContext.ToastError("No item?");
            return;
        }

        listItem.UserLink = listItem.UserLink.Trim();

        listItem.UserLink += toInsert;
    }

    [BlockingCommand]
    public async Task InsertLatestContentGallery(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, BracketCodeSpecialPages.LatestContentPageBracketCode);
    }

    [BlockingCommand]
    public async Task InsertMonthlyActivity(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, BracketCodeSpecialPages.MonthlyActivityBracketCode);
    }

    [BlockingCommand]
    public async Task InsertPhotoGallery(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, BracketCodeSpecialPages.PhotoGalleryPageBracketCode);
    }

    [BlockingCommand]
    public async Task InsertSearchPage(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, BracketCodeSpecialPages.SearchPageBracketCode);
    }

    [BlockingCommand]
    public async Task InsertSelectedRssPageLink(MenuLinkListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            await StatusContext.ToastError("No item?");
            return;
        }

        if (listItem.SelectedRssPage is null)
        {
            await StatusContext.ToastError("No RSS Selected?");
            return;
        }

        await InsertIntoLinkTag(listItem, listItem.SelectedRssPage.BracketCode);
    }

    [BlockingCommand]
    public async Task InsertSelectedSearchPageLink(MenuLinkListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            await StatusContext.ToastError("No item?");
            return;
        }

        if (listItem.SelectedSearchPage is null)
        {
            await StatusContext.ToastError("No Search Page Selected?");
            return;
        }

        await InsertIntoLinkTag(listItem, listItem.SelectedSearchPage.BracketCode);
    }

    [BlockingCommand]
    public async Task InsertTagSearch(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, BracketCodeSpecialPages.TagsPageBracketCode);
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = await Db.Context();
        var existingEntries = await context.MenuLinks.ToListAsync();
        List<MenuLinkListItem> listItems = [];

        foreach (var loopExisting in existingEntries)
        {
            var toAdd = await MenuLinkListItem.CreateInstance(loopExisting);
            toAdd.UserLink = toAdd.DbEntry.LinkTag?.Trim() ?? string.Empty;
            listItems.Add(toAdd);
        }

        listItems = listItems.OrderBy(x => x.UserOrder).ThenBy(x => x.UserLink).ToList();

        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "File", BracketCode = BracketCodeSpecialPages.FilesSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "GeoJson", BracketCode = BracketCodeSpecialPages.GeoJsonSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Image", BracketCode = BracketCodeSpecialPages.ImagesSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Line", BracketCode = BracketCodeSpecialPages.LinesSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Link", BracketCode = BracketCodeSpecialPages.LinksSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Note", BracketCode = BracketCodeSpecialPages.NotesSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Photo", BracketCode = BracketCodeSpecialPages.PhotosSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Point", BracketCode = BracketCodeSpecialPages.PointsSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Post", BracketCode = BracketCodeSpecialPages.PostSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Trail", BracketCode = BracketCodeSpecialPages.TrailsSearchPageBracketCode });
        ContentTypeSearchListChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Video", BracketCode = BracketCodeSpecialPages.VideoSearchPageBracketCode });

        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "File", BracketCode = BracketCodeSpecialPages.FilesRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "GeoJson", BracketCode = BracketCodeSpecialPages.GeoJsonRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Image", BracketCode = BracketCodeSpecialPages.ImageRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Index Page", BracketCode = BracketCodeSpecialPages.ImageRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Line", BracketCode = BracketCodeSpecialPages.LinesRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Link", BracketCode = BracketCodeSpecialPages.LinkRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Note", BracketCode = BracketCodeSpecialPages.NoteRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Photo", BracketCode = BracketCodeSpecialPages.PhotoRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Point", BracketCode = BracketCodeSpecialPages.PointsRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Post", BracketCode = BracketCodeSpecialPages.PostRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Trail", BracketCode = BracketCodeSpecialPages.TrailsRssBracketCode });
        ContentTypeRssChoices.Add(new MenuLinkEditorContentTypeSearchListChoice
            { DisplayValue = "Video", BracketCode = BracketCodeSpecialPages.VideoRssBracketCode });


        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        listItems.ForEach(x => Items.Add(x));

        await RenumberItems();
    }

    [NonBlockingCommand]
    private async Task MoveItemDown(MenuLinkListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            await StatusContext.ToastError("No item?");
            return;
        }

        await RenumberItems();

        var currentItemIndex = Items.IndexOf(listItem);

        if (currentItemIndex == Items.Count - 1) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Move(currentItemIndex, currentItemIndex + 1);

        await RenumberItems();
    }

    [NonBlockingCommand]
    private async Task MoveItemUp(MenuLinkListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            await StatusContext.ToastError("No item?");
            return;
        }

        await RenumberItems();

        var currentItemIndex = Items.IndexOf(listItem);

        if (currentItemIndex == 0) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Move(currentItemIndex, currentItemIndex - 1);

        await RenumberItems();
    }

    private async Task RenumberItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        for (var i = 0; i < Items.Count; i++) Items[i].UserOrder = i;
    }

    [BlockingCommand]
    [StopAndWarnIfNoItems]
    private async Task Save()
    {
        foreach (var loopItems in Items) loopItems.UserLink = loopItems.UserLink.Trim();

        await RenumberItems();

        var context = await Db.Context();

        var currentDbList = context.MenuLinks.ToList();

        var deletedItems = new List<MenuLink>();

        foreach (var loopDbItems in currentDbList)
        {
            var listItem = Items.SingleOrDefault(x => x.DbEntry.Id == loopDbItems.Id);
            if (listItem == null) deletedItems.Add(loopDbItems);
        }

        var withEdits = Items.Where(x => x.HasChanges).ToList();

        if (!withEdits.Any() && !deletedItems.Any())
        {
            await StatusContext.ToastError("No entries have changed?");
            return;
        }

        if (withEdits.Any(x => string.IsNullOrWhiteSpace(x.UserLink)))
        {
            await StatusContext.ToastError("All Entries must have a value.");
            return;
        }

        var frozenNowVersion = DateTime.Now.ToUniversalTime().TrimDateTimeToSeconds();

        foreach (var loopDeleted in deletedItems) context.Remove(loopDeleted);

        foreach (var loopChanges in withEdits)
            if (loopChanges.DbEntry.Id < 1)
            {
                loopChanges.DbEntry = new MenuLink
                {
                    LinkTag = loopChanges.UserLink,
                    MenuOrder = loopChanges.UserOrder,
                    ContentVersion = frozenNowVersion
                };

                await context.MenuLinks.AddAsync(loopChanges.DbEntry);
            }
            else
            {
                var toUpdate = await context.MenuLinks.SingleAsync(x => x.Id == loopChanges.DbEntry.Id);

                toUpdate.LinkTag = loopChanges.UserLink;
                toUpdate.MenuOrder = loopChanges.UserOrder;
                toUpdate.ContentVersion = frozenNowVersion;

                loopChanges.DbEntry = toUpdate;
            }

        await context.SaveChangesAsync(true);

        await StatusContext.ToastSuccess("Saved Changes");
    }

    public List<MenuLinkListItem> SelectedListItems()
    {
        return SelectedItems ?? [];
    }
}

[NotifyPropertyChanged]
public partial class MenuLinkEditorContentTypeSearchListChoice
{
    public required string BracketCode { get; set; }
    public required string DisplayValue { get; set; }
}