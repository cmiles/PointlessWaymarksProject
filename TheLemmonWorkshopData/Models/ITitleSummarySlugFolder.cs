namespace TheLemmonWorkshopData.Models
{
    public interface ITitleSummarySlugFolder
    {
        public string Folder { get; set; }
        public string Slug { get; set; }
        public string Summary { get; set; }
        public string Title { get; set; }
    }
}