using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
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


    public static Task<MenuLinkEditorContext> CreateInstance(StatusControlContext? statusContext,
        bool loadInBackground = true)
    {
        ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();

        ThreadSwitcher.ResumeForegroundAsync();

        var factoryItems = new ObservableCollection<MenuLinkListItem>();

        ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new MenuLinkEditorContext(factoryContext, factoryItems, loadInBackground);

        return Task.FromResult(toReturn);
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
    public async Task InsertIndexTagIndex(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, "{{index; text Main;}}");
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
        await InsertIntoLinkTag(listItem, "{{latestcontentpage; text Latest;}}");
    }

    [BlockingCommand]
    public async Task InsertLinkList(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, "{{linklistpage; text Links;}}");
    }

    [BlockingCommand]
    public async Task InsertPhotoGallery(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, "{{photogallerypage; text Photos;}}");
    }

    [BlockingCommand]
    public async Task InsertSearchPage(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, "{{searchpage; text Search;}}");
    }

    [BlockingCommand]
    public async Task InsertTagSearch(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, "{{tagspage; text Tags;}}");
    }
    
    [BlockingCommand]
    public async Task InsertMonthlyActivity(MenuLinkListItem? listItem)
    {
        await InsertIntoLinkTag(listItem, "{{monthlyactivity; text Activity Summary;}}");
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

        var withChanges = Items.Where(x => x.HasChanges).ToList();

        if (!withChanges.Any())
        {
            await StatusContext.ToastError("No entries have changed?");
            return;
        }

        if (withChanges.Any(x => string.IsNullOrWhiteSpace(x.UserLink)))
        {
            await StatusContext.ToastError("All Entries must have a value.");
            return;
        }

        var context = await Db.Context();

        var frozenNowVersion = DateTime.Now.ToUniversalTime().TrimDateTimeToSeconds();

        foreach (var loopChanges in withChanges)
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