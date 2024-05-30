using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web;

namespace PointlessWaymarks.CommonTools;

public static class StringTools
{
    /// <summary>
    ///     Returns a case-sensitive compare where null and empty are treated as equal
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool AreEqual(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b);
        
        return string.Equals(a, b);
    }
    
    /// <summary>
    ///     Returns the result of a case-sensitive compare where null and empty are equivalent and
    ///     strings are trimmed before comparison
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool AreEqualWithTrim(string? a, string? b)
    {
        a = a.TrimNullToEmpty();
        b = b.TrimNullToEmpty();
        
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    ///     Simple expansion of Camel Case into a 'normal' string
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string CamelCaseToSpacedString(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return string.Empty;
        
        var stringItems = new List<string>();
        
        var splitString = str.Split(' ');
        
        foreach (var loopParts in splitString)
        {
            if (string.IsNullOrWhiteSpace(loopParts))
            {
                stringItems.Add(loopParts);
                continue;
            }
            
            //https://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
            stringItems.Add(Regex.Replace(
                Regex.Replace(loopParts, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2"));
        }
        
        return string.Join(' ', stringItems);
    }
    
    public static string GetMethodName(this object type, [CallerMemberName] string? caller = null)
    {
        return type.GetType().FullName + "." + caller.TrimNullToEmpty();
    }
    
    /// <summary>
    ///     Html Encode that transforms null or whitespace only strings into string.Empty
    /// </summary>
    /// <param name="toEncode"></param>
    /// <returns></returns>
    public static string HtmlEncode(this string? toEncode)
    {
        return string.IsNullOrWhiteSpace(toEncode) ? string.Empty : HttpUtility.HtmlEncode(toEncode);
    }
    
    /// <summary>
    ///     Given a List of String "Joe", "Jorge" and "Jeff" joins to "Joe, Jorge and Jeff" - performs as expected with single
    ///     items lists remaining single items.
    /// </summary>
    /// <param name="toJoin"></param>
    /// <returns></returns>
    public static string JoinListOfNullableStringsToListWithAnd(this List<string?> toJoin)
    {
        //https://stackoverflow.com/questions/17560201/join-liststring-together-with-commas-plus-and-for-last-element
        
        var cleanedList = toJoin.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.TrimNullToEmpty())
            .ToList();
        
        return JoinListOfStringsToListWithAnd(cleanedList);
    }
    
    /// <summary>
    ///     Given a List of String "Joe", "Jorge" and "Jeff" joins to "Joe, Jorge and Jeff" - performs as expected with single
    ///     items lists remaining single items.
    /// </summary>
    /// <param name="toJoin"></param>
    /// <returns></returns>
    public static string JoinListOfStringsToListWithAnd(this List<string> toJoin)
    {
        //https://stackoverflow.com/questions/17560201/join-liststring-together-with-commas-plus-and-for-last-element
        
        var cleanedList = toJoin.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.TrimNullToEmpty())
            .ToList();
        
        if (!cleanedList.Any()) return string.Empty;
        
        if (cleanedList.Count == 1) return cleanedList.First();
        
        return string.Join(", ", toJoin.Take(toJoin.Count - 1)) + " and " + toJoin.Last();
    }
    
    /// <summary>
    ///     If the string is null an empty string is returned - if the string has value it is trimmed
    /// </summary>
    /// <param name="toTrim"></param>
    /// <returns></returns>
    public static string NullToEmptyTrim(string? toTrim)
    {
        return string.IsNullOrWhiteSpace(toTrim) ? string.Empty : toTrim.Trim();
    }
    
    /// <summary>
    ///     Removes all \n and \r to remove all common newline types.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <returns></returns>
    public static string RemoveNewLines(this string toProcess)
    {
        return toProcess.Replace("\n", "").Replace("\r", "");
    }
    
    public static string ReplaceEach(this string text, string search, Func<string> replacementGenerator)
    {
        var returnString = text;
        while (returnString.Contains(search))
            returnString = ReplaceFirst(returnString, search, replacementGenerator()) ?? string.Empty;
        
        return returnString;
    }
    
    public static string? ReplaceFirst(this string? text, string search, string replace)
    {
        if (text == null) return null;
        
        var length = text.IndexOf(search, StringComparison.Ordinal);
        
        return length < 0 ? text : text[..length] + replace + text[(length + search.Length)..];
    }
    
    public static Stream ToMemoryStream(this string str)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(str);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
    
    
    /// <summary>
    ///     If the string is null an empty string is returned - if the string has value it is trimmed
    /// </summary>
    /// <param name="toTrim"></param>
    /// <returns></returns>
    public static string TrimNullToEmpty(this string? toTrim)
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
            loopProperties.SetValue(toProcess, ((string?)loopProperties.GetValue(toProcess)).TrimNullToEmpty());
        
        return toProcess;
    }
    
    /// <summary>
    ///     Truncates a string if it exceeds a maximum length - null inputs and maxLengths less than
    ///     1 return empty strings.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static string Truncate(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength < 1) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
    
    /// <summary>
    ///     Truncates a string if it exceeds a maximum length - null inputs and maxLengths less than
    ///     1 return empty strings.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static string TruncateWithEllipses(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength < 1) return string.Empty;
        if (value.Length <= maxLength) return value;
        return maxLength <= 2 ? value[..maxLength] : $"{value[..(maxLength - 3)]}...";
    }
    
    /// <summary>
    ///     A simple method to combine pieces of a Url in the spirit of Path.Combine - note this
    ///     only handles simple cases and mostly is a convenience to deal with leading a trailing
    ///     '/' characters.
    /// </summary>
    /// <param name="url1"></param>
    /// <param name="url2"></param>
    /// <returns></returns>
    public static string UrlCombine(string url1, string url2)
    {
        //https://stackoverflow.com/questions/372865/path-combine-for-urls
        if (url1.Length == 0) return url2;
        
        if (url2.Length == 0) return url1;
        
        url1 = url1.TrimEnd('/', '\\');
        url2 = url2.TrimStart('/', '\\');
        
        return $"{url1}/{url2}";
    }
    
    
    /// <summary>
    ///     Extracts Urls from Text - of course you can probably fool this method with all kinds of interesting
    ///     edge cases but preventing that is not the goal here...
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static List<string> UrlsFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        
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