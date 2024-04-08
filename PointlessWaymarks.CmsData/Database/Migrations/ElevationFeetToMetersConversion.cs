using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202408080800)]
public class ElevationFeetToMetersConversion : Migration
{
    public override void Down()
    {
        throw new DataException($"No Down Available for Migration {nameof(ElevationFeetToMetersConversion)}");
    }

    public override void Up()
    {
        Execute.Sql(@"UPDATE PhotoContents 
                        SET Elevation = Round(Elevation / 0.3048)
                        WHERE Elevation is not NULL");
        Execute.Sql(@"UPDATE HistoricPhotoContents 
                        SET Elevation = Round(Elevation / 0.3048)
                        WHERE Elevation is not NULL");
        Execute.Sql(@"UPDATE PointContents 
                        SET Elevation = Round(Elevation / 0.3048)
                        WHERE Elevation is not NULL");
        Execute.Sql(@"UPDATE HistoricPointContents 
                        SET Elevation = Round(Elevation / 0.3048)
                        WHERE Elevation is not NULL");
    }
}