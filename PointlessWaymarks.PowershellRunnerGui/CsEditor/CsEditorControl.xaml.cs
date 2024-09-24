using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynPad.Editor;
using RoslynPad.Roslyn;

namespace PointlessWaymarks.PowerShellRunnerGui.CsEditor;

/// <summary>
///     Interaction logic for CsEditorControl.xaml
/// </summary>
public partial class CsEditorControl : UserControl
{
    private readonly RoslynHost _host;

    public CsEditorControl()
    {
        InitializeComponent();

        var assemblyList = RoslynHostReferences.NamespaceDefault.With(assemblyReferences: new[]
        {
            typeof(object).Assembly,
            typeof(Regex).Assembly,
            typeof(Enumerable).Assembly,
        }, imports: ["System.Console", "Internal"]);

        _host = new ScriptRunnerRoslynHost(new[]
        {
            Assembly.Load("RoslynPad.Roslyn.Windows"),
            Assembly.Load("RoslynPad.Editor.Windows")
        }, assemblyList);
    }

    private async void OnItemLoaded(object sender, EventArgs e)
    {
        if (sender is not RoslynCodeEditor editor) return;

        editor.Loaded -= OnItemLoaded;
        editor.Focus();

        var workingDirectory = Directory.GetCurrentDirectory();

        await editor.InitializeAsync(_host, new ClassificationHighlightColors(),
            workingDirectory, string.Empty, SourceCodeKind.Script).ConfigureAwait(true);
    }
}

public class ScriptRunnerRoslynHost(
    IEnumerable<Assembly>? additionalAssemblies = null,
    RoslynHostReferences? references = null,
    ImmutableArray<string>? disabledDiagnostics = null)
    : RoslynHost(additionalAssemblies, references, disabledDiagnostics)
{
    private bool _addedAnalyzers;

    protected override IEnumerable<AnalyzerReference> GetSolutionAnalyzerReferences()
    {
        if (!_addedAnalyzers)
        {
            _addedAnalyzers = true;
            return base.GetSolutionAnalyzerReferences();
        }

        return Enumerable.Empty<AnalyzerReference>();
    }
}