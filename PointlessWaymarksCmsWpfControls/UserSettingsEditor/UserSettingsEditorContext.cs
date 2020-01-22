using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.UserSettingsEditor
{
    public class UserSettingsEditorContext : INotifyPropertyChanged
    {
        private UserSettings _editorSettings;
        private RelayCommand _saveSettingsCommand;
        private StatusControlContext _statusContext;

        public UserSettingsEditorContext(StatusControlContext statusContext, UserSettings toLoad)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(toLoad));
        }

        public UserSettings EditorSettings
        {
            get => _editorSettings;
            set
            {
                if (Equals(value, _editorSettings)) return;
                _editorSettings = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SaveSettingsCommand
        {
            get => _saveSettingsCommand;
            set
            {
                if (Equals(value, _saveSettingsCommand)) return;
                _saveSettingsCommand = value;
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

        private async Task LoadData(UserSettings toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            SaveSettingsCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveSettings));

            EditorSettings = toLoad;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task SaveSettings()
        {
            var currentFile = UserSettingsUtilities.SettingsFile();

            if (currentFile.Exists) currentFile.Delete();

            currentFile.Refresh();

            var writeStream = File.Create(currentFile.FullName);

            await JsonSerializer.SerializeAsync(writeStream, EditorSettings, null, CancellationToken.None);

            UserSettingsSingleton.CurrentSettings().InjectFrom(EditorSettings);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}