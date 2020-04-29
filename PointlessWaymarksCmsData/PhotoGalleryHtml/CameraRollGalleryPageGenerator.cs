using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Pictures;

namespace PointlessWaymarksCmsData.PhotoGalleryHtml
{
    public static class CameraRollGalleryPageGenerator
    {
        public static async Task<CameraRollGalleryPage> CameraRoll(IProgress<string> progress)
        {
            var db = await Db.Context();

            progress?.Report("Starting Camera Roll Generation");

            var allDates = (await db.PhotoContents.Select(x => x.PhotoCreatedOn).ToListAsync()).Select(x => x.Date)
                .Distinct().OrderByDescending(x => x).ToList();

            progress?.Report($"Found {allDates.Count} Dates with Photos for Camera Roll");

            var loopGoal = allDates.Count;

            var cameraRollContainer = new DivTag().AddClass("camera-roll-list");

            PictureSiteInformation mainImage = null;
            var isFirstItem = true;

            for (var i = 0; i < allDates.Count; i++)
            {
                var loopDate = allDates[i];

                if (i % 10 == 0) progress?.Report($"Camera Gallery Section - {loopDate:D} - {i} of {loopGoal}");

                var startsAfterOrOn = loopDate.Date;
                var endsBefore = loopDate.AddDays(1).Date;

                var datePhotos = await db.PhotoContents
                    .Where(x => x.PhotoCreatedOn >= startsAfterOrOn && x.PhotoCreatedOn < endsBefore)
                    .OrderBy(x => x.PhotoCreatedOn).ToListAsync();


                var infoItem = new DivTag().AddClass("camera-roll-info-item-container");

                infoItem.Children.Add(new DivTag().AddClass("camera-roll-info-date")
                    .Text($"{loopDate:yyyy MMMM d, dddd}"));

                var cameras = datePhotos
                    .Where(x => !string.IsNullOrWhiteSpace(x.CameraMake) && !string.IsNullOrWhiteSpace(x.CameraModel))
                    .Select(x => $"{x.CameraMake.Trim()} {x.CameraModel.Trim()}").Distinct().OrderBy(x => x).ToList().JoinListOfStringsToCommonUsageListWithAnd();
                infoItem.Children.Add(new DivTag().AddClass("camera-roll-info-camera").Text(cameras));

                var lenses = datePhotos
                    .Where(x => !string.IsNullOrWhiteSpace(x.Lens))
                    .Select(x => x.Lens.Trim()).Distinct().OrderBy(x => x).ToList().JoinListOfStringsToCommonUsageListWithAnd();
                infoItem.Children.Add(new DivTag().AddClass("camera-roll-info-lens").Text(lenses));

                cameraRollContainer.Children.Add(infoItem);

                foreach (var loopPhotos in datePhotos)
                {
                    var listItemPhotoListItem = new DivTag().AddClass("camera-roll-photo-item-container");
                    var photoItem = new PictureSiteInformation(loopPhotos.ContentId);
                    listItemPhotoListItem.Children.Add(photoItem.PictureFigureWithLinkToPicturePageTag("120px"));

                    cameraRollContainer.Children.Add(listItemPhotoListItem);

                    if (isFirstItem)
                    {
                        isFirstItem = false;
                        mainImage = photoItem;
                    }
                }
            }

            var createdByEntries =
                (await db.PhotoContents.GroupBy(x => x.PhotoCreatedBy).Select(x => x.Key).ToListAsync())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).OrderBy(x => x).ToList();

            var toReturn = new CameraRollGalleryPage
            {
                CreatedBy = string.Join(",", createdByEntries),
                PageUrl = UserSettingsSingleton.CurrentSettings().CameraRollPhotoGalleryUrl(),
                CameraRollContentTag = cameraRollContainer,
                SiteName = UserSettingsSingleton.CurrentSettings().SiteName,
                LastDateGroupDateTime = allDates.First().Date,
                MainImage = mainImage
            };

            return toReturn;
        }
    }
}