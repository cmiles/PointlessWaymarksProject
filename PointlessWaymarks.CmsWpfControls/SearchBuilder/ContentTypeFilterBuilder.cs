using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

[NotifyPropertyChanged]
public partial class ContentTypeSearchBuilder
{
    public List<ContentTypeSearchChoice> ContentTypeChoices { get; set; } =
    [
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForFile },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForGeoJson },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForImage },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForLine },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForLink },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForMap },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForNote },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForPhoto },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForPost },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForPoint },
        new ContentTypeSearchChoice { TypeDescription = Db.ContentTypeDisplayStringForVideo }
    ];
}