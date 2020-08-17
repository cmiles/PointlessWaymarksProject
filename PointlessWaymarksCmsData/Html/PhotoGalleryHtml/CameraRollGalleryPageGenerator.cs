using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HtmlTags;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Html.CommonHtml;

namespace PointlessWaymarksCmsData.Html.PhotoGalleryHtml
{
    public static class CameraRollGalleryPageGenerator
    {
        public static async Task<CameraRollGalleryPage> CameraRoll(DateTime? generationVersion, IProgress<string> progress)
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
            var currentYear = -1;
            var currentMonth = -1;

            var yearList = allDates.Select(x => x.Year).Distinct().OrderByDescending(x => x).ToList();

            for (var i = 0; i < allDates.Count; i++)
            {
                var loopDate = allDates[i];

                var newYear = false;
                var newMonth = false;

                if (loopDate.Year != currentYear)
                {
                    newYear = true;
                    currentYear = loopDate.Year;

                    var yearNavigationDiv = new DivTag().AddClass("camera-roll-year-list-container");

                    var yearLabelContainer = new DivTag().AddClass("camera-roll-year-list-item");
                    var yearLabel = new DivTag().Text("Years:").AddClass("camera-roll-year-list-label");
                    yearLabelContainer.Children.Add(yearLabel);
                    yearNavigationDiv.Children.Add(yearLabelContainer);

                    foreach (var loopYear in yearList)
                    {
                        var yearContainer = new DivTag().AddClass("camera-roll-year-list-item");

                        if (loopYear == currentYear)
                        {
                            var activeYearContent = new DivTag().Text(loopYear.ToString())
                                .AddClass("camera-roll-year-list-content")
                                .AddClass("camera-roll-nav-current-selection");
                            yearContainer.Children.Add(activeYearContent);
                        }
                        else
                        {
                            var activeYearContent =
                                new LinkTag(loopYear.ToString(), $"#{loopYear}").AddClass(
                                    "camera-roll-year-list-content");
                            yearContainer.Children.Add(activeYearContent);
                        }

                        yearNavigationDiv.Children.Add(yearContainer);
                    }

                    cameraRollContainer.Children.Add(yearNavigationDiv);
                }

                if (loopDate.Month != currentMonth || newYear)
                {
                    newMonth = true;
                    currentMonth = loopDate.Month;

                    var currentYearMonths = allDates.Where(x => x.Year == currentYear).Select(x => x.Month).Distinct()
                        .OrderBy(x => x).ToList();

                    var monthNavigationDiv = new DivTag().AddClass("camera-roll-month-list-container");

                    var monthLabelContainer = new DivTag().AddClass("camera-roll-month-list-item");
                    var monthLabel = new DivTag(currentYear.ToString()).Text("Months:")
                        .AddClass("camera-roll-month-list-label");
                    monthLabelContainer.Children.Add(monthLabel);
                    monthNavigationDiv.Children.Add(monthLabelContainer);

                    for (var j = 1; j < 13; j++)
                    {
                        var monthContainer =
                            new DivTag($"{currentYear}-{currentMonth}").AddClass("camera-roll-month-list-item");

                        var monthText = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(j);

                        if (j == currentMonth)
                        {
                            var activeMonthContent = new DivTag().Text(monthText)
                                .AddClass("camera-roll-month-list-content")
                                .AddClass("camera-roll-nav-current-selection");
                            monthContainer.Children.Add(activeMonthContent);
                        }
                        else if (!currentYearMonths.Contains(j))
                        {
                            var activeMonthContent = new DivTag().Text(monthText)
                                .AddClass("camera-roll-month-list-content")
                                .AddClass("camera-roll-nav-unused-selection");
                            monthContainer.Children.Add(activeMonthContent);
                        }
                        else
                        {
                            var activeMonthContent =
                                new LinkTag(monthText, $"#{currentYear}-{j}").AddClass(
                                    "camera-roll-month-list-content");
                            monthContainer.Children.Add(activeMonthContent);
                        }

                        monthNavigationDiv.Children.Add(monthContainer);
                    }

                    cameraRollContainer.Children.Add(monthNavigationDiv);
                }

                if (i % 10 == 0) progress?.Report($"Camera Gallery Section - {loopDate:D} - {i} of {loopGoal}");

                var startsAfterOrOn = loopDate.Date;
                var endsBefore = loopDate.AddDays(1).Date;

                var datePhotos = await db.PhotoContents
                    .Where(x => x.PhotoCreatedOn >= startsAfterOrOn && x.PhotoCreatedOn < endsBefore)
                    .OrderBy(x => x.PhotoCreatedOn).ToListAsync();

                var infoItem = new DivTag().AddClass("camera-roll-info-item-container");

                var dateLink = new LinkTag($"{loopDate:yyyy MMMM d, dddd}",
                    UserSettingsSingleton.CurrentSettings().DailyPhotoGalleryUrl(loopDate),
                    "camera-roll-info-date-link");
                var dateDiv = new DivTag().AddClass("camera-roll-info-date");

                if (newMonth) dateDiv.Id($"{currentYear}-{currentMonth}");

                dateDiv.Children.Add(dateLink);
                infoItem.Children.Add(dateDiv);

                var cameras = datePhotos
                    .Where(x => !string.IsNullOrWhiteSpace(x.CameraMake) && !string.IsNullOrWhiteSpace(x.CameraModel))
                    .Select(x => $"{x.CameraMake.Trim()} {x.CameraModel.Trim()}").Distinct().OrderBy(x => x).ToList()
                    .JoinListOfStringsToCommonUsageListWithAnd();
                infoItem.Children.Add(new DivTag().AddClass("camera-roll-info-camera").Text(cameras));

                var lenses = datePhotos.Where(x => !string.IsNullOrWhiteSpace(x.Lens)).Select(x => x.Lens.Trim())
                    .Distinct().OrderBy(x => x).ToList().JoinListOfStringsToCommonUsageListWithAnd();
                infoItem.Children.Add(new DivTag().AddClass("camera-roll-info-lens").Text(lenses));

                cameraRollContainer.Children.Add(infoItem);

                foreach (var loopPhotos in datePhotos)
                {
                    var listItemPhotoListItem = new DivTag().AddClass("camera-roll-photo-item-container");
                    var photoItem = new PictureSiteInformation(loopPhotos.ContentId);
                    listItemPhotoListItem.Children.Add(
                        photoItem.PictureFigureWithLinkToPicturePageTag("(min-width: 1200px) 20vw, 120px"));

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
                MainImage = mainImage,
                GenerationVersion = generationVersion
            };

            return toReturn;
        }
    }
}