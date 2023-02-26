using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202201090000)]
public class AddShowImageSizes : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddShowImageSizes");
    }

    public override void Up()
    {
        if (!Schema.Table("ImageContents").Column("ShowImageSizes").Exists())
            Execute.Sql(@"ALTER TABLE ImageContents 
                    ADD COLUMN ShowImageSizes INTEGER 
                    NOT NULL DEFAULT 0");

        if (!Schema.Table("HistoricImageContents").Column("ShowImageSizes").Exists())
            Execute.Sql(@"ALTER TABLE HistoricImageContents 
                    ADD COLUMN ShowImageSizes INTEGER 
                    NOT NULL DEFAULT 0");
    }
}