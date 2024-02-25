using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202402201027)]
public class MapIconTables : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration MapIconTables");
    }

    public override void Up()
    {
        if (!Schema.Table("MapIcons").Exists())
            Execute.Sql("""
                        CREATE TABLE "MapIcons" (
                            "Id" INTEGER NOT NULL CONSTRAINT "PK_MapIcons" PRIMARY KEY AUTOINCREMENT,
                            "ContentId" TEXT NOT NULL,
                            "IconName" TEXT NULL,
                            "IconSource" TEXT NULL,
                            "IconSvg" TEXT NULL,
                            "LastUpdatedBy" TEXT NULL,
                            "LastUpdatedOn" TEXT NOT NULL,
                            "ContentVersion" TEXT NOT NULL
                        )
                        """);

        if (!Schema.Table("HistoricMapIcons").Exists())
            Execute.Sql("""
                        CREATE TABLE "HistoricMapIcons" (
                            "Id" INTEGER NOT NULL CONSTRAINT "PK_HistoricMapIcons" PRIMARY KEY AUTOINCREMENT,
                            "ContentId" TEXT NOT NULL,
                            "IconName" TEXT NULL,
                            "IconSource" TEXT NULL,
                            "IconSvg" TEXT NULL,
                            "LastUpdatedBy" TEXT NULL,
                            "LastUpdatedOn" TEXT NOT NULL,
                            "ContentVersion" TEXT NOT NULL
                        )
                        """);
    }
}