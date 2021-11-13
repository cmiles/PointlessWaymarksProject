namespace PointlessWaymarks.CmsData.Database.Models
{
    public class HistoricMapElement
    {
        public Guid ElementContentId { get; set; }
        public Guid HistoricGroupId { get; set; }
        public int Id { get; set; }
        public bool IncludeInDefaultView { get; set; }
        public bool IsFeaturedElement { get; set; }
        public DateTime LastUpdateOn { get; set; }
        public Guid MapComponentContentId { get; set; }
        public bool ShowDetailsDefault { get; set; }
    }
}