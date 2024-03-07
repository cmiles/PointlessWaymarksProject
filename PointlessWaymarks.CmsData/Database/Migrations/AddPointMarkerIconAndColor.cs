using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202402200000)]
public class AddPointMarkerIconAndColor : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddPointMarkerIconAndColor");
    }

    public override void Up()
    {
        if (!Schema.Table("PointContents").Column("MapMarkerColor").Exists())
            Execute.Sql(@"ALTER TABLE PointContents 
                    ADD COLUMN MapMarkerColor TEXT");

        if (!Schema.Table("PointContents").Column("MapIcon").Exists() &&
            !Schema.Table("PointContents").Column("MapIconName").Exists())
            Execute.Sql(@"ALTER TABLE PointContents 
                    ADD COLUMN MapIcon TEXT");

        if (!Schema.Table("HistoricPointContents").Column("MapMarkerColor").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPointContents 
                    ADD COLUMN MapMarkerColor TEXT");

        if (!Schema.Table("HistoricPointContents").Column("MapIcon").Exists() &&
            !Schema.Table("HistoricPointContents").Column("MapIconName").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPointContents 
                    ADD COLUMN MapIcon TEXT");
    }
}