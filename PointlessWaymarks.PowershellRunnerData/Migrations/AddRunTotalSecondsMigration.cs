using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CloudBackupData.Migrations;

[Migration(202407071700)]
public class AddRunTotalSecondsMigration : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(AddRunTotalSecondsMigration)}");
    }

    public override void Up()
    {
        if (Schema.Table("ScriptJobRuns").Column("LengthInSeconds").Exists())
            return;

        Execute.Sql(@"ALTER TABLE ScriptJobRuns 
                    ADD COLUMN LengthInSeconds INTEGER");
    }
}