using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TheLemmonWorkshopData;
using TheLemmonWorkshopData.Models;
using TheLemmonWorkshopWpfControls.ContentFormat;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;
using TheLemmonWorkshopWpfControls.WpfHtml;

namespace TheLemmonWorkshopWpfControls.BodyContentEditor
{
    public class BodyContentEditorContext : INotifyPropertyChanged
    {
        private ContentFormatChooserContext _bodyContentFormat;
        private string _bodyContentHtmlOutput;
        private IBodyContent _dbEntry;
        private string _userBodyContent = string.Empty;
        private StatusControlContext _statusContext;

        public BodyContentEditorContext(StatusControlContext statusContext, IBodyContent dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            BodyContentFormat = new ContentFormatChooserContext(StatusContext);
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(() => LoadData(dbEntry));
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

        public event PropertyChangedEventHandler PropertyChanged;

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

        public async Task UpdateUpdateNotesContentHtml()
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            try
            {
                var processResults = ContentProcessor.ContentHtml(BodyContentFormat.SelectedContentFormat, BodyContent);
                BodyContentHtmlOutput = processResults.ToHtmlDocument("Update Notes", string.Empty);
            }
            catch (Exception e)
            {
                BodyContentHtmlOutput = "<h2>Not able to process input</h2>".ToHtmlDocument("Invalid", string.Empty);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}