using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.WordPressXmlImport;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.AllContentList;

public partial class AllContentListWithActionsContext : ObservableObject
{
    [ObservableProperty] private CmsCommonCommands _commonCommands;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus? _windowStatus;
    [ObservableProperty] private RelayCommand? _wordPressImportWindowCommand;

    private AllContentListWithActionsContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus,
        ContentListContext factoryListContext)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _windowStatus = windowStatus;

        _commonCommands = new CmsCommonCommands(StatusContext, WindowStatus);
        _listContext = factoryListContext;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public static async Task<AllContentListWithActionsContext> CreateInstance(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        var factoryStatusContext = statusContext ?? new StatusControlContext();
        var factoryListContext = await ContentListContext.CreateInstance(factoryStatusContext, new AllContentListLoader(100), windowStatus);

        return new AllContentListWithActionsContext(factoryStatusContext, windowStatus, factoryListContext);
    }

    public static async Task<AllContentListWithActionsContext> CreateInstance(StatusControlContext? statusContext, IContentListLoader reportFilter)
    {
        var factoryStatusContext = statusContext ?? new StatusControlContext();
        var factoryListContext = await ContentListContext.CreateInstance(factoryStatusContext, reportFilter);

        return new AllContentListWithActionsContext(factoryStatusContext, null, factoryListContext);
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        WordPressImportWindowCommand = StatusContext.RunNonBlockingTaskCommand(WordPressImportWindow);

        ListContext.ContextMenuItems = new List<ContextMenuItemData>
        {
            new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new()
            {
                ItemName = "Code to Clipboard", ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand }
        };

        await ListContext.LoadData();
    }

    private async Task WordPressImportWindow()
    {
        await (await WordPressXmlImportWindow.CreateInstance()).PositionWindowAndShowOnUiThread();
    }
}