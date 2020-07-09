using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202006060619)]
    public class AddMenuLinksTable : Migration
    {
        public override void Down()
        {
            Delete.Table("MenuLinks");
        }

        public override void Up()
        {
            if (!Schema.Table("MenuLinks").Exists())
                Create.Table("MenuLinks").WithColumn("Id").AsInt64().PrimaryKey().Identity().WithColumn("LinkTag")
                    .AsString();
        }
    }
}