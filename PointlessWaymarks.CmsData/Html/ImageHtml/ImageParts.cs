﻿using System;
using HtmlTags;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html.CommonHtml;

namespace PointlessWaymarks.CmsData.Html.ImageHtml
{
    public static class ImageParts
    {
        public static HtmlTag ImageSourceNotesDivTag(ImageContent dbEntry, IProgress<string> progress = null)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.BodyContent)) return HtmlTag.Empty();

            var sourceNotesContainer = new DivTag().AddClass("image-source-notes-container");
            var sourceNotes = new DivTag().AddClass("image-source-notes-content").Encoded(false).Text(
                ContentProcessing.ProcessContent(
                    BracketCodeCommon.ProcessCodesForSite($"Source: {dbEntry.BodyContent}", progress),
                    ContentFormatEnum.MarkdigMarkdown01));
            sourceNotesContainer.Children.Add(sourceNotes);

            return sourceNotesContainer;
        }
    }
}