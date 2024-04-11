namespace PointlessWaymarks.CmsData.Database.Models;

public interface IOptionalLocation
{
    public double? Elevation { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool ShowLocation { get; set; }
}