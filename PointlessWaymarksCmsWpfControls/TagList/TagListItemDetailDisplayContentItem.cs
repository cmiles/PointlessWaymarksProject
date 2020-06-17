using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsWpfControls.TagList
{
    public class TagListItemDetailDisplayContentItem : INotifyPropertyChanged
    {
        private object _content;
        private string _displayText;

        public object Content
        {
            get => _content;
            set
            {
                if (Equals(value, _content)) return;
                _content = value;
                OnPropertyChanged();
            }
        }

        public string DisplayText
        {
            get => _displayText;
            set
            {
                if (value == _displayText) return;
                _displayText = value;
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