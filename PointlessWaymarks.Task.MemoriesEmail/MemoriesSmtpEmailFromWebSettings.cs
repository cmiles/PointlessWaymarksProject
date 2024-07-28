using PointlessWaymarks.VaultfuscationTools;

namespace PointlessWaymarks.Task.MemoriesEmail;

public record MemoriesSmtpEmailFromWebSettings : ISettingsFileType
{
    public string BasicAuthPassword { get; set; } = string.Empty;
    public string BasicAuthUserName { get; set; } = string.Empty;
    public string FromEmailAddress { get; set; } = string.Empty;
    public string FromEmailDisplayName { get; set; } = string.Empty;
    public string FromEmailPassword { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
    public bool SmtpEnableSsl { get; set; } = true;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string ToAddressList { get; set; } = string.Empty;
    public List<int> YearsBack { get; set; } = [20, 15, 10, 5, 2, 1];

    public string SettingsType { get; set; } = SettingsTypeIdentifier();

    public static string PasswordVaultResourceIdentifier(string loginCode)
    {
        return $"Pointless--{loginCode}--MemoriesEmail";
    }

    public static string ProgramShortName()
    {
        return "Memories Email";
    }

    public static string SettingsTypeIdentifier()
    {
        return nameof(MemoriesSmtpEmailFromWebSettings);
    }
}