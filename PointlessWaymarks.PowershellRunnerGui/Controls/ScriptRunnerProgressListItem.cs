﻿using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.PowershellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptRunnerProgressListItem
{
    public string Message { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
}