using GitCredentialManager;
using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui;

public static class ObfuscationKeyGuiHelpers
{
    /// <summary>
    ///     Gets the Obfuscation Key from the Credential Manager or prompts the user to enter one if it is not found. The
    ///     database must already have been set/created.
    /// </summary>
    /// <param name="statusContext"></param>
    /// <returns></returns>
    public static async Task<string> GetObfuscationKeyWithUserCreateAsNeeded(StatusControlContext statusContext)
    {
        var db = await PowerShellRunnerDbContext.CreateInstance();

        var account = await db.ObfuscationAccountNameWithCreateAsNeeded();

        var store = CredentialManager.Create();
        var obfuscationKey = store.Get(PowerShellRunnerDbQuery.ObfuscationService, account);

        if (obfuscationKey == null || string.IsNullOrWhiteSpace(obfuscationKey.Password))
        {
            var userObfuscationKeyResponse = await statusContext.ShowStringEntry("Obfuscation Key",
                "Please enter a non-blank key to use to obfuscate your PowerShell Scripts and Run Information in the database. You will need to remember this key - please consider entering it into your password manager...",
                string.Empty);

            while (!userObfuscationKeyResponse.Item1 || string.IsNullOrWhiteSpace(userObfuscationKeyResponse.Item2))
                userObfuscationKeyResponse = await statusContext.ShowStringEntry("Required Obfuscation Key",
                    "Please enter a non-blank key to use to obfuscate your PowerShell Scripts and Run Information in the database. This key is required for the program and you will need to remember this key - please consider entering it into your password manager...",
                    string.Empty);

            store.AddOrUpdate(PowerShellRunnerDbQuery.ObfuscationService, account, userObfuscationKeyResponse.Item2.Trim());

            return userObfuscationKeyResponse.Item2.Trim();
        }

        return obfuscationKey.Password;
    }
}