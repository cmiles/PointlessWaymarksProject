using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202008031434)]
    public class AddPointsTables : Migration
    {
        public override void Down()
        {
            if (Schema.Table("PointContents").Exists()) Delete.Table("PointContents");
            if (Schema.Table("PointDetails").Exists()) Delete.Table("PointDetails");
            if (Schema.Table("PointContentPointDetailLinks").Exists()) Delete.Table("PointContentPointDetailLinks");
            if (Schema.Table("HistoricPointContents").Exists()) Delete.Table("HistoricPointContents");
            if (Schema.Table("HistoricPointDetails").Exists()) Delete.Table("HistoricPointDetails");
            if (Schema.Table("HistoricPointContentPointDetailLinks").Exists())
                Delete.Table("HistoricPointContentPointDetailLinks");
            //The Note Index should not be reversed
        }

        public override void Up()
        {
            if (!Schema.Table("PointContents").Exists())
                Execute.Sql(@"
CREATE TABLE ""PointContents"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PointContents"" PRIMARY KEY AUTOINCREMENT,
    ""BodyContent"" TEXT NULL,
    ""BodyContentFormat"" TEXT NULL,
    ""ContentId"" TEXT NOT NULL,
    ""ContentVersion"" TEXT NOT NULL,
    ""CreatedBy"" TEXT NULL,
    ""CreatedOn"" TEXT NOT NULL,
    ""Elevation"" REAL NOT NULL,
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

            if (!Schema.Table("PointDetails").Exists())
                Execute.Sql(@"
CREATE TABLE ""PointDetails"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PointDetails"" PRIMARY KEY AUTOINCREMENT,
    ""ContentId"" TEXT NOT NULL,
    ""ContentVersion"" TEXT NOT NULL,
    ""CreatedBy"" TEXT NULL,
    ""CreatedOn"" TEXT NOT NULL,
    ""DataType"" TEXT NULL,
    ""LastUpdatedBy"" TEXT NULL,
    ""LastUpdatedOn"" TEXT NULL,
    ""StructuredDataAsJson"" TEXT NULL
)
; ");

            if (!Schema.Table("PointContentPointDetailLinks").Exists())
                Execute.Sql(@"
CREATE TABLE ""PointContentPointDetailLinks"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PointContentPointDetailLinks"" PRIMARY KEY AUTOINCREMENT,
    ""ContentVersion"" TEXT NOT NULL,
    ""CreatedBy"" TEXT NULL,
    ""CreatedOn"" TEXT NOT NULL,
    ""LastUpdatedBy"" TEXT NULL,
    ""LastUpdatedOn"" TEXT NULL,
    ""PointContentId"" TEXT NOT NULL,
    ""PointDetail"" TEXT NOT NULL
)
;");

            if (!Schema.Table("HistoricPointContents").Exists())
                Execute.Sql(@"
CREATE TABLE ""HistoricPointContents"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_HistoricPointContents"" PRIMARY KEY AUTOINCREMENT,
    ""BodyContent"" TEXT NULL,
    ""BodyContentFormat"" TEXT NULL,
    ""ContentId"" TEXT NOT NULL,
    ""ContentVersion"" TEXT NOT NULL,
    ""CreatedBy"" TEXT NULL,
    ""CreatedOn"" TEXT NOT NULL,
    ""Elevation"" REAL NOT NULL,
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
; ");

            if (!Schema.Table("HistoricPointDetails").Exists())
                Execute.Sql(@"
CREATE TABLE ""HistoricPointDetails"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_HistoricPointDetails"" PRIMARY KEY AUTOINCREMENT,
    ""ContentId"" TEXT NOT NULL,
    ""ContentVersion"" TEXT NOT NULL,
    ""CreatedBy"" TEXT NULL,
    ""CreatedOn"" TEXT NOT NULL,
    ""DataType"" TEXT NULL,
    ""LastUpdatedBy"" TEXT NULL,
    ""LastUpdatedOn"" TEXT NULL,
    ""StructuredDataAsJson"" TEXT NULL
)
; ");

            if (!Schema.Table("HistoricPointContentPointDetailLinks").Exists())
                Execute.Sql(@"
CREATE TABLE ""HistoricPointContentPointDetailLinks"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_HistoricPointContentPointDetailLinks"" PRIMARY KEY AUTOINCREMENT,
    ""ContentVersion"" TEXT NOT NULL,
    ""CreatedBy"" TEXT NULL,
    ""CreatedOn"" TEXT NOT NULL,
    ""LastUpdatedBy"" TEXT NULL,
    ""LastUpdatedOn"" TEXT NULL,
    ""PointContentId"" TEXT NOT NULL,
    ""PointDetail"" TEXT NOT NULL
)
; ");

            if (!Schema.Table("NoteContents").Index("IX_NoteContents_ContentId").Exists())
                Execute.Sql(@"
CREATE UNIQUE INDEX ""IX_NoteContents_ContentId"" ON ""NoteContents"" (""ContentId"")
; ");
        }
    }
}