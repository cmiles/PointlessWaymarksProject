using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202204100000)]
public class AddPointContentMapLabel : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddPointContentMapLabel");
    }

    public override void Up()
    {
        if (!Schema.Table("PointContents").Column("MapLabel").Exists())
            Execute.Sql(@"ALTER TABLE PointContents 
                    ADD COLUMN MapLabel TEXT");

        if (!Schema.Table("HistoricPointContents").Column("MapLabel").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPointContents 
                    ADD COLUMN MapLabel TEXT");
    }
}