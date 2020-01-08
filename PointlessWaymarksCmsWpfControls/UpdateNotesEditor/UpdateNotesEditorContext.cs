using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.ContentFormat;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.WpfHtml;

namespace PointlessWaymarksCmsWpfControls.UpdateNotesEditor
{
    public class UpdateNotesEditorContext : INotifyPropertyChanged
    {
        private IUpdateNotes _dbEntry;
        private ContentFormatChooserContext _updateNotesFormat;
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

        public StatusControlContext StatusContext { get; set; }

        public string UpdateNotes
        {
            get => _userUpdateNotes;
            set
            {
                if (value == _userUpdateNotes) return;
                _userUpdateNotes = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(UpdateUpdateNotesContentHtml);
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

        public async Task LoadData(IUpdateNotes toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;

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
        }

        public async Task UpdateUpdateNotesContentHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            try
            {
                var processResults = ContentProcessor.ContentHtml(UpdateNotesFormat.SelectedContentFormat, UpdateNotes);
                UpdateNotesHtmlOutput = processResults.ToHtmlDocument("Update Notes", string.Empty);
            }
            catch (Exception e)
            {
                UpdateNotesHtmlOutput = "<h2>Not able to process input</h2>".ToHtmlDocument("Invalid", string.Empty);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}