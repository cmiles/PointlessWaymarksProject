using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202402250900)]
public class AddMapIconContentVersion : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddMapIconContentVersion");
    }

    public override void Up()
    {
        if (!Schema.Table("MapIcons").Column("ContentVersion").Exists())
            Execute.Sql(@"ALTER TABLE MapIcons 
                    ADD COLUMN ContentVersion TEXT NOT NULL DEFAULT ''");

        if (!Schema.Table("HistoricMapIcons").Column("ContentVersion").Exists())
            Execute.Sql(@"ALTER TABLE HistoricMapIcons 
                    ADD COLUMN ContentVersion TEXT NOT NULL DEFAULT ''");
    }
}