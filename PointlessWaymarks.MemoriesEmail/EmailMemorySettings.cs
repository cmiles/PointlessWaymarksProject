namespace PointlessWaymarks.MemoriesEmail;

public record EmailMemorySettings
{
    public int YearsBack { get; set; } = 1;
    public string SiteUrl { get; set; } = string.Empty;
    public DateTime ReferenceDate { get; set; } = DateTime.Now;
    public string BasicAuthUserName { get; set; } = string.Empty;
    public string BasicAuthPassword { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;
    public string FromDisplayName { get; set; } = string.Empty;

    public string FromEmailAddress { get; set; } = string.Empty;
    public string FromEmailPassword { get; set; } = string.Empty;

    public string ToAddressList { get; set; } = string.Empty;
}