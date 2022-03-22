using Fractions;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using PointlessWaymarks.CmsData;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public static class ContentListSearchFunctions
{
    public static List<string> FilterListTokenOperatorList =>
        new()
        {
            "==",
            ">",
            "<",
            ">=",
            "<="
        };

    public static ContentListSearchFunctionReturn FilterAperture(string itemApertureString, string searchString)
    {
        if (!string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith("APERTURE:", StringComparison.OrdinalIgnoreCase))
            searchString = searchString[9..];

        if (string.IsNullOrWhiteSpace(itemApertureString) && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(true, "Blank Aperture and Blank Search String (true).");

        if (string.IsNullOrWhiteSpace(itemApertureString))
            return new ContentListSearchFunctionReturn(false, "Blank Aperture with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(false, "Blank Search String with Not Blank Aperture (false).");

        itemApertureString = itemApertureString.Trim();
        searchString = searchString.Trim();

        if (!decimal.TryParse(
                itemApertureString.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("f/", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("\uD835\uDC53/", string.Empty, StringComparison.OrdinalIgnoreCase).TrimNullToEmpty(),
                out var listItemAperture))
            //Couldn't parse a value from the list item's Aperture - compare as string to the search string
            return new ContentListSearchFunctionReturn(itemApertureString.Equals(searchString),
                $"Aperture input of '{itemApertureString}' could not " +
                $"be parsed into a numeric Aperture to search - instead checking if the Aperture as a string to is equal to '{searchString}'");

        //Remove f at this point - not currently aware of another valid Aperture measurement so removing this should be 
        //a nice way to make this optional for the user
        var tokens = FilterListSpaceDividedTokenList(searchString
            .Replace("f /", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("f/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("f", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\uD835\uDC53 /", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\uD835\uDC53/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("\uD835\uDC53", string.Empty, StringComparison.OrdinalIgnoreCase));

        if (tokens.Count == 1)
        {
            if (!decimal.TryParse(tokens.First(), out var parsedAperture))
                return new ContentListSearchFunctionReturn(itemApertureString.Contains(searchString),
                    $"Search input of {tokens.First()} could not " +
                    $"be parsed into a numeric Aperture to search - instead checking if the item Aperture '{itemApertureString}' " +
                    $"contains '{tokens.First()}'");

            return new ContentListSearchFunctionReturn(listItemAperture == parsedAperture,
                $"Search Aperture of {parsedAperture} compared to {listItemAperture}");
        }

        var apertureSearchResults = new List<ContentListSearchFunctionReturn>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var scanValue = tokens[i];

            if (!FilterListTokenOperatorList.Contains(scanValue))
            {
                if (!decimal.TryParse(tokens.First(), out var parsedAperture))
                {
                    apertureSearchResults.Add(new ContentListSearchFunctionReturn(
                        itemApertureString.Contains(scanValue),
                        $"Search input of {scanValue} could not " +
                        $"be parsed into a numeric Aperture to search - instead checking if the item Aperture '{itemApertureString}' " +
                        $"contains '{tokens.First()}'"));
                    continue;
                }

                apertureSearchResults.Add(new ContentListSearchFunctionReturn(listItemAperture == parsedAperture,
                    $"Search Aperture of {parsedAperture} compared to " + $"{listItemAperture}"));
                continue;
            }

            i++;

            //Last token is a operator - this isn't valid, just continue...
            if (i >= tokens.Count) continue;

            var lookaheadValue = tokens[i];

            if (!decimal.TryParse(lookaheadValue, out var parsedApertureForExpression))
            {
                apertureSearchResults.Add(new ContentListSearchFunctionReturn(itemApertureString.Contains(scanValue),
                    $"Search input of {scanValue} could not " +
                    $"be parsed into a numeric Aperture to search - instead checking if the item Aperture '{itemApertureString}' " +
                    $"contains '{tokens.First()}'"));
                continue;
            }

            switch (scanValue)
            {
                case "==":
                    apertureSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemAperture == parsedApertureForExpression,
                        $"Search Aperture of {parsedApertureForExpression} compared to " + $"{listItemAperture}"));
                    break;
                case "!=":
                    apertureSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemAperture != parsedApertureForExpression,
                        $"Search Aperture of {parsedApertureForExpression} not equal to " + $"{listItemAperture}"));
                    break;
                case ">":
                    apertureSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemAperture < parsedApertureForExpression,
                        $"Evaluated Search Aperture of {parsedApertureForExpression} greater than {listItemAperture}"));
                    break;
                case ">=":
                    apertureSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemAperture <= parsedApertureForExpression,
                        $"Evaluated Search Aperture of {parsedApertureForExpression} greater than or equal to {listItemAperture}"));
                    break;
                case "<":
                    apertureSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemAperture > parsedApertureForExpression,
                        $"Evaluated Search Aperture of {parsedApertureForExpression} less than {listItemAperture}"));
                    break;
                case "<=":
                    apertureSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemAperture >= parsedApertureForExpression,
                        $"Evaluated Search Aperture of {parsedApertureForExpression} less than or equal to {listItemAperture}"));
                    break;
            }
        }

        return !apertureSearchResults.Any()
            ? new ContentListSearchFunctionReturn(false, "No Search String Parse Results?")
            : new ContentListSearchFunctionReturn(apertureSearchResults.All(x => x.Include),
                string.Join(Environment.NewLine, apertureSearchResults.Select(x => $"{x.Explanation} ({x.Include}).")));
    }


    public static ContentListSearchFunctionReturn FilterDateTime(DateTime? itemDateTime, string searchString,
        string searchLabel)
    {
        if (!string.IsNullOrWhiteSpace(searchLabel) && !string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith(searchLabel.Trim(), StringComparison.OrdinalIgnoreCase))
            searchString = searchString[$"{searchLabel.Trim().Replace(":", string.Empty)}:".Length..];

        if (itemDateTime == null && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(true, $"Blank {searchLabel} and Blank Search String (true).");

        if (itemDateTime == null)
            return new ContentListSearchFunctionReturn(false,
                $"Blank {searchLabel} with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(false,
                $"Blank Search String with Not Blank {searchLabel} (false).");

        searchString = searchString.Trim();

        var tokens = FilterListOperatorDividedTokenList(searchString);

        var dateTimeSearchResults = new List<ContentListSearchFunctionReturn>();

        foreach (var loopDateTimeSearches in tokens)
        {
            var dateTimeParse = DateTimeRecognizer.RecognizeDateTime(loopDateTimeSearches.searchString, Culture.English,
                DateTimeOptions.None, DateTime.Now);

            if (dateTimeParse.Count == 0 || dateTimeParse[0].Resolution.Count == 0)
            {
                dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(true,
                    $"Search input of {loopDateTimeSearches.searchString} could not " +
                    "be parsed into a DateTime to search"));
                continue;
            }

            if (dateTimeParse[0].TypeName == "datetimeV2.daterange")
            {
                var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
                if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                    valuesDictionary.Count < 1 ||
                    !valuesDictionary[0].TryGetValue("start", out var searchStartDateTimeString) ||
                    !DateTime.TryParse(searchStartDateTimeString, out var searchStartDateTime) ||
                    !valuesDictionary[0].TryGetValue("end", out var searchEndDateTimeString) ||
                    !DateTime.TryParse(searchEndDateTimeString, out var searchEndDateTime))
                {
                    dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(true,
                        $"{loopDateTimeSearches.searchString} could not be parsed into a valid DateTime for Comparison (true)."));
                    continue;
                }

                switch (loopDateTimeSearches.operatorString)
                {
                    case "":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value >= searchStartDateTime && itemDateTime.Value < searchEndDateTime,
                            $"Search {searchLabel} of {itemDateTime.Value} is >= {searchStartDateTime} and < {searchEndDateTime}"));
                        break;
                    case "==":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value >= searchStartDateTime && itemDateTime.Value < searchEndDateTime,
                            $"Search {searchLabel} of {itemDateTime.Value} is >= {searchStartDateTime} and < {searchEndDateTime}"));
                        break;
                    case "!=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value < searchStartDateTime && itemDateTime.Value >= searchEndDateTime,
                            $"Search {searchLabel} of {itemDateTime.Value} is < {searchStartDateTime} and >= {searchEndDateTime}"));
                        break;
                    case ">":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value > searchEndDateTime,
                            $"Search {searchLabel} of {itemDateTime.Value} is greater than {searchEndDateTime}"));
                        break;
                    case ">=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value >= searchEndDateTime,
                            $"Search {searchLabel} of {itemDateTime.Value} is great than or equal to {searchEndDateTime}"));
                        break;
                    case "<":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value < searchEndDateTime,
                            $"Search {searchLabel} of {itemDateTime.Value} is less than {searchEndDateTime}"));
                        break;
                    case "<=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value <= searchEndDateTime,
                            $"Search {searchLabel} of {itemDateTime.Value} is less than or equal to {searchEndDateTime}"));
                        break;
                }

                continue;
            }

            if (dateTimeParse[0].TypeName == "datetimeV2.date")
            {
                var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
                if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                    valuesDictionary.Count < 1 ||
                    !valuesDictionary[0].TryGetValue("value", out var searchDateTimeString) ||
                    !DateTime.TryParse(searchDateTimeString, out var searchDateTime))
                {
                    dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(true,
                        $"{loopDateTimeSearches.searchString} could not be parsed into a valid DateTime for Comparison (true)."));
                    continue;
                }

                switch (loopDateTimeSearches.operatorString)
                {
                    case "":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.Date == searchDateTime.Date,
                            $"Search {searchLabel} of {itemDateTime.Value.Date} compared to {searchDateTime.Date}"));
                        break;
                    case "==":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.Date == searchDateTime.Date,
                            $"Search {searchLabel} of {itemDateTime.Value.Date} compared to {searchDateTime.Date}"));
                        break;
                    case "!=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.Date != searchDateTime.Date,
                            $"Search {searchLabel} of {itemDateTime.Value.Date} does not equal {searchDateTime.Date}"));
                        break;
                    case ">":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.Date > searchDateTime.Date,
                            $"Search {searchLabel} of {itemDateTime.Value.Date} is greater than {searchDateTime.Date}"));
                        break;
                    case ">=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.Date >= searchDateTime.Date,
                            $"Search {searchLabel} of {itemDateTime.Value.Date} is great than or equal to {searchDateTime.Date}"));
                        break;
                    case "<":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.Date < searchDateTime.Date,
                            $"Search {searchLabel} of {itemDateTime.Value.Date} is less than {searchDateTime.Date}"));
                        break;
                    case "<=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.Date <= searchDateTime.Date,
                            $"Search {searchLabel} of {itemDateTime.Value.Date} is less than or equal to {searchDateTime.Date}"));
                        break;
                }

                continue;
            }

            if (dateTimeParse[0].TypeName == "datetimeV2.datetime")
            {
                var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
                if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                    valuesDictionary.Count < 1 ||
                    !valuesDictionary[0].TryGetValue("value", out var searchDateTimeString) ||
                    !DateTime.TryParse(searchDateTimeString, out var searchDateTime))
                {
                    dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(true,
                        $"{loopDateTimeSearches.searchString} could not be parsed into a valid DateTime for Comparison (true)."));
                    continue;
                }

                switch (loopDateTimeSearches.operatorString)
                {
                    case "":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime == searchDateTime.Date,
                            $"Search {searchLabel} of {itemDateTime} compared to {searchDateTime}"));
                        break;
                    case "==":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(itemDateTime == searchDateTime,
                            $"Search {searchLabel} of {itemDateTime} compared to {searchDateTime}"));
                        break;
                    case "!=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(itemDateTime != searchDateTime,
                            $"Search {searchLabel} of {itemDateTime} does not equal {searchDateTime}"));
                        break;
                    case ">":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(itemDateTime > searchDateTime,
                            $"Search {searchLabel} of {itemDateTime} is greater than {searchDateTime}"));
                        break;
                    case ">=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(itemDateTime >= searchDateTime,
                            $"Search {searchLabel} of {itemDateTime} is great than or equal to {searchDateTime}"));
                        break;
                    case "<":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(itemDateTime < searchDateTime,
                            $"Search {searchLabel} of {itemDateTime} is less than {searchDateTime}"));
                        break;
                    case "<=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(itemDateTime <= searchDateTime,
                            $"Search {searchLabel} of {itemDateTime} is less than or equal to {searchDateTime}"));
                        break;
                }

                continue;
            }

            if (dateTimeParse[0].TypeName == "datetimeV2.time")
            {
                var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
                if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                    valuesDictionary.Count < 1 ||
                    !valuesDictionary[0].TryGetValue("value", out var searchDateTimeString) ||
                    !DateTime.TryParse(searchDateTimeString, out var searchTime))
                {
                    dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(true,
                        $"{loopDateTimeSearches.searchString} could not be parsed into a valid DateTime for Comparison (true)."));
                    continue;
                }

                switch (loopDateTimeSearches.operatorString)
                {
                    case "":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.TimeOfDay == searchTime.TimeOfDay,
                            $"Search {searchLabel} of {itemDateTime:T} compared to {searchTime:T}"));
                        break;
                    case "==":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.TimeOfDay == searchTime.TimeOfDay,
                            $"Search {searchLabel} of {itemDateTime:T} compared to {searchTime:T}"));
                        break;
                    case "!=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.TimeOfDay != searchTime.TimeOfDay,
                            $"Search {searchLabel} of {itemDateTime:T} does not equal {searchTime:T}"));
                        break;
                    case ">":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.TimeOfDay > searchTime.TimeOfDay,
                            $"Search {searchLabel} of {itemDateTime:T} is greater than {searchTime:T}"));
                        break;
                    case ">=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.TimeOfDay >= searchTime.TimeOfDay,
                            $"Search {searchLabel} of {itemDateTime:T} is greater than or equal to {searchTime:T}"));
                        break;
                    case "<":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.TimeOfDay < searchTime.TimeOfDay,
                            $"Search {searchLabel} of {itemDateTime:T} is less than {searchTime:T}"));
                        break;
                    case "<=":
                        dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(
                            itemDateTime.Value.TimeOfDay <= searchTime.TimeOfDay,
                            $"Search {searchLabel} of {itemDateTime:T} is less than or equal to {searchTime:T}"));
                        break;

                    //continue;
                }

                dateTimeSearchResults.Add(new ContentListSearchFunctionReturn(true,
                    $"{loopDateTimeSearches.searchString} could not be parsed into a valid DateTime for Comparison (true)."));
            }
        }

        return !dateTimeSearchResults.Any()
            ? new ContentListSearchFunctionReturn(false, "No Search String Parse Results?")
            : new ContentListSearchFunctionReturn(dateTimeSearchResults.All(x => x.Include),
                string.Join(Environment.NewLine, dateTimeSearchResults.Select(x => $"{x.Explanation} ({x.Include}).")));
    }

    public static ContentListSearchFunctionReturn FilterFocalLength(string itemFocalLengthString, string searchString)
    {
        if (!string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith("FOCAL LENGTH:", StringComparison.OrdinalIgnoreCase))
            searchString = searchString[13..];

        if (string.IsNullOrWhiteSpace(itemFocalLengthString) && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(true, "Blank Focal Length and Blank Search String (true).");

        if (string.IsNullOrWhiteSpace(itemFocalLengthString))
            return new ContentListSearchFunctionReturn(false,
                "Blank Focal Length with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(false,
                "Blank Search String with Not Blank Focal Length (false).");

        itemFocalLengthString = itemFocalLengthString.Trim();
        searchString = searchString.Trim();

        if (!double.TryParse(
                itemFocalLengthString.Replace("mm", string.Empty, StringComparison.OrdinalIgnoreCase).TrimNullToEmpty(),
                out var listItemFocalLength))
            //Couldn't parse a value from the list item's Focal Length - compare as string to the search string
            return new ContentListSearchFunctionReturn(itemFocalLengthString.Equals(searchString),
                $"Focal Length input of '{itemFocalLengthString}' could not " +
                $"be parsed into a numeric Focal Length to search - instead checking if the Focal Length as a string to is equal to '{searchString}'");

        //Remove mm at this point - not currently aware of another valid Focal Length measurement so removing this should be 
        //a nice way to make this optional for the user
        var tokens =
            FilterListSpaceDividedTokenList(
                searchString.Replace("mm", string.Empty, StringComparison.OrdinalIgnoreCase));

        var focalLengthComparisonTolerance = 1D;

        if (tokens.Count == 1)
        {
            if (!double.TryParse(tokens.First(), out var parsedFocalLength))
                return new ContentListSearchFunctionReturn(itemFocalLengthString.Contains(searchString),
                    $"Search input of {tokens.First()} could not " +
                    $"be parsed into a numeric Focal Length to search - instead checking if the item Focal Length '{itemFocalLengthString}' " +
                    $"contains '{tokens.First()}'");

            return new ContentListSearchFunctionReturn(
                Math.Abs(listItemFocalLength - parsedFocalLength) < focalLengthComparisonTolerance,
                $"Search Focal Length of {parsedFocalLength} compared to {listItemFocalLength} with a tolerance of {focalLengthComparisonTolerance}");
        }

        var focalLengthSearchResults = new List<ContentListSearchFunctionReturn>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var scanValue = tokens[i];

            if (!FilterListTokenOperatorList.Contains(scanValue))
            {
                if (!double.TryParse(tokens.First(), out var parsedFocalLength))
                {
                    focalLengthSearchResults.Add(new ContentListSearchFunctionReturn(
                        itemFocalLengthString.Contains(scanValue),
                        $"Search input of {scanValue} could not " +
                        $"be parsed into a numeric Focal Length to search - instead checking if the item Focal Length '{itemFocalLengthString}' " +
                        $"contains '{tokens.First()}'"));
                    continue;
                }

                focalLengthSearchResults.Add(new ContentListSearchFunctionReturn(
                    Math.Abs(listItemFocalLength - parsedFocalLength) < focalLengthComparisonTolerance,
                    $"Search Focal Length of {parsedFocalLength} compared to " +
                    $"{listItemFocalLength} with a tolerance of {focalLengthComparisonTolerance}"));
                continue;
            }

            i++;

            //Last token is a operator - this isn't valid, just continue...
            if (i >= tokens.Count) continue;

            var lookaheadValue = tokens[i];

            if (!double.TryParse(lookaheadValue, out var parsedFocalLengthForExpression))
            {
                focalLengthSearchResults.Add(new ContentListSearchFunctionReturn(
                    itemFocalLengthString.Contains(scanValue),
                    $"Search input of {scanValue} could not " +
                    $"be parsed into a numeric Focal Length to search - instead checking if the item Focal Length '{itemFocalLengthString}' " +
                    $"contains '{tokens.First()}'"));
                continue;
            }

            switch (scanValue)
            {
                case "==":
                    focalLengthSearchResults.Add(new ContentListSearchFunctionReturn(
                        Math.Abs(listItemFocalLength - parsedFocalLengthForExpression) < focalLengthComparisonTolerance,
                        $"Search Focal Length of {parsedFocalLengthForExpression} compared to " +
                        $"{listItemFocalLength} with a tolerance of {focalLengthComparisonTolerance}"));
                    break;
                case "!=":
                    focalLengthSearchResults.Add(new ContentListSearchFunctionReturn(
                        !(Math.Abs(listItemFocalLength - parsedFocalLengthForExpression) <
                          focalLengthComparisonTolerance),
                        $"Search Focal Length of {parsedFocalLengthForExpression} not equal to " +
                        $"{listItemFocalLength} with a tolerance of {focalLengthComparisonTolerance}"));
                    break;
                case ">":
                    focalLengthSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemFocalLength > parsedFocalLengthForExpression,
                        $"Evaluated Search Focal Length of {parsedFocalLengthForExpression} greater than {listItemFocalLength}"));
                    break;
                case ">=":
                    focalLengthSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemFocalLength >= parsedFocalLengthForExpression,
                        $"Evaluated Search Focal Length of {parsedFocalLengthForExpression} greater than or equal to {listItemFocalLength}"));
                    break;
                case "<":
                    focalLengthSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemFocalLength < parsedFocalLengthForExpression,
                        $"Evaluated Search Focal Length of {parsedFocalLengthForExpression} less than {listItemFocalLength}"));
                    break;
                case "<=":
                    focalLengthSearchResults.Add(new ContentListSearchFunctionReturn(
                        listItemFocalLength <= parsedFocalLengthForExpression,
                        $"Evaluated Search Focal Length of {parsedFocalLengthForExpression} less than or equal to {listItemFocalLength}"));
                    break;
            }
        }

        return !focalLengthSearchResults.Any()
            ? new ContentListSearchFunctionReturn(false, "No Search String Parse Results?")
            : new ContentListSearchFunctionReturn(focalLengthSearchResults.All(x => x.Include),
                string.Join(Environment.NewLine,
                    focalLengthSearchResults.Select(x => $"{x.Explanation} ({x.Include}).")));
    }

    public static ContentListSearchFunctionReturn FilterIso(string itemIsoString, string searchString)
    {
        if (!string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith("ISO:", StringComparison.OrdinalIgnoreCase))
            searchString = searchString[4..];

        if (string.IsNullOrWhiteSpace(itemIsoString) && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(true, "Blank ISO and Blank Search String (true).");

        if (string.IsNullOrWhiteSpace(itemIsoString))
            return new ContentListSearchFunctionReturn(false, "Blank ISO with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(false, "Blank Search String with Not Blank ISO (false).");

        itemIsoString = itemIsoString.Trim();
        searchString = searchString.Trim();

        if (!int.TryParse(itemIsoString.TrimNullToEmpty(), out var listItemIso))
            //Couldn't parse a value from the list item's ISO - compare as string to the search string
            return new ContentListSearchFunctionReturn(itemIsoString.Equals(searchString),
                $"ISO input of '{itemIsoString}' could not " +
                $"be parsed into a numeric ISO to search - instead checking if the ISO as a string to is equal to '{searchString}'");

        var tokens = FilterListSpaceDividedTokenList(searchString);

        if (tokens.Count == 1)
        {
            if (!int.TryParse(tokens.First(), out var parsedIso))
                return new ContentListSearchFunctionReturn(itemIsoString.Contains(searchString),
                    $"Search input of {tokens.First()} could not " +
                    $"be parsed into a numeric ISO to search - instead checking if the item ISO '{itemIsoString}' " +
                    $"contains '{tokens.First()}'");

            return new ContentListSearchFunctionReturn(listItemIso == parsedIso,
                $"Search ISO of {parsedIso} compared to {listItemIso}");
        }

        var isoSearchResults = new List<ContentListSearchFunctionReturn>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var scanValue = tokens[i];

            if (!FilterListTokenOperatorList.Contains(scanValue))
            {
                if (!int.TryParse(tokens.First(), out var parsedIso))
                {
                    isoSearchResults.Add(new ContentListSearchFunctionReturn(itemIsoString.Contains(scanValue),
                        $"Search input of {scanValue} could not " +
                        $"be parsed into a numeric ISO to search - instead checking if the item ISO '{itemIsoString}' " +
                        $"contains '{tokens.First()}'"));
                    continue;
                }

                isoSearchResults.Add(new ContentListSearchFunctionReturn(listItemIso == parsedIso,
                    $"Search ISO of {parsedIso} compared to " + $"{listItemIso}"));
                continue;
            }

            i++;

            //Last token is a operator - this isn't valid, just continue...
            if (i >= tokens.Count) continue;

            var lookaheadValue = tokens[i];

            if (!int.TryParse(lookaheadValue, out var parsedIsoForExpression))
            {
                isoSearchResults.Add(new ContentListSearchFunctionReturn(itemIsoString.Contains(scanValue),
                    $"Search input of {scanValue} could not " +
                    $"be parsed into a numeric ISO to search - instead checking if the item ISO '{itemIsoString}' " +
                    $"contains '{tokens.First()}'"));
                continue;
            }

            switch (scanValue)
            {
                case "==":
                    isoSearchResults.Add(new ContentListSearchFunctionReturn(listItemIso == parsedIsoForExpression,
                        $"Search ISO of {parsedIsoForExpression} compared to " + $"{listItemIso}"));
                    break;
                case "!=":
                    isoSearchResults.Add(new ContentListSearchFunctionReturn(listItemIso != parsedIsoForExpression,
                        $"Search ISO of {parsedIsoForExpression} not equal to " + $"{listItemIso}"));
                    break;
                case ">":
                    isoSearchResults.Add(new ContentListSearchFunctionReturn(listItemIso > parsedIsoForExpression,
                        $"Evaluated Search ISO of {parsedIsoForExpression} greater than {listItemIso}"));
                    break;
                case ">=":
                    isoSearchResults.Add(new ContentListSearchFunctionReturn(listItemIso >= parsedIsoForExpression,
                        $"Evaluated Search ISO of {parsedIsoForExpression} greater than or equal to {listItemIso}"));
                    break;
                case "<":
                    isoSearchResults.Add(new ContentListSearchFunctionReturn(listItemIso < parsedIsoForExpression,
                        $"Evaluated Search ISO of {parsedIsoForExpression} less than {listItemIso}"));
                    break;
                case "<=":
                    isoSearchResults.Add(new ContentListSearchFunctionReturn(listItemIso <= parsedIsoForExpression,
                        $"Evaluated Search ISO of {parsedIsoForExpression} less than or equal to {listItemIso}"));
                    break;
            }
        }

        return !isoSearchResults.Any()
            ? new ContentListSearchFunctionReturn(false, "No Search String Parse Results?")
            : new ContentListSearchFunctionReturn(isoSearchResults.All(x => x.Include),
                string.Join(Environment.NewLine, isoSearchResults.Select(x => $"{x.Explanation} ({x.Include}).")));
    }

    public static List<(string operatorString, string searchString)> FilterListOperatorDividedTokenList(
        string searchString)
    {
        var spaceSplitTokens = searchString.Split(" ").Select(x => x.TrimNullToEmpty())
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var tokens = new List<(string operatorString, string searchString)>();

        var singleCharacterOperators = new List<char> { '>', '=', '<' };
        var twoCharacterOperators = new List<string> { ">=", "==", "<=", "!=" };

        var currentFilter = new List<string>();
        var currentOperator = string.Empty;

        foreach (var loopTokens in spaceSplitTokens)
        {
            if (loopTokens.Length > 1 && twoCharacterOperators.Contains(loopTokens[..2]))
            {
                if (currentFilter.Any())
                {
                    tokens.Add((currentOperator, string.Join(" ", currentFilter)));
                    currentFilter.Clear();
                }

                currentOperator = loopTokens[..2];

                if (loopTokens.Length > 2) currentFilter.Add(loopTokens[2..]);
                continue;
            }

            if (singleCharacterOperators.Contains(loopTokens[0]))
            {
                if (currentFilter.Any())
                {
                    tokens.Add((currentOperator, string.Join(" ", currentFilter)));
                    currentFilter.Clear();
                }

                currentOperator = loopTokens[0] == '=' ? "==" : loopTokens[0].ToString();

                if (loopTokens.Length > 1) currentFilter.Add(loopTokens[1..]);
                continue;
            }

            currentFilter.Add(loopTokens);
        }

        if (currentFilter.Any()) tokens.Add((currentOperator, string.Join(" ", currentFilter)));

        return tokens;
    }

    private static List<string> FilterListSpaceDividedTokenList(string searchString)
    {
        var spaceSplitTokens = searchString.Split(" ").Select(x => x.TrimNullToEmpty())
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var tokens = new List<string>();

        var singleCharacterOperators = new List<char> { '>', '=', '<' };
        var twoCharacterOperators = new List<string> { ">=", "==", "<=", "!=" };

        foreach (var loopTokens in spaceSplitTokens)
        {
            if (loopTokens.Length > 1 && twoCharacterOperators.Contains(loopTokens[..2]))
            {
                tokens.Add(loopTokens[..2]);

                if (loopTokens.Length > 2) tokens.Add(loopTokens[2..]);
                continue;
            }

            if (singleCharacterOperators.Contains(loopTokens[0]))
            {
                tokens.Add(loopTokens[0] == '=' ? "==" : loopTokens[0].ToString());

                if (loopTokens.Length > 1) tokens.Add(loopTokens[1..]);
                continue;
            }

            tokens.Add(loopTokens);
        }

        return tokens;
    }

    public static ContentListSearchFunctionReturn FilterShutterSpeedLength(string itemShutterSpeedString,
        string searchString)
    {
        if (!string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith("SHUTTER SPEED:", StringComparison.OrdinalIgnoreCase))
            searchString = searchString[14..];

        if (string.IsNullOrWhiteSpace(itemShutterSpeedString) && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(true, "Blank Shutter Speed and Blank Search String (true).");

        if (string.IsNullOrWhiteSpace(itemShutterSpeedString))
            return new ContentListSearchFunctionReturn(false,
                "Blank Shutter Speed with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(false,
                "Blank Search String with Not Blank Shutter Speed (false).");

        itemShutterSpeedString = itemShutterSpeedString.Replace(" ", string.Empty).Trim();
        searchString = searchString.Trim();

        if (!Fraction.TryParse(itemShutterSpeedString, out var translatedItemShutterSpeed))
            return new ContentListSearchFunctionReturn(itemShutterSpeedString.Equals(searchString),
                $"Shutter Speed of '{itemShutterSpeedString}' could not " +
                $"be parsed into a numeric focal length to search - instead checking if the Shutter Speed as a string to is equal to '{searchString}'");

        var tokens = FilterListSpaceDividedTokenList(searchString);

        if (tokens.Count == 1)
        {
            if (!Fraction.TryParse(tokens[0].Replace(" ", string.Empty), out var translatedSingleInputToken))
                return new ContentListSearchFunctionReturn(itemShutterSpeedString.Contains(searchString),
                    $"Search input of {tokens[0]} could not " +
                    $"be parsed into a numeric focal length to search - instead checking if the item Shutter Speed '{itemShutterSpeedString}' " +
                    $"contains '{tokens[0]}'");

            return new ContentListSearchFunctionReturn(
                translatedSingleInputToken.IsEquivalentTo(translatedItemShutterSpeed),
                $"Search Shutter Speed of {tokens[0]} equals {itemShutterSpeedString}");
        }

        var shutterSpeedSearchResults = new List<ContentListSearchFunctionReturn>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var scanValue = tokens[i];

            if (!FilterListTokenOperatorList.Contains(scanValue))
            {
                if (!Fraction.TryParse(scanValue.Replace(" ", string.Empty), out var translatedToken))
                {
                    shutterSpeedSearchResults.Add(new ContentListSearchFunctionReturn(
                        itemShutterSpeedString.Contains(scanValue),
                        $"Search input of {scanValue} could not " +
                        $"be parsed into a numeric Shutter Speed to search - instead checking if the item Shutter Speed '{itemShutterSpeedString}' " +
                        $"contains '{tokens.First()}'"));
                    continue;
                }

                shutterSpeedSearchResults.Add(new ContentListSearchFunctionReturn(
                    translatedToken.IsEquivalentTo(translatedItemShutterSpeed),
                    $"Search Shutter Speed of {scanValue} compared to " + $"{itemShutterSpeedString}"));
                continue;
            }

            i++;

            //Last token is a operator - this isn't valid, just continue...
            if (i >= tokens.Count) continue;

            var lookAheadValue = tokens[i];

            if (!Fraction.TryParse(lookAheadValue, out var translatedLookAheadValue))
            {
                shutterSpeedSearchResults.Add(new ContentListSearchFunctionReturn(
                    itemShutterSpeedString.Contains(scanValue),
                    $"Search input of {scanValue} could not " +
                    $"be parsed into a numeric Shutter Speed to search - instead checking if the item Shutter Speed '{itemShutterSpeedString}' contains '{scanValue}'"));
                continue;
            }

            switch (scanValue)
            {
                case "==":
                    shutterSpeedSearchResults.Add(new ContentListSearchFunctionReturn(
                        translatedItemShutterSpeed.IsEquivalentTo(translatedLookAheadValue),
                        $"Search Shutter Speed of {lookAheadValue} compared to " + $"{itemShutterSpeedString}"));
                    break;
                case "!=":
                    shutterSpeedSearchResults.Add(new ContentListSearchFunctionReturn(
                        !translatedItemShutterSpeed.IsEquivalentTo(translatedLookAheadValue),
                        $"Search Shutter Speed of {lookAheadValue} not equal to " + $"{itemShutterSpeedString}"));
                    break;
                case ">":
                    shutterSpeedSearchResults.Add(new ContentListSearchFunctionReturn(
                        translatedLookAheadValue.CompareTo(translatedItemShutterSpeed) < 0,
                        $"Evaluated Search Shutter Speed of {lookAheadValue} greater than {itemShutterSpeedString}"));
                    break;
                case ">=":
                    shutterSpeedSearchResults.Add(new ContentListSearchFunctionReturn(
                        translatedLookAheadValue.CompareTo(translatedItemShutterSpeed) <= 0,
                        $"Evaluated Search Shutter Speed of {lookAheadValue} greater than or equal to {itemShutterSpeedString}"));
                    break;
                case "<":
                    shutterSpeedSearchResults.Add(new ContentListSearchFunctionReturn(
                        translatedLookAheadValue.CompareTo(translatedItemShutterSpeed) > 0,
                        $"Evaluated Search Shutter Speed of {lookAheadValue} less than {itemShutterSpeedString}"));
                    break;
                case "<=":
                    shutterSpeedSearchResults.Add(new ContentListSearchFunctionReturn(
                        translatedLookAheadValue.CompareTo(translatedItemShutterSpeed) >= 0,
                        $"Evaluated Search Shutter Speed of {lookAheadValue} less than or equal to {itemShutterSpeedString}"));
                    break;
            }
        }

        return !shutterSpeedSearchResults.Any()
            ? new ContentListSearchFunctionReturn(false, "No Search String Parse Results?")
            : new ContentListSearchFunctionReturn(shutterSpeedSearchResults.All(x => x.Include),
                string.Join(Environment.NewLine,
                    shutterSpeedSearchResults.Select(x => $"{x.Explanation} ({x.Include}).")));
    }

    public static ContentListSearchFunctionReturn FilterStringContains(string itemString, string searchString,
        string searchLabel)
    {
        if (!string.IsNullOrWhiteSpace(searchLabel) && !string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith(searchLabel.Trim(), StringComparison.OrdinalIgnoreCase))
            searchString = searchString[$"{searchLabel.Trim().Replace(":", string.Empty)}:".Length..];

        if (string.IsNullOrWhiteSpace(itemString) && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(true, $"Blank {searchLabel} and Blank Search String (true).");

        if (string.IsNullOrWhiteSpace(itemString))
            return new ContentListSearchFunctionReturn(false,
                $"Blank {searchLabel} with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchFunctionReturn(false,
                $"Blank Search String with Not Blank {searchLabel} (false).");

        itemString = itemString.Trim();
        searchString = searchString.Trim();

        var contains = itemString.Contains(searchString, StringComparison.OrdinalIgnoreCase);

        return new ContentListSearchFunctionReturn(
            itemString.Contains(searchString, StringComparison.OrdinalIgnoreCase),
            $"{searchLabel} contains {searchString} ({contains})");
    }
}

public record ContentListSearchFunctionReturn(bool Include, string Explanation);