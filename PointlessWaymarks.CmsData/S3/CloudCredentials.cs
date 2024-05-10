using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.S3;

public static class CloudCredentials
{
    /// <summary>
    ///     Returns the Credential Manager Resource Key for the current settings file for AWS Site credentials
    /// </summary>
    /// <returns></returns>
    public static string AwsSiteCredentialResourceString()
    {
        return
            $"Pointless Waymarks CMS - AWS, Site - {UserSettingsSingleton.CurrentSettings().SettingsId}";
    }
    
    /// <summary>
    ///     Returns the Credential Manager Resource Key for the current settings file for Cloudflare Site credentials
    /// </summary>
    /// <returns></returns>
    public static string CloudflareAccountIdResourceString()
    {
        return
            $"Pointless Waymarks CMS - Cloudflare Account Id, Site - {UserSettingsSingleton.CurrentSettings().SettingsId}";
    }
    
    /// <summary>
    ///     Returns the Credential Manager Resource Key for the current settings file for Cloudflare Site credentials
    /// </summary>
    /// <returns></returns>
    public static string CloudflareSiteCredentialResourceString()
    {
        return
            $"Pointless Waymarks CMS - Cloudflare Credentials, Site - {UserSettingsSingleton.CurrentSettings().SettingsId}";
    }
    
    /// <summary>
    ///     Retrieves the AWS Credentials associated with this settings file
    /// </summary>
    /// <returns></returns>
    public static (string accessKey, string secret) GetAwsSiteCredentials()
    {
        return PasswordVaultTools.GetCredentials(AwsSiteCredentialResourceString());
    }
    
    /// <summary>
    ///     Retrieves the Cloudflare Account Id associated with this settings file
    /// </summary>
    /// <returns></returns>
    public static (string accessKey, string secret) GetCloudflareAccountId()
    {
        return PasswordVaultTools.GetCredentials(CloudflareAccountIdResourceString());
    }
    
    /// <summary>
    ///     Retrieves the Cloudflare Credentials associated with this settings file
    /// </summary>
    /// <returns></returns>
    public static (string accessKey, string secret) GetCloudflareSiteCredentials()
    {
        return PasswordVaultTools.GetCredentials(CloudflareSiteCredentialResourceString());
    }
    
    /// <summary>
    ///     Removes all AWS Credentials associated with this settings file
    /// </summary>
    public static void RemoveAwsSiteCredentials()
    {
        PasswordVaultTools.RemoveCredentials(AwsSiteCredentialResourceString());
    }
    
    /// <summary>
    ///     Removes all Cloudflare Credentials associated with this settings file
    /// </summary>
    public static void RemoveCloudflareSiteAccountIdAndCredentials()
    {
        PasswordVaultTools.RemoveCredentials(CloudflareAccountIdResourceString());
        PasswordVaultTools.RemoveCredentials(CloudflareSiteCredentialResourceString());
    }
    
    /// <summary>
    ///     Removes any existing AWS Credentials Associated with this settings file and Saves new Credentials
    /// </summary>
    /// <param name="accessKey"></param>
    /// <param name="secret"></param>
    public static void SaveAwsSiteCredential(string accessKey, string secret)
    {
        PasswordVaultTools.SaveCredentials(AwsSiteCredentialResourceString(), accessKey, secret);
    }
    
    /// <summary>
    ///     Removes any existing Cloudflare Credentials Associated with this settings file and Saves new Credentials
    /// </summary>
    /// <param name="accountId"></param>
    public static void SaveCloudflareSiteAccountId(string accountId)
    {
        PasswordVaultTools.SaveCredentials(CloudflareAccountIdResourceString(),
            SlugTools.RandomLowerCaseString(10), accountId);
    }
    
    /// <summary>
    ///     Removes any existing Cloudflare Credentials Associated with this settings file and Saves new Credentials
    /// </summary>
    /// <param name="accessKey"></param>
    /// <param name="secret"></param>
    public static void SaveCloudflareSiteCredential(string accessKey, string secret)
    {
        PasswordVaultTools.SaveCredentials(CloudflareSiteCredentialResourceString(), accessKey, secret);
    }
}