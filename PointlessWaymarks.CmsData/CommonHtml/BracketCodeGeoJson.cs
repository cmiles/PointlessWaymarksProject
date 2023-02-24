﻿using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.ContentHtml.GeoJsonHtml;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.CommonHtml;

public static class BracketCodeGeoJson
{
    public const string BracketCodeToken = "geojson";

    public static string Create(GeoJsonContent content)
    {
        return $@"{{{{{BracketCodeToken} {content.ContentId}; {content.Title}}}}}";
    }

    public static async Task<List<GeoJsonContent>> DbContentFromBracketCodes(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return new List<GeoJsonContent>();

        progress?.Report("Searching for Point Codes...");

        var guidList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken)
            .Select(x => x.contentGuid).Distinct().ToList();

        var returnList = new List<GeoJsonContent>();

        if (!guidList.Any()) return returnList;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in guidList)
        {
            var dbContent = await context.GeoJsonContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"GeoJson Code - Adding DbContent For {dbContent.Title}");

            returnList.Add(dbContent);
        }

        return returnList;
    }

    public static async Task<string?> Process(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for GeoJson Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.GeoJsonContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Adding GeoJson {dbContent.Title} from Code");

            toProcess = toProcess.ReplaceEach(loopMatch.bracketCodeText,
                () => GeoJsonParts.GeoJsonDivAndScriptWithCaption(dbContent));
        }

        return toProcess;
    }

    public static async Task<string?> ProcessForDirectLocalAccess(string? toProcess,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for GeoJson Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.GeoJsonContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"Adding GeoJson {dbContent.Title} from Code");

            toProcess = toProcess.ReplaceEach(loopMatch.bracketCodeText,
                () => GeoJsonParts.GeoJsonDivAndScriptWithCaptionForDirectLocalAccess(dbContent));
        }

        return toProcess;
    }

    public static async Task<string?> ProcessForEmail(string? toProcess, IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(toProcess)) return string.Empty;

        progress?.Report("Searching for GeoJson Codes...");

        var resultList = BracketCodeCommon.ContentBracketCodeMatches(toProcess, BracketCodeToken);

        if (!resultList.Any()) return toProcess;

        var context = await Db.Context().ConfigureAwait(false);

        foreach (var loopMatch in resultList)
        {
            var dbContent =
                await context.GeoJsonContents.FirstOrDefaultAsync(x => x.ContentId == loopMatch.contentGuid).ConfigureAwait(false);
            if (dbContent == null) continue;

            progress?.Report($"For Email Subbing GeoJson Map for Link {dbContent.Title} from Code");

            var linkTag =
                new LinkTag(
                    string.IsNullOrWhiteSpace(loopMatch.displayText)
                        ? dbContent.Title
                        : loopMatch.displayText.Trim(),
                    UserSettingsSingleton.CurrentSettings().GeoJsonPageUrl(dbContent), "geojson-page-link");

            var centeredEmailTag = Tags.EmailCenterTableTag(linkTag);

            toProcess = toProcess.Replace(loopMatch.bracketCodeText, centeredEmailTag.ToString());
        }

        return toProcess;
    }
}