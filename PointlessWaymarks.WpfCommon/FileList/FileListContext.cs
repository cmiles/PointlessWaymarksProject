﻿using System.Collections.ObjectModel;
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

public partial class FileListContext : ObservableObject, IDropTarget
{
    [ObservableProperty] private List<ContextMenuItemData> _contextMenuItems;
    [ObservableProperty] private List<string> _droppedFileExtensionAllowList = new();
    [ObservableProperty] private string _fileImportFilter = string.Empty;
    [ObservableProperty] private ObservableCollection<FileInfo>? _files;
    [ObservableProperty] private bool _replaceMode = true;
    [ObservableProperty] private FileInfo? _selectedFile;
    [ObservableProperty] private ObservableCollection<FileInfo>? _selectedFiles;
    [ObservableProperty] private IFileListSettings? _settings;
    [ObservableProperty] private StatusControlContext _statusContext;

    public FileListContext(StatusControlContext? statusContext, IFileListSettings? settings,
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
        DeleteSelectedFilesCommand = StatusContext.RunNonBlockingTaskCommand(DeleteSelectedFiles);

        var localContextItems = new List<ContextMenuItemData>
        {
            new() { ItemCommand = OpenSelectedFileDirectoryCommand, ItemName = "Open Directory" },
            new() { ItemCommand = OpenSelectedFileDirectoryCommand, ItemName = "Open File" }
        };

        _contextMenuItems = contextMenuItems.Union(localContextItems).ToList();
    }

    public RelayCommand AddFilesToTagCommand { get; }

    public RelayCommand AddFilesToTagFromDirectoryAndSubdirectoriesCommand { get; }

    public RelayCommand AddFilesToTagFromDirectoryCommand { get; }

    public RelayCommand DeleteSelectedFilesCommand { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global - Used in Xaml
    public RelayCommand OpenSelectedFileCommand { get; }

    public RelayCommand OpenSelectedFileDirectoryCommand { get; }

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

        Debug.Assert(Settings != null, nameof(Settings) + " != null");
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { Title = "Add Files", Multiselect = true, CheckFileExists = true, ValidateNames = true };
        if (!string.IsNullOrWhiteSpace(FileImportFilter)) filePicker.Filter = FileImportFilter;

        if (lastDirectory != null) filePicker.FileName = $"{lastDirectory.FullName}\\";

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        if (ReplaceMode) Files?.Clear();

        await Settings.SetLastDirectory(Path.GetDirectoryName(filePicker.FileNames.FirstOrDefault()) ?? string.Empty);

        var selectedFiles = filePicker.FileNames.Select(x => new FileInfo(x)).Where(x => !Files!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x =>
        {
            if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
        });
    }

    public async Task AddFilesToTag(List<string> filesToAdd)
    {
        if (!filesToAdd.Any()) return;

        await ResumeForegroundAsync();

        if (ReplaceMode) Files?.Clear();

        var selectedFiles = filesToAdd.Select(x => new FileInfo(x)).Where(x => x.Exists && !Files!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x =>
        {
            if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
        });
    }

    public async Task AddFilesToTagFromDirectory()
    {
        await ResumeBackgroundAsync();

        Debug.Assert(Settings != null, nameof(Settings) + " != null");
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory to Add", Multiselect = true };

        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        if (!folderPicker.SelectedPaths.Any())
        {
            StatusContext.ToastWarning("No directories selected?");
            return;
        }

        if (ReplaceMode) Files?.Clear();

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPaths[0]);

        if (selectedDirectory.Parent != null) await Settings.SetLastDirectory(selectedDirectory.Parent.FullName);
        else await Settings.SetLastDirectory(selectedDirectory.FullName);

        foreach (var loopPaths in folderPicker.SelectedPaths)
        {
            var loopDirectory = new DirectoryInfo(loopPaths);

            if (!loopDirectory.Exists)
            {
                StatusContext.ToastError($"{loopDirectory.FullName} doesn't exist?");
                continue;
            }

            var selectedFiles = loopDirectory.EnumerateFiles("*").ToList().Where(x => !Files!.Contains(x))
                .ToList();

            selectedFiles.ForEach(x =>
            {
                if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
            });
        }
    }

    public async Task AddFilesToTagFromDirectoryAndSubdirectories()
    {
        await ResumeBackgroundAsync();

        Debug.Assert(Settings != null, nameof(Settings) + " != null");
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory And Subdirectories to Add", Multiselect = true };
        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        if (!folderPicker.SelectedPaths.Any())
        {
            StatusContext.ToastWarning("No directories selected?");
            return;
        }

        if (ReplaceMode) Files?.Clear();

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPaths[0]);

        if (selectedDirectory.Parent != null) await Settings.SetLastDirectory(selectedDirectory.Parent.FullName);
        else await Settings.SetLastDirectory(selectedDirectory.FullName);

        foreach (var loopPaths in folderPicker.SelectedPaths)
        {
            var loopDirectory = new DirectoryInfo(loopPaths);

            if (!loopDirectory.Exists)
            {
                StatusContext.ToastError($"{loopDirectory.FullName} doesn't exist?");
                continue;
            }

            var selectedFiles = loopDirectory.EnumerateFiles("*", SearchOption.AllDirectories).ToList().Where(x => !Files!.Contains(x))
                .ToList();

            selectedFiles.ForEach(x =>
            {
                if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
            });
        }
    }

    public static async Task<FileListContext> CreateInstance(StatusControlContext statusContext,
        IFileListSettings? settings, List<ContextMenuItemData> contextMenuItems)
    {
        var newInstance = new FileListContext(statusContext, settings, contextMenuItems);

        await ResumeForegroundAsync();

        newInstance.Files = new ObservableCollection<FileInfo>();
        newInstance.SelectedFiles = new ObservableCollection<FileInfo>();

        return newInstance;
    }

    public async Task DeleteSelectedFiles()
    {
        await ResumeForegroundAsync();

        var toRemove = SelectedFiles?.ToList() ?? new List<FileInfo>();

        if (toRemove.Count <= 0)
        {
            StatusContext.ToastWarning("No Files Selected to Delete");
            return;
        }

        foreach (var loopFile in toRemove) Files?.Remove(loopFile);
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