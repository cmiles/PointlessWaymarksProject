namespace PointlessWaymarks.GarminConnect.Exceptions;

public class GarminConnectAuthenticationException : Exception
{
    public GarminConnectAuthenticationException() : base("Authentication error")
    {
    }

    public GarminConnectAuthenticationException(string message, Exception? innerException = null) : base(message,
        innerException)
    {
    }
}