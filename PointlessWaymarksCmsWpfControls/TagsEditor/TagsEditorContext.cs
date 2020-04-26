using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.TagsEditor
{
    public class TagsEditorContext : INotifyPropertyChanged
    {
        private ITag _dbEntry;
        private StatusControlContext _statusContext;

        private string _tags;
        private bool _tagsHasChanges;

        public TagsEditorContext(StatusControlContext statusContext, ITag dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            DbEntry = dbEntry;
            Tags = dbEntry?.Tags ?? string.Empty;
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

        public bool TagsHasChanges
        {
            get => _tagsHasChanges;
            set
            {
                if (value == _tagsHasChanges) return;
                _tagsHasChanges = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void CheckForChanges()
        {
            TagsHasChanges = !TagList().SequenceEqual(DbTagList());
        }

        private List<string> DbTagList()
        {
            if (string.IsNullOrWhiteSpace(DbEntry?.Tags)) return new List<string>();
            return DbEntry.Tags.Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
                .OrderBy(x => x).ToList();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            CheckForChanges();
        }

        public List<string> TagList()
        {
            if (string.IsNullOrWhiteSpace(Tags)) return new List<string>();
            return Tags.Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).OrderBy(x => x)
                .ToList();
        }
    }
}