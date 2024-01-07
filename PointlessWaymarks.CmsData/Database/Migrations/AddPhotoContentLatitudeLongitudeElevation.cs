using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202203070000)]
public class AddPhotoContentLatitudeLongitudeElevation : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddPhotoContentLatitudeLongitudeElevation");
    }

    public override void Up()
    {
        if (!Schema.Table("PhotoContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE PhotoContents 
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("PhotoContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE PhotoContents 
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("PhotoContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE PhotoContents 
                    ADD COLUMN Elevation REAL NULL");


        if (!Schema.Table("HistoricPhotoContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPhotoContents 
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("HistoricPhotoContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPhotoContents 
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("HistoricPhotoContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPhotoContents 
                    ADD COLUMN Elevation REAL NULL");
    }
}