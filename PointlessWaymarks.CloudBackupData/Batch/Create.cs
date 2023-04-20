using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CommonTools.S3;

namespace PointlessWaymarks.CloudBackupData.Batch;

public static class Create
{
    public static async Task InDatabase(IS3AccountInformation accountInformation, BackupJob job)
    {
        var changes = await CreationTools.GetChanges(accountInformation, job);
        await CreationTools.WriteChangesToDatabase(changes);
    }
}