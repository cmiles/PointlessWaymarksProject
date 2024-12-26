using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.FeedReaderData.Migrations;

[Migration(202412260000)]
public class AddAutoMarkReadMigration : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(AddAutoMarkReadMigration)}");
    }

    public override void Up()
    {
        if (!Schema.Table("Feeds").Column("AutoMarkReadAfterDays").Exists())
            Execute.Sql(@"ALTER TABLE Feeds 
                    ADD COLUMN AutoMarkReadAfterDays INTEGER NULL");
        if (!Schema.Table("HistoricFeeds").Column("AutoMarkReadAfterDays").Exists())
            Execute.Sql(@"ALTER TABLE HistoricFeeds 
                    ADD COLUMN AutoMarkReadAfterDays INTEGER NULL");
    }
}