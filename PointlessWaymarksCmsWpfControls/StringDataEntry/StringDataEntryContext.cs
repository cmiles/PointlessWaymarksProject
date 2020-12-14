using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsWpfControls.Utility.ChangesAndValidation;

namespace PointlessWaymarksCmsWpfControls.StringDataEntry
{
    public class StringDataEntryContext : INotifyPropertyChanged, IHasChanges, IHasValidationIssues
    {
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private string _helpText;
        private string _referenceValue;
        private string _title;
        private string _userValue;

        private List<Func<string, (bool passed, string validationMessage)>> _validationFunctions = new();

        private string _validationMessage;

        private StringDataEntryContext()
        {
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

        public string ReferenceValue
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

        public string UserValue
        {
            get => _userValue;
            set
            {
                if (value == _userValue) return;
                _userValue = value;
                OnPropertyChanged();
            }
        }

        public List<Func<string, (bool passed, string validationMessage)>> ValidationFunctions
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

        private void CheckForChangesAndValidationIssues()
        {
            HasChanges = UserValue.TrimNullToEmpty() != ReferenceValue.TrimNullToEmpty();

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

        public static StringDataEntryContext CreateInstance()
        {
            return new();
        }

        public static StringDataEntryContext CreateSlugInstance(ITitleSummarySlugFolder dbEntry)
        {
            var slugEntry = new StringDataEntryContext
            {
                Title = "Slug",
                HelpText = "This will be the Folder and File Name used in URLs - limited to a-z 0-9 _ -",
                ReferenceValue = dbEntry?.Slug ?? string.Empty,
                UserValue = StringHelpers.NullToEmptyTrim(dbEntry?.Slug),
                ValidationFunctions = new List<Func<string, (bool passed, string validationMessage)>>
                {
                    CommonContentValidation.ValidateSlugLocal
                }
            };

            slugEntry.CheckForChangesAndValidationIssues();

            return slugEntry;
        }

        public static StringDataEntryContext CreateSummaryInstance(ITitleSummarySlugFolder dbEntry)
        {
            var summaryEntry = new StringDataEntryContext
            {
                Title = "Summary",
                HelpText = "A short text entry that will show in Search and short references to the content",
                ReferenceValue = dbEntry?.Summary ?? string.Empty,
                UserValue = StringHelpers.NullToEmptyTrim(dbEntry?.Summary),
                ValidationFunctions = new List<Func<string, (bool passed, string validationMessage)>>
                {
                    CommonContentValidation.ValidateSummary
                }
            };

            summaryEntry.CheckForChangesAndValidationIssues();

            return summaryEntry;
        }

        public static StringDataEntryContext CreateTitleInstance(ITitleSummarySlugFolder dbEntry)
        {
            var titleEntry = new StringDataEntryContext
            {
                Title = "Title",
                HelpText = "Title Text",
                ReferenceValue = dbEntry?.Title ?? string.Empty,
                UserValue = StringHelpers.NullToEmptyTrim(dbEntry?.Title),
                ValidationFunctions = new List<Func<string, (bool passed, string validationMessage)>>
                {
                    CommonContentValidation.ValidateTitle
                }
            };

            titleEntry.CheckForChangesAndValidationIssues();

            return titleEntry;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }
    }
}