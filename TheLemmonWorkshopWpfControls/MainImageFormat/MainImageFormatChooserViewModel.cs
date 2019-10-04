using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TheLemmonWorkshopData;

namespace TheLemmonWorkshopWpfControls.MainImageFormat
{
    public class MainImageFormatChooserViewModel : INotifyPropertyChanged
    {
        private List<MainImageContentFormatEnum> _contentFormatChoices;

        private MainImageContentFormatEnum _selectedContentFormat;

        public MainImageFormatChooserViewModel()
        {
            ContentFormatChoices = Enum.GetValues(typeof(MainImageContentFormatEnum))
                .Cast<MainImageContentFormatEnum>().ToList();
            SelectedContentFormat = ContentFormatChoices.First();
        }

        public event EventHandler<string> OnSelectedValueChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<MainImageContentFormatEnum> ContentFormatChoices
        {
            get => _contentFormatChoices;
            set
            {
                if (Equals(value, _contentFormatChoices)) return;
                _contentFormatChoices = value;
                OnPropertyChanged();
            }
        }

        public MainImageContentFormatEnum SelectedContentFormat
        {
            get => _selectedContentFormat;
            set
            {
                if (value == _selectedContentFormat) return;
                _selectedContentFormat = value;
                OnPropertyChanged();

                OnSelectedValueChanged?.Invoke(this, Enum.GetName(typeof(MainImageContentFormatEnum), SelectedContentFormat));
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}