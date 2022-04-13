namespace PointlessWaymarks.CmsWpfControls.GpxImport;

public interface IGpxImportListItem
{
    DateTime? CreatedOn { get; set; }
    Guid DisplayId { get; set; }
    bool MarkedForImport { get; set; }
    bool ReplaceElevationOnImport { get; set; }
    string UserContentName { get; set; }
    string UserSummary { get; set; }
}