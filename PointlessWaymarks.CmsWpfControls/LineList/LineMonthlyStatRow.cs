using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.CmsWpfControls.LineList;

[NotifyPropertyChanged]
public partial class LineMonthlyStatRow
{
    public int Activities { get; set; }
    public int Climb { get; set; }
    public int Descent { get; set; }
    public int Hours { get; set; }
    public List<Guid> LineContentIds { get; set; } = new();
    public int MaxElevation { get; set; }
    public int Miles { get; set; }
    public int MinElevation { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}