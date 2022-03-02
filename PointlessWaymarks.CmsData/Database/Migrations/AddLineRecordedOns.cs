using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202203010000)]
public class AddLineRecordedOns : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddShowPhotoSizes");
    }

    public override void Up()
    {
        if (!Schema.Table("LineContents").Column("RecordingStartedOn").Exists())
            Execute.Sql(@"ALTER TABLE LineContents 
                    ADD COLUMN RecordingStartedOn TEXT NULL");
        if (!Schema.Table("LineContents").Column("RecordingEndedOn").Exists())
            Execute.Sql(@"ALTER TABLE LineContents 
                    ADD COLUMN RecordingEndedOn TEXT NULL");

        if (!Schema.Table("HistoricLineContents").Column("RecordingStartedOn").Exists())
            Execute.Sql(@"ALTER TABLE HistoricLineContents 
                    ADD COLUMN RecordingStartedOn TEXT NULL");
        if (!Schema.Table("HistoricLineContents").Column("RecordingEndedOn").Exists())
            Execute.Sql(@"ALTER TABLE HistoricLineContents 
                    ADD COLUMN RecordingEndedOn TEXT NULL");
    }
}