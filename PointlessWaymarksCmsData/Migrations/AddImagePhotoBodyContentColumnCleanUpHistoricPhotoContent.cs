using System.Data;
using FluentMigrator;

namespace PointlessWaymarksCmsData.Migrations
{
    [Migration(202007071734)]
    public class AddImagePhotoBodyContentColumnCleanUpHistoricPhotoContent : Migration
    {
        public override void Down()
        {
            throw new DataException("No Down Available for Migration AddImagePhotoBodyContentColumnCleanUpHistoricPhotoContent");
        }

        public override void Up()
        {
            if (!Schema.Table("PhotoContents").Column("BodyContent").Exists())
                Alter.Table("PhotoContents").AddColumn("BodyContent").AsString().Nullable();
            if (!Schema.Table("PhotoContents").Column("BodyContentFormat").Exists())
                Alter.Table("PhotoContents").AddColumn("BodyContentFormat").AsString().Nullable()
                    .SetExistingRowsTo("MarkdigMarkdown01");

            if (!Schema.Table("HistoricPhotoContents").Column("BodyContent").Exists())
                Execute.Sql(@"
                CREATE TABLE HistoricPhotoContentsV2 (
                    Id INTEGER NOT NULL CONSTRAINT PK_PhotoContents PRIMARY KEY AUTOINCREMENT,
                    AltText TEXT NULL,
                    Aperture TEXT NULL,
                    BodyContent TEXT NULL,
                    BodyContentFormat TEXT NULL,
                    CameraMake TEXT NULL,
                    CameraModel TEXT NULL,
                    FocalLength TEXT NULL,
                    Iso INTEGER NULL,
                    Lens TEXT NULL,
                    License TEXT NULL,
                    OriginalFileName TEXT NULL,
                    PhotoCreatedBy TEXT NULL,
                    PhotoCreatedOn TEXT NOT NULL,
                    ShutterSpeed TEXT NULL,
                    ShowInMainSiteFeed INTEGER NOT NULL,
                    ContentId TEXT NOT NULL,
                    ContentVersion TEXT NOT NULL,
                    CreatedBy TEXT NULL,
                    CreatedOn TEXT NOT NULL,
                    LastUpdatedBy TEXT NULL,
                    LastUpdatedOn TEXT NULL,
                    Tags TEXT NULL,
                    Folder TEXT NULL,
                    Slug TEXT NULL,
                    Summary TEXT NULL,
                    Title TEXT NULL,
                    MainPicture TEXT NULL,
                    UpdateNotes TEXT NULL,
                    UpdateNotesFormat TEXT NULL
                );

                INSERT INTO HistoricPhotoContentsV2(
                    AltText,
                    Aperture,
                    CameraMake,
                    CameraModel,
                    FocalLength,
                    Iso,
                    Lens,
                    License,
                    PhotoCreatedBy,
                    PhotoCreatedOn,
                    ShutterSpeed,
                    ShowInMainSiteFeed,
                    ContentId,
                    ContentVersion,
                    CreatedBy,
                    CreatedOn,
                    LastUpdatedBy,
                    LastUpdatedOn,
                    Tags,
                    Folder,
                    Slug,
                    Summary,
                    Title,
                    MainPicture,
                    UpdateNotes,
                    UpdateNotesFormat)
                SELECT AltText,
                    Aperture,
                    CameraMake,
                    CameraModel,
                    FocalLength,
                    Iso,
                    Lens,
                    License,
                    PhotoCreatedBy,
                    PhotoCreatedOn,
                    ShutterSpeed,
                    ShowInMainSiteFeed,
                    ContentId,
                    ContentVersion,
                    CreatedBy,
                    CreatedOn,
                    LastUpdatedBy,
                    LastUpdatedOn,
                    Tags,
                    Folder,
                    Slug,
                    Summary,
                    Title,
                    MainPicture,
                    UpdateNotes,
                    UpdateNotesFormat
                FROM
                    HistoricPhotoContents;

                    DROP TABLE HistoricPhotoContents;

                    ALTER TABLE HistoricPhotoContentsV2 RENAME to HistoricPhotoContents;");

            if (!Schema.Table("ImageContents").Column("BodyContent").Exists())
            {
                Execute.Sql(@"
                CREATE TABLE ImageContentsV2 (
                Id INTEGER NOT NULL CONSTRAINT PK_ImageContents PRIMARY KEY AUTOINCREMENT,
                    AltText TEXT NULL,
                    BodyContent TEXT NULL,
                    BodyContentFormat TEXT NULL,
                    OriginalFileName TEXT NULL,
                    ContentId TEXT NOT NULL,
                    ContentVersion TEXT NOT NULL,
                    CreatedBy TEXT NULL,
                    CreatedOn TEXT NOT NULL,
                    LastUpdatedBy TEXT NULL,
                    LastUpdatedOn TEXT NULL,
                    Tags TEXT NULL,
                    Folder TEXT NULL,
                    ShowInMainSiteFeed INTEGER NOT NULL,
                    Slug TEXT NULL,
                    Summary TEXT NULL,
                    Title TEXT NULL,
                    MainPicture TEXT NULL,
                    UpdateNotes TEXT NULL,
                    UpdateNotesFormat TEXT NULL,
                    ShowInSearch INTEGER);

                INSERT INTO ImageContentsV2(AltText, BodyContent, BodyContentFormat, OriginalFileName,
                    ContentId, ContentVersion, CreatedBy, CreatedOn, LastUpdatedBy, LastUpdatedOn,
                    Tags, Folder, ShowInMainSiteFeed, Slug, Summary, Title, MainPicture, UpdateNotes, UpdateNotesFormat, ShowInSearch)
                SELECT AltText, ImageSourceNotes, 'MarkdigMarkdown01', OriginalFileName, 
                ContentId, ContentVersion, CreatedBy, CreatedOn, LastUpdatedBy, LastUpdatedOn,
                Tags, Folder, ShowInMainSiteFeed, Slug, Summary, Title, MainPicture, UpdateNotes, UpdateNotesFormat, ShowInSearch
                FROM ImageContents;

                DROP TABLE ImageContents;

                ALTER TABLE ImageContentsV2 RENAME to ImageContents; 
                
                COMMIT;");

                Execute.Sql(@"BEGIN TRANSACTION;

                CREATE TABLE HistoricImageContentsV2 (
                    Id INTEGER NOT NULL CONSTRAINT PK_ImageContents PRIMARY KEY AUTOINCREMENT,
                    AltText TEXT NULL,
                    BodyContent TEXT NULL,
	                BodyContentFormat TEXT NULL,
                    OriginalFileName TEXT NULL,
                    ContentId TEXT NOT NULL,
                    ContentVersion TEXT NOT NULL,
                    CreatedBy TEXT NULL,
                    CreatedOn TEXT NOT NULL,
                    LastUpdatedBy TEXT NULL,
                    LastUpdatedOn TEXT NULL,
                    Tags TEXT NULL,
                    Folder TEXT NULL,
                    ShowInMainSiteFeed INTEGER NOT NULL,
                    Slug TEXT NULL,
                    Summary TEXT NULL,
                    Title TEXT NULL,
                    MainPicture TEXT NULL,
                    UpdateNotes TEXT NULL,
                    UpdateNotesFormat TEXT NULL,
	                ShowInSearch INTEGER);

                INSERT INTO HistoricImageContentsV2(AltText, BodyContent, BodyContentFormat, OriginalFileName, 
                ContentId, ContentVersion, CreatedBy, CreatedOn, LastUpdatedBy, LastUpdatedOn,
                Tags, Folder, ShowInMainSiteFeed, Slug, Summary, Title, MainPicture, UpdateNotes, UpdateNotesFormat, ShowInSearch)
                SELECT AltText, ImageSourceNotes, 'MarkdigMarkdown01', OriginalFileName, 
                ContentId, ContentVersion, CreatedBy, CreatedOn, LastUpdatedBy, LastUpdatedOn,
                Tags, Folder, ShowInMainSiteFeed, Slug, Summary, Title, MainPicture, UpdateNotes, UpdateNotesFormat, ShowInSearch
                FROM HistoricImageContents;

                DROP TABLE HistoricImageContents;

                ALTER TABLE HistoricImageContentsV2 RENAME to HistoricImageContents;");
            }
        }
    }
}