﻿#region

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.GeoToolsGui.Models;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Settings;

[ObservableObject]
public partial class FeatureIntersectTaggerSettings
{
    [ObservableProperty] private bool _createBackups;
    [ObservableProperty] private bool _createBackupsInDefaultStorage;
    [ObservableProperty] private string _exifToolFullName = string.Empty;
    [ObservableProperty] private ObservableCollection<FeatureFileViewModel> _featureIntersectFiles = new();
    [ObservableProperty] private string _filesToTagLastDirectoryFullName = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _padUsAttributes = new();
    [ObservableProperty] private string _padUsDirectory = string.Empty;
    [ObservableProperty] private bool _sanitizeTags = true;
    [ObservableProperty] private bool _tagSpacesToHyphens;
    [ObservableProperty] private bool _tagsToLowerCase = true;
}