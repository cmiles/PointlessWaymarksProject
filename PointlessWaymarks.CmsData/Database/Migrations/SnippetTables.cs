using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202410031357)]
public class SnippetTables : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration SnippetTables");
    }

    public override void Up()
    {
        if (!Schema.Table("Snippets").Exists())
            Execute.Sql("""
                        CREATE TABLE "Snippets" (
                            "Id" INTEGER NOT NULL CONSTRAINT "PK_Snippets" PRIMARY KEY AUTOINCREMENT,
                            "BodyContent" TEXT NULL,
                            "ContentId" TEXT NOT NULL,
                            "ContentVersion" TEXT NOT NULL,
                            "CreatedBy" TEXT NULL,
                            "CreatedOn" TEXT NOT NULL,
                            "LastUpdatedBy" TEXT NULL,
                            "LastUpdatedOn" TEXT NULL,
                            "Summary" TEXT NULL,
                            "Title" TEXT NULL
                        )
                        """);

        if (!Schema.Table("HistoricSnippets").Exists())
            Execute.Sql("""
                        CREATE TABLE "HistoricSnippets" (
                            "Id" INTEGER NOT NULL CONSTRAINT "PK_HistoricSnippets" PRIMARY KEY AUTOINCREMENT,
                            "BodyContent" TEXT NULL,
                            "ContentId" TEXT NOT NULL,
                            "ContentVersion" TEXT NOT NULL,
                            "CreatedBy" TEXT NULL,
                            "CreatedOn" TEXT NOT NULL,
                            "LastUpdatedBy" TEXT NULL,
                            "LastUpdatedOn" TEXT NULL,
                            "Summary" TEXT NULL,
                            "Title" TEXT NULL
                        )
                        """);
    }
}