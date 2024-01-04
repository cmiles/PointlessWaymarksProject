using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.ContentMap;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LineList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LineMonthlySummaryWindow
{
    public LineMonthlySummaryWindow(List<LineMonthlyStatRow> statRows)

    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        Items = new ObservableCollection<LineMonthlyStatRow>(statRows);

        BuildCommands();

        DataContext = this;
    }

    public ObservableCollection<LineMonthlyStatRow> Items { get; set; }

    public LineMonthlyStatRow? SelectedItem { get; set; }

    public List<LineMonthlyStatRow> SelectedItems { get; set; } = new();

    public StatusControlContext StatusContext { get; set; }

    [BlockingCommand]
    public async Task ContentMap(LineMonthlyStatRow? row)
    {
        if (row == null)
        {
            StatusContext.ToastError("No Row Selected?");
            return;
        }

        var allGuids = SelectedItems.SelectMany(x => x.LineContentIds).ToList();

        var mapWindow =
            await ContentMapWindow.CreateInstance(new ContentMapListLoader("Mapped Content", allGuids));

        await mapWindow.PositionWindowAndShowOnUiThread();
    }

    [BlockingCommand]
    [StopAndWarnIfNoSelectedListItems]
    public async Task ContentMapForAllSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var allGuids = SelectedItems.SelectMany(x => x.LineContentIds).ToList();

        var mapWindow =
            await ContentMapWindow.CreateInstance(new ContentMapListLoader("Mapped Content", allGuids));

        await mapWindow.PositionWindowAndShowOnUiThread();
    }

    public static async Task<LineMonthlySummaryWindow> CreateInstance(List<Guid> lineContentIds)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var db = await Db.Context();

        var lines = await db.LineContents
            .Where(x => lineContentIds.Contains(x.ContentId) && x.RecordingStartedOn != null &&
                        x.RecordingEndedOn != null && x.RecordingStartedOn < x.RecordingEndedOn).AsNoTracking()
            .ToListAsync();

        var grouped = lines.GroupBy(x =>
                new { x.RecordingStartedOn.Value.Year, x.RecordingStartedOn.Value.Month })
            .OrderByDescending(x => x.Key.Year).ThenByDescending(x => x.Key.Month);

        var reportRows = grouped.Select(x => new LineMonthlyStatRow
        {
            Year = x.Key.Year,
            Month = x.Key.Month,
            Activities = x.Count(),
            Miles = (int)Math.Floor(x.Sum(y => y.LineDistance)),
            Hours = (int)Math.Floor(new TimeSpan(0, (int)x
                    .Where(y => y is { RecordingStartedOn: not null, RecordingEndedOn: not null } &&
                                y.RecordingStartedOn < y.RecordingEndedOn)
                    .Select(y => y.RecordingEndedOn.Value - y.RecordingStartedOn.Value).Sum(y => y.TotalMinutes), 0)
                .TotalHours),
            MinElevation = (int)Math.Floor(x.Min(y => y.MinimumElevation)),
            MaxElevation = (int)Math.Floor(x.Max(y => y.MaximumElevation)),
            Climb = (int)Math.Floor(x.Sum(y => y.ClimbElevation)),
            Descent = (int)Math.Floor(x.Sum(y => y.DescentElevation)),
            LineContentIds = x.Select(x => x.ContentId).ToList()
        }).ToList();

        await ThreadSwitcher.ResumeForegroundAsync();

        return new LineMonthlySummaryWindow(reportRows);
    }

    public LineMonthlyStatRow? SelectedListItem()
    {
        return SelectedItem;
    }

    public List<LineMonthlyStatRow> SelectedListItems()
    {
        return SelectedItems;
    }

    private void Selector_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext == null) return;
        var viewmodel = (LineMonthlySummaryWindow)DataContext;
        SelectedItems =
            LineStatsDataGrid?.SelectedItems.Cast<LineMonthlyStatRow>().ToList() ??
            new List<LineMonthlyStatRow>();
    }
}