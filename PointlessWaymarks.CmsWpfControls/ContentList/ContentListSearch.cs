using System.Text.Json;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.FileList;
using PointlessWaymarks.CmsWpfControls.GeoJsonList;
using PointlessWaymarks.CmsWpfControls.ImageList;
using PointlessWaymarks.CmsWpfControls.LineList;
using PointlessWaymarks.CmsWpfControls.LinkList;
using PointlessWaymarks.CmsWpfControls.MapComponentList;
using PointlessWaymarks.CmsWpfControls.NoteList;
using PointlessWaymarks.CmsWpfControls.PhotoList;
using PointlessWaymarks.CmsWpfControls.PointList;
using PointlessWaymarks.CmsWpfControls.PostList;
using PointlessWaymarks.CmsWpfControls.VideoList;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public static class ContentListSearch
{
    public static ContentListSearchReturn SearchActivityType(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not LineListListItem lineItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Activity Type Filter on Item that is not a Line - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(lineItem.DbEntry.ActivityType, searchString,
                "Activity Type"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchAperture(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "Aperture Search on Item that is not a Photo - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterAperture(photoItem.DbEntry.Aperture, searchString), searchResultModifier);
    }

    public static ContentListSearchReturn SearchBounds(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (!string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith("BOUNDS:", StringComparison.OrdinalIgnoreCase))
            searchString = searchString[7..];

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(new ContentListSearchFunctionReturn(false,
                "Blank Search String with Not Blank Shutter Speed (false)."), searchResultModifier);

        var bounds = JsonSerializer.Deserialize<SpatialBounds>(searchString.Trim());

        if (bounds == null)
            return new ContentListSearchReturn(new ContentListSearchFunctionReturn(false,
                "Could not Deserialize Bounds (false)."), searchResultModifier);

        switch (toFilter)
        {
            case LineListListItem lineItem:
                var includeLine = Db.LineContentBoundingBoxOverlaps(lineItem.DbEntry, bounds);
                return new ContentListSearchReturn(
                    new ContentListSearchFunctionReturn(includeLine,
                        $"Line Bounding Box Overlaps Search Bounding Box - {includeLine}"), searchResultModifier);
            case PhotoListListItem photoItem:
                var includePhoto = Db.PhotoContentIsInBoundingBox(photoItem.DbEntry, bounds);
                return new ContentListSearchReturn(
                    new ContentListSearchFunctionReturn(includePhoto,
                        $"Line Bounding Box Overlaps Search Bounding Box - {includePhoto}"), searchResultModifier);
            case PointListListItem pointItem:
                var includePoint = Db.PointContentIsInBoundingBox(pointItem.DbEntry, bounds);
                return new ContentListSearchReturn(
                    new ContentListSearchFunctionReturn(includePoint,
                        $"Line Bounding Box Overlaps Search Bounding Box - {includePoint}"), searchResultModifier);
            case GeoJsonListListItem geoJsonItem:
                var includeGeoJson = Db.GeoJsonBoundingBoxOverlaps(geoJsonItem.DbEntry, bounds);
                return new ContentListSearchReturn(
                    new ContentListSearchFunctionReturn(includeGeoJson,
                        $"Line Bounding Box Overlaps Search Bounding Box - {includeGeoJson}"), searchResultModifier);
            case MapComponentListListItem mapItem:
                var includeMap = Db.MapInitialBoundingBoxOverlaps(mapItem.DbEntry, bounds);
                return new ContentListSearchReturn(
                    new ContentListSearchFunctionReturn(includeMap,
                        $"Line Bounding Box Overlaps Search Bounding Box - {includeMap}"), searchResultModifier);
            default:
                return new ContentListSearchReturn(
                    new ContentListSearchFunctionReturn(false,
                        "Bounds Search on Item that is not a Line, Photo, or Point - Excluding"), searchResultModifier);
        }
    }

    public static ContentListSearchReturn SearchCamera(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "Camera Search on Item that is not a Photo - Excluding"),
                searchResultModifier);

        var cameraMakeModel =
            $"{photoItem.DbEntry.CameraMake.TrimNullToEmpty()} {photoItem.DbEntry.CameraModel.TrimNullToEmpty()}";
        searchString = searchString.Trim();

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(cameraMakeModel, searchString, "Camera"),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchClimb(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not LineListListItem lineItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "Climb Search on Item that is not a Line - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterNumber((decimal)lineItem.DbEntry.ClimbElevation, searchString, "Climb"),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchContentType(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        searchString = searchString[5..].TrimNullToEmpty();
        var contentTypes =
            searchString.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        //!!Content Type List!!
        var matchFound =
            (contentTypes.Contains("File", StringComparer.OrdinalIgnoreCase) && toFilter is FileListListItem)
            || (contentTypes.Contains("GeoJson", StringComparer.OrdinalIgnoreCase) &&
                toFilter is GeoJsonListListItem)
            || (contentTypes.Contains("Image", StringComparer.OrdinalIgnoreCase) &&
                toFilter is ImageListListItem)
            || (contentTypes.Contains("Line", StringComparer.OrdinalIgnoreCase) &&
                toFilter is LineListListItem)
            || (contentTypes.Contains("Link", StringComparer.OrdinalIgnoreCase) &&
                toFilter is LinkListListItem)
            || (contentTypes.Contains("Map", StringComparer.OrdinalIgnoreCase) &&
                toFilter is MapComponentListListItem)
            || (contentTypes.Contains("Note", StringComparer.OrdinalIgnoreCase) &&
                toFilter is NoteListListItem)
            || (contentTypes.Contains("Photo", StringComparer.OrdinalIgnoreCase) &&
                toFilter is PhotoListListItem)
            || (contentTypes.Contains("Photograph", StringComparer.OrdinalIgnoreCase) &&
                toFilter is PhotoListListItem)
            || (contentTypes.Contains("Point", StringComparer.OrdinalIgnoreCase) &&
                toFilter is PointListListItem)
            || (contentTypes.Contains("Post", StringComparer.OrdinalIgnoreCase) &&
                toFilter is PostListListItem)
            || (contentTypes.Contains("Video", StringComparer.OrdinalIgnoreCase) &&
                toFilter is VideoListListItem);

        if (!matchFound)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    $"Content Type did not Match Search String {searchString}"), searchResultModifier);

        return new ContentListSearchReturn(
            new ContentListSearchFunctionReturn(matchFound, $"Content Type did not Match Search String {searchString}"),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchCreatedBy(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Created By Search with no Search String - Including"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(toFilter.Content().CreatedBy ?? string.Empty,
                searchString.Trim(), "Created By"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchCreatedOn(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Created On Search with no Search String - Including"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterDateTime(toFilter.Content().CreatedOn, searchString.Trim(), "Created On"),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchDescent(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not LineListListItem lineItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "Descent Search on Item that is not a Line - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterNumber((decimal)lineItem.DbEntry.DescentElevation, searchString,
                "Descent"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchElevation(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is LineListListItem lineItem)
        {
            var minFilter = new ContentListSearchReturn(
                ContentListSearchFunctions.FilterNumber((decimal)lineItem.DbEntry.MinimumElevation, searchString,
                    "Elevation"), searchResultModifier);
            var maxFilter = new ContentListSearchReturn(
                ContentListSearchFunctions.FilterNumber((decimal)lineItem.DbEntry.MaximumElevation, searchString,
                    "Elevation"), searchResultModifier);

            var minFilterInclude = minFilter.SearchFunctionReturn.Include;
            var maxFilterInclude = maxFilter.SearchFunctionReturn.Include;

            if (minFilter.ResultModifier(minFilterInclude) || maxFilter.ResultModifier(maxFilterInclude))
                return new ContentListSearchReturn(
                    new ContentListSearchFunctionReturn(true,
                        $"Line Min Elevation Included {minFilterInclude} - Line Max Elevation Included {maxFilterInclude}"),
                    searchResultModifier);
        }

        if (toFilter is PhotoListListItem { DbEntry.Elevation: not null } photoItem)
            return new ContentListSearchReturn(
                ContentListSearchFunctions.FilterNumber((decimal)photoItem.DbEntry.Elevation,
                    searchString,
                    "Elevation"), searchResultModifier);

        if (toFilter is PointListListItem { DbEntry.Elevation: not null } pointItem)
            return new ContentListSearchReturn(
                ContentListSearchFunctions.FilterNumber((decimal)pointItem.DbEntry.Elevation,
                    searchString,
                    "Elevation"), searchResultModifier);

        return new ContentListSearchReturn(
            new ContentListSearchFunctionReturn(false, "No relevant type or data - Excluding"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchFileFileEmbed(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not FileListListItem fileItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "File Embed Search on Item that is not a File - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterBoolean(fileItem.DbEntry.EmbedFile, searchString,
                "File Embed"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchFocalLength(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Focal Length Search on Item that is not a Photo - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterFocalLength(photoItem.DbEntry.FocalLength, searchString),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchFolder(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Folder Search with no Search String - Including"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(toFilter.Content().Folder ?? string.Empty,
                searchString.Trim(), "Folder"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchGeneral(IContentListItem toFilter, string stringToSearch,
        Func<bool, bool> searchResultModifier)
    {
        if ((toFilter.Content().Title ?? string.Empty).Contains(stringToSearch, StringComparison.OrdinalIgnoreCase))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, $"{stringToSearch} found in Title"), searchResultModifier);
        if ((toFilter.Content().Tags ?? string.Empty).Contains(stringToSearch, StringComparison.OrdinalIgnoreCase))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, $"{stringToSearch} found in Tags"), searchResultModifier);

        if ((toFilter.Content().Summary ?? string.Empty).Contains(stringToSearch, StringComparison.OrdinalIgnoreCase))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, $"{stringToSearch} found in Summary"), searchResultModifier);

        if ((toFilter.Content().Folder ?? string.Empty).Contains(stringToSearch, StringComparison.OrdinalIgnoreCase))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, $"{stringToSearch} found in Folder"), searchResultModifier);

        if ((toFilter.Content().CreatedBy ?? string.Empty).Contains(stringToSearch, StringComparison.OrdinalIgnoreCase))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, $"{stringToSearch} found in Created By"),
                searchResultModifier);

        if ((toFilter.Content().LastUpdatedBy ?? string.Empty).Contains(stringToSearch,
                StringComparison.OrdinalIgnoreCase))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, $"{stringToSearch} found in Last Updated By"),
                searchResultModifier);

        if (toFilter.ContentId() != null && toFilter.ContentId().ToString()!
                .Contains(stringToSearch, StringComparison.OrdinalIgnoreCase))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, $"{stringToSearch} found in Content Id"),
                searchResultModifier);

        return new ContentListSearchReturn(
            new ContentListSearchFunctionReturn(false, $"{stringToSearch} not found in a General Content Search"),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchImageShowInSearch(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not ImageListListItem fileItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Image Show In Search on Item that is not a File - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterBoolean(fileItem.DbEntry.ShowInSearch, searchString,
                "File Embed"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchIncludeInActivityLog(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not LineListListItem lineItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Include In Activity Log on Item that is not a Line - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterBoolean(lineItem.DbEntry.IncludeInActivityLog, searchString,
                "Include In Activity Log"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchIso(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "ISO Search on Item that is not a Photo - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterIso(photoItem.DbEntry.Iso.ToString(), searchString), searchResultModifier);
    }

    public static ContentListSearchReturn SearchLastUpdatedBy(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Last Updated By Search with no Search String - Including"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(toFilter.Content().LastUpdatedBy ?? string.Empty,
                searchString.Trim(), "Created By"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchLastUpdatedOn(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Last Updated On Search with no Search String - Including"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterDateTime(toFilter.Content().CreatedOn, searchString.Trim(), "Created On"),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchLens(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "Lens Search on Item that is not a Photo - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(photoItem.DbEntry.Lens, searchString, "Lens"),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchLicense(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "License Search on Item that is not a Photo - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(photoItem.DbEntry.License, searchString, "License"),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchLineShowContentReferencesOnMap(IContentListItem toFilter,
        string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not LineListListItem lineItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Show Content References on Item that is not a Line - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterBoolean(lineItem.DbEntry.ShowContentReferencesOnMap, searchString,
                "Show Content References On Map"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchMapIcon(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PointListListItem pointItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Map Icon Filter on Item that is not a Point - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(pointItem.DbEntry.MapIconName, searchString,
                "Map Icon"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchMapLabel(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PointListListItem pointItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Map Label Filter on Item that is not a Point - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(pointItem.DbEntry.MapLabel, searchString,
                "Map Label"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchMapMarkerColor(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PointListListItem pointItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Map Marker Color Filter on Item that is not a Point - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(pointItem.DbEntry.MapMarkerColor, searchString,
                "Map Marker Color"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchMaxElevation(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not LineListListItem lineItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Max Elevation Search on Item that is not a Line - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterNumber((decimal)lineItem.DbEntry.MaximumElevation, searchString,
                "Max Elevation"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchMiles(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not LineListListItem lineItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "Miles Search on Item that is not a Line - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterNumber((decimal)lineItem.DbEntry.LineDistance, searchString, "Miles"),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchMinElevation(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not LineListListItem lineItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Min Elevation Search on Item that is not a Line - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterNumber((decimal)lineItem.DbEntry.MinimumElevation, searchString,
                "Min Elevation"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchOriginalFileName(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Original File Name with no Search String - Including"),
                searchResultModifier);

        var dynamicContent = toFilter.Content() as dynamic;

        if (!DynamicTypeTools.PropertyExists(toFilter.Content() as dynamic, "OriginalFileName"))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "No OriginalFileName Property - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(dynamicContent.OriginalFileName ?? string.Empty,
                searchString.Trim(), "Original File Name"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchPhotoCreatedOn(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Photo Created On Search on Item that is not a Photo - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterDateTime(photoItem.DbEntry.PhotoCreatedOn, searchString,
                "Photo Created On"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchPhotoPosition(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Show Photo Position on Item that is not a Photo - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterBoolean(photoItem.DbEntry.ShowPhotoPosition, searchString,
                "Show Photo Position"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchPictureShowSizes(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                ContentListSearchFunctions.FilterBoolean(photoItem.DbEntry.ShowPhotoSizes, searchString,
                    "Show Photo Sizes"), searchResultModifier);
        else if (toFilter is ImageListListItem imageItem)
            return new ContentListSearchReturn(
                ContentListSearchFunctions.FilterBoolean(imageItem.DbEntry.ShowImageSizes, searchString,
                    "Show Image Sizes"), searchResultModifier);

        return new ContentListSearchReturn(
            new ContentListSearchFunctionReturn(false,
                "Show Image Sizes on Item that is not an Image or Photo - Excluding"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchPublicDownloadLink(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Public Download Link with no Search String - Including"),
                searchResultModifier);

        var dynamicContent = toFilter.Content() as dynamic;

        if (!DynamicTypeTools.PropertyExists(toFilter.Content() as dynamic, "PublicDownloadLink"))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "No PublicDownloadLink Property - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterBoolean(dynamicContent.PublicDownloadLink,
                searchString.Trim(), "Public Download Link"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchShowInMainSiteFeed(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterBoolean(toFilter.Content().ShowInMainSiteFeed, searchString,
                "Show in Main Site Feed"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchShutterSpeed(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (toFilter is not PhotoListListItem photoItem)
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false,
                    "Shutter Speed Search on Item that is not a Photo - Excluding"), searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterShutterSpeedLength(photoItem.DbEntry.ShutterSpeed, searchString),
            searchResultModifier);
    }

    public static ContentListSearchReturn SearchSlug(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Slug Search with no Search String - Including"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(toFilter.Content().Slug ?? string.Empty,
                searchString.Trim(), "Slug"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchSummary(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Summary Search with no Search String - Including"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(toFilter.Content().Summary ?? string.Empty,
                searchString.Trim(), "Summary"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchTags(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Tags Search with no Search String - Including"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringListContains(toFilter.Content().Tags ?? string.Empty,
                searchString.Trim(), "Tags"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchTitle(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Title Search with no Search String - Including"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(toFilter.Content().Title ?? string.Empty,
                searchString.Trim(), "Title"), searchResultModifier);
    }

    public static ContentListSearchReturn SearchUpdateNotes(IContentListItem toFilter, string searchString,
        Func<bool, bool> searchResultModifier)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, "Original File Name with no Search String - Including"),
                searchResultModifier);

        var dynamicContent = toFilter.Content() as dynamic;

        if (!DynamicTypeTools.PropertyExists(toFilter.Content() as dynamic, "UpdateNotes"))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(false, "No UpdateNotes Property - Excluding"),
                searchResultModifier);

        return new ContentListSearchReturn(
            ContentListSearchFunctions.FilterStringContains(dynamicContent.UpdateNotes ?? string.Empty,
                searchString.Trim(), "Update Notes"), searchResultModifier);
    }

    public record ContentListSearchReturn(
        ContentListSearchFunctionReturn SearchFunctionReturn,
        Func<bool, bool> ResultModifier);
}