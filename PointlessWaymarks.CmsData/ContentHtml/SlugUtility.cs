using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;

namespace PointlessWaymarks.CmsData.ContentHtml
{
    public static class SlugUtility
    {
        private static string? ConvertEdgeCases(char c, bool toLower)
        {
            var swap = c switch
            {
                'ı' => "i",
                'ł' => "l",
                'Ł' => toLower ? "l" : "L",
                'đ' => "d",
                'ß' => "ss",
                'ø' => "o",
                'Þ' => "th",
                _ => null
            };

            return swap;
        }

        /// <summary>
        ///     Creates a Slug.
        /// </summary>
        /// <param name="toLower"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string Create(bool toLower, params string[] values)
        {
            return Create(toLower, string.Join("-", values));
        }

        /// <summary>
        ///     Creates a slug.
        /// </summary>
        /// <param name="toLower"></param>
        /// <param name="value"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        //     References:
        //     https://stackoverflow.com/questions/25259/how-does-stack-overflow-generate-its-seo-friendly-urls
        //     http://www.unicode.org/reports/tr15/tr15-34.html
        //     https://meta.stackexchange.com/questions/7435/non-us-ascii-characters-dropped-from-full-profile-url/7696#7696
        //     https://stackoverflow.com/questions/25259/how-do-you-include-a-webpage-title-as-part-of-a-webpage-url/25486#25486
        //     https://stackoverflow.com/questions/3769457/how-can-i-remove-accents-on-a-string
        public static string Create(bool toLower, string? value, int maxLength = 100)
        {
            if (value == null)
                return "";

            var normalized = value.Normalize(NormalizationForm.FormKD);

            var len = normalized.Length;
            var prevDash = false;
            var sb = new StringBuilder(len);

            for (var i = 0; i < len; i++)
            {
                var c = normalized[i];
                if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                {
                    if (prevDash)
                    {
                        sb.Append('-');
                        prevDash = false;
                    }

                    sb.Append(c);
                }
                else if (c is >= 'A' and <= 'Z')
                {
                    if (prevDash)
                    {
                        sb.Append('-');
                        prevDash = false;
                    }

                    // Tricky way to convert to lowercase
                    if (toLower)
                        sb.Append((char) (c | 32));
                    else
                        sb.Append(c);
                }
                else if (c is ' ' or ',' or '.' or '/' or '\\' or '-' or '_' or '=')
                {
                    if (!prevDash && sb.Length > 0) prevDash = true;
                }
                else
                {
                    var swap = ConvertEdgeCases(c, toLower);

                    if (swap != null)
                    {
                        if (prevDash)
                        {
                            sb.Append('-');
                            prevDash = false;
                        }

                        sb.Append(swap);
                    }
                }

                if (sb.Length == maxLength)
                    break;
            }

            return sb.ToString();
        }

        /// <summary>
        ///     This is intended for use in the live processing of user input where you want to create slug like strings but to be
        ///     friendly to typed input (for example so trailing spaces must be allowed to avoid fighting the user) - in general
        ///     this is not as strict as CreateSpacedString.
        /// </summary>
        /// <param name="toLower"></param>
        /// <param name="value"></param>
        /// <param name="allowedBeyondAtoZ1To9"></param>
        /// <returns></returns>
        public static string CreateRelaxedInputSpacedString(bool toLower, string? value,
            List<char>? allowedBeyondAtoZ1To9 = null)
        {
            if (value == null)
                return "";

            allowedBeyondAtoZ1To9 ??= new List<char>();

            var normalized = value.Normalize(NormalizationForm.FormKD);

            var len = normalized.Length;
            var sb = new StringBuilder(len);

            for (var i = 0; i < len; i++)
            {
                var c = normalized[i];
                if (c is >= 'a' and <= 'z' or >= '0' and <= '9' || allowedBeyondAtoZ1To9.Contains(c))
                {
                    sb.Append(c);
                }
                else if (c is >= 'A' and <= 'Z')
                {
                    // Tricky way to convert to lowercase
                    if (toLower)
                        sb.Append((char) (c | 32));
                    else
                        sb.Append(c);
                }
                else
                {
                    var swap = ConvertEdgeCases(c, toLower);

                    if (swap != null) sb.Append(swap);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        ///     This method mimics the Create method but is focused on creating a spaced string with the intent that in some cases
        ///     this may create a format that communicates the same information and intent as the Create Slug method but is easier
        ///     to read and more user friendly.
        /// </summary>
        /// <param name="toLower"></param>
        /// <param name="value"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string CreateSpacedString(bool toLower, string value, int? maxLength = 100)
        {
            var normalized = value.Normalize(NormalizationForm.FormKD);

            var len = normalized.Length;
            var previousSpace = false;
            var sb = new StringBuilder(len);

            for (var i = 0; i < len; i++)
            {
                var c = normalized[i];
                if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-')
                {
                    if (previousSpace)
                    {
                        sb.Append(' ');
                        previousSpace = false;
                    }

                    sb.Append(c);
                }
                else if (c is >= 'A' and <= 'Z')
                {
                    if (previousSpace)
                    {
                        sb.Append(' ');
                        previousSpace = false;
                    }

                    // Tricky way to convert to lowercase
                    if (toLower)
                        sb.Append((char) (c | 32));
                    else
                        sb.Append(c);
                }
                else if (c is ',' or '.' or '/' or '\\' or '=' or ' ' or ';')
                {
                    if (!previousSpace && sb.Length > 0) previousSpace = true;
                }
                else
                {
                    var swap = ConvertEdgeCases(c, toLower);

                    if (swap != null)
                    {
                        if (previousSpace)
                        {
                            sb.Append(' ');
                            previousSpace = false;
                        }

                        sb.Append(swap);
                    }
                }

                if (maxLength != null && sb.Length == maxLength)
                    break;
            }

            return sb.ToString();
        }


        public static async Task<bool> FileFilenameExistsInDatabase(this PointlessWaymarksContext context,
            string filename, Guid? exceptInThisContent)
        {
            if (string.IsNullOrWhiteSpace(filename)) return false;

            bool imageCheck;

            if (exceptInThisContent == null)
                imageCheck = await context.FileContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                    .AnyAsync(x => x.OriginalFileName!.ToLower() == filename.ToLower());
            else
                imageCheck = await context.FileContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                    .AnyAsync(x =>
                        x.OriginalFileName!.ToLower() == filename.ToLower() &&
                        x.ContentId != exceptInThisContent.Value);

            return imageCheck;
        }

        public static async Task<bool> ImageFilenameExistsInDatabase(this PointlessWaymarksContext context,
            string filename, Guid? exceptInThisContent)
        {
            if (string.IsNullOrWhiteSpace(filename)) return false;

            bool imageCheck;

            if (exceptInThisContent == null)
                imageCheck = await context.ImageContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                    .AnyAsync(x => x.OriginalFileName!.ToLower() == filename.ToLower());
            else
                imageCheck = await context.ImageContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                    .AnyAsync(x =>
                        x.OriginalFileName!.ToLower() == filename.ToLower() &&
                        x.ContentId != exceptInThisContent.Value);

            return imageCheck;
        }

        public static async Task<bool> PhotoFilenameExistsInDatabase(this PointlessWaymarksContext context,
            string filename, Guid? exceptInThisContent)
        {
            if (string.IsNullOrWhiteSpace(filename)) return false;

            bool photoCheck;

            if (exceptInThisContent == null)
                photoCheck = await context.PhotoContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                    .AnyAsync(x => x.OriginalFileName!.ToLower() == filename.ToLower());
            else
                photoCheck = await context.PhotoContents.Where(x => !string.IsNullOrWhiteSpace(x.OriginalFileName))
                    .AnyAsync(x =>
                        x.OriginalFileName!.ToLower() == filename.ToLower() &&
                        x.ContentId != exceptInThisContent.Value);

            return photoCheck;
        }

        public static string RandomLowerCaseString(int length)
        {
            // ReSharper disable once StringLiteralTypo
            var chars = "abcdefghijklmnopqrstuvwxyz";
            var stringChars = new char[length];
            var random = new Random();

            for (var i = 0; i < stringChars.Length; i++) stringChars[i] = chars[random.Next(chars.Length)];

            return new string(stringChars);
        }

        public static async Task<bool> SlugExistsInDatabase(this PointlessWaymarksContext context, string? slug,
            Guid? excludedContentId)
        {
            if (string.IsNullOrWhiteSpace(slug)) return false;

            //!!Content Type List!!
            if (excludedContentId == null)
            {
                var fileCheck = await context.FileContents.AnyAsync(x => x.Slug == slug);
                var imageCheck = await context.ImageContents.AnyAsync(x => x.Slug == slug);
                var noteCheck = await context.NoteContents.AnyAsync(x => x.Slug == slug);
                var photoCheck = await context.PhotoContents.AnyAsync(x => x.Slug == slug);
                var pointCheck = await context.PointContents.AnyAsync(x => x.Slug == slug);
                var postCheck = await context.PostContents.AnyAsync(x => x.Slug == slug);

                return photoCheck || postCheck || imageCheck || noteCheck || fileCheck || pointCheck;
            }

            var fileExcludeCheck =
                await context.FileContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);
            var imageExcludeCheck =
                await context.ImageContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);
            var noteExcludeCheck =
                await context.NoteContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);
            var photoExcludeCheck =
                await context.PhotoContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);
            var pointExcludeCheck =
                await context.PointContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);
            var postExcludeCheck =
                await context.PostContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);


            return photoExcludeCheck || postExcludeCheck || imageExcludeCheck || noteExcludeCheck || fileExcludeCheck ||
                   pointExcludeCheck;
        }
    }
}