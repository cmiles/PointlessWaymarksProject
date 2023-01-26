﻿#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport;

public partial class WordPressXmlImportListItem : ObservableObject, ISelectedTextTracker
{
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private string _content = string.Empty;
    [ObservableProperty] private string _createdBy = string.Empty;
    [ObservableProperty] private DateTime _createdOn = DateTime.Now;
    [ObservableProperty] private CurrentSelectedTextTracker _selectedTextTracker = new();
    [ObservableProperty] private string _slug = string.Empty;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private string _tags = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _wordPressType = string.Empty;
}