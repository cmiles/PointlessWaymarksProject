using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(20240411065200)]
public class AddFileImagePostVideoContentLatitudeLongitudeElevationShowLocation : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(AddFileImagePostVideoContentLatitudeLongitudeElevationShowLocation)}");
    }
    
    public override void Up()
    {
        if (!Schema.Table("PhotoContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE PhotoContents RENAME COLUMN ShowPhotoPosition TO ShowLocation");
        if (!Schema.Table("HistoricPhotoContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPhotoContents RENAME COLUMN ShowPhotoPosition TO ShowLocation");

        if (!Schema.Table("ImageContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE ImageContents 
                    ADD COLUMN ShowLocation INTEGER NOT NULL DEFAULT 0");
        if (!Schema.Table("HistoricImageContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricImageContents 
                    ADD COLUMN ShowLocation INTEGER NOT NULL DEFAULT 0");

        if (!Schema.Table("ImageContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE ImageContents
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("ImageContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE ImageContents
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("ImageContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE ImageContents 
                    ADD COLUMN Elevation REAL NULL");

        if (!Schema.Table("HistoricImageContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricImageContents 
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("HistoricImageContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricImageContents 
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("HistoricImageContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricImageContents 
                    ADD COLUMN Elevation REAL NULL");


        if (!Schema.Table("FileContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE FileContents 
                    ADD COLUMN ShowLocation INTEGER NOT NULL DEFAULT 0");
        if (!Schema.Table("HistoricFileContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricFileContents 
                    ADD COLUMN ShowLocation INTEGER NOT NULL DEFAULT 0");

        if (!Schema.Table("FileContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE FileContents 
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("FileContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE FileContents 
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("FileContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE FileContents 
                    ADD COLUMN Elevation REAL NULL");
        
        
        if (!Schema.Table("HistoricFileContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricFileContents 
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("HistoricFileContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricFileContents 
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("HistoricFileContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricFileContents 
                    ADD COLUMN Elevation REAL NULL");
        
        
        if (!Schema.Table("PostContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE PostContents 
                    ADD COLUMN ShowLocation INTEGER NOT NULL DEFAULT 0");
        if (!Schema.Table("HistoricPostContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPostContents 
                    ADD COLUMN ShowLocation INTEGER NOT NULL DEFAULT 0");

        if (!Schema.Table("PostContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE PostContents 
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("PostContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE PostContents 
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("PostContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE PostContents 
                    ADD COLUMN Elevation REAL NULL");
        
        
        if (!Schema.Table("HistoricPostContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPostContents 
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("HistoricPostContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPostContents 
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("HistoricPostContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPostContents 
                    ADD COLUMN Elevation REAL NULL");
        
        
        if (!Schema.Table("VideoContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE VideoContents 
                    ADD COLUMN ShowLocation INTEGER NOT NULL DEFAULT 0");
        if (!Schema.Table("HistoricVideoContents").Column("ShowLocation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricVideoContents 
                    ADD COLUMN ShowLocation INTEGER NOT NULL DEFAULT 0");

        if (!Schema.Table("VideoContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE VideoContents 
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("VideoContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE VideoContents 
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("VideoContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE VideoContents 
                    ADD COLUMN Elevation REAL NULL");
        
        
        if (!Schema.Table("HistoricVideoContents").Column("Latitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricVideoContents 
                    ADD COLUMN Latitude REAL NULL");
        if (!Schema.Table("HistoricVideoContents").Column("Longitude").Exists())
            Execute.Sql(@"ALTER TABLE HistoricVideoContents 
                    ADD COLUMN Longitude REAL NULL");
        if (!Schema.Table("HistoricVideoContents").Column("Elevation").Exists())
            Execute.Sql(@"ALTER TABLE HistoricVideoContents 
                    ADD COLUMN Elevation REAL NULL");
    }
}