using System.Text.Json.Serialization;

namespace PointlessWaymarks.GarminConnect.Models;

public record GarminActivityDetails
{
    [JsonPropertyName("activityDetailMetrics")]
    public ActivityDetailMetric[] ActivityDetailMetrics { get; init; }

    [JsonPropertyName("activityId")] public long ActivityId { get; init; }

    [JsonPropertyName("detailsAvailable")] public bool DetailsAvailable { get; init; }

    [JsonPropertyName("geoPolylineDTO")] public GeoPolylineDto GeoPolylineDto { get; init; }

    [JsonPropertyName("heartRateDTOs")] public object HeartRateDtOs { get; init; }

    [JsonPropertyName("measurementCount")] public long MeasurementCount { get; init; }

    [JsonPropertyName("metricDescriptors")]
    public MetricDescriptor[] MetricDescriptors { get; init; }

    [JsonPropertyName("metricsCount")] public long MetricsCount { get; init; }
}

public record ActivityDetailMetric
{
    [JsonPropertyName("metrics")] public double[] Metrics { get; init; }
}

public record GeoPolylineDto
{
    [JsonPropertyName("endPoint")] public EndPoint EndPoint { get; init; }

    [JsonPropertyName("maxLat")] public double MaxLat { get; init; }

    [JsonPropertyName("maxLon")] public double MaxLon { get; init; }

    [JsonPropertyName("minLat")] public double MinLat { get; init; }

    [JsonPropertyName("minLon")] public double MinLon { get; init; }

    [JsonPropertyName("polyline")] public EndPoint[] Polyline { get; init; }

    [JsonPropertyName("startPoint")] public EndPoint StartPoint { get; init; }
}

public record EndPoint
{
    [JsonPropertyName("altitude")] public object Altitude { get; init; }

    [JsonPropertyName("cumulativeAscent")] public object CumulativeAscent { get; init; }

    [JsonPropertyName("cumulativeDescent")]
    public object CumulativeDescent { get; init; }

    [JsonPropertyName("distanceFromPreviousPoint")]
    public object DistanceFromPreviousPoint { get; init; }

    [JsonPropertyName("distanceInMeters")] public object DistanceInMeters { get; init; }

    [JsonPropertyName("extendedCoordinate")]
    public bool ExtendedCoordinate { get; init; }

    [JsonPropertyName("lat")] public double Lat { get; init; }

    [JsonPropertyName("lon")] public double Lon { get; init; }

    [JsonPropertyName("speed")] public double Speed { get; init; }

    [JsonPropertyName("time")] public long Time { get; init; }

    [JsonPropertyName("timerStart")] public bool TimerStart { get; init; }

    [JsonPropertyName("timerStop")] public bool TimerStop { get; init; }

    [JsonPropertyName("valid")] public bool Valid { get; init; }
}

public record MetricDescriptor
{
    [JsonPropertyName("appID")] public string AppId { get; init; }

    [JsonPropertyName("developerFieldNumber")]
    public long DeveloperFieldNumber { get; init; }

    [JsonPropertyName("key")] public string Key { get; init; }

    [JsonPropertyName("metricsIndex")] public long MetricsIndex { get; init; }

    [JsonPropertyName("unit")] public Unit Unit { get; init; }
}

public record Unit
{
    [JsonPropertyName("factor")] public double Factor { get; init; }

    [JsonPropertyName("id")] public long Id { get; init; }

    [JsonPropertyName("key")] public string Key { get; init; }
}