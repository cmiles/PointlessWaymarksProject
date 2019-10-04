using System;
using System.IO;
using System.Text.Json.Serialization;

namespace TheLemmonWorkshopWpfControls.Settings
{
    public static class SettingsUtility
    {
        public static void VerifyAndCreate()
        {
            ReadSettings();
        }

        public static DirectoryInfo StorageDirectory()
        {
            var storageDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "HikeLemmon Workshop"));

            if (!storageDirectory.Exists) storageDirectory.Create();

            storageDirectory.Refresh();

            return storageDirectory;
        }

        public static FileInfo SettingsFile()
        {
            return new FileInfo(Path.Combine(StorageDirectory().FullName, "HikeLemmonWorkshopSettings.json"));
        }

        public static void WriteSettings(UserSettings toWrite)
        {
            var currentFile = SettingsFile();
            File.WriteAllText(currentFile.FullName, System.Text.Json.JsonSerializer.Serialize(toWrite));
        }

        public static UserSettings ReadSettings()
        {
            var currentFile = SettingsFile();
            if (!currentFile.Exists)
            {
                WriteSettings(new UserSettings());
                currentFile.Refresh();
            }

            return System.Text.Json.JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(currentFile.FullName));
        }

    }

    public class UserSettings
    {
        public string BingApiKey { get; set; } = string.Empty;
        public string CalTopoApiKey { get; set; } = string.Empty;
        public string GoogleMapsApiKey { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "TheLemmonWorkshopDb";
    }
}