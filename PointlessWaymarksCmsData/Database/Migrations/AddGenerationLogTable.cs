using System.Data;
using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202008181434)]
    public class AddGenerationLogTable : Migration
    {
        public override void Down()
        {
            throw new DataException("No Down Available for Migration AddAndReviseGenerationSupportTables");
        }

        public override void Up()
        {
            if (!Schema.Table("GenerationLogs").Exists())
                Execute.Sql(@"
CREATE TABLE ""GenerationLogs"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_GenerationLogs"" PRIMARY KEY AUTOINCREMENT,
    ""GenerationVersion"" TEXT NOT NULL,
    ""GenerationSettings"" TEXT NULL
); ");
        }
    }
}