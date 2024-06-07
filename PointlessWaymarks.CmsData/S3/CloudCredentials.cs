using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WindowsTools;

namespace PointlessWaymarks.CmsData.S3;

public static class CloudCredentials
{
    /// <summary>
    ///     Retrieves the S3 Credentials associated with this settings file
    /// </summary>
    /// <returns></returns>
    public static string GetS3ServiceUrl()
    {
        return PasswordVaultTools.GetCredentials(S3SiteServiceUrlResourceString()).password;
    }
    
    /// <summary>
    ///     Retrieves the S3 Credentials associated with this settings file
    /// </summary>
    /// <returns></returns>
    public static (string accessKey, string secret) GetS3SiteCredentials()
    {
        return PasswordVaultTools.GetCredentials(S3SiteCredentialResourceString());
    }
    
    /// <summary>
    ///     Removes all S3 Service URLs associated with this settings file
    /// </summary>
    public static void RemoveS3ServiceUrls()
    {
        PasswordVaultTools.RemoveCredentials(S3SiteServiceUrlResourceString());
    }
    
    /// <summary>
    ///     Removes all S3 Credentials associated with this settings file
    /// </summary>
    public static void RemoveS3SiteCredentials()
    {
        PasswordVaultTools.RemoveCredentials(S3SiteCredentialResourceString());
    }
    
    /// <summary>
    ///     Returns the Credential Manager Resource Key for the current settings file for S3 Site credentials
    /// </summary>
    /// <returns></returns>
    public static string S3SiteCredentialResourceString()
    {
        return
            $"Pointless Waymarks CMS - S3 Credentials, Site - {UserSettingsSingleton.CurrentSettings().SettingsId}";
    }
    
    /// <summary>
    ///     Returns the Credential Manager Resource Key for the current settings file for an S3 Service URL
    /// </summary>
    /// <returns></returns>
    public static string S3SiteServiceUrlResourceString()
    {
        return
            $"Pointless Waymarks CMS - S3 Service URL, Site - {UserSettingsSingleton.CurrentSettings().SettingsId}";
    }
    
    /// <summary>
    ///     Removes any existing S3 Service URLs Saves a new Service URL
    /// </summary>
    /// <param name="serviceUrl"></param>
    public static void SaveS3ServiceUrl(string serviceUrl)
    {
        PasswordVaultTools.SaveCredentials(S3SiteServiceUrlResourceString(), "Service Url", serviceUrl);
    }
    
    /// <summary>
    ///     Removes any existing S3 Credentials Associated with this settings file and Saves new Credentials
    /// </summary>
    /// <param name="accessKey"></param>
    /// <param name="secret"></param>
    public static void SaveS3SiteCredential(string accessKey, string secret)
    {
        PasswordVaultTools.SaveCredentials(S3SiteCredentialResourceString(), accessKey, secret);
    }
}