using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PointlessWaymarksCmsData
{
    public static class SlugUtility
    {
        private static string ConvertEdgeCases(char c, bool toLower)
        {
            string swap = null;
            switch (c)
            {
                case 'ı':
                    swap = "i";
                    break;

                case 'ł':
                    swap = "l";
                    break;

                case 'Ł':
                    swap = toLower ? "l" : "L";
                    break;

                case 'đ':
                    swap = "d";
                    break;

                case 'ß':
                    swap = "ss";
                    break;

                case 'ø':
                    swap = "o";
                    break;

                case 'Þ':
                    swap = "th";
                    break;
            }

            return swap;
        }

        public static string Create(bool toLower, params string[] values)
        {
            return Create(toLower, string.Join("-", values));
        }

        /// <summary>
        ///     Creates a slug.
        ///     References:
        ///     https://stackoverflow.com/questions/25259/how-does-stack-overflow-generate-its-seo-friendly-urls
        ///     http://www.unicode.org/reports/tr15/tr15-34.html
        ///     https://meta.stackexchange.com/questions/7435/non-us-ascii-characters-dropped-from-full-profile-url/7696#7696
        ///     https://stackoverflow.com/questions/25259/how-do-you-include-a-webpage-title-as-part-of-a-webpage-url/25486#25486
        ///     https://stackoverflow.com/questions/3769457/how-can-i-remove-accents-on-a-string
        /// </summary>
        /// <param name="toLower"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Create(bool toLower, string value)
        {
            if (value == null)
                return "";

            var normalized = value.Normalize(NormalizationForm.FormKD);

            const int maxlength = 80;
            var len = normalized.Length;
            var prevDash = false;
            var sb = new StringBuilder(len);

            for (var i = 0; i < len; i++)
            {
                var c = normalized[i];
                if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9')
                {
                    if (prevDash)
                    {
                        sb.Append('-');
                        prevDash = false;
                    }

                    sb.Append(c);
                }
                else if (c >= 'A' && c <= 'Z')
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
                else if (c == ' ' || c == ',' || c == '.' || c == '/' || c == '\\' || c == '-' || c == '_' || c == '=')
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

                if (sb.Length == maxlength)
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
                imageCheck =
                    await context.FileContents.AnyAsync(x => x.OriginalFileName.ToLower() == filename.ToLower());
            else
                imageCheck = await context.FileContents.AnyAsync(x =>
                    x.OriginalFileName.ToLower() == filename.ToLower() && x.ContentId != exceptInThisContent.Value);

            return imageCheck;
        }

        public static (bool isValid, string explanation) FolderAndSlugCreateValidUri(List<string> folderList,
            string slug)
        {
            var isValid = true;
            var explanation = string.Empty;

            if (string.IsNullOrWhiteSpace(slug))
            {
                isValid = false;
                explanation = "Folder and Slug must only use lower case.";
            }

            if (!isValid) return (false, explanation);

            if (slug.Any(char.IsUpper) || folderList.Any(x => x.Any(char.IsUpper)))
            {
                isValid = false;
                explanation = "Folder and Slug must only use lower case.";
            }

            if (!isValid) return (false, explanation);

            var uriToCheck = $@"//{string.Join("/", folderList)}{(folderList.Any() ? "/" : "")}{slug}";

            if (!Uri.IsWellFormedUriString(uriToCheck, UriKind.RelativeOrAbsolute))
            {
                isValid = false;
                explanation = "Folders and Slug do not form a legal uri - illegal characters?";
            }

            return (isValid, explanation);
        }

        public static async Task<bool> ImageFilenameExistsInDatabase(this PointlessWaymarksContext context,
            string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) return false;

            var imageCheck =
                await context.ImageContents.AnyAsync(x => x.OriginalFileName.ToLower() == filename.ToLower());

            return imageCheck;
        }

        public static async Task<bool> ImageFilenameExistsInDatabase(this PointlessWaymarksContext context,
            string filename, Guid? exceptInThisContent)
        {
            if (string.IsNullOrWhiteSpace(filename)) return false;

            bool imageCheck;

            if (exceptInThisContent == null)
                imageCheck =
                    await context.ImageContents.AnyAsync(x => x.OriginalFileName.ToLower() == filename.ToLower());
            else
                imageCheck = await context.ImageContents.AnyAsync(x =>
                    x.OriginalFileName.ToLower() == filename.ToLower() && x.ContentId != exceptInThisContent.Value);

            return imageCheck;
        }

        public static async Task<bool> PhotoFilenameExistsInDatabase(this PointlessWaymarksContext context,
            string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) return false;

            var photoCheck =
                await context.PhotoContents.AnyAsync(x => x.OriginalFileName.ToLower() == filename.ToLower());

            return photoCheck;
        }

        public static async Task<bool> PhotoFilenameExistsInDatabase(this PointlessWaymarksContext context,
            string filename, Guid? exceptInThisContent)
        {
            if (string.IsNullOrWhiteSpace(filename)) return false;

            bool photoCheck;

            if (exceptInThisContent == null)
                photoCheck =
                    await context.PhotoContents.AnyAsync(x => x.OriginalFileName.ToLower() == filename.ToLower());
            else
                photoCheck = await context.PhotoContents.AnyAsync(x =>
                    x.OriginalFileName.ToLower() == filename.ToLower() && x.ContentId != exceptInThisContent.Value);

            return photoCheck;
        }

        public static string RandomLowerCaseString(int length)
        {
            var chars = "abcdefghijklmnopqrstuvwxyz";
            var stringChars = new char[length];
            var random = new Random();

            for (var i = 0; i < stringChars.Length; i++) stringChars[i] = chars[random.Next(chars.Length)];

            return new string(stringChars);
        }

        public static async Task<bool> SlugExistsInDatabase(this PointlessWaymarksContext context, string slug,
            Guid? excludedContentId)
        {
            if (string.IsNullOrWhiteSpace(slug)) return false;

            if (excludedContentId == null)
            {
                var photoCheck = await context.PhotoContents.AnyAsync(x => x.Slug == slug);
                var imageCheck = await context.ImageContents.AnyAsync(x => x.Slug == slug);
                var noteCheck = await context.NoteContents.AnyAsync(x => x.Slug == slug);
                var fileCheck = await context.FileContents.AnyAsync(x => x.Slug == slug);
                var postCheck = await context.PostContents.AnyAsync(x => x.Slug == slug);

                return photoCheck || postCheck || imageCheck || noteCheck || fileCheck;
            }


            var photoExcludeCheck =
                await context.PhotoContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);
            var imageExcludeCheck =
                await context.ImageContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);
            var noteExcludeCheck =
                await context.NoteContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);
            var fileExcludeCheck =
                await context.FileContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);
            var postExcludeCheck =
                await context.PostContents.AnyAsync(x => x.Slug == slug && x.ContentId != excludedContentId);

            return photoExcludeCheck || postExcludeCheck || imageExcludeCheck || noteExcludeCheck || fileExcludeCheck;
        }
    }
}