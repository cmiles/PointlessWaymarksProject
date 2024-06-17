using System.ComponentModel.DataAnnotations;
using PointlessWaymarks.VaultfuscationTools;

namespace PointlessWaymarks.Task.GarminConnectGpxImport;

public class GarminConnectGpxImportSettings : ISettingsFileType
{
    public const string SettingsTypeIdentifier = nameof(GarminConnectGpxImportSettings);
    public string SettingsType { get; set; } = SettingsTypeIdentifier;

    public const string ProgramShortName = "Garmin Connect Gpx Import";
    public string ConnectPassword { get; set; } = string.Empty;
    public string ConnectUserName { get; set; } = string.Empty;
    public int DownloadDaysBack { get; set; } = 1;
    public bool GenerateSiteAndUploadAfterImport { get; set; }
    public string GpxArchiveDirectoryFullName { get; set; } = string.Empty;
    public bool ImportActivitiesToSite { get; set; } = true;
    public string IntersectionTagSettings { get; set; } = string.Empty;
    public bool OverwriteExistingArchiveDirectoryFiles { get; set; }
    public string PointlessWaymarksSiteSettingsFileFullName { get; set; } = string.Empty;
    public bool ShowInMainSiteFeed { get; set; }

    public static string PasswordVaultResourceIdentifier(string loginCode)
    {
        return $"Pointless--{loginCode}--GarminConnect";
    }
}