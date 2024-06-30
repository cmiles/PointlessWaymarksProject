using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;

namespace PointlessWaymarks.PowerShellRunnerGui.PowerShellEditor;

//Code copied from or based on [GitHub - dfinke/PowerShellConsole: Create a PowerShell Console using the AvalonEdit control](https://github.com/dfinke/PowerShellConsole/tree/master)
//[dfinke (Doug Finke)](https://github.com/dfinke) -  Apache-2.0 license 
public sealed class TextMarker : TextSegment
{
    private readonly TextMarkerService _service;

    private Color? _backgroundColor;

    private Color? _foregroundColor;

    private Color _markerColor;

    private TextMarkerType _markerType;

    public TextMarker(TextMarkerService? service, int startOffset, int length)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        StartOffset = startOffset;
        Length = length;
        _markerType = TextMarkerType.None;
    }

    public Color? BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (_backgroundColor != value)
            {
                _backgroundColor = value;
                Redraw();
            }
        }
    }

    public Color? ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (_foregroundColor != value)
            {
                _foregroundColor = value;
                Redraw();
            }
        }
    }

    public bool IsDeleted => !IsConnectedToCollection;

    public Color MarkerColor
    {
        get => _markerColor;
        set
        {
            if (_markerColor != value)
            {
                _markerColor = value;
                Redraw();
            }
        }
    }

    public TextMarkerType MarkerType
    {
        get => _markerType;
        set
        {
            if (_markerType != value)
            {
                _markerType = value;
                Redraw();
            }
        }
    }

    public object? Tag { get; set; }

    public object? ToolTip { get; set; }

    public void Delete()
    {
        _service.Remove(this);
    }

    public event EventHandler? Deleted;

    internal void OnDeleted()
    {
        if (Deleted != null)
            Deleted(this, EventArgs.Empty);
    }

    private void Redraw()
    {
        _service.Redraw(this);
    }
}