using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ConversionDataEntry
{
    public class ConversionDataEntryContext<T> : INotifyPropertyChanged, IHasChanges, IHasValidationIssues
    {
        private Func<string, (bool passed, string conversionMessage, T result)> _converter;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private string _helpText;
        private T _referenceValue;
        private string _title;
        private string _userText;
        private T _userValue;

        private List<Func<T, (bool passed, string validationMessage)>> _validationFunctions =
            new List<Func<T, (bool passed, string validationMessage)>>();

        private string _validationMessage;

        public Func<string, (bool passed, string conversionMessage, T result)> Converter
        {
            get => _converter;
            set
            {
                if (Equals(value, _converter)) return;
                _converter = value;
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

        public string HelpText
        {
            get => _helpText;
            set
            {
                if (value == _helpText) return;
                _helpText = value;
                OnPropertyChanged();
            }
        }

        public T ReferenceValue
        {
            get => _referenceValue;
            set
            {
                if (value.Equals(_referenceValue)) return;
                _referenceValue = value;
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

        public string UserText
        {
            get => _userText;
            set
            {
                if (value == _userText) return;
                _userText = value;
                OnPropertyChanged();
            }
        }

        public T UserValue
        {
            get => _userValue;
            set
            {
                if (value.Equals(_userValue)) return;
                _userValue = value;
                OnPropertyChanged();
            }
        }

        public List<Func<T, (bool passed, string validationMessage)>> ValidationFunctions
        {
            get => _validationFunctions;
            set
            {
                if (Equals(value, _validationFunctions)) return;
                _validationFunctions = value;
                OnPropertyChanged();
            }
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChangesAndValidate()
        {
            if (Converter == null)
            {
                HasValidationIssues = true;
                ValidationMessage = "No conversion available";
                return;
            }

            var converted = Converter(UserText.TrimNullToEmpty());

            if (!converted.passed)
            {
                HasValidationIssues = true;
                ValidationMessage = converted.conversionMessage;
                return;
            }

            UserValue = converted.result;

            HasChanges = !ReferenceValue.Equals(UserValue);

            if (ValidationFunctions != null && ValidationFunctions.Any())
                foreach (var loopValidations in ValidationFunctions)
                {
                    var validationResult = loopValidations(UserValue);
                    if (!validationResult.passed)
                    {
                        HasValidationIssues = true;
                        ValidationMessage = validationResult.validationMessage;
                        return;
                    }
                }

            HasValidationIssues = false;
            ValidationMessage = string.Empty;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation")) CheckForChangesAndValidate();
        }
    }
}