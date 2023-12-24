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

    public static SpatialBounds FromEnvelope(Envelope envelope)
    {
        return new SpatialBounds(envelope.MaxY, envelope.MaxX, envelope.MinY, envelope.MinX);
    }

    public Envelope ToEnvelope(double? minimumInMeters = null)
    {
        var currentEnvelope = new Envelope(MinLongitude, MinLatitude, MaxLongitude, MaxLatitude);
        if (minimumInMeters is not null) DistanceTools.MinimumEnvelopeInMeters(currentEnvelope, minimumInMeters.Value);
        return currentEnvelope;
    }
}