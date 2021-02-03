using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarks.CmsData
{
    public class UserSettings : INotifyPropertyChanged
    {
        private string _bingApiKey = string.Empty;
        private string _calTopoApiKey = string.Empty;
        private string _databaseFile = string.Empty;
        private string _defaultCreatedBy = string.Empty;
        private double _latitudeDefault;
        private string _localMediaArchive = string.Empty;
        private string _localSiteRootDirectory = string.Empty;
        private double _longitudeDefault;
        private string _pdfToCairoExeDirectory = string.Empty;
        private string _pinboardApiToken = string.Empty;
        private Guid _settingsId;
        private string _siteAuthors = string.Empty;
        private string _siteEmailTo = string.Empty;
        private string _siteKeywords = string.Empty;
        private string _siteName = string.Empty;
        private string _siteS3Bucket = string.Empty;
        private string _siteS3BucketRegion = string.Empty;
        private string _siteSummary = string.Empty;
        private string _siteUrl = string.Empty;

        public string BingApiKey
        {
            get => _bingApiKey;
            set
            {
                if (value == _bingApiKey) return;
                _bingApiKey = value;
                OnPropertyChanged();
            }
        }

        public string CalTopoApiKey
        {
            get => _calTopoApiKey;
            set
            {
                if (value == _calTopoApiKey) return;
                _calTopoApiKey = value;
                OnPropertyChanged();
            }
        }

        public string DatabaseFile
        {
            get => _databaseFile;
            set
            {
                if (value == _databaseFile) return;
                _databaseFile = value;
                OnPropertyChanged();
            }
        }

        public string DefaultCreatedBy
        {
            get => _defaultCreatedBy;
            set
            {
                if (value == _defaultCreatedBy) return;
                _defaultCreatedBy = value;
                OnPropertyChanged();
            }
        }

        public double LatitudeDefault
        {
            get => _latitudeDefault;
            set
            {
                if (value.Equals(_latitudeDefault)) return;
                _latitudeDefault = value;
                OnPropertyChanged();
            }
        }

        public string LocalMediaArchive
        {
            get => _localMediaArchive;
            set
            {
                if (value == _localMediaArchive) return;
                _localMediaArchive = value;
                OnPropertyChanged();
            }
        }

        public string LocalSiteRootDirectory
        {
            get => _localSiteRootDirectory;
            set
            {
                if (value == _localSiteRootDirectory) return;
                _localSiteRootDirectory = value;
                OnPropertyChanged();
            }
        }

        public double LongitudeDefault
        {
            get => _longitudeDefault;
            set
            {
                if (value.Equals(_longitudeDefault)) return;
                _longitudeDefault = value;
                OnPropertyChanged();
            }
        }

        public string PdfToCairoExeDirectory
        {
            get => _pdfToCairoExeDirectory;
            set
            {
                if (value == _pdfToCairoExeDirectory) return;
                _pdfToCairoExeDirectory = value;
                OnPropertyChanged();
            }
        }

        public string PinboardApiToken
        {
            get => _pinboardApiToken;
            set
            {
                if (value == _pinboardApiToken) return;
                _pinboardApiToken = value;
                OnPropertyChanged();
            }
        }

        public Guid SettingsId
        {
            get => _settingsId;
            set
            {
                if (value.Equals(_settingsId)) return;
                _settingsId = value;
                OnPropertyChanged();
            }
        }

        public string SiteAuthors
        {
            get => _siteAuthors;
            set
            {
                if (value == _siteAuthors) return;
                _siteAuthors = value;
                OnPropertyChanged();
            }
        }

        public string SiteEmailTo
        {
            get => _siteEmailTo;
            set
            {
                if (value == _siteEmailTo) return;
                _siteEmailTo = value;
                OnPropertyChanged();
            }
        }

        public string SiteKeywords
        {
            get => _siteKeywords;
            set
            {
                if (value == _siteKeywords) return;
                _siteKeywords = value;
                OnPropertyChanged();
            }
        }

        public string SiteName
        {
            get => _siteName;
            set
            {
                if (value == _siteName) return;
                _siteName = value;
                OnPropertyChanged();
            }
        }

        public string SiteS3Bucket
        {
            get => _siteS3Bucket;
            set
            {
                if (value == _siteS3Bucket) return;
                _siteS3Bucket = value;
                OnPropertyChanged();
            }
        }

        public string SiteS3BucketRegion
        {
            get => _siteS3BucketRegion;
            set
            {
                if (value == _siteS3BucketRegion) return;
                _siteS3BucketRegion = value;
                OnPropertyChanged();
            }
        }

        public string SiteSummary
        {
            get => _siteSummary;
            set
            {
                if (value == _siteSummary) return;
                _siteSummary = value;
                OnPropertyChanged();
            }
        }

        public string SiteUrl
        {
            get => _siteUrl;
            set
            {
                if (value == _siteUrl) return;
                _siteUrl = value;
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