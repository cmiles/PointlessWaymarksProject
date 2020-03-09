﻿using System.Collections.Generic;
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