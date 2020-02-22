using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PointlessWaymarksCmsData
{
    public static class StringHelper
    {
        public static List<string> UrlsFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            var matchList = new List<string>();

            var regexObj = new Regex(@"\b(https?|ftp|file)://[-A-Z0-9+&@#/%?=~_|$!:,.;]*[A-Z0-9+&@#/%=~_|$]",
                RegexOptions.IgnoreCase);
            var matchResult = regexObj.Match(text);
            while (matchResult.Success)
            {
                matchList.Add(matchResult.Value);
                matchResult = matchResult.NextMatch();
            }

            return matchList;
        }
    }
}