using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;

namespace PointlessWaymarks.CmsWpfControls.BoolDataEntry
{
    public class BoolNullableDataEntryContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues
    {
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private string _helpText;
        private bool? _referenceValue;
        private string _title;
        private bool? _userValue;

        private List<Func<bool?, IsValid>> _validationFunctions = new();

        private string _validationMessage;

        private BoolNullableDataEntryContext()
        {
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

        public bool? ReferenceValue
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

        public bool? UserValue
        {
            get => _userValue;
            set
            {
                if (value == _userValue) return;
                _userValue = value;
                OnPropertyChanged();
            }
        }

        public bool UserValueIsNullable => true;

        public List<Func<bool?, IsValid>> ValidationFunctions
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void CheckForChangesAndValidate()
        {
            HasChanges = UserValue != ReferenceValue;

            if (ValidationFunctions != null && ValidationFunctions.Any())
                foreach (var loopValidations in ValidationFunctions)
                {
                    var validationResult = loopValidations(UserValue);
                    if (!validationResult.Valid)
                    {
                        HasValidationIssues = true;
                        ValidationMessage = validationResult.Explanation;
                        return;
                    }
                }

            HasValidationIssues = false;
            ValidationMessage = string.Empty;
        }

        public static BoolNullableDataEntryContext CreateInstance()
        {
            return new();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidate();
        }
    }
}