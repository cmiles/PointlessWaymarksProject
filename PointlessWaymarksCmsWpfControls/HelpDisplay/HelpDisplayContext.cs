using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsWpfControls.HelpDisplay
{
    public class HelpDisplayContext : INotifyPropertyChanged
    {
        private string _helpMarkdownContent;

        public HelpDisplayContext(string markdownHelp)
        {
            HelpMarkdownContent = markdownHelp ?? string.Empty;
        }

        public string HelpMarkdownContent
        {
            get => _helpMarkdownContent;
            set
            {
                if (value == _helpMarkdownContent) return;
                _helpMarkdownContent = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}