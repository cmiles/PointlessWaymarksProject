#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarks.CmsWpfControls.FilesWrittenLogList
{
    public class FilesWrittenLogListListItem : INotifyPropertyChanged
    {
        private string _fileBase = string.Empty;
        private bool _isInGenerationDirectory;
        private string _transformedFile = string.Empty;
        private string _writtenFile = string.Empty;
        private DateTime _writtenOn;

        public string FileBase
        {
            get => _fileBase;
            set
            {
                if (value == _fileBase) return;
                _fileBase = value;
                OnPropertyChanged();
            }
        }

        public bool IsInGenerationDirectory
        {
            get => _isInGenerationDirectory;
            set
            {
                if (value == _isInGenerationDirectory) return;
                _isInGenerationDirectory = value;
                OnPropertyChanged();
            }
        }

        public string TransformedFile
        {
            get => _transformedFile;
            set
            {
                if (value == _transformedFile) return;
                _transformedFile = value;
                OnPropertyChanged();
            }
        }

        public string WrittenFile
        {
            get => _writtenFile;
            set
            {
                if (value == _writtenFile) return;
                _writtenFile = value;
                OnPropertyChanged();
            }
        }

        public DateTime WrittenOn
        {
            get => _writtenOn;
            set
            {
                if (value.Equals(_writtenOn)) return;
                _writtenOn = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}