using System.Diagnostics;
using System.Net;
using PointlessWaymarks.GarminConnect.Authorization;
using PointlessWaymarks.GarminConnect.Converters;
using PointlessWaymarks.GarminConnect.Exceptions;

namespace PointlessWaymarks.GarminConnect;

public class GarminConnectContext(HttpClient httpClient, string username, string password)
{
    private const int Attempts = 3;
    private const int DelayAfterFailAuth = 300;
    private readonly GarminConnectAuthorization _authService = new();
    private GarminConnectAuthorizationState _authState = new();


    public async Task<T> GetAndDeserialize<T>(string url)
    {
        var response = await MakeHttpGet(url);
        var json = await response.Content.ReadAsByteArrayAsync();

        return GarminSerializer.To<T>(json);
    }

    private async Task Login()
    {
        await _authService.RefreshGarminAuthenticationAsync(_authState, username, password);
    }

    public Task<HttpResponseMessage> MakeHttpGet(string url)
    {
        return MakeHttpRequest(url, HttpMethod.Get);
    }

    private async Task<HttpResponseMessage> MakeHttpRequest(string url, HttpMethod method, HttpContent? content = null)
    {
        var force = false;
        Exception? exception = null;

        for (var i = 0; i < Attempts; i++)
            try
            {
                await ReLoginIfExpired(force);

                var httpRequestMessage = new HttpRequestMessage(method, $"https://connectapi.garmin.com{url}");
                httpRequestMessage.Headers.Add("Authorization", $"Bearer {_authState.OAuth2Token!.Access_Token}");
                httpRequestMessage.Headers.Add("NK", "NT");
                httpRequestMessage.Headers.Add("origin", GarminConnectAuthorization.OriginUrl);
                httpRequestMessage.Headers.Add("User-Agent", GarminConnectAuthorization.UserAgent);
                httpRequestMessage.Content = content;

                var response = await httpClient.SendAsync(httpRequestMessage);

                RaiseForStatus(response);

                return response;
            }
            catch (GarminConnectRequestException ex)
            {
                exception = ex;
                if (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    await Task.Delay(DelayAfterFailAuth);
                    force = true;
                    continue;
                }

                Debug.WriteLine(ex.Message);
                throw;
            }

        throw new GarminConnectAuthenticationException($"Authentication fail after {Attempts} attempts", exception);
    }

    private static void RaiseForStatus(HttpResponseMessage response)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.TooManyRequests:
                throw new GarminConnectTooManyRequestsException();
            case HttpStatusCode.NoContent:
            case HttpStatusCode.OK:
                return;
            default:
            {
                var message = $"{response.RequestMessage?.Method.Method}: {response.RequestMessage?.RequestUri}";
                throw new GarminConnectRequestException(message, response.StatusCode);
            }
        }
    }

    public async Task ReLoginIfExpired(bool force = false)
    {
        if (force || _authState.OAuth2Token == null ||
            _authState.OAuth2Token.Created.AddMinutes(_authState.OAuth2Token.Expires_In) < DateTime.Now)
            _authState = await _authService.RefreshGarminAuthenticationAsync(_authState, username, password);
    }
}