using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList;

[ObservableObject]
public partial class MapComponentListWithActionsContext
{
    [ObservableProperty] private readonly StatusControlContext _statusContext;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private Command _refreshDataCommand;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public MapComponentListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new MapComponentListLoader(100), WindowStatus);

        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);

        ListContext.ContextMenuItems = new List<ContextMenuItemData>
        {
            new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new()
            {
                ItemName = "Map Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.OpenUrlSelectedCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    public List<MapComponentListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is MapComponentListListItem)
            .Cast<MapComponentListListItem>().ToList() ?? new List<MapComponentListListItem>();
    }
}