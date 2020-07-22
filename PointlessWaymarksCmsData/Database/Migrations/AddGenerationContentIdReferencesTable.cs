using FluentMigrator;

namespace PointlessWaymarksCmsData.Database.Migrations
{
    [Migration(202007330845)]
    public class AddGenerationContentIdReferencesTable : Migration
    {
        public override void Down()
        {
            Delete.Table("GenerationContentIdReferences");
        }

        public override void Up()
        {
            if (!Schema.Table("GenerationContentIdReferences").Exists())
                Create.Table("GenerationContentIdReferences").WithColumn("ContentId").AsGuid().PrimaryKey();
        }
    }
}