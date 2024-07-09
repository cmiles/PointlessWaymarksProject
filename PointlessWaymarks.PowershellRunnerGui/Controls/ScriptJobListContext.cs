using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CronExpressionDescriptor;
using Cronos;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ColumnSort;
using PointlessWaymarks.WpfCommon.Status;
using Serilog;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ScriptJobListContext
{
    private readonly PeriodicTimer _cronNextTimer = new(TimeSpan.FromSeconds(30));
    private string _databaseFile = string.Empty;
    private Guid _dbId = Guid.Empty;

    public ScriptJobListContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public required string DatabaseFile { get; set; }
    public NotificationCatcher? DataNotificationsProcessor { get; set; }
    public required ObservableCollection<ScriptJobListListItem> Items { get; set; }
    public required ColumnSortControlContext ListSort { get; set; }
    public ScriptJobListListItem? SelectedItem { get; set; }
    public List<ScriptJobListListItem> SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }

    public string? UserFilterText { get; set; }

    public static async Task<ScriptJobListContext> CreateInstance(StatusControlContext? statusContext,
        string databaseFile)
    {
        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryContext = new ScriptJobListContext
        {
            StatusContext = statusContext ?? new StatusControlContext(),
            Items = [],
            DatabaseFile = databaseFile,
            ListSort = new ColumnSortControlContext
            {
                Items =
                [
                    new ColumnSortControlSortItem
                    {
                        DisplayName = "Name",
                        ColumnName = "DbEntry.Name",
                        Order = 1,
                        DefaultSortDirection = ListSortDirection.Ascending
                    },

                    new ColumnSortControlSortItem
                    {
                        DisplayName = "Next Run",
                        ColumnName = "NextRun",
                        DefaultSortDirection = ListSortDirection.Descending
                    }
                ]
            },
            _databaseFile = databaseFile,
            _dbId = dbId
        };

        await ThreadSwitcher.ResumeBackgroundAsync();

        factoryContext.BuildCommands();
        await factoryContext.RefreshList();

        factoryContext.DataNotificationsProcessor = new NotificationCatcher()
        {
            JobDataNotification = factoryContext.ProcessJobDataUpdateNotification
        };

        factoryContext.UpdateCronExpressionInformation();

        factoryContext.UpdateCronNextRun();

        await ListContextSortHelpers.SortList(
            factoryContext.ListSort.SortDescriptions(), factoryContext.Items);

        factoryContext.ListSort.SortUpdated += (_, list) =>
            factoryContext.StatusContext.RunFireAndForgetNonBlockingTask(() =>
                ListContextSortHelpers.SortList(list, factoryContext.Items));

        return factoryContext;
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

        if ((await StatusContext.ShowMessageWithYesNoButton($"Delete Confirmation - Job: {toDelete.DbEntry.Name}",
                $"Deletes are permanent - this program does have a 'recycle bin' or save any information about a Deleted Job (all job run information will also be deleted!) - are you sure you want to delete {toDelete.DbEntry.Name}?"))
            .Equals("no", StringComparison.OrdinalIgnoreCase)) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var currentItem = await db.ScriptJobs.SingleAsync(x => x.PersistentId == toDelete.DbEntry.PersistentId);
        var currentPersistentId = currentItem.PersistentId;

        var currentRuns = await db.ScriptJobRuns.Where(x => x.ScriptJobPersistentId == currentItem.PersistentId)
            .ToListAsync();
        db.ScriptJobRuns.RemoveRange(currentRuns);
        await db.SaveChangesAsync();

        Log.ForContext("JobPersistentId", currentItem.PersistentId)
            .ForContext(nameof(currentRuns), currentRuns.SafeObjectDump()).Information(
                "Deleting {0} ScriptJobRuns from '{1}' as part of Deleting the Job List GUI.", currentRuns.Count,
                currentItem.Name);

        foreach (var loopScriptJobs in currentRuns)
            DataNotifications.PublishRunDataNotification("Script Job Run List",
                DataNotifications.DataNotificationUpdateType.Delete, _dbId, loopScriptJobs.ScriptJobPersistentId,
                loopScriptJobs.PersistentId);

        db.ScriptJobs.Remove(currentItem);
        await db.SaveChangesAsync();

        Log.Information("Deleted Script Job {Name} - {PersistentId}", currentItem.Name, currentPersistentId);

        DataNotifications.PublishJobDataNotification("Script Job List",
            DataNotifications.DataNotificationUpdateType.Delete, _dbId, currentPersistentId);
    }

    [NonBlockingCommand]
    public async Task DiffLatestRuns(ScriptJobListListItem? toEdit)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toEdit == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var topRun = db.ScriptJobRuns.Where(x => x.ScriptJobPersistentId == toEdit.DbEntry.PersistentId)
            .OrderByDescending(x => x.CompletedOnUtc)
            .FirstOrDefault();

        if (topRun == null)
        {
            StatusContext.ToastWarning("No Runs to Compare?");
            return;
        }

        await ScriptJobRunOutputDiffWindow.CreateInstance(topRun.PersistentId, null, DatabaseFile);
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

        await ScriptJobEditorWindow.CreateInstance(toEdit.DbEntry, DatabaseFile);
    }

    private async Task FilterList()
    {
        if (!Items.Any()) return;

        await ThreadSwitcher.ResumeForegroundAsync();

        if (string.IsNullOrWhiteSpace(UserFilterText))
        {
            ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = _ => true;
            return;
        }

        var cleanedFilterText = UserFilterText.Trim();

        ((CollectionView)CollectionViewSource.GetDefaultView(Items)).Filter = o =>
        {
            if (o is not ScriptJobListListItem toFilter) return false;

            return toFilter.DbEntry.Name.Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase)
                   || toFilter.DbEntry.PersistentId.ToString()
                       .Contains(cleanedFilterText, StringComparison.OrdinalIgnoreCase);
        };
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

        await ScriptJobEditorWindow.CreateInstance(newJob, DatabaseFile);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (e.PropertyName.Equals(nameof(UserFilterText)))
            StatusContext.RunFireAndForgetNonBlockingTask(FilterList);
    }

    private async Task ProcessJobDataUpdateNotification(
        DataNotifications.InterProcessJobDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId) return;

        if (interProcessUpdateNotification is
            {
                UpdateType: DataNotifications.DataNotificationUpdateType.Delete
            })

        {
            await ThreadSwitcher.ResumeForegroundAsync();

            var toRemove = Items.Where(x => x.DbEntry.PersistentId == interProcessUpdateNotification.JobPersistentId)
                .ToList();
            toRemove.ForEach(x => Items.Remove(x));
            return;
        }

        if (interProcessUpdateNotification is
            {
                UpdateType: DataNotifications.DataNotificationUpdateType.Update
                or DataNotifications.DataNotificationUpdateType.New
            })
        {
            var listItem =
                Items.SingleOrDefault(x => x.DbEntry.PersistentId == interProcessUpdateNotification.JobPersistentId);
            var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
            var dbItem =
                await db.ScriptJobs.SingleOrDefaultAsync(x =>
                    x.PersistentId == interProcessUpdateNotification.JobPersistentId);

            if (dbItem == null) return;

            if (listItem != null)
            {
                listItem.DbEntry = dbItem;
                return;
            }

            var toAdd = await ScriptJobListListItem.CreateInstance(dbItem, _databaseFile);

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Add(toAdd);
        }
    }

    [BlockingCommand]
    public async Task RefreshList()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);

        var jobs = await db.ScriptJobs.ToListAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Clear();

        foreach (var x in jobs) Items.Add(await ScriptJobListListItem.CreateInstance(x, _databaseFile));

        await FilterList();

        UpdateCronExpressionInformation();

        await ListContextSortHelpers.SortList(
            ListSort.SortDescriptions(), Items);
    }

    [NonBlockingCommand]
    public async Task RunJob(ScriptJobListListItem? toRun)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toRun == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Run?");
            return;
        }

        await PowerShellRunner.ExecuteJob(toRun.DbEntry.PersistentId, DatabaseFile,
            "Run From PowerShell Runner Gui");
    }

    [NonBlockingCommand]
    public async Task RunJobsFromSelectedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var currentSelection = SelectedItems;

        if (!currentSelection.Any())
        {
            StatusContext.ToastWarning("Nothing Selected to Run?");
            return;
        }

        foreach (var loopSelected in currentSelection)
            await PowerShellRunner.ExecuteJob(loopSelected.DbEntry.PersistentId, DatabaseFile,
                "Run From PowerShell Runner Gui");
    }

    [NonBlockingCommand]
    public async Task RunWithProgressWindow(ScriptJobListListItem? toRun)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toRun == null)
        {
            StatusContext.ToastWarning("Nothing Selected to Run?");
            return;
        }

        await PowerShellRunner.ExecuteJob(toRun.DbEntry.PersistentId, DatabaseFile,
            "Run From PowerShell Runner Gui",
            async run =>
            {
                await ScriptProgressWindow.CreateInstance(run.ScriptJobPersistentId.AsList(), run.PersistentId.AsList(),
                    _databaseFile);
            });
    }

    private void UpdateCronExpressionInformation()
    {
        var jobs = Items.ToList();

        foreach (var loopJobs in jobs)
        {
            if (!loopJobs.DbEntry.ScheduleEnabled || string.IsNullOrWhiteSpace(loopJobs.DbEntry.CronExpression))
                loopJobs.NextRun = null;

            try
            {
                var expression = CronExpression.Parse(loopJobs.DbEntry.CronExpression);
                var nextRun = expression.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
                if (nextRun != null) loopJobs.NextRun = nextRun.Value.LocalDateTime;
                loopJobs.CronDescription = ExpressionDescriptor.GetDescription(loopJobs.DbEntry.CronExpression);
            }
            catch (Exception)
            {
                loopJobs.NextRun = null;
                loopJobs.CronDescription = string.Empty;
            }
        }
    }

    private async Task UpdateCronNextRun()
    {
        try
        {
            while (await _cronNextTimer.WaitForNextTickAsync())
                if (!StatusContext.BlockUi)
                    UpdateCronExpressionInformation();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [NonBlockingCommand]
    public async Task ViewJobRun(ScriptJobRun? toShow)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toShow == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        await ScriptJobRunViewerWindow.CreateInstance(toShow.PersistentId, DatabaseFile);
    }

    [NonBlockingCommand]
    public async Task ViewLatestJobRun(ScriptJobListListItem? toShow)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toShow == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var topRun = db.ScriptJobRuns.Where(x => x.ScriptJobPersistentId == toShow.DbEntry.PersistentId)
            .OrderByDescending(x => x.CompletedOnUtc)
            .FirstOrDefault();

        if (topRun == null)
        {
            StatusContext.ToastWarning("No Runs to Compare?");
            return;
        }

        await ScriptJobRunViewerWindow.CreateInstance(topRun.PersistentId, DatabaseFile);
    }

    [NonBlockingCommand]
    public async Task ViewRunList(ScriptJobListListItem? toShow)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toShow == null)
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        await ScriptJobRunListWindow.CreateInstance(toShow.DbEntry.PersistentId.AsList(), DatabaseFile);
    }

    [NonBlockingCommand]
    public async Task ViewRunListFromSelectedItems()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastWarning("Nothing Selected?");
            return;
        }

        await ScriptJobRunListWindow.CreateInstance(SelectedItems.Select(x => x.DbEntry.PersistentId).ToList(),
            DatabaseFile);
    }

    [NonBlockingCommand]
    public async Task ViewScript(ScriptJobListListItem? toRun)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (toRun == null)
        {
            StatusContext.ToastWarning("Nothing Selected to View?");
            return;
        }

        await ScriptViewWindow.CreateInstance(toRun.DbEntry.PersistentId, DatabaseFile);
    }
}