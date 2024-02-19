using System.IO;
using System.Net;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.CmsWpfControls.GeoJsonContentEditor;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.LineContentEditor;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.PhotoContentEditor;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.SitePreview;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;
using SimMetricsCore;

namespace PointlessWaymarks.CmsWpfControls.WpfCmsHtml;

public static class MapCmsJson
{
    private static (string description, string? imageFileToCopy) GenerateDescription(Guid? imageContentId,
        string? title,
        string? summary)
    {
        var description = string.Empty;

        title = title.TrimNullToEmpty();
        summary = summary.TrimNullToEmpty();

        var smallImageFile = imageContentId != null
            ? PictureAssetProcessing.ProcessPictureDirectory(imageContentId.Value)?.SmallPicture
                ?.File?.FullName
            : null;

        if (smallImageFile != null)
            description += $"""
                            <img src="https://[[VirtualDomain]]/{Path.GetFileName(smallImageFile)}"/>
                            """;

        if (!string.IsNullOrWhiteSpace(summary)
            && title.GetSimilarity(summary, SimMetricType.JaroWinkler) < .9 &&
            !(title.ContainsFuzzy(summary, 0.8, SimMetricType.JaroWinkler)
              || summary.ContainsFuzzy(title, 0.8, SimMetricType.JaroWinkler)))
            description += $" <p>{summary}</p>";

        return (description, smallImageFile);
    }

    public static Envelope GetBounds(List<IContentListItem> toMeasure)
    {
        var boundsKeeper = new List<Point>();

        foreach (var loopElements in toMeasure)
            switch (loopElements)
            {
                case GeoJsonListListItem { DbEntry.GeoJson: not null } mapGeoJson:
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMaxLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapGeoJson.DbEntry.InitialViewBoundsMinLongitude,
                        mapGeoJson.DbEntry.InitialViewBoundsMinLatitude));
                    break;
                case LineListListItem { DbEntry.Line: not null } mapLine:
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMaxLongitude,
                        mapLine.DbEntry.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapLine.DbEntry.InitialViewBoundsMinLongitude,
                        mapLine.DbEntry.InitialViewBoundsMinLatitude));
                    break;
            }

        if (toMeasure.Any(x => x is PointListListItem))
            foreach (var loopElements in toMeasure.Where(x => x is PointListListItem).Cast<PointListListItem>()
                         .ToList())
                boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude, loopElements.DbEntry.Latitude));

        if (toMeasure.Any(x => x is PhotoListListItem))
            foreach (var loopElements in toMeasure.Where(x => x is PhotoListListItem).Cast<PhotoListListItem>()
                         .ToList())
            {
                if (loopElements.DbEntry.Latitude is null || loopElements.DbEntry.Longitude is null) continue;

                boundsKeeper.Add(new Point(loopElements.DbEntry.Longitude.Value, loopElements.DbEntry.Latitude.Value));
            }

        return SpatialConverters.PointBoundingBox(boundsKeeper);
    }

    public static Action<Uri, string> LocalActionNavigation(StatusControlContext statusContext)
    {
        return (Uri navigationUri, string virtualDomain) =>
        {
            if (navigationUri.Segments.Last().Equals("LocalEdit", StringComparison.OrdinalIgnoreCase))
            {
                statusContext.RunFireAndForgetBlockingTask(async () =>
                {
                    await ThreadSwitcher.ResumeForegroundAsync();
                    var contentId = WebUtility.UrlDecode(navigationUri.Query[1..]);
                    var db = await Db.Context();
                    var content = await db.ContentFromContentId(Guid.Parse(contentId));

                    switch (content)
                    {
                        case PhotoContent photoContent:
                            var photoEditWindow = await PhotoContentEditorWindow.CreateInstance(photoContent);
                            await photoEditWindow.PositionWindowAndShowOnUiThread();
                            break;
                        case LineContent lineContent:
                            var lineEditWindow = await LineContentEditorWindow.CreateInstance(lineContent);
                            await lineEditWindow.PositionWindowAndShowOnUiThread();
                            break;
                        case PointContentDto pointContent:
                            var pointEditWindow =
                                await PointContentEditorWindow.CreateInstance(pointContent.ToDbObject());
                            await pointEditWindow.PositionWindowAndShowOnUiThread();
                            break;
                        case GeoJsonContent geoJsonContent:
                            var editWindow = await GeoJsonContentEditorWindow.CreateInstance(geoJsonContent);
                            await editWindow.PositionWindowAndShowOnUiThread();
                            break;
                        default:
                            statusContext.ToastError($"Content Not Found? {contentId}");
                            break;
                    }
                });

                return;
            }

            if (navigationUri.Segments.Last().Equals("LocalPreview", StringComparison.OrdinalIgnoreCase))
            {
                statusContext.RunFireAndForgetBlockingTask(async () =>
                {
                    await ThreadSwitcher.ResumeForegroundAsync();

                    var sitePreviewWindow =
                        await SiteOnDiskPreviewWindow.CreateInstance(
                            WebUtility.UrlDecode(navigationUri.Query[1..]));
                    await sitePreviewWindow.PositionWindowAndShowOnUiThread();
                });

                return;
            }

            statusContext.RunFireAndForgetBlockingTask(async () =>
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                ProcessHelpers.OpenUrlInExternalBrowser(navigationUri.OriginalString);
            });
        };
    }

    public static async Task<MapJsonNewFeatureCollectionDto> NewMapFeatureCollectionDto(
        List<FeatureCollection> featureCollections,
        SpatialBounds? bounds, string messageType = "NewFeatureCollectionAndCenter")
    {
        bounds ??= new SpatialBounds(await UserSettingsSingleton.CurrentSettings().DefaultLatitudeValidated(),
            await UserSettingsSingleton.CurrentSettings().DefaultLongitudeValidated(),
            await UserSettingsSingleton.CurrentSettings().DefaultLatitudeValidated(),
            await UserSettingsSingleton.CurrentSettings().DefaultLongitudeValidated());

        var expandedBounds = bounds.ExpandToMinimumMeters(1000);

        var mapJsonDto =
            new MapJsonNewFeatureCollectionDto(Guid.NewGuid(), expandedBounds, featureCollections, messageType);
        return mapJsonDto;
    }

    public static async Task<string> NewMapFeatureCollectionDtoSerialized(List<FeatureCollection> featureCollections,
        SpatialBounds? bounds, string messageType = "NewFeatureCollectionAndCenter")
    {
        var mapJsonDto =
            await GeoJsonTools.SerializeWithGeoJsonSerializer(
                await NewMapFeatureCollectionDto(featureCollections, bounds, messageType));

        await BracketCodeCommon.ProcessCodesForSite(mapJsonDto).ConfigureAwait(false);

        return mapJsonDto;
    }

    public static async Task<string> NewMapFeatureCollectionDtoSerialized(List<FeatureCollection> featureCollections,
        string messageType = "NewFeatureCollectionAndCenter")
    {
        var bounds = SpatialBounds.FromEnvelope(GeoJsonTools.GeometryBoundingBox(featureCollections));

        var mapJsonDto =
            await GeoJsonTools.SerializeWithGeoJsonSerializer(
                await NewMapFeatureCollectionDto(featureCollections, bounds, messageType));

        await BracketCodeCommon.ProcessCodesForSite(mapJsonDto).ConfigureAwait(false);

        return mapJsonDto;
    }

    public static async Task<string> NewMapFeatureCollectionDtoSerialized(
        string featureCollection)
    {
        var contentFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(featureCollection);

        var envelope = GeoJsonTools.GeometryBoundingBox(GeoJsonTools.GeoJsonToGeometries(featureCollection));

        return await NewMapFeatureCollectionDtoSerialized(contentFeatureCollection.AsList(),
            SpatialBounds.FromEnvelope(envelope));
    }

    public static async Task<(SpatialBounds bounds, List<FeatureCollection> featureList, List<string> fileCopyList)>
        ProcessContentToMapInformation(
            List<IContentListItem> frozenItems)
    {
        var dbEntries = new List<object>();

        foreach (var loopElements in frozenItems)
            switch (loopElements)
            {
                case GeoJsonListListItem { DbEntry.GeoJson: not null } geoJson:
                    dbEntries.Add(geoJson.DbEntry);
                    break;
                case LineListListItem { DbEntry.Line: not null } line:
                    dbEntries.Add(line.DbEntry);
                    break;
                case PointListListItem point:
                    dbEntries.Add(point.DbEntry);
                    break;
                case PhotoListListItem { DbEntry.Latitude: not null, DbEntry.Longitude: not null } photo:
                    dbEntries.Add(photo.DbEntry);
                    break;
            }

        return await ProcessContentToMapInformation(dbEntries);
    }

    public static async Task<(SpatialBounds bounds, List<FeatureCollection> featureList, List<string> fileCopyList)>
        ProcessContentToMapInformation(
            List<Guid> contentIds)
    {
        var db = await Db.Context();
        var content = await db.ContentFromContentIds(contentIds, true);

        return await ProcessContentToMapInformation(content.Cast<object>().ToList());
    }

    /// <summary>
    ///     If your processing starts with IContentListItems use the overload that takes those - this version must query the
    ///     database for point information and process on disk content for photo image information.
    /// </summary>
    /// <param name="dbEntries"></param>
    /// <returns></returns>
    public static async Task<(SpatialBounds bounds, List<FeatureCollection> featureList, List<string> fileCopyList)>
        ProcessContentToMapInformation(
            List<object> dbEntries)
    {
        var geoJsonList = new List<FeatureCollection>();

        var boundsKeeper = new List<Point>();
        var filesToCopy = new List<string>();

        foreach (var loopElements in dbEntries)
            switch (loopElements)
            {
                case GeoJsonContent { GeoJson: not null } mapGeoJson:
                    var featureCollection =
                        GeoJsonTools.DeserializeStringToFeatureCollection(mapGeoJson.GeoJson);
                    foreach (var feature in featureCollection)
                        feature.Attributes.Add("displayId", mapGeoJson.ContentId);
                    geoJsonList.Add(featureCollection);
                    boundsKeeper.Add(new Point(mapGeoJson.InitialViewBoundsMaxLongitude,
                        mapGeoJson.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapGeoJson.InitialViewBoundsMinLongitude,
                        mapGeoJson.InitialViewBoundsMinLatitude));
                    break;
                case LineContent mapLine:
                    var lineFeatureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(mapLine.Line);
                    var line = lineFeatureCollection[0];
                    line.Attributes.Add("displayId", mapLine.ContentId);
                    if (!line.Attributes.Exists("description")) line.Attributes.Add("description", string.Empty);

                    var descriptionAndImage = GenerateDescription(mapLine.MainPicture, mapLine.Title, mapLine.Summary);
                    if (!string.IsNullOrWhiteSpace(descriptionAndImage.imageFileToCopy))
                        filesToCopy.Add(descriptionAndImage.imageFileToCopy);

                    line.Attributes["description"] = descriptionAndImage.description;

                    if (!line.Attributes.Exists("title")) line.Attributes.Add("title", string.Empty);

                    line.Attributes["title"] =
                        $"""<a href="http://[[VirtualDomain]]/LocalPreview?{WebUtility.UrlEncode(UserSettingsSingleton.CurrentSettings().LinePageUrl(mapLine))}">{(string.IsNullOrWhiteSpace(mapLine.Title) ? "Preview" : mapLine.Title)}</a> <a href="http://[[VirtualDomain]]/LocalEdit?{WebUtility.UrlEncode(mapLine.ContentId.ToString())}">Edit</a>""";

                    geoJsonList.Add(lineFeatureCollection);
                    boundsKeeper.Add(new Point(mapLine.InitialViewBoundsMaxLongitude,
                        mapLine.InitialViewBoundsMaxLatitude));
                    boundsKeeper.Add(new Point(mapLine.InitialViewBoundsMinLongitude,
                        mapLine.InitialViewBoundsMinLatitude));
                    break;
            }

        var db = await Db.Context();

        var points = await dbEntries.Where(x => x is PointContent).Cast<PointContent>()
            .SelectInSequenceAsync(async x => await Db.PointContentDtoFromPoint(x, db));

        var pointDtos = dbEntries.Where(x => x is PointContentDto).Cast<PointContentDto>().Union(points)
            .OrderBy(x => x.Title).ToList();

        if (pointDtos.Any())
        {
            var featureCollection = new FeatureCollection();

            foreach (var loopElements in pointDtos)
            {
                var descriptionAndImage =
                    GenerateDescription(loopElements.MainPicture, loopElements.Title, loopElements.Summary);
                if (!string.IsNullOrWhiteSpace(descriptionAndImage.imageFileToCopy))
                    filesToCopy.Add(descriptionAndImage.imageFileToCopy);

                featureCollection.Add(new Feature(
                    PointTools.Wgs84Point(loopElements.Longitude, loopElements.Latitude,
                        loopElements.Elevation ?? 0),
                    new AttributesTable(new Dictionary<string, object>
                    {
                        {
                            "title",
                            $"""<a href="http://[[VirtualDomain]]/LocalPreview?{WebUtility.UrlEncode(UserSettingsSingleton.CurrentSettings().PointPageUrl(loopElements))}">{(string.IsNullOrWhiteSpace(loopElements.Title) ? "Preview" : loopElements.Title)}</a> <a href="http://[[VirtualDomain]]/LocalEdit?{WebUtility.UrlEncode(loopElements.ContentId.ToString())}">Edit</a>"""
                        },
                        { "description", descriptionAndImage.description },
                        { "mapLabel", loopElements.MapLabel },
                        { "displayId", loopElements.ContentId }
                    })));
                boundsKeeper.Add(new Point(loopElements.Longitude, loopElements.Latitude));
            }

            geoJsonList.Add(featureCollection);
        }


        var photos = dbEntries.Where(x => x is PhotoContent).Cast<PhotoContent>()
            .Where(x => x.Latitude is not null && x.Longitude is not null).OrderBy(x => x.Title).ToList();

        if (photos.Count != 0)
        {
            var featureCollection = new FeatureCollection();

            foreach (var loopElements in photos)
            {
                var descriptionAndImage =
                    GenerateDescription(loopElements.MainPicture, loopElements.Title, loopElements.Summary);
                if (!string.IsNullOrWhiteSpace(descriptionAndImage.imageFileToCopy))
                    filesToCopy.Add(descriptionAndImage.imageFileToCopy);

                featureCollection.Add(new Feature(
                    PointTools.Wgs84Point(loopElements.Longitude.Value, loopElements.Latitude.Value,
                        loopElements.Elevation ?? 0),
                    new AttributesTable(new Dictionary<string, object>
                    {
                        {
                            "title",
                            $"""<a href="http://[[VirtualDomain]]/LocalPreview?{WebUtility.UrlEncode(UserSettingsSingleton.CurrentSettings().PhotoPageUrl(loopElements))}">{(string.IsNullOrWhiteSpace(loopElements.Title) ? "Preview" : loopElements.Title)}</a> <a href="http://[[VirtualDomain]]/LocalEdit?{WebUtility.UrlEncode(loopElements.ContentId.ToString())}">Edit</a>"""
                        },
                        { "description", descriptionAndImage.description },
                        { "displayId", loopElements.ContentId }
                    })));
                boundsKeeper.Add(new Point(loopElements.Longitude.Value, loopElements.Latitude.Value));
            }

            geoJsonList.Add(featureCollection);
        }

        var contentBounds = SpatialConverters.PointBoundingBox(boundsKeeper);

        return (SpatialBounds.FromEnvelope(contentBounds), geoJsonList, filesToCopy);
    }
}