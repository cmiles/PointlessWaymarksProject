using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PointlessWaymarks.PowerShellRunnerGui.PowerShellEditor;

//Code copied from or based on [GitHub - dfinke/PowerShellConsole: Create a PowerShell Console using the AvalonEdit control](https://github.com/dfinke/PowerShellConsole/tree/master)
//[dfinke (Doug Finke)](https://github.com/dfinke) -  Apache-2.0 license 
public static class PsConsolePowerShell
{
    private static PowerShell? _powerShell;
    private static Runspace? _rs;

    public static PowerShell PowerShellInstance
    {
        get
        {
            if (_powerShell == null)
            {
                _powerShell = PowerShell.Create();
                _powerShell.Runspace = PsConsoleRunspace;
            }

            return _powerShell;
        }
    }

    internal static Runspace PsConsoleRunspace
    {
        get
        {
            if (_rs == null)
            {
                _rs = RunspaceFactory.CreateRunspace();
                _rs.ThreadOptions = PSThreadOptions.UseCurrentThread;
                _rs.Open();
            }

            return _rs;
        }
    }
}