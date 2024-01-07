using System.ComponentModel.DataAnnotations;

namespace PointlessWaymarks.Task.MemoriesEmail;

public record MemoriesSmtpEmailFromWebSettings
{
    public static string ProgramShortName = "Memories Email";
    public string BasicAuthPassword { get; set; } = string.Empty;
    public string BasicAuthUserName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromDisplayName { get; set; } = "Pointless Waymarks Memories";
    public string FromEmailAddress { get; set; } = string.Empty;
    public string FromEmailPassword { get; set; } = string.Empty;
    public string LoginCode { get; set; } = string.Empty;
    public DateTime ReferenceDate { get; set; } = DateTime.Now;
    [Required(ErrorMessage = "Site Url Is Required - example: https://example.com")]
    public string SiteUrl { get; set; } = string.Empty;
    [Required(ErrorMessage = "Host Name Required")]
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    [Required(ErrorMessage = "At Least One To Address is required - example: person@example.com;another@example.com;")]
    public string ToAddressList { get; set; } = string.Empty;
    public List<int> YearsBack { get; set; } = [10, 5, 2, 1];

    public static string PasswordVaultResourceIdentifier(string loginCode)
    {
        return $"Pointless--{loginCode}--MemoriesEmail";
    }
}