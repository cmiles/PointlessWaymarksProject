using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using Serilog;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
[StaThreadConstructorGuard]
public partial class ScriptJobRunListContext
{
    private string _databaseFile = string.Empty;
    private Guid _dbId = Guid.Empty;
    private string _key = string.Empty;

    public ScriptJobRunListContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public NotificationCatcher? DataNotificationsProcessor { get; set; }
    public required string FilterDescription { get; set; }
    public required ObservableCollection<ScriptJobRunGuiView> Items { get; set; }
    public List<Guid> JobFilter { get; set; } = [];
    public required StringDataEntryNoIndicatorsContext ScriptViewerContext { get; set; }
    public ScriptJobRunGuiView? SelectedItem { get; set; }
    public List<ScriptJobRunGuiView> SelectedItems { get; set; } = [];
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<ScriptJobRunListContext> CreateInstance(StatusControlContext? statusContext,
        List<Guid> jobFilter, string databaseFile)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await PowerShellRunnerDbContext.CreateInstance(databaseFile);
        var key = await ObfuscationKeyHelpers.GetObfuscationKey(databaseFile);
        var dbId = await PowerShellRunnerDbQuery.DbId(databaseFile);

        var filteredRuns = jobFilter.Any()
            ? await db.ScriptJobRuns.Where(x => jobFilter.Contains(x.ScriptJobPersistentId))
                .OrderByDescending(x => x.StartedOnUtc).AsNoTracking().ToListAsync()
            : await db.ScriptJobRuns.OrderByDescending(x => x.StartedOnUtc).AsNoTracking().ToListAsync();

        var possibleJobs = jobFilter.Any()
            ? await db.ScriptJobs.Where(x => jobFilter.Contains(x.PersistentId)).ToListAsync()
            : await db.ScriptJobs.ToListAsync();

        var filterDescription = jobFilter.Any()
            ? $"Job{(possibleJobs.Count > 1 ? "s" : "")}: {string.Join(", ", possibleJobs.OrderBy(x => x.Name).Select(x => x.Name))}"
            : "All Jobs";

        var runList = new List<ScriptJobRunGuiView>();

        foreach (var loopRun in filteredRuns)
            runList.Add(ScriptJobRunGuiView.CreateInstance(loopRun,
                possibleJobs.SingleOrDefault(x => x.PersistentId == loopRun.ScriptJobPersistentId), key));

        var factoryScriptViewerContext = StringDataEntryNoIndicatorsContext.CreateInstance();
        factoryScriptViewerContext.Title = "Script";

        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryContext = new ScriptJobRunListContext
        {
            StatusContext = statusContext ?? new StatusControlContext(),
            Items = new ObservableCollection<ScriptJobRunGuiView>(runList),
            JobFilter = jobFilter,
            FilterDescription = filterDescription,
            ScriptViewerContext = factoryScriptViewerContext,
            _key = key,
            _databaseFile = databaseFile,
            _dbId = dbId
        };

        factoryContext.BuildCommands();

        factoryContext.DataNotificationsProcessor = new NotificationCatcher
        {
            JobDataNotification = factoryContext.ProcessJobUpdateNotification,
            RunDataNotification = factoryContext.ProcessRunUpdateNotification
        };

        return factoryContext;
    }

    [BlockingCommand]
    public async Task DeleteSelectedRuns()
    {
        if (!SelectedItems.Any())
        {
            StatusContext.ToastError("No Runs Selected to Delete?");
            return;
        }

        var selectedIdsToDelete = SelectedItems.Select(x => x.PersistentId).ToList();

        if (selectedIdsToDelete.Count > 1)
            if ((await StatusContext.ShowMessageWithYesNoButton("Confirm Delete",
                    $"Runs are permanently deleted without any ability to restore later - do you really want to delete {SelectedItems.Count} Items?"))
                .Equals("no", StringComparison.OrdinalIgnoreCase))
                return;

        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);

        var toDelete = await db.ScriptJobRuns.Where(x => selectedIdsToDelete.Contains(x.PersistentId))
            .ToListAsync();

        db.ScriptJobRuns.RemoveRange(toDelete);
        await db.SaveChangesAsync();

        Log.ForContext("JobPersistentIds",
                toDelete.Select(x => x.ScriptJobPersistentId).Distinct().ToList().SafeObjectDump())
            .ForContext(nameof(selectedIdsToDelete), selectedIdsToDelete.SafeObjectDump())
            .Information("Deleting {0} ScriptJobRuns manually from the Run List", selectedIdsToDelete.Count);

        foreach (var loopDelete in toDelete)
            DataNotifications.PublishRunDataNotification("Run List",
                DataNotifications.DataNotificationUpdateType.Delete,
                _dbId, loopDelete.ScriptJobPersistentId, loopDelete.PersistentId);
    }

    [NonBlockingCommand]
    public async Task DiffSelectedRun()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (!SelectedItems.Any())
        {
            StatusContext.ToastError("No Run Selected?");
            return;
        }

        if (SelectedItems.Count > 2)
        {
            StatusContext.ToastError($"Selected 2 Runs to Diff - {SelectedItems.Count} Selected?");
            return;
        }

        if (SelectedItems.Count == 2)
        {
            await ScriptJobRunOutputDiffWindow.CreateInstance(SelectedItems[0].PersistentId,
                SelectedItems[1].PersistentId, _databaseFile);
            return;
        }

        await ScriptJobRunOutputDiffWindow.CreateInstance(SelectedItems[0].PersistentId, null, _databaseFile);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (e.PropertyName.Equals(nameof(SelectedItem)))
            ScriptViewerContext.UserValue = SelectedItem?.TranslatedScript ?? string.Empty;
    }


    private async Task ProcessJobUpdateNotification(
        DataNotifications.InterProcessJobDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId ||
            !JobFilter.Contains(interProcessUpdateNotification.JobPersistentId)) return;

        //New or Deletes should be covered by the related notifications on runs
        if (interProcessUpdateNotification.UpdateType !=
            DataNotifications.DataNotificationUpdateType.Update) return;

        var updatedJob = await (await PowerShellRunnerDbContext.CreateInstance(_databaseFile))
            .ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == interProcessUpdateNotification.JobPersistentId);
        if (updatedJob == null) return;

        var toUpdate = Items.Where(x => x.ScriptJobPersistentId == interProcessUpdateNotification.JobPersistentId)
            .ToList();
        toUpdate.ForEach(x => x.Job = updatedJob);
    }

    private async Task ProcessRunUpdateNotification(
        DataNotifications.InterProcessRunDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId ||
            !JobFilter.Contains(interProcessUpdateNotification.JobPersistentId)) return;

        if (interProcessUpdateNotification.UpdateType ==
            DataNotifications.DataNotificationUpdateType.Delete)
        {
            var toRemove = Items.Where(x => x.PersistentId == interProcessUpdateNotification.RunPersistentId).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();

            foreach (var loopDeletes in toRemove) Items.Remove(loopDeletes);

            return;
        }

        var listItem =
            Items.SingleOrDefault(x => x.PersistentId == interProcessUpdateNotification.RunPersistentId);
        var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
        var dbRun =
            await db.ScriptJobRuns.SingleOrDefaultAsync(x =>
                x.PersistentId == interProcessUpdateNotification.RunPersistentId);

        if (dbRun == null) return;

        var dbJob = await db.ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == dbRun.ScriptJobPersistentId);

        if (dbJob == null) return;

        if (listItem != null)
        {
            listItem.Update(dbRun, dbJob, _key);
            return;
        }

        var toAdd = ScriptJobRunGuiView.CreateInstance(dbRun, dbJob, _key);

        await ThreadSwitcher.ResumeForegroundAsync();

        Items.Add(toAdd);
    }

    [NonBlockingCommand]
    public async Task ViewRun(Guid? persistentGuid)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (persistentGuid == null)
        {
            StatusContext.ToastError("No Run Selected?");
            return;
        }

        await ScriptJobRunViewerWindow.CreateInstance(persistentGuid.Value, _databaseFile);
    }



    [NonBlockingCommand]
    public async Task ViewSelectedRun()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItem == null)
        {
            StatusContext.ToastError("No Run Selected?");
            return;
        }

        await ScriptJobRunViewerWindow.CreateInstance(SelectedItem.PersistentId, _databaseFile);
    }

    [NonBlockingCommand]
    public async Task ViewProgressWindowForSelectedRun()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (SelectedItem == null)
        {
            StatusContext.ToastError("No Run Selected?");
            return;
        }

        await ScriptProgressWindow.CreateInstance(SelectedItem.ScriptJobPersistentId.AsList(), SelectedItem.PersistentId.AsList(), _databaseFile);
    }
}