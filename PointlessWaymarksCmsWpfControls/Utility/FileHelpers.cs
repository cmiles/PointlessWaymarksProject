using System;
using System.IO;
using System.Threading.Tasks;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public static class FileHelpers
    {
        public static bool ImageFileTypeIsSupported(FileInfo toCheck)
        {
            if (toCheck == null) return false;
            if (!toCheck.Exists) return false;
            return toCheck.Extension.ToLower().Contains("jpg") || toCheck.Extension.ToLower().Contains("jpeg");
        }

        public static bool PhotoFileTypeIsSupported(FileInfo toCheck)
        {
            if (toCheck == null) return false;
            if (!toCheck.Exists) return false;
            return toCheck.Extension.ToLower().Contains("jpg") || toCheck.Extension.ToLower().Contains("jpeg");
        }

        public static async Task RenameSelectedFile(FileInfo selectedFile, StatusControlContext statusContext,
            Action<FileInfo> setSelectedFile)
        {
            if (selectedFile == null || !selectedFile.Exists)
            {
                statusContext.ToastWarning("No file to rename?");
                return;
            }

            var newName = await statusContext.ShowStringEntry("Rename File",
                $"Rename {Path.GetFileNameWithoutExtension(selectedFile.Name)} - " +
                "File Names must be limited to A-Z a-z 0-9 - . _  :",
                Path.GetFileNameWithoutExtension(selectedFile.Name));

            if (!newName.Item1) return;

            var cleanedName = newName.Item2.TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(cleanedName))
            {
                statusContext.ToastError("Can't rename the file to an empty string...");
                return;
            }

            var noExtensionCleaned = Path.GetFileNameWithoutExtension(cleanedName);

            if (string.IsNullOrWhiteSpace(noExtensionCleaned))
            {
                statusContext.ToastError("Not a valid filename...");
                return;
            }

            if (!FolderFileUtility.IsNoUrlEncodingNeeded(noExtensionCleaned))
            {
                statusContext.ToastError("File Names must be limited to A - Z a - z 0 - 9 - . _");
                return;
            }

            var moveToName = Path.Combine(selectedFile.Directory?.FullName ?? string.Empty,
                $"{noExtensionCleaned}{Path.GetExtension(selectedFile.Name)}");

            try
            {
                File.Copy(selectedFile.FullName, moveToName);
            }
            catch (Exception e)
            {
                await EventLogContext.TryWriteExceptionToLog(e, statusContext.StatusControlContextId.ToString(),
                    "Exception while trying to rename file.");
                statusContext.ToastError($"Error Copying File: {e.Message}");
                return;
            }

            var finalFile = new FileInfo(moveToName);

            if (!finalFile.Exists)
            {
                statusContext.ToastError("Unknown error renaming file - original file still selected.");
                return;
            }

            try
            {
                setSelectedFile(finalFile);
            }
            catch (Exception e)
            {
                statusContext.ToastError($"Error setting selected file - {e.Message}");
                return;
            }

            statusContext.ToastSuccess($"Selected file now {selectedFile.FullName}");
        }
    }
}