using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.BoolDataEntry;
using PointlessWaymarksCmsWpfControls.ConversionDataEntry;
using PointlessWaymarksCmsWpfControls.CreatedAndUpdatedByAndOnDisplay;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.StringDataEntry;
using PointlessWaymarksCmsWpfControls.TagsEditor;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.LinkContentEditor
{
    public class LinkContentEditorContext : INotifyPropertyChanged, IHasChanges
    {
        private StringDataEntryContext _authorEntry;
        private StringDataEntryContext _commentsEntry;
        private CreatedAndUpdatedByAndOnDisplayContext _createdUpdatedDisplay;
        private LinkContent _dbEntry;
        private StringDataEntryContext _descriptionEntry;
        private Command _extractDataCommand;
        private ConversionDataEntryContext<DateTime?> _linkDateTimeEntry;
        private StringDataEntryContext _linkUrlEntry;
        private Command _openUrlInBrowserCommand;
        private Command _saveAndCloseCommand;
        private Command _saveCommand;
        private BoolDataEntryContext _showInLinkRssEntry;
        private StringDataEntryContext _siteEntry;
        private StatusControlContext _statusContext;
        private TagsEditorContext _tagEdit;
        private StringDataEntryContext _titleEntry;

        public EventHandler RequestLinkContentEditorWindowClose;

        private LinkContentEditorContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            SaveCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(false));
            SaveAndCloseCommand = StatusContext.RunBlockingTaskCommand(async () => await SaveAndGenerateHtml(true));
            ExtractDataCommand = StatusContext.RunBlockingTaskCommand(ExtractDataFromLink);
            OpenUrlInBrowserCommand = StatusContext.RunNonBlockingActionCommand(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(LinkUrlEntry.UserValue)) StatusContext.ToastWarning("Link is Blank?");
                    var ps = new ProcessStartInfo(LinkUrlEntry.UserValue) {UseShellExecute = true, Verb = "open"};
                    Process.Start(ps);
                }
                catch (Exception e)
                {
                    StatusContext.ToastWarning($"Trouble opening link - {e.Message}");
                }
            });
        }

        public StringDataEntryContext AuthorEntry
        {
            get => _authorEntry;
            set
            {
                if (Equals(value, _authorEntry)) return;
                _authorEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext CommentsEntry
        {
            get => _commentsEntry;
            set
            {
                if (Equals(value, _commentsEntry)) return;
                _commentsEntry = value;
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

        public StringDataEntryContext DescriptionEntry
        {
            get => _descriptionEntry;
            set
            {
                if (Equals(value, _descriptionEntry)) return;
                _descriptionEntry = value;
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

        public bool HasChanges => PropertyScanners.ChildPropertiesHaveChanges(this);

        public ConversionDataEntryContext<DateTime?> LinkDateTimeEntry
        {
            get => _linkDateTimeEntry;
            set
            {
                if (Equals(value, _linkDateTimeEntry)) return;
                _linkDateTimeEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext LinkUrlEntry
        {
            get => _linkUrlEntry;
            set
            {
                if (Equals(value, _linkUrlEntry)) return;
                _linkUrlEntry = value;
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

        public Command SaveAndCloseCommand
        {
            get => _saveAndCloseCommand;
            set
            {
                if (Equals(value, _saveAndCloseCommand)) return;
                _saveAndCloseCommand = value;
                OnPropertyChanged();
            }
        }

        public Command SaveCommand
        {
            get => _saveCommand;
            set
            {
                if (Equals(value, _saveCommand)) return;
                _saveCommand = value;
                OnPropertyChanged();
            }
        }

        public BoolDataEntryContext ShowInLinkRssEntry
        {
            get => _showInLinkRssEntry;
            set
            {
                if (Equals(value, _showInLinkRssEntry)) return;
                _showInLinkRssEntry = value;
                OnPropertyChanged();
            }
        }

        public StringDataEntryContext SiteEntry
        {
            get => _siteEntry;
            set
            {
                if (Equals(value, _siteEntry)) return;
                _siteEntry = value;
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

        public StringDataEntryContext TitleEntry
        {
            get => _titleEntry;
            set
            {
                if (Equals(value, _titleEntry)) return;
                _titleEntry = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<LinkContentEditorContext> CreateInstance(StatusControlContext statusContext,
            LinkContent linkContent, bool extractDataOnLoad = false)
        {
            var newControl = new LinkContentEditorContext(statusContext);
            await newControl.LoadData(linkContent, extractDataOnLoad);
            return newControl;
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
                newEntry.LastUpdatedBy = CreatedUpdatedDisplay.UpdatedByEntry.UserValue.TrimNullToEmpty();
            }

            newEntry.Tags = TagEdit.TagListString();
            newEntry.CreatedBy = CreatedUpdatedDisplay.CreatedByEntry.UserValue.TrimNullToEmpty();
            newEntry.Comments = CommentsEntry.UserValue.TrimNullToEmpty();
            newEntry.Url = LinkUrlEntry.UserValue.TrimNullToEmpty();
            newEntry.Title = TitleEntry.UserValue.TrimNullToEmpty();
            newEntry.Site = SiteEntry.UserValue.TrimNullToEmpty();
            newEntry.Author = AuthorEntry.UserValue.TrimNullToEmpty();
            newEntry.Description = DescriptionEntry.UserValue.TrimNullToEmpty();
            newEntry.LinkDate = LinkDateTimeEntry.UserValue;
            newEntry.ShowInLinkRss = ShowInLinkRssEntry.UserValue;

            return newEntry;
        }

        private async Task ExtractDataFromLink()
        {
            var (generationReturn, linkMetadata) =
                await LinkGenerator.LinkMetadataFromUrl(LinkUrlEntry.UserValue, StatusContext.ProgressTracker());

            if (generationReturn.HasError)
            {
                StatusContext.ToastError(generationReturn.GenerationNote);
                return;
            }

            if (!string.IsNullOrWhiteSpace(linkMetadata.Title))
                TitleEntry.UserValue = linkMetadata.Title.TrimNullToEmpty();
            if (!string.IsNullOrWhiteSpace(linkMetadata.Author))
                AuthorEntry.UserValue = linkMetadata.Author.TrimNullToEmpty();
            if (!string.IsNullOrWhiteSpace(linkMetadata.Description))
                DescriptionEntry.UserValue = linkMetadata.Description.TrimNullToEmpty();
            if (!string.IsNullOrWhiteSpace(linkMetadata.Site))
                SiteEntry.UserValue = linkMetadata.Site.TrimNullToEmpty();
            if (linkMetadata.LinkDate != null) LinkDateTimeEntry.UserText = linkMetadata.LinkDate == null ? string.Empty : linkMetadata.LinkDate.Value.ToString("M/d/yyyy h:mm:ss tt");
        }

        private async Task LoadData(LinkContent toLoad, bool extractDataOnLoad = false)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new LinkContent
            {
                ShowInLinkRss = true, CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy
            };

            LinkUrlEntry = StringDataEntryContext.CreateInstance();
            LinkUrlEntry.Title = "URL";
            LinkUrlEntry.HelpText = "Link address";
            LinkUrlEntry.ValidationFunctions =
                new List<Func<string, (bool passed, string validationMessage)>> {ValidateUrl};
            LinkUrlEntry.ReferenceValue = DbEntry.Url.TrimNullToEmpty();
            LinkUrlEntry.UserValue = DbEntry.Url.TrimNullToEmpty();

            CommentsEntry = StringDataEntryContext.CreateInstance();
            CommentsEntry.Title = "Comments";
            CommentsEntry.HelpText = "Comments on the Linked Contents";
            CommentsEntry.ReferenceValue = DbEntry.Comments.TrimNullToEmpty();
            CommentsEntry.UserValue = DbEntry.Comments.TrimNullToEmpty();

            TitleEntry = StringDataEntryContext.CreateInstance();
            TitleEntry.Title = "Title";
            TitleEntry.HelpText = "Title Text";
            TitleEntry.ReferenceValue = DbEntry.Title.TrimNullToEmpty();
            TitleEntry.UserValue = DbEntry.Title.TrimNullToEmpty();
            TitleEntry.ValidationFunctions = new List<Func<string, (bool passed, string validationMessage)>>
            {
                CommonContentValidation.ValidateTitle
            };

            SiteEntry = StringDataEntryContext.CreateInstance();
            SiteEntry.Title = "Site";
            SiteEntry.HelpText = "Name of the Site";
            SiteEntry.ReferenceValue = DbEntry.Site.TrimNullToEmpty();
            SiteEntry.UserValue = DbEntry.Site.TrimNullToEmpty();

            AuthorEntry = StringDataEntryContext.CreateInstance();
            AuthorEntry.Title = "Author";
            AuthorEntry.HelpText = "Author of the linked content";
            AuthorEntry.ReferenceValue = DbEntry.Author.TrimNullToEmpty();
            AuthorEntry.UserValue = DbEntry.Author.TrimNullToEmpty();

            DescriptionEntry = StringDataEntryContext.CreateInstance();
            DescriptionEntry.Title = "Description";
            DescriptionEntry.HelpText = "Description of the linked content";
            DescriptionEntry.ReferenceValue = DbEntry.Description.TrimNullToEmpty();
            DescriptionEntry.UserValue = DbEntry.Description.TrimNullToEmpty();

            ShowInLinkRssEntry = BoolDataEntryContext.CreateInstance();
            ShowInLinkRssEntry.Title = "Show in Link RSS Feed";
            ShowInLinkRssEntry.HelpText = "If checked the link will appear in the site's Link RSS Feed";
            ShowInLinkRssEntry.ReferenceValue = DbEntry.ShowInLinkRss;
            ShowInLinkRssEntry.UserValue = DbEntry.ShowInLinkRss;

            LinkDateTimeEntry = ConversionDataEntryContext<DateTime?>.CreateInstance();
            LinkDateTimeEntry.Converter = ConversionDataEntryHelpers.DateTimeNullableConversion;
            LinkDateTimeEntry.Title = "Link Date";
            LinkDateTimeEntry.HelpText = "Date the Link Content was Created or Updated";
            LinkDateTimeEntry.ReferenceValue = DbEntry.LinkDate;
            LinkDateTimeEntry.UserText = DbEntry.LinkDate == null ? string.Empty : DbEntry.LinkDate.Value.ToString("M/d/yyyy h:mm:ss tt");

            CreatedUpdatedDisplay = await CreatedAndUpdatedByAndOnDisplayContext.CreateInstance(StatusContext, DbEntry);
            TagEdit = TagsEditorContext.CreateInstance(StatusContext, DbEntry);

            if (extractDataOnLoad) await ExtractDataFromLink();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task SaveAndGenerateHtml(bool closeAfterSave)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var (generationReturn, newContent) = await LinkGenerator.SaveAndGenerateHtml(CurrentStateToLinkContent(),
                null, StatusContext.ProgressTracker());

            if (generationReturn.HasError || newContent == null)
            {
                await StatusContext.ShowMessageWithOkButton("Problem Saving and Generating Html",
                    generationReturn.GenerationNote);
                return;
            }

            await LoadData(newContent);

            if (closeAfterSave)
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                RequestLinkContentEditorWindowClose?.Invoke(this, new EventArgs());
            }
        }

        public (bool passed, string validationMessage) ValidateUrl(string linkUrl)
        {
            return CommonContentValidation.ValidateLinkContentLinkUrl(linkUrl, DbEntry?.ContentId).Result;
        }
    }
}