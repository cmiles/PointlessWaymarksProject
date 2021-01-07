using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.Status;
using PointlessWaymarks.CmsWpfControls.Utility.Aws;
using PointlessWaymarks.CmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.UserSettingsEditor
{
    public class UserSettingsEditorContext : INotifyPropertyChanged
    {
        private Command _deleteAwsCredentials;
        private UserSettings _editorSettings;
        private Command _enterAwsCredentials;
        private List<string> _regionChoices;
        private Command _saveSettingsCommand;
        private StatusControlContext _statusContext;

        public UserSettingsEditorContext(StatusControlContext statusContext, UserSettings toLoad)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(toLoad));
        }

        public Command DeleteAwsCredentials
        {
            get => _deleteAwsCredentials;
            set
            {
                if (Equals(value, _deleteAwsCredentials)) return;
                _deleteAwsCredentials = value;
                OnPropertyChanged();
            }
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

        public Command EnterAwsCredentials
        {
            get => _enterAwsCredentials;
            set
            {
                if (Equals(value, _enterAwsCredentials)) return;
                _enterAwsCredentials = value;
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

        public List<string> RegionChoices
        {
            get => _regionChoices;
            set
            {
                if (Equals(value, _regionChoices)) return;
                _regionChoices = value;
                OnPropertyChanged();
            }
        }

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

            RegionChoices = RegionEndpoint.EnumerableAllRegions.Select(x => x.SystemName).ToList();

            SaveSettingsCommand = StatusContext.RunBlockingTaskCommand(SaveSettings);
            EnterAwsCredentials = StatusContext.RunBlockingTaskCommand(UserAwsKeyAndSecretEntry);
            DeleteAwsCredentials = StatusContext.RunBlockingActionCommand(AwsCredentials.RemoveAwsSiteCredentials);

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

        public async Task UserAwsKeyAndSecretEntry()
        {
            var newKeyEntry = await StatusContext.ShowStringEntry("AWS Access Key",
                "Enter the AWS Access Key", string.Empty);

            if (!newKeyEntry.Item1)
            {
                StatusContext.ToastWarning("AWS Credential Entry Canceled");
                return;
            }

            var cleanedKey = newKeyEntry.Item2.TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(cleanedKey))
            {
                StatusContext.ToastError("AWS Credential Entry Canceled - key can not be blank");
                return;
            }

            var newSecretEntry = await StatusContext.ShowStringEntry("AWS Secret Access Key",
                "Enter the AWS Secret Access Key", string.Empty);

            if (!newSecretEntry.Item1)
            {
                StatusContext.ToastWarning("AWS Credential Entry Canceled");
                return;
            }

            var cleanedSecret = newSecretEntry.Item2.TrimNullToEmpty();

            if (string.IsNullOrWhiteSpace(cleanedSecret))
            {
                StatusContext.ToastError("AWS Credential Entry Canceled - secret can not be blank");
                return;
            }

            AwsCredentials.SaveAwsSiteCredential(cleanedKey, cleanedSecret);
        }
    }
}