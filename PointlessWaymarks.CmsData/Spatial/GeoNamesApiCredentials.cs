using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WindowsTools;

namespace PointlessWaymarks.CmsData.Spatial;

public static class GeoNamesApiCredentials
{
    /// <summary>
    ///     Returns the Credential Manager Resource Key for the current settings file for GEONAMES Site credentials
    /// </summary>
    /// <returns></returns>
    public static string GeoNamesSiteCredentialResourceString()
    {
        return
            $"Pointless Waymarks CMS - GeoNames Username, Site - {UserSettingsSingleton.CurrentSettings().SettingsId}";
    }

    /// <summary>
    ///     Retrieves the GEONAMES Credentials associated with this settings file
    /// </summary>
    /// <returns></returns>
    public static string GetGeoNamesSiteCredentials()
    {
        return PasswordVaultTools.GetCredentials(GeoNamesSiteCredentialResourceString()).password;
    }

    /// <summary>
    ///     Removes all GEONAMES Credentials associated with this settings file
    /// </summary>
    public static void RemoveGeoNamesSiteCredentials()
    {
        PasswordVaultTools.RemoveCredentials(GeoNamesSiteCredentialResourceString());
    }

    /// <summary>
    ///     Saves any existing GeoNames Credentials Associated with this settings file and Saves new Credentials
    /// </summary>
    /// <param name="username"></param>
    public static void SaveGeoNamesSiteCredential(string username)
    {
        PasswordVaultTools.SaveCredentials(GeoNamesSiteCredentialResourceString(), "GeoNamesApiUserName", username);
    }
}