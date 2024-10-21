using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202410210750)]
public class AddTrailContentTables : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddTrailContentTables");
    }

    public override void Up()
    {
        if (!Schema.Table("TrailContents").Exists())
            Execute.Sql("""
                        CREATE TABLE "TrailContents" (
                            "Id" INTEGER NOT NULL CONSTRAINT "PK_TrailContents" PRIMARY KEY AUTOINCREMENT,
                            "Bikes" TEXT NULL,
                            "BikesNote" TEXT NULL,
                            "Dogs" TEXT NULL,
                            "DogsNote" TEXT NULL,
                            "EndingPointContentId" TEXT NULL,
                            "Fee" TEXT NULL,
                            "FeeNote" TEXT NULL,
                            "LineContentId" TEXT NULL,
                            "LocationArea" TEXT NULL,
                            "MapComponentId" TEXT NULL,
                            "OtherDetails" TEXT NULL,
                            "StartingPointContentId" TEXT NULL,
                            "TrailShape" TEXT NULL,
                            "BodyContent" TEXT NULL,
                            "BodyContentFormat" TEXT NULL,
                            "ContentId" TEXT NOT NULL,
                            "ContentVersion" TEXT NOT NULL,
                            "CreatedBy" TEXT NULL,
                            "CreatedOn" TEXT NOT NULL,
                            "LastUpdatedBy" TEXT NULL,
                            "LastUpdatedOn" TEXT NULL,
                            "MainPicture" TEXT NULL,
                            "FeedOn" TEXT NOT NULL,
                            "IsDraft" INTEGER NOT NULL,
                            "ShowInMainSiteFeed" INTEGER NOT NULL,
                            "ShowInSearch" INTEGER NOT NULL,
                            "Tags" TEXT NULL,
                            "Title" TEXT NULL,
                            "Folder" TEXT NULL,
                            "Slug" TEXT NULL,
                            "Summary" TEXT NULL,
                            "UpdateNotes" TEXT NULL,
                            "UpdateNotesFormat" TEXT NULL
                        )
                        """);

        if (!Schema.Table("HistoricTrailContents").Exists())
            Execute.Sql("""
                        CREATE TABLE "HistoricTrailContents" (
                            "Id" INTEGER NOT NULL CONSTRAINT "PK_HistoricTrailContents" PRIMARY KEY AUTOINCREMENT,
                            "Bikes" TEXT NULL,
                            "BikesNote" TEXT NULL,
                            "Dogs" TEXT NULL,
                            "DogsNote" TEXT NULL,
                            "EndingPointContentId" TEXT NULL,
                            "Fee" TEXT NULL,
                            "FeeNote" TEXT NULL,
                            "LineContentId" TEXT NULL,
                            "LocationArea" TEXT NULL,
                            "MapComponentId" TEXT NULL,
                            "OtherDetails" TEXT NULL,
                            "StartingPointContentId" TEXT NULL,
                            "TrailShape" TEXT NULL,
                            "BodyContent" TEXT NULL,
                            "BodyContentFormat" TEXT NULL,
                            "ContentId" TEXT NOT NULL,
                            "ContentVersion" TEXT NOT NULL,
                            "CreatedBy" TEXT NULL,
                            "CreatedOn" TEXT NOT NULL,
                            "LastUpdatedBy" TEXT NULL,
                            "LastUpdatedOn" TEXT NULL,
                            "MainPicture" TEXT NULL,
                            "FeedOn" TEXT NOT NULL,
                            "IsDraft" INTEGER NOT NULL,
                            "ShowInMainSiteFeed" INTEGER NOT NULL,
                            "ShowInSearch" INTEGER NOT NULL,
                            "Tags" TEXT NULL,
                            "Title" TEXT NULL,
                            "Folder" TEXT NULL,
                            "Slug" TEXT NULL,
                            "Summary" TEXT NULL,
                            "UpdateNotes" TEXT NULL,
                            "UpdateNotesFormat" TEXT NULL
                        )
                        """);
    }
}