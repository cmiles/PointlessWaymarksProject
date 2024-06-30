using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace PointlessWaymarks.PowerShellRunnerGui.PowerShellEditor;

//Code copied from or based on [GitHub - dfinke/PowerShellConsole: Create a PowerShell Console using the AvalonEdit control](https://github.com/dfinke/PowerShellConsole/tree/master)
//[dfinke (Doug Finke)](https://github.com/dfinke) -  Apache-2.0 license 
public class BraceFoldingStrategy
{
    /// <summary>
    ///     Creates a new BraceFoldingStrategy.
    /// </summary>
    public BraceFoldingStrategy()
    {
        OpeningBrace = '{';
        ClosingBrace = '}';
    }

    /// <summary>
    ///     Gets/Sets the closing brace. The default value is '}'.
    /// </summary>
    public char ClosingBrace { get; set; }

    /// <summary>
    ///     Gets/Sets the opening brace. The default value is '{'.
    /// </summary>
    public char OpeningBrace { get; set; }

    /// <summary>
    ///     Create <see cref="NewFolding" />s for the specified document.
    /// </summary>
    public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    {
        firstErrorOffset = -1;
        return CreateNewFoldings(document);
    }

    /// <summary>
    ///     Create <see cref="NewFolding" />s for the specified document.
    /// </summary>
    public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
    {
        List<NewFolding> newFoldings = [];

        var startOffsets = new Stack<int>();
        var lastNewLineOffset = 0;
        var openingBrace = OpeningBrace;
        var closingBrace = ClosingBrace;
        for (var i = 0; i < document.TextLength; i++)
        {
            var c = document.GetCharAt(i);
            if (c == openingBrace)
            {
                startOffsets.Push(i);
            }
            else if (c == closingBrace && startOffsets.Count > 0)
            {
                var startOffset = startOffsets.Pop();
                // don't fold if opening and closing brace are on the same line
                if (startOffset < lastNewLineOffset) newFoldings.Add(new NewFolding(startOffset, i + 1));
            }
            else if (c is '\n' or '\r')
            {
                lastNewLineOffset = i + 1;
            }
        }

        newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return newFoldings;
    }

    public void UpdateFoldings(FoldingManager? manager, TextDocument document)
    {
        var foldings = CreateNewFoldings(document, out var firstErrorOffset);
        manager?.UpdateFoldings(foldings, firstErrorOffset);
    }
}