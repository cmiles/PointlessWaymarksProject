using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202301180558)]
public class VideoContentTables : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration VideoContentTables");
    }

    public override void Up()
    {
        if (!Schema.Table("VideoContents").Exists())
            Execute.Sql("""
                        CREATE TABLE "VideoContents" (
                            "Id" INTEGER NOT NULL CONSTRAINT "PK_VideoContents" PRIMARY KEY AUTOINCREMENT,
                            "License" TEXT NULL,
                            "OriginalFileName" TEXT NULL,
                            "UserMainPicture" TEXT NULL,
                            "VideoCreatedBy" TEXT NULL,
                            "VideoCreatedOn" TEXT NOT NULL,
                            "VideoCreatedOnUtc" TEXT NULL,
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
                            "Tags" TEXT NULL,
                            "Folder" TEXT NULL,
                            "Slug" TEXT NULL,
                            "Summary" TEXT NULL,
                            "Title" TEXT NULL,
                            "UpdateNotes" TEXT NULL,
                            "UpdateNotesFormat" TEXT NULL
                        )
                        """);

        if (!Schema.Table("HistoricVideoContents").Exists())
            Execute.Sql("""
                        CREATE TABLE "HistoricVideoContents" (
                            "Id" INTEGER NOT NULL CONSTRAINT "PK_HistoricVideoContents" PRIMARY KEY AUTOINCREMENT,
                            "License" TEXT NULL,
                            "OriginalFileName" TEXT NULL,
                            "UserMainPicture" TEXT NULL,
                            "VideoCreatedBy" TEXT NULL,
                            "VideoCreatedOn" TEXT NOT NULL,
                            "VideoCreatedOnUtc" TEXT NULL,
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
                            "Tags" TEXT NULL,
                            "Folder" TEXT NULL,
                            "Slug" TEXT NULL,
                            "Summary" TEXT NULL,
                            "Title" TEXT NULL,
                            "UpdateNotes" TEXT NULL,
                            "UpdateNotesFormat" TEXT NULL
                        )
                        """);
    }
}