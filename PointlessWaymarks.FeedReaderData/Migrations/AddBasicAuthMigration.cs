using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.FeedReaderData.Migrations;

[Migration(202312060000)]
public class AddBasicAuthMigration : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(AddBasicAuthMigration)}");
    }

    public override void Up()
    {
        if (Schema.Table("Feeds").Column("UseBasicAuth").Exists())
            return;

        Execute.Sql(@"ALTER TABLE Feeds 
                    ADD COLUMN UseBasicAuth INTEGER 
                    NOT NULL DEFAULT 0");
        Execute.Sql(@"ALTER TABLE Feeds
                    ADD COLUMN BasicAuthPassword TEXT NOT NULL DEFAULT ''");
        Execute.Sql(@"ALTER TABLE Feeds
                    ADD COLUMN BasicAuthUsername TEXT NOT NULL DEFAULT ''");
    }
}