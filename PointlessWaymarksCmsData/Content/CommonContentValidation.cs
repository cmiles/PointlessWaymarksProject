using System;
using System.Collections.Generic;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Content
{
    public static class CommonContentValidation
    {
        public static (bool valid, string explanation) ValidateContentCommon(IContentCommon toValidate)
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

            if (toValidate.CreatedOn == DateTime.MinValue)
            {
                isValid = false;
                errorMessage.Add($"Created on of {toValidate.CreatedOn} is not valid.");
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

            if (string.IsNullOrWhiteSpace(toValidate.Slug))
            {
                isValid = false;
                errorMessage.Add("Slug can not be blank.");
            }

            if (string.IsNullOrWhiteSpace(toValidate.Folder))
            {
                isValid = false;
                errorMessage.Add("Folder can not be blank.");
            }
            else
            {
                if (!FolderFileUtility.IsNoUrlEncodingNeededFilename(toValidate.Folder))
                {
                    isValid = false;
                    errorMessage.Add("Limit Folder Names to A-Z a-z - . _");
                }
            }

            if (isNewEntry && string.IsNullOrWhiteSpace(toValidate.CreatedBy))
            {
                isValid = false;
                errorMessage.Add("Created by can not be blank for a new entry.");
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