using NetTopologySuite.Geometries;

namespace TheLemmonWorkshopWpfControls.GeoDataPicker
{
    public class SelectedGeoData
    {
        public Geometry GeoData { get; set; }
        public string GeoType { get; set; }
    }
}