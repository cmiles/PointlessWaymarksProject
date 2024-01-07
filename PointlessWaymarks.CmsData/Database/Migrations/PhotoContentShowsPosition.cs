using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202210231800)]
public class PhotoContentShowsPosition : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration PhotoContentShowsPosition");
    }

    public override void Up()
    {
        if (!Schema.Table("PhotoContents").Column("ShowPhotoPosition").Exists())
            Execute.Sql(@"ALTER TABLE PhotoContents 
                    ADD COLUMN ShowPhotoPosition INTEGER NOT NULL DEFAULT 0");
        if (!Schema.Table("HistoricPhotoContents").Column("ShowPhotoPosition").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPhotoContents 
                    ADD COLUMN ShowPhotoPosition INTEGER NOT NULL DEFAULT 0");
    }
}