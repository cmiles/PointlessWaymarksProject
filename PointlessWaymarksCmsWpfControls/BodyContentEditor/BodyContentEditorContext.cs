using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.CommonHtml;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.ContentFormat;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;
using PointlessWaymarksCmsWpfControls.WpfHtml;

namespace PointlessWaymarksCmsWpfControls.BodyContentEditor
{
    public class BodyContentEditorContext : INotifyPropertyChanged
    {
        private ContentFormatChooserContext _bodyContentFormat;
        private string _bodyContentHtmlOutput;
        private IBodyContent _dbEntry;
        private StatusControlContext _statusContext;
        private string _userBodyContent = string.Empty;

        public BodyContentEditorContext(StatusControlContext statusContext, IBodyContent dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            BodyContentFormat = new ContentFormatChooserContext(StatusContext);
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
        }

        public string BodyContent
        {
            get => _userBodyContent;
            set
            {
                if (value == _userBodyContent) return;
                _userBodyContent = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(UpdateUpdateNotesContentHtml);
            }
        }

        public ContentFormatChooserContext BodyContentFormat
        {
            get => _bodyContentFormat;
            set
            {
                if (Equals(value, _bodyContentFormat)) return;
                _bodyContentFormat = value;
                OnPropertyChanged();

                StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(UpdateUpdateNotesContentHtml);
            }
        }

        public string BodyContentHtmlOutput
        {
            get => _bodyContentHtmlOutput;
            set
            {
                if (value == _bodyContentHtmlOutput) return;
                _bodyContentHtmlOutput = value;
                OnPropertyChanged();
            }
        }

        public IBodyContent DbEntry
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

        public async Task LoadData(IBodyContent toLoad)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            DbEntry = toLoad;

            if (toLoad == null)
            {
                BodyContent = string.Empty;
                BodyContentFormat.SelectedContentFormat = BodyContentFormat.ContentFormatChoices.First();
                return;
            }

            BodyContent = toLoad.BodyContent;
            var setUpdateFormatOk = await BodyContentFormat.TrySelectContentChoice(toLoad.BodyContentFormat);

            if (!setUpdateFormatOk) StatusContext.ToastWarning("Trouble loading Format from Db...");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task UpdateUpdateNotesContentHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var settings = await UserSettingsUtilities.ReadSettings();

            try
            {
                var preprocessResults = BracketCodes.PhotoCodeProcessForDirectLocalAccess(BodyContent);
                var processResults =
                    ContentProcessor.ContentHtml(BodyContentFormat.SelectedContentFormat, preprocessResults);

                var styleBlock = settings.CssMainStyleFileUrl();

                BodyContentHtmlOutput = processResults.ToHtmlDocument("Body Content", styleBlock);
            }
            catch (Exception e)
            {
                BodyContentHtmlOutput = "<h2>Not able to process input</h2>".ToHtmlDocument("Invalid", string.Empty);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}