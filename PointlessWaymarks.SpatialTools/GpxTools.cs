using System.Globalization;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace PointlessWaymarks.SpatialTools;

public static class GpxTools
{
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

        var startDateTime = toConvert.Segments.FirstOrDefault()?.Waypoints.FirstOrDefault()?.TimestampUtc
            ?.ToLocalTime();
        var endDateTime = toConvert.Segments.LastOrDefault()?.Waypoints.LastOrDefault()?.TimestampUtc
            ?.ToLocalTime();

        var nameAndLabelAndTypeList =
            new List<string> { name, label, type, startDateTime?.ToString("M/d/yyyy") ?? string.Empty }
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var nameAndLabelAndType = string.Join(" - ", nameAndLabelAndTypeList);

        var descriptionAndCommentList = new List<string> { description, comment }
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var descriptionAndComment = string.Join(". ", descriptionAndCommentList);

        return new GpxTrackInformation(nameAndLabelAndType, descriptionAndComment, startDateTime,
            endDateTime, pointList);
    }

    public static async Task<List<GpxTrackInformation>> TracksFromGpxFile(
        FileInfo gpxFile, IProgress<string>? progress = null)
    {
        var returnList = new List<GpxTrackInformation>();

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

        foreach (var loopTracks in parsedGpx.Tracks)
        {
            trackCounter++;
            progress?.Report($"Extracting Track {trackCounter} of {parsedGpx.Tracks.Count} in {gpxFile.FullName}");
            returnList.Add(TrackInformationFromGpxTrack(loopTracks));
        }

        return returnList;
    }

    public record GpxRouteInformation(string Name, string Description, List<CoordinateZ> Track);

    public record GpxTrackInformation(string Name, string Description, DateTime? StartsOn, DateTime? EndsOn,
        List<CoordinateZ> Track);
}