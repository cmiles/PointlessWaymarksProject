using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.GeoJsonList;

public class GeoJsonListWithActionsContext : INotifyPropertyChanged
{
    private Command _geoJsonLinkCodesToClipboardForSelectedCommand;
    private ContentListContext _listContext;
    private Command _refreshDataCommand;
    private StatusControlContext _statusContext;
    private WindowIconStatus _windowStatus;

    public GeoJsonListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public Command GeoJsonLinkCodesToClipboardForSelectedCommand
    {
        get => _geoJsonLinkCodesToClipboardForSelectedCommand;
        set
        {
            if (Equals(value, _geoJsonLinkCodesToClipboardForSelectedCommand)) return;
            _geoJsonLinkCodesToClipboardForSelectedCommand = value;
            OnPropertyChanged();
        }
    }

    public ContentListContext ListContext
    {
        get => _listContext;
        set
        {
            if (Equals(value, _listContext)) return;
            _listContext = value;
            OnPropertyChanged();
        }
    }

    public Command RefreshDataCommand
    {
        get => _refreshDataCommand;
        set
        {
            if (Equals(value, _refreshDataCommand)) return;
            _refreshDataCommand = value;
            OnPropertyChanged();
        }
    }


    public StatusControlContext StatusContext
    {
        get => _statusContext;
        set
        {
            if (Equals(value, _statusContext)) return;
            _statusContext = value;
            OnPropertyChanged();
        }
    }

    public WindowIconStatus WindowStatus
    {
        get => _windowStatus;
        set
        {
            if (Equals(value, _windowStatus)) return;
            _windowStatus = value;
            OnPropertyChanged();
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;

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
                ItemName = "Text Code to Clipboard",
                ItemCommand = GeoJsonLinkCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.OpenUrlSelectedCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public List<GeoJsonListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is GeoJsonListListItem)
            .Cast<GeoJsonListListItem>().ToList() ?? new List<GeoJsonListListItem>();
    }
}