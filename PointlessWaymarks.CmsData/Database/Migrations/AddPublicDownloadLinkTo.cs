using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202312020000)]
public class AddPublicDownloadLinkTo : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddPublicDownloadLinkTo");
    }

    public override void Up()
    {
        if (Schema.Table("GeoJsonContents").Column("PublicDownloadLink").Exists())
            return;

        var tableList = new List<string>
        {
            "GeoJsonContents",
            "HistoricGeoJsonContents",
            "HistoricLineContents",
            "LineContents"
        };

        foreach (var loopTable in tableList)
            Execute.Sql(@$"ALTER TABLE {loopTable} 
                    ADD COLUMN PublicDownloadLink INTEGER 
                    NOT NULL DEFAULT 0");
    }
}