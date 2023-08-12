using System.ComponentModel;
using System.Diagnostics;
using PointlessWaymarks.FeedReaderData.Models;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
public partial class FeedListListItem
{
    public int UnreadItemsCount { get; set; }
    public int ItemsCount { get; set; }
    public required ReaderFeed DbReaderFeed { get; set; }
}