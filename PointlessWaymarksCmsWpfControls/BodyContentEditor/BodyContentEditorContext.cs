using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html.CommonHtml;
using PointlessWaymarksCmsWpfControls.ContentFormat;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.WpfHtml;

namespace PointlessWaymarksCmsWpfControls.BodyContentEditor
{
    public class BodyContentEditorContext : INotifyPropertyChanged
    {
        private ContentFormatChooserContext _bodyContentFormat;
        private bool _bodyContentHasChanges;
        private string _bodyContentHtmlOutput;
        private IBodyContent _dbEntry;
        private Command _refreshPreviewCommand;
        private string _selectedBodyText;
        private StatusControlContext _statusContext;
        private string _userBodyContent = string.Empty;

        public BodyContentEditorContext(StatusControlContext statusContext, IBodyContent dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            BodyContentFormat = new ContentFormatChooserContext(StatusContext);

            RemoveLineBreaksFromSelectedCommand = StatusContext.RunBlockingActionCommand(RemoveLineBreaksFromSelected);

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        public string BodyContent
        {
            get => _userBodyContent;
            set
            {
                if (value == _userBodyContent) return;
                _userBodyContent = value;
                OnPropertyChanged();

                BodyContentHasChanges = !StringHelpers.AreEqual(DbEntry.BodyContent, BodyContent);
            }
        }

        public ContentFormatChooserContext BodyContentFormat
        {
            get => _bodyContentFormat;
            set
            {
                if (Equals(value, _bodyContentFormat)) return;
                _bodyContentFormat = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(UpdateContentHtml);
            }
        }

        public bool BodyContentHasChanges
        {
            get => _bodyContentHasChanges;
            set
            {
                if (value == _bodyContentHasChanges) return;
                _bodyContentHasChanges = value;
                OnPropertyChanged();
            }
        }

        public string BodyContentHtmlOutput
        {
            get => _bodyContentHtmlOutput;
            set
            {
                if (value == _bodyContentHtmlOutput) return;
                _bodyContentHtmlOutput = value;
                OnPropertyChanged();
            }
        }

        public IBodyContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public Command RefreshPreviewCommand
        {
            get => _refreshPreviewCommand;
            set
            {
                if (Equals(value, _refreshPreviewCommand)) return;
                _refreshPreviewCommand = value;
                OnPropertyChanged();
            }
        }

        public Command RemoveLineBreaksFromSelectedCommand { get; set; }

        public string SelectedBodyText
        {
            get => _selectedBodyText;
            set
            {
                if (value == _selectedBodyText) return;
                _selectedBodyText = value;
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

        public async Task LoadData(IBodyContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;

            RefreshPreviewCommand = StatusContext.RunBlockingTaskCommand(UpdateContentHtml);

            BodyContentFormat.InitialValue = toLoad?.BodyContentFormat;

            if (toLoad == null)
            {
                BodyContent = string.Empty;
                BodyContentFormat.SelectedContentFormat = BodyContentFormat.ContentFormatChoices.First();
                return;
            }

            BodyContent = toLoad.BodyContent;

            var setUpdateFormatOk = await BodyContentFormat.TrySelectContentChoice(toLoad.BodyContentFormat);

            if (!setUpdateFormatOk) StatusContext.ToastWarning("Trouble loading Format from Db...");

            SelectedBodyText = string.Empty;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RemoveLineBreaksFromSelected()
        {
            if (string.IsNullOrWhiteSpace(SelectedBodyText)) return;
            SelectedBodyText = Regex.Replace(SelectedBodyText, @"\r\n?|\n", " ");
        }

        public async Task UpdateContentHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            StatusContext.Progress("Building HTML");

            var settings = UserSettingsSingleton.CurrentSettings();

            try
            {
                var preprocessResults =
                    BracketCodeCommon.ProcessCodesForLocalDisplay(BodyContent, StatusContext.ProgressTracker());
                var processResults =
                    ContentProcessing.ProcessContent(preprocessResults, BodyContentFormat.SelectedContentFormat);

                var possibleStyleFile =
                    new FileInfo(Path.Combine(settings.LocalSiteDirectory().FullName, "styles.css"));

                var styleBlock = "body { margin-right: 20px; }" + Environment.NewLine;

                if (possibleStyleFile.Exists) styleBlock += await File.ReadAllTextAsync(possibleStyleFile.FullName);

                BodyContentHtmlOutput = processResults.ToHtmlDocument("Body Content", styleBlock);
            }
            catch (Exception e)
            {
                BodyContentHtmlOutput =
                    $"<h2>Not able to process input</h2><p>{HttpUtility.HtmlEncode(e)}</p>".ToHtmlDocument("Invalid",
                        string.Empty);
            }
        }
    }
}