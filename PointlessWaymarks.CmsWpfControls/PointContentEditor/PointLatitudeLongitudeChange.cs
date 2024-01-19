namespace PointlessWaymarks.CmsWpfControls.PointContentEditor;

public class PointLatitudeLongitudeChange(double latitude, double longitude) : EventArgs
{
    public double Latitude { get; set; } = latitude;
    public double Longitude { get; set; } = longitude;
}