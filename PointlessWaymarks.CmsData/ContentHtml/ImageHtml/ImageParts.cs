using HtmlTags;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.ContentHtml.ImageHtml
{
    public static class ImageParts
    {
        public static async Task<HtmlTag> ImageSourceNotesDivTag(ImageContent dbEntry,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(dbEntry.BodyContent)) return HtmlTag.Empty();

            var sourceNotesContainer = new DivTag().AddClass("image-source-notes-container");
            var sourceNotes = new DivTag().AddClass("image-source-notes-content").Encoded(false).Text(
                ContentProcessing.ProcessContent(
                    await BracketCodeCommon.ProcessCodesForSite($"Source: {dbEntry.BodyContent}", progress).ConfigureAwait(false),
                    ContentFormatEnum.MarkdigMarkdown01));
            sourceNotesContainer.Children.Add(sourceNotes);

            return sourceNotesContainer;
        }
    }
}