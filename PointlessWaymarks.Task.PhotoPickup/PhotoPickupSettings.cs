using System.ComponentModel.DataAnnotations;

namespace PointlessWaymarks.Task.PhotoPickup;

public class PhotoPickupSettings
{
    public static string ProgramShortName = "Photo Pickup";

    [Required(ErrorMessage = "A directory to move processed Photo files into.")]
    public string PhotoPickupArchiveDirectory { get; set; } = string.Empty;

    [Required(ErrorMessage = "The directory to look in for jpg photos in.")]
    public string PhotoPickupDirectory { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Settings file for a Pointless Waymarks CMS Site must be specified.")]
    public string PointlessWaymarksSiteSettingsFileFullName { get; set; } = string.Empty;

    public bool RenameFileToTitle { get; set; }

    public bool ShowInMainSiteFeed { get; set; }
}