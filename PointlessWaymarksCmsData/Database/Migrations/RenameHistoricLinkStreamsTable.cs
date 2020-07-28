using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202007280956)]
    public class RenameHistoricLinkStreamsTable : Migration
    {
        public override void Down()
        {
            if (Schema.Table("HistoricLinkContents").Exists())
                Rename.Table("HistoricLinkContents").To("HistoricLinkStreams");
        }

        public override void Up()
        {
            if (!Schema.Table("HistoricLinkContents").Exists())
                Rename.Table("HistoricLinkStreams").To("HistoricLinkContents");
        }
    }
}