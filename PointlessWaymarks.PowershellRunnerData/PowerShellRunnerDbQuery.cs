using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.PowerShellRunnerData.Models;

namespace PointlessWaymarks.PowerShellRunnerData;

public static class PowerShellRunnerDbQuery
{
    public const string ObfuscationService = "https://pointlesswaymarks.powershellrunner.private";
    public const string ObfuscationServiceAccountKey = "ObfuscationServiceAccountKey";

    public static async Task<string?> ObfuscationAccountName(this PowerShellRunnerDbContext context)
    {
        var possibleEntry = await context.PowerShellRunnerSettings.FirstOrDefaultAsync(x => x.Key == ObfuscationServiceAccountKey);

        return possibleEntry?.Value;
    }

    public static async Task<string> ObfuscationAccountNameWithCreateAsNeeded(this PowerShellRunnerDbContext context)
    {
        //Clear any invalid entries
        var invalidEntries =
            context.PowerShellRunnerSettings.Where(x => x.Key == ObfuscationServiceAccountKey && string.IsNullOrWhiteSpace(x.Value));

        if (invalidEntries.Any())
        {
            await invalidEntries.ExecuteDeleteAsync();
            await context.SaveChangesAsync();
        }

        var currentSettings = await context.PowerShellRunnerSettings.Where(x => x.Key == ObfuscationServiceAccountKey).ToListAsync();

        if (currentSettings.Count > 1) context.PowerShellRunnerSettings.RemoveRange(currentSettings.Skip(1));

        if (currentSettings.Any()) return currentSettings[0].Value;

        await context.SetNewObfuscationAccountName();

        return (await context.ObfuscationAccountName())!;
    }

    private static async Task SetNewObfuscationAccountName(this PowerShellRunnerDbContext context)
    {
        await context.PowerShellRunnerSettings.Where(x => x.Key == "ObfuscationService").ExecuteDeleteAsync();

        await context.PowerShellRunnerSettings.AddAsync(new PowerShellRunnerSetting()
        {
            Key = ObfuscationServiceAccountKey,
            Value = Guid.NewGuid().ToString()
        });

        await context.SaveChangesAsync();
    }
}