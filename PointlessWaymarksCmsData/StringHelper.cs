using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace PointlessWaymarksCmsData
{
    public static class StringHelper
    {
        public static string HtmlEncode(this string toEncode)
        {
            return string.IsNullOrWhiteSpace(toEncode) ? string.Empty : HttpUtility.HtmlEncode(toEncode);
        }

        /// <summary>
        ///     Given a List of String "Joe", "Jorge" and "Jeff" joins to "Joe, Jorge and Jeff" - performs as expected with single
        ///     items lists remaining
        ///     single items.
        ///     https://stackoverflow.com/questions/17560201/join-liststring-together-with-commas-plus-and-for-last-element
        /// </summary>
        /// <param name="toJoin"></param>
        /// <returns></returns>
        public static string JoinListOfStringsToCommonUsageListWithAnd(this List<string> toJoin)
        {
            toJoin ??= new List<string>();

            return toJoin.Count > 1
                ? string.Join(", ", toJoin.Take(toJoin.Count - 1)) + " and " + toJoin.Last()
                : toJoin.FirstOrDefault();
        }

        public static string TrimNullSafe(this string toTrim)
        {
            return string.IsNullOrWhiteSpace(toTrim) ? string.Empty : toTrim.Trim();
        }

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