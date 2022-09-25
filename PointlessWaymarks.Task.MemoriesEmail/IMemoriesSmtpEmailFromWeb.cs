namespace PointlessWaymarks.Task.MemoriesEmail;

public interface IMemoriesSmtpEmailFromWeb
{
    System.Threading.Tasks.Task GenerateEmail(string settingsFile);
}