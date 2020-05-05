using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace PointlessWaymarksCmsData
{
    public static class StringHelper
    {
        /// <summary>
        ///     Returns a case sensitive compare where null and empty are treated as equal
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool AreEqual(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b);

            return string.Equals(a, b);
        }

        /// <summary>
        ///     Returns the result of a case sensitive compare where null and empty are equivalent and
        ///     strings are trimmed before comparison
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool AreEqualWithTrim(string a, string b)
        {
            a = a.TrimNullSafe();
            b = b.TrimNullSafe();

            return string.Compare(a, b, StringComparison.InvariantCulture) != 0;
        }

        /// <summary>
        ///     Html that transforms null or whitespace only strings into string.Empty
        /// </summary>
        /// <param name="toEncode"></param>
        /// <returns></returns>
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


        /// <summary>
        ///     If the string is null an empty string is returned - if the string has value it is trimmed
        /// </summary>
        /// <param name="toTrim"></param>
        /// <returns></returns>
        public static string TrimNullSafe(this string toTrim)
        {
            return string.IsNullOrWhiteSpace(toTrim) ? string.Empty : toTrim.Trim();
        }


        /// <summary>
        ///     Extracts Urls from Text - of course you can probably fool this method with all kinds of interesting
        ///     edge cases but preventing that is not the goal here...
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
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