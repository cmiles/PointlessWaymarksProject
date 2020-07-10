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

            if (string.IsNullOrWhiteSpace(toValidate.Title))
            {
                isValid = false;
                errorMessage.Add("Title can not be blank.");
            }

            if (string.IsNullOrWhiteSpace(toValidate.Summary))
            {
                isValid = false;
                errorMessage.Add("Summary can not be blank.");
            }

            var (createdUpdatedIsValid, createdUpdatedExplanation) =
                ValidateCreatedAndUpdatedBy(toValidate, isNewEntry);

            if (!createdUpdatedIsValid)
            {
                isValid = false;
                errorMessage.Add(createdUpdatedExplanation);
            }

            if (string.IsNullOrWhiteSpace(toValidate.Slug))
            {
                isValid = false;
                errorMessage.Add("Slug can not be blank.");
            }

            if (!string.IsNullOrWhiteSpace(toValidate.Slug))
            {
                if (!FolderFileUtility.IsNoUrlEncodingNeeded(toValidate.Slug) || !toValidate.Slug.All(char.IsLower))
                {
                    isValid = false;
                    errorMessage.Add("Limit Slugs to a-z - . _");
                }

                if (await (await Db.Context()).SlugExistsInDatabase(toValidate.Slug, toValidate.ContentId))
                    isValid = false;
                errorMessage.Add("This slug already exists in the database - slugs must be unique.");
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
    }
}