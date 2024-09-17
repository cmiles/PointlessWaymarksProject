using PointlessWaymarks.VaultfuscationTools;

namespace PointlessWaymarks.CmsTask.PublishSiteToS3;

public class PublishToS3Settings : ISettingsFileType
{
    public string PointlessWaymarksSiteSettingsFileFullName { get; set; } = string.Empty;

    public string SettingsType { get; set; } = SettingsTypeIdentifier();

    public static string PasswordVaultResourceIdentifier(string loginCode)
    {
        return $"Pointless--{loginCode}--PublishSiteToS3";
    }

    public static string ProgramShortName()
    {
        return "Publish Site to S3";
    }

    public static string SettingsTypeIdentifier()
    {
        return nameof(PublishToS3Settings);
    }
}