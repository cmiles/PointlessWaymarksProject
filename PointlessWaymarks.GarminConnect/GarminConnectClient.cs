using PointlessWaymarks.GarminConnect.Models;

namespace PointlessWaymarks.GarminConnect;

public class GarminConnectClient(GarminConnectContext context)
{
    private const string ActivitiesUrl = "/activitylist-service/activities/search/activities";
    private const string ActivityUrl = "/activity-service/activity/";
    private const string CsvDownloadUrl = "/download-service/export/csv/activity/";
    private const string FitDownloadUrl = "/download-service/files/activity/";
    private const string GpxDownloadUrl = "/download-service/export/gpx/activity/";
    private const string KmlDownloadUrl = "/download-service/export/kml/activity/";
    private const string TcxDownloadUrl = "/download-service/export/tcx/activity/";

    public async Task<byte[]> DownloadActivity(long activityId, ActivityDownloadFormat format)
    {
        var urls = new Dictionary<ActivityDownloadFormat, string>
        {
            {
                ActivityDownloadFormat.ORIGINAL,
                $"{FitDownloadUrl}{activityId}"
            },
            {
                ActivityDownloadFormat.TCX,
                $"{TcxDownloadUrl}{activityId}"
            },
            {
                ActivityDownloadFormat.GPX,
                $"{GpxDownloadUrl}{activityId}"
            },
            {
                ActivityDownloadFormat.KML,
                $"{KmlDownloadUrl}{activityId}"
            },
            {
                ActivityDownloadFormat.CSV,
                $"{CsvDownloadUrl}{activityId}"
            }
        };
        if (!urls.ContainsKey(format)) throw new ArgumentException($"Unexpected value {format} for dl_fmt");

        var url = urls[format];

        var response = await context.MakeHttpGet(url);

        return await response.Content.ReadAsByteArrayAsync();
    }

    public Task<GarminActivity[]> GetActivities(int start, int limit)
    {
        var activitiesUrl = $"{ActivitiesUrl}?start={start}&limit={limit}";

        return context.GetAndDeserialize<GarminActivity[]>(activitiesUrl);
    }

    public async Task<GarminActivity[]> GetActivitiesByDate(DateTime startDate, DateTime endDate,
        string activityType)
    {
        string activitySlug;

        var start = 0;
        var limit = 20;

        // mimicking the behavior of the web interface that fetches 20 activities at a time
        // and automatically loads more on scroll
        if (!string.IsNullOrEmpty(activityType))
            activitySlug = "&activityType=" + activityType;
        else
            activitySlug = "";

        var result = new List<GarminActivity>();

        var returnData = true;
        while (returnData)
        {
            var activitiesUrl =
                $"{ActivitiesUrl}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&start={start}&limit={limit}{activitySlug}";

            var activities = await context.GetAndDeserialize<GarminActivity[]>(activitiesUrl);

            if (activities.Any())
            {
                result.AddRange(activities);
                start += limit;
            }
            else
            {
                returnData = false;
            }
        }

        return result.ToArray();
    }

    public Task<GarminActivityDetails> GetActivityDetails(long activityId, int maxChartSize,
        int maxPolylineSize = 4000)
    {
        var queryParams = $"maxChartSize={maxChartSize}&maxPolylineSize={maxPolylineSize}";
        var detailsUrl = $"{ActivityUrl}{activityId}/details?{queryParams}";

        return context.GetAndDeserialize<GarminActivityDetails>(detailsUrl);
    }
}