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
    [ObservableProperty] private WindowIconStatus _windowStatus;
    [ObservableProperty] private RelayCommand _wordPressImportWindowCommand;

    public AllContentListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        CommonCommands = new CmsCommonCommands(StatusContext, WindowStatus);

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public AllContentListWithActionsContext(StatusControlContext statusContext, IContentListLoader reportFilter)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        ListContext ??= new ContentListContext(StatusContext, reportFilter);

        StatusContext.RunFireAndForgetBlockingTask(ListContext.LoadData);
    }

    public async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new AllContentListLoader(100), WindowStatus);

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