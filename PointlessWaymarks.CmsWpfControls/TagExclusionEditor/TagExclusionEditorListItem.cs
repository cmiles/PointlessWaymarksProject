using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsWpfControls.TagExclusionEditor;

public partial class TagExclusionEditorListItem : ObservableObject
{
    [ObservableProperty] private TagExclusion _dbEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private string _tagValue;

    public TagExclusionEditorListItem()
    {
        PropertyChanged += OnPropertyChanged;
    }

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
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges"))
            CheckForChanges();
    }
}