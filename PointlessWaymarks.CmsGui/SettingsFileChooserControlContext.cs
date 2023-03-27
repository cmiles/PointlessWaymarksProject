using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsGui;

public partial class SettingsFileChooserControlContext : ObservableObject
{
    [ObservableProperty] private RelayCommand _chooseFileCommand;
    [ObservableProperty] private RelayCommand<SettingsFileListItem> _chooseRecentFileCommand;
    [ObservableProperty] private ObservableCollection<SettingsFileListItem> _items;
    [ObservableProperty] private RelayCommand _newFileCommand;
    [ObservableProperty] private List<string> _recentSettingFilesNames;
    [ObservableProperty] private RelayCommand<SettingsFileListItem> _removeSelectedFileCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _userNewFileName;

    private SettingsFileChooserControlContext(StatusControlContext statusContext, List<string> recentSettingFiles)
    {
        _statusContext = statusContext;
        _recentSettingFilesNames = recentSettingFiles;

        _newFileCommand = StatusContext.RunNonBlockingTaskCommand(NewFile);
        _chooseFileCommand = StatusContext.RunNonBlockingTaskCommand(ChooseFile);
        _chooseRecentFileCommand = StatusContext.RunNonBlockingTaskCommand<SettingsFileListItem>(LaunchRecentFile);
        _removeSelectedFileCommand = StatusContext.RunNonBlockingTaskCommand<SettingsFileListItem>(RemoveSelectedFile);
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
            (false, possibleFile.FullName, Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    public static async Task<SettingsFileChooserControlContext> CreateInstance(StatusControlContext statusContext,
        string recentSettingFiles)
    {
        var context = new SettingsFileChooserControlContext(statusContext ?? new StatusControlContext(),
            recentSettingFiles?.Split("|").ToList() ?? new List<string>());

        await context.LoadData();

        return context;
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
        await ThreadSwitcher.ResumeForegroundAsync();

        Items ??= new ObservableCollection<SettingsFileListItem>();

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

        if (!FileAndFolderTools.IsValidWindowsFileSystemFilename(UserNewFileName))
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