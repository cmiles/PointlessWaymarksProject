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

        private string _tags = string.Empty;
        private bool _tagsHaveChanges;

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

        public event PropertyChangedEventHandler PropertyChanged;

        public void CheckForChanges()
        {
            TagsHaveChanges = !TagList().SequenceEqual(DbTagList());
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
            if (!propertyName.Contains("HaveChanges")) CheckForChanges();
        }

        public List<string> TagList()
        {
            if (string.IsNullOrWhiteSpace(Tags)) return new List<string>();
            return Tags.Split(",").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).OrderBy(x => x)
                .ToList();
        }
    }
}