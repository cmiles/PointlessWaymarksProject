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

        var editorScriptType = ScriptType.PowerShell;

        if (Enum.TryParse(toLoad.ScriptType, out ScriptType parsedScriptType)) editorScriptType = parsedScriptType;

        if (editorScriptType == ScriptType.PowerShell)
            await ScriptJobEditorWindow.CreateInstance(toLoad, databaseFile);
        else if (editorScriptType == ScriptType.CsScript)
            await CsScriptJobEditorWindow.CreateInstance(toLoad, databaseFile);
    }
}