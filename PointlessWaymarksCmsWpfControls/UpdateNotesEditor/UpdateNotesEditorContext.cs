using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace PointlessWaymarksCmsWpfControls.UpdateNotesEditor
{
    public class UpdateNotesEditorContext : INotifyPropertyChanged, IHasChanges
    {
        private IUpdateNotes _dbEntry;
        private bool _hasChanges;
        private Command _refreshPreviewCommand;
        private ContentFormatChooserContext _updateNotesFormat;
        private bool _updateNotesHasChanges;
        private string _updateNotesHtmlOutput;
        private string _userUpdateNotes = string.Empty;

        public UpdateNotesEditorContext(StatusControlContext statusContext, IUpdateNotes dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            UpdateNotesFormat = new ContentFormatChooserContext(statusContext);
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        public IUpdateNotes DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
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

        public StatusControlContext StatusContext { get; set; }

        public string UpdateNotes
        {
            get => _userUpdateNotes;
            set
            {
                if (value == _userUpdateNotes) return;
                _userUpdateNotes = value;
                OnPropertyChanged();

                UpdateNotesHasChanges = !StringHelpers.AreEqual(DbEntry.UpdateNotes, UpdateNotes);
            }
        }

        public ContentFormatChooserContext UpdateNotesFormat
        {
            get => _updateNotesFormat;
            set
            {
                if (Equals(value, _updateNotesFormat)) return;
                _updateNotesFormat = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(UpdateUpdateNotesContentHtml);
            }
        }

        public bool UpdateNotesHasChanges
        {
            get => _updateNotesHasChanges;
            set
            {
                if (value == _updateNotesHasChanges) return;
                _updateNotesHasChanges = value;
                OnPropertyChanged();
            }
        }

        public string UpdateNotesHtmlOutput
        {
            get => _updateNotesHtmlOutput;
            set
            {
                if (value == _updateNotesHtmlOutput) return;
                _updateNotesHtmlOutput = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChanges()
        {
            UpdateNotesHasChanges = !StringHelpers.AreEqual((DbEntry?.UpdateNotes).TrimNullToEmpty(), UpdateNotes);

            HasChanges = UpdateNotesHasChanges || UpdateNotesFormat.SelectedContentFormatHasChanges;
        }

        public async Task LoadData(IUpdateNotes toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;

            RefreshPreviewCommand = StatusContext.RunBlockingTaskCommand(UpdateUpdateNotesContentHtml);

            UpdateNotesFormat.InitialValue = DbEntry?.UpdateNotesFormat;

            if (toLoad == null || string.IsNullOrWhiteSpace(toLoad.UpdateNotesFormat))
            {
                UpdateNotes = string.Empty;
                UpdateNotesFormat.SelectedContentFormat = UpdateNotesFormat.ContentFormatChoices.First();
                return;
            }

            UpdateNotes = toLoad.UpdateNotes;
            var setUpdateFormatOk = await UpdateNotesFormat.TrySelectContentChoice(toLoad.UpdateNotesFormat);

            if (!setUpdateFormatOk) StatusContext.ToastWarning("Trouble loading Format from Db...");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChanges();
        }

        public async Task UpdateUpdateNotesContentHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            try
            {
                var preprocessResults =
                    BracketCodeCommon.ProcessCodesForLocalDisplay(UpdateNotes, StatusContext.ProgressTracker());
                var processResults =
                    ContentProcessing.ProcessContent(preprocessResults, UpdateNotesFormat.SelectedContentFormat);
                UpdateNotesHtmlOutput = processResults.ToHtmlDocument("Update Notes", string.Empty);
            }
            catch (Exception e)
            {
                UpdateNotesHtmlOutput =
                    $"<h2>Not able to process input</h2><p>{HttpUtility.HtmlEncode(e)}</p>".ToHtmlDocument("Invalid",
                        string.Empty);
            }
        }
    }
}