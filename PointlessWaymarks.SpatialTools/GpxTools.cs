using System.Collections.Immutable;
using System.Globalization;
using GeoTimeZone;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.SpatialTools;

public static class GpxTools
{
    public static GpxTrack GpxTrackFromLineFeature(IFeature line, DateTime? utcStart, string name, string comment = "",
        string description = "")
    {
        var pointList = new List<GpxWaypoint>();
        var trackClock = DateTime.SpecifyKind(utcStart ?? DateTime.UtcNow, DateTimeKind.Utc);

        pointList.Add(new GpxWaypoint(new GpxLongitude(line.Geometry.Coordinates[0].X!),
                new GpxLatitude(line.Geometry.Coordinates[0].Y!), line.Geometry.Coordinates[0].Z)
            .WithTimestampUtc(trackClock));

        for (var i = 1; i < line.Geometry.Coordinates.Length; i++)
        {
            var startPoint = line.Geometry.Coordinates[i - 1]!;
            var endPoint = line.Geometry.Coordinates[i]!;

            var distance = DistanceTools.GetDistanceInMeters(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y)
                .MetersToMiles();

            var time = Math.Max(TimeSpan.FromHours(distance / 2).TotalSeconds, 1);
            trackClock = trackClock.AddSeconds(time);

            pointList.Add(new GpxWaypoint(new GpxLongitude(line.Geometry.Coordinates[i].X!),
                    new GpxLatitude(line.Geometry.Coordinates[i].Y!), line.Geometry.Coordinates[i].Z)
                .WithTimestampUtc(trackClock));
        }

        var trackSegment = new GpxTrackSegment(new ImmutableGpxWaypointTable(pointList), new object());

        return new GpxTrack(name, "Test", description, "Pointless Waymarks CMS",
            ImmutableArray<GpxWebLink>.Empty, null, "Test", null, [trackSegment]);
    }

    public static Feature LineFeatureFromGpxRoute(GpxRouteInformation routeInformation)
    {
        // ReSharper disable once CoVariantArrayConversion
        var newLine = new LineString(routeInformation.Track.ToArray());
        var feature = new Feature
        {
            Geometry = newLine,
            BoundingBox = GeoJsonTools.GeometryBoundingBox([newLine]),
            Attributes = new AttributesTable()
        };

        feature.Attributes.Add("title", routeInformation.Name);
        feature.Attributes.Add("description", routeInformation.Description);

        return feature;
    }

    public static Feature LineFeatureFromGpxTrack(GpxTrackInformation trackInformation)
    {
        // ReSharper disable once CoVariantArrayConversion
        var newLine = new LineString(trackInformation.Track.ToArray());
        var feature = new Feature
        {
            Geometry = newLine,
            BoundingBox = GeoJsonTools.GeometryBoundingBox([newLine]),
            Attributes = new AttributesTable()
        };

        feature.Attributes.Add("title", trackInformation.Name);
        feature.Attributes.Add("description", trackInformation.Description);

        return feature;
    }

    public static GpxRouteInformation RouteInformationFromGpxRoute(GpxRoute toConvert)
    {
        var name = toConvert.Name ?? string.Empty;

        var description = toConvert.Description ?? string.Empty;
        var comment = toConvert.Comment ?? string.Empty;
        var type = string.Empty;
        var label = string.Empty;

        var extensions = toConvert.Extensions;

        if (extensions is ImmutableXElementContainer extensionsContainer)
        {
            label = extensionsContainer
                .FirstOrDefault(x => x.Name.LocalName.ToLower() == "label")?.Value ?? string.Empty;
            type = extensionsContainer
                .FirstOrDefault(x => x.Name.LocalName.ToLower() == "type")?.Value ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(type))
            {
                var caseTextInfo = new CultureInfo("en-US", false).TextInfo;
                type = caseTextInfo.ToTitleCase(type.Replace("_", " "));
            }
        }

        var pointList = new List<CoordinateZ>();

        pointList.AddRange(toConvert.Waypoints.Select(x =>
            new CoordinateZ(x.Longitude.Value, x.Latitude.Value, x.ElevationInMeters ?? 0)));

        var nameAndLabelAndTypeList =
            new List<string> { name, label, type }
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var nameAndLabelAndType = string.Join(" - ", nameAndLabelAndTypeList);

        var descriptionAndCommentList = new List<string> { description, comment }
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var descriptionAndComment = string.Join(". ", descriptionAndCommentList);

        return new GpxRouteInformation(nameAndLabelAndType, descriptionAndComment, pointList);
    }

    public static async Task<(List<Feature> features, Envelope boundingBox)> RouteLinesFromGpxFile(FileInfo gpxFile)
    {
        var gpxInfo = await RoutesFromGpxFile(gpxFile);

        var featureCollection = new List<Feature>();
        var boundingBox = new Envelope();

        foreach (var loopGpxInfo in gpxInfo)
        {
            var feature = LineFeatureFromGpxRoute(loopGpxInfo);
            boundingBox.ExpandToInclude(feature.BoundingBox);
            featureCollection.Add(feature);
        }

        return (featureCollection, boundingBox);
    }

    public static async Task<List<GpxRouteInformation>> RoutesFromGpxFile(
        FileInfo gpxFile, IProgress<string>? progress = null)
    {
        var returnList = new List<GpxRouteInformation>();

        if (gpxFile is not { Exists: true }) return returnList;

        GpxFile parsedGpx;

        try
        {
            parsedGpx = GpxFile.Parse(await File.ReadAllTextAsync(gpxFile.FullName).ConfigureAwait(false),
                new GpxReaderSettings
                {
                    IgnoreUnexpectedChildrenOfTopLevelElement = true,
                    IgnoreVersionAttribute = true
                });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        var trackCounter = 0;

        foreach (var loopRoutes in parsedGpx.Routes)
        {
            trackCounter++;
            progress?.Report($"Extracting Route {trackCounter} of {parsedGpx.Tracks.Count} in {gpxFile.FullName}");
            returnList.Add(RouteInformationFromGpxRoute(loopRoutes));
        }

        return returnList;
    }

    public static GpxTrackInformation TrackInformationFromGpxTrack(GpxTrack toConvert)
    {
        var name = toConvert.Name ?? string.Empty;

        var description = toConvert.Description ?? string.Empty;
        var comment = toConvert.Comment ?? string.Empty;
        var type = string.Empty;
        var label = string.Empty;

        var extensions = toConvert.Extensions;

        if (extensions is ImmutableXElementContainer extensionsContainer)
        {
            label = extensionsContainer
                .FirstOrDefault(x => x.Name.LocalName.ToLower() == "label")?.Value ?? string.Empty;
            type = extensionsContainer
                .FirstOrDefault(x => x.Name.LocalName.ToLower() == "type")?.Value ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(type))
            {
                var caseTextInfo = new CultureInfo("en-US", false).TextInfo;
                type = caseTextInfo.ToTitleCase(type.Replace("_", " "));
            }
        }

        var pointList = new List<CoordinateZ>();

        foreach (var loopSegments in toConvert.Segments)
            pointList.AddRange(loopSegments.Waypoints.Select(x =>
                new CoordinateZ(x.Longitude.Value, x.Latitude.Value, x.ElevationInMeters ?? 0)));

        var firstPoint = toConvert.Segments.FirstOrDefault()?.Waypoints.FirstOrDefault();
        var lastPoint = toConvert.Segments.LastOrDefault()?.Waypoints.LastOrDefault();

        DateTime? startDateTimeLocal = null;
        DateTime? endDateTimeLocal = null;
        DateTime? startDateTimeUtc = null;
        DateTime? endDateTimeUtc = null;

        if (firstPoint?.TimestampUtc != null && lastPoint?.TimestampUtc != null)
        {
            startDateTimeUtc = firstPoint.TimestampUtc;
            endDateTimeUtc = lastPoint.TimestampUtc;

            var startTimezoneIanaIdentifier
                = TimeZoneLookup.GetTimeZone(firstPoint.Latitude, firstPoint.Longitude);
            var startTimeZone = TimeZoneInfo.FindSystemTimeZoneById(startTimezoneIanaIdentifier.Result);
            var startUtcOffset = startTimeZone.GetUtcOffset(firstPoint.TimestampUtc.Value);
            startDateTimeLocal = startDateTimeUtc.Value.Add(startUtcOffset);

            var endTimezoneIanaIdentifier
                = TimeZoneLookup.GetTimeZone(lastPoint.Latitude, lastPoint.Longitude);
            var endTimeZone = TimeZoneInfo.FindSystemTimeZoneById(endTimezoneIanaIdentifier.Result);
            var endUtcOffset = endTimeZone.GetUtcOffset(lastPoint.TimestampUtc.Value);
            endDateTimeLocal = endDateTimeUtc.Value.Add(endUtcOffset);
        }

        var nameAndLabelAndTypeList =
            new List<string> { name, label, type, startDateTimeLocal?.ToString("M/d/yyyy") ?? string.Empty }
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var nameAndLabelAndType = string.Join(" - ", nameAndLabelAndTypeList);

        var descriptionAndCommentList = new List<string> { description, comment }
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var descriptionAndComment = string.Join(". ", descriptionAndCommentList);

        return new GpxTrackInformation(nameAndLabelAndType, descriptionAndComment, startDateTimeLocal, endDateTimeLocal,
            startDateTimeUtc, endDateTimeUtc, pointList);
    }

    public static async Task<(List<Feature> features, Envelope boundingBox)> TrackLinesFromGpxFile(FileInfo gpxFile)
    {
        var gpxInfo = await TracksFromGpxFile(gpxFile);

        var featureCollection = new List<Feature>();
        var boundingBox = new Envelope();

        foreach (var loopGpxInfo in gpxInfo)
        {
            var feature = LineFeatureFromGpxTrack(loopGpxInfo);
            boundingBox.ExpandToInclude(feature.BoundingBox);
            featureCollection.Add(feature);
        }

        return (featureCollection, boundingBox);
    }

    public static async Task<List<GpxTrackInformation>> TracksFromGpxFile(
        FileInfo gpxFile, IProgress<string>? progress = null)
    {
        var returnList = new List<GpxTrackInformation>();

        if (gpxFile is not { Exists: true }) return returnList;

        var parsedGpx = GpxFile.Parse(await File.ReadAllTextAsync(gpxFile.FullName).ConfigureAwait(false),
            new GpxReaderSettings
            {
                IgnoreUnexpectedChildrenOfTopLevelElement = true,
                IgnoreVersionAttribute = true
            });

        var trackCounter = 0;

        foreach (var loopTracks in parsedGpx.Tracks)
        {
            trackCounter++;
            progress?.Report($"Extracting Track {trackCounter} of {parsedGpx.Tracks.Count} in {gpxFile.FullName}");
            returnList.Add(TrackInformationFromGpxTrack(loopTracks));
        }

        return returnList;
    }

    public static async Task<(List<Feature> features, Envelope boundingBox)> WaypointPointsFromGpxFile(FileInfo gpxFile)
    {
        var parsedGpx = GpxFile.Parse(await File.ReadAllTextAsync(gpxFile.FullName).ConfigureAwait(false),
            new GpxReaderSettings
            {
                IgnoreUnexpectedChildrenOfTopLevelElement = true,
                IgnoreVersionAttribute = true
            });

        var returnList = new List<Feature>();

        var bounds = new Envelope();

        foreach (var loopWaypoint in parsedGpx.Waypoints)
        {
            var attributeTable = new AttributesTable
            {
                { "title", loopWaypoint.Name },
                { "description", loopWaypoint.Description }
            };

            if (loopWaypoint.ElevationInMeters == null)
            {
                var point = PointTools.Wgs84Point(loopWaypoint.Longitude, loopWaypoint.Latitude);
                returnList.Add(new Feature(point, attributeTable));
                bounds.ExpandToInclude(point.Coordinate);
            }
            else
            {
                var point = PointTools.Wgs84Point(loopWaypoint.Longitude, loopWaypoint.Latitude,
                    loopWaypoint.ElevationInMeters.Value);
                returnList.Add(new Feature(point, attributeTable));
                bounds.ExpandToInclude(point.Coordinate);
            }
        }

        return (returnList, bounds);
    }

    public record GpxRouteInformation(string Name, string Description, List<CoordinateZ> Track);

    public record GpxTrackInformation(string Name, string Description, DateTime? StartsOnLocal, DateTime? EndsOnLocal,
        DateTime? StartsOnUtc, DateTime? EndsOnUtc, List<CoordinateZ> Track);
}