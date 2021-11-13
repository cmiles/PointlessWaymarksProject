namespace PointlessWaymarks.CmsData.Database.Models;

public class GenerationFileTransferScriptLog
{
    public string? FileName { get; set; }
    public int Id { get; set; }
    public DateTime WrittenOnVersion { get; set; }
}