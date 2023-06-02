namespace PointlessWaymarks.CmsData.Database.Models;

public interface IContentCommon : IContentId, IMainImage, ITag, ITitleSummarySlugFolder,
    ICreatedAndLastUpdateOnAndBy, IMainSiteFeed, IBodyContent
{ }