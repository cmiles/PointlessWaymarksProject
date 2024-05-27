using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CloudBackupData;
using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CloudBackupData.Reports;
using PointlessWaymarks.CloudBackupRunner;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class JobListContext
{
    public required string CurrentDatabase { get; set; }
    public bool CurrentDatabaseIsValid { get; set; }
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<JobListListItem> Items { get; set; }
    public JobListListItem? SelectedJob { get; set; }
    public List<JobListListItem> SelectedJobs { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }
    
    [NonBlockingCommand]
    public async Task BasicCommandLineCommandToClipboard(BackupJob? listItem)
    {
        if (listItem is null) return;
        
        var settings = CloudBackupGuiSettingTools.ReadSettings();
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        Clipboard.SetText($""".\PointlessWaymarks.CloudBackupRunner.exe "{settings.DatabaseFile}" {listItem.Id}""");
        
        StatusContext.ToastSuccess("Command Line Command on Clipboard");
    }
    
    [BlockingCommand]
    public async Task ChooseCurrentDb()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var initialDirectoryString = CloudBackupGuiSettingTools.ReadSettings().LastDirectory;
        
        DirectoryInfo? initialDirectory = null;
        
        try
        {
            if (!string.IsNullOrWhiteSpace(initialDirectoryString))
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
    
    [BlockingCommand]
    public async Task CloudCacheFilesReport(BackupJob? toEdit)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (toEdit == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Edit?");
            return;
        }
        
        var file = await CloudCacheFilesToExcel.Run(toEdit.Id, StatusContext.ProgressTracker());
        
        StatusContext.Progress($"Opening Excel File {file}");
        
        ProcessTools.Open(file);
    }
    
    public static async Task<JobListContext> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var factoryStatusContext = statusContext;
        var settings = CloudBackupGuiSettingTools.ReadSettings();
        
        if (string.IsNullOrWhiteSpace(settings.DatabaseFile) || !File.Exists(settings.DatabaseFile))
        {
            var newDb = UniqueFileTools.UniqueFile(
                FileLocationHelpers.DefaultStorageDirectory(), "PointlessWaymarks-CloudBackup.db");
            settings.DatabaseFile = newDb!.FullName;
            
            await CloudBackupContext.CreateInstanceWithEnsureCreated(newDb.FullName);
            
            await CloudBackupGuiSettingTools.WriteSettings(settings);
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var initialItems = new ObservableCollection<JobListListItem>();
        
        var toReturn = new JobListContext
        {
            StatusContext = factoryStatusContext,
            Items = initialItems,
            CurrentDatabase = settings.DatabaseFile
        };
        
        await toReturn.Setup();
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        try
        {
            await toReturn.UpdateDatabaseFile();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in Job List Startup with Database File {databaseFile}", settings.DatabaseFile);
            await statusContext.ShowMessageWithOkButton("Database Load Error",
                $"There was an error loading the database:{Environment.NewLine}{Environment.NewLine}{e.Message}");
        }
        
        return toReturn;
    }
    
    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);
        
        var toRun = translatedMessage.Match(ProcessDataUpdateNotification,
            ProcessProgressNotification,
            x =>
            {
                StatusContext.ToastError(x.ErrorMessage);
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}", x.ErrorMessage,
                    StatusContext.StatusControlContextId);
                return Task.CompletedTask;
            }
        );
        
        if (toRun is not null) await toRun;
    }
    
    [BlockingCommand]
    public async Task DeleteJob(BackupJob? toDelete)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (toDelete == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Delete?");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        if (MessageBox.Show(
                "Deleting a Backup Job will NOT delete any files or directories - but it will delete all records associated with this backup job! Continue??",
                "Delete Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
            return;
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        PasswordVaultTools.RemoveCredentials(toDelete.VaultS3CredentialsIdentifier);
        
        var db = await CloudBackupContext.CreateInstance();
        var currentItem = await db.BackupJobs.SingleAsync(x => x.Id == toDelete.Id);
        
        db.Remove(currentItem);
        await db.SaveChangesAsync();
        
        await RefreshList();
    }
    
    [NonBlockingCommand]
    public async Task EditJob(BackupJob? toEdit)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (toEdit == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Edit?");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var window = await JobEditorWindow.CreateInstance(toEdit, CurrentDatabase);
        window.PositionWindowAndShow();
    }
    
    
    [NonBlockingCommand]
    public async Task EditSelectedJob()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (SelectedJob?.DbJob == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Edit?");
            return;
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var window = await JobEditorWindow.CreateInstance(SelectedJob.DbJob, CurrentDatabase);
        window.PositionWindowAndShow();
    }
    
    [BlockingCommand]
    public async Task IncludedAndExcludedFilesReport(BackupJob? toEdit)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (toEdit == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Edit?");
            return;
        }
        
        var file = await IncludedAndExcludedFilesToExcel.Run(toEdit.Id, StatusContext.ProgressTracker());
        
        StatusContext.Progress($"Opening Excel File {file.FullName}");
        
        ProcessTools.Open(file.FullName);
    }
    
    [NonBlockingCommand]
    public async Task NewBatchWindow(BackupJob? listItem)
    {
        if (listItem is null) return;
        await ThreadSwitcher.ResumeForegroundAsync();
        
        await BatchListWindow.CreateInstanceAndShow(listItem.Id, listItem.Name);
    }
    
    [BlockingCommand]
    public async Task NewDatabase()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var initialDirectoryString = CloudBackupGuiSettingTools.ReadSettings().LastDirectory;
        
        DirectoryInfo? initialDirectory = null;
        
        try
        {
            if (!string.IsNullOrWhiteSpace(initialDirectoryString))
                initialDirectory = new DirectoryInfo(initialDirectoryString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        StatusContext.Progress("Starting File Chooser");
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var userFilePicker = new VistaSaveFileDialog { OverwritePrompt = true, CheckPathExists = true, Filter = "db files (*.db)|*.db|All files (*.*)|*.*" };
        
        if (initialDirectory != null) userFilePicker.FileName = $"{initialDirectory.FullName}\\";
        
        if (!userFilePicker.ShowDialog() ?? false) return;
        
        var userChoice = userFilePicker.FileName;
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        if (string.IsNullOrWhiteSpace(userChoice))
        {
            StatusContext.ToastWarning("No File Selected? New Db Cancelled...");
            return;
        }
        
        if(!Path.HasExtension(userChoice)) userChoice += ".db";

        var userDatabaseFile = new FileInfo(userChoice);
        
        if (!userDatabaseFile.Directory?.Exists ?? false)
        {
            StatusContext.ToastError("Directory for New Database Doesn't Exist?");
            return;
        }
        
        var result = await CloudBackupContext.TryCreateInstance(userDatabaseFile.FullName, false, true);
        
        if (!result.success)
        {
            StatusContext.ToastError($"Trouble Creating New Database - {result.message}");
            return;
        }
        
        var currentSettings = CloudBackupGuiSettingTools.ReadSettings();
        if (!string.IsNullOrWhiteSpace(userDatabaseFile.Directory?.Parent?.FullName))
            currentSettings.LastDirectory = userDatabaseFile.Directory?.Parent?.FullName;
        currentSettings.DatabaseFile = userDatabaseFile.FullName;
        await CloudBackupGuiSettingTools.WriteSettings(currentSettings);
        CurrentDatabase = userDatabaseFile.FullName;
    }
    
    [NonBlockingCommand]
    public async Task NewJob()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var newJob = new BackupJob
        {
            CreatedOn = DateTime.Now,
            Name = "New Backup Job",
            MaximumRunTimeInHours = 6,
            PersistentId = Guid.NewGuid()
        };
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var window = await JobEditorWindow.CreateInstance(newJob, CurrentDatabase);
        window.PositionWindowAndShow();
    }
    
    [NonBlockingCommand]
    public async Task NewProgressWindow(BackupJob? listItem)
    {
        if (listItem is null) return;
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var window = await ProgressTrackerWindow.CreateInstance(listItem.PersistentId, listItem.Name);
        window.PositionWindowAndShow();
    }
    
    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
    }
    
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (e.PropertyName.Equals(nameof(CurrentDatabase)))
            StatusContext.RunFireAndForgetBlockingTask(UpdateDatabaseFile);
    }
    
    private async Task ProcessDataUpdateNotification(InterProcessDataNotification interProcessUpdateNotification)
    {
        if (interProcessUpdateNotification is
            { ContentType: DataNotificationContentType.BackupJob, UpdateType: DataNotificationUpdateType.Delete })
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            
            var toRemove = Items.Where(x => x.PersistentId == interProcessUpdateNotification.JobPersistentId).ToList();
            toRemove.ForEach(x => Items.Remove(x));
            return;
        }
        
        if (interProcessUpdateNotification is
            {
                ContentType: DataNotificationContentType.BackupJob, UpdateType: DataNotificationUpdateType.Update
                or DataNotificationUpdateType.New
            })
        {
            var listItem = Items.SingleOrDefault(x => x.PersistentId == interProcessUpdateNotification.JobPersistentId);
            var db = await CloudBackupContext.CreateInstance();
            var dbItem =
                db.BackupJobs.SingleOrDefault(x => x.PersistentId == interProcessUpdateNotification.JobPersistentId);
            
            if (dbItem == null) return;
            
            if (listItem != null)
            {
                listItem.DbJob = dbItem;
                return;
            }
            
            var toAdd = await JobListListItem.CreateInstance(dbItem);
            
            await ThreadSwitcher.ResumeForegroundAsync();
            
            Items.Add(toAdd);
        }
        
        if (interProcessUpdateNotification is { ContentType: DataNotificationContentType.CloudTransferBatch })
        {
            var listItem = Items.SingleOrDefault(x => x.PersistentId == interProcessUpdateNotification.JobPersistentId);
            if (listItem is not null) await listItem.RefreshLatestBatch();
        }
    }
    
    private async Task ProcessProgressNotification(InterProcessProgressNotification arg)
    {
        var possibleListItem = Items.SingleOrDefault(x => x.PersistentId == arg.JobPersistentId);
        if (possibleListItem == null) return;
        
        possibleListItem.ProgressString = arg.ProgressMessage;
        possibleListItem.ProgressProcess = arg.ProcessId;
        
        if (arg.BatchId == possibleListItem.LatestBatch?.BatchId) await possibleListItem.RefreshLatestBatchStatistics();
    }
    
    [BlockingCommand]
    public async Task RefreshList()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;
        
        var db = await CloudBackupContext.CreateInstance();
        
        var jobs = await db.BackupJobs.ToListAsync();
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        Items.Clear();
        
        foreach (var x in jobs) Items.Add(await JobListListItem.CreateInstance(x));
        
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }
    
    [NonBlockingCommand]
    public async Task RunJob(BackupJob? toRun)
    {
        if (toRun == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Run?");
            return;
        }
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        DataNotifications.PublishProgressNotification("CloudBackupGui", 0, "Starting Backup Runner", toRun.PersistentId,
            null);
        
        StatusContext.RunFireAndForgetNonBlockingTask(async () =>
            await Program.Main([CloudBackupContext.CurrentDatabaseFileName, toRun.Id.ToString(), "auto"]));
        
        StatusContext.ToastSuccess("Starting Backup Runner...");
    }
    
    [NonBlockingCommand]
    public async Task RunJobForceCloudCacheRescan(BackupJob? toRun)
    {
        if (toRun == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Run?");
            return;
        }
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        DataNotifications.PublishProgressNotification("CloudBackupGui", 0, "Starting Backup Runner", toRun.PersistentId,
            null);
        
        StatusContext.RunFireAndForgetNonBlockingTask(async () =>
            await Program.Main([CloudBackupContext.CurrentDatabaseFileName, toRun.Id.ToString(), "new"]));
        
        StatusContext.ToastSuccess("Starting Backup Runner...");
    }
    
    public Task Setup()
    {
        BuildCommands();
        PropertyChanged += OnPropertyChanged;
        DataNotificationsProcessor = new DataNotificationsWorkQueue { Processor = DataNotificationReceived };
        
        return Task.CompletedTask;
    }
    
    public async Task UpdateDatabaseFile()
    {
        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;
        
        var dbCheck = await CloudBackupContext.TryCreateInstance(CurrentDatabase);
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        Items.Clear();
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        CurrentDatabaseIsValid = dbCheck.success;
        
        if (!dbCheck.success) return;
        
        var jobs = await dbCheck.context!.BackupJobs.ToListAsync();
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        foreach (var x in jobs)
            try
            {
                Items.Add(await JobListListItem.CreateInstance(x));
            }
            catch (Exception e)
            {
                Log.Error(e, $"Trouble Adding Job Items from {CurrentDatabase}");
                StatusContext.ToastError("Trouble Adding Job...");
            }
        
        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }
}