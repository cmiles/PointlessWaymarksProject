using FluentMigrator;

namespace PointlessWaymarksCmsData.Migrations
{
    [Migration(202006062044)]
    public class AddMenuLinkOrderColumn : Migration
    {
        public override void Down()
        {
            Delete.Column("MenuOrder").FromTable("MenuLinks");
        }

        public override void Up()
        {
            if (!Schema.Table("MenuLinks").Column("MenuOrder").Exists())
                Alter.Table("MenuLinks").AddColumn("MenuOrder").AsInt64().SetExistingRowsTo(0).NotNullable();
        }
    }
}