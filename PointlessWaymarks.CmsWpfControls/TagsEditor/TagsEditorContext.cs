using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Html;
using PointlessWaymarks.CmsWpfControls.Status;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;

namespace PointlessWaymarks.CmsWpfControls.TagsEditor
{
    public class TagsEditorContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues,
        ICheckForChangesAndValidation
    {
        private ITag _dbEntry;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private string _helpText;
        private StatusControlContext _statusContext;
        private string _tags = string.Empty;
        private string _tagsValidationMessage;

        private TagsEditorContext(StatusControlContext statusContext, ITag dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            DbEntry = dbEntry;
            HelpText =
                "Comma separated tags - only a-z 0-9 _ - [space] are valid, each tag must be less than 200 characters long.";
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

        public bool HasValidationIssues
        {
            get => _hasValidationIssues;
            set
            {
                if (value == _hasValidationIssues) return;
                _hasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public string HelpText
        {
            get => _helpText;
            set
            {
                if (value == _helpText) return;
                _helpText = value;
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

        public void CheckForChangesAndValidationIssues()
        {
            Tags = SlugUtility.CreateRelaxedInputSpacedString(true, Tags, new List<char> {',', ' ', '-', '_'})
                .ToLower();

            HasChanges = !TagSlugList().SequenceEqual(DbTagList());

            var tagValidation = CommonContentValidation.ValidateTags(Tags);

            HasValidationIssues = !tagValidation.isValid;
            TagsValidationMessage = tagValidation.explanation;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static TagsEditorContext CreateInstance(StatusControlContext statusContext, ITag dbEntry)
        {
            return new(statusContext, dbEntry);
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

            if (!propertyName.Contains("HaveChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
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