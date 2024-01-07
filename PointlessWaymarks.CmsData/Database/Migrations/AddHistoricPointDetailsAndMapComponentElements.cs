using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202401070000)]
public class AddHistoricPointDetailsAndMapComponentElements : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(AddHistoricPointDetailsAndMapComponentElements)}");
    }

    public override void Up()
    {
        if (!Schema.Table("HistoricPointContents").Column("PointDetails").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPointContents 
                    ADD COLUMN PointDetails TEXT NULL");
        if (!Schema.Table("HistoricMapComponents").Column("Elements").Exists())
            Execute.Sql(@"ALTER TABLE HistoricMapComponents 
                    ADD COLUMN Elements TEXT NULL");
    }
}