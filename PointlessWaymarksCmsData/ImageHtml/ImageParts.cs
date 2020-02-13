using HtmlTags;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.ImageHtml
{
    public static class ImageParts
    {
        public static HtmlTag ImageSourceNotesDivTag(ImageContent dbEntry)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.ImageSourceNotes)) return HtmlTag.Empty();

            var sourceNotesContainer = new DivTag().AddClass("image-source-notes-container");
            var sourceNotes = new DivTag().AddClass("image-source-notes-content").Encoded(false)
                .Text(BracketCodeCommon.ProcessCodesAndMarkdownForSite($"Source: {dbEntry.ImageSourceNotes}"));
            sourceNotesContainer.Children.Add(sourceNotes);

            return sourceNotesContainer;
        }
    }
}