using NetTopologySuite.Geometries;

namespace PointlessWaymarksCmsWpfControls.GeoDataPicker
{
    public class SelectedGeoData
    {
        public Geometry GeoData { get; set; }
        public string GeoType { get; set; }
    }
}