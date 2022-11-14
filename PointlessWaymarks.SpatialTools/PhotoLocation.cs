﻿namespace PointlessWaymarks.SpatialTools;

public class PhotoLocation
{
    public double? Elevation { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool HasValidLocation()
    {
        return Latitude != null && Longitude != null;
    }
}