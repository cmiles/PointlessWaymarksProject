using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using TheLemmonWorkshopWpfControls.ControlStatus;

namespace TheLemmonWorkshopWpfControls.HtmlViewer
{
    public class HtmlViewerContext : INotifyPropertyChanged
    {
        public HtmlViewerContext()
        {
            StatusContext = new StatusControlContext();
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

        private string _htmlString;
        private StatusControlContext _statusContext;

        public string HtmlString
        {
            get => _htmlString;
            set
            {
                if (value == _htmlString) return;
                _htmlString = value;
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