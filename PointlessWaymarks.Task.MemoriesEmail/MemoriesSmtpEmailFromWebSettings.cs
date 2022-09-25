using System.ComponentModel.DataAnnotations;

namespace PointlessWaymarks.Task.MemoriesEmail;

public record MemoriesSmtpEmailFromWebSettings
{

    public List<int> YearsBack { get; set; } = new() {10, 5, 2, 1};

    [Required(ErrorMessage = "Site Url Is Required - example: https://example.com")]
    public string SiteUrl { get; set; } = string.Empty;
    public DateTime ReferenceDate { get; set; } = DateTime.Now;
    public string BasicAuthUserName { get; set; } = string.Empty;
    public string BasicAuthPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "First Name Required")]
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;
    public string FromDisplayName { get; set; } = "Pointless Waymarks Memories";

    [Required(ErrorMessage = "A From Email Address is Required for SMTP Email")]
    public string FromEmailAddress { get; set; } = string.Empty;
    public string FromEmailPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "At Least One To Address is required - example: person@example.com;another@example.com;")]
    public string ToAddressList { get; set; } = string.Empty;
}