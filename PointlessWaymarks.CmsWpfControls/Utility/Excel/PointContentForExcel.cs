namespace PointlessWaymarks.CmsWpfControls.Utility.Excel;

public class PointContentDtoForExcel
{
    //2024/4/15 - In the current Excel code this is not used directly but is rather used
    //as a base structure for dynamically building a class that has a column for each
    //Point Detail.
    public string? BodyContent { get; set; }
    public string? BodyContentFormat { get; set; }
    public Guid ContentId { get; set; }
    public DateTime ContentVersion { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public double? Elevation { get; set; }
    public DateTime FeedOn { get; set; }
    public string? Folder { get; set; }
    public int Id { get; set; }
    public bool IsDraft { get; set; }
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid? MainPicture { get; set; }
    public string? MapIconName { get; set; }
    public string? MapLabel { get; set; }
    public string? MapMarkerColor { get; set; }
    public bool ShowInMainSiteFeed { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Tags { get; set; }
    public string? Title { get; set; }
    public string? UpdateNotes { get; set; }
    public string? UpdateNotesFormat { get; set; }
}