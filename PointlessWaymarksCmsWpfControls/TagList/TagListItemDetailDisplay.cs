using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsWpfControls.TagList
{
    public class TagListItemDetailDisplay : INotifyPropertyChanged
    {
        private List<TagListItemDetailDisplayContentItem> _contentList;
        private TagListListItem _listItem;
        private string _userNewTagName;

        public List<TagListItemDetailDisplayContentItem> ContentList
        {
            get => _contentList;
            set
            {
                if (Equals(value, _contentList)) return;
                _contentList = value;
                OnPropertyChanged();
            }
        }

        public TagListListItem ListItem
        {
            get => _listItem;
            set
            {
                if (Equals(value, _listItem)) return;
                _listItem = value;
                OnPropertyChanged();
            }
        }

        public string UserNewTagName
        {
            get => _userNewTagName;
            set
            {
                if (value == _userNewTagName) return;
                _userNewTagName = value;
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