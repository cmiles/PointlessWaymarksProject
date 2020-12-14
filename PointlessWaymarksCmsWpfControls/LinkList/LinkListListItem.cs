using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsWpfControls.LinkList
{
    public class LinkListListItem : INotifyPropertyChanged
    {
        private LinkContent _dbEntry;
        private string _linkContentString;

        public LinkContent DbEntry
        {
            get => _dbEntry;
            set
            {
                if (Equals(value, _dbEntry)) return;
                _dbEntry = value;
                OnPropertyChanged();

                ConstructContentString();
            }
        }

        public string LinkContentString
        {
            get => _linkContentString;
            private set
            {
                if (value == _linkContentString) return;
                _linkContentString = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ConstructContentString()
        {
            if (DbEntry == null)
            {
                LinkContentString = string.Empty;
                return;
            }

            var newContentString = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(DbEntry.Description))
                newContentString.Append($"Description: {DbEntry.Description}");
            if (DbEntry.LinkDate != null)
                newContentString.Append($"Link Date: {DbEntry.LinkDate:d}");
            if (!string.IsNullOrWhiteSpace(DbEntry.Comments))
                newContentString.Append($"Comments: {DbEntry.Comments}");
            if (!string.IsNullOrWhiteSpace(DbEntry.Site))
                newContentString.Append($"Site: {DbEntry.Site}");
            if (!string.IsNullOrWhiteSpace(DbEntry.Author))
                newContentString.Append($"Author: {DbEntry.Author}");
            if (!string.IsNullOrWhiteSpace(DbEntry.Tags))
                newContentString.Append($"Tags: {DbEntry.Tags}");

            LinkContentString = newContentString.ToString();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}