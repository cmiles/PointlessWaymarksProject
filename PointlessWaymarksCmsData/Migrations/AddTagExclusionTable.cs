using FluentMigrator;

namespace PointlessWaymarksCmsData.Migrations
{
    [Migration(202005040712)]
    public class AddTagExclusionTable : Migration
    {
        public override void Down()
        {
            Delete.Table("TagExclusions");
        }

        public override void Up()
        {
            if (!Schema.Table("TagExclusions").Exists())
                Create.Table("TagExclusions").WithColumn("Id").AsInt64().PrimaryKey().Identity().WithColumn("Tag")
                    .AsString();
        }
    }
}