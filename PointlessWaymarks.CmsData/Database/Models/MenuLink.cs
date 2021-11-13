namespace PointlessWaymarks.CmsData.Database.Models
{
    public class MenuLink
    {
        public DateTime ContentVersion { get; set; }
        public int Id { get; set; }
        public string? LinkTag { get; set; }
        public int MenuOrder { get; set; }
    }
}