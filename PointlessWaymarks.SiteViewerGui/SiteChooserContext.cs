﻿using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.SiteViewerGui;

public partial class SiteChooserContext : ObservableObject
{
    [ObservableProperty] private RelayCommand _chooseDirectoryCommand;
    [ObservableProperty] private RelayCommand<SiteDirectoryListItem> _chooseRecentDirectoryCommand;
    [ObservableProperty] private RelayCommand<SiteSettingsFileListItem> _chooseRecentSettingsFileCommand;
    [ObservableProperty] private RelayCommand _chooseSettingsFileCommand;
    [ObservableProperty] private ObservableCollection<object> _items;
    [ObservableProperty] private List<string> _recentSiteStrings;
    [ObservableProperty] private RelayCommand<SiteDirectoryListItem> _removeSelectedDirectoryCommand;
    [ObservableProperty] private RelayCommand<SiteSettingsFileListItem> _removeSelectedFileCommand;
    [ObservableProperty] private StatusControlContext _statusContext;

    private SiteChooserContext(StatusControlContext statusContext, ObservableCollection<object> items,
        List<string> recentFiles)
    {
        _statusContext = statusContext;
        _chooseSettingsFileCommand = StatusContext.RunNonBlockingTaskCommand(ChooseFile);
        _chooseDirectoryCommand = StatusContext.RunBlockingTaskCommand(ChooseDirectory);
        _chooseRecentSettingsFileCommand =
            StatusContext.RunNonBlockingTaskCommand<SiteSettingsFileListItem>(LaunchRecentFile);
        _chooseRecentDirectoryCommand =
            StatusContext.RunNonBlockingTaskCommand<SiteDirectoryListItem>(LaunchRecentDirectory);
        _removeSelectedFileCommand =
            StatusContext.RunNonBlockingTaskCommand<SiteSettingsFileListItem>(RemoveSelectedFile);
        _removeSelectedDirectoryCommand =
            StatusContext.RunNonBlockingTaskCommand<SiteDirectoryListItem>(RemoveSelectedDirectory);
        _recentSiteStrings = recentFiles;
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

        SiteDirectoryChosen?.Invoke(this,
            (possibleDirectory.FullName, StringsFromItems()));
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

        SiteSettingsFileChosen?.Invoke(this,
            (possibleFile.FullName, StringsFromItems()));
    }

    public static async Task<SiteChooserContext> CreateInstance(StatusControlContext? statusContext,
        string recentSettingFiles)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();

        var recentFiles = recentSettingFiles.Split("|").ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryItems = new ObservableCollection<object>();

        await ThreadSwitcher.ResumeBackgroundAsync();

        var context = new SiteChooserContext(factoryContext, factoryItems, recentFiles);

        await context.LoadData();

        return context;
    }

    private async Task LaunchRecentDirectory(SiteDirectoryListItem? projectDirectoryListItem)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (projectDirectoryListItem == null)
        {
            StatusContext.ToastWarning("Nothing selected?");
            return;
        }

        projectDirectoryListItem.SiteDirectory.Refresh();

        if (!projectDirectoryListItem.SiteDirectory.Exists)
        {
            StatusContext.ToastWarning("Directory doesn't appear to currently exist...");
            return;
        }

        SiteDirectoryChosen?.Invoke(this,
            (projectDirectoryListItem.SiteDirectory.FullName,
                StringsFromItems()));
    }

    private async Task LaunchRecentFile(SiteSettingsFileListItem? projectFileListItem)
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


    private async Task RemoveSelectedDirectory(SiteDirectoryListItem? projectDirectoryListItem)
    {
        if (projectDirectoryListItem == null) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Remove(projectDirectoryListItem);
    }

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