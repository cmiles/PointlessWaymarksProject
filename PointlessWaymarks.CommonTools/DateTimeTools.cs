using System.Text.RegularExpressions;
using Markdig.Helpers;

namespace PointlessWaymarks.CommonTools;

public static class DateTimeTools
{
    public static readonly Dictionary<string, int> MonthNameLowercaseIntMonthLookup = new()
    {
        //See the MonthNameToMonthInt method for notes
        { "january", 1 },
        { "february", 2 },
        { "march", 3 },
        { "april", 4 },
        { "may", 5 },
        { "june", 6 },
        { "july", 7 },
        { "august", 8 },
        { "september", 9 },
        { "october", 10 },
        { "november", 11 },
        { "december", 12 },
        { "jan", 1 },
        { "feb", 2 },
        { "mar", 3 },
        { "apr", 4 },
        { "jun", 6 },
        { "jul", 7 },
        { "aug", 8 },
        { "sep", 9 },
        { "sept", 9 },
        { "oct", 10 },
        { "nov", 11 },
        { "dec", 12 }
    };


    /// <summary>
    ///     Processes a string that begins or ends with: YYMM, YYYY MM, YYYY MMM, YYYY MMMM into a DateOnly and
    ///     the string with the date removed. If the string does not match this convention null is returned. Of course
    ///     with the patterns involved (YYMM vs YYYY) and the assumption the string may have other digits following
    ///     or preceding these patterns some of the processing is 'by convention'
    /// </summary>
    /// <param name="toProcess"></param>
    /// <returns></returns>
    public static (DateOnly titleDate, string titleWithDateRemoved)? DateOnlyFromTitleStringByConvention(
        string? toProcess)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return null;

        //2023/6/11 - For me photographs are inherently time based - the date/time a photograph is taken is so 
        //important even in casual viewing that at least some indication of 'when' should be included directly in
        //the title of the photo. This also serves as a valuable backup to protect some 'when' information from
        //being lost as various programs and sites process/update/mangle/try to fix/change/best guess/miscalculate/incorrectly
        //export/inappropriately overwrite the photos metadata. I would like to say that this is an opinion and 
        //pattern from the 2000s and before when digital photography was younger but I'm not sure that is the case.
        //
        //This pattern extraction has both useful general patterns and some patterns that are not novel but are
        //probably used by few others...

        var fourDigitYearAndTwoDigitMonthAtStart =
            new Regex(@"\A(?<possibleDate>\d\d\d\d[\s-][01]\d)[\s-].*", RegexOptions.IgnoreCase);
        var fourDigitYearAndTwoDigitMonthAtEnd =
            new Regex(@".*[\s-](?<possibleDate>\d\d\d\d[\s-][01]\d)\z", RegexOptions.IgnoreCase);
        var twoDigitYearAndTwoDigitMonthAtStart = new Regex(@"\A[01]\d[01]\d[\s-].*", RegexOptions.IgnoreCase);
        var twoDigitYearAndTwoDigitMonthAtEnd = new Regex(@".*[\s-][01]\d[01]\d\z", RegexOptions.IgnoreCase);
        var fourDigitYearAndTextMonthAtStart = new Regex(
            @"\A(?<possibleDate>\d\d\d\d[\s-](?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|(Nov|Dec)(?:ember)?))[\s-].*",
            RegexOptions.IgnoreCase);
        var fourDigitYearAndTextMonthAtEnd = new Regex(
            @"(?<possibleDate>\d\d\d\d[\s-](?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Sept|Oct(?:ober)?|(Nov|Dec)(?:ember)?))\z",
            RegexOptions.IgnoreCase);


        if (fourDigitYearAndTwoDigitMonthAtStart.IsMatch(toProcess))
        {
            var possibleTitleDate =
                fourDigitYearAndTwoDigitMonthAtStart.Match(toProcess)
                    .Groups["possibleDate"].Value;
            if (!string.IsNullOrWhiteSpace(possibleTitleDate))
                try
                {
                    return (new DateOnly(int.Parse(possibleTitleDate[..4]),
                            int.Parse(possibleTitleDate.Substring(5, 2)), 1),
                        $"{toProcess[possibleTitleDate.Length..]}".TrimNullToEmpty());
                }
                catch
                {
                    return null;
                }
        }
        else if (fourDigitYearAndTextMonthAtStart.IsMatch(toProcess))
        {
            var possibleTitleDate =
                fourDigitYearAndTextMonthAtStart.Match(toProcess)
                    .Groups["possibleDate"].Value;
            if (!string.IsNullOrWhiteSpace(possibleTitleDate))
                try
                {
                    return (new DateOnly(int.Parse(possibleTitleDate[..4]),
                            MonthEnglishNameToMonthInt(possibleTitleDate.Substring(5, possibleTitleDate.Length - 5))!
                                .Value,
                            1),
                        $"{toProcess[possibleTitleDate.Length..]}".TrimNullToEmpty());
                }
                catch
                {
                    return null;
                }
        }
        else if (twoDigitYearAndTwoDigitMonthAtStart.IsMatch(toProcess))
        {
            try
            {
                var year = int.Parse(toProcess[..2]);
                var month = int.Parse(toProcess.Substring(2, 2));

                return (year < 20
                    ? new DateOnly(2000 + year, month, 1)
                    : new DateOnly(1900 + year, month, 1), $"{toProcess[5..]}".TrimNullToEmpty());
            }
            catch
            {
                return null;
            }
        }
        else if (twoDigitYearAndTwoDigitMonthAtEnd.IsMatch(toProcess))
        {
            try
            {
                var year = int.Parse(toProcess.Substring(toProcess.Length - 4, 2));
                var month = int.Parse(toProcess.Substring(toProcess.Length - 2, 2));

                return (year < 20 ? new DateOnly(2000 + year, month, 1) : new DateOnly(1900 + year, month, 1),
                    $"{toProcess[..^5]}".TrimNullToEmpty());
            }
            catch
            {
                return null;
            }
        }
        else if (fourDigitYearAndTwoDigitMonthAtEnd.IsMatch(toProcess))
        {
            var possibleTitleDate =
                fourDigitYearAndTwoDigitMonthAtEnd.Match(toProcess)
                    .Groups["possibleDate"].Value;
            if (!string.IsNullOrWhiteSpace(possibleTitleDate))
                try
                {
                    return (new DateOnly(int.Parse(possibleTitleDate[..4]),
                            int.Parse(possibleTitleDate.Substring(5, 2)), 1),
                        $"{toProcess[..^possibleTitleDate.Length].TrimNullToEmpty()}");
                }
                catch
                {
                    return null;
                }
        }
        else if (fourDigitYearAndTextMonthAtEnd.IsMatch(toProcess))
        {
            var possibleTitleDate =
                fourDigitYearAndTextMonthAtEnd.Match(toProcess)
                    .Groups["possibleDate"].Value;
            if (!string.IsNullOrWhiteSpace(possibleTitleDate))
                try
                {
                    return (new DateOnly(int.Parse(possibleTitleDate[..4]),
                            MonthEnglishNameToMonthInt(possibleTitleDate.Substring(5, possibleTitleDate.Length - 5))!
                                .Value,
                            1),
                        $"{toProcess[possibleTitleDate.Length..]}".TrimNullToEmpty());
                }
                catch
                {
                    return null;
                }
        }

        return null;
    }

    public static (int totalMinutes, string? presentationString) LineDurationInHoursAndMinutes(TimeSpan timeSpan)
    {
        var minuteDuration = (int)timeSpan.TotalMinutes;

        if (minuteDuration > 1)
        {
            var hours = minuteDuration / 60;
            var minutes = minuteDuration - hours * 60;

            if (hours == 0)
                return (minuteDuration, $"{minutes} Minutes");
            if (minutes == 0)
                return (minuteDuration, $"{hours} Hour{(hours > 1 ? "s" : "")}");
            return (minuteDuration,
                $"{hours} Hour{(hours > 1 ? "s" : "")} {minutes} Minute{(minutes > 1 ? "s" : "")}");
        }

        return (minuteDuration, null);
    }

    public static int? MonthEnglishNameToMonthInt(string? toConvert)
    {
        //The stack overflow question below nicely runs thru some of the options for this:
        //https://stackoverflow.com/questions/258793/how-to-parse-a-month-name-string-to-an-integer-for-comparison-in-c#258895
        //The approach here lacks elegance but makes up for it in simplicity and flexibility.

        if (string.IsNullOrWhiteSpace(toConvert)) return null;

        var cleanedInput = toConvert.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(cleanedInput)) return null;

        if (!MonthNameLowercaseIntMonthLookup.ContainsKey(cleanedInput)) return null;

        return MonthNameLowercaseIntMonthLookup[cleanedInput];
    }

    /// <summary>
    ///     Uses reflection to truncate all DateTime and DateTime? properties in the input to the second
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="toProcess"></param>
    /// <returns></returns>
    public static T TrimDateTimesToSeconds<T>(T toProcess)
    {
        var dateTimeProperties = typeof(T).GetProperties()
            .Where(x => x.PropertyType == typeof(DateTime) && x.GetSetMethod() != null).ToList();

        foreach (var loopProperty in dateTimeProperties)
        {
            var current = (DateTime)loopProperty.GetValue(toProcess)!;
            loopProperty.SetValue(toProcess,
                new DateTime(current.Year, current.Month, current.Day, current.Hour, current.Minute, current.Second,
                    current.Kind));
        }

        var nullableDateTimeProperties = typeof(T).GetProperties()
            .Where(x => x.PropertyType == typeof(DateTime?) && x.GetSetMethod() != null).ToList();

        foreach (var loopProperties in nullableDateTimeProperties)
        {
            var current = (DateTime?)loopProperties.GetValue(toProcess);
            if (current == null) continue;
            DateTime? newDateTime = new DateTime(current.Value.Year, current.Value.Month, current.Value.Day,
                current.Value.Hour, current.Value.Minute, current.Value.Second, current.Value.Kind);
            loopProperties.SetValue(toProcess, newDateTime);
        }

        return toProcess;
    }


    /// <summary>
    ///     Trims a DateTime to seconds
    /// </summary>
    /// <param name="toTrim"></param>
    /// <returns></returns>
    public static DateTime TrimDateTimeToSeconds(this DateTime toTrim)
    {
        return new DateTime(toTrim.Year, toTrim.Month, toTrim.Day, toTrim.Hour,
            toTrim.Minute, toTrim.Second, toTrim.Kind);
    }

    /// <summary>
    ///     Parses a YYYY MMMM or YYYY MMM from the start of a string - only supports English month names. If a year and
    ///     month is found the DateOnly return will be the first of the month.
    /// </summary>
    /// <param name="toProcess"></param>
    /// <returns></returns>
    public static DateOnly? YearAndEnglishTextMonthFromStartOfString(string? toProcess)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return null;

        var cleanedInput = toProcess.Trim().ToLowerInvariant();

        var splitName = cleanedInput.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
            .ToList();

        if (splitName.Count < 2) return null;

        if (!splitName[0].All(x => x.IsDigit())) return null;

        if (!int.TryParse(splitName[0], out var titleYear)) return null;

        if (!MonthNameLowercaseIntMonthLookup.ContainsKey(splitName[1])) return null;

        var titleMonth = MonthNameLowercaseIntMonthLookup[splitName[1]];

        return new DateOnly(titleYear, titleMonth, 1);
    }
}