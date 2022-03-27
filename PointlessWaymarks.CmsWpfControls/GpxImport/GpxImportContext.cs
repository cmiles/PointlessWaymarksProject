using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NetTopologySuite.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

[ObservableObject]
public partial class GpxImportContext
{
    [ObservableProperty] private RelayCommand _chooseAndLoadFileCommand;
    [ObservableProperty] private string _importFileName;
    [ObservableProperty] private ObservableCollection<IGpxImportListItem> _items;
    [ObservableProperty] private ObservableCollection<IGpxImportListItem> _listSelection;
    [ObservableProperty] private IGpxImportListItem _selectedItem;
    [ObservableProperty] private List<IGpxImportListItem> _selectedItems;
    [ObservableProperty] private StatusControlContext _statusContext;

    public GpxImportContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();

        ChooseAndLoadFileCommand = StatusContext.RunBlockingTaskCommand(ChooseAndLoadFile);
    }

    public async Task ChooseAndLoadFile()
    {
        StatusContext.Progress("Starting File Chooser");

        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { Filter = "gpx files (*.gpx)|*.gpx|tcx files (*.tcx)|*.tcx|fit files (*.fit)|*.fit|All files (*.*)|*.*" };

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Checking that file exists");

        var possibleFile = new FileInfo(filePicker.FileName);

        if (!possibleFile.Exists)
        {
            StatusContext.ToastError("File doesn't exist?");
            return;
        }

        await LoadFile(possibleFile.FullName);
    }

    public async Task LoadFile(string fileName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var fileInfo = new FileInfo(fileName);

        if (!fileInfo.Exists)
        {
            StatusContext.ToastError("File does not exist?");
            return;
        }

        GpxFile gpxFile;
        try
        {
            StatusContext.Progress($"Parsing GPX File {fileInfo.FullName}...");
            gpxFile = GpxFile.Parse(await File.ReadAllTextAsync(fileInfo.FullName),
                new GpxReaderSettings
                {
                    BuildWebLinksForVeryLongUriValues = true, IgnoreBadDateTime = true,
                    IgnoreUnexpectedChildrenOfTopLevelElement = true, IgnoreVersionAttribute = true
                });
        }
        catch (Exception e)
        {
            await StatusContext.ShowMessageWithOkButton("GPX File Parse Error",
                $"Parsing {fileInfo.FullName} as a GPX file resulted in an error - {e.Message}");
            return;
        }

        var waypoints = gpxFile.Waypoints.Select(x => new GpxImportWaypoint { Waypoint = x }).ToList();
        StatusContext.Progress($"Found {waypoints.Count} Waypoints");

        var tracks = gpxFile.Tracks.Select(x => new GpxImportTrack { Track = x }).ToList();
        StatusContext.Progress($"Found {tracks.Count} Tracks");

        var routes = gpxFile.Routes.Select(x => new GpxImportRoute { Route = x }).ToList();
        StatusContext.Progress($"Found {routes.Count} Routes");

        ImportFileName = fileInfo.FullName;

        StatusContext.Progress("Setting up list of import items...");

        await ThreadSwitcher.ResumeForegroundAsync();

        Items ??= new ObservableCollection<IGpxImportListItem>();
        Items.Clear();

        waypoints.ForEach(x => Items.Add(x));
        tracks.ForEach(x => Items.Add(x));
        routes.ForEach(x => Items.Add(x));
    }
}