#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using static PointlessWaymarks.WpfCommon.ThreadSwitcher.ThreadSwitcher;

namespace PointlessWaymarks.WpfCommon.FileList;

[ObservableObject]
public partial class FileListViewModel
{
    [ObservableProperty] private List<ContextMenuItemData> _contextMenuItems;
    [ObservableProperty] private ObservableCollection<FileInfo>? _filesToTag;
    [ObservableProperty] private ObservableCollection<FileInfo>? _filesToTagSelected;
    [ObservableProperty] private IFileListSettings _settings;
    [ObservableProperty] private StatusControlContext _statusContext;


    public FileListViewModel(StatusControlContext? statusContext, IFileListSettings settings,
        List<ContextMenuItemData> contextMenuItems)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _settings = settings;

        AddFilesToTagFromDirectoryCommand = StatusContext.RunBlockingTaskCommand(AddFilesToTagFromDirectory);
        AddFilesToTagCommand = StatusContext.RunBlockingTaskCommand(AddFilesToTag);
        AddFilesToTagFromDirectoryAndSubdirectoriesCommand =
            StatusContext.RunBlockingTaskCommand(AddFilesToTagFromDirectoryAndSubdirectories);
        _contextMenuItems = contextMenuItems;
    }

    public RelayCommand AddFilesToTagCommand { get; set; }

    public RelayCommand AddFilesToTagFromDirectoryAndSubdirectoriesCommand { get; set; }

    public RelayCommand AddFilesToTagFromDirectoryCommand { get; set; }

    public async Task AddFilesToTag()
    {
        await ResumeBackgroundAsync();
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { Title = "Add Files", Multiselect = true, CheckFileExists = true, ValidateNames = true };
        if (lastDirectory != null) filePicker.FileName = $"{lastDirectory.FullName}\\";

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        FilesToTag?.Clear();

        await _settings.SetLastDirectory(Path.GetDirectoryName(filePicker.FileNames.FirstOrDefault()));

        var selectedFiles = filePicker.FileNames.Select(x => new FileInfo(x)).Where(x => !FilesToTag!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => FilesToTag!.Add(x));
    }

    public async Task AddFilesToTagFromDirectory()
    {
        await ResumeBackgroundAsync();
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory to Add", Multiselect = false };

        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        FilesToTag?.Clear();

        await Settings.SetLastDirectory(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*").ToList().Where(x => !FilesToTag!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => FilesToTag!.Add(x));
    }

    public async Task AddFilesToTagFromDirectoryAndSubdirectories()
    {
        await ResumeBackgroundAsync();
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory And Subdirectories to Add", Multiselect = false };
        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        FilesToTag?.Clear();

        await Settings.SetLastDirectory(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(x => !FilesToTag!.Contains(x)).ToList();

        selectedFiles.ForEach(x => FilesToTag!.Add(x));
    }

    public static async Task<FileListViewModel> CreateInstance(StatusControlContext statusContext,
        IFileListSettings settings, List<ContextMenuItemData> contextMenuItems)
    {
        var newInstance = new FileListViewModel(statusContext, settings, contextMenuItems);

        await ResumeForegroundAsync();

        newInstance.FilesToTag = new ObservableCollection<FileInfo>();
        newInstance.FilesToTagSelected = new ObservableCollection<FileInfo>();

        return newInstance;
    }
}