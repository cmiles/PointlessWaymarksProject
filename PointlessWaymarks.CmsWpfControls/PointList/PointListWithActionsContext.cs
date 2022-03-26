using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NetTopologySuite.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.PointList;

[ObservableObject]
public partial class PointListWithActionsContext
{
    [ObservableProperty] private ContentListContext _listContext;
    [ObservableProperty] private RelayCommand _pointLinkBracketCodesToClipboardForSelectedCommand;
    [ObservableProperty] private RelayCommand _refreshDataCommand;
    [ObservableProperty] private RelayCommand _selectedToGpxFileCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private WindowIconStatus _windowStatus;

    public PointListWithActionsContext(StatusControlContext statusContext, WindowIconStatus windowStatus = null)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        WindowStatus = windowStatus;

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        ListContext ??= new ContentListContext(StatusContext, new PointListLoader(100), WindowStatus);

        RefreshDataCommand = StatusContext.RunBlockingTaskCommand(ListContext.LoadData);
        PointLinkBracketCodesToClipboardForSelectedCommand =
            StatusContext.RunNonBlockingTaskCommand(PointLinkBracketCodesToClipboardForSelected);
        SelectedToGpxFileCommand = StatusContext.RunBlockingTaskCommand(SelectedToGpxFile);


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
                ItemCommand = PointLinkBracketCodesToClipboardForSelectedCommand
            },
            new() { ItemName = "Selected Points to GPX File", ItemCommand = SelectedToGpxFileCommand },
            new() { ItemName = "Extract New Links", ItemCommand = ListContext.ExtractNewLinksSelectedCommand },
            new() { ItemName = "Open URL", ItemCommand = ListContext.ViewOnSiteCommand },
            new() { ItemName = "Delete", ItemCommand = ListContext.DeleteSelectedCommand },
            new() { ItemName = "View History", ItemCommand = ListContext.ViewHistorySelectedCommand },
            new() { ItemName = "Refresh Data", ItemCommand = RefreshDataCommand }
        };

        await ListContext.LoadData();
    }

    private async Task PointLinkBracketCodesToClipboardForSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        var finalString = SelectedItems().Aggregate(string.Empty,
            (current, loopSelected) =>
                current + @$"{BracketCodePointLinks.Create(loopSelected.DbEntry)}{Environment.NewLine}");

        await ThreadSwitcher.ResumeForegroundAsync();

        Clipboard.SetText(finalString);

        StatusContext.ToastSuccess($"To Clipboard {finalString}");
    }

    public List<PointListListItem> SelectedItems()
    {
        return ListContext?.ListSelection?.SelectedItems?.Where(x => x is PointListListItem).Cast<PointListListItem>()
            .ToList() ?? new List<PointListListItem>();
    }

    private async Task SelectedToGpxFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItems() == null || !SelectedItems().Any())
        {
            StatusContext.ToastError("Nothing Selected?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var fileDialog = new VistaSaveFileDialog
        {
            Filter = "gpx|*.gpx;",
            AddExtension = true,
            OverwritePrompt = true,
            DefaultExt = ".gpx"
        };
        var fileDialogResult = fileDialog.ShowDialog();

        var fileName = fileDialog.FileName;

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!fileDialogResult ?? false) return;

        var waypointList = new List<GpxWaypoint>();

        foreach (var loopItems in SelectedItems())
        {
            var toAdd = new GpxWaypoint(new GpxLongitude(loopItems.DbEntry.Longitude),
                new GpxLatitude(loopItems.DbEntry.Latitude),
                loopItems.DbEntry.Elevation,
                loopItems.DbEntry.LastUpdatedOn?.ToUniversalTime() ?? loopItems.DbEntry.CreatedOn.ToUniversalTime(),
                null, null,
                loopItems.DbEntry.Title, null, loopItems.DbEntry.Summary, null, new ImmutableArray<GpxWebLink>(), null,
                null, null, null, null, null, null, null, null, null);
            waypointList.Add(toAdd);
        }

        var fileStream = new FileStream(fileName, FileMode.OpenOrCreate);

        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, CloseOutput = true };
        await using var xmlWriter = XmlWriter.Create(fileStream, writerSettings);
        GpxWriter.Write(xmlWriter, null, new GpxMetadata("Pointless Waymarks CMS"), waypointList, null, null, null);
        xmlWriter.Close();
    }
}