namespace PointlessWaymarksCmsData.Database.Models
{
    public interface IContentCommon : IContentId, IMainImage, ITag, ITitleSummarySlugFolder,
        ICreatedAndLastUpdateOnAndBy, IShowInSiteFeed, IBodyContent
    {
    }
}