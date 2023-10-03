using System.Net;

namespace PointlessWaymarks.GarminConnect.Exceptions;

public class GarminConnectRequestException : Exception
{
    public GarminConnectRequestException(string url, HttpStatusCode status) : base(
        $"Request [{url}] return code {(int)status} ({status.ToString()}).")
    {
        Url = url;
        Status = status;
    }

    public HttpStatusCode Status { get; }
    public string Url { get; }
}