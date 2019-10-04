namespace TheLemmonWorkshopWpfControls.XamlMapConstructs
{
    public class MapLocationM : MapControl.Location
    {
        public MapLocationM(double latitude, double longitude, double? elevation)
        {
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
        }

        public double? Elevation { get; set; }
    }
}