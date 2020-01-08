using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsWpfControls.ContentList
{
    public class ContentListItem : INotifyPropertyChanged
    {
        private string _contentType;
        private ITitleSummarySlugFolder _summaryInfo;

        public string ContentType
        {
            get => _contentType;
            set
            {
                if (value == _contentType) return;
                _contentType = value;
                OnPropertyChanged();
            }
        }

        public ITitleSummarySlugFolder SummaryInfo
        {
            get => _summaryInfo;
            set
            {
                if (Equals(value, _summaryInfo)) return;
                _summaryInfo = value;
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