using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PhotoGalleryHtml
{
    public static class DailyPhotoPageGenerators
    {
        public static async Task<List<DailyPhotosPage>> DailyPhotoGalleries(IProgress<string> progress)
        {
            var db = await Db.Context();

            progress?.Report("Starting Daily Photo Pages Generation");

            var allDates = (await db.PhotoContents.Select(x => x.PhotoCreatedOn).ToListAsync()).Select(x => x.Date).Distinct().OrderByDescending(x => x).ToList();

            progress?.Report($"Found {allDates.Count} Dates with Photos");

            var returnList = new List<DailyPhotosPage>();

            var loopCounter = 1;
            var loopGoal = allDates.Count;

            foreach (var loopDates in allDates)
            {
                if (loopCounter % 10 == 0)
                {
                    progress?.Report($"Daily Photo Page - {loopDates:D} - {loopCounter} of {loopGoal}");
                }
                var toAdd = await DailyPhotoGallery(loopDates);
                if (toAdd != null) returnList.Add(toAdd);

                loopCounter++;
            }

            return returnList;
        }

        public static async Task<DailyPhotosPage> DailyPhotoGallery(DateTime dateTimeForPictures)
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
                    datePhotos.SelectMany(x => x.Tags.Split(",")).Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList(),
                PageUrl = UserSettingsSingleton.CurrentSettings().DailyPhotoGalleryUrl(startsAfterOrOn)
            };

            return photoPage;
        }
    }
}