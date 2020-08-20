using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AngleSharp.Text;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Database;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public class TextBoxDoubleClickLauncher : Behavior<TextBox>
    {
        private void AssociatedObjectOnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is TextBox textBox)) return;

            if (string.IsNullOrWhiteSpace(textBox.Text)) return;

            var mousePoint = Mouse.GetPosition(textBox);
            var characterPosition = AssociatedObject.GetCharacterIndexFromPoint(mousePoint, true);

            if (characterPosition < 0) return;

            var text = textBox.Text;

            var startingCharacter = text[characterPosition];

            if (startingCharacter.IsWhiteSpaceCharacter()) return;

            var before = new List<char>();

            for (var i = characterPosition - 1; i >= 0; i--)
            {
                var newCharacter = text[i];
                if (newCharacter.IsWhiteSpaceCharacter()) break;
                before.Add(newCharacter);
            }

            before.Reverse();

            var after = new List<char>();

            for (var i = characterPosition; i < text.Length; i++)
            {
                var newCharacter = text[i];
                if (newCharacter.IsWhiteSpaceCharacter()) break;
                after.Add(newCharacter);
            }

            var finalString = new string(before.Concat(after).ToArray());

            if (finalString.Length < 3) return;

            var removeOuterContainers = true;

            while (removeOuterContainers)
            {
                var removal = StripContainerCharacters(finalString);
                if (removal.removed)
                {
                    finalString = removal.returnString;
                    if (finalString.Length < 3) return;
                }
                else
                {
                    removeOuterContainers = false;
                }
            }

            (bool removed, string returnString) StripContainerCharacters(string toCheck)
            {
                if (toCheck.StartsWith("(") && toCheck.EndsWith(")"))
                    return (true, toCheck.Substring(1, toCheck.Length - 2).TrimNullToEmpty());
                if (toCheck.StartsWith("[") && toCheck.EndsWith("]"))
                    return (true, toCheck.Substring(1, toCheck.Length - 2).TrimNullToEmpty());
                if (toCheck.StartsWith("{") && toCheck.EndsWith("}"))
                    return (true, toCheck.Substring(1, toCheck.Length - 2).TrimNullToEmpty());
                if (toCheck.StartsWith("\"") && toCheck.EndsWith("\""))
                    return (true, toCheck.Substring(1, toCheck.Length - 2).TrimNullToEmpty());
                if (toCheck.StartsWith("'") && toCheck.EndsWith("'"))
                    return (true, toCheck.Substring(1, toCheck.Length - 2).TrimNullToEmpty());
                if (toCheck.StartsWith(",") && toCheck.EndsWith(","))
                    return (true, toCheck.Substring(1, toCheck.Length - 2).TrimNullToEmpty());

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
                        EventLogContext.TryWriteExceptionToLogBlocking(ex, "TextBoxDoubleClickLauncher",
                            $"Trying to process start {finalString}");
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
                        EventLogContext.TryWriteExceptionToLogBlocking(ex, "TextBoxDoubleClickLauncher",
                            $"Trying to process start {finalString}");
                    }
                }));
            }
            catch (Exception ex)
            {
                EventLogContext.TryWriteExceptionToLogBlocking(ex, "TextBoxDoubleClickLauncher",
                    $"Trying to process start {finalString}");
            }
        }

        protected override void OnAttached()
        {
            AssociatedObject.MouseDoubleClick += AssociatedObjectOnMouseDoubleClick;
        }
    }
}