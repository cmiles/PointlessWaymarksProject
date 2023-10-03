using Flurl.Http;

namespace PointlessWaymarks.GarminConnect.Authorization;

public class GarminConnectGarthConsumerCredentials
{
    public string Consumer_Key { get; set; } = string.Empty;
    public string Consumer_Secret { get; set; } = string.Empty;

    public static async Task<GarminConnectGarthConsumerCredentials> CreateInstance()
    {
        return await "https://thegarth.s3.amazonaws.com/oauth_consumer.json"
            .GetJsonAsync<GarminConnectGarthConsumerCredentials>();
    }
}