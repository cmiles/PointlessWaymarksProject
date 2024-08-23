using GitCredentialManager;

namespace PointlessWaymarks.PowerShellRunnerData;

public static class ObfuscationKeyHelpers
{
    private static ICredentialStore? _credentialStore;

    public static async Task<string> GetObfuscationKey(string dbFileName)
    {
        var db = await PowerShellRunnerDbContext.CreateInstance(dbFileName);

        var account = await db.ObfuscationAccountName();

        if (account == null || string.IsNullOrWhiteSpace(account))
            throw new Exception(
                "No Obfuscation Account Name found in the database - cannot retrieve Obfuscation Key. Restart the program???");

        _credentialStore ??= CredentialManager.Create();

        var obfuscationKey = _credentialStore.Get(PowerShellRunnerDbQuery.ObfuscationService, account);

        if (obfuscationKey == null || string.IsNullOrWhiteSpace(obfuscationKey.Password))
            throw new Exception(
                "No Obfuscation Key found in the Credential Manager - cannot retrieve Obfuscation Key. Restart the program???");

        return obfuscationKey.Password;
    }
}