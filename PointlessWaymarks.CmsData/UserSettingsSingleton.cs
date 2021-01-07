namespace PointlessWaymarks.CmsData
{
    public static class UserSettingsSingleton
    {
        private static UserSettings _userSettings;

        public static bool LogDiagnosticEvents { get; set; }

        public static UserSettings CurrentSettings()
        {
            return _userSettings ??= UserSettingsUtilities.ReadSettings(null).Result;
        }
    }
}