namespace PointlessWaymarks.GarminConnect.Authorization;

public record OAuth2Token
{
    public string Access_Token { get; set; }
    public DateTime Created { get; init; } = DateTime.Now;
    public int Expires_In { get; set; }
    public string Jti { get; set; }
    public string Refresh_Token { get; set; }
    public int Refresh_Token_Expires_In { get; set; }
    public string Scope { get; set; }
    public string Token_Type { get; set; }
}