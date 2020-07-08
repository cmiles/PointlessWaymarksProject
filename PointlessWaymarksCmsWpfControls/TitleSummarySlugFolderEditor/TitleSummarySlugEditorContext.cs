using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor
{
    public class TitleSummarySlugEditorContext : INotifyPropertyChanged
    {
        private ITitleSummarySlugFolder _dbEntry;
        private string _folder = string.Empty;
        private bool _folderHasChanges;
        private string _slug = string.Empty;
        private bool _slugHasChanges;
        private StatusControlContext _statusContext;
        private string _summary = string.Empty;
        private bool _summaryHasChanges;
        private string _title = string.Empty;
        private bool _titleHasChanges;
        private Command _titleToSlugCommand;

        public TitleSummarySlugEditorContext(StatusControlContext statusContext, ITitleSummarySlugFolder dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        public ITitleSummarySlugFolder DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public string Folder
        {
            get => _folder;
            set
            {
                if (value == _folder) return;
                _folder = value;
                OnPropertyChanged();
            }
        }

        public bool FolderHasChanges
        {
            get => _folderHasChanges;
            set
            {
                if (value == _folderHasChanges) return;
                _folderHasChanges = value;
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

        public bool SlugHasChanges
        {
            get => _slugHasChanges;
            set
            {
                if (value == _slugHasChanges) return;
                _slugHasChanges = value;
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

        public bool SummaryHasChanges
        {
            get => _summaryHasChanges;
            set
            {
                if (value == _summaryHasChanges) return;
                _summaryHasChanges = value;
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

        public bool TitleHasChanges
        {
            get => _titleHasChanges;
            set
            {
                if (value == _titleHasChanges) return;
                _titleHasChanges = value;
                OnPropertyChanged();
            }
        }

        public Command TitleToSlugCommand
        {
            get => _titleToSlugCommand;
            set
            {
                if (Equals(value, _titleToSlugCommand)) return;
                _titleToSlugCommand = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChanges()
        {
            // ReSharper disable InvokeAsExtensionMethod - in this case TrimNullSage - which returns an
            //Empty string from null will not be invoked as an extension if DbEntry is null...
            SummaryHasChanges = StringHelpers.TrimNullSafe(DbEntry?.Summary) != Summary.TrimNullSafe();
            TitleHasChanges = StringHelpers.TrimNullSafe(DbEntry?.Title) != Title.TrimNullSafe();
            SlugHasChanges = StringHelpers.TrimNullSafe(DbEntry?.Slug) != Slug.TrimNullSafe();
            FolderHasChanges = StringHelpers.TrimNullSafe(DbEntry?.Folder) != Folder.TrimNullSafe();
            // ReSharper restore InvokeAsExtensionMethod
        }

        public async Task LoadData(ITitleSummarySlugFolder dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            TitleToSlugCommand = new Command(() => StatusContext.RunBlockingAction(TitleToSlug));

            DbEntry = dbEntry;

            if (DbEntry == null)
            {
                Summary = string.Empty;
                Title = string.Empty;
                Slug = string.Empty;
                Folder = string.Empty;

                return;
            }

            Summary = DbEntry.Summary ?? string.Empty;
            Title = DbEntry.Title ?? string.Empty;
            Slug = DbEntry.Slug ?? string.Empty;
            Folder = DbEntry.Folder ?? string.Empty;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (!propertyName.Contains("HasChanges")) CheckForChanges();
        }

        public void TitleToSlug()
        {
            Slug = SlugUtility.Create(true, Title);
        }

        public async Task<(bool valid, string explanation)> Validate()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var isValid = true;
            var errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Title))
            {
                isValid = false;
                errorMessage += "Title can not be blank.";
            }

            if (string.IsNullOrWhiteSpace(Summary))
            {
                isValid = false;
                errorMessage += "Summary can not be blank.";
            }

            if (string.IsNullOrWhiteSpace(Slug))
            {
                isValid = false;
                errorMessage += "Slug can not be blank.";
            }

            if (string.IsNullOrWhiteSpace(Folder))
            {
                isValid = false;
                errorMessage += "Folder can not be blank.";
            }

            if (!isValid) return (isValid, errorMessage);

            if (!FolderFileUtility.IsNoUrlEncodingNeededFilename(Folder))
            {
                isValid = false;
                errorMessage += "Folders have illegal characters...";
            }

            if (!isValid) return (isValid, errorMessage);

            return (isValid, errorMessage);
        }
    }
}