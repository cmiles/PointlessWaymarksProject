using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ContentFormat;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.UpdateNotesEditor
{
    public class UpdateNotesEditorContext : INotifyPropertyChanged
    {
        private IUpdateNotes _dbEntry;
        private ContentFormatChooserContext _updateNotesFormat = new ContentFormatChooserContext();
        private string _userUpdateNotes = string.Empty;

        public UpdateNotesEditorContext(StatusControlContext statusControl)
        {
            StatusContext = statusControl;
        }

        public StatusControlContext StatusContext { get; set; }

        public ContentFormatChooserContext UpdateNotesFormat
        {
            get => _updateNotesFormat;
            set
            {
                if (Equals(value, _updateNotesFormat)) return;
                _updateNotesFormat = value;
                OnPropertyChanged();
            }
        }

        public string UpdateNotes
        {
            get => _userUpdateNotes;
            set
            {
                if (value == _userUpdateNotes) return;
                _userUpdateNotes = value;
                OnPropertyChanged();
            }
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

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadData(IUpdateNotes toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;

            if (toLoad == null)
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
    }
}