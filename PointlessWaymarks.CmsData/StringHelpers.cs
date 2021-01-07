using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace PointlessWaymarks.CmsData
{
    public static class StringHelpers
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
            a = a.TrimNullToEmpty();
            b = b.TrimNullToEmpty();

            return string.Compare(a, b, StringComparison.InvariantCulture) == 0;
        }

        /// <summary>
        ///     Html Encode that transforms null or whitespace only strings into string.Empty
        /// </summary>
        /// <param name="toEncode"></param>
        /// <returns></returns>
        public static string HtmlEncode(this string toEncode)
        {
            return string.IsNullOrWhiteSpace(toEncode) ? string.Empty : HttpUtility.HtmlEncode(toEncode);
        }

        /// <summary>
        ///     Given a List of String "Joe", "Jorge" and "Jeff" joins to "Joe, Jorge and Jeff" - performs as expected with single
        ///     items lists remaining single items.
        /// </summary>
        /// <param name="toJoin"></param>
        /// <returns></returns>
        public static string JoinListOfStringsToCommonUsageListWithAnd(this List<string> toJoin)
        {
            //https://stackoverflow.com/questions/17560201/join-liststring-together-with-commas-plus-and-for-last-element
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
        public static string NullToEmptyTrim(string toTrim)
        {
            return string.IsNullOrWhiteSpace(toTrim) ? string.Empty : toTrim.Trim();
        }

        public static string RemoveNewLines(this string toProcess)
        {
            return toProcess.Replace("\n", "").Replace("\r", "");
        }

        /// <summary>
        ///     Simple expansion of Camel Case into a 'normal' string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string SplitCamelCase(this string str)
        {
            //https://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
            return Regex.Replace(Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
        }


        /// <summary>
        ///     If the string is null an empty string is returned - if the string has value it is trimmed
        /// </summary>
        /// <param name="toTrim"></param>
        /// <returns></returns>
        public static string TrimNullToEmpty(this string toTrim)
        {
            return string.IsNullOrWhiteSpace(toTrim) ? string.Empty : toTrim.Trim();
        }

        /// <summary>
        ///     Uses reflection to call TrimNullToEmpty on all string properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toProcess"></param>
        /// <returns></returns>
        public static T TrimNullToEmptyAllStringProperties<T>(T toProcess)
        {
            var properties = typeof(T).GetProperties()
                .Where(x => x.PropertyType == typeof(string) && x.GetSetMethod() != null).ToList();

            foreach (var loopProperties in properties)
                loopProperties.SetValue(toProcess, ((string) loopProperties.GetValue(toProcess)).TrimNullToEmpty());

            return toProcess;
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