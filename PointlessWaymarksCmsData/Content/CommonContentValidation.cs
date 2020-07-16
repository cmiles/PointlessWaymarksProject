using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;

namespace PointlessWaymarksCmsData.Content
{
    public static class CommonContentValidation
    {
        public static async Task<(bool valid, string explanation)> ValidateContentCommon(IContentCommon toValidate)
        {
            if (toValidate == null) return (false, "Null Content to Validate");

            var isNewEntry = toValidate.Id < 1;

            var isValid = true;
            var errorMessage = new List<string>();

            if (isNewEntry && toValidate.ContentId == Guid.Empty)
            {
                isValid = false;
                errorMessage.Add("Content ID is Empty");
            }

            if (toValidate.ContentVersion == DateTime.MinValue)
            {
                isValid = false;
                errorMessage.Add($"Content Version of {toValidate.ContentVersion} is not valid.");
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

            var slugValidation = ValidateSlug(toValidate.Slug);

            if (!slugValidation.isValid)
            {
                isValid = false;
                errorMessage.Add(slugValidation.explanation);
            }
            else
            {
                if (await (await Db.Context()).SlugExistsInDatabase(toValidate.Slug, toValidate.ContentId))
                {
                    isValid = false;
                    errorMessage.Add("This slug already exists in the database - slugs must be unique.");
                }
            }

            if (string.IsNullOrWhiteSpace(toValidate.Folder))
            {
                isValid = false;
                errorMessage.Add("Folder can not be blank.");
            }
            else
            {
                if (!FolderFileUtility.IsNoUrlEncodingNeeded(toValidate.Folder))
                {
                    isValid = false;
                    errorMessage.Add("Limit Folder Names to A-Z a-z - . _");
                }
            }

            if (!string.IsNullOrWhiteSpace(toValidate.Tags))
            {
                var tags = Db.TagListParse(toValidate.Tags);

                if (tags.Any(x => !FolderFileUtility.IsNoUrlEncodingNeededLowerCaseSpacesOk(x) || x.Length > 200))
                {
                    isValid = false;
                    errorMessage.Add(
                        "Tags should be limited to a-z A-Z _ - [space] and each tag should be less than 200 characters");
                }
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

            if (string.IsNullOrWhiteSpace(toValidate.CreatedBy))
            {
                isValid = false;
                errorMessage.Add("Created by can not be blank.");
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

        public static (bool isValid, string explanation) ValidateFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return (false, "Folder can't be blank or only whitespace.");

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(folder))
                return (false, "Limit folder names to a-z A-Z 0-9 _ -");

            return (true, string.Empty);
        }

        public static (bool isValid, string explanation) ValidateSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return (false, "Slug can't be blank or only whitespace.");

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(slug)) return (false, "Slug should only contain 0-9 a-z _ -");

            if (slug.Length > 100) return (false, "Limit slugs to 100 characters.");

            return (true, string.Empty);
        }

        public static (bool isValid, string explanation) ValidateSummary(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary)) return (false, "Summary can not be blank");

            return (true, string.Empty);
        }

        public static (bool isValid, string explanation) ValidateTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags)) return (true, string.Empty);

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
    }
}