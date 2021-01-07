using System;

namespace PointlessWaymarks.CmsWpfControls.PointContentEditor
{
    public class PointLatitudeLongitudeChange : EventArgs
    {
        public PointLatitudeLongitudeChange(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}