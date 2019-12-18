namespace TheLemmonWorkshopData.Models
{
    public interface ITitleSummarySlug
    {
        public string Summary { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
    }
}