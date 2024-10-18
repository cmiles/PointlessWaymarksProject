using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202410151100)]
public class AddMapElementLinksTo : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddMapElementLinksTo");
    }

    public override void Up()
    {
        if (!Schema.Table("MapComponentElements").Column("LinksTo").Exists())
            Execute.Sql(@"ALTER TABLE MapComponentElements 
                    ADD COLUMN LinksTo TEXT NULL");
        if (!Schema.Table("HistoricMapComponentElement").Column("LinksTo").Exists())
            Execute.Sql(@"ALTER TABLE HistoricMapComponentElements 
                    ADD COLUMN LinksTo TEXT NULL");
    }
}