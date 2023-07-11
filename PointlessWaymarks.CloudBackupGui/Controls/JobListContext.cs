using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class JobListContext
{
    public required string CurrentDatabase { get; set; }
    public bool CurrentDatabaseIsValid { get; set; }
    public required ObservableCollection<BackupJob> Items { get; set; }
    public BackupJob? SelectedFile { get; set; }
    public List<BackupJob> SelectedFiles { get; set; } = new();
    public required StatusControlContext StatusContext { get; set; }

    [BlockingCommand]
    public async Task ChooseCurrentDb()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var initialDirectoryString = CloudBackupGuiSettingTools.ReadSettings().LastDirectory;

        DirectoryInfo? initialDirectory = null;

        try
        {
            initialDirectory = new DirectoryInfo(initialDirectoryString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        StatusContext.Progress("Starting File Chooser");

        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { Filter = "db files (*.db)|*.db|All files (*.*)|*.*" };

        if (initialDirectory != null) filePicker.FileName = $"{initialDirectory.FullName}\\";

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Checking that file exists");

        var possibleFile = new FileInfo(filePicker.FileName);

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!possibleFile.Exists) return;

        var currentSettings = CloudBackupGuiSettingTools.ReadSettings();
        if (!string.IsNullOrWhiteSpace(possibleFile.Directory?.Parent?.FullName))
            currentSettings.LastDirectory = possibleFile.Directory?.Parent?.FullName;
        currentSettings.DatabaseFile = possibleFile.FullName;
        await CloudBackupGuiSettingTools.WriteSettings(currentSettings);
        CurrentDatabase = possibleFile.FullName;
    }

    public static async Task<JobListContext> CreateInstance(StatusControlContext? statusContext)
    {
        var factoryStatusContext = statusContext ?? new StatusControlContext();
        var settings = CloudBackupGuiSettingTools.ReadSettings();

        if (string.IsNullOrWhiteSpace(settings.DatabaseFile) || !File.Exists(settings.DatabaseFile))
        {
            var newDb = UniqueFileTools.UniqueFile(
                FileLocationHelpers.DefaultStorageDirectory(), "PointlessWaymarks-S3Backup.db");
            settings.DatabaseFile = newDb!.FullName;

            await CloudBackupContext.CreateInstanceWithEnsureCreated(newDb.FullName);

            await CloudBackupGuiSettingTools.WriteSettings(settings);
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var initialItems = new ObservableCollection<BackupJob>();

        var toReturn = new JobListContext
        {
            StatusContext = factoryStatusContext,
            Items = initialItems,
            CurrentDatabase = settings.DatabaseFile
        };

        await toReturn.Setup();

        await ThreadSwitcher.ResumeBackgroundAsync();

        await toReturn.UpdateDatabaseFile();

        return toReturn;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (e.PropertyName.Equals(nameof(CurrentDatabase)))
            StatusContext.RunFireAndForgetBlockingTask(UpdateDatabaseFile);
    }

    public Task Setup()
    {
        BuildCommands();
        PropertyChanged += OnPropertyChanged;
        return Task.CompletedTask;
    }

    public async Task UpdateDatabaseFile()
    {
        var dbCheck = await CloudBackupContext.TryCreateInstance(CurrentDatabase);

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        await ThreadSwitcher.ResumeBackgroundAsync();

        CurrentDatabaseIsValid = dbCheck.success;

        if (!dbCheck.success) return;

        var jobs = await dbCheck.context!.BackupJobs.ToListAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        jobs.ForEach(x => Items.Add(x));
    }
}