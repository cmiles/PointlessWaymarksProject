using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.PowerShellRunnerData.Models;

namespace PointlessWaymarks.PowerShellRunnerData;

public static class PowerShellRunnerDb
{
    public const string ObfuscationService = "https://pointlesswaymarks.powershellrunner.private";
    public const string ObfuscationServiceAccountKey = "ObfuscationServiceAccountKey";

    public static async Task<string> GetObfuscationAccountNameWithCreateAsNeeded(this PowerShellRunnerContext context)
    {
        //Clear any invalid entries
        var invalidEntries =
            context.Settings.Where(x => x.Key == ObfuscationServiceAccountKey && string.IsNullOrWhiteSpace(x.Value));

        if (invalidEntries.Any())
        {
            await invalidEntries.ExecuteDeleteAsync();
            await context.SaveChangesAsync();
        }

        var currentSettings = await context.Settings.Where(x => x.Key == ObfuscationServiceAccountKey).ToListAsync();

        if (currentSettings.Count > 1) context.Settings.RemoveRange(currentSettings.Skip(1));

        if (currentSettings.Any()) return currentSettings[0].Value;

        await context.SetNewObfuscationAccountName();

        return (await context.ObfuscationAccountName())!;
    }

    private static async Task<string?> ObfuscationAccountName(this PowerShellRunnerContext context)
    {
        var possibleEntry = await context.Settings.FirstOrDefaultAsync(x => x.Key == ObfuscationServiceAccountKey);

        return possibleEntry?.Value;
    }

    private static async Task SetNewObfuscationAccountName(this PowerShellRunnerContext context)
    {
        await context.Settings.Where(x => x.Key == "ObfuscationService").ExecuteDeleteAsync();

        await context.Settings.AddAsync(new Setting()
        {
            Key = ObfuscationServiceAccountKey,
            Value = Guid.NewGuid().ToString()
        });

        await context.SaveChangesAsync();
    }
}