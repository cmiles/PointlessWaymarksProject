using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PhotoGalleryHtml
{
    public static class DailyPhotosPageGenerator
    {
        public static async Task<string> DailyPhotosGallery(DateTime dateTimeForPictures)
        {
            var db = await Db.Context();

            var startsAfterOrOn = dateTimeForPictures.Date;
            var endsBefore = dateTimeForPictures.AddDays(1).Date;

            var datePhotos = await db.PhotoContents
                .Where(x => x.PhotoCreatedOn >= startsAfterOrOn && x.PhotoCreatedOn < endsBefore)
                .OrderBy(x => x.PhotoCreatedOn).ToListAsync();

            if (!datePhotos.Any()) return string.Empty;

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
                PhotoTags = string.Join(",",
                    datePhotos.SelectMany(x => x.Tags.Split(",")).Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim()).Distinct().ToList()),
                PageUrl = UserSettingsSingleton.CurrentSettings().DailyPhotosGalleryUrl(startsAfterOrOn)
            };

            return photoPage.TransformText();
        }
    }
}