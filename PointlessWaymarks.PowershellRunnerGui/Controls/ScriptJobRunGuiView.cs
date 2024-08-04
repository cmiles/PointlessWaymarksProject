using System.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.PowerShellRunnerData.Models;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class ScriptJobRunGuiView
{
    public ScriptJobRunGuiView()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public DateTime? CompletedOn { get; set; }
    public DateTime? CompletedOnUtc { get; set; }
    public bool Errors { get; set; }
    public string HumanReadableLength { get; set; } = string.Empty;
    public int Id { get; set; }
    public required ScriptJob? Job { get; set; }
    public int? LengthInSeconds { get; set; }
    public string Output { get; set; } = string.Empty;
    public required Guid PersistentId { get; set; }
    public string RunType { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public required Guid ScriptJobPersistentId { get; set; }
    public required DateTime StartedOn { get; set; }
    public required DateTime StartedOnUtc { get; set; }
    public string TranslatedOutput { get; set; } = string.Empty;
    public string TranslatedScript { get; set; } = string.Empty;

    public static ScriptJobRunGuiView CreateInstance(ScriptJobRun run, ScriptJob? job, string key)
    {
        var newView = new ScriptJobRunGuiView
        {
            CompletedOn = run.CompletedOnUtc?.ToLocalTime(),
            CompletedOnUtc = run.CompletedOnUtc,
            Errors = run.Errors,
            Id = run.Id,
            Job = job,
            LengthInSeconds = run.LengthInSeconds,
            Output = run.Output,
            PersistentId = run.PersistentId,
            RunType = run.RunType,
            Script = run.Script,
            ScriptJobPersistentId = run.ScriptJobPersistentId,
            StartedOn = run.StartedOnUtc.ToLocalTime(),
            StartedOnUtc = run.StartedOnUtc,
            TranslatedOutput = run.Output.TryDecrypt(key),
            TranslatedScript = run.Script.TryDecrypt(key)
        };

        return newView;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (e.PropertyName.Equals(nameof(LengthInSeconds)))
        {
            if (LengthInSeconds is null)
            {
                HumanReadableLength = string.Empty;
                return;
            }

            HumanReadableLength = TimeSpan.FromSeconds(LengthInSeconds.Value).ToString(@"hh\:mm\:ss");
        }
    }

    public void Update(ScriptJobRun run, ScriptJob? job, string key)
    {
        CompletedOn = run.CompletedOnUtc?.ToLocalTime();
        CompletedOnUtc = run.CompletedOnUtc;
        Errors = run.Errors;
        Id = run.Id;
        Job = job;
        LengthInSeconds = run.LengthInSeconds;
        Output = run.Output;
        PersistentId = run.PersistentId;
        RunType = run.RunType;
        Script = run.Script;
        ScriptJobPersistentId = run.ScriptJobPersistentId;
        StartedOn = run.StartedOnUtc.ToLocalTime();
        StartedOnUtc = run.StartedOnUtc;
        TranslatedOutput = run.Output.Decrypt(key);
        TranslatedScript = run.Script.Decrypt(key);
    }
}