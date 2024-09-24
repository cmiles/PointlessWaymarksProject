using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.PowerShellRunnerData.Migrations;

[Migration(202409230800)]
public class AddScriptTypeMigration : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(AddRunTotalSecondsMigration)}");
    }

    public override void Up()
    {
        if (Schema.Table("ScriptJobs").Column("ScriptType").Exists())
            return;

        Execute.Sql("ALTER TABLE ScriptJobs ADD COLUMN ScriptType TEXT NOT NULL DEFAULT 'PowerShell'");
    }
}