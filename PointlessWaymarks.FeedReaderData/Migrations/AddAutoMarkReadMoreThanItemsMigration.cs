using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.FeedReaderData.Migrations;

[Migration(202412270000)]
public class AddAutoMarkReadMoreThanItemsMigration : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(AddAutoMarkReadMoreThanItemsMigration)}");
    }

    public override void Up()
    {
        if (!Schema.Table("Feeds").Column("AutoMarkReadMoreThanItems").Exists())
            Execute.Sql(@"ALTER TABLE Feeds 
                    ADD COLUMN AutoMarkReadMoreThanItems INTEGER NULL");
        if (!Schema.Table("HistoricFeeds").Column("AutoMarkReadMoreThanItems").Exists())
            Execute.Sql(@"ALTER TABLE HistoricFeeds 
                    ADD COLUMN AutoMarkReadMoreThanItems INTEGER NULL");
    }
}