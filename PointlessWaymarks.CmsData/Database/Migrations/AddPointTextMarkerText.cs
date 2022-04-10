using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202204100000)]
public class AddPointTextMarkerText : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddPointMarkerText");
    }

    public override void Up()
    {
        if (!Schema.Table("PointContents").Column("TextMarkerText").Exists())
            Execute.Sql(@"ALTER TABLE PointContents 
                    ADD COLUMN TextMarkerText TEXT");

        if (!Schema.Table("HistoricPointContents").Column("TextMarkerText").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPointContents 
                    ADD COLUMN TextMarkerText TEXT");
    }
}