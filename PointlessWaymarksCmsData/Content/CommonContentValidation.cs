using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Html.CommonHtml;

namespace PointlessWaymarksCmsData.Content
{
    public static class CommonContentValidation
    {
        public static async Task<List<GenerationReturn>> CheckAllContentForBadContentReferences(
            IProgress<string> progress)
        {
            var returnList = new List<GenerationReturn>();

            var db = await Db.Context();

            returnList.AddRange(await CheckForBadContentReferences(
                (await db.FileContents.ToListAsync()).Cast<IContentCommon>().ToList(), db, progress));
            returnList.AddRange(await CheckForBadContentReferences(
                (await db.GeoJsonContents.ToListAsync()).Cast<IContentCommon>().ToList(), db, progress));
            returnList.AddRange(await CheckForBadContentReferences(
                (await db.ImageContents.ToListAsync()).Cast<IContentCommon>().ToList(), db, progress));
            returnList.AddRange(await CheckForBadContentReferences(
                (await db.LineContents.ToListAsync()).Cast<IContentCommon>().ToList(), db, progress));
            returnList.AddRange(await CheckForBadContentReferences(
                (await db.NoteContents.ToListAsync()).Cast<IContentCommon>().ToList(), db, progress));
            returnList.AddRange(await CheckForBadContentReferences(
                (await db.PhotoContents.ToListAsync()).Cast<IContentCommon>().ToList(), db, progress));
            returnList.AddRange(await CheckForBadContentReferences(
                (await db.PointContents.ToListAsync()).Cast<IContentCommon>().ToList(), db, progress));
            returnList.AddRange(await CheckForBadContentReferences(
                (await db.PostContents.ToListAsync()).Cast<IContentCommon>().ToList(), db, progress));

            return returnList;
        }

        public static async Task<List<GenerationReturn>> CheckForBadContentReferences(List<IContentCommon> content,
            PointlessWaymarksContext db, IProgress<string> progress)
        {
            var returnList = new List<GenerationReturn>();

            if (content == null || !content.Any()) return returnList;

            foreach (var loopContent in content)
                returnList.Add(await CheckForBadContentReferences(loopContent, db, progress));

            return returnList;
        }

        public static async Task<GenerationReturn> CheckForBadContentReferences(IContentCommon content,
            PointlessWaymarksContext db, IProgress<string> progress)
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
                (await Db.PointDetailsForPoint(point.ContentId, db)).ForEach(x => toSearch += x.StructuredDataAsJson);

            if (string.IsNullOrWhiteSpace(toSearch) && !extracted.Any())
                return await GenerationReturn.Success(
                    $"{Db.ContentTypeString(content)} {content.Title} - No Content Ids Found", content.ContentId);

            extracted.AddRange(BracketCodeCommon.BracketCodeContentIds(toSearch));

            if (!extracted.Any())
                return await GenerationReturn.Success(
                    $"{Db.ContentTypeString(content)} {content.Title} - No Content Ids Found", content.ContentId);

            progress?.Report($"Found {extracted.Count} ContentIds to check for {content.Title}");

            var notFoundList = new List<Guid>();

            foreach (var loopExtracted in extracted)
            {
                var contentLookup = await db.ContentFromContentId(loopExtracted);

                if (contentLookup == null)
                {
                    progress?.Report($"ContentId {loopExtracted} Not Found in Db...");
                    notFoundList.Add(loopExtracted);
                }
            }

            if (notFoundList.Any())
                return await GenerationReturn.Error(
                    $"{Db.ContentTypeString(content)} {content.Title} has " +
                    $"Invalid ContentIds in Bracket Codes - {string.Join(", ", notFoundList)}", content.ContentId);

            return await GenerationReturn.Success(
                $"{Db.ContentTypeString(content)} {content.Title} - No Invalid Content Ids Found");
        }

        public static async Task<GenerationReturn> CheckStringForBadContentReferences(string toSearch,
            PointlessWaymarksContext db, IProgress<string> progress)
        {
            var extracted = new List<Guid>();

            if (string.IsNullOrWhiteSpace(toSearch))
                return await GenerationReturn.Success("No Content Ids Found");

            extracted.AddRange(BracketCodeCommon.BracketCodeContentIds(toSearch));

            if (!extracted.Any())
                return await GenerationReturn.Success("No Content Ids Found");

            progress?.Report($"Found {extracted.Count} ContentIds to check for");

            var notFoundList = new List<Guid>();

            foreach (var loopExtracted in extracted)
            {
                var contentLookup = await db.ContentFromContentId(loopExtracted);

                if (contentLookup == null)
                {
                    progress?.Report($"ContentId {loopExtracted} Not Found in Db...");
                    notFoundList.Add(loopExtracted);
                }
            }

            if (notFoundList.Any())
                return await GenerationReturn.Error(
                    $"Invalid ContentIds in Bracket Codes - {string.Join(", ", notFoundList)}");

            return await GenerationReturn.Success("No Invalid Content Ids Found");
        }

        public static (bool isValid, string explanation) ElevationValidation(double? elevation)
        {
            if (elevation == null) return (true, "Null Elevation is Valid");

            if (elevation > 29029)
                return (false,
                    $"Elevations are limited to the elevation of Mount Everest at 29,092 ft above sea level - {elevation} was input...");

            if (elevation < -50000)
                return (false,
                    $"This is very unlikely to be a valid elevation, this exceeds the depth of the Mariana Trench and known Extended-Reach Drilling (as of 2020) - elevations under -50,000 are not considered valid - {elevation} was input...");

            return (true, "Elevation is Valid");
        }


        public static async Task<(bool isValid, string explanation)> FileContentFileValidation(FileInfo fileContentFile,
            Guid? currentContentId)
        {
            if (fileContentFile == null) return (false, "Please choose a file");

            fileContentFile.Refresh();

            if (!fileContentFile.Exists) return (false, "File does not Exist?");

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(fileContentFile.Name)))
                return (false, "Limit File Names to A-Z a-z 0-9 - . _");

            var db = await Db.Context();

            if (await (await Db.Context()).ImageFilenameExistsInDatabase(fileContentFile.Name, currentContentId))
                return (false, "This filename already exists in the database - file names must be unique.");

            return (true, "File is Valid");
        }

        public static async Task<(bool isValid, string explanation)> ImageFileValidation(FileInfo imageFile,
            Guid? currentContentId)
        {
            imageFile.Refresh();

            if (!imageFile.Exists) return (false, "File does not Exist?");

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(imageFile.Name)))
                return (false, "Limit File Names to A-Z a-z 0-9 - . _");

            if (!FolderFileUtility.PictureFileTypeIsSupported(imageFile))
                return (false, "The file doesn't appear to be a supported file type.");

            var db = await Db.Context();

            if (await (await Db.Context()).ImageFilenameExistsInDatabase(imageFile.Name, currentContentId))
                return (false, "This filename already exists in the database - image file names must be unique.");

            return (true, "File is Valid");
        }

        public static (bool isValid, string explanation) LatitudeValidation(double latitude)
        {
            if (latitude > 90 || latitude < -90)
                return (false, $"Latitude on Earth must be between -90 and 90 - {latitude} is not valid.");

            return (true, "Latitude is Valid");
        }

        public static (bool isValid, string explanation) LongitudeValidation(double longitude)
        {
            if (longitude > 180 || longitude < -180)
                return (false, $"Longitude on Earth must be between -180 and 180 - {longitude} is not valid.");

            return (true, "Longitude is Valid");
        }

        public static async Task<(bool isValid, string explanation)> PhotoFileValidation(FileInfo photoFile,
            Guid? currentContentId)
        {
            photoFile.Refresh();

            if (!photoFile.Exists) return (false, "File does not Exist?");

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(Path.GetFileNameWithoutExtension(photoFile.Name)))
                return (false, "Limit File Names to A-Z a-z 0-9 - . _");

            if (!FolderFileUtility.PictureFileTypeIsSupported(photoFile))
                return (false, "The file doesn't appear to be a supported file type.");

            var db = await Db.Context();

            if (await (await Db.Context()).PhotoFilenameExistsInDatabase(photoFile.Name, currentContentId))
                return (false, "This filename already exists in the database - photo file names must be unique.");

            return (true, "File is Valid");
        }

        public static (bool isValid, string explanation) ValidateBodyContentFormat(string contentFormat)
        {
            if (string.IsNullOrWhiteSpace(contentFormat)) return (false, "Body Content Format must be set");

            if (Enum.TryParse(typeof(ContentFormatEnum), contentFormat, true, out _))
                return (true, string.Empty);

            return (false, $"Could not parse {contentFormat} into a known Content Format");
        }

        public static async Task<(bool valid, string explanation)> ValidateContentCommon(IContentCommon toValidate)
        {
            if (toValidate == null) return (false, "Null Content to Validate");

            var isNewEntry = toValidate.Id < 1;

            var isValid = true;
            var errorMessage = new List<string>();

            if (toValidate.ContentId == Guid.Empty)
            {
                isValid = false;
                errorMessage.Add("Content ID is Empty");
            }

            var titleValidation = ValidateTitle(toValidate.Title);

            if (!titleValidation.isValid)
            {
                isValid = false;
                errorMessage.Add(titleValidation.explanation);
            }

            var summaryValidation = ValidateSummary(toValidate.Summary);

            if (!summaryValidation.isValid)
            {
                isValid = false;
                errorMessage.Add(summaryValidation.explanation);
            }

            var (createdUpdatedIsValid, createdUpdatedExplanation) =
                ValidateCreatedAndUpdatedBy(toValidate, isNewEntry);

            if (!createdUpdatedIsValid)
            {
                isValid = false;
                errorMessage.Add(createdUpdatedExplanation);
            }

            var slugValidation = await ValidateSlugLocalAndDb(toValidate.Slug, toValidate.ContentId);

            if (!slugValidation.isValid)
            {
                isValid = false;
                errorMessage.Add(slugValidation.explanation);
            }

            var folderValidation = ValidateFolder(toValidate.Folder);

            if (!folderValidation.isValid)
            {
                isValid = false;
                errorMessage.Add(folderValidation.explanation);
            }

            var tagValidation = ValidateTags(toValidate.Tags);

            if (!tagValidation.isValid)
            {
                isValid = false;
                errorMessage.Add(tagValidation.explanation);
            }

            var bodyContentFormatValidation = ValidateBodyContentFormat(toValidate.BodyContentFormat);

            if (!bodyContentFormatValidation.isValid)
            {
                isValid = false;
                errorMessage.Add(bodyContentFormatValidation.explanation);
            }

            var contentIdCheck = await CheckForBadContentReferences(toValidate, await Db.Context(), null);

            if (contentIdCheck.HasError)
            {
                isValid = false;
                errorMessage.Add(contentIdCheck.GenerationNote);
            }

            return (isValid, string.Join(Environment.NewLine, errorMessage));
        }

        public static (bool valid, string explanation) ValidateCreatedAndUpdatedBy(
            ICreatedAndLastUpdateOnAndBy toValidate, bool isNewEntry)
        {
            var isValid = true;
            var errorMessage = new List<string>();

            if (toValidate.CreatedOn == DateTime.MinValue)
            {
                isValid = false;
                errorMessage.Add($"Created on of {toValidate.CreatedOn} is not valid.");
            }

            var createdByValidation = ValidateCreatedBy(toValidate.CreatedBy);

            if (!createdByValidation.valid)
            {
                isValid = false;
                errorMessage.Add(createdByValidation.explanation);
            }

            if (!isNewEntry && string.IsNullOrWhiteSpace(toValidate.LastUpdatedBy))
            {
                isValid = false;
                errorMessage.Add("Updated by can not be blank when updating an entry");
            }

            if (!isNewEntry && toValidate.LastUpdatedOn == null || toValidate.LastUpdatedOn == DateTime.MinValue)
            {
                isValid = false;
                errorMessage.Add("Last Updated On can not be blank/empty when updating an entry");
            }

            return (isValid, string.Join(Environment.NewLine, errorMessage));
        }

        public static (bool valid, string explanation) ValidateCreatedBy(string createdBy)
        {
            if (string.IsNullOrWhiteSpace(createdBy.TrimNullToEmpty())) return (false, "Created by can not be blank.");

            return (true, "Created By is Ok");
        }

        public static (bool isValid, string explanation) ValidateFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return (false, "Folder can't be blank or only whitespace.");

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(folder))
                return (false, "Limit folder names to a-z A-Z 0-9 _ -");

            return (true, string.Empty);
        }

        public static async Task<(bool isValid, string explanation)> ValidateLinkContentLinkUrl(string url,
            Guid? contentGuid)
        {
            if (string.IsNullOrWhiteSpace(url)) return (false, "Link URL can not be blank");

            var db = await Db.Context();

            if (contentGuid == null)
            {
                var duplicateUrl = await db.LinkContents.AnyAsync(x => x.Url.ToLower() == url.ToLower());
                if (duplicateUrl)
                    return (false,
                        "URL Already exists in the database - duplicates are not allowed, try editing the existing entry to add new/updated information.");
            }
            else
            {
                var duplicateUrl = await db.LinkContents.AnyAsync(x =>
                    x.ContentId != contentGuid.Value && x.Url.ToLower() == url.ToLower());
                if (duplicateUrl)
                    return (false,
                        "URL Already exists in the database - duplicates are not allowed, try editing the existing entry to add new/updated information.");
            }

            return (true, string.Empty);
        }

        public static (bool isValid, string explanation) ValidateSlugLocal(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return (false, "Slug can't be blank or only whitespace.");

            if (!FolderFileUtility.IsNoUrlEncodingNeededLowerCase(slug))
                return (false, "Slug should only contain a-z 0-9 _ -");

            if (slug.Length > 100) return (false, "Limit slugs to 100 characters.");

            return (true, string.Empty);
        }

        public static async Task<(bool isValid, string explanation)> ValidateSlugLocalAndDb(string slug, Guid contentId)
        {
            var localValidation = ValidateSlugLocal(slug);

            if (!localValidation.isValid) return localValidation;

            if (await (await Db.Context()).SlugExistsInDatabase(slug, contentId))
                return (false, "This slug already exists in the database - slugs must be unique.");

            return (true, string.Empty);
        }

        public static (bool isValid, string explanation) ValidateSummary(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary)) return (false, "Summary can not be blank");

            return (true, string.Empty);
        }

        public static (bool isValid, string explanation) ValidateTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags)) return (false, "At least one tag must be included.");

            var tagList = Db.TagListParse(tags);

            if (tagList.Any(x => !FolderFileUtility.IsNoUrlEncodingNeededLowerCaseSpacesOk(x) || x.Length > 200))
                return (false, "Limit tags to a-z 0-9 _ - [space] and less than 200 characters per tag.");

            return (true, string.Empty);
        }

        public static (bool isValid, string explanation) ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return (false, "Title can not be blank");

            return (true, string.Empty);
        }

        public static (bool isValid, string explanation) ValidateUpdateContentFormat(string contentFormat)
        {
            if (string.IsNullOrWhiteSpace(contentFormat)) return (false, "Update Content Format must be set");

            if (Enum.TryParse(typeof(ContentFormatEnum), contentFormat, true, out _))
                return (true, string.Empty);

            return (false, $"Could not parse {contentFormat} into a known Content Format");
        }
    }
}