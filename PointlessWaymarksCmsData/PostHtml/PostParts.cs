using System;
using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.PostHtml
{
    public static class PostParts
    {
        public static HtmlTag CreatedByAndUpdatedOnDiv(PostContent dbEntry)
        {
            var titleContainer = new HtmlTag("div").AddClass("post-title-area-created-and-updated-container");
            titleContainer.Children.Add(new HtmlTag("h3").AddClass("post-title-area-created-and-updated-content")
                .Text(CreatedByAndUpdatedOnString(dbEntry)));
            return titleContainer;
        }

        public static string CreatedByAndUpdatedOnString(PostContent dbEntry)
        {
            var createdUpdatedString = string.Empty;

            var onlyCreated = false;

            if (dbEntry.LastUpdatedOn != null && dbEntry.CreatedOn.Date == dbEntry.LastUpdatedOn.Value.Date)
            {
                if (string.Compare(dbEntry.CreatedBy, dbEntry.LastUpdatedBy, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    createdUpdatedString += $"Created by {dbEntry.CreatedBy} and {dbEntry.LastUpdatedBy} ";
                    onlyCreated = true;
                }
                else
                {
                    createdUpdatedString += $"Created by {dbEntry.CreatedBy} ";
                }
            }

            createdUpdatedString += $"on {dbEntry.CreatedOn:M/d/yyyy}. ";

            if (onlyCreated) return createdUpdatedString;

            if (string.IsNullOrWhiteSpace(dbEntry.LastUpdatedBy) && dbEntry.LastUpdatedOn == null)
                return createdUpdatedString;

            if (dbEntry.LastUpdatedOn != null && dbEntry.CreatedOn.Date == dbEntry.LastUpdatedOn.Value.Date)
                return createdUpdatedString;

            var updatedString = "Updated";

            if (!string.IsNullOrWhiteSpace(dbEntry.LastUpdatedBy)) updatedString += $" by {dbEntry.LastUpdatedBy}";

            if (dbEntry.LastUpdatedOn != null) updatedString += $" on {dbEntry.LastUpdatedOn.Value:M/d/yyyy}";

            updatedString += ".";

            return (createdUpdatedString + updatedString).Trim();
        }

        public static HtmlTag PostBodyDiv(PostContent dbEntry)
        {
            var bodyContainer = new HtmlTag("div").AddClass("post-body-container");

            var bodyText = BracketCodes.PhotoCodeProcessToFigure(dbEntry.BodyContent);

            var bodyHtmlProcessing = ContentProcessor.ContentHtml(dbEntry.BodyContentFormat, bodyText);

            if (bodyHtmlProcessing.success)
                bodyContainer.Children.Add(new HtmlTag("div").AddClass("post-body-content").Encoded(false)
                    .Text(bodyHtmlProcessing.output));

            return bodyContainer;
        }
    }
}