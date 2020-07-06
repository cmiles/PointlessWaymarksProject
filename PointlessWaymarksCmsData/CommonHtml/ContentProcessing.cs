using System;
using System.Collections.Generic;
using System.Text;
using Markdig;

namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class ContentProcessing
    {
        public static string ProcessContent(string toProcess, string processFormat)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var markdownOut = Markdown.ToHtml(toProcess, pipeline);

            return markdownOut;
        }
    }
}
