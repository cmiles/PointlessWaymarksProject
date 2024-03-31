namespace PointlessWaymarks.SpatialTools.GeoNames;

public class GeoName
{
    public string? adminCode1 { get; set; }
    public GeoNameAdminCode adminCodes1 { get; set; }
    public string? adminName1 { get; set; }
    public string? countryCode { get; set; }
    public string? countryId { get; set; }
    public string? countryName { get; set; }
    public string? fcl { get; set; }
    public string? fclName { get; set; }
    public string? fcode { get; set; }
    public string? fcodeName { get; set; }
    public int geonameId { get; set; }
    public double lat { get; set; }
    public double lng { get; set; }
    public string? name { get; set; }
    public int population { get; set; }
    public string? toponymName { get; set; }
}