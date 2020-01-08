using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.HtmlViewer
{
    public class HtmlViewerContext : INotifyPropertyChanged
    {
        private string _htmlString;
        private StatusControlContext _statusContext;

        public HtmlViewerContext()
        {
            StatusContext = new StatusControlContext();
        }

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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}