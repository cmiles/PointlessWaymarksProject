using System.Data;
using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202008031843)]
    public class PointTableDetailAndIndexCleanup : Migration
    {
        public override void Down()
        {
            throw new DataException(
                "No Down Available for Migration AddImagePhotoBodyContentColumnCleanUpHistoricPhotoContent");
        }

        public override void Up()
        {
            if (!Schema.Table("PointContents").Index("IX_PointContents_ContentId").Exists())
                Execute.Sql(@"
CREATE UNIQUE INDEX ""IX_PointContents_ContentId"" ON ""PointContents"" (""ContentId"")
; ");

            if (!Schema.Table("PointDetails").Index("IX_PointDetails_ContentId").Exists())
                Execute.Sql(@"
CREATE UNIQUE INDEX ""IX_PointDetails_ContentId"" ON ""PointDetails"" (""ContentId"")
; ");

            if (!Schema.Table("PointContentPointDetailLinks").Column("PointDetailContentId").Exists())
                Execute.Sql(@"
                CREATE TABLE PointContentPointDetailLinksV2 (
                    Id INTEGER NOT NULL CONSTRAINT PK_PointContentPointDetailLinks PRIMARY KEY AUTOINCREMENT,
                    ContentVersion TEXT NOT NULL,
                    CreatedBy TEXT NULL,
                    CreatedOn TEXT NOT NULL,
                    LastUpdatedBy TEXT NULL,
                    LastUpdatedOn TEXT NULL,
                    PointContentId TEXT NOT NULL,
                    PointDetailContentId TEXT NOT NULL
                );

                INSERT INTO PointContentPointDetailLinksV2(
                    ContentVersion,
                    CreatedBy,
                    CreatedOn,
                    LastUpdatedBy,
                    LastUpdatedOn,
                    PointContentId,
                    PointDetailContentId)
                SELECT 
                    ContentVersion,
                    CreatedBy,
                    CreatedOn,
                    LastUpdatedBy,
                    LastUpdatedOn,
                    PointContentId,
                    PointDetail
                FROM
                    PointContentPointDetailLinks;

                    DROP TABLE PointContentPointDetailLinks;

                    ALTER TABLE PointContentPointDetailLinksV2 RENAME to PointContentPointDetailLinks;");

            if (!Schema.Table("PointContentPointDetailLinks").Index("IX_PointContentPointDetailLinks_PointContentId")
                .Exists())
                Execute.Sql(@"
CREATE INDEX ""IX_PointContentPointDetailLinks_PointContentId"" ON ""PointContentPointDetailLinks"" (""PointContentId"")
; ");

            if (!Schema.Table("HistoricPointContentPointDetailLinks").Column("PointDetailContentId").Exists())
                Execute.Sql(@"
                CREATE TABLE HistoricPointContentPointDetailLinksV2 (
                    Id INTEGER NOT NULL CONSTRAINT PK_HistoricPointContentPointDetailLinks PRIMARY KEY AUTOINCREMENT,
                    ContentVersion TEXT NOT NULL,
                    CreatedBy TEXT NULL,
                    CreatedOn TEXT NOT NULL,
                    LastUpdatedBy TEXT NULL,
                    LastUpdatedOn TEXT NULL,
                    PointContentId TEXT NOT NULL,
                    PointDetailContentId TEXT NOT NULL
                );

                INSERT INTO HistoricPointContentPointDetailLinksV2(
                    ContentVersion,
                    CreatedBy,
                    CreatedOn,
                    LastUpdatedBy,
                    LastUpdatedOn,
                    PointContentId,
                    PointDetailContentId)
                SELECT 
                    ContentVersion,
                    CreatedBy,
                    CreatedOn,
                    LastUpdatedBy,
                    LastUpdatedOn,
                    PointContentId,
                    PointDetail
                FROM
                    HistoricPointContentPointDetailLinks;

                    DROP TABLE HistoricPointContentPointDetailLinks;

                    ALTER TABLE HistoricPointContentPointDetailLinksV2 RENAME to HistoricPointContentPointDetailLinks;");
        }
    }
}