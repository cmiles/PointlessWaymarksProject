using System.ComponentModel;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

[NotifyPropertyChanged]
public partial class DateTimeSearchFieldBuilder
{
    public DateTimeSearchFieldBuilder()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public bool EnableDateTimeTwo { get; set; }
    public required string FieldTitle { get; set; }
    public bool Not { get; set; }
    public List<string> OperatorChoices { get; set; } = ["==", ">", ">=", "<", "<="];
    public string SelectedOperatorOne { get; set; } = "==";
    public string? SelectedOperatorTwo { get; set; } = "<";
    public bool ShowDateTimeOneTextWarning { get; set; }
    public bool ShowDateTimeTwoTextWarning { get; set; }
    public bool UserDateTimeOneTextConverts { get; set; }
    public string UserDateTimeOneTranslation { get; set; }
    public string UserDateTimeTextOne { get; set; } = string.Empty;
    public string UserDateTimeTextTwo { get; set; } = string.Empty;
    public bool UserDateTimeTwoTextConverts { get; set; }
    public string UserDateTimeTwoTranslation { get; set; }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName?.Equals(nameof(UserDateTimeTextOne)) ?? false)
        {
            var parseOneResults = TextParses(UserDateTimeTextOne, SelectedOperatorOne);
            UserDateTimeOneTextConverts = parseOneResults.Item1;
            UserDateTimeOneTranslation = parseOneResults.Item2;
            ShowDateTimeOneTextWarning =
                !string.IsNullOrWhiteSpace(UserDateTimeTextOne) && !UserDateTimeOneTextConverts;
        }

        if ((e.PropertyName?.Equals(nameof(UserDateTimeTextOne)) ?? false) ||
            (e.PropertyName?.Equals(nameof(SelectedOperatorOne)) ?? false))
            EnableDateTimeTwo = UserDateTimeOneTextConverts && SelectedOperatorOne != "==";

        if (e.PropertyName?.Equals(nameof(UserDateTimeTextTwo)) ?? false)
        {
            var parseTwoResults = TextParses(UserDateTimeTextTwo, SelectedOperatorTwo ?? "");
            UserDateTimeTwoTextConverts = parseTwoResults.Item1;
            UserDateTimeTwoTranslation = parseTwoResults.Item2;
            ShowDateTimeTwoTextWarning = EnableDateTimeTwo && (!UserDateTimeOneTextConverts ||
                                                               (!string.IsNullOrWhiteSpace(UserDateTimeTextTwo) &&
                                                                !UserDateTimeTwoTextConverts));
        }
    }

    private (bool, string) TextParses(string searchString, string operatorChoice)
    {
        var dateTimeParse = DateTimeRecognizer.RecognizeDateTime(searchString, Culture.English,
            DateTimeOptions.None, DateTime.Now);

        if (dateTimeParse.Count == 0 || dateTimeParse[0].Resolution.Count == 0) return (false, string.Empty);

        if (dateTimeParse[0].TypeName == "datetimeV2.daterange")
        {
            var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
            if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                valuesDictionary.Count < 1 ||
                !valuesDictionary[0].TryGetValue("start", out var searchStartDateTimeString) ||
                !DateTime.TryParse(searchStartDateTimeString, out var searchStartDateTime) ||
                !valuesDictionary[0].TryGetValue("end", out var searchEndDateTimeString) ||
                !DateTime.TryParse(searchEndDateTimeString, out var searchEndDateTime))
                return (false, string.Empty);

            switch (operatorChoice)
            {
                case "":
                    return (true, $">= {searchStartDateTime} and < {searchEndDateTime}");
                case "==":
                    return (true, $">= {searchStartDateTime} and < {searchEndDateTime}");
                case "!=":
                    return (true, $"< {searchStartDateTime} and >= {searchEndDateTime}");
                case ">":
                    return (true, $"> {searchEndDateTime}");
                case ">=":
                    return (true, $">= {searchEndDateTime}");
                case "<":
                    return (true, $"< {searchEndDateTime}");
                case "<=":
                    return (true, $"<= {searchEndDateTime}");
            }
        }

        if (dateTimeParse[0].TypeName == "datetimeV2.date")
        {
            var valuesFound = dateTimeParse[0].Resolution.TryGetValue("values", out var valuesObject);
            if (!valuesFound || valuesObject is not List<Dictionary<string, string>> valuesDictionary ||
                valuesDictionary.Count < 1 ||
                !valuesDictionary[0].TryGetValue("value", out var searchDateTimeString) ||
                !DateTime.TryParse(searchDateTimeString, out var searchDateTime))
                return (false, string.Empty);

            return (true, $"{operatorChoice} {searchDateTime.Date}");
        }

        return (false, string.Empty);
    }
}