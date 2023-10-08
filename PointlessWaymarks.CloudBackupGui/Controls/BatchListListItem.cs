using PointlessWaymarks.CloudBackupData.Models;
using PointlessWaymarks.CloudBackupData.Reports;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CloudBackupGui.Controls;

[NotifyPropertyChanged]
public partial class BatchListListItem
{
    public required BatchStatistics Statistics { get; set; }

    public static async Task<BatchListListItem> CreateInstance(CloudTransferBatch batch)
    {
        var newItem = new BatchListListItem { Statistics = await BatchStatistics.CreateInstance(batch.Id) };

        return newItem;
    }
}