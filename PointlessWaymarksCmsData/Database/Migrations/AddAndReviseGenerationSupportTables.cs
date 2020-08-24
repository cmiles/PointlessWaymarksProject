using System;
using System.Data;
using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202008241244)]
    public class AddAndReviseGenerationSupportTables : Migration
    {
        public override void Down()
        {
            throw new DataException("No Down Available for Migration AddAndReviseGenerationSupportTables");
        }

        public override void Up()
        {
            if (!Schema.Table("MenuLinks").Column("ContentVersion").Exists())
                Alter.Table("MenuLinks").AddColumn("ContentVersion").AsString()
                    .SetExistingRowsTo(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).NotNullable();

            if (!Schema.Table("TagExclusions").Column("ContentVersion").Exists())
                Alter.Table("TagExclusions").AddColumn("ContentVersion").AsString()
                    .SetExistingRowsTo(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).NotNullable();

            if (Schema.Table("RelatedContents").Exists())
                Delete.Table("RelatedContents");

            if (!Schema.Table("GenerationRelatedContents").Exists())
                Execute.Sql(@"
CREATE TABLE ""GenerationRelatedContents"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_GenerationRelatedContents"" PRIMARY KEY AUTOINCREMENT,
    ""ContentOne"" TEXT NOT NULL,
    ""ContentTwo"" TEXT NOT NULL
);");

            if (!Schema.Table("GenerationTagLogs").Exists())
                Execute.Sql(@"
CREATE TABLE ""GenerationTagLogs"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_GenerationTagLogs"" PRIMARY KEY AUTOINCREMENT,
    ""TagSlug"" TEXT NULL,
    ""TagIsExcludedFromSearch"" INTEGER NOT NULL,
    ""GenerationVersion"" TEXT NOT NULL,
    ""RelatedContentId"" TEXT NOT NULL
);
");

            if (!Schema.Table("GenerationDailyPhotoLogs").Exists())
                Execute.Sql(@"
CREATE TABLE ""GenerationDailyPhotoLogs"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_GenerationDailyPhotoLogs"" PRIMARY KEY AUTOINCREMENT,
    ""DailyPhotoDate"" TEXT NOT NULL,
    ""GenerationVersion"" TEXT NOT NULL,
    ""RelatedContentId"" TEXT NOT NULL
);

");

            if (Schema.Table("GenerationContentIdReferences").Exists())
                Delete.Table("GenerationContentIdReferences");

            if (!Schema.Table("GenerationChangedContentIds").Exists())
                Execute.Sql(@"
CREATE TABLE ""GenerationChangedContentIds"" (""ContentId"" UNIQUEIDENTIFIER NOT NULL, CONSTRAINT ""PK_GenerationContentIdReferences"" PRIMARY KEY (""ContentId""))");
        }
    }
}