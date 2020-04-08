using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarksCmsData
{
    public class UserSettings : INotifyPropertyChanged
    {
        private string _bingApiKey = string.Empty;
        private string _calTopoApiKey = string.Empty;
        private string _databaseFile;
        private string _defaultCreatedBy;
        private string _googleMapsApiKey = string.Empty;
        private string _localMasterMediaArchive;
        private string _localSiteRootDirectory;
        private string _pinboardApiToken;
        private string _siteAuthors;
        private string _siteEmailTo;
        private string _siteKeywords;
        private string _siteName;
        private string _siteSummary;
        private string _siteUrl;
        private string _pdfToCairoExeDirectory;

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

        public string GoogleMapsApiKey
        {
            get => _googleMapsApiKey;
            set
            {
                if (value == _googleMapsApiKey) return;
                _googleMapsApiKey = value;
                OnPropertyChanged();
            }
        }

        public string LocalMasterMediaArchive
        {
            get => _localMasterMediaArchive;
            set
            {
                if (value == _localMasterMediaArchive) return;
                _localMasterMediaArchive = value;
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}