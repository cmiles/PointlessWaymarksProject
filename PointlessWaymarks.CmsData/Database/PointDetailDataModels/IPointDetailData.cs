﻿namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels;

public interface IPointDetailData
{
    string DataTypeIdentifier { get; }
    public IsValid Validate();
}