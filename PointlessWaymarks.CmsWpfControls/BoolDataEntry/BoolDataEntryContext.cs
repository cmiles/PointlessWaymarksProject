using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;

namespace PointlessWaymarks.CmsWpfControls.BoolDataEntry
{
    public class BoolDataEntryContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues
    {
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private string _helpText;
        private bool _isEnabled = true;
        private bool _referenceValue;
        private string _title;
        private bool _userValue;

        private List<Func<bool, IsValid>> _validationFunctions = new();

        private string _validationMessage;

        private BoolDataEntryContext()
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

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value == _isEnabled) return;
                _isEnabled = value;
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

        public bool UserValueIsNullable => false;

        public List<Func<bool, IsValid>> ValidationFunctions
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

        public static BoolDataEntryContext CreateInstance()
        {
            return new BoolDataEntryContext();
        }

        public static BoolDataEntryContext CreateInstanceForShowInSearch(IShowInSearch dbEntry, bool defaultSetting)
        {
            var newContext = new BoolDataEntryContext
            {
                ReferenceValue = dbEntry?.ShowInSearch ?? defaultSetting,
                UserValue = dbEntry?.ShowInSearch ?? defaultSetting,
                Title = "Show in Search",
                HelpText =
                    "If checked the content will appear in Site, Tag and other search screens - otherwise the content will still be " +
                    "on the site and publicly available but it will not show in search"
            };

            return newContext;
        }

        public static BoolDataEntryContext CreateInstanceForShowInSiteFeed(IShowInSiteFeed dbEntry, bool defaultSetting)
        {
            var newContext = new BoolDataEntryContext
            {
                ReferenceValue = dbEntry?.ShowInMainSiteFeed ?? defaultSetting,
                UserValue = dbEntry?.ShowInMainSiteFeed ?? defaultSetting,
                Title = "Show in Main Site Feed",
                HelpText =
                    "Checking this box will make the content appear in the Main Site RSS Feed and - if the content is recent - on the site's homepage"
            };

            return newContext;
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