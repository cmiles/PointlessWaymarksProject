using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.BoolDataEntry
{
    public class BoolDataEntryContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues
    {
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private string _helpText;
        private bool _referenceValue;
        private string _title;
        private bool _userValue;

        private List<Func<bool, (bool passed, string validationMessage)>> _validationFunctions =
            new List<Func<bool, (bool passed, string validationMessage)>>();

        private string _validationMessage;

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

        public bool ReferenceValue
        {
            get => _referenceValue;
            set
            {
                if (value == _referenceValue) return;
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

        public bool UserValue
        {
            get => _userValue;
            set
            {
                if (value == _userValue) return;
                _userValue = value;
                OnPropertyChanged();
            }
        }

        public List<Func<bool, (bool passed, string validationMessage)>> ValidationFunctions
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
            HasChanges = UserValue != ReferenceValue;

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