using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202402110000)]
public class AddLineActivityLog : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddLineRecordedOns");
    }

    public override void Up()
    {
        if (!Schema.Table("LineContents").Column("IncludeInActivityLog").Exists())
            Execute.Sql(@"ALTER TABLE LineContents 
                    ADD COLUMN IncludeInActivityLog INTEGER 
                    NOT NULL DEFAULT 0");
        if (!Schema.Table("LineContents").Column("ShowContentReferencesOnMap").Exists())
            Execute.Sql(@"ALTER TABLE LineContents 
                    ADD COLUMN ShowContentReferencesOnMap INTEGER 
                    NOT NULL DEFAULT 0");
        if (!Schema.Table("LineContents").Column("ActivityType").Exists())
            Execute.Sql(@"ALTER TABLE LineContents 
                    ADD COLUMN ActivityType TEXT NULL");

        if (!Schema.Table("HistoricLineContents").Column("IncludeInActivityLog").Exists())
            Execute.Sql(@"ALTER TABLE HistoricLineContents 
                    ADD COLUMN IncludeInActivityLog INTEGER 
                    NOT NULL DEFAULT 0");
        if (!Schema.Table("HistoricLineContents").Column("ShowContentReferencesOnMap").Exists())
            Execute.Sql(@"ALTER TABLE HistoricLineContents 
                    ADD COLUMN ShowContentReferencesOnMap INTEGER 
                    NOT NULL DEFAULT 0");
        if (!Schema.Table("HistoricLineContents").Column("ActivityType").Exists())
            Execute.Sql(@"ALTER TABLE HistoricLineContents 
                    ADD COLUMN ActivityType TEXT NULL");
    }
}