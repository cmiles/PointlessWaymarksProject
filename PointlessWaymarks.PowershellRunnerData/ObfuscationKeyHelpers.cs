using GitCredentialManager;

namespace PointlessWaymarks.PowerShellRunnerData;

public static class ObfuscationKeyHelpers
{
    public static async Task<string> GetObfuscationKey(string dbFileName)
    {
        var db = await PowerShellRunnerDbContext.CreateInstance(dbFileName, false);

        var account = await db.ObfuscationAccountName();

        if (account == null || string.IsNullOrWhiteSpace(account))
            throw new Exception(
                "No Obfuscation Account Name found in the database - cannot retrieve Obfuscation Key. Restart the program???");

        var store = CredentialManager.Create();
        var obfuscationKey = store.Get(PowerShellRunnerDbQuery.ObfuscationService, account);

        if (obfuscationKey == null || string.IsNullOrWhiteSpace(obfuscationKey.Password))
            throw new Exception(
                "No Obfuscation Key found in the Credential Manager - cannot retrieve Obfuscation Key. Restart the program???");

        return obfuscationKey.Password;
    }
}