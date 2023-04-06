using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.SiteViewerGui;

public partial class ProjectChooserContext : ObservableObject
{
    [ObservableProperty] private RelayCommand _chooseDirectoryCommand;
    [ObservableProperty] private RelayCommand _chooseFileCommand;
    [ObservableProperty] private RelayCommand<ProjectFileListItem> _chooseRecentFileCommand;
    [ObservableProperty] private ObservableCollection<ProjectFileListItem> _items;
    [ObservableProperty] private List<string> _recentSettingFilesNames;
    [ObservableProperty] private RelayCommand<ProjectFileListItem> _removeSelectedFileCommand;
    [ObservableProperty] private StatusControlContext _statusContext;

    private ProjectChooserContext(StatusControlContext statusContext, ObservableCollection<ProjectFileListItem> items,
        List<string> recentFiles)
    {
        _statusContext = statusContext;
        _chooseFileCommand = StatusContext.RunNonBlockingTaskCommand(ChooseFile);
        _chooseDirectoryCommand = StatusContext.RunBlockingTaskCommand(ChooseDirectory);
        _chooseRecentFileCommand = StatusContext.RunNonBlockingTaskCommand<ProjectFileListItem>(LaunchRecentFile);
        _removeSelectedFileCommand =
            StatusContext.RunNonBlockingTaskCommand<ProjectFileListItem>(RemoveSelectedFile);
        _recentSettingFilesNames = recentFiles;
        _items = items;
    }

    private async Task ChooseDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var directoryPicker = new VistaFolderBrowserDialog();

        var result = directoryPicker.ShowDialog();

        if (!result ?? false) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var possibleDirectory = new DirectoryInfo(directoryPicker.SelectedPath);

        if (!possibleDirectory.Exists)
        {
            StatusContext.ToastError("Directory doesn't exist?");
            return;
        }

        DirectoryUpdated?.Invoke(this,
            (possibleDirectory.FullName, Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    private async Task ChooseFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog { Filter = "ini files (*.ini)|*.ini|All files (*.*)|*.*" };

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var possibleFile = new FileInfo(filePicker.FileName);

        if (!possibleFile.Exists)
        {
            StatusContext.ToastError("File doesn't exist?");
            return;
        }

        SettingsFileUpdated?.Invoke(this,
            (possibleFile.FullName, Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    public static async Task<ProjectChooserContext> CreateInstance(StatusControlContext? statusContext,
        string recentSettingFiles)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();

        var recentFiles = recentSettingFiles?.Split("|").ToList() ?? new List<string>();

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryItems = new ObservableCollection<ProjectFileListItem>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = new ProjectChooserContext(factoryContext, factoryItems, recentFiles);

        await context.LoadData();

        return context;
    }

    public event EventHandler<(string userString, List<string> recentFiles)>? DirectoryUpdated;

    private async Task LaunchRecentFile(ProjectFileListItem? projectFileListItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (projectFileListItem == null)
        {
            StatusContext.ToastWarning("Nothing selected?");
            return;
        }

        projectFileListItem.SettingsFile.Refresh();

        if (!projectFileListItem.SettingsFile.Exists)
        {
            StatusContext.ToastWarning("File doesn't appear to currently exist...");
            return;
        }

        SettingsFileUpdated?.Invoke(this,
            (projectFileListItem.SettingsFile.FullName,
                Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Looking for recent files");

        foreach (var loopFiles in RecentSettingFilesNames)
        {
            if (string.IsNullOrWhiteSpace(loopFiles)) continue;

            var loopFileInfo = new FileInfo(loopFiles);

            if (!loopFileInfo.Exists) continue;

            try
            {
                StatusContext.Progress($"Recent Files - getting info from {loopFileInfo.FullName}");

                var readResult = await UserSettingsUtilities.ReadFromSettingsFile(
                    new FileInfo(loopFileInfo.FullName),
                    StatusContext.ProgressTracker());

                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Add(new ProjectFileListItem { ParsedSettings = readResult, SettingsFile = loopFileInfo });
                await ThreadSwitcher.ResumeBackgroundAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private async Task RemoveSelectedFile(ProjectFileListItem? projectFileListItem)
    {
        if (projectFileListItem == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Remove(projectFileListItem);
    }

    public event EventHandler<(string userString, List<string> recentFiles)>? SettingsFileUpdated;
}