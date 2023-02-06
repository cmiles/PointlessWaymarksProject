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

[ObservableObject]
public partial class MenuLinkEditorContext
{
    [ObservableProperty] private CmsCommonCommands _commonCommands;
    [ObservableProperty] private string _helpMarkdown;
    [ObservableProperty] private ObservableCollection<MenuLinkListItem> _items = new();
    [ObservableProperty] private List<MenuLinkListItem> _selectedItems;
    [ObservableProperty] private StatusControlContext _statusContext;

    public MenuLinkEditorContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        CommonCommands = new CmsCommonCommands(StatusContext);

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

        HelpMarkdown = MenuLinksHelpMarkdown.HelpBlock;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public RelayCommand AddItemCommand { get; init; }

    public RelayCommand DeleteItemCommand { get; init; }

    public RelayCommand<MenuLinkListItem> InsertIndexTagIndexCommand { get; init; }

    public RelayCommand<MenuLinkListItem> InsertLinkListCommand { get; init; }

    public RelayCommand<MenuLinkListItem> InsertPhotoGalleryCommand { get; init; }

    public RelayCommand<MenuLinkListItem> InsertSearchPageCommand { get; init; }

    public RelayCommand<MenuLinkListItem> InsertTagSearchCommand { get; init; }

    public RelayCommand<MenuLinkListItem> MoveItemDownCommand { get; init; }

    public RelayCommand<MenuLinkListItem> MoveItemUpCommand { get; init; }

    public RelayCommand SaveCommand { get; init; }

    private async Task AddItem()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newItem = new MenuLinkListItem { DbEntry = new MenuLink(), UserOrder = Items.Count };

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

    private async Task InsertIntoLinkTag(MenuLinkListItem listItem, string toInsert)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (listItem == null)
        {
            StatusContext.ToastError("No item?");
            return;
        }

        listItem.UserLink = (listItem.UserLink ?? string.Empty).Trim();

        listItem.UserLink += toInsert;
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = await Db.Context();
        var existingEntries = await context.MenuLinks.ToListAsync();
        var listItems = existingEntries.Select(x =>
                new MenuLinkListItem { DbEntry = x, UserLink = x.LinkTag?.Trim() ?? string.Empty })
            .OrderBy(x => x.UserOrder).ThenBy(x => x.UserLink).ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        listItems.ForEach(x => Items.Add(x));

        await RenumberItems();
    }

    private async Task MoveItemDown(MenuLinkListItem listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await RenumberItems();

        var currentItemIndex = Items.IndexOf(listItem);

        if (currentItemIndex == Items.Count - 1) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Move(currentItemIndex, currentItemIndex + 1);

        await RenumberItems();
    }

    private async Task MoveItemUp(MenuLinkListItem listItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

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

        if (Items == null || !Items.Any())
        {
            StatusContext.ToastError("No entries to save?");
            return;
        }

        foreach (var loopItems in Items) loopItems.UserLink = (loopItems.UserLink ?? string.Empty).Trim();

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
            if (loopChanges.DbEntry == null || loopChanges.DbEntry.Id < 1)
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
                var toUpdate = await context.MenuLinks.SingleOrDefaultAsync(x => x.Id == loopChanges.DbEntry.Id);

                toUpdate.LinkTag = loopChanges.UserLink;
                toUpdate.MenuOrder = loopChanges.UserOrder;
                toUpdate.ContentVersion = frozenNowVersion;

                loopChanges.DbEntry = toUpdate;
            }

        await context.SaveChangesAsync(true);

        StatusContext.ToastSuccess("Saved Changes");
    }
}