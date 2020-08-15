using Microsoft.EntityFrameworkCore;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Database
{
    public class PointlessWaymarksContext : DbContext
    {
        public PointlessWaymarksContext(DbContextOptions<PointlessWaymarksContext> options) : base(options)
        {
        }

        public DbSet<FileContent> FileContents { get; set; }
        public DbSet<GenerationChangedContentId> GenerationChangedContentIds { get; set; }
        public DbSet<GenerationDailyPhotoLog> GenerationDailyPhotoLogs { get; set; }
        public DbSet<GenerationLog> GenerationLogs { get; set; }
        public DbSet<GenerationRelatedContent> GenerationRelatedContents { get; set; }
        public DbSet<GenerationTagLog> GenerationTagLogs { get; set; }
        public DbSet<HistoricFileContent> HistoricFileContents { get; set; }
        public DbSet<HistoricImageContent> HistoricImageContents { get; set; }
        public DbSet<HistoricLinkContent> HistoricLinkContents { get; set; }
        public DbSet<HistoricNoteContent> HistoricNoteContents { get; set; }
        public DbSet<HistoricPhotoContent> HistoricPhotoContents { get; set; }
        public DbSet<HistoricPointContentPointDetailLink> HistoricPointContentPointDetailLinks { get; set; }
        public DbSet<HistoricPointContent> HistoricPointContents { get; set; }
        public DbSet<HistoricPointDetail> HistoricPointDetails { get; set; }
        public DbSet<HistoricPostContent> HistoricPostContents { get; set; }
        public DbSet<ImageContent> ImageContents { get; set; }
        public DbSet<LinkContent> LinkContents { get; set; }
        public DbSet<MenuLink> MenuLinks { get; set; }
        public DbSet<NoteContent> NoteContents { get; set; }
        public DbSet<PhotoContent> PhotoContents { get; set; }
        public DbSet<PointContentPointDetailLink> PointContentPointDetailLinks { get; set; }
        public DbSet<PointContent> PointContents { get; set; }
        public DbSet<PointDetail> PointDetails { get; set; }
        public DbSet<PostContent> PostContents { get; set; }
        public DbSet<TagExclusion> TagExclusions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<ImageContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<LinkContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<NoteContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PhotoContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PointContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PointDetail>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PostContent>().HasIndex(b => b.ContentId).IsUnique();

            modelBuilder.Entity<PointContentPointDetailLink>().HasIndex(p => new {p.PointContentId});

            modelBuilder.Entity<GenerationDailyPhotoLog>().HasIndex(p => new {p.GenerationVersion, p.DailyPhotoDate});
            modelBuilder.Entity<GenerationDailyPhotoLog>().HasIndex(p => new {p.RelatedContentId});

            modelBuilder.Entity<GenerationTagLog>().HasIndex(p => new {p.GenerationVersion, p.TagSlug});
            modelBuilder.Entity<GenerationTagLog>().HasIndex(p => new {p.RelatedContentId});

            modelBuilder.Entity<GenerationChangedContentId>().Property(e => e.ContentId).ValueGeneratedNever();
        }
    }
}