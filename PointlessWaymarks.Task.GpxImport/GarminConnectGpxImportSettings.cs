using System.ComponentModel.DataAnnotations;

namespace PointlessWaymarks.Task.GarminConnectGpxImport;

public class GarminConnectGpxImportSettings
{
    [Required(ErrorMessage = "A Password for Garmin Connect must be specified.")]
    public string ConnectPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Username (email) for Garmin Connect must be specified.")]
    public string ConnectUserName { get; set; } = string.Empty;

    [RegularExpression("(.*[1-9].*)|(.*[.].*[1-9].*)",
        ErrorMessage = "A non-zero value must be given (or use the default of 1)")]
    public int DownloadDaysBack { get; set; } = 1;

    public DateTime DownloadEndDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "A location for archived GPX files must be given.")]
    public string GpxArchiveDirectoryFullName { get; set; } = string.Empty;

    public bool ImportActivitiesToSite { get; set; } = true;

    public bool OverwriteExistingArchiveDirectoryFiles { get; set; }

    [Required(ErrorMessage = "A Settings file for a Pointless Waymarks CMS Site must be specified.")]
    public string PointlessWaymarksSiteSettingsFileFullName { get; set; } = string.Empty;

    public bool ShowInMainSiteFeed { get; set; }
}