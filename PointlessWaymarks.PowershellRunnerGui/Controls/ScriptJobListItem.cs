using System.Collections.ObjectModel;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptJobListItem
{
    public required ScriptJob DbEntry { get; set; }
    public required ObservableCollection<ScriptProgressListItem> Items { get; set; }
    public ScriptProgressListItem? SelectedItem { get; set; }
    public List<ScriptProgressListItem> SelectedItems { get; set; } = [];

    public static async Task<ScriptJobListItem> CreateInstance(ScriptJob dbEntry)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new ScriptJobListItem
        {
            DbEntry = dbEntry,
            Items = new ObservableCollection<ScriptProgressListItem>()
        };
    }

    //TODO: Setup Data Notifications for progress
}