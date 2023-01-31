using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels;

public interface IPointDetailData
{
    string DataTypeIdentifier { get; }
    public Task<IsValid> Validate();
}