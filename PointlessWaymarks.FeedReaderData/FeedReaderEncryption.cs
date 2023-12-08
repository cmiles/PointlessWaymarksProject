using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.FeedReaderData;

public static class FeedReaderEncryption
{
    public static (string username, string password) DecryptBasicAuthCredentials(string username, string password,
        string dbFileName)
    {
        var key = GetUserBasicAuthEncryptionKeyEntry(dbFileName);

        return (username.Decrypt(key), password.Decrypt(key));
    }

    public static (string username, string password) EncryptBasicAuthCredentials(string username, string password,
        string dbFileName)
    {
        var key = GetUserBasicAuthEncryptionKeyEntry(dbFileName);

        return (username.Encrypt(key), password.Encrypt(key));
    }

    public static string FeedReaderBasicAuthEncryptionResourceKey(string dbFileName)
    {
        var readerId = FeedContext.FeedReaderGuidIdString(dbFileName);

        return $"FeedReaderBasicAuth-{readerId}";
    }

    public static string GetUserBasicAuthEncryptionKeyEntry(string dbFileName)
    {
        var resourceKey = FeedReaderBasicAuthEncryptionResourceKey(dbFileName);

        var credentials = PasswordVaultTools.GetCredentials(resourceKey);

        return credentials.password;
    }


}