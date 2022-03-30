namespace PointlessWaymarks.CmsWpfControls.GpxImport;

public interface IGpxImportListItem
{
    Guid DisplayId { get; set; }
    bool MarkedForImport { get; set; }
    bool ReplaceElevationOnImport { get; set; }
}