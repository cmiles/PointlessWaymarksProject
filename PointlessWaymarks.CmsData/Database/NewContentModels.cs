using PointlessWaymarks.CmsData.ContentGeneration;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Database;

public static class NewContentModels
{
    public static FileContent InitializeFileContent(FileContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new FileContent
        {
            ContentId = Guid.NewGuid(),
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = created,
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime(),
            PublicDownloadLink = UserSettingsSingleton.CurrentSettings().FilesHavePublicDownloadLinkByDefault
        };

        return returnEntry;
    }

    public static GeoJsonContent InitializeGeoJsonContent(GeoJsonContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new GeoJsonContent
        {
            ContentId = Guid.NewGuid(),
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = created,
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime(),
            PublicDownloadLink = UserSettingsSingleton.CurrentSettings().GeoJsonHasPublicDownloadLinkByDefault
        };

        return returnEntry;
    }

    public static ImageContent InitializeImageContent(ImageContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new ImageContent
        {
            ContentId = Guid.NewGuid(),
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = created,
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime(),
            ShowImageSizes = UserSettingsSingleton.CurrentSettings().ImagePagesHaveLinksToImageSizesByDefault
        };

        return returnEntry;
    }

    public static LineContent InitializeLineContent(LineContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new LineContent
        {
            ContentId = Guid.NewGuid(),
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = created,
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime(),
            PublicDownloadLink = UserSettingsSingleton.CurrentSettings().LinesHavePublicDownloadLinkByDefault,
            ShowContentReferencesOnMap =
                UserSettingsSingleton.CurrentSettings().LinesShowContentReferencesOnMapByDefault
        };

        return returnEntry;
    }

    public static LinkContent InitializeLinkContent(LinkContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new LinkContent
        {
            ContentId = Guid.NewGuid(),
            CreatedOn = created,
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            ContentVersion = Db.ContentVersionDateTime(),
            ShowInLinkRss = true
        };

        return returnEntry;
    }

    public static MapComponent InitializeMapComponent(MapComponent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new MapComponent
        {
            ContentId = Guid.NewGuid(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = created,
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            ContentVersion = Db.ContentVersionDateTime()
        };

        return returnEntry;
    }

    public static async Task<NoteContent> InitializeNoteContent(NoteContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new NoteContent
        {
            ContentId = Guid.NewGuid(),
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            CreatedOn = created,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime(),
            Slug = await NoteGenerator.UniqueNoteSlug()
        };

        return returnEntry;
    }

    public static PhotoContent InitializePhotoContent(PhotoContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new PhotoContent
        {
            ContentId = Guid.NewGuid(),
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedOn = created,
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime(),
            PhotoCreatedOn = created,
            ShowPhotoSizes = UserSettingsSingleton.CurrentSettings().PhotoPagesHaveLinksToPhotoSizesByDefault,
            ShowLocation = UserSettingsSingleton.CurrentSettings().PhotoPagesShowPositionByDefault
        };

        return returnEntry;
    }

    public static PointContent InitializePointContent(PointContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new PointContent
        {
            ContentId = Guid.NewGuid(),
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            CreatedOn = created,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime(),
            Latitude = UserSettingsSingleton.CurrentSettings().LatitudeDefault,
            Longitude = UserSettingsSingleton.CurrentSettings().LongitudeDefault
        };

        return returnEntry;
    }

    public static PointDetail InitializePointDetail(PointDetail? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new PointDetail
        {
            ContentId = Guid.NewGuid(),
            CreatedOn = created,
            ContentVersion = Db.ContentVersionDateTime()
        };

        return returnEntry;
    }

    public static PostContent InitializePostContent(PostContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new PostContent
        {
            ContentId = Guid.NewGuid(),
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            CreatedOn = created,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime()
        };

        return returnEntry;
    }

    public static VideoContent InitializeVideoContent(VideoContent? dbEntry)
    {
        var created = DateTime.Now;

        var returnEntry = dbEntry ?? new VideoContent
        {
            ContentId = Guid.NewGuid(),
            BodyContentFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            UpdateNotesFormat = UserSettingsUtilities.DefaultContentFormatChoice(),
            CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy,
            CreatedOn = created,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime(),
            VideoCreatedOn = created
        };

        return returnEntry;
    }
}