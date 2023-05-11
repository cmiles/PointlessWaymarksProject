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
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime()
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
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime()
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
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime()
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
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime(),
            PhotoCreatedOn = created,
            ShowPhotoSizes = UserSettingsSingleton.CurrentSettings().PhotoPagesHaveLinksToPhotoSizesByDefault,
            ShowPhotoPosition = UserSettingsSingleton.CurrentSettings().PhotoPagesShowPositionByDefault
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
            CreatedOn = created,
            FeedOn = created,
            ContentVersion = Db.ContentVersionDateTime()
        };

        return returnEntry;
    }
}