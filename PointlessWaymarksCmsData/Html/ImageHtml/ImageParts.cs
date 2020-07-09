using System;
using HtmlTags;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;

namespace PointlessWaymarksCmsData.Html.ImageHtml
{
    public static class ImageParts
    {
        public static HtmlTag ImageSourceNotesDivTag(ImageContent dbEntry, IProgress<string> progress = null)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.BodyContent)) return HtmlTag.Empty();

            var sourceNotesContainer = new DivTag().AddClass("image-source-notes-container");
            var sourceNotes = new DivTag().AddClass("image-source-notes-content").Encoded(false).Text(
                ContentProcessing.ProcessContent(BracketCodeCommon.ProcessCodesForSite($"Source: {dbEntry.BodyContent}", progress), ContentFormatEnum.MarkdigMarkdown01));
            sourceNotesContainer.Children.Add(sourceNotes);

            return sourceNotesContainer;
        }
    }
}