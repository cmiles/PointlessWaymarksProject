using PointlessWaymarks.VaultfuscationTools;

namespace PointlessWaymarks.Task.PhotoPickup;

public class PhotoPickupSettings : ISettingsFileType
{
    public string PhotoPickupArchiveDirectory { get; set; } = string.Empty;

    public string PhotoPickupDirectory { get; set; } = string.Empty;

    public string PointlessWaymarksSiteSettingsFileFullName { get; set; } = string.Empty;

    public bool RenameFileToTitle { get; set; }

    public bool ShowInMainSiteFeed { get; set; }

    public string SettingsType { get; set; } = SettingsTypeIdentifier();

    public static string PasswordVaultResourceIdentifier(string loginCode)
    {
        return $"Pointless--{loginCode}--PhotoPickup";
    }

    public static string ProgramShortName()
    {
        return "Photo Pickup";
    }

    public static string SettingsTypeIdentifier()
    {
        return nameof(PhotoPickupSettings);
    }
}