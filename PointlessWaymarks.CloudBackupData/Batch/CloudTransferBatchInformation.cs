using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CloudBackupData.Models;

namespace PointlessWaymarks.CloudBackupData.Batch;

public class CloudTransferBatchInformation
{
    public CloudTransferBatch Batch { get; set; }
    public List<CloudCopy> CloudCopies { get; set; }
    public List<CloudDelete> CloudDeletions { get; set; }
    public List<CloudFile> CloudFiles { get; set; }
    public List<CloudUpload> CloudUploads { get; set; }
    public List<FileSystemFile> FileSystemFiles { get; set; }
    
    public static async Task<CloudTransferBatchInformation> CreateInstance(int batchId)
    {
        var context = await CloudBackupContext.CreateInstance();
        
        return new CloudTransferBatchInformation
        {
            Batch = await context.CloudTransferBatches.AsNoTracking().FirstAsync(x => x.Id == batchId),
            FileSystemFiles = await context.FileSystemFiles.Where(x => x.CloudTransferBatchId == batchId).AsNoTracking()
                .ToListAsync(),
            CloudCopies = await context.CloudCopies.Where(x => x.CloudTransferBatchId == batchId).AsNoTracking()
                .ToListAsync(),
            CloudUploads = await context.CloudUploads.Where(x => x.CloudTransferBatchId == batchId).AsNoTracking()
                .ToListAsync(),
            CloudDeletions = await context.CloudDeletions.Where(x => x.CloudTransferBatchId == batchId).AsNoTracking()
                .ToListAsync(),
            CloudFiles = await context.CloudFiles.Where(x => x.CloudTransferBatchId == batchId).AsNoTracking()
                .ToListAsync()
        };
    }
}