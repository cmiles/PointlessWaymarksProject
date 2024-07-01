using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData.Models;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls
{
    [NotifyPropertyChanged]
    public partial class ScriptJobRunViewerContext
    {
        public ScriptJobRun? DbEntry { get; set; }
        public ScriptJob? Job { get; set; }
        public bool ScriptRunMatchesCurrentScript { get; set; }



    }
}
