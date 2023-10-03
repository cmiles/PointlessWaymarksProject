using Flurl.Http;

namespace PointlessWaymarks.GarminConnect.Authorization;

public class GarminConnectAuthorizationState
{
    public CookieJar? CookieJar { get; set; }
    public OAuth1Token? OAuth1Token { get; set; }
    public OAuth2Token? OAuth2Token { get; set; }
}