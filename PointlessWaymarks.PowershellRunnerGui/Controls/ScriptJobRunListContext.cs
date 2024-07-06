using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.StringDataEntry;
using Serilog;
using TinyIpc.Messaging;

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

    public DataNotificationsWorkQueue? DataNotificationsProcessor { get; set; }
    public required string FilterDescription { get; set; }
    public required ObservableCollection<ScriptJobRunGuiView> Items { get; set; }
    public List<Guid> JobFilter { get; set; } = [];
    public StringDataEntryNoIndicatorsContext ScriptViewerContext { get; set; }
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

        var possibleJobs = await db.ScriptJobs.Where(x => jobFilter.Contains(x.PersistentId)).ToListAsync();

        string filterDescription;
        if (jobFilter.Any())
            filterDescription = $"Job{(possibleJobs.Count > 1 ? "s" : "")}: {string.Join(", ", possibleJobs.OrderBy(x => x.Name).Select(x => x.Name))}";
        else
            filterDescription = "All Jobs";

        var runList = new List<ScriptJobRunGuiView>();

        foreach (var loopRun in filteredRuns)
        {
            var toAdd = new ScriptJobRunGuiView
            {
                Id = loopRun.Id,
                CompletedOnUtc = loopRun.CompletedOnUtc,
                CompletedOn = loopRun.CompletedOnUtc?.ToLocalTime(),
                Errors = loopRun.Errors,
                Output = loopRun.Output,
                RunType = loopRun.RunType,
                Script = loopRun.Script,
                StartedOnUtc = loopRun.StartedOnUtc,
                StartedOn = loopRun.StartedOnUtc.ToLocalTime(),
                ScriptJobPersistentId = loopRun.ScriptJobPersistentId,
                TranslatedOutput = loopRun.Output.Decrypt(key),
                TranslatedScript = loopRun.Script.Decrypt(key),
                PersistentId = loopRun.PersistentId,
                Job = possibleJobs.Single(x => x.PersistentId == loopRun.ScriptJobPersistentId)
            };

            runList.Add(toAdd);
        }

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

        factoryContext.DataNotificationsProcessor = new DataNotificationsWorkQueue
            { Processor = factoryContext.DataNotificationReceived };
        DataNotifications.NewDataNotificationChannel().MessageReceived += factoryContext.OnDataNotificationReceived;

        return factoryContext;
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
                Log.Error("Data Notification Failure. Error Note {0}. Status Control Context PersistentId {1}",
                    x.ErrorMessage,
                    StatusContext.StatusControlContextId);
                return Task.CompletedTask;
            }
        );

        if (toRun is not null) await toRun;
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

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        DataNotificationsProcessor?.Enqueue(e);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (e.PropertyName.Equals(nameof(SelectedItem)))
            ScriptViewerContext.UserValue = SelectedItem?.TranslatedScript ?? string.Empty;
    }

    private async Task ProcessDataUpdateNotification(
        DataNotifications.InterProcessDataNotification interProcessUpdateNotification)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (interProcessUpdateNotification.DatabaseId != _dbId ||
            (interProcessUpdateNotification.ContentType == DataNotifications.DataNotificationContentType.ScriptJob &&
             !JobFilter.Contains(interProcessUpdateNotification.PersistentId))) return;

        if (interProcessUpdateNotification is
            {
                ContentType: DataNotifications.DataNotificationContentType.ScriptJob
            })
        {
            //New or Deletes should be covered by the related notifications on runs
            if (interProcessUpdateNotification.UpdateType !=
                DataNotifications.DataNotificationUpdateType.Update) return;

            var updatedJob = await (await PowerShellRunnerDbContext.CreateInstance(_databaseFile))
                .ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == interProcessUpdateNotification.PersistentId);
            if (updatedJob == null) return;

            var toUpdate = Items.Where(x => x.ScriptJobPersistentId == interProcessUpdateNotification.PersistentId)
                .ToList();
            toUpdate.ForEach(x => x.Job = updatedJob);

            return;
        }

        if (interProcessUpdateNotification is
            {
                ContentType: DataNotifications.DataNotificationContentType.ScriptJobRun
            })
        {
            if (interProcessUpdateNotification.UpdateType ==
                DataNotifications.DataNotificationUpdateType.Delete)
            {
                var toRemove =
                    Items.SingleOrDefault(x => x.PersistentId == interProcessUpdateNotification.PersistentId);
                if (toRemove != null) Items.Remove(toRemove);
                return;
            }

            var listItem =
                Items.SingleOrDefault(x => x.PersistentId == interProcessUpdateNotification.PersistentId);
            var db = await PowerShellRunnerDbContext.CreateInstance(_databaseFile);
            var dbRun =
                await db.ScriptJobRuns.SingleOrDefaultAsync(x =>
                    x.PersistentId == interProcessUpdateNotification.PersistentId);

            if (dbRun == null) return;

            var dbJob = await db.ScriptJobs.SingleOrDefaultAsync(x => x.PersistentId == dbRun.ScriptJobPersistentId);

            if (dbJob == null) return;

            if (listItem != null)
            {
                listItem.Id = dbRun.Id;
                listItem.CompletedOnUtc = dbRun.CompletedOnUtc;
                listItem.CompletedOn = dbRun.CompletedOnUtc?.ToLocalTime();
                listItem.Errors = dbRun.Errors;
                listItem.Output = dbRun.Output;
                listItem.RunType = dbRun.RunType;
                listItem.Script = dbRun.Script;
                listItem.StartedOnUtc = dbRun.StartedOnUtc;
                listItem.StartedOn = dbRun.StartedOnUtc.ToLocalTime();
                listItem.ScriptJobPersistentId = dbRun.ScriptJobPersistentId;
                listItem.TranslatedOutput = dbRun.Output.Decrypt(_key);
                listItem.TranslatedScript = dbRun.Script.Decrypt(_key);
                return;
            }

            var toAdd = new ScriptJobRunGuiView
            {
                Id = dbRun.Id,
                CompletedOnUtc = dbRun.CompletedOnUtc,
                CompletedOn = dbRun.CompletedOnUtc?.ToLocalTime(),
                Errors = dbRun.Errors,
                Output = dbRun.Output,
                RunType = dbRun.RunType,
                Script = dbRun.Script,
                StartedOnUtc = dbRun.StartedOnUtc,
                StartedOn = dbRun.StartedOnUtc.ToLocalTime(),
                ScriptJobPersistentId = dbRun.ScriptJobPersistentId,
                TranslatedOutput = dbRun.Output.Decrypt(_key),
                TranslatedScript = dbRun.Script.Decrypt(_key),
                PersistentId = dbRun.PersistentId,
                Job = dbJob
            };

            await ThreadSwitcher.ResumeForegroundAsync();

            Items.Add(toAdd);
        }
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
}