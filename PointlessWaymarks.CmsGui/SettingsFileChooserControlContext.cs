using System.Collections.ObjectModel;
using System.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsGui;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class SettingsFileChooserControlContext
{
    private SettingsFileChooserControlContext(StatusControlContext statusContext, List<string> recentSettingFiles)
    {
        StatusContext = statusContext;
        RecentSettingFilesNames = recentSettingFiles;

        BuildCommands();
    }

    public ObservableCollection<SettingsFileListItem> Items { get; set; }
    public List<string> RecentSettingFilesNames { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string UserNewFileName { get; set; }

    [NonBlockingCommand]
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
            await StatusContext.ToastError("File doesn't exist?");
            return;
        }

        SettingsFileUpdated?.Invoke(this,
            (false, possibleFile.FullName, Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    public static async Task<SettingsFileChooserControlContext> CreateInstance(StatusControlContext statusContext,
        string recentSettingFiles)
    {
        var context = new SettingsFileChooserControlContext(statusContext ?? new StatusControlContext(),
            recentSettingFiles?.Split("|").ToList() ?? []);

        await context.LoadData();

        return context;
    }

    [NonBlockingCommand]
    private async Task LaunchRecentFile(SettingsFileListItem settingsFileListItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        settingsFileListItem.SettingsFile.Refresh();

        if (!settingsFileListItem.SettingsFile.Exists)
        {
            await StatusContext.ToastWarning("File doesn't appear to currently exist...");
            return;
        }

        SettingsFileUpdated?.Invoke(this,
            (false, settingsFileListItem.SettingsFile.FullName, Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items ??= [];

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

    [NonBlockingCommand]
    private async Task NewFile()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(UserNewFileName))
        {
            await StatusContext.ToastError("Please fill in a file name...");
            return;
        }

        UserNewFileName = UserNewFileName.Trim();

        if (!FileAndFolderTools.IsValidWindowsFileSystemFilename(UserNewFileName))
        {
            await StatusContext.ToastError("File name is not valid - avoid special characters...");
            return;
        }

        StatusContext.Progress($"New File Selected - {UserNewFileName}");

        SettingsFileUpdated?.Invoke(this, (true, UserNewFileName, Items.Select(x => x.SettingsFile.FullName).ToList()));
    }

    [NonBlockingCommand]
    private async Task RemoveSelectedFile(SettingsFileListItem settingsFileListItem)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Remove(settingsFileListItem);
    }

    public event EventHandler<(bool isNew, string userString, List<string> recentFiles)> SettingsFileUpdated;
}