using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace PointlessWaymarks.CmsWpfControls.HelpDisplay;

public class HelpDisplayContext : INotifyPropertyChanged
{
    private string _helpMarkdownContent;

    public HelpDisplayContext(List<string> markdownHelp)
    {
        if (markdownHelp == null || !markdownHelp.Any()) HelpMarkdownContent = string.Empty;
        else
            HelpMarkdownContent = string.Join(Environment.NewLine + Environment.NewLine,
                markdownHelp.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    public string HelpMarkdownContent
    {
        get => _helpMarkdownContent;
        set
        {
            if (value == _helpMarkdownContent) return;
            _helpMarkdownContent = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}