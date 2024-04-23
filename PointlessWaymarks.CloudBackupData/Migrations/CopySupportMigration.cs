using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CloudBackupData.Migrations;

[Migration(202404230808)]
public class CopySupportMigration : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(CopySupportMigration)}");
    }
    
    public override void Up()
    {
        if (!Schema.Table("CloudCopies").Exists())
            Execute.Sql("""
                        CREATE TABLE "CloudCopies" (
                            "Id" INTEGER NOT NULL CONSTRAINT "PK_CloudCopies" PRIMARY KEY AUTOINCREMENT,
                            "BucketName" TEXT NOT NULL,
                            "CloudTransferBatchId" INTEGER NOT NULL,
                            "CopyCompletedSuccessfully" INTEGER NOT NULL,
                            "CreatedOn" TEXT NOT NULL,
                            "ErrorMessage" TEXT NOT NULL,
                            "ExistingCloudObjectKey" TEXT NOT NULL,
                            "FileSize" INTEGER NOT NULL,
                            "FileSystemFile" TEXT NOT NULL,
                            "LastUpdatedOn" TEXT NOT NULL,
                            "NewCloudObjectKey" TEXT NOT NULL,
                            CONSTRAINT "FK_CloudCopies_CloudTransferBatches_CloudTransferBatchId" FOREIGN KEY ("CloudTransferBatchId") REFERENCES "CloudTransferBatches" ("Id") ON DELETE CASCADE
                        )
                        """);
    }
}