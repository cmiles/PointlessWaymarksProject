﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Html.CommonHtml;

namespace PointlessWaymarks.CmsData.Html.PhotoGalleryHtml
{
    public static class DailyPhotoPageGenerators
    {
        public static async Task<List<DailyPhotosPage>> DailyPhotoGalleries(List<DateTime> datesToCreate,
            DateTime? generationVersion, IProgress<string> progress)
        {
            var db = await Db.Context();

            progress?.Report($"Starting Daily Photo Pages Generation for {datesToCreate.Count} Dates");

            var allDates = (await db.PhotoContents.Select(x => x.PhotoCreatedOn).ToListAsync()).Select(x => x.Date)
                .Distinct().OrderByDescending(x => x).ToList();

            progress?.Report($"Found {allDates.Count} Dates with Photos");

            var returnList = new List<DailyPhotosPage>();

            var loopGoal = datesToCreate.Count;

            for (var i = 0; i < datesToCreate.Count; i++)
            {
                var loopDate = datesToCreate[i];

                if (i % 10 == 0) progress?.Report($"Daily Photo Page - {loopDate:D} - {i} of {loopGoal}");
                var toAdd = await DailyPhotoGallery(loopDate, generationVersion);

                var nextDate = allDates.Where(x => x > loopDate).OrderBy(x => x).FirstOrDefault();
                if (nextDate == default) toAdd.NextDailyPhotosPage = null;
                else toAdd.NextDailyPhotosPage = await DailyPhotoGallery(nextDate, generationVersion);

                var previousDate = allDates.Where(x => x < loopDate).OrderByDescending(x => x).FirstOrDefault();
                if (previousDate == default) toAdd.PreviousDailyPhotosPage = null;
                else toAdd.PreviousDailyPhotosPage = await DailyPhotoGallery(previousDate, generationVersion);

                returnList.Add(toAdd);
            }

            return returnList;
        }

        public static async Task<List<DailyPhotosPage>> DailyPhotoGalleries(DateTime? generationVersion,
            IProgress<string> progress)
        {
            var db = await Db.Context();

            progress?.Report("Starting Daily Photo Pages Generation");

            var allDates = (await db.PhotoContents.Select(x => x.PhotoCreatedOn).ToListAsync()).Select(x => x.Date)
                .Distinct().OrderByDescending(x => x).ToList();

            progress?.Report($"Found {allDates.Count} Dates with Photos");

            var returnList = new List<DailyPhotosPage>();

            var loopGoal = allDates.Count;

            for (var i = 0; i < allDates.Count; i++)
            {
                var loopDate = allDates[i];

                if (i % 10 == 0) progress?.Report($"Daily Photo Page - {loopDate:D} - {i} of {loopGoal}");
                var toAdd = await DailyPhotoGallery(loopDate, generationVersion);

                if (i > 0)
                {
                    toAdd.NextDailyPhotosPage = returnList[i - 1];

                    returnList[i - 1].PreviousDailyPhotosPage = toAdd;
                }

                returnList.Add(toAdd);
            }

            return returnList;
        }

        public static async Task<DailyPhotosPage> DailyPhotoGallery(DateTime dateTimeForPictures,
            DateTime? generationVersion)
        {
            var db = await Db.Context();

            var startsAfterOrOn = dateTimeForPictures.Date;
            var endsBefore = dateTimeForPictures.AddDays(1).Date;

            var datePhotos = await db.PhotoContents
                .Where(x => x.PhotoCreatedOn >= startsAfterOrOn && x.PhotoCreatedOn < endsBefore)
                .OrderBy(x => x.PhotoCreatedOn).ToListAsync();

            if (!datePhotos.Any()) return null;

            var photographersList = datePhotos.Where(x => !string.IsNullOrWhiteSpace(x.PhotoCreatedBy))
                .Select(x => x.PhotoCreatedBy).Distinct().ToList();
            var createdByList = datePhotos.Select(x => x.CreatedBy).Distinct().ToList();

            var photographersString = photographersList.ToList().JoinListOfStringsToCommonUsageListWithAnd();
            var photographersAndCreatedByString = photographersList.Concat(createdByList).Distinct().ToList()
                .JoinListOfStringsToCommonUsageListWithAnd();

            var photoPage = new DailyPhotosPage
            {
                MainImage = new PictureSiteInformation(datePhotos.First().ContentId),
                ImageList = datePhotos.Select(x => new PictureSiteInformation(x.ContentId)).ToList(),
                Title = $"Photographs - {dateTimeForPictures:MMMM d, dddd, yyyy}",
                Summary =
                    $"Photographs taken on {dateTimeForPictures:M/d/yyyy}{(photographersList.Any() ? " by " : "")}{photographersString}.",
                CreatedBy = photographersAndCreatedByString,
                PhotoPageDate = startsAfterOrOn,
                SiteName = UserSettingsSingleton.CurrentSettings().SiteName,
                PhotoTags =
                    datePhotos.SelectMany(Db.TagListParseToSlugsAndIsExcluded).Distinct().OrderBy(x => x.TagSlug)
                        .ToList(),
                PageUrl = UserSettingsSingleton.CurrentSettings().DailyPhotoGalleryUrl(startsAfterOrOn),
                GenerationVersion = generationVersion
            };

            return photoPage;
        }
    }
}