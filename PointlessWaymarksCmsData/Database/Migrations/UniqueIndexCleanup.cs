using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202008031459)]
    public class UniqueIndexCleanup : Migration
    {
        public override void Down()
        {
            //No indexes should be reversed
        }

        public override void Up()
        {
            if (Schema.Table("LinkContents").Index("IX_LinkStreams_ContentId").Exists())
                Delete.Index("IX_LinkStreams_ContentId").OnTable("LinkContents");

            if (!Schema.Table("FileContents").Index("IX_FileContents_ContentId").Exists())
                Execute.Sql(@"
CREATE UNIQUE INDEX ""IX_FileContents_ContentId"" ON ""FileContents"" (""ContentId"")
; ");
            if (!Schema.Table("ImageContents").Index("IX_ImageContents_ContentId").Exists())
                Execute.Sql(@"
CREATE UNIQUE INDEX ""IX_ImageContents_ContentId"" ON ""ImageContents"" (""ContentId"")
; ");
            if (!Schema.Table("LinkContents").Index("IX_LinkContents_ContentId").Exists())
                Execute.Sql(@"
CREATE UNIQUE INDEX ""IX_LinkContents_ContentId"" ON ""LinkContents"" (""ContentId"")
; ");
            if (!Schema.Table("NoteContents").Index("IX_NoteContents_ContentId").Exists())
                Execute.Sql(@"
CREATE UNIQUE INDEX ""IX_NoteContents_ContentId"" ON ""NoteContents"" (""ContentId"")
; ");
            if (!Schema.Table("PhotoContents").Index("IX_PhotoContents_ContentId").Exists())
                Execute.Sql(@"
CREATE UNIQUE INDEX ""IX_PhotoContents_ContentId"" ON ""PhotoContents"" (""ContentId"")
; ");
            if (!Schema.Table("PostContents").Index("IX_PostContents_ContentId").Exists())
                Execute.Sql(@"
CREATE UNIQUE INDEX ""IX_PostContents_ContentId"" ON ""PostContents"" (""ContentId"")
; ");
        }
    }
}