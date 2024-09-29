using PointlessWaymarks.PowerShellRunnerData;
using PointlessWaymarks.PowerShellRunnerData.Models;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

public static class ScriptJobEditorLauncher
{
    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task CreateInstance(ScriptJob toLoad, string databaseFile)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var editorScriptType = ScriptKind.PowerShell;

        if (Enum.TryParse(toLoad.ScriptType, out ScriptKind parsedScriptType)) editorScriptType = parsedScriptType;

        if (editorScriptType == ScriptKind.PowerShell)
            await ScriptJobEditorWindow.CreateInstance(toLoad, databaseFile);
        else if (editorScriptType == ScriptKind.CsScript)
            await CsScriptJobEditorWindow.CreateInstance(toLoad, databaseFile);
    }
}