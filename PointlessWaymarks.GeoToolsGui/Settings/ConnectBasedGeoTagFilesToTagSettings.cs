﻿using System.IO;
using PointlessWaymarks.GeoToolsGui.Controls;
using PointlessWaymarks.WpfCommon.FileList;

namespace PointlessWaymarks.GeoToolsGui.Settings;

public class ConnectBasedGeoTagFilesToTagSettings : IFileListSettings
{
    private readonly ConnectBasedGeoTaggerContext _context;

    public ConnectBasedGeoTagFilesToTagSettings(ConnectBasedGeoTaggerContext context)
    {
        _context = context;
    }

    public async Task<DirectoryInfo?> GetLastDirectory()
    {
        var lastDirectory = _context.Settings.FilesToTagLastDirectoryFullName;

        if (string.IsNullOrWhiteSpace(lastDirectory)) return null;

        var returnDirectory = new DirectoryInfo(lastDirectory);

        if (!returnDirectory.Exists) return null;

        return returnDirectory;
    }

    public async Task SetLastDirectory(string newDirectory)
    {
        _context.Settings.FilesToTagLastDirectoryFullName = newDirectory ?? string.Empty;
    }
}