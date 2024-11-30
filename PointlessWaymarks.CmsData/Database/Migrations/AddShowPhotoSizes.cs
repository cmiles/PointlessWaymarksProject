using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202201080000)]
public class AddShowPhotoSizes : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddShowPhotoSizes");
    }

    public override void Up()
    {
        if (!Schema.Table("PhotoContents").Column("ShowPhotoSizes").Exists())
            Execute.Sql(@"ALTER TABLE PhotoContents 
                    ADD COLUMN ShowPhotoSizes INTEGER 
                    NOT NULL DEFAULT 0");

        if (!Schema.Table("HistoricPhotoContents").Column("ShowPhotoSizes").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPhotoContents 
                    ADD COLUMN ShowPhotoSizes INTEGER 
                    NOT NULL DEFAULT 0");
    }
}