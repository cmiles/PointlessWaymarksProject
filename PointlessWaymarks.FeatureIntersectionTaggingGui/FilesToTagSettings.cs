﻿using System.IO;
using PointlessWaymarks.WpfCommon.FileList;

namespace PointlessWaymarks.FeatureIntersectionTaggingGui;

public class FeatureIntersectionFilesToTagSettings : IFileListSettings
{
    public async Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = (await FeatureIntersectionGuiSettingTools.ReadSettings()).FilesToTagLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async Task SetLastDirectory(string newDirectory)
    {
        var settings = await FeatureIntersectionGuiSettingTools.ReadSettings();
        settings.FilesToTagLastDirectoryFullName = newDirectory ?? string.Empty;
        await FeatureIntersectionGuiSettingTools.WriteSettings(settings);
    }
}