using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ScriptJobListContext
{
    public required string DatabaseFile { get; set; }
    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<ScriptJobListListItem> Items { get; set; }
    public ScriptJobListListItem? SelectedItem { get; set; }
    public List<ScriptJobListListItem> SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }

    [NonBlockingCommand]
    public async Task DiffRunOutput(ScriptJobListListItem? toEdit)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toEdit == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var db = await PowerShellRunnerDbContext.CreateInstance();
        var topRun = db.ScriptJobRuns.Where(x => x.ScriptJobId == toEdit.DbEntry.Id)
            .OrderByDescending(x => x.CompletedOnUtc)
            .FirstOrDefault();

        if (topRun == null)
        {
            StatusContext.ToastWarning("No Runs to Compare?");
            return;
        }

        await ScriptJobRunOutputDiffWindow.CreateInstance(topRun.Id, DatabaseFile);
    }

    [NonBlockingCommand]
    public async Task ShowLastJobRun(ScriptJobListListItem? toEdit)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toEdit == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var db = await PowerShellRunnerDbContext.CreateInstance();
        var topRun = db.ScriptJobRuns.Where(x => x.ScriptJobId == toEdit.DbEntry.Id)
            .OrderByDescending(x => x.CompletedOnUtc)
            .FirstOrDefault();

        if (topRun == null)
        {
            StatusContext.ToastWarning("No Runs to Compare?");
            return;
        }

        await ScriptJobRunViewerWindow.CreateInstance(topRun.Id, DatabaseFile);
    }

    public static async Task<ScriptJobListContext> CreateInstance(StatusControlContext? statusContext,
        string currentDatabase)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newContext = new ScriptJobListContext
        {
            StatusContext = statusContext ?? new StatusControlContext(),
            Items = [],
            DatabaseFile = currentDatabase
        };

        await ThreadSwitcher.ResumeBackgroundAsync();

        newContext.BuildCommands();
        await newContext.RefreshList();

        newContext.DataNotificationsProcessor = new DataNotificationsWorkQueue
            { Processor = newContext.DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += newContext.OnDataNotificationReceived;

        return newContext;
    }

    private async Task DataNotificationReceived(TinyMessageReceivedEventArgs eventArgs)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var translatedMessage = DataNotifications.TranslateDataNotification(eventArgs.Message);

        var toRun = translatedMessage.Match(ProcessDataUpdateNotification,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            x =>
            {
                StatusContext.ToastError(x.ErrorMessage);
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context Id {1}",
                    x.ErrorMessage,
                    StatusContext.StatusControlContextId);
                return Task.CompletedTask;
            }
        );

        if (toRun is not null) await toRun;
    }

    [BlockingCommand]
    public async Task DeleteJob(ScriptJobListListItem? toDelete)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toDelete == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Delete?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        if (MessageBox.Show(
                "Deleting a Script Job is permanent - Continue?",
                "Delete Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
            return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance();
        var currentItem = await db.ScriptJobs.SingleAsync(x => x.Id == toDelete.DbEntry.Id);
        var currentId = currentItem.Id;
        var currentRuns = await db.ScriptJobRuns.Where(x => x.ScriptJobId == currentItem.Id).ExecuteDeleteAsync();

        db.ScriptJobs.Remove(currentItem);
        await db.SaveChangesAsync();

        DataNotifications.PublishDataNotification("Script Job List",
            DataNotifications.DataNotificationContentType.ScriptJob,
            DataNotifications.DataNotificationUpdateType.Delete, currentId);
    }

    [NonBlockingCommand]
    public async Task EditJob(ScriptJobListListItem? toEdit)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toEdit == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Edit?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await ScriptJobEditorWindow.CreateInstance(toEdit.DbEntry, DatabaseFile);
        window.PositionWindowAndShow();
    }


    [NonBlockingCommand]
    public async Task EditSelectedJob()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItem == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Edit?");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await ScriptJobEditorWindow.CreateInstance(SelectedItem.DbEntry, DatabaseFile);
        window.PositionWindowAndShow();
    }

    [NonBlockingCommand]
    public async Task NewJob()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var newJob = new ScriptJob()
        {
            Name = "New Script Job",
            LastEditOn = DateTime.Now
        };

        await ThreadSwitcher.ResumeForegroundAsync();

        var window = await ScriptJobEditorWindow.CreateInstance(newJob, DatabaseFile);
        window.PositionWindowAndShow();
    }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
    }

    private async Task ProcessDataUpdateNotification(
        DataNotifications.InterProcessDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification is
            {
                ContentType: DataNotifications.DataNotificationContentType.ScriptJob,
                UpdateType: DataNotifications.DataNotificationUpdateType.Delete
            })

        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var toRemove = Items.Where(x => x.DbEntry.Id == interProcessUpdateNotification.Id)
                .ToList();
            toRemove.ForEach(x => Items.Remove(x));
            return;
        }

        if (interProcessUpdateNotification is
            {
                ContentType: DataNotifications.DataNotificationContentType.ScriptJob,
                UpdateType: DataNotifications.DataNotificationUpdateType.Update
                or DataNotifications.DataNotificationUpdateType.New
            })
        {
            var listItem =
                Items.SingleOrDefault(x => x.DbEntry.Id == interProcessUpdateNotification.Id);
            var db = await PowerShellRunnerDbContext.CreateInstance();
            var dbItem =
                await db.ScriptJobs.SingleOrDefaultAsync(x =>
                    x.Id == interProcessUpdateNotification.Id);

            if (dbItem == null) return;

            if (listItem != null)
            {
                listItem.DbEntry = dbItem;
                return;
            }

            var toAdd = await ScriptJobListListItem.CreateInstance(dbItem);

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Add(toAdd);
        }
    }

    [BlockingCommand]
    public async Task RefreshList()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        DataNotifications.NewDataNotificationChannel().MessageReceived -= OnDataNotificationReceived;

        var db = await PowerShellRunnerDbContext.CreateInstance();

        var jobs = await db.ScriptJobs.ToListAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        foreach (var x in jobs) Items.Add(await ScriptJobListListItem.CreateInstance(x));

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;
    }

    [NonBlockingCommand]
    public async Task RunSelectedJob()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItem == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Run?");
            return;
        }

        await PowerShellRun.ExecuteJob(SelectedItem.DbEntry.Id, DatabaseFile, "Run From PowerShell Runner Gui");
    }
}