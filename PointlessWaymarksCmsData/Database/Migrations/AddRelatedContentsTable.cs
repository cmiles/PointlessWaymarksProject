using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202005310606)]
    public class AddRelatedContentsTable : Migration
    {
        public override void Down()
        {
            Delete.Table("RelatedContents");
        }

        public override void Up()
        {
            if (!Schema.Table("RelatedContents").Exists())
                Create.Table("RelatedContents").WithColumn("Id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("ContentOne").AsGuid().WithColumn("ContentTwo").AsGuid();
        }
    }
}