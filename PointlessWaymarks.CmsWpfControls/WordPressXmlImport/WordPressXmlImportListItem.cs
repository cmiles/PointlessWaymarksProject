#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarks.CmsWpfControls.WordPressXmlImport
{
    public class WordPressXmlImportListItem : INotifyPropertyChanged
    {
        private string _category = string.Empty;
        private string _content = string.Empty;
        private string _createdBy = string.Empty;
        private DateTime _createdOn = DateTime.Now;
        private string _slug = string.Empty;
        private string _summary = string.Empty;
        private string _tags = string.Empty;
        private string _title = string.Empty;
        private string _wordPressType = string.Empty;

        public string Category
        {
            get => _category;
            set
            {
                if (value == _category) return;
                _category = value;
                OnPropertyChanged();
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                if (value == _content) return;
                _content = value;
                OnPropertyChanged();
            }
        }

        public string CreatedBy
        {
            get => _createdBy;
            set
            {
                if (value == _createdBy) return;
                _createdBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedOn
        {
            get => _createdOn;
            set
            {
                if (value.Equals(_createdOn)) return;
                _createdOn = value;
                OnPropertyChanged();
            }
        }

        public string Slug
        {
            get => _slug;
            set
            {
                if (value == _slug) return;
                _slug = value;
                OnPropertyChanged();
            }
        }

        public string Summary
        {
            get => _summary;
            set
            {
                if (value == _summary) return;
                _summary = value;
                OnPropertyChanged();
            }
        }

        public string Tags
        {
            get => _tags;
            set
            {
                if (value == _tags) return;
                _tags = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public string WordPressType
        {
            get => _wordPressType;
            set
            {
                if (value == _wordPressType) return;
                _wordPressType = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}