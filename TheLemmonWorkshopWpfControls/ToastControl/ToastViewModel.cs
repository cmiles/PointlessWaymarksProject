using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TheLemmonWorkshopWpfControls.ToastControl
{
    public class ToastViewModel : INotifyPropertyChanged
    {
        public Action InvokeHideAnimation;
        private string _message = "";
        private ToastType _type;

        public ToastViewModel()
        {
            Id = Guid.NewGuid();
            CreateTime = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DateTime CreateTime { get; }
        public Guid Id { get; }

        public string Message
        {
            get => _message;
            set
            {
                if (value == _message) return;
                _message = value;
                OnPropertyChanged();
            }
        }

        public ToastType Type
        {
            get => _type;
            set
            {
                if (value == _type) return;
                _type = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}