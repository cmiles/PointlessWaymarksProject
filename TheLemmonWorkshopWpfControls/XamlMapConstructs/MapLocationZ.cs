using MapControl;

namespace TheLemmonWorkshopWpfControls.XamlMapConstructs
{
    public class MapLocationZ : Location
    {
        public MapLocationZ(double latitude, double longitude, double? elevation)
        {
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
        }

        public double? Elevation { get; set; }
    }
}