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
    [ObservableProperty] private ObservableCollection<FileInfo>? _files;
    [ObservableProperty] private ObservableCollection<FileInfo>? _selectedFiles;
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

        Files?.Clear();

        await _settings.SetLastDirectory(Path.GetDirectoryName(filePicker.FileNames.FirstOrDefault()));

        var selectedFiles = filePicker.FileNames.Select(x => new FileInfo(x)).Where(x => !Files!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => Files!.Add(x));
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

        Files?.Clear();

        await Settings.SetLastDirectory(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*").ToList().Where(x => !Files!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x => Files!.Add(x));
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

        Files?.Clear();

        await Settings.SetLastDirectory(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(x => !Files!.Contains(x)).ToList();

        selectedFiles.ForEach(x => Files!.Add(x));
    }

    public static async Task<FileListViewModel> CreateInstance(StatusControlContext statusContext,
        IFileListSettings settings, List<ContextMenuItemData> contextMenuItems)
    {
        var newInstance = new FileListViewModel(statusContext, settings, contextMenuItems);

        await ResumeForegroundAsync();

        newInstance.Files = new ObservableCollection<FileInfo>();
        newInstance.SelectedFiles = new ObservableCollection<FileInfo>();

        return newInstance;
    }
}