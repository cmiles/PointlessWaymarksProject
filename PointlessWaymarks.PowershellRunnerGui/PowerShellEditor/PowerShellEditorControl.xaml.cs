using System.Diagnostics;
using System.Management.Automation.Language;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using PointlessWaymarks.WpfCommon.StringDataEntry;

namespace PointlessWaymarks.PowerShellRunnerGui.PowerShellEditor;

/// <summary>
///     Interaction logic for PowerShellEditorControl.xaml
/// </summary>
public partial class PowerShellEditorControl
{
    public static readonly DependencyProperty TextBoxHeightProperty = DependencyProperty.Register(nameof(TextBoxHeight),
        typeof(double), typeof(StringDataEntryControl), new PropertyMetadata(default(double)));

    private readonly FoldingManager _foldingManager;
    private readonly BraceFoldingStrategy _foldingStrategy;
    private readonly TextMarkerService _textMarkerService;
    private ToolTip? _toolTip;

    public PowerShellEditorControl()
    {
        InitializeComponent();

        TextEditor.Focus();

        TextEditor.TextArea.PreviewKeyUp += TextArea_PreviewKeyUp;
        TextEditor.TextArea.TextEntered += TextArea_TextEntered;

        TextEditor.MouseHover += textEditor_MouseHover;
        TextEditor.MouseHoverStopped += textEditor_MouseHoverStopped;

        TextEditor.ShowLineNumbers = true;

        _foldingManager = FoldingManager.Install(TextEditor.TextArea);
        _foldingStrategy = new BraceFoldingStrategy();
        _foldingStrategy.UpdateFoldings(_foldingManager, TextEditor.Document);
        AddFoldingStrategyTimer();

        AddSyntaxHighlighting();
        AddCtrlSpaceBar();

        _textMarkerService = new TextMarkerService(TextEditor.Document);
        TextEditor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
        var textView = TextEditor.TextArea.TextView;
        textView.LineTransformers.Add(_textMarkerService);
        textView.Services.AddService(typeof(TextMarkerService), _textMarkerService);

        TestForSyntaxErrors();
    }


    public double TextBoxHeight
    {
        get => (double)GetValue(TextBoxHeightProperty);
        set
        {
            {
                SetValue(TextBoxHeightProperty, value);
                TextEditor.Height = value;
            }
        }
    }

    private void AddCtrlSpaceBar()
    {
        var handleCtrlSpaceBar =
            new KeyBinding(new ControlSpaceBarCommand(TextEditor), Key.Space, ModifierKeys.Control);
        TextEditor.TextArea.DefaultInputHandler.Editing.InputBindings.Add(handleCtrlSpaceBar);
    }

    private void AddFoldingStrategyTimer()
    {
        var foldingUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };

        foldingUpdateTimer.Tick += foldingUpdateTimer_Tick;
        foldingUpdateTimer.Start();
    }

    private void AddSyntaxHighlighting()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("PowerShellConsole.PowerShell.xshd");
        if (s != null)
        {
            using var reader = new XmlTextReader(s);
            TextEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
    }

    private void foldingUpdateTimer_Tick(object? sender, EventArgs e)
    {
        _foldingStrategy.UpdateFoldings(_foldingManager, TextEditor.Document);
    }

    private void TestForSyntaxErrors()
    {
        _textMarkerService.RemoveAll();
        var script = TextEditor.TextArea.TextView.Document.Text;

        var ast = Parser.ParseInput(script, out _, out var errors);

        foreach (var item in errors)
        {
            var startOffset = item.Extent.StartOffset;
            var endOffset = item.Extent.EndOffset;
            var toolTip = item.Message;
            var length = endOffset - startOffset;
            var m = _textMarkerService.Create(startOffset, length);

            m.MarkerType = TextMarkerType.SquigglyUnderline;
            m.MarkerColor = Colors.Red;
            m.ToolTip = toolTip;
        }
    }

    private void TextArea_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers is ModifierKeys.Control or ModifierKeys.None or ModifierKeys.Shift)
            if (e.Key is Key.Down or Key.Up or Key.PageDown or Key.PageUp or Key.Home or Key.End or Key.Left
                or Key.Right or Key.RightCtrl or Key.LeftCtrl)
                return;

        // don't check for syntax errors 
        // if any of those keys were pressed
        var msg = $"PreviewKeyUp: {e.KeyboardDevice.Modifiers} {e.Key}";
        Debug.WriteLine(msg);

        TestForSyntaxErrors();
    }

    private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.IndexOfAny(@"$-.:\".ToCharArray()) != -1) TextEditorUtilities.InvokeCompletionWindow(TextEditor);
    }

    private void textEditor_MouseHover(object sender, MouseEventArgs e)
    {
        var textView = TextEditor.TextArea.TextView;
        if (textView.Services.GetService(typeof(TextMarkerService)) is TextMarkerService textMarkerService)
            foreach (var textMarker in textMarkerService.TextMarkers)
            {
                _toolTip ??= new ToolTip();

                _toolTip.Content = textMarker.ToolTip;
                _toolTip.IsOpen = true;
                e.Handled = true;
            }
    }

    private void textEditor_MouseHoverStopped(object sender, MouseEventArgs e)
    {
        if (_toolTip != null) _toolTip.IsOpen = false;
    }
}