using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility;

namespace PointlessWaymarks.CmsWpfControls.LinkList
{
    public class LinkListListItem : IContentListItem
    {
        private LinkContent _dbEntry;
        private LinkListItemActions _itemActions;
        private string _linkContentString;
        private CurrentSelectedTextTracker _selectedTextTracker = new();

        private bool _showType;

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

        public LinkListItemActions ItemActions
        {
            get => _itemActions;
            set
            {
                if (Equals(value, _itemActions)) return;
                _itemActions = value;
                OnPropertyChanged();
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

        public bool ShowType
        {
            get => _showType;
            set
            {
                if (value == _showType) return;
                _showType = value;
                OnPropertyChanged();
            }
        }

        public Guid? ContentId()
        {
            return DbEntry?.ContentId;
        }

        public IContentCommon Content()
        {
            return new ContentCommonShell
            {
                Summary = string.Join(Environment.NewLine, new List<string>
                {
                    DbEntry.Title,
                    DbEntry.Site,
                    DbEntry.Url,
                    DbEntry.Author,
                    DbEntry.Description,
                    DbEntry.Comments
                }),
                Title = DbEntry.Title,
                ContentId = DbEntry.ContentId,
                ContentVersion = DbEntry.ContentVersion,
                Id = DbEntry.Id,
                CreatedBy = DbEntry.CreatedBy,
                CreatedOn = DbEntry.CreatedOn,
                LastUpdatedBy = DbEntry.LastUpdatedBy,
                LastUpdatedOn = DbEntry.LastUpdatedOn,
                Tags = DbEntry.Tags
            };
        }

        public string DefaultBracketCode()
        {
            if (DbEntry?.ContentId == null || ItemActions == null) return string.Empty;
            return $"[{DbEntry.Title}]({DbEntry.Url})";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CurrentSelectedTextTracker SelectedTextTracker
        {
            get => _selectedTextTracker;
            set
            {
                if (Equals(value, _selectedTextTracker)) return;
                _selectedTextTracker = value;
                OnPropertyChanged();
            }
        }

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