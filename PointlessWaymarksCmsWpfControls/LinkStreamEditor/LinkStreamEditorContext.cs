using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AngleSharp;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using pinboard.net;
using pinboard.net.Models;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.LinkStreamEditor
{
    public class LinkStreamEditorContext : INotifyPropertyChanged, IHasUnsavedChanges
    {
        private string _author;
        private bool _authorHasChanges;
        private string _comments;
        private bool _commentsHaveChanges;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private LinkStream _dbEntry;
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

        public EventHandler RequestLinkStreamEditorWindowClose;

        public LinkStreamEditorContext(StatusControlContext statusContext, LinkStream linkContent,
            bool extractDataOnLoad = false)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            SaveUpdateDatabaseCommand = new Command(() =>
                StatusContext.RunBlockingTask(() => SaveToDbWithValidation(StatusContext?.ProgressTracker())));
            SaveUpdateDatabaseAndCloseCommand = new Command(() =>
                StatusContext.RunBlockingTask(() => SaveToDbWithValidationAndClose(StatusContext?.ProgressTracker())));
            ExtractDataCommand = new Command(() =>
                StatusContext.RunBlockingTask(() => ExtractDataFromLink(StatusContext?.ProgressTracker())));
            OpenUrlInBrowserCommand = new Command(() =>
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

        public LinkStream DbEntry
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
                     StringHelpers.AreEqual(DbEntry.CreatedBy, CreatedUpdatedDisplay.CreatedBy.TrimNullSafe()) &&
                     StringHelpers.AreEqual(DbEntry.Comments, Comments.TrimNullSafe()) &&
                     StringHelpers.AreEqual(DbEntry.Url, LinkUrl.TrimNullSafe()) &&
                     StringHelpers.AreEqual(DbEntry.Title, Title.TrimNullSafe()) &&
                     StringHelpers.AreEqual(DbEntry.Site, Site.TrimNullSafe()) &&
                     StringHelpers.AreEqual(DbEntry.Author, Author.TrimNullSafe()) &&
                     StringHelpers.AreEqual(DbEntry.Description, Description.TrimNullSafe()));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChanges()
        {
            // ReSharper disable InvokeAsExtensionMethod - in this case TrimNullSage - which returns an
            //Empty string from null will not be invoked as an extension if DbEntry is null...
            AuthorHasChanges = StringHelpers.TrimNullSafe(DbEntry?.Author) != Author.TrimNullSafe();
            CommentsHaveChanges = StringHelpers.TrimNullSafe(DbEntry?.Comments) != Comments.TrimNullSafe();
            DescriptionHasChanges = StringHelpers.TrimNullSafe(DbEntry?.Description) != Description.TrimNullSafe();
            LinkDateTimeHasChanges = DbEntry?.LinkDate != LinkDateTime;
            LinkUrlHasChanges = StringHelpers.TrimNullSafe(DbEntry?.Url) != LinkUrl.TrimNullSafe();
            ShowInLinkRssHasChanges = DbEntry?.ShowInLinkRss != ShowInLinkRss;
            SiteHasChanges = StringHelpers.TrimNullSafe(DbEntry?.Site) != Site.TrimNullSafe();
            TitleHasChanges = StringHelpers.TrimNullSafe(DbEntry?.Title) != Title.TrimNullSafe();
            // ReSharper restore InvokeAsExtensionMethod
        }

        private async Task ExtractDataFromLink(IProgress<string> progress)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            progress?.Report("Setting up and Downloading Site");

            var config = Configuration.Default.WithDefaultLoader().WithJs();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(LinkUrl);

            progress?.Report("Looking for Title");

            var titleString = document.Head.Children.FirstOrDefault(x => x.TagName == "TITLE")?.TextContent;

            if (string.IsNullOrWhiteSpace(titleString))
                titleString = document.QuerySelector("meta[property='og:title']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(titleString))
                titleString = document.QuerySelector("meta[name='DC.title']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(titleString))
                titleString = document.QuerySelector("meta[name='twitter:title']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "value")?.Value;

            if (!string.IsNullOrWhiteSpace(titleString)) Title = titleString;

            progress?.Report("Looking for Author");


            var authorString = document.QuerySelector("meta[property='og:author']")?.Attributes
                .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector("meta[name='DC.contributor']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector("meta[property='article:author']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector("meta[name='author']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector("a[rel~=\"author\"]")?.TextContent;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector(".author__name")?.TextContent;

            if (string.IsNullOrWhiteSpace(authorString))
                authorString = document.QuerySelector(".author_name")?.TextContent;

            if (!string.IsNullOrWhiteSpace(authorString)) Author = authorString;

            progress?.Report($"Looking for Author - Found {Author}");


            progress?.Report("Looking for Date Time");

            var linkDateString = document.QuerySelector("meta[property='article:modified_time']")?.Attributes
                .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(linkDateString))
                linkDateString = document.QuerySelector("meta[property='og:updated_time']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(linkDateString))
                linkDateString = document.QuerySelector("meta[property='article:published_time']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(linkDateString))
                linkDateString = document.QuerySelector("meta[property='article:published_time']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(linkDateString))
                linkDateString = document.QuerySelector("meta[name='DC.date.created']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            progress?.Report($"Looking for Date Time - Found {linkDateString}");

            if (!string.IsNullOrWhiteSpace(linkDateString))
            {
                if (DateTime.TryParse(linkDateString, out var parsedDateTime))
                {
                    LinkDateTime = parsedDateTime;
                    progress?.Report($"Looking for Date Time - Parsed to {parsedDateTime}");
                }
                else
                {
                    progress?.Report("Did not parse Date Time");
                }
            }

            progress?.Report("Looking for Site Name");

            var siteString = document.QuerySelector("meta[property='og:site_name']")?.Attributes
                .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(siteString))
                siteString = document.QuerySelector("meta[name='DC.publisher']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(siteString))
                siteString = document.QuerySelector("meta[name='twitter:site']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "value")?.Value.Replace("@", "");

            if (!string.IsNullOrWhiteSpace(siteString)) Site = siteString;

            progress?.Report($"Looking for Site Name - Found {Site}");


            progress?.Report("Looking for Description");

            var descriptionString = document.QuerySelector("meta[name='description']")?.Attributes
                .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(descriptionString))
                descriptionString = document.QuerySelector("meta[property='og:description']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (string.IsNullOrWhiteSpace(descriptionString))
                descriptionString = document.QuerySelector("meta[name='twitter:description']")?.Attributes
                    .FirstOrDefault(x => x.LocalName == "content")?.Value;

            if (!string.IsNullOrWhiteSpace(descriptionString)) Description = descriptionString;

            progress?.Report($"Looking for Description - Found {Description}");
        }

        private async Task LoadData(LinkStream toLoad, bool extractDataOnLoad = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new LinkStream
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

            if (extractDataOnLoad) await ExtractDataFromLink(StatusContext?.ProgressTracker());
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (!(propertyName.Contains("HasChanges") || propertyName.Contains("HaveChanges"))) CheckForChanges();
        }

        private async Task SaveToDatabase(IProgress<string> progress)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            progress?.Report("Setting up new Entry");

            var newEntry = new LinkStream();

            var isNewEntry = false;

            if (DbEntry == null || DbEntry.Id < 1)
            {
                isNewEntry = true;
                newEntry.ContentId = Guid.NewGuid();
                newEntry.CreatedOn = DateTime.Now;
                newEntry.ContentVersion = newEntry.CreatedOn.ToUniversalTime();
            }
            else
            {
                newEntry.ContentId = DbEntry.ContentId;
                newEntry.CreatedOn = DbEntry.CreatedOn;
                newEntry.LastUpdatedOn = DateTime.Now;
                newEntry.ContentVersion = newEntry.LastUpdatedOn.Value.ToUniversalTime();
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedBy;
            }

            newEntry.Tags = TagEdit.TagListString();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy.TrimNullSafe();
            newEntry.Comments = Comments.TrimNullSafe();
            newEntry.Url = LinkUrl.TrimNullSafe();
            newEntry.Title = Title.TrimNullSafe();
            newEntry.Site = Site.TrimNullSafe();
            newEntry.Author = Author.TrimNullSafe();
            newEntry.Description = Description.TrimNullSafe();
            newEntry.LinkDate = LinkDateTime;
            newEntry.ShowInLinkRss = ShowInLinkRss;

            await Db.SaveLinkStream(newEntry);

            DbEntry = newEntry;

            await LoadData(newEntry);

            if (isNewEntry)
                await DataNotifications.PublishDataNotification(StatusContext.StatusControlContextId.ToString(),
                    DataNotificationContentType.Link, DataNotificationUpdateType.New,
                    new List<Guid> {newEntry.ContentId});
            else
                await DataNotifications.PublishDataNotification(StatusContext.StatusControlContextId.ToString(),
                    DataNotificationContentType.Link, DataNotificationUpdateType.Update,
                    new List<Guid> {newEntry.ContentId});
        }

        private async Task SaveToDbWithValidation(IProgress<string> progress)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var validationList = await ValidateAll();

            if (validationList.Any(x => !x.Item1))
            {
                await StatusContext.ShowMessage("Validation Error",
                    string.Join(Environment.NewLine, validationList.Where(x => !x.Item1).Select(x => x.Item2).ToList()),
                    new List<string> {"Ok"});
                return;
            }

            await SaveToDatabase(progress);
            await SaveToPinboard(progress);
        }

        private async Task SaveToDbWithValidationAndClose(IProgress<string> progress)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var validationList = await ValidateAll();

            if (validationList.Any(x => !x.Item1))
            {
                await StatusContext.ShowMessage("Validation Error",
                    string.Join(Environment.NewLine, validationList.Where(x => !x.Item1).Select(x => x.Item2).ToList()),
                    new List<string> {"Ok"});
                return;
            }

            await SaveToDatabase(progress);
            await SaveToPinboard(progress);

            RequestLinkStreamEditorWindowClose?.Invoke(this, new EventArgs());
        }

        private async Task SaveToPinboard(IProgress<string> progress)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().PinboardApiToken))
            {
                progress?.Report("No Pinboard Api Token... Skipping save to Pinboard.");
                return;
            }

            var descriptionFragments = new List<string>();
            if (!string.IsNullOrWhiteSpace(Site)) descriptionFragments.Add($"Site: {Site}");
            if (LinkDateTime != null) descriptionFragments.Add($"Date: {LinkDateTime.Value:g}");
            if (!string.IsNullOrWhiteSpace(Description)) descriptionFragments.Add($"Description: {Description}");
            if (!string.IsNullOrWhiteSpace(Comments)) descriptionFragments.Add($"Comments: {Comments}");
            if (!string.IsNullOrWhiteSpace(Author)) descriptionFragments.Add($"Author: {Author}");

            var tagList = TagEdit.TagSlugList();
            tagList.Add(UserSettingsSingleton.CurrentSettings().SiteName);
            tagList = tagList.Select(x => x.Replace(" ", "-")).ToList();

            progress?.Report("Setting up Pinboard");
            using var pb = new PinboardAPI(UserSettingsSingleton.CurrentSettings().PinboardApiToken);

            var bookmark = new Bookmark
            {
                Url = LinkUrl,
                Description = Title,
                Extended = string.Join(" ;; ", descriptionFragments),
                Tags = tagList,
                CreatedDate = DateTime.Now,
                Shared = true,
                ToRead = false,
                Replace = true
            };

            progress?.Report("Adding Pinboard Bookmark");
            await pb.Posts.Add(bookmark);

            progress?.Report("Pinboard Bookmark Complete");
        }

        private async Task<(bool, string)> Validate()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(LinkUrl)) return (false, "Link Url can not be null");

            return (true, string.Empty);
        }

        private async Task<List<(bool, string)>> ValidateAll()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            return new List<(bool, string)>
            {
                UserSettingsUtilities.ValidateLocalSiteRootDirectory(),
                await CreatedUpdatedDisplay.Validate(),
                await Validate()
            };
        }
    }
}