using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor
{
    public class TitleSummarySlugEditorContext : INotifyPropertyChanged
    {
        private ITitleSummarySlugFolder _dbEntry;
        private ObservableCollection<string> _existingFolderChoices;
        private string _folder = string.Empty;
        private bool _folderHasChanges;
        private bool _folderHasValidationIssues;
        private string _folderValidationMessage;
        private bool _hasChanges;
        private DirectoryInfo _parentDirectory;
        private string _slug = string.Empty;
        private bool _slugHasChanges;
        private bool _slugHasValidationIssues;
        private string _slugValidationMessage;
        private StatusControlContext _statusContext;
        private string _summary = string.Empty;
        private bool _summaryHasChanges;
        private bool _summaryHasValidationIssues;
        private string _summaryValidationMessage;
        private string _title = string.Empty;
        private bool _titleHasChanges;
        private bool _titleHasValidationIssues;
        private Command _titleToSlugCommand;
        private string _titleValidationMessage;

        public TitleSummarySlugEditorContext(StatusControlContext statusContext, ITitleSummarySlugFolder dbEntry,
            DirectoryInfo parentDirectory)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            ParentDirectory = parentDirectory;

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

        public bool FolderHasValidationIssues
        {
            get => _folderHasValidationIssues;
            set
            {
                if (value == _folderHasValidationIssues) return;
                _folderHasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public string FolderValidationMessage
        {
            get => _folderValidationMessage;
            set
            {
                if (value == _folderValidationMessage) return;
                _folderValidationMessage = value;
                OnPropertyChanged();
            }
        }

        public DirectoryInfo ParentDirectory
        {
            get => _parentDirectory;
            set
            {
                if (Equals(value, _parentDirectory)) return;
                _parentDirectory = value;
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

        public bool SlugHasValidationIssues
        {
            get => _slugHasValidationIssues;
            set
            {
                if (value == _slugHasValidationIssues) return;
                _slugHasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public string SlugValidationMessage
        {
            get => _slugValidationMessage;
            set
            {
                if (value == _slugValidationMessage) return;
                _slugValidationMessage = value;
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

        public bool SummaryHasValidationIssues
        {
            get => _summaryHasValidationIssues;
            set
            {
                if (value == _summaryHasValidationIssues) return;
                _summaryHasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public string SummaryValidationMessage
        {
            get => _summaryValidationMessage;
            set
            {
                if (value == _summaryValidationMessage) return;
                _summaryValidationMessage = value;
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

        public bool TitleHasValidationIssues
        {
            get => _titleHasValidationIssues;
            set
            {
                if (value == _titleHasValidationIssues) return;
                _titleHasValidationIssues = value;
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

        public string TitleValidationMessage
        {
            get => _titleValidationMessage;
            set
            {
                if (value == _titleValidationMessage) return;
                _titleValidationMessage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChangesAndValidate()
        {
            // ReSharper disable InvokeAsExtensionMethod - in this case TrimNullToEmpty - which returns an
            //Empty string from null will not be invoked as an extension if DbEntry is null...
            SummaryHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Summary) != Summary.TrimNullToEmpty();
            TitleHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Title) != Title.TrimNullToEmpty();
            SlugHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Slug) != Slug.TrimNullToEmpty();
            FolderHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Folder) != Folder.TrimNullToEmpty();
            // ReSharper restore InvokeAsExtensionMethod

            HasChanges = SummaryHasChanges || TitleHasChanges || SlugHasChanges || FolderHasChanges;

            ValidateTitle();
            ValidateSummary();
            ValidateSlug();
            ValidateFolder();
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

        public void ValidateSummary()
        {
            var validationResult = CommonContentValidation.ValidateSummary(Summary);

            SummaryHasValidationIssues = !validationResult.isValid;
            SummaryValidationMessage = validationResult.explanation;
        }

        public async Task LoadData(ITitleSummarySlugFolder dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            TitleToSlugCommand = StatusContext.RunBlockingActionCommand(TitleToSlug);

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

            await ThreadSwitcher.ResumeForegroundAsync();

            if (ParentDirectory != null && ParentDirectory.Exists)
            {
                var existingSubDirectories = ParentDirectory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                    .OrderBy(x => x.Name).Select(x => x.Name);

                ExistingFolderChoices = new ObservableCollection<string>(existingSubDirectories);
            }
            else
            {
                ExistingFolderChoices = new ObservableCollection<string>();
            }
        }

        public ObservableCollection<string> ExistingFolderChoices
        {
            get => _existingFolderChoices;
            set
            {
                if (Equals(value, _existingFolderChoices)) return;
                _existingFolderChoices = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidate();
        }

        public void TitleToSlug()
        {
            Slug = SlugUtility.Create(true, Title);
        }

        public void ValidateTitle()
        {
            var validationResult = CommonContentValidation.ValidateTitle(Title);

            TitleHasValidationIssues = !validationResult.isValid;
            TitleValidationMessage = validationResult.explanation;
        }

        public void ValidateSlug()
        {
            var validationResult = CommonContentValidation.ValidateSlugLocal(Slug);

            SlugHasValidationIssues = !validationResult.isValid;
            SlugValidationMessage = validationResult.explanation;
        }

        public void ValidateFolder()
        {
            var validationResult = CommonContentValidation.ValidateFolder(Folder);

            FolderHasValidationIssues = !validationResult.isValid;
            FolderValidationMessage = validationResult.explanation;
        }
    }
}