using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TheLemmonWorkshopData;

namespace TheLemmonWorkshopWpfControls.ContentFormat
{
    public class ContentFormatChooserViewModel : INotifyPropertyChanged
    {
        private List<ContentFormatEnum> _contentFormatChoices;

        private ContentFormatEnum _selectedContentFormat;

        public ContentFormatChooserViewModel()
        {
            ContentFormatChoices = Enum.GetValues(typeof(ContentFormatEnum))
                .Cast<ContentFormatEnum>().ToList();
            SelectedContentFormat = ContentFormatChoices.First();
        }

        public event EventHandler<string> OnSelectedValueChanged;

        public event PropertyChangedEventHandler PropertyChanged;

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
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}