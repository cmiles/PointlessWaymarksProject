﻿using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;

namespace PointlessWaymarks.CmsGui;

[ObservableObject]
public partial class SettingsFileListItem
{
    [ObservableProperty] private UserSettings _parsedSettings;
    [ObservableProperty] private FileInfo _settingsFile;
}