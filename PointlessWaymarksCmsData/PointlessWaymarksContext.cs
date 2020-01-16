using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData
{
    public class PointlessWaymarksContext : DbContext
    {
        public PointlessWaymarksContext(DbContextOptions<PointlessWaymarksContext> options) : base(options)
        {
        }

        public DbSet<FileContent> FileContents { get; set; }

        public DbSet<HistoricFileContent> HistoricFileContents { get; set; }
        public DbSet<HistoricImageContent> HistoricImageContents { get; set; }
        public DbSet<HistoricLineContent> HistoricLineContents { get; set; }
        public DbSet<HistoricPhotoContent> HistoricPhotoContents { get; set; }
        public DbSet<HistoricPointContent> HistoricPointContents { get; set; }
        public DbSet<HistoricPostContent> HistoricPostContents { get; set; }
        public DbSet<HistoricTrailSegment> HistoricTrailSegments { get; set; }
        public DbSet<ImageContent> ImageContents { get; set; }
        public DbSet<LineContent> LineContents { get; set; }
        public DbSet<PhotoContent> PhotoContents { get; set; }
        public DbSet<PointContent> PointContents { get; set; }
        public DbSet<PostContent> PostContents { get; set; }
        public DbSet<TrailSegment> TrailSegments { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<ImageContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<LineContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PhotoContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PointContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PostContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<TrailSegment>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PostContent>().HasIndex(b => b.ContentId).IsUnique();
        }
    }
}