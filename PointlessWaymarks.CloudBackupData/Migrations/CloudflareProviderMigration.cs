using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CloudBackupData.Migrations;

[Migration(202404260753)]
public class CloudflareProviderMigration : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(CloudflareProviderMigration)}");
    }
    
    public override void Up()
    {
        if (!Schema.Table("BackupJobs").Column("CloudProvider").Exists())
            Execute.Sql(@"ALTER TABLE BackupJobs 
                    ADD COLUMN CloudProvider TEXT NOT NULL DEFAULT ''");
    }
}