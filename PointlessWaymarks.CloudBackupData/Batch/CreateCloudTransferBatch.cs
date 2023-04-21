using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupData.Batch;

public static class CreateCloudTransferBatch
{
    public static async Task<CloudTransferBatch> InDatabase(IS3AccountInformation accountInformation, BackupJob job)
    {
        var changes = await CreationTools.GetChanges(accountInformation, job);
        return await CreationTools.WriteChangesToDatabase(changes);
    }
}