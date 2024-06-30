// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

//Code copied from or based on [GitHub - dfinke/PowerShellConsole: Create a PowerShell Console using the AvalonEdit control](https://github.com/dfinke/PowerShellConsole/tree/master)
//[dfinke (Doug Finke)](https://github.com/dfinke) -  Apache-2.0 license 

using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace PointlessWaymarks.PowerShellRunnerGui.PowerShellEditor;

/// <summary>
///     Handles the text markers for a code editor.
/// </summary>
public sealed class TextMarkerService(TextDocument document)
    : DocumentColorizingTransformer, IBackgroundRenderer, ITextViewConnect
{
    private readonly TextSegmentCollection<TextMarker>? _markers = new(document);

    private readonly List<TextView> _textViews = [];

    public IEnumerable<TextMarker> TextMarkers => _markers ?? Enumerable.Empty<TextMarker>();

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView == null)
            throw new ArgumentNullException(nameof(textView));
        if (drawingContext == null)
            throw new ArgumentNullException(nameof(drawingContext));
        if (_markers == null || !textView.VisualLinesValid)
            return;
        var visualLines = textView.VisualLines;
        if (visualLines.Count == 0)
            return;
        var viewStart = visualLines.First().FirstDocumentLine.Offset;
        var viewEnd = visualLines.Last().LastDocumentLine.EndOffset;
        foreach (var marker in _markers.FindOverlappingSegments(viewStart, viewEnd - viewStart))
        {
            if (marker.BackgroundColor != null)
            {
                var geoBuilder = new BackgroundGeometryBuilder
                {
                    AlignToWholePixels = true,
                    CornerRadius = 3
                };
                geoBuilder.AddSegment(textView, marker);
                var geometry = geoBuilder.CreateGeometry();
                if (geometry != null)
                {
                    var color = marker.BackgroundColor.Value;
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    drawingContext.DrawGeometry(brush, null, geometry);
                }
            }

            if (marker.MarkerType != TextMarkerType.None)
                foreach (var r in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
                {
                    var startPoint = r.BottomLeft;
                    var endPoint = r.BottomRight;

                    var usedPen = new Pen(new SolidColorBrush(marker.MarkerColor), 1);
                    usedPen.Freeze();
                    switch (marker.MarkerType)
                    {
                        case TextMarkerType.SquigglyUnderline:
                            var offset = 2.5;

                            var count = Math.Max((int)((endPoint.X - startPoint.X) / offset) + 1, 4);

                            var geometry = new StreamGeometry();

                            using (var ctx = geometry.Open())
                            {
                                ctx.BeginFigure(startPoint, false, false);
                                ctx.PolyLineTo(CreatePoints(startPoint, offset, count).ToArray(), true, false);
                            }

                            geometry.Freeze();

                            drawingContext.DrawGeometry(Brushes.Transparent, usedPen, geometry);
                            break;
                    }
                }
        }
    }

    public KnownLayer Layer =>
        // draw behind selection
        KnownLayer.Selection;

    void ITextViewConnect.AddToTextView(TextView? textView)
    {
        if (textView != null && !_textViews.Contains(textView))
        {
            Debug.Assert(textView.Document == document);
            _textViews.Add(textView);
        }
    }

    void ITextViewConnect.RemoveFromTextView(TextView? textView)
    {
        if (textView != null)
        {
            Debug.Assert(textView.Document == document);
            _textViews.Remove(textView);
        }
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_markers == null)
            return;
        var lineStart = line.Offset;
        var lineEnd = lineStart + line.Length;
        foreach (var marker in _markers.FindOverlappingSegments(lineStart, line.Length))
        {
            Brush? foregroundBrush = null;
            if (marker.ForegroundColor != null)
            {
                foregroundBrush = new SolidColorBrush(marker.ForegroundColor.Value);
                foregroundBrush.Freeze();
            }

            ChangeLinePart(
                Math.Max(marker.StartOffset, lineStart),
                Math.Min(marker.EndOffset, lineEnd),
                element =>
                {
                    if (foregroundBrush != null) element.TextRunProperties.SetForegroundBrush(foregroundBrush);
                }
            );
        }
    }

    public TextMarker Create(int startOffset, int length)
    {
        if (_markers == null)
            throw new InvalidOperationException("Cannot create a marker when not attached to a document");

        var textLength = document.TextLength;
        if (startOffset < 0 || startOffset > textLength)
            throw new ArgumentOutOfRangeException(nameof(startOffset), startOffset,
                "Value must be between 0 and " + textLength);
        if (length < 0 || startOffset + length > textLength)
            throw new ArgumentOutOfRangeException(nameof(length), length,
                "length must not be negative and startOffset+length must not be after the end of the document");

        var m = new TextMarker(this, startOffset, length);
        _markers.Add(m);
        // no need to mark segment for redraw: the text marker is invisible until a property is set
        return m;
    }

    private IEnumerable<Point> CreatePoints(Point start, double offset, int count)
    {
        for (var i = 0; i < count; i++)
            yield return new Point(start.X + i * offset, start.Y - ((i + 1) % 2 == 0 ? offset : 0));
    }

    public IEnumerable<TextMarker> GetMarkersAtOffset(int offset)
    {
        if (_markers == null)
            return [];

        return _markers.FindSegmentsContaining(offset);
    }

    /// <summary>
    ///     Redraws the specified text segment.
    /// </summary>
    internal void Redraw(ISegment segment)
    {
        foreach (var view in _textViews) view.Redraw(segment);
    }

    public void Remove(TextMarker marker)
    {
        if (marker == null)
            throw new ArgumentNullException(nameof(marker));
        if (_markers != null && marker is { } m && _markers.Remove(m))
        {
            Redraw(m);
            m.OnDeleted();
        }
    }

    public void RemoveAll()
    {
        if (_markers != null)
            foreach (var m in _markers.ToArray())
                Remove(m);
    }

    public void RemoveAll(Predicate<TextMarker> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        if (_markers != null)
            foreach (var m in _markers.ToArray())
                if (predicate(m))
                    Remove(m);
    }
}