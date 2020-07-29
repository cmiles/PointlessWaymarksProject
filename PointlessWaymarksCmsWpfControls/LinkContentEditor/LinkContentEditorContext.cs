using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.LinkContentEditor
{
    public class LinkContentEditorContext : INotifyPropertyChanged, IHasUnsavedChanges
    {
        private string _author;
        private bool _authorHasChanges;
        private string _comments;
        private bool _commentsHaveChanges;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private LinkContent _dbEntry;
        private string _description;
        private bool _descriptionHasChanges;
        private Command _extractDataCommand;
        private DateTime? _linkDateTime;
        private bool _linkDateTimeHasChanges;
        private string _linkUrl;
        private bool _linkUrlHasChanges;
        private Command _openUrlInBrowserCommand;
        private Command _saveUpdateDatabaseAndCloseCommand;
        private Command _saveUpdateDatabaseCommand;
        private bool _showInLinkRss;
        private bool _showInLinkRssHasChanges;
        private string _site;
        private bool _siteHasChanges;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private string _title;
        private bool _titleHasChanges;

        public EventHandler RequestLinkContentEditorWindowClose;

        public LinkContentEditorContext(StatusControlContext statusContext, LinkContent linkContent,
            bool extractDataOnLoad = false)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            SaveUpdateDatabaseCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
            SaveUpdateDatabaseAndCloseCommand =
                StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
            ExtractDataCommand = StatusContext.RunBlockingTaskCommand(ExtractDataFromLink);
            OpenUrlInBrowserCommand = StatusContext.RunNonBlockingActionCommand(() =>
            {
                try
                {
                    var ps = new ProcessStartInfo(LinkUrl) {UseShellExecute = true, Verb = "open"};
                    Process.Start(ps);
                }
                catch (Exception e)
                {
                    StatusContext.ToastWarning($"Trouble opening link - {e.Message}");
                }
            });

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () =>
                await LoadData(linkContent, extractDataOnLoad));
        }

        public string Author
        {
            get => _author;
            set
            {
                if (value == _author) return;
                _author = value;
                OnPropertyChanged();
            }
        }

        public bool AuthorHasChanges
        {
            get => _authorHasChanges;
            set
            {
                if (value == _authorHasChanges) return;
                _authorHasChanges = value;
                OnPropertyChanged();
            }
        }

        public string Comments
        {
            get => _comments;
            set
            {
                if (value == _comments) return;
                _comments = value;
                OnPropertyChanged();
            }
        }

        public bool CommentsHaveChanges
        {
            get => _commentsHaveChanges;
            set
            {
                if (value == _commentsHaveChanges) return;
                _commentsHaveChanges = value;
                OnPropertyChanged();
            }
        }

        public CreatedAndUpdatedByAndOnDisplayContext CreatedUpdatedDisplay
        {
            get => _createdUpdatedDisplay;
            set
            {
                if (Equals(value, _createdUpdatedDisplay)) return;
                _createdUpdatedDisplay = value;
                OnPropertyChanged();
            }
        }

        public LinkContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public bool DescriptionHasChanges
        {
            get => _descriptionHasChanges;
            set
            {
                if (value == _descriptionHasChanges) return;
                _descriptionHasChanges = value;
                OnPropertyChanged();
            }
        }

        public Command ExtractDataCommand
        {
            get => _extractDataCommand;
            set
            {
                if (Equals(value, _extractDataCommand)) return;
                _extractDataCommand = value;
                OnPropertyChanged();
            }
        }

        public DateTime? LinkDateTime
        {
            get => _linkDateTime;
            set
            {
                if (value.Equals(_linkDateTime)) return;
                _linkDateTime = value;
                OnPropertyChanged();
            }
        }

        public bool LinkDateTimeHasChanges
        {
            get => _linkDateTimeHasChanges;
            set
            {
                if (value == _linkDateTimeHasChanges) return;
                _linkDateTimeHasChanges = value;
                OnPropertyChanged();
            }
        }

        public string LinkUrl
        {
            get => _linkUrl;
            set
            {
                if (value == _linkUrl) return;
                _linkUrl = value;
                OnPropertyChanged();
            }
        }

        public bool LinkUrlHasChanges
        {
            get => _linkUrlHasChanges;
            set
            {
                if (value == _linkUrlHasChanges) return;
                _linkUrlHasChanges = value;
                OnPropertyChanged();
            }
        }

        public Command OpenUrlInBrowserCommand
        {
            get => _openUrlInBrowserCommand;
            set
            {
                if (Equals(value, _openUrlInBrowserCommand)) return;
                _openUrlInBrowserCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveUpdateDatabaseAndCloseCommand
        {
            get => _saveUpdateDatabaseAndCloseCommand;
            set
            {
                if (Equals(value, _saveUpdateDatabaseAndCloseCommand)) return;
                _saveUpdateDatabaseAndCloseCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveUpdateDatabaseCommand
        {
            get => _saveUpdateDatabaseCommand;
            set
            {
                if (Equals(value, _saveUpdateDatabaseCommand)) return;
                _saveUpdateDatabaseCommand = value;
                OnPropertyChanged();
            }
        }

        public bool ShowInLinkRss
        {
            get => _showInLinkRss;
            set
            {
                if (value == _showInLinkRss) return;
                _showInLinkRss = value;
                OnPropertyChanged();
            }
        }

        public bool ShowInLinkRssHasChanges
        {
            get => _showInLinkRssHasChanges;
            set
            {
                if (value == _showInLinkRssHasChanges) return;
                _showInLinkRssHasChanges = value;
                OnPropertyChanged();
            }
        }

        public string Site
        {
            get => _site;
            set
            {
                if (value == _site) return;
                _site = value;
                OnPropertyChanged();
            }
        }

        public bool SiteHasChanges
        {
            get => _siteHasChanges;
            set
            {
                if (value == _siteHasChanges) return;
                _siteHasChanges = value;
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

        public TagsEditorContext TagEdit
        {
            get => _tagEdit;
            set
            {
                if (Equals(value, _tagEdit)) return;
                _tagEdit = value;
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

        public bool HasChanges()
        {
            return !(!TagEdit.TagsHaveChanges && DbEntry.ShowInLinkRss == ShowInLinkRss &&
                     DbEntry.LinkDate == LinkDateTime &&
                     StringHelpers.AreEqual(DbEntry.CreatedBy, CreatedUpdatedDisplay.CreatedBy.TrimNullToEmpty()) &&
                     StringHelpers.AreEqual(DbEntry.Comments, Comments.TrimNullToEmpty()) &&
                     StringHelpers.AreEqual(DbEntry.Url, LinkUrl.TrimNullToEmpty()) &&
                     StringHelpers.AreEqual(DbEntry.Title, Title.TrimNullToEmpty()) &&
                     StringHelpers.AreEqual(DbEntry.Site, Site.TrimNullToEmpty()) &&
                     StringHelpers.AreEqual(DbEntry.Author, Author.TrimNullToEmpty()) &&
                     StringHelpers.AreEqual(DbEntry.Description, Description.TrimNullToEmpty()));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChanges()
        {
            // ReSharper disable InvokeAsExtensionMethod - in this case TrimNullSage - which returns an
            //Empty string from null will not be invoked as an extension if DbEntry is null...
            AuthorHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Author) != Author.TrimNullToEmpty();
            CommentsHaveChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Comments) != Comments.TrimNullToEmpty();
            DescriptionHasChanges =
                StringHelpers.TrimNullToEmpty(DbEntry?.Description) != Description.TrimNullToEmpty();
            LinkDateTimeHasChanges = DbEntry?.LinkDate != LinkDateTime;
            LinkUrlHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Url) != LinkUrl.TrimNullToEmpty();
            ShowInLinkRssHasChanges = DbEntry?.ShowInLinkRss != ShowInLinkRss;
            SiteHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Site) != Site.TrimNullToEmpty();
            TitleHasChanges = StringHelpers.TrimNullToEmpty(DbEntry?.Title) != Title.TrimNullToEmpty();
            // ReSharper restore InvokeAsExtensionMethod
        }

        private async Task ExtractDataFromLink()
        {
            var (generationReturn, linkMetadata) =
                await LinkGenerator.LinkMetadataFromUrl(LinkUrl, StatusContext.ProgressTracker());

            if (generationReturn.HasError)
            {
                StatusContext.ToastError(generationReturn.GenerationNote);
                return;
            }

            if (!string.IsNullOrWhiteSpace(linkMetadata.Title)) Title = linkMetadata.Title.TrimNullToEmpty();
            if (!string.IsNullOrWhiteSpace(linkMetadata.Author)) Author = linkMetadata.Author.TrimNullToEmpty();
            if (!string.IsNullOrWhiteSpace(linkMetadata.Description))
                Description = linkMetadata.Description.TrimNullToEmpty();
            if (!string.IsNullOrWhiteSpace(linkMetadata.Site)) Site = linkMetadata.Site.TrimNullToEmpty();
            if (linkMetadata.LinkDate != null) LinkDateTime = linkMetadata.LinkDate;
        }

        private async Task LoadData(LinkContent toLoad, bool extractDataOnLoad = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new LinkContent
            {
                ShowInLinkRss = true, CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
            };

            LinkUrl = DbEntry?.Url ?? string.Empty;
            Comments = DbEntry?.Comments ?? string.Empty;
            Title = DbEntry?.Title ?? string.Empty;
            Site = DbEntry?.Site ?? string.Empty;
            Author = DbEntry?.Author ?? string.Empty;
            Description = DbEntry?.Description ?? string.Empty;
            ShowInLinkRss = DbEntry?.ShowInLinkRss ?? true;

            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);
            TagEdit = new TagsEditorContext(StatusContext, DbEntry);
            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, DbEntry);

            if (extractDataOnLoad) await ExtractDataFromLink();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!(propertyName.Contains("HasChanges") || propertyName.Contains("HaveChanges"))) CheckForChanges();
        }

        private LinkContent CurrentStateToLinkContent()
        {
            var newEntry = new LinkContent();

            if (DbEntry == null || DbEntry.Id < 1)
            {
                newEntry.ContentId = Guid.NewGuid();
                newEntry.CreatedOn = DateTime.Now;
            }
            else
            {
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedBy.TrimNullToEmpty();
            }

            newEntry.Tags = TagEdit.TagListString();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy.TrimNullToEmpty();
            newEntry.Comments = Comments.TrimNullToEmpty();
            newEntry.Url = LinkUrl.TrimNullToEmpty();
            newEntry.Title = Title.TrimNullToEmpty();
            newEntry.Site = Site.TrimNullToEmpty();
            newEntry.Author = Author.TrimNullToEmpty();
            newEntry.Description = Description.TrimNullToEmpty();
            newEntry.LinkDate = LinkDateTime;
            newEntry.ShowInLinkRss = ShowInLinkRss;

            return newEntry;
        }

        public async Task SaveAndGenerateHtml(bool closeAfterSave)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) =
                await LinkGenerator.SaveAndGenerateHtml(CurrentStateToLinkContent(), StatusContext.ProgressTracker());

            if (generationReturn.HasError || newContent == null)
            {
                await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                    generationReturn.GenerationNote);
                return;
            }

            if (closeAfterSave)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                RequestLinkContentEditorWindowClose?.Invoke(this, new EventArgs());
                return;
            }

            await LoadData(newContent);
        }
    }
}