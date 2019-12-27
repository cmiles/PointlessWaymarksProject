using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ControlStatus;

namespace TheLemmonWorkshopWpfControls.MainImageEditor
{
    public class MainImageEditorContext : INotifyPropertyChanged
    {
        private IMainImage _dbEntry;

        public MainImageEditorContext(StatusControlContext statusContext, IMainImage dbEntry)
        {
            DbEntry = dbEntry;
        }

        public IMainImage DbEntry
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}