namespace PointlessWaymarks.MemoriesEmail;

public interface IMemoriesSmtpEmailFromWeb
{
    Task GenerateEmail(string settingsFile);
}