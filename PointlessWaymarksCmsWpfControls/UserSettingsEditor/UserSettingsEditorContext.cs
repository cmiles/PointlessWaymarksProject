using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.UserSettingsEditor
{
    public class UserSettingsEditorContext : INotifyPropertyChanged
    {
        private UserSettings _editorSettings;
        private Command _saveSettingsCommand;
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

        public string PdfToCairoHelpText =>
            "pdftocairo transforms Pdf files to other formats including jpg files - this program " +
            "can be use it to automatically generate an image of the first/cover page of a pdf, very " +
            "useful when adding PDFs to the File Content. However pdftocariro is not included with this " +
            "program... On Windows the easiest way to get pdftocairo is to install [MiKTeX](https://miktex.org/download). " +
            "Once installed the setting above should be the folder where pdftocairo.exe is located - " +
            "for example C:\\MiKTeX 2.9\\miktex\\bin";

        public Command SaveSettingsCommand
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

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task LoadData(UserSettings toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            SaveSettingsCommand = StatusContext.RunBlockingTaskCommand(SaveSettings);

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
    }
}