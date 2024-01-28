using System.Text;
using System.Xml;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using PointlessWaymarks.CmsData.CommonHtml;
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
        if (lineCoordinates.Count == 0) return [];

        var returnList = new List<LineElevationPlotDataPoint>
            { new(0, lineCoordinates[0].Z, 0, 0, lineCoordinates[0].Y, lineCoordinates[0].X) };

        if (lineCoordinates.Count == 1) return returnList;

        var accumulatedDistance = 0D;
        var accumulatedClimb = 0D;
        var accumulatedDescent = 0D;

        for (var i = 1; i < lineCoordinates.Count; i++)
        {
            var elevationChange = lineCoordinates[i - 1].Z - lineCoordinates[i].Z;
            switch (elevationChange)
            {
                case > 0:
                    accumulatedClimb += elevationChange;
                    break;
                case < 0:
                    accumulatedDescent += elevationChange;
                    break;
            }

            accumulatedDistance += DistanceTools.GetDistanceInMeters(lineCoordinates[i - 1].X, lineCoordinates[i - 1].Y,
                lineCoordinates[i].X, lineCoordinates[i].Y);

            returnList.Add(new LineElevationPlotDataPoint(accumulatedDistance, lineCoordinates[i].Z, accumulatedClimb,
                accumulatedDescent, lineCoordinates[i].Y, lineCoordinates[i].X));
        }

        return returnList;
    }

    public static List<LineElevationPlotDataPoint> GenerateLineElevationDataList(LineContent lineContent)
    {
        if (string.IsNullOrWhiteSpace(lineContent.Line)) return [];

        return GenerateLineElevationDataList(
            LineTools.CoordinateListFromGeoJsonFeatureCollectionWithLinestring(lineContent.Line));
    }

    public static async Task<string> GenerateLineJson(string lineGeoJson, string title, string pageUrl,
        string smallImageUrl)
    {
        var dto = GenerateLineJsonDto(lineGeoJson, title, pageUrl, smallImageUrl);

        return await GeoJsonTools.SerializeWithGeoJsonSerializer(dto);
    }

    public static LineSiteJsonData GenerateLineJsonDto(string lineGeoJson, string title, string pageUrl,
        string smallImageUrl)
    {
        var contentFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(lineGeoJson);

        var bounds = GeoJsonTools.GeometryBoundingBox(GeoJsonTools.GeoJsonToGeometries(lineGeoJson));

        var elevationPlot =
            GenerateLineElevationDataList(
                LineTools.CoordinateListFromGeoJsonFeatureCollectionWithLinestring(lineGeoJson));

        return new LineSiteJsonData(pageUrl, smallImageUrl,
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
                    Log.ForContext("exception", e.ToString())
                        .Debug("Ignored Temporary File Delete Exception in {methodName}", nameof(WriteGpxData));
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

        var smallPictureUrl = lineContent.MainPicture == null
            ? string.Empty
            : new PictureSiteInformation(lineContent.MainPicture.Value).Pictures?.SmallPicture?.SiteUrl ?? string.Empty;

        var currentDto = GenerateLineJsonDto(lineContent.Line, lineContent.Title ?? string.Empty,
            UserSettingsSingleton.CurrentSettings().LinePageUrl(lineContent), smallPictureUrl);
        var currentSerializedDto = await GeoJsonTools.SerializeWithGeoJsonSerializer(currentDto);

        //If the file exists and the data is the same - don't write it again
        if (dataFileInfo.Exists)
        {
            var onDiskDto = await File.ReadAllTextAsync(dataFileInfo.FullName);

            if (onDiskDto == currentSerializedDto) return;
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

    public record LineElevationPlotDataPoint(
        double AccumulatedDistance,
        double? Elevation,
        double AccumulatedClimb,
        double AccumulatedDescent,
        double Latitude,
        double Longitude);

    public record LineSiteJsonData(
        string PageUrl,
        string SmallPictureUrl,
        SpatialBounds Bounds,
        FeatureCollection GeoJson,
        List<LineElevationPlotDataPoint> ElevationPlotData);
}