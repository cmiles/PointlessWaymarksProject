using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ContentFormat
{
    public class ContentFormatChooserContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues
    {
        private List<ContentFormatEnum> _contentFormatChoices;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private string _initialValue;

        private ContentFormatEnum _selectedContentFormat;
        private string _selectedContentFormatAsString;
        private bool _selectedContentFormatHasChanges;
        private StatusControlContext _statusContext;
        private string _validationMessage;

        private ContentFormatChooserContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            ContentFormatChoices = Enum.GetValues(typeof(ContentFormatEnum)).Cast<ContentFormatEnum>().ToList();
            SelectedContentFormat = ContentFormatChoices.First();
        }

        public List<ContentFormatEnum> ContentFormatChoices
        {
            get => _contentFormatChoices;
            set
            {
                if (Equals(value, _contentFormatChoices)) return;
                _contentFormatChoices = value;
                OnPropertyChanged();
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool SelectedContentFormatHasChanges
        {
            get => _selectedContentFormatHasChanges;
            set
            {
                if (value == _selectedContentFormatHasChanges) return;
                _selectedContentFormatHasChanges = value;
                OnPropertyChanged();
            }
        }

        public void CheckForChangesAndValidationIssues()
        {
            // ReSharper disable InvokeAsExtensionMethod - in this case TrimNullSage - which returns an
            //Empty string from null will not be invoked as an extension if DbEntry is null...
            SelectedContentFormatHasChanges = StringHelpers.TrimNullToEmpty(InitialValue) !=
                                              SelectedContentFormatAsString.TrimNullToEmpty();
            // ReSharper restore InvokeAsExtensionMethod

            HasChanges = SelectedContentFormatHasChanges;
            var validation =
                CommonContentValidation.ValidateBodyContentFormat(SelectedContentFormatAsString.TrimNullToEmpty());
            HasValidationIssues = !validation.isValid;
            ValidationMessage = validation.explanation;
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                if (value == _validationMessage) return;
                _validationMessage = value;
                OnPropertyChanged();
            }
        }

        public static ContentFormatChooserContext CreateInstance(StatusControlContext statusContext)
        {
            return new ContentFormatChooserContext(statusContext);
        }

        public ContentFormatEnum SelectedContentFormat
        {
            get => _selectedContentFormat;
            set
            {
                if (value != _selectedContentFormat)
                {
                    _selectedContentFormat = value;
                    OnPropertyChanged();

                    OnSelectedValueChanged?.Invoke(this,
                        Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat));
                }

                SelectedContentFormatAsString =
                    Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat) ?? string.Empty;
            }
        }

        public string SelectedContentFormatAsString
        {
            get => _selectedContentFormatAsString;
            set
            {
                if (value == _selectedContentFormatAsString) return;
                _selectedContentFormatAsString = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public string InitialValue
        {
            get => _initialValue;
            set
            {
                if (value == _initialValue) return;
                _initialValue = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }

        public async Task<bool> TrySelectContentChoice(string contentChoice)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(contentChoice))
            {
                SelectedContentFormat = ContentFormatDefaults.Content;
                return true;
            }

            var toSelect = Enum.TryParse(typeof(ContentFormatEnum), contentChoice, true, out var parsedSelection);
            if (toSelect && parsedSelection != null) SelectedContentFormat = (ContentFormatEnum) parsedSelection;
            return toSelect;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<string> OnSelectedValueChanged;

        public bool HasValidationIssues
        {
            get => _hasValidationIssues;
            set
            {
                if (value == _hasValidationIssues) return;
                _hasValidationIssues = value;
                OnPropertyChanged();
            }
        }
    }
}