using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.S3;

public static class AwsCredentials
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
    ///     Retrieves the AWS Credentials associated with this settings file
    /// </summary>
    /// <returns></returns>
    public static (string accessKey, string secret) GetAwsSiteCredentials()
    {
        return PasswordVaultTools.GetCredentials(AwsSiteCredentialResourceString());
    }

    /// <summary>
    ///     Removes all AWS Credentials associated with this settings file
    /// </summary>
    public static void RemoveAwsSiteCredentials()
    {
        PasswordVaultTools.RemoveCredentials(AwsSiteCredentialResourceString());
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
}