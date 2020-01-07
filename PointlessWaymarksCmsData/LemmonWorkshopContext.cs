using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData
{
    public class LemmonWorkshopContext : DbContext
    {
        public LemmonWorkshopContext(DbContextOptions<LemmonWorkshopContext> options) : base(options)
        {
        }

        public DbSet<HistoricLineContent> HistoricLineContents { get; set; }
        public DbSet<HistoricPhotoContent> HistoricPhotoContents { get; set; }
        public DbSet<HistoricPointContent> HistoricPointContents { get; set; }
        public DbSet<HistoricPostContent> HistoricPostContents { get; set; }
        public DbSet<HistoricTrailSegment> HistoricTrailSegments { get; set; }

        public DbSet<LineContent> LineContents { get; set; }
        public DbSet<PhotoContent> PhotoContents { get; set; }
        public DbSet<PointContent> PointContents { get; set; }
        public DbSet<PostContent> PostContents { get; set; }
        public DbSet<TrailSegment> TrailSegments { get; set; }
    }
}