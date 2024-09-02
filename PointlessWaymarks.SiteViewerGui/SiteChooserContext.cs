using System.Collections.ObjectModel;
using System.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.SiteViewerGui;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class SiteChooserContext
{
    private SiteChooserContext(StatusControlContext statusContext, ObservableCollection<object> items,
        List<string> recentFiles)
    {
        StatusContext = statusContext;

        BuildCommands();

        RecentSiteStrings = recentFiles;
        Items = items;
    }

    public ObservableCollection<object> Items { get; set; }
    public List<string> RecentSiteStrings { get; set; }
    public StatusControlContext StatusContext { get; set; }

    [BlockingCommand]
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
            await StatusContext.ToastError("Directory doesn't exist?");
            return;
        }

        SiteDirectoryChosen?.Invoke(this,
            (possibleDirectory.FullName, StringsFromItems()));
    }

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

        SiteSettingsFileChosen?.Invoke(this,
            (possibleFile.FullName, StringsFromItems()));
    }

    public static async Task<SiteChooserContext> CreateInstance(StatusControlContext? statusContext,
        string recentSettingFiles)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var recentFiles = recentSettingFiles.Split("|").ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryStatusContext = await StatusControlContext.CreateInstance(statusContext);

        var factoryItems = new ObservableCollection<object>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = new SiteChooserContext(factoryStatusContext, factoryItems, recentFiles);

        await context.LoadData();

        return context;
    }

    [NonBlockingCommand]
    private async Task LaunchRecentDirectory(SiteDirectoryListItem? projectDirectoryListItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (projectDirectoryListItem == null)
        {
            await StatusContext.ToastWarning("Nothing selected?");
            return;
        }

        projectDirectoryListItem.SiteDirectory.Refresh();

        if (!projectDirectoryListItem.SiteDirectory.Exists)
        {
            await StatusContext.ToastWarning("Directory doesn't appear to currently exist...");
            return;
        }

        SiteDirectoryChosen?.Invoke(this,
            (projectDirectoryListItem.SiteDirectory.FullName,
                StringsFromItems()));
    }

    [NonBlockingCommand]
    private async Task LaunchRecentFile(SiteSettingsFileListItem? projectFileListItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (projectFileListItem == null)
        {
            await StatusContext.ToastWarning("Nothing selected?");
            return;
        }

        projectFileListItem.SettingsFile.Refresh();

        if (!projectFileListItem.SettingsFile.Exists)
        {
            await StatusContext.ToastWarning("File doesn't appear to currently exist...");
            return;
        }

        SiteSettingsFileChosen?.Invoke(this,
            (projectFileListItem.SettingsFile.FullName,
                StringsFromItems()));
    }

    private async Task LoadData()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Looking for recent files");

        foreach (var loopRecent in RecentSiteStrings)
        {
            if (string.IsNullOrWhiteSpace(loopRecent)) continue;

            if (loopRecent.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
            {
                var loopFileInfo = new FileInfo(loopRecent);

                if (loopFileInfo is not { Exists: true }) continue;

                try
                {
                    StatusContext.Progress($"Recent Files - getting info from {loopFileInfo.FullName}");

                    var readResult = await UserSettingsUtilities.ReadFromSettingsFile(
                        new FileInfo(loopFileInfo.FullName),
                        StatusContext.ProgressTracker());

                    await ThreadSwitcher.ResumeForegroundAsync();
                    Items.Add(new SiteSettingsFileListItem
                        { ParsedSettings = readResult, SettingsFile = loopFileInfo });
                    await ThreadSwitcher.ResumeBackgroundAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                var loopDirectoryInfo = new DirectoryInfo(loopRecent);

                if (loopDirectoryInfo is not { Exists: true }) continue;

                try
                {
                    StatusContext.Progress($"Recent Directories - {loopDirectoryInfo.FullName}");

                    await ThreadSwitcher.ResumeForegroundAsync();
                    Items.Add(new SiteDirectoryListItem { SiteDirectory = loopDirectoryInfo });
                    await ThreadSwitcher.ResumeBackgroundAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }

    [NonBlockingCommand]
    private async Task RemoveSelectedDirectory(SiteDirectoryListItem? projectDirectoryListItem)
    {
        if (projectDirectoryListItem == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Remove(projectDirectoryListItem);
    }

    [NonBlockingCommand]
    private async Task RemoveSelectedFile(SiteSettingsFileListItem? projectFileListItem)
    {
        if (projectFileListItem == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Remove(projectFileListItem);
    }

    public event EventHandler<(string userString, List<string> recentFiles)>? SiteDirectoryChosen;

    public event EventHandler<(string userString, List<string> recentFiles)>? SiteSettingsFileChosen;

    public List<string> StringsFromItems()
    {
        return Items.Select(x =>
        {
            switch (x)
            {
                case SiteSettingsFileListItem asFile:
                    return asFile.SettingsFile.FullName;
                case SiteDirectoryListItem asDirectory:
                    return asDirectory.SiteDirectory.FullName;
                default:
                    return string.Empty;
            }
        }).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }
}