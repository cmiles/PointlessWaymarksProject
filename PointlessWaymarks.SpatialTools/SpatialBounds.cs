using NetTopologySuite.Geometries;

namespace PointlessWaymarks.SpatialTools;

public record SpatialBounds(
    double MaxLatitude,
    double MaxLongitude,
    double MinLatitude,
    double MinLongitude)
{
    public SpatialBounds ExpandToMinimumMeters(double minimumInMeters)
    {
        return FromEnvelope(ToEnvelope(minimumInMeters));
    }

    public static SpatialBounds FromCoordinates(double latitude, double longitude, double sizeInMeters)
    {
        return
            new SpatialBounds(latitude, longitude, latitude, longitude).ExpandToMinimumMeters(sizeInMeters);
    }

    public static SpatialBounds FromEnvelope(Envelope envelope)
    {
        return new SpatialBounds(envelope.MaxY, envelope.MaxX, envelope.MinY, envelope.MinX);
    }

    public double Height()
    {
        return DistanceTools.GetDistanceInMeters(MaxLongitude, MinLatitude, MaxLongitude, MaxLatitude);
    }

    public Envelope ToEnvelope(double? minimumInMeters = null)
    {
        var currentEnvelope = new Envelope(MinLongitude, MaxLongitude, MinLatitude, MaxLatitude);
        if (minimumInMeters is not null) DistanceTools.MinimumEnvelopeInMeters(currentEnvelope, minimumInMeters.Value);
        return currentEnvelope;
    }

    public double Width()
    {
        return DistanceTools.GetDistanceInMeters(MinLongitude, MinLatitude, MaxLongitude, MinLatitude);
    }
}