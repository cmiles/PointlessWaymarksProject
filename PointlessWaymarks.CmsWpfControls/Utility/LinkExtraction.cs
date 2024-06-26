﻿using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.LinkContentEditor;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.Utility;

public static class LinkExtraction
{
    public static async Task ExtractNewAndShowLinkContentEditors(string? toExtractFrom,
        IProgress<string>? progressTracker, List<string>? excludedUrls = null)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        excludedUrls ??= [];
        excludedUrls = excludedUrls.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLower())
            .ToList();

        if (string.IsNullOrWhiteSpace(toExtractFrom))
        {
            progressTracker?.Report("Nothing to Extract From");
            return;
        }

        progressTracker?.Report("Looking for URLs");

        var allMatches = StringTools.UrlsFromText(toExtractFrom).Where(x =>
            !x.Contains(UserSettingsSingleton.CurrentSettings().SiteUrl(), StringComparison.OrdinalIgnoreCase) &&
            !excludedUrls.Contains(x.ToLower())).ToList();

        progressTracker?.Report($"Found {allMatches.Count} Matches");

        var linksToShow = new List<string>();

        var db = await Db.Context();

        foreach (var loopMatches in allMatches)
        {
            progressTracker?.Report($"Checking to see if {loopMatches} exists in database...");

            var alreadyExists = await db.LinkContents.AnyAsync(x => x.Url != null && x.Url.ToLower() == loopMatches.ToLower());
            if (alreadyExists)
            {
                progressTracker?.Report($"{loopMatches} exists in database...");
            }
            else
            {
                if (!linksToShow.Contains(loopMatches))
                {
                    progressTracker?.Report($"Adding {loopMatches} to list to show...");
                    linksToShow.Add(loopMatches);
                }
            }
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        foreach (var loopLinks in linksToShow)
        {
            progressTracker?.Report($"Launching an editor for {loopLinks}...");

            var newContent = LinkContent.CreateInstance();
            newContent.Url = loopLinks;

            var newWindow = await LinkContentEditorWindow.CreateInstance(newContent, true);

            await newWindow.PositionWindowAndShowOnUiThread();
        }
    }
}