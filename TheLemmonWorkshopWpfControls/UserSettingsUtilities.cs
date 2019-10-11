using System;
using System.IO;

namespace TheLemmonWorkshopWpfControls
{
    public static class UserSettingsUtilities
    {
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

        public static FileInfo SettingsFile()
        {
            return new FileInfo(Path.Combine(StorageDirectory().FullName, "HikeLemmonWorkshopSettings.json"));
        }

        public static DirectoryInfo StorageDirectory()
        {
            var storageDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "HikeLemmon Workshop"));

            if (!storageDirectory.Exists) storageDirectory.Create();

            storageDirectory.Refresh();

            return storageDirectory;
        }

        public static void VerifyAndCreate()
        {
            ReadSettings();
        }

        public static void WriteSettings(UserSettings toWrite)
        {
            var currentFile = SettingsFile();
            File.WriteAllText(currentFile.FullName, System.Text.Json.JsonSerializer.Serialize(toWrite));
        }
    }
}