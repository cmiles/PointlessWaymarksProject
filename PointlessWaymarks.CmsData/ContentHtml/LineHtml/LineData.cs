using System.Text;
using System.Xml;
using KellermanSoftware.CompareNetObjects;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;
using Serilog;

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
        var dto = GenerateLineJsonDto(lineGeoJson, title, pageUrl);

        return await GeoJsonTools.SerializeWithGeoJsonSerializer(dto);
    }

    public static LineSiteJsonData GenerateLineJsonDto(string lineGeoJson, string title, string pageUrl)
    {
        var contentFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(lineGeoJson);

        var bounds = GeoJsonTools.GeometryBoundingBox(GeoJsonTools.GeoJsonToGeometries(lineGeoJson));

        var elevationPlot =
            GenerateLineElevationDataList(
                LineTools.CoordinateListFromGeoJsonFeatureCollectionWithLinestring(lineGeoJson));

        return new LineSiteJsonData(pageUrl,
            SpatialBounds.FromEnvelope(bounds),
            contentFeatureCollection, elevationPlot);
    }

    public static async Task WriteGpxData(LineContent lineContent)
    {
        if (string.IsNullOrWhiteSpace(lineContent.Line))
            throw new ArgumentException(
                "WriteGpxData in LineData was given a LineContent with a null/blank/empty Line");

        var dataFileInfo = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalSiteLineDataDirectory().FullName,
            $"Line-{lineContent.ContentId}.gpx"));

        var trackList = GpxTools.GpxTrackFromLineFeature(lineContent.FeatureFromGeoJsonLine()!,
            lineContent.RecordingStartedOnUtc,
            lineContent.Title ?? lineContent.RecordingEndedOnUtc?.ToString("yyyy MM dd") ??
            lineContent.CreatedOn.ToString("yyyy MM dd"), string.Empty,
            lineContent.Summary ?? string.Empty).AsList();

        var textStream = new StringWriter();

        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, CloseOutput = true };
        await using var xmlWriter = XmlWriter.Create(textStream, writerSettings);
        GpxWriter.Write(xmlWriter, new GpxWriterSettings(), new GpxMetadata("Pointless Waymarks CMS"), null, null,
            trackList, null);
        xmlWriter.Close();

        var temporaryGpxFile = UniqueFileTools.UniqueFile(FileLocationTools.TempStorageDirectory(),
            $"GpxDataTemp-{DateTime.Now:yyyyMMddHHmmssfff}.gpx");

        await File.WriteAllTextAsync(temporaryGpxFile!.FullName, textStream.ToString());
        temporaryGpxFile.Refresh();

        if (dataFileInfo.Exists)
        {
            var temporaryMd5 = temporaryGpxFile.CalculateMD5();
            var onDiskMd5 = dataFileInfo.CalculateMD5();

            if (temporaryMd5 == onDiskMd5)
            {
                try
                {
                    temporaryGpxFile.Delete();
                }
                catch (Exception e)
                {
                    Log.Debug("Ignored Temporary File Delete Exception in {methodName}", nameof(WriteGpxData));
                }

                return;
            }   
        }

        if (dataFileInfo.Exists)
        {
            dataFileInfo.Delete();
            dataFileInfo.Refresh();
        }
        
        temporaryGpxFile.MoveTo(dataFileInfo.FullName);
    }

    public static async Task WriteJsonData(LineContent lineContent)
    {
        if (string.IsNullOrWhiteSpace(lineContent.Line))
            throw new ArgumentException(
                "WriteJsonData in LineData was given a LineContent with a null/blank/empty Line");

        var dataFileInfo = new FileInfo(Path.Combine(
            UserSettingsSingleton.CurrentSettings().LocalSiteLineDataDirectory().FullName,
            $"Line-{lineContent.ContentId}.json"));

        var currentDto = GenerateLineJsonDto(lineContent.Line, lineContent.Title ?? string.Empty,
            UserSettingsSingleton.CurrentSettings().LinePageUrl(lineContent));

        //If the file exists and the data is the same - don't write it again
        if (dataFileInfo.Exists)
        {
            var onDiskDto = GeoJsonTools.DeserializeWithGeoJsonSerializer<LineSiteJsonData>(
                await File.ReadAllTextAsync(dataFileInfo.FullName));

            if (onDiskDto is not null)
                if (onDiskDto.PageUrl == currentDto.PageUrl
                    && new CompareLogic().Compare(onDiskDto.Bounds, currentDto.Bounds).AreEqual
                    && new CompareLogic().Compare(onDiskDto.ElevationPlotData, currentDto.ElevationPlotData).AreEqual)
                {
                    var onDiskLineGeometry = onDiskDto.GeoJson.FirstOrDefault()?.Geometry;
                    var currentLineGeometry = currentDto.GeoJson.FirstOrDefault()?.Geometry;

                    if (onDiskLineGeometry is not null && currentLineGeometry is not null)
                        if (onDiskLineGeometry.EqualsNormalized(currentLineGeometry))
                            return;
                }
        }

        if (dataFileInfo.Exists)
        {
            dataFileInfo.Delete();
            dataFileInfo.Refresh();
        }

        await FileManagement.WriteAllTextToFileAndLogAsync(dataFileInfo.FullName,
                await GeoJsonTools.SerializeWithGeoJsonSerializer(currentDto))
            .ConfigureAwait(false);
    }

    public record LineElevationPlotDataPoint(double DistanceFromOrigin, double? Elevation);

    public record LineSiteJsonData(
        string PageUrl,
        SpatialBounds Bounds,
        FeatureCollection GeoJson,
        List<LineElevationPlotDataPoint> ElevationPlotData);
}