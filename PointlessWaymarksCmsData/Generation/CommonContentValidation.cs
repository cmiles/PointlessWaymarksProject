using System;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.Generation
{
    public static class CommonContentValidation
    {
        public static (bool valid, string explanation) ValidateContentCommon(IContentCommon toValidate)
        {
            if (toValidate == null) return (false, "Null Content to Validate");

            var isNewEntry = toValidate.Id < 1;

            var isValid = true;
            var errorMessage = string.Empty;

            if (isNewEntry && toValidate.ContentId == Guid.Empty)
            {
                isValid = false;
                errorMessage += "Content ID is Empty";
            }

            if (toValidate.CreatedOn == DateTime.MinValue)
            {
                isValid = false;
                errorMessage += $"Created on of {toValidate.CreatedOn} is not valid.";
            }

            if (toValidate.ContentVersion == DateTime.MinValue)
            {
                isValid = false;
                errorMessage += $"Content Version of {toValidate.ContentVersion} is not valid.";
            }

            if (string.IsNullOrWhiteSpace(toValidate.Title))
            {
                isValid = false;
                errorMessage += "Title can not be blank.";
            }

            if (string.IsNullOrWhiteSpace(toValidate.Summary))
            {
                isValid = false;
                errorMessage += "Summary can not be blank.";
            }

            if (string.IsNullOrWhiteSpace(toValidate.Slug))
            {
                isValid = false;
                errorMessage += "Slug can not be blank.";
            }

            if (string.IsNullOrWhiteSpace(toValidate.Folder))
            {
                isValid = false;
                errorMessage += "Folder can not be blank.";
            }
            else
            {
                if (!FolderFileUtility.IsNoUrlEncodingNeededFilename(toValidate.Folder))
                {
                    isValid = false;
                    errorMessage += "Limit Folder Names to A-Z a-z - . _";
                }
            }

            if (isNewEntry && string.IsNullOrWhiteSpace(toValidate.CreatedBy))
                return (false, "Created by can not be blank for a new entry.");

            if (!isNewEntry && string.IsNullOrWhiteSpace(toValidate.LastUpdatedBy))
                return (false, "Updated by can not be blank when updating an entry");

            return (isValid, errorMessage);
        }
    }
}