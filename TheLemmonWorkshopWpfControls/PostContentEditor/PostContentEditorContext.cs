using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.CommandWpf;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using TheLemmonWorkshopData;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopData.PhotoHtml;
using TheLemmonWorkshopData.PostHtml;
using TheLemmonWorkshopWpfControls.BodyContentEditor;
using TheLemmonWorkshopWpfControls.ContentIdViewer;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.TagsEditor;
using TheLemmonWorkshopWpfControls.TitleSummarySlugEditor;
using TheLemmonWorkshopWpfControls.UpdateNotesEditor;
using TheLemmonWorkshopWpfControls.UpdatesByAndOnDisplay;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.PostContentEditor
{
    public class PostContentEditorContext : INotifyPropertyChanged
    {
        private PostContent _dbEntry;
        private TitleSummarySlugEditorContext _titleSummarySlugFolder;
        private CreatedAndUpdatedByAndOnDisplayContext _createdAndUpdatedByAndOnDisplay;
        private ContentIdViewerControlContext _contentId;
        private UpdateNotesEditorContext _updateNotes;
        private TagsEditorContext _tags;
        private BodyContentEditorContext _bodyContent;
        private RelayCommand _saveUpdateDatabaseCommand;
        private RelayCommand _saveAndCreateLocalCommand;
        public event PropertyChangedEventHandler PropertyChanged;

        public PostContentEditorContext(StatusControlContext statusContext, PostContent postContent)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(postContent));
        }

        public StatusControlContext StatusContext { get; set; }

        public async Task LoadData(PostContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad ?? new PostContent();
            TitleSummarySlugFolder = new TitleSummarySlugEditorContext(StatusContext, toLoad);
            CreatedAndUpdatedByAndOnDisplay = new CreatedAndUpdatedByAndOnDisplayContext(StatusContext, toLoad);
            ContentId = new ContentIdViewerControlContext(StatusContext, toLoad);
            UpdateNotes = new UpdateNotesEditorContext(StatusContext, toLoad);
            Tags = new TagsEditorContext(StatusContext, toLoad);
            BodyContent = new BodyContentEditorContext(StatusContext, toLoad);

            SaveAndCreateLocalCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveAndCreateLocal));
            SaveUpdateDatabaseCommand = new RelayCommand(() => StatusContext.RunBlockingTask(SaveToDbWithValidation));
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

        public RelayCommand SaveAndCreateLocalCommand
        {
            get => _saveAndCreateLocalCommand;
            set
            {
                if (Equals(value, _saveAndCreateLocalCommand)) return;
                _saveAndCreateLocalCommand = value;
                OnPropertyChanged();
            }
        }

        private async Task SaveToDatabase()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var newEntry = new PostContent();

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
                newEntry.LastUpdatedBy = CreatedAndUpdatedByAndOnDisplay.UpdatedBy;
            }

            newEntry.Folder = TitleSummarySlugFolder.Folder;
            newEntry.Slug = TitleSummarySlugFolder.Slug;
            newEntry.Summary = TitleSummarySlugFolder.Summary;
            newEntry.Tags = Tags.Tags;
            newEntry.Title = TitleSummarySlugFolder.Title;
            newEntry.CreatedBy = CreatedAndUpdatedByAndOnDisplay.CreatedBy;
            newEntry.UpdateNotes = UpdateNotes.UpdateNotes;
            newEntry.UpdateNotesFormat = UpdateNotes.UpdateNotesFormat.SelectedContentFormatAsString;
            newEntry.BodyContent = BodyContent.BodyContent;
            newEntry.BodyContentFormat = BodyContent.BodyContentFormat.SelectedContentFormatAsString;

            newEntry.MainImage = PhotoBracketCode.FirstPhotoId(newEntry.BodyContent);

            var context = await Db.Context();

            var toHistoric = await context.PostContents.Where(x => x.ContentId == newEntry.ContentId).ToListAsync();

            foreach (var loopToHistoric in toHistoric)
            {
                var newHistoric = new HistoricPostContent();
                newHistoric.InjectFrom(loopToHistoric);
                newHistoric.Id = 0;
                await context.HistoricPostContents.AddAsync(newHistoric);
                context.PostContents.Remove(loopToHistoric);
            }

            context.PostContents.Add(newEntry);

            await context.SaveChangesAsync(true);

            DbEntry = newEntry;

            await LoadData(newEntry);
        }

        private async Task SaveToDbWithValidation()
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

            await SaveToDatabase();
        }

        public BodyContentEditorContext BodyContent
        {
            get => _bodyContent;
            set
            {
                if (Equals(value, _bodyContent)) return;
                _bodyContent = value;
                OnPropertyChanged();
            }
        }

        public TagsEditorContext Tags
        {
            get => _tags;
            set
            {
                if (Equals(value, _tags)) return;
                _tags = value;
                OnPropertyChanged();
            }
        }

        public UpdateNotesEditorContext UpdateNotes
        {
            get => _updateNotes;
            set
            {
                if (Equals(value, _updateNotes)) return;
                _updateNotes = value;
                OnPropertyChanged();
            }
        }

        public ContentIdViewerControlContext ContentId
        {
            get => _contentId;
            set
            {
                if (Equals(value, _contentId)) return;
                _contentId = value;
                OnPropertyChanged();
            }
        }

        public CreatedAndUpdatedByAndOnDisplayContext CreatedAndUpdatedByAndOnDisplay
        {
            get => _createdAndUpdatedByAndOnDisplay;
            set
            {
                if (Equals(value, _createdAndUpdatedByAndOnDisplay)) return;
                _createdAndUpdatedByAndOnDisplay = value;
                OnPropertyChanged();
            }
        }

        public TitleSummarySlugEditorContext TitleSummarySlugFolder
        {
            get => _titleSummarySlugFolder;
            set
            {
                if (Equals(value, _titleSummarySlugFolder)) return;
                _titleSummarySlugFolder = value;
                OnPropertyChanged();
            }
        }

        public PostContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();
            }
        }

        private async Task<List<(bool, string)>> ValidateAll()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            return new List<(bool, string)>
            {
                await UserSettingsUtilities.ValidateLocalSiteRootDirectory(),
                await TitleSummarySlugFolder.Validate(),
                await CreatedAndUpdatedByAndOnDisplay.Validate(),
                await Validate()
            };
        }

        private async Task SaveAndCreateLocal()
        {
            var validationList = await ValidateAll();

            if (validationList.Any(x => !x.Item1))
            {
                await StatusContext.ShowMessage("Validation Error",
                    string.Join(Environment.NewLine, validationList.Where(x => !x.Item1).Select(x => x.Item2).ToList()),
                    new List<string> {"Ok"});
                return;
            }

            await SaveToDatabase();
            await GenerateHtml();
            await WriteLocalDbJson();
        }

        private async Task WriteLocalDbJson()
        {
            var settings = await UserSettingsUtilities.ReadSettings();
            var db = await Db.Context();
            var jsonDbEntry = System.Text.Json.JsonSerializer.Serialize(DbEntry);

            var jsonFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(DbEntry).FullName,
                $"{DbEntry.ContentId}.json"));

            if (jsonFile.Exists) jsonFile.Delete();
            jsonFile.Refresh();

            File.WriteAllText(jsonFile.FullName, jsonDbEntry);

            var latestHistoricEntries = db.HistoricPostContents.Where(x => x.ContentId == DbEntry.ContentId)
                .OrderByDescending(x => x.LastUpdatedOn).Take(10);

            if (!latestHistoricEntries.Any()) return;

            var jsonHistoricDbEntry = System.Text.Json.JsonSerializer.Serialize(latestHistoricEntries);

            var jsonHistoricFile = new FileInfo(Path.Combine(settings.LocalSitePostContentDirectory(DbEntry).FullName,
                $"{DbEntry.ContentId}-Historic.json"));

            if (jsonHistoricFile.Exists) jsonHistoricFile.Delete();
            jsonHistoricFile.Refresh();

            File.WriteAllText(jsonHistoricFile.FullName, jsonHistoricDbEntry);
        }

        private async Task<(bool, string)> Validate()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            return (true, string.Empty);
        }

        private async Task GenerateHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var htmlContext = new SinglePostPage(DbEntry);

            htmlContext.WriteLocalHtml();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}