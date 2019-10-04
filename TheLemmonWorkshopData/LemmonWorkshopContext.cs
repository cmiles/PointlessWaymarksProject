using Microsoft.EntityFrameworkCore;
using TheLemmonWorkshopData.Models;

namespace TheLemmonWorkshopData
{
    public class LemmonWorkshopContext : DbContext
    {
        public LemmonWorkshopContext(DbContextOptions<LemmonWorkshopContext> options) : base(options)
        {
        }

        public DbSet<HistoricSiteContent> HistoricSiteContents { get; set; }
        public DbSet<HistoricTrailSegment> HistoricTrailSegments { get; set; }
        public DbSet<SiteContent> SiteContents { get; set; }
        public DbSet<TrailSegment> TrailSegments { get; set; }
    }
}