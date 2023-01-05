using Markdig;
using Markdig.Extensions.Hardlines;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class ContentProcessing
{
    public static string ProcessContent(string? toProcess, string? contentFormat)
    {
        if (string.IsNullOrWhiteSpace(contentFormat)) return toProcess.TrimNullToEmpty();

        return ProcessContent(toProcess,
            (ContentFormatEnum) Enum.Parse(typeof(ContentFormatEnum), contentFormat, true));
    }

    public static string ProcessContent(string? toProcess, ContentFormatEnum contentFormat)
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
                throw new ArgumentOutOfRangeException( $"{nameof(contentFormat)} is not a recognized {nameof(ContentFormatEnum)} for {nameof(ProcessContent)}");
        }
    }
}