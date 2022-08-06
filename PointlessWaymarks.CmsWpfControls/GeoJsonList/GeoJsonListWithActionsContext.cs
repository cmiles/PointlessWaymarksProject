using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonList;

[ObservableObject]
public partial class GeoJsonListWithActionsContext
{
    [ObservableProperty] private RelayCommand _geoJsonLinkCodesToClipboardForSelectedCommand;
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private RelayCommand _refreshDataCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public GeoJsonListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    private async Task LinkBracketCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = SelectedItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + @$"{BracketCodeGeoJsonLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new GeoJsonListLoader(100), WindowStatus);

        GeoJsonLinkCodesToClipboardForSelectedCommand =
            StatusContext.RunBlockingTaskCommand(LinkBracketCodesToClipboardForSelected);
        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);

        ListContext.ContextMenuItems = new List<ContextMenuItemData>
        {
            new() { ItemName = "Edit", ItemCommand = ListContext.EditSelectedCommand },
            new()
            {
                ItemName = "Map Code to Clipboard",
                ItemCommand = ListContext.BracketCodeToClipboardSelectedCommand
            },
            new()
            {
                ItemName = "Text Code to Clipboard", ItemCommand = GeoJsonLinkCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    public List<GeoJsonListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is GeoJsonListListItem)
            .Cast<GeoJsonListListItem>().ToList() ?? new List<GeoJsonListListItem>();
    }
}