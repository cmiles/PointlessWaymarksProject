using Garmin.Connect;
using Garmin.Connect.Auth;
using Garmin.Connect.Models;
using Polly;

namespace PointlessWaymarks.SpatialTools;

public class ConnectGpxService : IRemoteGpxService
{
    public required string ConnectPassword { get; set; }
    public required string ConnectUsername { get; set; }

    private GarminConnectClient? _client;

    public async Task<FileInfo?> DownloadGpxFile(long activityId, string fullNameForFile)
    {
        var client = _client ?? new GarminConnectClient(new GarminConnectContext(new HttpClient(), new BasicAuthParameters(ConnectUsername, ConnectPassword)));

        var file = await Policy.Handle<Exception>().WaitAndRetryAsync(3, i => TimeSpan.FromMinutes(1 * i))
            .ExecuteAsync(async () =>
                await client.DownloadActivity(activityId, ActivityDownloadFormat.GPX));

        if (file is null) return null;

        await File.WriteAllBytesAsync(fullNameForFile, file);

        return new FileInfo(fullNameForFile);
    }

    public async Task<List<GarminActivity>> GetActivityList(DateTime startUtc, DateTime endUtc)
    {
        var client = _client ?? new GarminConnectClient(new GarminConnectContext(new HttpClient(), new 
            BasicAuthParameters(ConnectUsername, ConnectPassword)));
        var activities = await Policy.Handle<Exception>().WaitAndRetryAsync(3, i => TimeSpan.FromMinutes(1 * i))
            .ExecuteAsync(async () => await client.GetActivitiesByDate(startUtc, endUtc, string.Empty) ??
                                      []);
        return activities.ToList();
    }
}