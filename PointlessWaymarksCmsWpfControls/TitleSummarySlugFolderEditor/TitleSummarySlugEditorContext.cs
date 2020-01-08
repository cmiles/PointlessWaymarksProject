using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.TitleSummarySlugFolderEditor
{
    public class TitleSummarySlugEditorContext : INotifyPropertyChanged
    {
        private ITitleSummarySlugFolder _dbEntry;
        private string _folder;
        private string _slug;
        private StatusControlContext _statusContext;
        private string _summary;
        private string _title;
        private RelayCommand _titleToSlugCommand;

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

        public RelayCommand TitleToSlugCommand
        {
            get => _titleToSlugCommand;
            set
            {
                if (Equals(value, _titleToSlugCommand)) return;
                _titleToSlugCommand = value;
                OnPropertyChanged();
            }
        }

        private bool IsValidFilename(string testName)
        {
            //https://stackoverflow.com/questions/62771/how-do-i-check-if-a-given-string-is-a-legal-valid-file-name-under-windows
            var containsABadCharacter = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]");
            if (containsABadCharacter.IsMatch(testName)) return false;

            return true;
        }

        public async Task LoadData(ITitleSummarySlugFolder dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            TitleToSlugCommand = new RelayCommand(() =>
                StatusContext.RunBlockingAction(() => Slug = PointlessWaymarksCmsData.Slug.Create(true, Title)));

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

            var folderList = Folder.Split(",").ToList();

            if (folderList.Any(x => !IsValidFilename(x)))
            {
                isValid = false;
                errorMessage += "Folders have illegal characters...";
            }

            if (!isValid) return (isValid, errorMessage);

            if (await (await Db.Context()).SlugExistsInDatabase(Slug))
            {
                isValid = false;
                errorMessage += "Slug already exists in Database";
            }

            return (isValid, errorMessage);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}