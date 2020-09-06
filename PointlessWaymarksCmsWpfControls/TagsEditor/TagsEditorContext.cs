using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.TagsEditor
{
    public class TagsEditorContext : INotifyPropertyChanged, IHasChanges
    {
        public static TagsEditorContext CreateInstance(StatusControlContext statusContext, ITag dbEntry)
        {
            return new TagsEditorContext(statusContext, dbEntry);
        }

        private ITag _dbEntry;
        private bool _hasChanges;
        private StatusControlContext _statusContext;

        private string _tags = string.Empty;
        private bool _tagsHaveChanges;
        private bool _tagsHaveValidationIssues;
        private bool _tagsHaveWarnings;
        private string _tagsValidationMessage;
        private string _tagsWarningMessage;

        private TagsEditorContext(StatusControlContext statusContext, ITag dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            DbEntry = dbEntry;
            Tags = dbEntry?.Tags ?? string.Empty;
            Tags = TagListString();
        }

        public ITag DbEntry
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

        public bool TagsHaveChanges
        {
            get => _tagsHaveChanges;
            set
            {
                if (value == _tagsHaveChanges) return;
                _tagsHaveChanges = value;
                OnPropertyChanged();
            }
        }

        public bool TagsHaveValidationIssues
        {
            get => _tagsHaveValidationIssues;
            set
            {
                if (value == _tagsHaveValidationIssues) return;
                _tagsHaveValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public bool TagsHaveWarnings
        {
            get => _tagsHaveWarnings;
            set
            {
                if (value == _tagsHaveWarnings) return;
                _tagsHaveWarnings = value;
                OnPropertyChanged();
            }
        }

        public string TagsValidationMessage
        {
            get => _tagsValidationMessage;
            set
            {
                if (value == _tagsValidationMessage) return;
                _tagsValidationMessage = value;
                OnPropertyChanged();
            }
        }

        public string TagsWarningMessage
        {
            get => _tagsWarningMessage;
            set
            {
                if (value == _tagsWarningMessage) return;
                _tagsWarningMessage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void CheckForChanges()
        {
            TagsHaveChanges = !TagSlugList().SequenceEqual(DbTagList());

            if (string.IsNullOrWhiteSpace(Tags))
            {
                TagsHaveWarnings = true;
                TagsWarningMessage = "Tags are not required but are very helpful!";
            }
            else
            {
                TagsHaveWarnings = false;
                TagsWarningMessage = string.Empty;
            }

            var tagValidation = CommonContentValidation.ValidateTags(Tags);

            TagsHaveValidationIssues = !tagValidation.isValid;
            TagsValidationMessage = tagValidation.explanation;

            HasChanges = TagsHaveChanges;
        }

        private List<string> DbTagList()
        {
            return string.IsNullOrWhiteSpace(DbEntry?.Tags)
                ? new List<string>()
                : Db.TagListParseToSlugs(DbEntry, false);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HaveChanges") && !propertyName.Contains("Validation")) CheckForChanges();
        }

        public List<string> TagList()
        {
            return string.IsNullOrWhiteSpace(Tags) ? new List<string>() : Db.TagListParse(Tags);
        }

        public string TagListString()
        {
            return string.IsNullOrWhiteSpace(Tags) ? string.Empty : Db.TagListJoin(TagList());
        }

        public List<string> TagSlugList()
        {
            return string.IsNullOrWhiteSpace(Tags) ? new List<string>() : Db.TagListParseToSlugs(Tags, false);
        }
    }
}