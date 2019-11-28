using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace TheLemmonWorkshopWpfControls.ContentList
{
    public class ContentListItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Title { get; set; }
        public string Summary { get; set; }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}