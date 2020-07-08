using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsWpfControls.TagExclusionEditor
{
    public class TagExclusionEditorListItem : INotifyPropertyChanged
    {
        private TagExclusion _dbEntry;
        private bool _hasChanges;
        private string _tagValue;

        public TagExclusion DbEntry
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

        public string TagValue
        {
            get => _tagValue;
            set
            {
                if (value == _tagValue) return;
                _tagValue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void CheckForChanges()
        {
            if (DbEntry == null || DbEntry.Id < 1)
            {
                if (string.IsNullOrWhiteSpace(TagValue)) HasChanges = false;
                HasChanges = true;
                return;
            }

            HasChanges = !StringHelpers.AreEqualWithTrim(TagValue, DbEntry.Tag);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName != null && !propertyName.Contains("HasChanges")) CheckForChanges();
        }
    }
}