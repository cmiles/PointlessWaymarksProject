using Fractions;
using PointlessWaymarks.CmsData;

namespace PointlessWaymarks.CmsWpfControls.ContentList;

public static class ContentListSearchFunctions
{
    public static List<string> FilterListTokenOperatorList => new() { "==", ">", "<", ">=", "<=" };

    public static ContentListSearchReturn FilterStringContains(string itemString, string searchString,
        string searchLabel)
    {
        if (!string.IsNullOrWhiteSpace(searchLabel) && !string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith(searchLabel.Trim(), StringComparison.OrdinalIgnoreCase))
            searchString = searchString[($"{searchLabel.Trim().Replace(":", string.Empty)}:".Length)..];

        if (string.IsNullOrWhiteSpace(itemString) && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(true, $"Blank {searchLabel} and Blank Search String (true).");

        if (string.IsNullOrWhiteSpace(itemString))
            return new ContentListSearchReturn(false, $"Blank {searchLabel} with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(false, $"Blank Search String with Not Blank {searchLabel} (false).");

        itemString = itemString.Trim();
        searchString = searchString.Trim();

        var contains = itemString.Contains(searchString, StringComparison.OrdinalIgnoreCase);

        return new ContentListSearchReturn(itemString.Contains(searchString, StringComparison.OrdinalIgnoreCase),
            $"{searchLabel} contains {searchString} ({contains})");
    }

    public static ContentListSearchReturn FilterFocalLength(string itemFocalLengthString, string searchString)
    {
        if (!string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith("FOCAL LENGTH:", StringComparison.OrdinalIgnoreCase))
            searchString = searchString[13..];

        if (string.IsNullOrWhiteSpace(itemFocalLengthString) && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(true, "Blank Focal Length and Blank Search String (true).");

        if (string.IsNullOrWhiteSpace(itemFocalLengthString))
            return new ContentListSearchReturn(false, "Blank Focal Length with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(false, "Blank Search String with Not Blank Focal Length (false).");

        itemFocalLengthString = itemFocalLengthString.Trim();
        searchString = searchString.Trim();

        if (!double.TryParse(
                itemFocalLengthString.Replace("mm", string.Empty, StringComparison.OrdinalIgnoreCase).TrimNullToEmpty(),
                out var listItemFocalLength))
            //Couldn't parse a value from the list item's focal length - compare as string to the search string
            return new ContentListSearchReturn(itemFocalLengthString.Equals(searchString),
                $"Focal Length input of '{itemFocalLengthString}' could not " +
                $"be parsed into a numeric focal length to search - instead checking if the focal length as a string to is equal to '{searchString}'");

        //Remove mm at this point - not currently aware of another valid focal length measurement so removing this should be 
        //a nice way to make this optional for the user
        var tokens = FilterListTokenList(searchString.Replace("mm", string.Empty, StringComparison.OrdinalIgnoreCase));

        var focalLengthComparisonTolerance = 1D;

        if (tokens.Count == 1)
        {
            if (!double.TryParse(tokens.First(), out var parsedFocalLength))
                return new ContentListSearchReturn(itemFocalLengthString.Contains(searchString),
                    $"Search input of {tokens.First()} could not " +
                    $"be parsed into a numeric focal length to search - instead checking if the item focal length '{itemFocalLengthString}' " +
                    $"contains '{tokens.First()}'");

            return new ContentListSearchReturn(
                Math.Abs(listItemFocalLength - parsedFocalLength) < focalLengthComparisonTolerance,
                $"Search Focal Length of {parsedFocalLength} compared to {listItemFocalLength} with a tolerance of {focalLengthComparisonTolerance}");
        }

        var focalLengthSearchResults = new List<ContentListSearchReturn>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var scanValue = tokens[i];

            if (!FilterListTokenOperatorList.Contains(scanValue))
            {
                if (!double.TryParse(tokens.First(), out var parsedFocalLength))
                {
                    focalLengthSearchResults.Add(new ContentListSearchReturn(itemFocalLengthString.Contains(scanValue),
                        $"Search input of {scanValue} could not " +
                        $"be parsed into a numeric focal length to search - instead checking if the item focal length '{itemFocalLengthString}' " +
                        $"contains '{tokens.First()}'"));
                    continue;
                }

                focalLengthSearchResults.Add(new ContentListSearchReturn(
                    Math.Abs(listItemFocalLength - parsedFocalLength) < focalLengthComparisonTolerance,
                    $"Search Focal Length of {parsedFocalLength} compared to " +
                    $"{listItemFocalLength} with a tolerance of {focalLengthComparisonTolerance}"));
                continue;
            }

            i++;

            //Last token is a operator - this isn't valid, just continue...
            if (i >= tokens.Count) continue;

            var lookaheadValue = tokens[i];

            if (!int.TryParse(lookaheadValue, out var parsedFocalLengthForExpression))
            {
                focalLengthSearchResults.Add(new ContentListSearchReturn(itemFocalLengthString.Contains(scanValue),
                    $"Search input of {scanValue} could not " +
                    $"be parsed into a numeric focal length to search - instead checking if the item focal length '{itemFocalLengthString}' " +
                    $"contains '{tokens.First()}'"));
                continue;
            }

            switch (scanValue)
            {
                case "==":
                    focalLengthSearchResults.Add(new ContentListSearchReturn(
                        Math.Abs(listItemFocalLength - parsedFocalLengthForExpression) < focalLengthComparisonTolerance,
                        $"Search Focal Length of {parsedFocalLengthForExpression} compared to " +
                        $"{listItemFocalLength} with a tolerance of {focalLengthComparisonTolerance}"));
                    break;
                case "!=":
                    focalLengthSearchResults.Add(new ContentListSearchReturn(
                        !(Math.Abs(listItemFocalLength - parsedFocalLengthForExpression) <
                          focalLengthComparisonTolerance),
                        $"Search Focal Length of {parsedFocalLengthForExpression} not equal to " +
                        $"{listItemFocalLength} with a tolerance of {focalLengthComparisonTolerance}"));
                    break;
                case ">":
                    focalLengthSearchResults.Add(new ContentListSearchReturn(
                        listItemFocalLength > parsedFocalLengthForExpression,
                        $"Evaluated Search Focal Length of {parsedFocalLengthForExpression} greater than {listItemFocalLength}"));
                    break;
                case ">=":
                    focalLengthSearchResults.Add(new ContentListSearchReturn(
                        listItemFocalLength >= parsedFocalLengthForExpression,
                        $"Evaluated Search Focal Length of {parsedFocalLengthForExpression} greater than or equal to {listItemFocalLength}"));
                    break;
                case "<":
                    focalLengthSearchResults.Add(new ContentListSearchReturn(
                        listItemFocalLength < parsedFocalLengthForExpression,
                        $"Evaluated Search Focal Length of {parsedFocalLengthForExpression} less than {listItemFocalLength}"));
                    break;
                case "<=":
                    focalLengthSearchResults.Add(new ContentListSearchReturn(
                        listItemFocalLength <= parsedFocalLengthForExpression,
                        $"Evaluated Search Focal Length of {parsedFocalLengthForExpression} less than or equal to {listItemFocalLength}"));
                    break;
            }
        }

        return !focalLengthSearchResults.Any()
            ? new ContentListSearchReturn(false, "No Search String Parse Results?")
            : new ContentListSearchReturn(focalLengthSearchResults.All(x => x.Include),
                string.Join(Environment.NewLine,
                    focalLengthSearchResults.Select(x => $"{x.Explanation} ({x.Include}).")));
    }

    public static ContentListSearchReturn FilterShutterSpeedLength(string itemShutterSpeedString, string searchString)
    {
        if (!string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith("SHUTTER SPEED:", StringComparison.OrdinalIgnoreCase))
            searchString = searchString[14..];

        if (string.IsNullOrWhiteSpace(itemShutterSpeedString) && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(true, "Blank Shutter Speed and Blank Search String (true).");

        if (string.IsNullOrWhiteSpace(itemShutterSpeedString))
            return new ContentListSearchReturn(false, "Blank Shutter Speed with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(false, "Blank Search String with Not Blank Shutter Speed (false).");

        itemShutterSpeedString = itemShutterSpeedString.Replace(" ", string.Empty).Trim();
        searchString = searchString.Trim();

        if (!Fraction.TryParse(itemShutterSpeedString, out var translatedItemShutterSpeed))
            return new ContentListSearchReturn(itemShutterSpeedString.Equals(searchString),
                $"Shutter Speed of '{itemShutterSpeedString}' could not " +
                $"be parsed into a numeric focal length to search - instead checking if the shutter speed as a string to is equal to '{searchString}'");

        var tokens = FilterListTokenList(searchString);

        if (tokens.Count == 1)
        {
            if (!Fraction.TryParse(tokens[0].Replace(" ", string.Empty), out var translatedSingleInputToken))
                return new ContentListSearchReturn(itemShutterSpeedString.Contains(searchString),
                    $"Search input of {tokens[0]} could not " +
                    $"be parsed into a numeric focal length to search - instead checking if the item shutter speed '{itemShutterSpeedString}' " +
                    $"contains '{tokens[0]}'");

            return new ContentListSearchReturn(translatedSingleInputToken.IsEquivalentTo(translatedItemShutterSpeed),
                $"Search Shutter Speed of {tokens[0]} equals {itemShutterSpeedString}");
        }

        var shutterSpeedSearchResults = new List<ContentListSearchReturn>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var scanValue = tokens[i];

            if (!FilterListTokenOperatorList.Contains(scanValue))
            {
                if (!Fraction.TryParse(scanValue.Replace(" ", string.Empty), out var translatedToken))
                {
                    shutterSpeedSearchResults.Add(new ContentListSearchReturn(
                        itemShutterSpeedString.Contains(scanValue),
                        $"Search input of {scanValue} could not " +
                        $"be parsed into a numeric shutter speed to search - instead checking if the item shutter speed '{itemShutterSpeedString}' " +
                        $"contains '{tokens.First()}'"));
                    continue;
                }

                shutterSpeedSearchResults.Add(new ContentListSearchReturn(
                    translatedToken.IsEquivalentTo(translatedItemShutterSpeed),
                    $"Search Shutter Speed of {scanValue} compared to " +
                    $"{itemShutterSpeedString}"));
                continue;
            }

            i++;

            //Last token is a operator - this isn't valid, just continue...
            if (i >= tokens.Count) continue;

            var lookAheadValue = tokens[i];

            if (!Fraction.TryParse(lookAheadValue, out var translatedLookAheadValue))
            {
                shutterSpeedSearchResults.Add(new ContentListSearchReturn(itemShutterSpeedString.Contains(scanValue),
                    $"Search input of {scanValue} could not " +
                    $"be parsed into a numeric shutter speed to search - instead checking if the item shutter speed '{itemShutterSpeedString}' contains '{scanValue}'"));
                continue;
            }

            switch (scanValue)
            {
                case "==":
                    shutterSpeedSearchResults.Add(new ContentListSearchReturn(
                        translatedItemShutterSpeed.IsEquivalentTo(translatedLookAheadValue),
                        $"Search Shutter Speed of {lookAheadValue} compared to " +
                        $"{itemShutterSpeedString}"));
                    break;
                case "!=":
                    shutterSpeedSearchResults.Add(new ContentListSearchReturn(
                        !(translatedItemShutterSpeed.IsEquivalentTo(translatedLookAheadValue)),
                        $"Search Shutter Speed of {lookAheadValue} not equal to " +
                        $"{itemShutterSpeedString}"));
                    break;
                case ">":
                    shutterSpeedSearchResults.Add(new ContentListSearchReturn(
                        translatedLookAheadValue.CompareTo(translatedItemShutterSpeed) < 0,
                        $"Evaluated Search Shutter Speed of {lookAheadValue} greater than {itemShutterSpeedString}"));
                    break;
                case ">=":
                    shutterSpeedSearchResults.Add(new ContentListSearchReturn(
                        translatedLookAheadValue.CompareTo(translatedItemShutterSpeed) <= 0,
                        $"Evaluated Search Shutter Speed of {lookAheadValue} greater than or equal to {itemShutterSpeedString}"));
                    break;
                case "<":
                    shutterSpeedSearchResults.Add(new ContentListSearchReturn(
                        translatedLookAheadValue.CompareTo(translatedItemShutterSpeed) > 0,
                        $"Evaluated Search Shutter Speed of {lookAheadValue} less than {itemShutterSpeedString}"));
                    break;
                case "<=":
                    shutterSpeedSearchResults.Add(new ContentListSearchReturn(
                        translatedLookAheadValue.CompareTo(translatedItemShutterSpeed) >= 0,
                        $"Evaluated Search Shutter Speed of {lookAheadValue} less than or equal to {itemShutterSpeedString}"));
                    break;
            }
        }

        return !shutterSpeedSearchResults.Any()
            ? new ContentListSearchReturn(false, "No Search String Parse Results?")
            : new ContentListSearchReturn(shutterSpeedSearchResults.All(x => x.Include),
                string.Join(Environment.NewLine,
                    shutterSpeedSearchResults.Select(x => $"{x.Explanation} ({x.Include}).")));
    }

    public static ContentListSearchReturn FilterIso(string itemIsoString, string searchString)
    {
        if (!string.IsNullOrWhiteSpace(searchString) &&
            searchString.StartsWith("ISO:", StringComparison.OrdinalIgnoreCase))
            searchString = searchString[4..];

        if (string.IsNullOrWhiteSpace(itemIsoString) && string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(true, "Blank ISO and Blank Search String (true).");

        if (string.IsNullOrWhiteSpace(itemIsoString))
            return new ContentListSearchReturn(false, "Blank ISO with Not Blank Search String (false).");

        if (string.IsNullOrWhiteSpace(searchString))
            return new ContentListSearchReturn(false, "Blank Search String with Not Blank ISO (false).");

        itemIsoString = itemIsoString.Trim();
        searchString = searchString.Trim();

        if (!int.TryParse(
                itemIsoString.TrimNullToEmpty(),
                out var listItemIso))
            //Couldn't parse a value from the list item's ISO - compare as string to the search string
            return new ContentListSearchReturn(itemIsoString.Equals(searchString),
                $"ISO input of '{itemIsoString}' could not " +
                $"be parsed into a numeric ISO to search - instead checking if the ISO as a string to is equal to '{searchString}'");

        var tokens = FilterListTokenList(searchString);

        if (tokens.Count == 1)
        {
            if (!int.TryParse(tokens.First(), out var parsedIso))
                return new ContentListSearchReturn(itemIsoString.Contains(searchString),
                    $"Search input of {tokens.First()} could not " +
                    $"be parsed into a numeric ISO to search - instead checking if the item ISO '{itemIsoString}' " +
                    $"contains '{tokens.First()}'");

            return new ContentListSearchReturn(listItemIso == parsedIso,
                $"Search ISO of {parsedIso} compared to {listItemIso}");
        }

        var isoSearchResults = new List<ContentListSearchReturn>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var scanValue = tokens[i];

            if (!FilterListTokenOperatorList.Contains(scanValue))
            {
                if (!int.TryParse(tokens.First(), out var parsedIso))
                {
                    isoSearchResults.Add(new ContentListSearchReturn(itemIsoString.Contains(scanValue),
                        $"Search input of {scanValue} could not " +
                        $"be parsed into a numeric ISO to search - instead checking if the item ISO '{itemIsoString}' " +
                        $"contains '{tokens.First()}'"));
                    continue;
                }

                isoSearchResults.Add(new ContentListSearchReturn(
                    listItemIso == parsedIso,
                    $"Search ISO of {parsedIso} compared to " +
                    $"{listItemIso}"));
                continue;
            }

            i++;

            //Last token is a operator - this isn't valid, just continue...
            if (i >= tokens.Count) continue;

            var lookaheadValue = tokens[i];

            if (!int.TryParse(lookaheadValue, out var parsedIsoForExpression))
            {
                isoSearchResults.Add(new ContentListSearchReturn(itemIsoString.Contains(scanValue),
                    $"Search input of {scanValue} could not " +
                    $"be parsed into a numeric ISO to search - instead checking if the item ISO '{itemIsoString}' " +
                    $"contains '{tokens.First()}'"));
                continue;
            }

            switch (scanValue)
            {
                case "==":
                    isoSearchResults.Add(new ContentListSearchReturn(
                        listItemIso == parsedIsoForExpression,
                        $"Search ISO of {parsedIsoForExpression} compared to " +
                        $"{listItemIso}"));
                    break;
                case "!=":
                    isoSearchResults.Add(new ContentListSearchReturn(
                        listItemIso != parsedIsoForExpression,
                        $"Search ISO of {parsedIsoForExpression} not equal to " +
                        $"{listItemIso}"));
                    break;
                case ">":
                    isoSearchResults.Add(new ContentListSearchReturn(
                        listItemIso > parsedIsoForExpression,
                        $"Evaluated Search ISO of {parsedIsoForExpression} greater than {listItemIso}"));
                    break;
                case ">=":
                    isoSearchResults.Add(new ContentListSearchReturn(
                        listItemIso >= parsedIsoForExpression,
                        $"Evaluated Search ISO of {parsedIsoForExpression} greater than or equal to {listItemIso}"));
                    break;
                case "<":
                    isoSearchResults.Add(new ContentListSearchReturn(
                        listItemIso < parsedIsoForExpression,
                        $"Evaluated Search ISO of {parsedIsoForExpression} less than {listItemIso}"));
                    break;
                case "<=":
                    isoSearchResults.Add(new ContentListSearchReturn(
                        listItemIso <= parsedIsoForExpression,
                        $"Evaluated Search ISO of {parsedIsoForExpression} less than or equal to {listItemIso}"));
                    break;
            }
        }

        return !isoSearchResults.Any()
            ? new ContentListSearchReturn(false, "No Search String Parse Results?")
            : new ContentListSearchReturn(isoSearchResults.All(x => x.Include),
                string.Join(Environment.NewLine,
                    isoSearchResults.Select(x => $"{x.Explanation} ({x.Include}).")));
    }

    private static List<string> FilterListTokenList(string searchString)
    {
        var spaceSplitTokens = searchString.Split(" ").Select(x => x.TrimNullToEmpty())
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x == "=" ? "==" : x).ToList();

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
                tokens.Add(loopTokens[0].ToString());

                if (loopTokens.Length > 1) tokens.Add(loopTokens[1..]);
                continue;
            }

            tokens.Add(loopTokens);
        }

        return tokens;
    }
}

public record ContentListSearchReturn(bool Include, string Explanation);