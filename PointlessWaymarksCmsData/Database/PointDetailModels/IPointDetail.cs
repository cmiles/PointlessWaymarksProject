namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public interface IPointDetail
    {
        string DataTypeIdentifier { get; }
        public (bool isValid, string validationMessage) Validate();
    }
}