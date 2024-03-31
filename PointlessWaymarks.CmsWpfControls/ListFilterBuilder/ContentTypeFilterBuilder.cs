using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.ListFilterBuilder;

[NotifyPropertyChanged]
public partial class ContentTypeListFilterBuilder
{
    public ContentTypeListFilterBuilder()
    {
        ContentTypeChoices =
        [
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForFile },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForGeoJson },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForImage },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForLine },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForLink },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForMap },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForNote },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForPhoto },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForPost },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForPoint },
            new ContentTypeListFilterChoice { TypeDescription = Db.ContentTypeDisplayStringForVideo }
        ];

        ContentTypeChoices.ForEach(x => x.PropertyChanged += (sender, args) =>
        {
            OnPropertyChanged(nameof(ContentTypeChoices));
        });
    }

    public List<ContentTypeListFilterChoice> ContentTypeChoices { get; set; }

    public bool Not { get; set; }
}