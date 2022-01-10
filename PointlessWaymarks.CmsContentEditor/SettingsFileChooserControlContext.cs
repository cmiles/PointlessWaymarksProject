using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsContentEditor;

[ObservableObject]
public partial class SettingsFileChooserControlContext
{
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _userNewFileName;

    public SettingsFileChooserControlContext(StatusControlContext statusContext, string recentSettingFiles)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        RecentSettingFilesNames = recentSettingFiles?.Split("|").ToList() ?? new List<string>();

        NewFileCommand = StatusContext.RunNonBlockingTaskCommand(NewFile);
        ChooseFileCommand = StatusContext.RunNonBlockingTaskCommand(ChooseFile);
        ChooseRecentFileCommand = StatusContext.RunNonBlockingTaskCommand<SettingsFileListItem>(LaunchRecentFile);
        RemoveSelectedFileCommand = StatusContext.RunNonBlockingTaskCommand<SettingsFileListItem>(RemoveSelectedFile);

        StatusContext.RunFireAndForgetBlockingTask(LoadData);
    }

    public RelayCommand ChooseFileCommand { get; set; }

    public RelayCommand<SettingsFileListItem> ChooseRecentFileCommand { get; set; }

    public ObservableCollection<SettingsFileListItem> Items { get; } = new();

    public RelayCommand NewFileCommand { get; set; }

    public List<string> RecentSettingFilesNames { get; set; }

    public RelayCommand<SettingsFileListItem> RemoveSelectedFileCommand { get; set; }

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
            (false, possibleFile.FullName, Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    private async Task LaunchRecentFile(SettingsFileListItem settingsFileListItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        settingsFileListItem.SettingsFile.Refresh();

        if (!settingsFileListItem.SettingsFile.Exists)
        {
            StatusContext.ToastWarning("File doesn't appear to currently exist...");
            return;
        }

        SettingsFileUpdated?.Invoke(this,
            (false, settingsFileListItem.SettingsFile.FullName, Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    private async Task LoadData()
    {
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

                var readResult = await UserSettingsUtilities.ReadFromSettingsFile(new FileInfo(loopFileInfo.FullName),
                    StatusContext.ProgressTracker());

                await ThreadSwitcher.ResumeForegroundAsync();
                Items.Add(new SettingsFileListItem { ParsedSettings = readResult, SettingsFile = loopFileInfo });
                await ThreadSwitcher.ResumeBackgroundAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private async Task NewFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(UserNewFileName))
        {
            StatusContext.ToastError("Please fill in a file name...");
            return;
        }

        UserNewFileName = UserNewFileName.Trim();

        if (!FolderFileUtility.IsValidWindowsFileSystemFilename(UserNewFileName))
        {
            StatusContext.ToastError("File name is not valid - avoid special characters...");
            return;
        }

        StatusContext.Progress($"New File Selected - {UserNewFileName}");

        SettingsFileUpdated?.Invoke(this, (true, UserNewFileName, Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    private async Task RemoveSelectedFile(SettingsFileListItem settingsFileListItem)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Remove(settingsFileListItem);
    }

    public event EventHandler<(bool isNew, string userString, List<string> recentFiles)> SettingsFileUpdated;
}