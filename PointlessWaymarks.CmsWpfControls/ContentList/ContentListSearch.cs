using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.PhotoList;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public static class ContentListSearch
{
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

        if (toFilter.ContentId() != null && toFilter.ContentId().ToString()
                .Contains(stringToSearch, StringComparison.OrdinalIgnoreCase))
            return new ContentListSearchReturn(
                new ContentListSearchFunctionReturn(true, $"{stringToSearch} found in Content Id"),
                searchResultModifier);

        return new ContentListSearchReturn(
            new ContentListSearchFunctionReturn(false, $"{stringToSearch} not found in a General Content Search"),
            searchResultModifier);
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
            ContentListSearchFunctions.FilterStringContains(toFilter.Content().Tags ?? string.Empty,
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

    public record ContentListSearchReturn(ContentListSearchFunctionReturn SearchFunctionReturn,
        Func<bool, bool> ResultModifier);
}