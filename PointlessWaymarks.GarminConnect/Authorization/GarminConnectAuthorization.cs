using System.Text.RegularExpressions;
using System.Web;
using Flurl.Http;
using OAuth;
using PointlessWaymarks.GarminConnect.Exceptions;

namespace PointlessWaymarks.GarminConnect.Authorization;

public class GarminConnectAuthorization()
{
    public const string OriginUrl = "https://sso.garmin.com";
    public const string RefererUrl = "https://sso.garmin.com/sso/signin";
    public const string SsoEmbedUrl = "https://sso.garmin.com/sso/embed";
    public const string SsoSigninUrl = "https://sso.garmin.com/sso/signin";

    public const string UserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 16_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";

    private readonly object _gauthQueryParameters = new
    {
        id = "gauth-widget",
        embedWidget = "true",
        gauthHost = "https://sso.garmin.com/sso"
    };

    private async Task<GarminConnectAuthorizationState> CompleteGarminAuthenticationAsync(string loginResult,
        GarminConnectAuthorizationState state)
    {
        // Try to find the full post login ServiceTicket
        var ticketRegex = new Regex("embed\\?ticket=(?<ticket>[^\"]+)\"");
        var ticketMatch = ticketRegex.Match(loginResult);
        if (!ticketMatch.Success)
            throw new GarminConnectAuthenticationException(
                "Could not find Ticket in the Login Result.");

        var ticket = ticketMatch.Groups.GetValueOrDefault("ticket")?.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(ticket))
            throw new GarminConnectAuthenticationException(
                "Garmin Ticket was found from the Login Result but is null or empty...");

        //Get the info from/for Garth and Oauth
        var consumerCredentials = await GarminConnectGarthConsumerCredentials.CreateInstance();
        await GetOAuth1Async(ticket, state, consumerCredentials);

        state.OAuth2Token = await GetOAuth2TokenAsync(state, consumerCredentials);

        return state;
    }

    private async Task GetOAuth1Async(string ticket, GarminConnectAuthorizationState auth,
        GarminConnectGarthConsumerCredentials credentials)
    {
        var oauthClient = OAuthRequest.ForRequestToken(credentials.Consumer_Key, credentials.Consumer_Secret);
        oauthClient.RequestUrl =
            $"https://connectapi.garmin.com/oauth-service/oauth/preauthorized?ticket={ticket}&login-url=https://sso.garmin.com/sso/embed&accepts-mfa-tokens=true";
        var oauth1Response = await oauthClient.RequestUrl
            .WithHeader("User-Agent", UserAgent)
            .WithHeader("Authorization", oauthClient.GetAuthorizationHeader())
            .GetStringAsync();

        var queryParams = HttpUtility.ParseQueryString(oauth1Response ?? string.Empty);

        var oAuthToken = queryParams.Get("oauth_token");
        var oAuthTokenSecret = queryParams.Get("oauth_token_secret");

        if (string.IsNullOrWhiteSpace(oAuthToken) || string.IsNullOrWhiteSpace(oAuthTokenSecret))
            throw new GarminConnectAuthenticationException(
                $"Oauth Token is invalid - Token is blank {string.IsNullOrWhiteSpace(oAuthToken)}, Secret is blank {string.IsNullOrWhiteSpace(oAuthTokenSecret)}");

        auth.OAuth1Token = new OAuth1Token()
        {
            Token = oAuthToken,
            TokenSecret = oAuthTokenSecret
        };
    }

    private Task<OAuth2Token> GetOAuth2TokenAsync(GarminConnectAuthorizationState state,
        GarminConnectGarthConsumerCredentials credentials)
    {
        var oauthClient2 = OAuthRequest.ForProtectedResource("POST", credentials.Consumer_Key,
            credentials.Consumer_Secret, state.OAuth1Token!.Token, state.OAuth1Token.TokenSecret);
        oauthClient2.RequestUrl = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0";

        //Helpful comments below from the https://github.com/philosowaffle/peloton-to-garmin/ - thanks!
        return oauthClient2.RequestUrl
            .WithHeader("User-Agent", UserAgent)
            .WithHeader("Authorization", oauthClient2.GetAuthorizationHeader())
            .WithHeader("Content-Type",
                "application/x-www-form-urlencoded") // this header is required, without it you get a 500
            .PostUrlEncodedAsync(
                new object()) // hack: PostAsync() will drop the content-type header, by posting empty object we trick flurl into leaving the header
            .ReceiveJson<OAuth2Token>();
    }

    public async Task<GarminConnectAuthorizationState> RefreshGarminAuthenticationAsync(
        GarminConnectAuthorizationState auth,
        string userName, string password)
    {
        await SsoEmbedUrl
            .WithHeader("User-Agent", UserAgent)
            .WithHeader("origin", OriginUrl)
            .SetQueryParams(_gauthQueryParameters)
            .WithCookies(out var jar)
            .GetStringAsync();

        object csrfRequest = new
        {
            id = "gauth-widget",
            embedWidget = "true",
            gauthHost = "https://sso.garmin.com/sso/embed",
            service = "https://sso.garmin.com/sso/embed",
            source = "https://sso.garmin.com/sso/embed",
            redirectAfterAccountLoginUrl = "https://sso.garmin.com/sso/embed",
            redirectAfterAccountCreationUrl = "https://sso.garmin.com/sso/embed"
        };

        var tokenResult = await SsoSigninUrl
            .WithHeader("User-Agent", UserAgent)
            .WithHeader("origin", OriginUrl)
            .SetQueryParams(csrfRequest)
            .WithCookies(jar)
            .GetAsync()
            .ReceiveString();

        var tokenRegex = new Regex("name=\"_csrf\"\\s+value=\"(?<csrf>.+?)\"");
        var match = tokenRegex.Match(tokenResult);

        var csrfToken = match.Groups.GetValueOrDefault("csrf")?.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(csrfToken))
            throw new GarminConnectAuthenticationException("Found csrfToken but its blank.");

        var sendCredentialsRequest = new
        {
            username = userName,
            password,
            embed = "true",
            _csrf = csrfToken
        };

        var redirectedTo = string.Empty;

        var result = await SsoSigninUrl
            .WithHeader("User-Agent", UserAgent)
            .WithHeader("origin", OriginUrl)
            .WithHeader("referer", RefererUrl)
            .WithHeader("NK", "NT")
            .SetQueryParams(csrfRequest)
            .WithCookies(jar)
            .OnRedirect((r) => { redirectedTo = r.Redirect.Url; })
            .PostUrlEncodedAsync(sendCredentialsRequest)
            .ReceiveString();


        if (redirectedTo.Contains("https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"))
            throw new GarminConnectAuthenticationException(
                "This program doesn't currently support MFA for Garmin Connect.");

        if (string.IsNullOrWhiteSpace(result))
            throw new GarminConnectAuthenticationException(
                "Blank result from ");

        var loginResult = result;
        return await CompleteGarminAuthenticationAsync(loginResult, auth);
    }
}