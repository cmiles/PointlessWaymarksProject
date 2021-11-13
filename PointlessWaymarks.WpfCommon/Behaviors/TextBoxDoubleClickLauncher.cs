using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarks.WpfCommon.Behaviors;

public class TextBoxDoubleClickLauncher : Behavior<TextBox>
{
    private void AssociatedObjectOnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
#pragma warning disable IDE0083 // Use pattern matching
        if (sender is not TextBox textBox) return;
#pragma warning restore IDE0083 // Use pattern matching

        if (string.IsNullOrWhiteSpace(textBox.Text)) return;

        var mousePoint = Mouse.GetPosition(textBox);
        var characterPosition = AssociatedObject.GetCharacterIndexFromPoint(mousePoint, true);

        if (characterPosition < 0) return;

        var text = textBox.Text;

        var startingCharacter = text[characterPosition];

        if (char.IsWhiteSpace(startingCharacter)) return;

        var before = new List<char>();

        for (var i = characterPosition - 1; i >= 0; i--)
        {
            var newCharacter = text[i];
            if (char.IsWhiteSpace(newCharacter)) break;
            before.Add(newCharacter);
        }

        before.Reverse();

        var after = new List<char>();

        for (var i = characterPosition; i < text.Length; i++)
        {
            var newCharacter = text[i];
            if (char.IsWhiteSpace(newCharacter)) break;
            after.Add(newCharacter);
        }

        var finalString = new string(before.Concat(after).ToArray());

        if (finalString.Length < 3) return;

        var removeOuterContainers = true;

        while (removeOuterContainers)
        {
            var (removed, returnString) = StripContainerCharacters(finalString);
            if (removed)
            {
                finalString = returnString;
                if (finalString.Length < 3) return;
            }
            else
            {
                removeOuterContainers = false;
            }
        }

        string TrimNullToEmpty(string toTrim)
        {
            return string.IsNullOrWhiteSpace(toTrim) ? string.Empty : toTrim.Trim();
        }

        (bool removed, string returnString) StripContainerCharacters(string toCheck)
        {
            if (toCheck.StartsWith("(") && toCheck.EndsWith(")"))
                return (true, TrimNullToEmpty(toCheck[1..^1]));
            if (toCheck.StartsWith("[") && toCheck.EndsWith("]"))
                return (true, TrimNullToEmpty(toCheck[1..^1]));
            if (toCheck.StartsWith("{") && toCheck.EndsWith("}"))
                return (true, TrimNullToEmpty(toCheck[1..^1]));
            if (toCheck.StartsWith("\"") && toCheck.EndsWith("\""))
                return (true, TrimNullToEmpty(toCheck[1..^1]));
            if (toCheck.StartsWith("'") && toCheck.EndsWith("'"))
                return (true, TrimNullToEmpty(toCheck[1..^1]));
            if (toCheck.StartsWith(",") && toCheck.EndsWith(","))
                return (true, TrimNullToEmpty(toCheck[1..^1]));

            return (false, toCheck);
        }


        var firstHttpUrlMatch = Regex.Match(finalString,
            @"\b(https?)://[-A-Z0-9+&@#/%?=~_|$!:,.;]*[A-Z0-9+&@#/%=~_|$]", RegexOptions.IgnoreCase);
        if (firstHttpUrlMatch.Success)
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var ps = new ProcessStartInfo(firstHttpUrlMatch.Value) {UseShellExecute = true, Verb = "open"};
                    Process.Start(ps);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }));

        var firstFileMatch = Regex.Match(finalString, @"(?<drive> \b[a-z]:\\)
		(?<folder>(?>[^\\/:*?""<>|\x00-\x1F]{0,254}[^.\\/:*?""<>|\x00-\x1F]\\)*)
		(?<file>  (?>[^\\/:*?""<>|\x00-\x1F]{0,254}[^.\\/:*?""<>|\x00-\x1F])?)",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        if (!firstFileMatch.Success || !firstFileMatch.Groups["drive"].Success ||
            !firstFileMatch.Groups["folder"].Success) return;

        try
        {
            var directory = new DirectoryInfo(Path.Combine(firstFileMatch.Groups["drive"].Captures[0].Value,
                firstFileMatch.Groups["folder"].Captures[0].Value));

            if (!directory.Exists) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var ps = new ProcessStartInfo(directory.FullName) {UseShellExecute = true, Verb = "open"};
                    Process.Start(ps);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    protected override void OnAttached()
    {
        AssociatedObject.MouseDoubleClick += AssociatedObjectOnMouseDoubleClick;
    }
}