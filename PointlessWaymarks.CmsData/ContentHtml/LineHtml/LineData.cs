using System.Text;
using System.Xml;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData.ContentHtml.LineHtml;

public static class LineData
{
    public static List<LineElevationPlotDataPoint> GenerateLineElevationDataList(List<CoordinateZ> lineCoordinates)
    {
        if (!lineCoordinates.Any()) return new List<LineElevationPlotDataPoint>();

        var returnList = new List<LineElevationPlotDataPoint> { new(0, lineCoordinates[0].Z) };

        if (lineCoordinates.Count == 1) return returnList;

        var totalDistance = 0D;

        for (var i = 1; i < lineCoordinates.Count; i++)
        {
            totalDistance += DistanceTools.GetDistanceInMeters(lineCoordinates[i - 1].X, lineCoordinates[i - 1].Y,
                lineCoordinates[i].X, lineCoordinates[i].Y);

            returnList.Add(new LineElevationPlotDataPoint(totalDistance, lineCoordinates[i].Z));
        }

        return returnList;
    }

    public static List<LineElevationPlotDataPoint> GenerateLineElevationDataList(LineContent lineContent)
    {
        if (string.IsNullOrWhiteSpace(lineContent.Line)) return new List<LineElevationPlotDataPoint>();

        return GenerateLineElevationDataList(
            LineTools.CoordinateListFromGeoJsonFeatureCollectionWithLinestring(lineContent.Line));
    }

    public static async Task<string> GenerateLineJson(string lineGeoJson, string title, string pageUrl)
    {
        var contentFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(lineGeoJson);

        var bounds = GeoJsonTools.GeometryBoundingBox(GeoJsonTools.GeoJsonToGeometries(lineGeoJson));

        var elevationPlot =
            GenerateLineElevationDataList(
                LineTools.CoordinateListFromGeoJsonFeatureCollectionWithLinestring(lineGeoJson));

        var jsonDto = new LineSiteJsonData(pageUrl,
            new GeoJsonData.SpatialBounds(bounds.MaxY, bounds.MaxX, bounds.MinY, bounds.MinX),
            contentFeatureCollection, elevationPlot);

        return await GeoJsonTools.SerializeWithGeoJsonSerializer(jsonDto);
    }

    public static async Task WriteJsonData(LineContent lineContent)
    {
        if (string.IsNullOrWhiteSpace(lineContent.Line))
            throw new ArgumentException(
                "WriteJsonData in LineData was given a LineContent with a null/blank/empty Line");

        var dataFileInfo = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalSiteLineDataDirectory().FullName,
            $"Line-{lineContent.ContentId}.json"));

        if (dataFileInfo.Exists)
        {
            dataFileInfo.Delete();
            dataFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName,
                await GenerateLineJson(lineContent.Line, lineContent.Title ?? string.Empty,
                    UserSettingsSingleton.CurrentSettings().LinePageUrl(lineContent)).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    public static async Task WriteGpxData(LineContent lineContent)
    {
        if (string.IsNullOrWhiteSpace(lineContent.Line))
            throw new ArgumentException(
                "WriteGpxData in LineData was given a LineContent with a null/blank/empty Line");

        var dataFileInfo = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalSiteLineDataDirectory().FullName,
            $"Line-{lineContent.ContentId}.gpx"));

        if (dataFileInfo.Exists)
        {
            dataFileInfo.Delete();
            dataFileInfo.Refresh();
        }

        var trackList = GpxTools.GpxTrackFromLineFeature(lineContent.FeatureFromGeoJsonLine()!,
            lineContent.RecordingStartedOnUtc, lineContent.Title ?? lineContent.RecordingEndedOnUtc?.ToString("yyyy MM dd") ?? lineContent.CreatedOn.ToString("yyyy MM dd"), string.Empty,
            lineContent.Summary ?? string.Empty).AsList();

        var textStream = new StringWriter();

        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, CloseOutput = true };
        await using var xmlWriter = XmlWriter.Create(textStream, writerSettings);
        GpxWriter.Write(xmlWriter, new GpxWriterSettings(), new GpxMetadata("Pointless Waymarks CMS"), null, null, trackList, null);
        xmlWriter.Close();

        await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName, textStream.ToString())
            .ConfigureAwait(false);
    }

    public record LineElevationPlotDataPoint(double DistanceFromOrigin, double? Elevation);

    public record LineSiteJsonData(string PageUrl, GeoJsonData.SpatialBounds Bounds, FeatureCollection GeoJson,
        List<LineElevationPlotDataPoint> ElevationPlotData);
}