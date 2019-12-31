using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TheLemmonWorkshopData;
using TheLemmonWorkshopWpfControls.ControlStatus;
using TheLemmonWorkshopWpfControls.Utility;

namespace TheLemmonWorkshopWpfControls.ContentFormat
{
    public class ContentFormatChooserContext : INotifyPropertyChanged
    {
        private List<ContentFormatEnum> _contentFormatChoices;

        private ContentFormatEnum _selectedContentFormat;
        private StatusControlContext _statusContext;
        private string _selectedContentFormatAsString;

        public ContentFormatChooserContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            ContentFormatChoices = Enum.GetValues(typeof(ContentFormatEnum)).Cast<ContentFormatEnum>().ToList();
            SelectedContentFormat = ContentFormatChoices.First();
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

        public List<ContentFormatEnum> ContentFormatChoices
        {
            get => _contentFormatChoices;
            set
            {
                if (Equals(value, _contentFormatChoices)) return;
                _contentFormatChoices = value;
                OnPropertyChanged();
            }
        }

        public ContentFormatEnum SelectedContentFormat
        {
            get => _selectedContentFormat;
            set
            {
                if (value == _selectedContentFormat) return;
                _selectedContentFormat = value;
                OnPropertyChanged();

                OnSelectedValueChanged?.Invoke(this, Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat));
                SelectedContentFormatAsString =
                    Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat) ?? string.Empty;
            }
        }

        public string SelectedContentFormatAsString
        {
            get => _selectedContentFormatAsString;
            set
            {
                if (value == _selectedContentFormatAsString) return;
                _selectedContentFormatAsString = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task<bool> TrySelectContentChoice(string contentChoice)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(contentChoice)) return false;

            var toSelect = Enum.TryParse(typeof(ContentFormatEnum), contentChoice, true, out var parsedSelection);
            if (toSelect) SelectedContentFormat = (ContentFormatEnum) parsedSelection;
            return toSelect;
        }

        public event EventHandler<string> OnSelectedValueChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}