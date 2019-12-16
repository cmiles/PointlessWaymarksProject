using System;
using Markdig;

namespace TheLemmonWorkshopData
{
    public static class MainImageProcessor
    {
        public static (bool success, string output) MainImageHtml(string format, string imageRawString)
        {
            var parsed = Enum.TryParse(format, true, out MainImageContentFormatEnum parsedEnum);

            return !parsed ? (false, imageRawString) : (true, MainImageHtml(parsedEnum, imageRawString));
        }

        public static string MainImageHtml(MainImageContentFormatEnum format, string imageRawString)
        {
            switch (format)
            {
                case MainImageContentFormatEnum.Html01:
                    return imageRawString;

                case MainImageContentFormatEnum.MarkdigMarkdown01:
                    return Markdown.ToHtml(imageRawString ?? string.Empty);

                default:
                    return imageRawString;
            }
        }
    }
}