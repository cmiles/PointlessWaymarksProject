using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarks.WpfCommon.ToastControl
{
    public class ToastViewModel : INotifyPropertyChanged
    {
        private string _message = "";
        private ToastType _type;
        public Action InvokeHideAnimation;

        public ToastViewModel()
        {
            Id = Guid.NewGuid();
            CreateTime = DateTime.Now;
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}