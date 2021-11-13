﻿using HtmlTags;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.NoteHtml;

public static class NoteParts
{
    public static string NoteCreatedByAndUpdatedOnString(ICreatedAndLastUpdateOnAndBy dbEntry)
    {
        var createdUpdatedString = $"{dbEntry.CreatedBy}";

        var onlyCreated = false;

        if (dbEntry.LastUpdatedOn != null && dbEntry.CreatedOn.Date == dbEntry.LastUpdatedOn.Value.Date)
            if (!string.Equals(dbEntry.CreatedBy, dbEntry.LastUpdatedBy, StringComparison.OrdinalIgnoreCase))
            {
                createdUpdatedString = $"{dbEntry.LastUpdatedBy} ";
                onlyCreated = true;
            }

        createdUpdatedString += $" {dbEntry.CreatedOn:M/d/yyyy}";

        if (onlyCreated) return createdUpdatedString.Trim();

        if (string.IsNullOrWhiteSpace(dbEntry.LastUpdatedBy) && dbEntry.LastUpdatedOn == null)
            return createdUpdatedString;

        if (dbEntry.LastUpdatedOn != null && dbEntry.CreatedOn.Date == dbEntry.LastUpdatedOn.Value.Date)
            return createdUpdatedString;

        var updatedString = ", Updated";

        if (!string.IsNullOrWhiteSpace(dbEntry.LastUpdatedBy)) updatedString += $" by {dbEntry.LastUpdatedBy}";

        if (dbEntry.LastUpdatedOn != null) updatedString += $" {dbEntry.LastUpdatedOn.Value:M/d/yyyy}";

        return (createdUpdatedString + updatedString).Trim();
    }

    public static HtmlTag NoteTitleDiv(NoteContent dbEntry)
    {
        var titleContainer = new HtmlTag("div").AddClass("title-container");
        titleContainer.Children.Add(new HtmlTag("h1").AddClass("title-content").Text(dbEntry.Title));
        return titleContainer;
    }


    public static HtmlTag NoteTitleLinkDiv(NoteContent dbEntry)
    {
        var settings = UserSettingsSingleton.CurrentSettings();

        var titleContainer = new HtmlTag("div").AddClass("title-link-container");

        var header = new HtmlTag("h2").AddClass("title-link-content");
        var linkToFullPost = new LinkTag(dbEntry.Title, settings.NotePageUrl(dbEntry));
        header.Children.Add(linkToFullPost);

        titleContainer.Children.Add(header);

        return titleContainer;
    }

    public static string TrimLastCharacter(this string str)
    {
        return string.IsNullOrEmpty(str) ? str : str.TrimEnd(str[^1]);
    }
}