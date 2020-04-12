using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Ookii.Dialogs.Wpf;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsContentEditor
{
    public class SettingsFileChooserControlContext : INotifyPropertyChanged
    {
        private List<string> _recentFiles;
        private StatusControlContext _statusContext;
        private string _userNewFileName;

        public SettingsFileChooserControlContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            NewFileCommand = new Command(NewFile);
            ChooseFileCommand = new Command(ChooseFile);
        }

        public Command ChooseFileCommand { get; set; }

        public Command NewFileCommand { get; set; }

        public List<string> RecentFiles
        {
            get => _recentFiles;
            set
            {
                if (Equals(value, _recentFiles)) return;
                _recentFiles = value;
                OnPropertyChanged();
            }
        }

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

            SettingsFileUpdated?.Invoke(this, possibleFile.FullName);
        }

        private void NewFile()
        {
            if (string.IsNullOrWhiteSpace(UserNewFileName))
            {
                StatusContext.ToastError("Please fill in a file name...");
                return;
            }

            UserNewFileName = UserNewFileName.Trim();

            if (!UserNewFileName.EndsWith(".json")) UserNewFileName = $"{UserNewFileName}.json";

            if (!FolderFileUtility.IsValidFilename(UserNewFileName))
            {
                StatusContext.ToastError("File name is not valid - avoid special characters...");
                return;
            }

            SettingsFileUpdated?.Invoke(this, UserNewFileName);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler<string> SettingsFileUpdated;
    }
}