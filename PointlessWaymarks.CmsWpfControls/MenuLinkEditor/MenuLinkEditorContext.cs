using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.HelpDisplay;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.MenuLinkEditor;

public partial class MenuLinkEditorContext : ObservableObject
{
    [ObservableProperty] private CmsCommonCommands _commonCommands;
    [ObservableProperty] private string _helpMarkdown;
    [ObservableProperty] private ObservableCollection<MenuLinkListItem> _items;
    [ObservableProperty] private List<MenuLinkListItem>? _selectedItems;
    [ObservableProperty] private StatusControlContext _statusContext;

    private MenuLinkEditorContext(StatusControlContext statusContext, ObservableCollection<MenuLinkListItem> items, bool loadInBackground = true)
    {
        _statusContext = statusContext;
        _commonCommands = new CmsCommonCommands(StatusContext);

        AddItemCommand = StatusContext.RunBlockingTaskCommand(AddItem);
        DeleteItemCommand = StatusContext.RunBlockingTaskCommand(DeleteItems);
        MoveItemUpCommand = StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(MoveItemUp);
        MoveItemDownCommand = StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(MoveItemDown);
        SaveCommand = StatusContext.RunBlockingTaskCommand(Save);
        InsertIndexTagIndexCommand =
            StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(
                x => InsertIntoLinkTag(x, "{{index; text Main;}}"));
        InsertTagSearchCommand =
            StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(x =>
                InsertIntoLinkTag(x, "{{tagspage; text Tags;}}"));
        InsertPhotoGalleryCommand =
            StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(x =>
                InsertIntoLinkTag(x, "{{photogallerypage; text Photos;}}"));
        InsertSearchPageCommand =
            StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(x =>
                InsertIntoLinkTag(x, "{{searchpage; text Search;}}"));
        InsertLinkListCommand =
            StatusContext.RunNonBlockingTaskCommand<MenuLinkListItem>(x =>
                InsertIntoLinkTag(x, "{{linklistpage; text Links;}}"));

        _helpMarkdown = MenuLinksHelpMarkdown.HelpBlock;

        _items = items;

        if(loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public static Task<MenuLinkEditorContext> CreateInstance(StatusControlContext? statusContext, bool loadInBackground = true)
    {
        ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();

        ThreadSwitcher.ResumeForegroundAsync();

        var factoryItems = new ObservableCollection<MenuLinkListItem>();

        ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new MenuLinkEditorContext(factoryContext, factoryItems, loadInBackground);

        return Task.FromResult(toReturn);
    }

    public RelayCommand AddItemCommand { get; }

    public RelayCommand DeleteItemCommand { get; }

    public RelayCommand<MenuLinkListItem> InsertIndexTagIndexCommand { get; }

    public RelayCommand<MenuLinkListItem> InsertLinkListCommand { get; }

    public RelayCommand<MenuLinkListItem> InsertPhotoGalleryCommand { get; }

    public RelayCommand<MenuLinkListItem> InsertSearchPageCommand { get; }

    public RelayCommand<MenuLinkListItem> InsertTagSearchCommand { get; }

    public RelayCommand<MenuLinkListItem> MoveItemDownCommand { get; }

    public RelayCommand<MenuLinkListItem> MoveItemUpCommand { get; }

    public RelayCommand SaveCommand { get; }

    private async Task AddItem()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newItem = await MenuLinkListItem.CreateInstance(new MenuLink());
        newItem.UserOrder = Items.Count;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(newItem);
    }

    private async Task DeleteItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var selected = SelectedItems;

        if (selected == null || !selected.Any())
        {
            StatusContext.ToastError("Nothing Selected to Delete?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        selected.ForEach(x => Items.Remove(x));

        await RenumberItems();
    }

    private async Task InsertIntoLinkTag(MenuLinkListItem? listItem, string toInsert)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            StatusContext.ToastError("No item?");
            return;
        }

        listItem.UserLink = (listItem.UserLink).Trim();

        listItem.UserLink += toInsert;
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = await Db.Context();
        var existingEntries = await context.MenuLinks.ToListAsync();
        List<MenuLinkListItem> listItems = new();

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

    private async Task MoveItemDown(MenuLinkListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            StatusContext.ToastError("No item?");
            return;
        }

        await RenumberItems();

        var currentItemIndex = Items.IndexOf(listItem);

        if (currentItemIndex == Items.Count - 1) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Move(currentItemIndex, currentItemIndex + 1);

        await RenumberItems();
    }

    private async Task MoveItemUp(MenuLinkListItem? listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            StatusContext.ToastError("No item?");
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

    private async Task Save()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!Items.Any())
        {
            StatusContext.ToastError("No entries to save?");
            return;
        }

        foreach (var loopItems in Items) loopItems.UserLink = (loopItems.UserLink).Trim();

        await RenumberItems();

        var withChanges = Items.Where(x => x.HasChanges).ToList();

        if (!withChanges.Any())
        {
            StatusContext.ToastError("No entries have changed?");
            return;
        }

        if (withChanges.Any(x => string.IsNullOrWhiteSpace(x.UserLink)))
        {
            StatusContext.ToastError("All Entries must have a value.");
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

        StatusContext.ToastSuccess("Saved Changes");
    }
}