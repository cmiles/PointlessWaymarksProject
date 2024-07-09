using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.PowerShellRunnerData.Models;

namespace PointlessWaymarks.PowerShellRunnerData;

public static class PowerShellRunnerDbQuery
{
    public const string DbPersistentIdSettingsKey = "DbPersistentIdSettingsKey";
    public const string ObfuscationService = "https://pointlesswaymarks.powershellrunner.private";
    public const string ObfuscationServiceAccountSettingsKey = "ObfuscationServiceAccountSettingsKey";

    public static async Task<Guid> DbId(this PowerShellRunnerDbContext context)
    {
        var keyValuePair = await context.PowerShellRunnerSettings.SingleAsync(x => x.Key == DbPersistentIdSettingsKey);

        return Guid.Parse(keyValuePair.Value);
    }

    public static async Task<Guid> DbId(string dbFileName)
    {
        var db = await PowerShellRunnerDbContext.CreateInstance(dbFileName);
        return await db.DbId();
    }

    public static async Task DeleteScriptJobRunsBasedOnDeleteScriptJobRunsAfterMonthsSetting(string dbFileName)
    {
        var db = await PowerShellRunnerDbContext.CreateInstance(dbFileName);
        var dbId = await DbId(dbFileName);
        var allJob = await db.ScriptJobs.OrderByDescending(x => x.Name).ToListAsync();

        var frozenUtcNow = DateTime.UtcNow;

        foreach (var loopJobs in allJob)
        {
            var deleteBefore = frozenUtcNow.AddMonths(-Math.Abs(loopJobs.DeleteScriptJobRunsAfterMonths));

            var runsToDelete = await db.ScriptJobRuns
                .Where(x => x.ScriptJobPersistentId == loopJobs.PersistentId &&
                            ((x.StartedOnUtc < deleteBefore && x.CompletedOnUtc == null) ||
                             (x.StartedOnUtc < deleteBefore && x.CompletedOnUtc < deleteBefore)))
                .ToListAsync();

            db.ScriptJobRuns.RemoveRange(runsToDelete);
            await db.SaveChangesAsync();

            foreach (var loopDeleteRuns in runsToDelete)
                DataNotifications.PublishRunDataNotification($"Automated Deletes for Runs Before {deleteBefore:G}",
                    DataNotifications.DataNotificationUpdateType.Delete, dbId, loopJobs.PersistentId,
                    loopDeleteRuns.PersistentId);
        }
    }

    public static async Task<string?> ObfuscationAccountName(this PowerShellRunnerDbContext context)
    {
        var possibleEntry =
            await context.PowerShellRunnerSettings.FirstOrDefaultAsync(x =>
                x.Key == ObfuscationServiceAccountSettingsKey);

        return possibleEntry?.Value;
    }

    public static async Task<string> ObfuscationAccountNameWithCreateAsNeeded(this PowerShellRunnerDbContext context)
    {
        //Clear any invalid entries
        var invalidEntries =
            context.PowerShellRunnerSettings.Where(x =>
                x.Key == ObfuscationServiceAccountSettingsKey && string.IsNullOrWhiteSpace(x.Value));

        if (invalidEntries.Any())
        {
            await invalidEntries.ExecuteDeleteAsync();
            await context.SaveChangesAsync();
        }

        var currentSettings = await context.PowerShellRunnerSettings
            .Where(x => x.Key == ObfuscationServiceAccountSettingsKey)
            .ToListAsync();

        if (currentSettings.Count > 1) context.PowerShellRunnerSettings.RemoveRange(currentSettings.Skip(1));

        if (currentSettings.Any()) return currentSettings[0].Value;

        await context.SetNewObfuscationAccountName();

        return (await context.ObfuscationAccountName())!;
    }

    private static async Task SetNewObfuscationAccountName(this PowerShellRunnerDbContext context)
    {
        await context.PowerShellRunnerSettings.Where(x => x.Key == ObfuscationServiceAccountSettingsKey)
            .ExecuteDeleteAsync();

        await context.PowerShellRunnerSettings.AddAsync(new PowerShellRunnerSetting()
        {
            Key = ObfuscationServiceAccountSettingsKey,
            Value = Guid.NewGuid().ToString()
        });

        await context.SaveChangesAsync();
    }

    public static async Task VerifyOrAddDbId(this PowerShellRunnerDbContext context)
    {
        var existingKey =
            await context.PowerShellRunnerSettings.FirstOrDefaultAsync(x => x.Key == DbPersistentIdSettingsKey);

        if (existingKey != null && Guid.TryParse(existingKey.Value, out var keyValue)) return;

        await context.PowerShellRunnerSettings.Where(x => x.Key == DbPersistentIdSettingsKey).ExecuteDeleteAsync();

        await context.PowerShellRunnerSettings.AddAsync(new PowerShellRunnerSetting()
        {
            Key = DbPersistentIdSettingsKey,
            Value = Guid.NewGuid().ToString()
        });

        await context.SaveChangesAsync();
    }
}