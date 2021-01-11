namespace PointlessWaymarks.CmsData
{
    public record IsValid(bool Valid, string Explanation);

    public record Success(bool Succeeded, string Explanation);
}