using PointlessWaymarks.VaultfuscationTools;
using PointlessWaymarks.WindowsTools;

namespace PointlessWaymarks.FeedReaderData;

public static class FeedReaderEncryption
{
    public static async Task<(string username, string password)> DecryptBasicAuthCredentials(string username,
        string password,
        string dbFileName)
    {
        var key = await GetUserBasicAuthEncryptionKeyEntry(dbFileName);
        
        return (username.Decrypt(key), password.Decrypt(key));
    }
    
    public static async Task<(string username, string password)> EncryptBasicAuthCredentials(string username,
        string password,
        string dbFileName)
    {
        var key = await GetUserBasicAuthEncryptionKeyEntry(dbFileName);
        
        return (username.Encrypt(key), password.Encrypt(key));
    }
    
    public static async Task<string> FeedReaderBasicAuthEncryptionResourceKey(string dbFileName)
    {
        var readerId = await FeedContext.FeedReaderGuidIdString(dbFileName);
        
        return $"FeedReaderBasicAuth-{readerId}";
    }
    
    public static async Task<string> GetUserBasicAuthEncryptionKeyEntry(string dbFileName)
    {
        var resourceKey = await FeedReaderBasicAuthEncryptionResourceKey(dbFileName);
        
        var credentials = PasswordVaultTools.GetCredentials(resourceKey);
        
        return credentials.password;
    }
}