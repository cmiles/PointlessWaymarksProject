using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.FeedReaderGui;

public static class FeedReaderEncryptionHelper
{
    public static async Task SetUserBasicAuthEncryptionKeyEntry(StatusControlContext statusContext, string dbFileName)
    {
        var newSecretEntry = await statusContext.ShowStringEntry("Feed Reader Basic Auth Key",
            "Enter your Pointless Waymarks Feed Reader Basic Auth Key. This will be used to decrypt Basic Auth information stored in the Feed Reader database. This setting is 'per computer' and will need to be re-entered if you change computers or potentially with OS resets and changes - this is best stored in a password manager for later reference (if you loose this key there is no way to recover your Basic Auth information for Feeds)...",
            string.Empty);

        if (!newSecretEntry.Item1) return;

        var cleanedSecret = newSecretEntry.Item2.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedSecret))
        {
            statusContext.ToastError("Feed Reader Basic Auth Key Entry Canceled - Key can not be blank");
            return;
        }

        PasswordVaultTools.SaveCredentials(await FeedReaderEncryption.FeedReaderBasicAuthEncryptionResourceKey(dbFileName),
            DateTime.Now.ToString("s"),
            cleanedSecret);
    }
}