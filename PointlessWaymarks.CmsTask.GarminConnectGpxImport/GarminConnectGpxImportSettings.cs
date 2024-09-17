using PointlessWaymarks.VaultfuscationTools;

namespace PointlessWaymarks.CmsTask.GarminConnectGpxImport;

public class GarminConnectGpxImportSettings : ISettingsFileType
{
    public string ConnectPassword { get; set; } = string.Empty;
    public string ConnectUserName { get; set; } = string.Empty;
    public int DownloadDaysBack { get; set; } = 1;
    public bool GenerateSiteAndUploadAfterImport { get; set; }
    public string GpxArchiveDirectoryFullName { get; set; } = string.Empty;
    public bool ImportActivitiesToSite { get; set; } = true;
    public bool OverwriteExistingArchiveDirectoryFiles { get; set; }
    public string PointlessWaymarksSiteSettingsFileFullName { get; set; } = string.Empty;
    public bool ShowInMainSiteFeed { get; set; }
    public string SettingsType { get; set; } = SettingsTypeIdentifier();

    public static string PasswordVaultResourceIdentifier(string loginCode)
    {
        return $"Pointless--{loginCode}--GarminConnect";
    }

    public static string ProgramShortName()
    {
        return "Garmin Connect Gpx Import";
    }

    public static string SettingsTypeIdentifier()
    {
        return nameof(GarminConnectGpxImportSettings);
    }
}