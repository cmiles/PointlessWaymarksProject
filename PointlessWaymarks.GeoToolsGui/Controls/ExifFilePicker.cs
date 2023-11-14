using System.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.GeoToolsGui.Controls;

public static class ExifFilePicker
{
    public static async Task<(bool validFileFound, string pickedFileName)> ChooseExifFile(
        StatusControlContext statusContext, string? exifToolFullName)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var startingDirectory = string.Empty;
        if (!string.IsNullOrWhiteSpace(exifToolFullName))
        {
            var currentExifFile = new FileInfo(exifToolFullName);
            var currentExifDirectory = currentExifFile.Directory;

            if (currentExifDirectory is not null)
            {
                if (currentExifDirectory.Exists)
                {
                    startingDirectory = currentExifDirectory.FullName;
                }
                else
                {
                    if (currentExifDirectory.Parent is { Exists: true })
                        startingDirectory = currentExifDirectory.Parent.FullName;
                }
            }
        }

        var filePicker = new VistaOpenFileDialog
        {
            Title = "Select ExifTool.exe",
            Filter = "ExifTool.exe|ExifTool.exe",
            Multiselect = false,
            CheckFileExists = true
        };

        if (!string.IsNullOrWhiteSpace(startingDirectory)) filePicker.FileName = $"{startingDirectory}\\";

        var result = filePicker.ShowDialog();

        if (!result ?? false) return (false, string.Empty);

        var possibleFile = filePicker.FileName;

        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(possibleFile))
        {
            statusContext.ToastWarning("No ExifTool.exe found...");
            return (false, string.Empty);
        }

        var possibleNewExifTool = new FileInfo(possibleFile);

        if (!possibleNewExifTool.Exists)
        {
            statusContext.ToastWarning($"Selected File {possibleNewExifTool} does not exist?");
            return (false, string.Empty);
        }

        return (true, possibleNewExifTool.FullName);
    }
}