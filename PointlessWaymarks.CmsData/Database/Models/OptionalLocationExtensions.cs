using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.Database.Models;

public static class OptionalLocationExtensions
{
    public static IFeature? FeatureFromPoint(this IOptionalLocation content)
    {
        if (content.Longitude is null || content.Latitude is null) return null;
        return new Feature(content.PointFromLatitudeLongitude(), new AttributesTable());
    }
    
    public static bool HasLocation(this IOptionalLocation content)
    {
        return content.Longitude is not null && content.Latitude is not null;
    }
    
    public static async Task<bool> HasValidLocation(this IOptionalLocation content)
    {
        if (content.Longitude is null || content.Latitude is null) return false;
        
        if (!(await CommonContentValidation.LatitudeValidation(content.Latitude.Value)).Valid) return false;
        if (!(await CommonContentValidation.LongitudeValidation(content.Latitude.Value)).Valid) return false;
        
        return true;
    }
    
    /// <summary>
    ///     Returns either a Point or a PointZ from the Contents Values
    /// </summary>
    /// <returns></returns>
    public static Point? PointFromLatitudeLongitude(this IOptionalLocation content)
    {
        if (content.Longitude is null || content.Latitude is null) return null;
        return content.Elevation is null
            ? new Point(content.Longitude.Value, content.Latitude.Value)
            : new Point(content.Longitude.Value, content.Latitude.Value, content.Elevation.Value.FeetToMeters());
    }
}