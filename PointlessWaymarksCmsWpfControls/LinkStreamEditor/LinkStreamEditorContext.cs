using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AngleSharp;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using pinboard.net;
using pinboard.net.Models;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.LinkStreamEditor
{
    public class LinkStreamEditorContext : INotifyPropertyChanged
    {
        private string _author;
        private string _comments;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private LinkStream _dbEntry;
        private string _description;
        private RelayCommand _extractDataCommand;
        private DateTime? _linkDateTime;
        private string _linkUrl;
        private RelayCommand _saveUpdateDatabaseCommand;
        private string _site;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private string _title;

        public LinkStreamEditorContext(StatusControlContext statusContext, LinkStream linkContent)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(linkContent));
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

        public RelayCommand ExtractDataCommand
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

        public RelayCommand SaveUpdateDatabaseCommand
        {
            get => _saveUpdateDatabaseCommand;
            set
            {
                if (Equals(value, _saveUpdateDatabaseCommand)) return;
                _saveUpdateDatabaseCommand = value;
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

        private async Task ExtractDataFromLink(IProgress<string> progress = null)
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

        private async Task LoadData(LinkStream toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new LinkStream();

            LinkUrl = DbEntry?.Url ?? string.Empty;
            Comments = DbEntry?.Comments ?? string.Empty;
            Title = DbEntry?.Title ?? string.Empty;
            Site = DbEntry?.Site ?? string.Empty;
            Author = DbEntry?.Author ?? string.Empty;
            Description = DbEntry?.Description ?? string.Empty;

            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, toLoad);
            TagEdit = new TagsEditorContext(StatusContext, toLoad);

            CreatedUpdatedDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, toLoad);
            SaveUpdateDatabaseCommand = new RelayCommand(() =>
                StatusContext.RunBlockingTask(() => SaveToDbWithValidation(StatusContext?.ProgressTracker())));
            ExtractDataCommand = new RelayCommand(() =>
                StatusContext.RunBlockingTask(() => ExtractDataFromLink(StatusContext?.ProgressTracker())));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task SaveToDatabase(IProgress<string> progress = null)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            progress?.Report("Setting up new Entry");

            var newEntry = new LinkStream();

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
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedBy;
            }

            newEntry.Tags = TagEdit.Tags;
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedBy;
            newEntry.Comments = Comments;
            newEntry.Url = LinkUrl;
            newEntry.Title = Title;
            newEntry.Site = Site;
            newEntry.Author = Author;
            newEntry.Description = Description;
            newEntry.LinkDate = LinkDateTime;

            var context = await Db.Context();

            progress?.Report("Setting up new Historic Entry");

            var toHistoric = await context.LinkStreams.Where(x => x.ContentId == newEntry.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                progress?.Report("Saving Historic Entry");

                var newHistoric = new HistoricLinkStream();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricLinkStreams.AddAsync(newHistoric);
                context.LinkStreams.Remove(loopToHistoric);
            }

            progress?.Report("Adding new entry");

            context.LinkStreams.Add(newEntry);

            progress?.Report("Saving Changes");

            await context.SaveChangesAsync(true);

            DbEntry = newEntry;

            await LoadData(newEntry);
        }

        private async Task SaveToDbWithValidation(IProgress<string> progress = null)
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

        private async Task SaveToPinboard(IProgress<string> progress = null)
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

            var tagList = TagEdit.TagList();
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
                await UserSettingsUtilities.ValidateLocalSiteRootDirectory(),
                await CreatedUpdatedDisplay.Validate(),
                await Validate()
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}