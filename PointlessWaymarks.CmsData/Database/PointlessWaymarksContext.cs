﻿using Microsoft.EntityFrameworkCore;
using PointlessWaymarks.CmsData.Database.Models;
using SQLitePCL;

#pragma warning disable 8618

namespace PointlessWaymarks.CmsData.Database
{
    public class PointlessWaymarksContext : DbContext
    {
        public PointlessWaymarksContext(DbContextOptions<PointlessWaymarksContext> options) : base(options)
        {
            //https://github.com/dotnet/efcore/issues/9994
            Batteries.Init();
            raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
        }

        public DbSet<FileContent> FileContents { get; set; }
        public DbSet<GenerationChangedContentId> GenerationChangedContentIds { get; set; }
        public DbSet<GenerationDailyPhotoLog> GenerationDailyPhotoLogs { get; set; }
        public DbSet<GenerationFileTransferScriptLog> GenerationFileTransferScriptLogs { get; set; }
        public DbSet<GenerationFileWriteLog> GenerationFileWriteLogs { get; set; }
        public DbSet<GenerationLog> GenerationLogs { get; set; }
        public DbSet<GenerationRelatedContent> GenerationRelatedContents { get; set; }
        public DbSet<GenerationTagLog> GenerationTagLogs { get; set; }
        public DbSet<GeoJsonContent> GeoJsonContents { get; set; }
        public DbSet<HistoricFileContent> HistoricFileContents { get; set; }
        public DbSet<HistoricGeoJsonContent> HistoricGeoJsonContents { get; set; }
        public DbSet<HistoricImageContent> HistoricImageContents { get; set; }
        public DbSet<HistoricLineContent> HistoricLineContents { get; set; }
        public DbSet<HistoricLinkContent> HistoricLinkContents { get; set; }
        public DbSet<HistoricMapElement> HistoricMapComponentElements { get; set; }
        public DbSet<HistoricMapComponent> HistoricMapComponents { get; set; }
        public DbSet<HistoricNoteContent> HistoricNoteContents { get; set; }
        public DbSet<HistoricPhotoContent> HistoricPhotoContents { get; set; }
        public DbSet<HistoricPointContent> HistoricPointContents { get; set; }
        public DbSet<HistoricPointDetail> HistoricPointDetails { get; set; }
        public DbSet<HistoricPostContent> HistoricPostContents { get; set; }
        public DbSet<ImageContent> ImageContents { get; set; }
        public DbSet<LineContent> LineContents { get; set; }
        public DbSet<LinkContent> LinkContents { get; set; }
        public DbSet<MapElement> MapComponentElements { get; set; }
        public DbSet<MapComponent> MapComponents { get; set; }
        public DbSet<MenuLink> MenuLinks { get; set; }
        public DbSet<NoteContent> NoteContents { get; set; }
        public DbSet<PhotoContent> PhotoContents { get; set; }
        public DbSet<PointContent> PointContents { get; set; }
        public DbSet<PointDetail> PointDetails { get; set; }
        public DbSet<PostContent> PostContents { get; set; }
        public DbSet<TagExclusion> TagExclusions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<GeoJsonContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<ImageContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<LineContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<LinkContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<MapComponent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<MapElement>().HasIndex(b => new { b.ElementContentId, b.MapComponentContentId })
                .IsUnique();
            modelBuilder.Entity<NoteContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PhotoContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PointContent>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PointDetail>().HasIndex(b => b.ContentId).IsUnique();
            modelBuilder.Entity<PostContent>().HasIndex(b => b.ContentId).IsUnique();

            modelBuilder.Entity<GenerationChangedContentId>().Property(e => e.ContentId).ValueGeneratedNever();
        }
    }
}