using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.MapComponentList;

public partial class MapComponentListWithActionsContext : ObservableObject
{
    [ObservableProperty] private CmsCommonCommands _commonCommands;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private RelayCommand _refreshDataCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus? _windowStatus;

    private MapComponentListWithActionsContext(StatusControlContext statusContext, WindowIconStatus? windowStatus, ContentListContext listContext, bool loadInBackground = true)
    {
        _statusContext = statusContext;
        _windowStatus = windowStatus;

        _listContext = listContext;

        _commonCommands = new CmsCommonCommands(StatusContext, WindowStatus);

        _refreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);

        ListContext.ContextMenuItems = new List<ContextMenuItemData>
        {
            new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new()
            {
                ItemName = "Map Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };


        if(loadInBackground) StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public static async Task<MapComponentListWithActionsContext> CreateInstance(StatusControlContext? statusContext, WindowIconStatus? windowStatus = null, bool loadInBackground = true)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();
        var factoryListContext = await ContentListContext.CreateInstance(factoryContext, new MapComponentListLoader(100), windowStatus);

        return new MapComponentListWithActionsContext(factoryContext, windowStatus, factoryListContext, loadInBackground);
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        await ListContext.LoadData();
    }

    public List<MapComponentListListItem> SelectedItems()
    {
        return ListContext.ListSelection.SelectedItems?.Where(x => x is MapComponentListListItem)
            .Cast<MapComponentListListItem>().ToList() ?? new List<MapComponentListListItem>();
    }
}