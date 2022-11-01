using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.GeoTaggingService;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.GeoTaggingGui;

[ObservableObject]
public partial class DirectoryBasedTaggerContext
{
    [ObservableProperty] private bool _createBackups;

    [ObservableProperty] private string _exifToolFullName = string.Empty;
    [ObservableProperty] private ObservableCollection<FileInfo>? _filesToTag;
    [ObservableProperty] private ObservableCollection<FileInfo>? _gpxFiles;

    [ObservableProperty] private string _lastTagOutput = string.Empty;
    [ObservableProperty] private int _offsetPhotoTimeInMinutes;
    [ObservableProperty] private bool _overwriteExistingGeoLocation;
    [ObservableProperty] private int _pointsMustBeWithinMinutes = 5;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private bool _testRunOnly;
    [ObservableProperty] private WindowIconStatus? _windowStatus;

    public DirectoryBasedTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _windowStatus = windowStatus;

        AddFilesToTagFromDirectoryCommand = StatusContext.RunBlockingTaskCommand(AddFilesToTagFromDirectory);
        AddFilesToTagCommand = StatusContext.RunBlockingTaskCommand(AddFilesToTag);
        AddFilesToTagFromDirectoryAndSubdirectoriesCommand =
            StatusContext.RunBlockingTaskCommand(AddFilesToTagFromDirectoryAndSubdirectories);

        AddGpxFilesFromDirectoryCommand = StatusContext.RunBlockingTaskCommand(AddGpxFilesFromDirectory);
        AddGpxFilesCommand = StatusContext.RunBlockingTaskCommand(AddGpxFiles);
        AddGpxFilesFromDirectoryAndSubdirectoriesCommand =
            StatusContext.RunBlockingTaskCommand(AddGpxFilesFromDirectoryAndSubdirectories);

        TagCommand = StatusContext.RunBlockingTaskCommand(Tag);
    }

    public RelayCommand AddFilesToTagCommand { get; set; }

    public RelayCommand AddFilesToTagFromDirectoryAndSubdirectoriesCommand { get; set; }

    public RelayCommand AddFilesToTagFromDirectoryCommand { get; set; }

    public RelayCommand AddGpxFilesCommand { get; set; }

    public RelayCommand AddGpxFilesFromDirectoryAndSubdirectoriesCommand { get; set; }

    public RelayCommand AddGpxFilesFromDirectoryCommand { get; set; }

    public RelayCommand TagCommand { get; set; }

    public async System.Threading.Tasks.Task AddFilesToTag()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var filePicker = new VistaOpenFileDialog
            { Title = "Add Files", Multiselect = true, CheckFileExists = true, ValidateNames = true };
        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        var selectedFiles = filePicker.FileNames.Select(x => new FileInfo(x)).Where(x => !FilesToTag!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => FilesToTag!.Add(x));
    }

    public async System.Threading.Tasks.Task AddFilesToTagFromDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory to Add", Multiselect = false };
        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*").ToList().Where(x => !FilesToTag!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => FilesToTag!.Add(x));
    }

    public async System.Threading.Tasks.Task AddFilesToTagFromDirectoryAndSubdirectories()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory And Subdirectories to Add", Multiselect = false };
        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(x => !FilesToTag!.Contains(x)).ToList();

        selectedFiles.ForEach(x => FilesToTag!.Add(x));
    }

    public async System.Threading.Tasks.Task AddGpxFiles()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var filePicker = new VistaOpenFileDialog
        {
            Title = "Select Gpx Files", Multiselect = true, CheckFileExists = true, ValidateNames = true,
            DefaultExt = ".gpx"
        };
        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        var selectedFiles = filePicker.FileNames.Select(x => new FileInfo(x)).Where(x =>
                x.Extension.Equals(".GPX", StringComparison.InvariantCultureIgnoreCase) && !GpxFiles!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => GpxFiles!.Add(x));
    }

    public async System.Threading.Tasks.Task AddGpxFilesFromDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Add gpx files in Directory", Multiselect = false };
        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*").ToList().Where(x =>
                x.Extension.Equals(".GPX", StringComparison.InvariantCultureIgnoreCase) && !GpxFiles!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => GpxFiles!.Add(x));
    }

    public async System.Threading.Tasks.Task AddGpxFilesFromDirectoryAndSubdirectories()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Add GPX Files in Directory And Subdirectories", Multiselect = false };
        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Where(x =>
                x.Extension.Equals(".GPX", StringComparison.InvariantCultureIgnoreCase) && !GpxFiles!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => GpxFiles!.Add(x));
    }

    public static async Task<DirectoryBasedTaggerContext> CreateInstance(StatusControlContext? statusContext,
        WindowIconStatus? windowStatus)
    {
        var control = new DirectoryBasedTaggerContext(statusContext, windowStatus);
        await control.LoadData();
        return control;
    }

    public async System.Threading.Tasks.Task LoadData()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        FilesToTag = new ObservableCollection<FileInfo>();
        GpxFiles = new ObservableCollection<FileInfo>();
    }

    public async System.Threading.Tasks.Task Tag()
    {
        var fileListGpxService = new FileListGpxService(GpxFiles!.ToList());
        var tagger = new GeoTag();
        LastTagOutput = await tagger.Tag(FilesToTag!.ToList(), new List<IGpxService> { fileListGpxService },
            TestRunOnly, CreateBackups,
            PointsMustBeWithinMinutes, OffsetPhotoTimeInMinutes, OverwriteExistingGeoLocation, ExifToolFullName,
            StatusContext.ProgressTracker());
    }
}