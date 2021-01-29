using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CmsWpfControls.WpfHtml;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.UpdateNotesEditor
{
    public class UpdateNotesEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private IUpdateNotes _dbEntry;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private Command _refreshPreviewCommand;
        private ContentFormatChooserContext _updateNotesFormat;
        private bool _updateNotesHasChanges;
        private string _updateNotesHtmlOutput;
        private string _userUpdateNotes = string.Empty;

        private UpdateNotesEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
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

        public void CheckForChangesAndValidationIssues()
        {
            UpdateNotesHasChanges = !StringHelpers.AreEqual((DbEntry?.UpdateNotes).TrimNullToEmpty(), UpdateNotes);

            HasChanges = UpdateNotesHasChanges || PropertyScanners.ChildPropertiesHaveChanges(this);
            HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
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

        public bool HasValidationIssues
        {
            get => _hasValidationIssues;
            set
            {
                if (value == _hasValidationIssues) return;
                _hasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<UpdateNotesEditorContext> CreateInstance(StatusControlContext statusContext,
            IUpdateNotes dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newContext = new UpdateNotesEditorContext(statusContext);
            newContext.UpdateNotesFormat = ContentFormatChooserContext.CreateInstance(newContext.StatusContext);

            await newContext.LoadData(dbEntry);

            return newContext;
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

            PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
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