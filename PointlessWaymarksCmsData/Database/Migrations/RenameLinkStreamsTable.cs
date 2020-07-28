using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202007280859)]
    public class RenameLinkStreamsTable : Migration
    {
        public override void Down()
        {
            if (Schema.Table("LinkContents").Exists())
                Rename.Table("LinkContents").To("LinkStreams");
        }

        public override void Up()
        {
            if (!Schema.Table("LinkContents").Exists())
                Rename.Table("LinkStreams").To("LinkContents");
        }
    }
}