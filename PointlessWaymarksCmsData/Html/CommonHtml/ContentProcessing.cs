using System;
using Markdig;
using PointlessWaymarksCmsData.Database;

namespace PointlessWaymarksCmsData.Html.CommonHtml
{
    public static class ContentProcessing
    {
        public static string ProcessContent(string toProcess, string contentFormat)
        {
            return ProcessContent(toProcess, (ContentFormatEnum) Enum.Parse(typeof(ContentFormatEnum), contentFormat, true));
        }

        public static string ProcessContent(string toProcess, ContentFormatEnum contentFormat)
        {
            if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

            switch (contentFormat)
            {
                case ContentFormatEnum.MarkdigMarkdown01:
                    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                    return Markdown.ToHtml(toProcess, pipeline);
                case ContentFormatEnum.Html01:
                    return toProcess;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}