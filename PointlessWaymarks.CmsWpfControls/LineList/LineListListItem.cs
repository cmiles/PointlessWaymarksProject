using System.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentList;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.LineList;

[NotifyPropertyChanged]
public partial class LineListListItem : IContentListItem, IContentListSmallImage
{
    private protected LineListListItem(LineContentActions itemActions, LineContent dbEntry)
    {
        DbEntry = dbEntry;
        ItemActions = itemActions;

        PropertyChanged += OnPropertyChanged;
    }

    public LineContent DbEntry { get; set; }
    public LineContentActions ItemActions { get; set; }
    public double? RecordedOnLengthInMinutes { get; set; } = null;
    public CurrentSelectedTextTracker? SelectedTextTracker { get; set; } = new();
    public bool ShowType { get; set; }
    public string? SmallImageUrl { get; set; }

    public IContentCommon Content()
    {
        return DbEntry;
    }

    public Guid? ContentId()
    {
        return DbEntry.ContentId;
    }

    public string DefaultBracketCode()
    {
        return ItemActions.DefaultBracketCode(DbEntry);
    }

    public async Task DefaultBracketCodeToClipboard()
    {
        await ItemActions.DefaultBracketCodeToClipboard(DbEntry);
    }

    public async Task Delete()
    {
        await ItemActions.Delete(DbEntry);
    }

    public async Task Edit()
    {
        await ItemActions.Edit(DbEntry);
    }

    public async Task ExtractNewLinks()
    {
        await ItemActions.ExtractNewLinks(DbEntry);
    }

    public async Task GenerateHtml()
    {
        await ItemActions.GenerateHtml(DbEntry);
    }

    public async Task ViewHistory()
    {
        await ItemActions.ViewHistory(DbEntry);
    }

    public async Task ViewOnSite()
    {
        await ItemActions.ViewOnSite(DbEntry);
    }

    public static Task<LineListListItem> CreateInstance(LineContentActions itemActions)
    {
        return Task.FromResult(new LineListListItem(itemActions, LineContent.CreateInstance()));
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName == nameof(DbEntry)) UpdateRecordedOnLengthInMinutes();
    }

    private void UpdateRecordedOnLengthInMinutes()
    {
        RecordedOnLengthInMinutes = DbEntry is { RecordingStartedOn: null, RecordingEndedOn: null }
            ? null
            : (DbEntry.RecordingEndedOn - DbEntry.RecordingStartedOn)?.TotalMinutes;
    }
}