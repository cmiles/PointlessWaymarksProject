using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.PowerShellRunnerData.Migrations;

[Migration(202407140700)]
public class AllowSimultaneousRunsMigration : Migration
{
    public override void Down()
    {
        throw new DataException(
            $"No Down Available for Migration {nameof(AllowSimultaneousRunsMigration)}");
    }

    public override void Up()
    {
        if (Schema.Table("ScriptJobs").Column("AllowSimultaneousRuns").Exists())
            return;

        Execute.Sql(@"ALTER TABLE ScriptJobs 
                    ADD COLUMN AllowSimultaneousRuns INTEGER NOT NULL DEFAULT 0");
    }
}