using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData.BracketCodes;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.ContentHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.SpatialTools;

namespace PointlessWaymarks.CmsData;

public static class CommonContentValidation
{
    public static async Task<List<GenerationReturn>> CheckAllContentForBadContentReferences(
        IProgress<string>? progress = null)
    {
        var returnList = new List<GenerationReturn>();

        var db = await Db.Context().ConfigureAwait(false);

        returnList.AddRange(await CheckForBadContentReferences(
                (await db.FileContents.ToListAsync().ConfigureAwait(false)).Cast<IContentCommon>().ToList(), db,
                progress)
            .ConfigureAwait(false));
        returnList.AddRange(await CheckForBadContentReferences(
            (await db.GeoJsonContents.ToListAsync().ConfigureAwait(false)).Cast<IContentCommon>().ToList(), db,
            progress).ConfigureAwait(false));
        returnList.AddRange(await CheckForBadContentReferences(
                (await db.ImageContents.ToListAsync().ConfigureAwait(false)).Cast<IContentCommon>().ToList(), db,
                progress)
            .ConfigureAwait(false));
        returnList.AddRange(await CheckForBadContentReferences(
                (await db.LineContents.ToListAsync().ConfigureAwait(false)).Cast<IContentCommon>().ToList(), db,
                progress)
            .ConfigureAwait(false));
        returnList.AddRange(await CheckForBadContentReferences(
                (await db.NoteContents.ToListAsync().ConfigureAwait(false)).Cast<IContentCommon>().ToList(), db,
                progress)
            .ConfigureAwait(false));
        returnList.AddRange(await CheckForBadContentReferences(
                (await db.PhotoContents.ToListAsync().ConfigureAwait(false)).Cast<IContentCommon>().ToList(), db,
                progress)
            .ConfigureAwait(false));
        returnList.AddRange(await CheckForBadContentReferences(
                (await db.PointContents.ToListAsync().ConfigureAwait(false)).Cast<IContentCommon>().ToList(), db,
                progress)
            .ConfigureAwait(false));
        returnList.AddRange(await CheckForBadContentReferences(
                (await db.PostContents.ToListAsync().ConfigureAwait(false)).Cast<IContentCommon>().ToList(), db,
                progress)
            .ConfigureAwait(false));
        returnList.AddRange(await CheckForBadContentReferences(
                (await db.VideoContents.ToListAsync().ConfigureAwait(false)).Cast<IContentCommon>().ToList(), db,
                progress)
            .ConfigureAwait(false));

        return returnList;
    }

    public static async Task<List<GenerationReturn>> CheckForBadContentReferences(List<IContentCommon> content,
        PointlessWaymarksContext db, IProgress<string>? progress = null)
    {
        var returnList = new List<GenerationReturn>();

        if (!content.Any()) return returnList;

        foreach (var loopContent in content)
            returnList.Add(await CheckForBadContentReferences(loopContent, db, progress).ConfigureAwait(false));

        return returnList;
    }

    public static async Task<GenerationReturn> CheckForBadContentReferences(IContentCommon content,
        PointlessWaymarksContext db, IProgress<string>? progress)
    {
        progress?.Report($"Checking ContentIds for {content.Title}");

        var extracted = new List<Guid>();

        if (content.MainPicture != null && content.MainPicture != content.ContentId)
            extracted.Add(content.MainPicture.Value);

        var toSearch = string.Empty;

        toSearch += content.BodyContent + content.Summary;

        if (content is IUpdateNotes updateContent) toSearch += updateContent.UpdateNotes;

        if (content is PointContentDto pointDto)
            pointDto.PointDetails.ForEach(x => toSearch += x.StructuredDataAsJson);

        if (content is PointContent point)
            (await Db.PointDetailsForPoint(point.ContentId, db).ConfigureAwait(false)).ForEach(x =>
                toSearch += x.StructuredDataAsJson);

        if (string.IsNullOrWhiteSpace(toSearch) && !extracted.Any())
            return GenerationReturn.Success(
                $"{Db.ContentTypeDisplayString(content)} {content.Title} - No Content Ids Found", content.ContentId);

        extracted.AddRange(BracketCodeCommon.BracketCodeContentIds(toSearch));

        if (!extracted.Any())
            return GenerationReturn.Success(
                $"{Db.ContentTypeDisplayString(content)} {content.Title} - No Content Ids Found", content.ContentId);

        progress?.Report($"Found {extracted.Count} ContentIds to check for {content.Title}");

        var notFoundList = new List<Guid>();

        foreach (var loopExtracted in extracted)
        {
            var contentLookup = await db.ContentFromContentId(loopExtracted).ConfigureAwait(false);

            if (contentLookup != null) continue;
            if (await db.MapComponents.AnyAsync(x => x.ContentId == loopExtracted).ConfigureAwait(false)) continue;

            progress?.Report($"ContentId {loopExtracted} Not Found in Db...");
            notFoundList.Add(loopExtracted);
        }

        if (notFoundList.Any())
            return GenerationReturn.Error(
                $"{Db.ContentTypeDisplayString(content)} {content.Title} has " +
                $"Invalid ContentIds in Bracket Codes - {string.Join(", ", notFoundList)}", content.ContentId);

        return GenerationReturn.Success(
            $"{Db.ContentTypeDisplayString(content)} {content.Title} - No Invalid Content Ids Found");
    }

    public static async Task<GenerationReturn> CheckForIsDraftConflicts(IContentCommon content)
    {
        if (content.IsDraft)
            return GenerationReturn.Success("Content is Draft - references to both Draft/Production content are OK",
                content.ContentId);

        var toSearch = string.Empty;

        toSearch += content.BodyContent + content.Summary;

        if (content is IUpdateNotes updateContent) toSearch += updateContent.UpdateNotes;

        if (string.IsNullOrWhiteSpace(toSearch))
            return GenerationReturn.Success(
                "Production Content does not Reference Any Draft Content (No Content String)");

        var db = await Db.Context().ConfigureAwait(false);

        var bracketCodeContent = BracketCodeCommon.BracketCodeContentIds(toSearch);

        if (!bracketCodeContent.Any())
            return GenerationReturn.Success(
                "Production Content does not Reference Any Draft Content (No Bracket Code Content References)");

        var contentFromDb = await db.ContentCommonShellFromContentIds(bracketCodeContent);

        if (contentFromDb.All(x => !x.IsDraft))
            return GenerationReturn.Success(
                $"All {contentFromDb.Count} Bracket Code Content References are Production");

        var problems = contentFromDb.Where(x => x.IsDraft).OrderBy(x => x.Title).ToList();

        var draftReferencesList = new List<string>();

        foreach (var loopProblem in problems)
        {
            var contentDisplayName = Db.ContentTypeDisplayString(loopProblem.ContentId);

            problems.ForEach(x => draftReferencesList.Add($"{contentDisplayName}: {x.Title} - Is Draft: {x.IsDraft}"));
        }

        return GenerationReturn.Error(
            $"Production Content can not Reference Draft Content - this post references:{Environment.NewLine}{string.Join(Environment.NewLine, draftReferencesList)}");
    }

    public static async Task<GenerationReturn> CheckStringForBadContentReferences(string? toSearch,
        PointlessWaymarksContext db, IProgress<string>? progress)
    {
        if (string.IsNullOrWhiteSpace(toSearch))
            return GenerationReturn.Success("No Content Ids Found");

        var extracted = new List<Guid>();

        extracted.AddRange(BracketCodeCommon.BracketCodeContentIds(toSearch));

        if (!extracted.Any())
            return GenerationReturn.Success("No Content Ids Found");

        progress?.Report($"Found {extracted.Count} ContentIds to check for");

        var notFoundList = new List<Guid>();

        foreach (var loopExtracted in extracted)
        {
            var contentLookup = await db.ContentFromContentId(loopExtracted).ConfigureAwait(false);

            if (contentLookup == null)
            {
                progress?.Report($"ContentId {loopExtracted} Not Found in Db...");
                notFoundList.Add(loopExtracted);
            }
        }

        if (notFoundList.Any())
            return GenerationReturn.Error($"Invalid ContentIds in Bracket Codes - {string.Join(", ", notFoundList)}");

        return GenerationReturn.Success("No Invalid Content Ids Found");
    }

    public static Task<IsValid> ElevationValidation(double? elevation)
    {
        if (elevation == null) return Task.FromResult(new IsValid(true, "Null Elevation is Valid"));

        if (elevation > 29500)
            return Task.FromResult(new IsValid(false,
                $"Elevations are limited to the elevation of Mount Everest - 29,092' above sea level - {elevation} was input..."));

        if (elevation < -50000)
            return Task.FromResult(new IsValid(false,
                $"This is very unlikely to be a valid elevation, this exceeds the depth of the Mariana Trench and known Extended-Reach Drilling (as of 2020) - elevations under -50,000' are not considered valid - {elevation} was input..."));

        return Task.FromResult(new IsValid(true, "Elevation is Valid"));
    }

    public static Task<bool> FileContentFileFileNameHasInvalidCharacters(FileInfo? fileContentFile,
        Guid? currentContentId)
    {
        if (fileContentFile == null) return Task.FromResult(false);

        fileContentFile.Refresh();

        if (!fileContentFile.Exists) return Task.FromResult(false);

        return Task.FromResult(
            !FileAndFolderTools.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(fileContentFile.Name)));
    }

    public static async Task<IsValid> FileContentFileValidation(FileInfo? fileContentFile, Guid? currentContentId)
    {
        if (fileContentFile == null) return new IsValid(false, "No File?");

        fileContentFile.Refresh();

        if (!fileContentFile.Exists) return new IsValid(false, "File does not Exist?");

        if (!FileAndFolderTools.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(fileContentFile.Name)))
            return new IsValid(false, "Limit File Names to A-Z a-z 0-9 - . _");

        if (await (await Db.Context().ConfigureAwait(false))
            .FileFilenameExistsInDatabase(fileContentFile.Name, currentContentId).ConfigureAwait(false))
            return new IsValid(false, "This filename already exists in the database - file names must be unique.");

        return new IsValid(true, "File is Valid");
    }

    public static Task<IsValid> GeoJsonValidation(string? geoJsonString)
    {
        if (string.IsNullOrWhiteSpace(geoJsonString))
            return Task.FromResult(new IsValid(false, "Blank GeoJson is not Valid"));

        try
        {
            var featureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(geoJsonString);
            if (featureCollection.Count < 1)
                return Task.FromResult(new IsValid(false, "The GeoJson appears to have an empty Feature Collection?"));
        }
        catch (Exception e)
        {
            return Task.FromResult(new IsValid(false,
                $"Error parsing a Feature Collection from the GeoJson, this CMS needs even single GeoJson types to be wrapped into a FeatureCollection... {e.Message}"));
        }

        return Task.FromResult(new IsValid(true, string.Empty));
    }

    public static async Task<IsValid> ImageFileValidation(FileInfo? imageFile, Guid? currentContentId)
    {
        if (imageFile == null) return new IsValid(false, "File is Null?");

        imageFile.Refresh();

        if (!imageFile.Exists) return new IsValid(false, "File does not Exist?");

        if (!FileAndFolderTools.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(imageFile.Name)))
            return new IsValid(false, "Limit File Names to A-Z a-z 0-9 - . _");

        if (!FileAndFolderTools.PictureFileTypeIsSupported(imageFile))
            return new IsValid(false, "The file doesn't appear to be a supported file type.");

        if (await (await Db.Context().ConfigureAwait(false))
            .ImageFilenameExistsInDatabase(imageFile.Name, currentContentId).ConfigureAwait(false))
            return new IsValid(false,
                "This filename already exists in the database - image file names must be unique.");

        return new IsValid(true, "File is Valid");
    }

    public static Task<IsValid> LatitudeValidation(double latitude)
    {
        if (latitude is > 90 or < -90)
            return Task.FromResult(new IsValid(false,
                $"Latitude on Earth must be between -90 and 90 - {latitude} is not valid."));

        return Task.FromResult(new IsValid(true, "Latitude is Valid"));
    }

    public static async Task<IsValid> LatitudeValidationWithNullOk(double? latitude)
    {
        if (latitude == null) return new IsValid(true, "No Latitude is Ok...");

        return await LatitudeValidation(latitude.Value);
    }

    public static IsValid LineGeoJsonValidation(string? geoJsonString)
    {
        if (string.IsNullOrWhiteSpace(geoJsonString)) return new IsValid(false, "Blank Line GeoJson is not Valid");

        try
        {
            var featureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(geoJsonString);
            if (featureCollection.Count < 1)
                return new IsValid(false,
                    "The GeoJson for the line appears to have an empty Feature Collection?");
            if (featureCollection.Count > 1)
                return new IsValid(false,
                    "The GeoJson for the line appears to contain multiple elements? It should only contain 1 line...");
            if (featureCollection[0].Geometry is not LineString)
                return new IsValid(false, "The GeoJson for the line has one element but it isn't a LineString?");
            var lineString = featureCollection[0].Geometry as LineString;
            if (lineString == null || lineString.Count < 1 || lineString.Length == 0)
                return new IsValid(false, "The LineString doesn't have any points or is zero length?");
        }
        catch (Exception e)
        {
            return new IsValid(false,
                $"Error parsing the FeatureCollection and/or problems checking the LineString {e.Message}");
        }

        return new IsValid(true, string.Empty);
    }

    public static Task<IsValid> LongitudeValidation(double longitude)
    {
        if (longitude is > 180 or < -180)
            return Task.FromResult(new IsValid(false,
                $"Longitude on Earth must be between -180 and 180 - {longitude} is not valid."));

        return Task.FromResult(new IsValid(true, "Longitude is Valid"));
    }

    public static async Task<IsValid> LongitudeValidationWithNullOk(double? longitude)
    {
        if (longitude == null) return new IsValid(true, "No Longitude is Ok...");

        return await LongitudeValidation(longitude.Value);
    }

    public static async Task<IsValid> PhotoFileValidation(FileInfo? photoFile, Guid? currentContentId)
    {
        if (photoFile == null) return new IsValid(false, "File is Null?");

        photoFile.Refresh();

        if (!photoFile.Exists) return new IsValid(false, "File does not Exist?");

        if (!FileAndFolderTools.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(photoFile.Name)))
            return new IsValid(false, "Limit File Names to A-Z a-z 0-9 - . _");

        if (!FileAndFolderTools.PictureFileTypeIsSupported(photoFile))
            return new IsValid(false, "The file doesn't appear to be a supported file type.");

        if (await (await Db.Context().ConfigureAwait(false))
            .PhotoFilenameExistsInDatabase(photoFile.Name, currentContentId).ConfigureAwait(false))
            return new IsValid(false,
                "This filename already exists in the database - photo file names must be unique.");

        return new IsValid(true, "File is Valid");
    }

    public static Task<IsValid> ValidateBodyContentFormat(string? contentFormat)
    {
        if (string.IsNullOrWhiteSpace(contentFormat))
            return Task.FromResult(new IsValid(false, "Body Content Format must be set"));

        if (Enum.TryParse(typeof(ContentFormatEnum), contentFormat, true, out _))
            return Task.FromResult(new IsValid(true, string.Empty));

        return Task.FromResult(new IsValid(false, $"Could not parse {contentFormat} into a known Content Format"));
    }

    public static async Task<IsValid> ValidateContentCommon(IContentCommon toValidate)
    {
        var isNewEntry = toValidate.Id < 1;

        var isValid = true;
        var errorMessage = new List<string?>();

        if (toValidate.ContentId == Guid.Empty)
        {
            isValid = false;
            errorMessage.Add("Content ID is Empty");
        }

        var titleValidation = await ValidateTitle(toValidate.Title);

        if (!titleValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(titleValidation.Explanation);
        }

        var summaryValidation = await ValidateSummary(toValidate.Summary);

        if (!summaryValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(summaryValidation.Explanation);
        }

        var createdUpdatedValidation = await ValidateCreatedAndUpdatedBy(toValidate, isNewEntry);

        if (!createdUpdatedValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(createdUpdatedValidation.Explanation);
        }

        var mainSiteFeedOnValidation = ValidateSiteFeedOn(toValidate, isNewEntry);

        if (!mainSiteFeedOnValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(mainSiteFeedOnValidation.Explanation);
        }

        var slugValidation = await ValidateSlugLocalAndDb(toValidate.Slug, toValidate.ContentId).ConfigureAwait(false);

        if (!slugValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(slugValidation.Explanation);
        }

        var folderValidation = ValidateFolder(toValidate.Folder);

        if (!folderValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(folderValidation.Explanation);
        }

        var tagValidation = ValidateTags(toValidate.Tags);

        if (!tagValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(tagValidation.Explanation);
        }

        var bodyContentFormatValidation = await ValidateBodyContentFormat(toValidate.BodyContentFormat);

        if (!bodyContentFormatValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(bodyContentFormatValidation.Explanation);
        }

        var draftContentInProductionCheck = await CheckForIsDraftConflicts(toValidate);

        if (draftContentInProductionCheck.HasError)
        {
            isValid = false;
            errorMessage.Add(draftContentInProductionCheck.GenerationNote);
        }

        var contentIdCheck =
            await CheckForBadContentReferences(toValidate, await Db.Context().ConfigureAwait(false), null)
                .ConfigureAwait(false);

        if (contentIdCheck.HasError)
        {
            isValid = false;
            errorMessage.Add(contentIdCheck.GenerationNote);
        }

        return new IsValid(isValid, string.Join(Environment.NewLine, errorMessage));
    }

    public static async Task<IsValid> ValidateCreatedAndUpdatedBy(ICreatedAndLastUpdateOnAndBy toValidate,
        bool isNewEntry)
    {
        var isValid = true;
        var errorMessage = new List<string>();

        if (toValidate.CreatedOn == DateTime.MinValue)
        {
            isValid = false;
            errorMessage.Add($"Created on of {toValidate.CreatedOn} is not valid.");
        }

        var (valid, explanation) = await ValidateCreatedBy(toValidate.CreatedBy);

        if (!valid)
        {
            isValid = false;
            errorMessage.Add(explanation);
        }

        if (!isNewEntry && string.IsNullOrWhiteSpace(toValidate.LastUpdatedBy))
        {
            isValid = false;
            errorMessage.Add("Updated by can not be blank when updating an entry");
        }

        if ((!isNewEntry && toValidate.LastUpdatedOn == null) || toValidate.LastUpdatedOn == DateTime.MinValue)
        {
            isValid = false;
            errorMessage.Add("Last Updated On can not be blank/empty when updating an entry");
        }

        return new IsValid(isValid, string.Join(Environment.NewLine, errorMessage));
    }

    public static Task<IsValid> ValidateCreatedBy(string? createdBy)
    {
        if (string.IsNullOrWhiteSpace(createdBy.TrimNullToEmpty()))
            return Task.FromResult(new IsValid(false, "Created by can not be blank."));

        return Task.FromResult(new IsValid(true, "Created By is Ok"));
    }

    public static Task<IsValid> ValidateStringIsNotEmptyOrWhitespace(string? stringToCheck)
    {
        if (string.IsNullOrWhiteSpace(stringToCheck.TrimNullToEmpty()))
            return Task.FromResult(new IsValid(false, "Can not be blank."));

        return Task.FromResult(new IsValid(true, "Ok"));
    }

    public static Task<IsValid> ValidateSvgTag(string? stringToCheck)
    {
        if (string.IsNullOrWhiteSpace(stringToCheck.TrimNullToEmpty()))
            return Task.FromResult(new IsValid(false, "Can not be blank."));

        if(!stringToCheck.Trim().StartsWith("<svg", StringComparison.OrdinalIgnoreCase) || !stringToCheck.Trim().EndsWith("</svg>", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new IsValid(false, "The SVG Tag should start with <svg and end with </svg>"));

        return Task.FromResult(new IsValid(true, "Ok"));
    }

    public static Task<IsValid> ValidateFeatureType(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return Task.FromResult(new IsValid(false, "Type can not be blank"));

        return Task.FromResult(new IsValid(true, string.Empty));
    }

    public static IsValid ValidateFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return new IsValid(false, "Folder can't be blank or only whitespace.");

        if (!FileAndFolderTools.IsNoUrlEncodingNeeded(folder))
            return new IsValid(false, "Limit folder names to a-z A-Z 0-9 _ -");

        if (string.Equals(folder, "data", StringComparison.OrdinalIgnoreCase))
            return new IsValid(false, "Folders can not be named 'Data' - this folder is reserved for use by the CMS");
        if (string.Equals(folder, "galleries", StringComparison.OrdinalIgnoreCase))
            return new IsValid(false,
                "Folders can not be named 'Galleries' - this folder is reserved for use by the CMS");

        return new IsValid(true, string.Empty);
    }

    public static async Task<IsValid> ValidateLinkContentLinkUrl(string? url, Guid? contentGuid)
    {
        if (string.IsNullOrWhiteSpace(url)) return new IsValid(false, "Link URL can not be blank");

        var db = await Db.Context().ConfigureAwait(false);

        if (contentGuid == null)
        {
            var duplicateUrl =
                (await db.LinkContents.Where(x => x.Url != null).ToListAsync().ConfigureAwait(false))
                .Any(x => x.Url!.Equals(url, StringComparison.OrdinalIgnoreCase));
            if (duplicateUrl)
                return new IsValid(false,
                    "URL Already exists in the database - duplicates are not allowed, try editing the existing entry to add new/updated information.");
        }
        else
        {
            var duplicateUrl =
                (await db.LinkContents.Where(x => x.Url != null && x.ContentId != contentGuid.Value)
                    .ToListAsync().ConfigureAwait(false))
                .Any(x => x.Url!.Equals(url, StringComparison.OrdinalIgnoreCase));
            if (duplicateUrl)
                return new IsValid(false,
                    "URL Already exists in the database - duplicates are not allowed, try editing the existing entry to add new/updated information.");
        }

        return new IsValid(true, string.Empty);
    }

    public static async Task<IsValid> ValidateMapComponent(MapComponentDto mapComponent)
    {
        var isNewEntry = mapComponent.Id < 1;

        var isValid = true;
        var errorMessage = new List<string?>();

        if (mapComponent.ContentId == Guid.Empty)
        {
            isValid = false;
            errorMessage.Add("Content ID is Empty");
        }

        var titleValidation = await ValidateTitle(mapComponent.Title);

        if (!titleValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(titleValidation.Explanation);
        }

        var summaryValidation = await ValidateSummary(mapComponent.Summary);

        if (!summaryValidation.Valid)
        {
            isValid = false;
            errorMessage.Add(summaryValidation.Explanation);
        }

        var (createdUpdatedIsValid, createdUpdatedExplanation) =
            await ValidateCreatedAndUpdatedBy(mapComponent, isNewEntry);

        if (!createdUpdatedIsValid)
        {
            isValid = false;
            errorMessage.Add(createdUpdatedExplanation);
        }

        if (!mapComponent.Elements.Any())
        {
            isValid = false;
            errorMessage.Add("A map must have at least one element");
        }

        if (!isValid) return new IsValid(false, string.Join(Environment.NewLine, errorMessage));

        if (mapComponent.Elements.Any(x => x.ElementContentId == Guid.Empty))
        {
            isValid = false;
            errorMessage.Add("Not all map elements have a valid Content Id.");
        }

        if (mapComponent.Elements.Any(x => x.MapComponentContentId != mapComponent.ContentId))
        {
            isValid = false;
            errorMessage.Add("Not all map elements are correctly associated with the map.");
        }

        if (!isValid) return new IsValid(false, string.Join(Environment.NewLine, errorMessage));

        foreach (var loopElements in mapComponent.Elements)
        {
            if (loopElements.Id < 1 || await Db.ContentIdIsSpatialContentInDatabase(loopElements.MapComponentContentId)
                    .ConfigureAwait(false)) continue;
            isValid = false;
            errorMessage.Add("Could not find all Elements Content Items in Db?");
            break;
        }

        return new IsValid(isValid, string.Join(Environment.NewLine, errorMessage));
    }

    public static async Task<IsValid> ValidatePointMapIconName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return new IsValid(true, "Blank Map Icon is Valid");

        if (!FileAndFolderTools.IsNoUrlEncodingNeededLowerCase(name))
            return new IsValid(false, "Map Icon Names should only contain a-z 0-9 _ -");

        if (name.Length > 100) return new IsValid(false, "Limit Names to 100 characters.");


        if (!(await Db.Context().ConfigureAwait(false)).MapIcons.Any(x =>
                x.IconName == name))
            return new IsValid(false, "This Name doesn't appear in the database? Check the icons in the Map Icon List.");

        return new IsValid(true, string.Empty);
    }

    public static Task<IsValid> ValidatePointMapMarkerColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return Task.FromResult(new IsValid(true, "Blank Map Marker Color is Valid"));

        if (!PointContent.MapMarkerColorChoices().Contains(color))
        {
            return Task.FromResult(new IsValid(false, "Not a current Map Marker Color choice."));
        }

        return Task.FromResult(new IsValid(true, string.Empty));
    }


    public static async Task<IsValid> ValidateMapIconName(string? name, Guid contentId)
    {
        if (string.IsNullOrWhiteSpace(name)) return new IsValid(false, "Name can not be blank");

        if (!FileAndFolderTools.IsNoUrlEncodingNeededLowerCase(name))
            return new IsValid(false, "Names should only contain a-z 0-9 _ -");

        if (name.Length > 100) return new IsValid(false, "Limit Names to 100 characters.");


        if ((await Db.Context().ConfigureAwait(false)).MapIcons.Any(x =>
                x.IconName == name && x.ContentId != contentId))
            return new IsValid(false, "This Name already exists in the database - names must be unique.");

        return new IsValid(true, string.Empty);
    }

    public static IsValid ValidateSiteFeedOn(IMainSiteFeed toValidate, bool isNewEntry)
    {
        var isValid = true;
        var errorMessage = new List<string>();

        if (toValidate.FeedOn == DateTime.MinValue)
        {
            isValid = false;
            errorMessage.Add($"A Feed On date of {toValidate.FeedOn} is not valid.");
        }

        return new IsValid(isValid, string.Join(Environment.NewLine, errorMessage));
    }

    public static Task<IsValid> ValidateSlugLocal(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Task.FromResult(new IsValid(false, "Slug can't be blank or only whitespace."));

        if (!FileAndFolderTools.IsNoUrlEncodingNeededLowerCase(slug))
            return Task.FromResult(new IsValid(false, "Slug should only contain a-z 0-9 _ -"));

        if (slug.Length > 100) return Task.FromResult(new IsValid(false, "Limit slugs to 100 characters."));

        return Task.FromResult(new IsValid(true, string.Empty));
    }

    public static async Task<IsValid> ValidateSlugLocalAndDb(string? slug, Guid contentId)
    {
        var localValidation = await ValidateSlugLocal(slug);

        if (!localValidation.Valid) return localValidation;

        if (await (await Db.Context().ConfigureAwait(false)).SlugExistsInDatabase(slug, contentId)
            .ConfigureAwait(false))
            return new IsValid(false, "This slug already exists in the database - slugs must be unique.");

        return new IsValid(true, string.Empty);
    }

    public static Task<IsValid> ValidateSummary(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return Task.FromResult(new IsValid(false, "Summary can not be blank"));

        return Task.FromResult(new IsValid(true, string.Empty));
    }

    public static IsValid ValidateTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return new IsValid(false, "At least one tag must be included.");

        var tagList = Db.TagListParse(tags);

        if (tagList.Any(x => !FileAndFolderTools.IsNoUrlEncodingNeededLowerCaseSpacesOk(x) || x.Length > 200))
            return new IsValid(false, "Limit tags to a-z 0-9 _ - [space] and less than 200 characters per tag.");

        return new IsValid(true, string.Empty);
    }

    public static Task<IsValid> ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return Task.FromResult(new IsValid(false, "Title can not be blank"));

        return Task.FromResult(new IsValid(true, string.Empty));
    }

    public static IsValid ValidateUpdateContentFormat(string? contentFormat)
    {
        if (string.IsNullOrWhiteSpace(contentFormat))
            return new IsValid(false, "Update Content Format must be set");

        if (Enum.TryParse(typeof(ContentFormatEnum), contentFormat, true, out _))
            return new IsValid(true, string.Empty);

        return new IsValid(false, $"Could not parse {contentFormat} into a known Content Format");
    }

    public static async Task<IsValid> ValidateUserMainPicture(Guid? pictureContentId)
    {
        if (pictureContentId == null) return new IsValid(true, "No picture specified.");

        var db = await Db.Context();

        if (await db.PhotoContents.AnyAsync(x => x.ContentId == pictureContentId.Value))
            return new IsValid(true, "Photo Found.");
        if (await db.ImageContents.AnyAsync(x => x.ContentId == pictureContentId.Value))
            return new IsValid(true, "Image Found.");

        return new IsValid(false, $"No matching Photo or Image found for ContentId {pictureContentId}");
    }
}