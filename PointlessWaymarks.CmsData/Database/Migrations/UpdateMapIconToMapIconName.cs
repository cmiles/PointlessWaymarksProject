using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202402250000)]
public class UpdateMapIconToMapIconName : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration UpdateMapIconToMapIconName");
    }

    public override void Up()
    {
        if (Schema.Table("PointContents").Column("MapIcon").Exists())
            Execute.Sql(@"ALTER TABLE PointContents 
                    RENAME COLUMN MapIcon to MapIconName");

        if (Schema.Table("HistoricPointContents").Column("MapIcon").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPointContents 
                    RENAME COLUMN MapIcon to MapIconName");
    }
}