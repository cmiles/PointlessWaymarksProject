using System.Data;
using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202008130653)]
    public class AddAndReviseGenerationSupportTables : Migration
    {
        public override void Down()
        {
            throw new DataException("No Down Available for Migration AddAndReviseGenerationSupportTables");
        }

        public override void Up()
        {
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
    ""GenerationVersion"" TEXT NOT NULL,
    ""RelatedContentId"" TEXT NOT NULL
);

CREATE INDEX ""IX_GenerationTagLogs_RelatedContentId"" ON ""GenerationTagLogs"" (""RelatedContentId"");
CREATE INDEX ""IX_GenerationTagLogs_GenerationVersion_TagSlug"" ON ""GenerationTagLogs"" (""GenerationVersion"", ""TagSlug"");
");

            if (!Schema.Table("GenerationDailyPhotoLogs").Exists())
                Execute.Sql(@"
CREATE TABLE ""GenerationDailyPhotoLogs"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_GenerationDailyPhotoLogs"" PRIMARY KEY AUTOINCREMENT,
    ""DailyPhotoDate"" TEXT NOT NULL,
    ""GenerationVersion"" TEXT NOT NULL,
    ""RelatedContentId"" TEXT NOT NULL
);

CREATE INDEX ""IX_GenerationDailyPhotoLogs_RelatedContentId"" ON ""GenerationDailyPhotoLogs"" (""RelatedContentId"");
CREATE INDEX ""IX_GenerationDailyPhotoLogs_GenerationVersion_DailyPhotoDate"" ON ""GenerationDailyPhotoLogs"" (""GenerationVersion"", ""DailyPhotoDate"");
");
        }
    }
}