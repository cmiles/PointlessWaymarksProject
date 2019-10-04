using JetBrains.Annotations;
using NetTopologySuite.Geometries;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheLemmonWorkshopData;
using TheLemmonWorkshopWpfControls.WpfHtml;

namespace TheLemmonWorkshopWpfControls.Models
{
    public class UserSiteContent : INotifyPropertyChanged
    {
        private string _bodyContent;
        private string _bodyContentFormat;
        private string _bodyContentHtmlOutput;
        private string _code;
        private string _contentType;
        private string _createdBy;
        private DateTime _createdOn;
        private Guid _fingerprint;
        private int _id;
        private string _lastUpdatedBy;
        private DateTime? _lastUpdatedOn;
        private Geometry _locationData;
        private string _locationDataType;
        private string _mainImage;
        private string _mainImageFormat;
        private string _mainImageHtmlOutput;
        private string _summary;
        private string _title;
        private string _updateNotes;
        private string _updateNotesFormat;
        private string _updateNotesHtmlOutput;

        public event PropertyChangedEventHandler PropertyChanged;

        public string BodyContent
        {
            get => _bodyContent;
            set
            {
                if (value == _bodyContent) return;
                _bodyContent = value;
                OnPropertyChanged();

                UpdateBodyContentHtml();
            }
        }

        public string BodyContentFormat
        {
            get => _bodyContentFormat;
            set
            {
                if (value == _bodyContentFormat) return;
                _bodyContentFormat = value;
                OnPropertyChanged();

                UpdateBodyContentHtml();
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

        public string Code
        {
            get => _code;
            set
            {
                if (value == _code) return;
                _code = value;
                OnPropertyChanged();
            }
        }

        public string ContentType
        {
            get => _contentType;
            set
            {
                if (value == _contentType) return;
                _contentType = value;
                OnPropertyChanged();
            }
        }

        public string CreatedBy
        {
            get => _createdBy;
            set
            {
                if (value == _createdBy) return;
                _createdBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedOn
        {
            get => _createdOn;
            set
            {
                if (value.Equals(_createdOn)) return;
                _createdOn = value;
                OnPropertyChanged();
            }
        }

        public Guid Fingerprint
        {
            get => _fingerprint;
            set
            {
                if (value.Equals(_fingerprint)) return;
                _fingerprint = value;
                OnPropertyChanged();
            }
        }

        public int Id
        {
            get => _id;
            set
            {
                if (value == _id) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public string LastUpdatedBy
        {
            get => _lastUpdatedBy;
            set
            {
                if (value == _lastUpdatedBy) return;
                _lastUpdatedBy = value;
                OnPropertyChanged();
            }
        }

        public DateTime? LastUpdatedOn
        {
            get => _lastUpdatedOn;
            set
            {
                if (Nullable.Equals(value, _lastUpdatedOn)) return;
                _lastUpdatedOn = value;
                OnPropertyChanged();
            }
        }

        public Geometry LocationData
        {
            get => _locationData;
            set
            {
                if (Equals(value, _locationData)) return;
                _locationData = value;
                OnPropertyChanged();
            }
        }

        public string LocationDataType
        {
            get => _locationDataType;
            set
            {
                if (value == _locationDataType) return;
                _locationDataType = value;
                OnPropertyChanged();
            }
        }

        public string MainImage
        {
            get => _mainImage;
            set
            {
                if (value == _mainImage) return;
                _mainImage = value;
                OnPropertyChanged();

                UpdateMainImageHtml();
            }
        }

        public string MainImageFormat
        {
            get => _mainImageFormat;
            set
            {
                if (value == _mainImageFormat) return;
                _mainImageFormat = value;
                OnPropertyChanged();

                UpdateMainImageHtml();
            }
        }

        public string MainImageHtmlOutput
        {
            get => _mainImageHtmlOutput;
            set
            {
                if (value == _mainImageHtmlOutput) return;
                _mainImageHtmlOutput = value;
                OnPropertyChanged();
            }
        }

        public string Summary
        {
            get => _summary;
            set
            {
                if (value == _summary) return;
                _summary = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public string UpdateNotes
        {
            get => _updateNotes;
            set
            {
                if (value == _updateNotes) return;
                _updateNotes = value;
                OnPropertyChanged();

                UpdateUpdateNotesContentHtml();
            }
        }

        public string UpdateNotesFormat
        {
            get => _updateNotesFormat;
            set
            {
                if (value == _updateNotesFormat) return;
                _updateNotesFormat = value;
                OnPropertyChanged();

                UpdateUpdateNotesContentHtml();
            }
        }

        public string UpdateNotesHtmlOutput
        {
            get => _updateNotesHtmlOutput;
            set
            {
                if (value == _updateNotesHtmlOutput) return;
                _updateNotesHtmlOutput = value;
                OnPropertyChanged();
            }
        }

        public void CleanStrings()
        {
            BodyContent = CoalesceTrim(BodyContent);
            BodyContentFormat = CoalesceTrim(BodyContentFormat);
            Code = CoalesceTrim(Code);
            ContentType = CoalesceTrim(ContentType);
            CreatedBy = CoalesceTrim(CreatedBy);
            LastUpdatedBy = CoalesceTrim(LastUpdatedBy);
            LocationDataType = CoalesceTrim(LocationDataType);
            MainImage = CoalesceTrim(MainImage);
            MainImageFormat = CoalesceTrim(MainImageFormat);
            Summary = CoalesceTrim(Summary);
            Title = CoalesceTrim(Title);
            UpdateNotes = CoalesceTrim(UpdateNotes);
            UpdateNotesFormat = CoalesceTrim(UpdateNotesFormat);
        }

        public void UpdateBodyContentHtml()
        {
            var (success, output) = ContentProcessor.ContentHtml(BodyContentFormat, BodyContent);
            if (success)
            {
                BodyContentHtmlOutput = output.ToHtmlDocument("Body Content", string.Empty);
                return;
            }
            BodyContentHtmlOutput = "<h2>Not able to process input</h2>".ToHtmlDocument("Invalid", string.Empty);
        }

        public void UpdateMainImageHtml()
        {
            var (success, output) = MainImageProcessor.MainImageHtml(MainImageFormat, MainImage);
            if (success)
            {
                MainImageHtmlOutput = output.ToHtmlDocument("Main Image", string.Empty);
                return;
            }
            MainImageHtmlOutput = "<h2>Not able to process input</h2>".ToHtmlDocument("Invalid", string.Empty);
        }

        public void UpdateUpdateNotesContentHtml()
        {
            var (success, output) = ContentProcessor.ContentHtml(UpdateNotesFormat, UpdateNotes);
            if (success)
            {
                UpdateNotesHtmlOutput = output.ToHtmlDocument("Update Notes", string.Empty);
                return;
            }
            UpdateNotesHtmlOutput = "<h2>Not able to process input</h2>".ToHtmlDocument("Invalid", string.Empty);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string CoalesceTrim(string toModify)
        {
            if (string.IsNullOrWhiteSpace(toModify)) return string.Empty;
            return toModify.Trim();
        }
    }
}