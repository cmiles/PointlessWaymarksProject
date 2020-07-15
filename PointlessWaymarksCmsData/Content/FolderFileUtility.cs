using System.IO;
using System.Text.RegularExpressions;

namespace PointlessWaymarksCmsData.Content
{
    public static class FolderFileUtility
    {
        /// <summary>
        ///     Appends the Long File Prefix \\?\ to the FileInfo FullName - this should only be used to interop in situations
        ///     not related to .NET Core - .NET Core handles this automatically. Only tested with absolute file paths.
        /// </summary>
        /// <param name="forName"></param>
        /// <returns></returns>
        public static string FullNameWithLongFilePrefix(this FileInfo forName)
        {
            //See https://stackoverflow.com/questions/5188527/how-to-deal-with-files-with-a-name-longer-than-259-characters for a good summary
            //and for some library and other alternatives.
            if (forName == null) return null;

            return forName.FullName.Length > 240 ? $"\\\\?\\{forName.FullName}" : forName.FullName;
        }

        public static string InvalidFileNameCharsRegexPattern()
        {
            return $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]";
        }

        /// <summary>
        ///     This checks if a string is 'no url encoding needed'
        /// </summary>
        /// <param name="testName"></param>
        /// <returns></returns>
        public static bool IsNoUrlEncodingNeeded(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName)) return false;

            return Regex.IsMatch(testName, @"^[a-zA-Z\d_\-]+$");
        }

        /// <summary>
        ///     This checks if a string is 'no url encoding needed'
        /// </summary>
        /// <param name="testName"></param>
        /// <returns></returns>
        public static bool IsNoUrlEncodingNeededLowerCase(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName)) return false;

            return Regex.IsMatch(testName, @"^[a-z\d_\-]+$");
        }

        /// <summary>
        ///     This checks if a string is 'no url encoding needed' with the exception of spaces which are allowed
        /// </summary>
        /// <param name="testName"></param>
        /// <returns></returns>
        public static bool IsNoUrlEncodingNeededLowerCaseSpacesOk(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName)) return false;

            return Regex.IsMatch(testName, @"^[a-z \d_\-]+$");
        }

        public static bool IsValidWindowsFileSystemFilename(string testName)
        {
            //https://stackoverflow.com/questions/62771/how-do-i-check-if-a-given-string-is-a-legal-valid-file-name-under-windows
            var containsABadCharacter = new Regex(InvalidFileNameCharsRegexPattern());
            if (containsABadCharacter.IsMatch(testName)) return false;

            return true;
        }

        public static bool PictureFileTypeIsSupported(FileInfo toCheck)
        {
            if (toCheck == null) return false;
            if (!toCheck.Exists) return false;

            return toCheck.Extension.ToLower().Contains("jpg") || toCheck.Extension.ToLower().Contains("jpeg");
        }

        public static string TryMakeFilenameValid(string filenameWithoutExtensionToTransform)
        {
            return string.IsNullOrWhiteSpace(filenameWithoutExtensionToTransform)
                ? string.Empty
                : Regex.Replace(filenameWithoutExtensionToTransform, InvalidFileNameCharsRegexPattern(), string.Empty);
        }
    }
}