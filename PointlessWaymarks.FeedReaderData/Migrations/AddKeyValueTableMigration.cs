using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.FeedReaderData.Migrations;

[Migration(202312083000)]
public class AddKeyValueTableMigration : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(AddBasicAuthMigration)}");
    }

    public override void Up()
    {
        if (Schema.Table("KeyValues").Exists())
            return;

        Execute.Sql(
            """
            CREATE TABLE "KeyValues" (
            	"Id"	INTEGER,
            	"Key"	TEXT NOT NULL,
            	"Value"	TEXT NOT NULL,
            	PRIMARY KEY("Id" AUTOINCREMENT)
            );
            """);
    }
}