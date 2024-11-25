using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.PowerShellRunnerData.Models;
using TinyIpc.Messaging;

namespace PointlessWaymarks.PowerShellRunnerData;

internal class PowerShellRunnerCloseOrphanRuns
{
    private readonly ConcurrentBag<Guid> _repliedScriptJobRuns = [];
    private List<ScriptJobRun> _runsToCheck = [];
    internal required string DatabaseFile { get; set; }
    internal required Guid DatabaseId { get; set; }

    private void OnDataNotificationReceived(object? sender, TinyMessageReceivedEventArgs e)
    {
        var translatedMessage = DataNotifications.TranslateDataNotification(e.Message.ToString());

        if (!translatedMessage.IsT7) return;

        var message = translatedMessage.AsT7;

        if (message.DatabaseId != DatabaseId) return;

        _repliedScriptJobRuns.Add(message.RunPersistentId);
    }

    internal async Task CloseOrphans()
    {
        var db = await PowerShellRunnerDbContext.CreateInstance(DatabaseFile);
        _runsToCheck = await db.ScriptJobRuns
            .Where(run => run.CompletedOnUtc == null)
            .ToListAsync();

        if (!_runsToCheck.Any()) return;

        DataNotifications.NewDataNotificationChannel().MessageReceived += OnDataNotificationReceived;

        DataNotifications.PublishOpenJobsRequest(nameof(PowerShellRunnerCloseOrphanRuns), DatabaseId);

        await Task.Delay(30000);

        foreach (var loopRuns in _runsToCheck)
            if (!_repliedScriptJobRuns.Contains(loopRuns.PersistentId))
            {
                var currentEntry =
                    await db.ScriptJobRuns.SingleOrDefaultAsync(x => x.PersistentId == loopRuns.PersistentId);
                if (currentEntry == null) continue;

                currentEntry.CompletedOnUtc = DateTime.UtcNow;
                currentEntry.Errors = true;
                var key = await ObfuscationKeyHelpers.GetObfuscationKey(DatabaseFile);

                var currentOutput = string.IsNullOrWhiteSpace(currentEntry.Output) ? string.Empty
                    : currentEntry.Output.Decrypt(key);

                var newOutput = currentOutput + Environment.NewLine +
                                "Run was orphaned - maybe the program closed or crashed while this Run was still active? Output may be lost and the outcome of the job is marked as 'Error' because it is not known...";

                currentEntry.Output = newOutput.Encrypt(key);
                await db.SaveChangesAsync();

                DataNotifications.PublishRunDataNotification("Automated Close Orphans",
                    DataNotifications.DataNotificationUpdateType.Update, DatabaseId, currentEntry.ScriptJobPersistentId,
                    currentEntry.PersistentId);
            }
    }
}