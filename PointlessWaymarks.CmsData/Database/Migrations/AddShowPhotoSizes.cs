using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202201070000)]
public class AddShowPhotoSizes : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddAndReviseGenerationSupportTables");
    }

    public override void Up()
    {
        if (Schema.Table("FileContents").Column("FeedOn").Exists())
            return;

        var tableList = new List<string>
        {
            "HistoricPhotoContents",
            "PhotoContents"
        };

        foreach (var loopTable in tableList)
            Execute.Sql(@$"ALTER TABLE {loopTable} 
                    ADD COLUMN ShowPhotoSizes INTEGER 
                    NOT NULL DEFAULT 0");
    }
}