using System.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.TagExclusionEditor;

[NotifyPropertyChanged]
public partial class TagExclusionEditorListItem
{
    public TagExclusionEditorListItem()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public TagExclusion? DbEntry { get; set; }
    public bool HasChanges { get; set; }
    public string? TagValue { get; set; }

    public void CheckForChanges()
    {
        if (DbEntry == null || DbEntry.Id < 1)
        {
            if (string.IsNullOrWhiteSpace(TagValue)) HasChanges = false;
            HasChanges = true;
            return;
        }

        HasChanges = !StringTools.AreEqualWithTrim(TagValue, DbEntry.Tag);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges"))
            CheckForChanges();
    }
}