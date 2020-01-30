using System.IO;
using System.Text.RegularExpressions;

namespace PointlessWaymarksCmsData
{
    public static class FolderFileUtility
    {
        public static bool IsValidFilename(string testName)
        {
            //https://stackoverflow.com/questions/62771/how-do-i-check-if-a-given-string-is-a-legal-valid-file-name-under-windows
            var containsABadCharacter = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]");
            if (containsABadCharacter.IsMatch(testName)) return false;

            return true;
        }
    }
}