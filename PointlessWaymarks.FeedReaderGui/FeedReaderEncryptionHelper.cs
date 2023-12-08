using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeedReaderData;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.FeedReaderGui;

public static class FeedReaderEncryptionHelper
{
    public static async Task SetUserBasicAuthEncryptionKeyEntry(StatusControlContext statusContext, string dbFileName)
    {
        var newSecretEntry = await statusContext.ShowStringEntry("Feed Reader Basic Auth Key",
            "Enter your Pointless Waymarks Feed Reader Basic Auth Key", string.Empty);

        if (!newSecretEntry.Item1) return;

        var cleanedSecret = newSecretEntry.Item2.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedSecret))
        {
            statusContext.ToastError("Feed Reader Basic Auth Key Entry Canceled - Key can not be blank");
            return;
        }

        PasswordVaultTools.SaveCredentials(FeedReaderEncryption.FeedReaderBasicAuthEncryptionResourceKey(dbFileName), DateTime.Now.ToString("s"),
            cleanedSecret);
    }
}