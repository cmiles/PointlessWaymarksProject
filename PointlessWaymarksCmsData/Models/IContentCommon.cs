namespace PointlessWaymarksCmsData.Models
{
    public interface IContentCommon : IContentId, IMainImage, ITag, ITitleSummarySlugFolder,
        ICreatedAndLastUpdateOnAndBy, IShowInSiteFeed
    {
    }
}