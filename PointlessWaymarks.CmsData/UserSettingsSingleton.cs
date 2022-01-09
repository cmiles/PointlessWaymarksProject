namespace PointlessWaymarks.CmsData;

public static class UserSettingsSingleton
{
    private static UserSettings? _userSettings;

    public static UserSettings CurrentSettings()
    {
        return _userSettings ??= UserSettingsUtilities.ReadFromCurrentSettingsFile().Result;
    }
}