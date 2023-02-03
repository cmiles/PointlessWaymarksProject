#nullable enable

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using static PointlessWaymarks.WpfCommon.ThreadSwitcher.ThreadSwitcher;

namespace PointlessWaymarks.WpfCommon.FileList;

public partial class FileListViewModel : ObservableObject, IDropTarget
{
    [ObservableProperty] private List<ContextMenuItemData> _contextMenuItems;
    [ObservableProperty] private List<string> _droppedFileExtensionAllowList = new();
    [ObservableProperty] private string _fileImportFilter = string.Empty;
    [ObservableProperty] private ObservableCollection<FileInfo>? _files;
    [ObservableProperty] private bool _replaceMode = true;
    [ObservableProperty] private FileInfo? _selectedFile;
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
        OpenSelectedFileDirectoryCommand = StatusContext.RunNonBlockingTaskCommand(OpenSelectedFileDirectory);
        OpenSelectedFileCommand = StatusContext.RunNonBlockingTaskCommand(OpenSelectedFile);

        var localContextItems = new List<ContextMenuItemData>
        {
            new() { ItemCommand = OpenSelectedFileDirectoryCommand, ItemName = "Open Directory" },
            new() { ItemCommand = OpenSelectedFileDirectoryCommand, ItemName = "Open File" }
        };

        _contextMenuItems = contextMenuItems.Union(localContextItems).ToList();
    }

    public RelayCommand AddFilesToTagCommand { get; set; }

    public RelayCommand AddFilesToTagFromDirectoryAndSubdirectoriesCommand { get; set; }

    public RelayCommand AddFilesToTagFromDirectoryCommand { get; set; }

    public RelayCommand OpenSelectedFileCommand { get; set; }

    public RelayCommand OpenSelectedFileDirectoryCommand { get; set; }

    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not DataObject data) return;

        var dataIsFileDrop = data.GetDataPresent("FileDrop");

        if (!dataIsFileDrop) return;

        if (data.GetData("FileDrop") is not string[] fileData || !fileData.Any()) return;

        if (!DroppedFileExtensionAllowList.Any() || DroppedFileExtensionAllowList.Any(x =>
                fileData.Any(y => y.EndsWith(x, StringComparison.OrdinalIgnoreCase))))
            dropInfo.Effects = DragDropEffects.Copy;
    }

    public void Drop(IDropInfo dropInfo)
    {
        StatusContext.RunBlockingTask(async () => await AddFilesFromGuiDrop(dropInfo.Data));
    }

    public async Task AddFilesFromGuiDrop(object dropData)
    {
        await ResumeBackgroundAsync();

        if (dropData is not DataObject data)
        {
            StatusContext.ToastWarning("The program didn't find file information in the dropped info?");
            return;
        }

        if (data.GetData("FileDrop") is not string[] fileData || !fileData.Any())
        {
            StatusContext.ToastWarning("The program didn't find files in the dropped info?");
            return;
        }

        var selectedFiles = fileData.Where(File.Exists).Select(x => new FileInfo(x)).OrderBy(x => x.FullName).ToList();

        if (DroppedFileExtensionAllowList.Any())
            selectedFiles = selectedFiles
                .Where(x => DroppedFileExtensionAllowList.Any(y =>
                    x.FullName.EndsWith(y, StringComparison.OrdinalIgnoreCase))).OrderBy(x => x.FullName).ToList();

        await ResumeForegroundAsync();

        if (ReplaceMode) Files?.Clear();

        selectedFiles.ForEach(x =>
        {
            if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
        });
    }

    public async Task AddFilesToTag()
    {
        await ResumeBackgroundAsync();
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { Title = "Add Files", Multiselect = true, CheckFileExists = true, ValidateNames = true };
        if (!string.IsNullOrWhiteSpace(FileImportFilter)) filePicker.Filter = FileImportFilter;

        if (lastDirectory != null) filePicker.FileName = $"{lastDirectory.FullName}\\";

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        if (ReplaceMode) Files?.Clear();

        await _settings.SetLastDirectory(Path.GetDirectoryName(filePicker.FileNames.FirstOrDefault()));

        var selectedFiles = filePicker.FileNames.Select(x => new FileInfo(x)).Where(x => !Files!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x =>
        {
            if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
        });
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

        if (ReplaceMode) Files?.Clear();

        await Settings.SetLastDirectory(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*").ToList().Where(x => !Files!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x =>
        {
            if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
        });
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

        if (ReplaceMode) Files?.Clear();

        await Settings.SetLastDirectory(folderPicker.SelectedPath);

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPath);
        var selectedFiles = selectedDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(x => !Files!.Contains(x)).ToList();

        selectedFiles.ForEach(x =>
        {
            if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
        });
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

    private async Task OpenSelectedFile()
    {
        await ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            StatusContext.ToastWarning("No Selected File or Selected File no longer exists?");
            return;
        }

        await ResumeForegroundAsync();

        var ps = new ProcessStartInfo(SelectedFile.FullName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }


    private async Task OpenSelectedFileDirectory()
    {
        await ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            StatusContext.ToastWarning("No Selected File or Selected File no longer exists?");
            return;
        }

        await ProcessHelpers.OpenExplorerWindowForFile(SelectedFile.FullName);
    }
}