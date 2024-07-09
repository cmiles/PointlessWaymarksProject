using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.PowerShellRunnerData.Migrations;

[Migration(202407071300)]
public class AddDeleteScriptJobRunsAfterMonthsMigration : Migration
{
    public override void Down()
    {
        throw new DataException(
            $"No Down Available for Migration {nameof(AddDeleteScriptJobRunsAfterMonthsMigration)}");
    }

    public override void Up()
    {
        if (Schema.Table("ScriptJobs").Column("DeleteScriptJobRunsAfterMonths").Exists())
            return;

        Execute.Sql(@"ALTER TABLE ScriptJobs 
                    ADD COLUMN DeleteScriptJobRunsAfterMonths INTEGER NOT NULL DEFAULT 12");
    }
}