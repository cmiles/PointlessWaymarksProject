using System.Data;
using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202008290748)]
    public class MakeElevationNullable : Migration
    {
        public override void Down()
        {
            throw new DataException("No Down Available for Migration AddAndReviseGenerationSupportTables");
        }

        public override void Up()
        {
            if (!Schema.Table("PointContents").Exists()) {
                Delete.Table("PointContents");

                Execute.Sql(@"
CREATE TABLE ""PointContents"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PointContents"" PRIMARY KEY AUTOINCREMENT,
    ""BodyContent"" TEXT NULL,
    ""BodyContentFormat"" TEXT NULL,
    ""ContentId"" TEXT NOT NULL,
    ""ContentVersion"" TEXT NOT NULL,
    ""CreatedBy"" TEXT NULL,
    ""CreatedOn"" TEXT NOT NULL,
    ""Elevation"" REAL NULL,
    ""Folder"" TEXT NULL,
    ""LastUpdatedBy"" TEXT NULL,
    ""LastUpdatedOn"" TEXT NULL,
    ""Latitude"" REAL NOT NULL,
    ""Longitude"" REAL NOT NULL,
    ""MainPicture"" TEXT NULL,
    ""ShowInMainSiteFeed"" INTEGER NOT NULL,
    ""Slug"" TEXT NULL,
    ""Summary"" TEXT NULL,
    ""Tags"" TEXT NULL,
    ""Title"" TEXT NULL,
    ""UpdateNotes"" TEXT NULL,
    ""UpdateNotesFormat"" TEXT NULL
);");
            }

            if (!Schema.Table("HistoricPointContents").Exists())
            {
                Delete.Table("HistoricPointContents");

                Execute.Sql(@"
CREATE TABLE ""HistoricPointContents"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_HistoricPointContents"" PRIMARY KEY AUTOINCREMENT,
    ""BodyContent"" TEXT NULL,
    ""BodyContentFormat"" TEXT NULL,
    ""ContentId"" TEXT NOT NULL,
    ""ContentVersion"" TEXT NOT NULL,
    ""CreatedBy"" TEXT NULL,
    ""CreatedOn"" TEXT NOT NULL,
    ""Elevation"" REAL NULL,
    ""Folder"" TEXT NULL,
    ""LastUpdatedBy"" TEXT NULL,
    ""LastUpdatedOn"" TEXT NULL,
    ""Latitude"" REAL NOT NULL,
    ""Longitude"" REAL NOT NULL,
    ""MainPicture"" TEXT NULL,
    ""ShowInMainSiteFeed"" INTEGER NOT NULL,
    ""Slug"" TEXT NULL,
    ""Summary"" TEXT NULL,
    ""Tags"" TEXT NULL,
    ""Title"" TEXT NULL,
    ""UpdateNotes"" TEXT NULL,
    ""UpdateNotesFormat"" TEXT NULL
)
); ");
            }
        }

    }
}