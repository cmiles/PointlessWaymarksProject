using System;
using Markdig;

namespace TheLemmonWorkshopData
{
    public static class ContentProcessor
    {
        public static (bool success, string output) ContentHtml(string format, string imageRawString)
        {
            var parsed = Enum.TryParse(format, true, out ContentFormatEnum parsedEnum);

            return !parsed ? (false, imageRawString) : (true, ContentHtml(parsedEnum, imageRawString));
        }

        public static string ContentHtml(ContentFormatEnum format, string imageRawString)
        {
            switch (format)
            {
                case ContentFormatEnum.Html01:
                    return imageRawString;

                case ContentFormatEnum.MarkdigMarkdown01:
                    return Markdown.ToHtml(imageRawString ?? string.Empty);

                default:
                    return imageRawString;
            }
        }
    }
}