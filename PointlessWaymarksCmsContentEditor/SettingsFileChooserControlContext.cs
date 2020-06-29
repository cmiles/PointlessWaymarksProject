using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers;
using MvvmHelpers.Commands;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsContentEditor
{
    public class SettingsFileChooserControlContext : INotifyPropertyChanged
    {
        private ObservableRangeCollection<SettingsFileListItem> _items =
            new ObservableRangeCollection<SettingsFileListItem>();

        private StatusControlContext _statusContext;
        private string _userNewFileName;

        public SettingsFileChooserControlContext(StatusControlContext statusContext, string recentSettingFiles)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            RecentSettingFilesNames = recentSettingFiles?.Split("|").ToList() ?? new List<string>();

            NewFileCommand = new Command(NewFile);
            ChooseFileCommand = new Command(ChooseFile);
            ChooseRecentFileCommand = new Command<SettingsFileListItem>(LaunchRecentFile);

            StatusContext.RunFireAndForgetBlockingTaskWithUiMessageReturn(LoadData);
        }

        public Command ChooseFileCommand { get; set; }

        public Command<SettingsFileListItem> ChooseRecentFileCommand { get; set; }

        public ObservableRangeCollection<SettingsFileListItem> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public Command NewFileCommand { get; set; }

        public List<string> RecentSettingFilesNames { get; set; }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public string UserNewFileName
        {
            get => _userNewFileName;
            set
            {
                if (value == _userNewFileName) return;
                _userNewFileName = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ChooseFile()
        {
            var filePicker = new VistaOpenFileDialog {Filter = "json files (*.json)|*.json|All files (*.*)|*.*"};

            var result = filePicker.ShowDialog();

            if (!result ?? false) return;

            var possibleFile = new FileInfo(filePicker.FileName);

            if (!possibleFile.Exists)
            {
                StatusContext.ToastError("File doesn't exist?");
                return;
            }

            SettingsFileUpdated?.Invoke(this, (false, possibleFile.FullName));
        }

        private void LaunchRecentFile(SettingsFileListItem settingsFileListItem)
        {
            settingsFileListItem.SettingsFile.Refresh();

            if (!settingsFileListItem.SettingsFile.Exists)
            {
                StatusContext.ToastWarning("File doesn't appear to currently exist...");
                return;
            }

            SettingsFileUpdated?.Invoke(this, (false, settingsFileListItem.SettingsFile.FullName));
        }

        private async Task LoadData()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Looking for recent files");

            foreach (var loopFiles in RecentSettingFilesNames)
            {
                if (string.IsNullOrWhiteSpace(loopFiles)) continue;

                var loopFileInfo = new FileInfo(loopFiles);

                if (!loopFileInfo.Exists) continue;

                try
                {
                    StatusContext.Progress($"Recent Files - getting info from {loopFileInfo.FullName}");

                    var readResult =
                        await JsonSerializer.DeserializeAsync<UserSettings>(File.OpenRead(loopFileInfo.FullName));

                    await ThreadSwitcher.ResumeForegroundAsync();
                    Items.Add(new SettingsFileListItem {ParsedSettings = readResult, SettingsFile = loopFileInfo});
                    await ThreadSwitcher.ResumeBackgroundAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private void NewFile()
        {
            if (string.IsNullOrWhiteSpace(UserNewFileName))
            {
                StatusContext.ToastError("Please fill in a file name...");
                return;
            }

            UserNewFileName = UserNewFileName.Trim();

            if (!FolderFileUtility.IsValidWindowsFileSystemFilename(UserNewFileName))
            {
                StatusContext.ToastError("File name is not valid - avoid special characters...");
                return;
            }

            StatusContext.Progress($"New File Selected - {UserNewFileName}");

            SettingsFileUpdated?.Invoke(this, (true, UserNewFileName));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler<(bool isNew, string userString)> SettingsFileUpdated;

        public class SettingsFileListItem : INotifyPropertyChanged
        {
            private UserSettings _parsedSettings;
            private FileInfo _settingsFile;

            public UserSettings ParsedSettings
            {
                get => _parsedSettings;
                set
                {
                    if (Equals(value, _parsedSettings)) return;
                    _parsedSettings = value;
                    OnPropertyChanged1();
                }
            }

            public FileInfo SettingsFile
            {
                get => _settingsFile;
                set
                {
                    if (Equals(value, _settingsFile)) return;
                    _settingsFile = value;
                    OnPropertyChanged1();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged1([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}