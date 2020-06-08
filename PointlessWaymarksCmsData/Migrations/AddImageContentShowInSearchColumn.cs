using FluentMigrator;

namespace PointlessWaymarksCmsData.Migrations
{
    [Migration(202006060623)]
    public class AddImageContentAndHistoricImageContentShowInSearchColumn : Migration
    {
        public override void Down()
        {
            Delete.Column("ShowInSearch").FromTable("ImageContents");
        }

        public override void Up()
        {
            if (!Schema.Table("ImageContents").Column("ShowInSearch").Exists())
                Alter.Table("ImageContents").AddColumn("ShowInSearch").AsBoolean().SetExistingRowsTo(true)
                    .NotNullable();

            if (!Schema.Table("HistoricImageContents").Column("ShowInSearch").Exists())
                Alter.Table("HistoricImageContents").AddColumn("ShowInSearch").AsBoolean().SetExistingRowsTo(true)
                    .NotNullable();
        }
    }
}