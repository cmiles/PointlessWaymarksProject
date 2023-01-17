using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202210230900)]
public class UtcToPhotosAndLines : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration UtcToPhotosAndLines");
    }

    public override void Up()
    {
        if (!Schema.Table("PhotoContents").Column("PhotoCreatedOnUtc").Exists())
            Execute.Sql(@"ALTER TABLE PhotoContents 
                    ADD COLUMN PhotoCreatedOnUtc TEXT");
        if (!Schema.Table("HistoricPhotoContents").Column("PhotoCreatedOnUtc").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPhotoContents 
                    ADD COLUMN PhotoCreatedOnUtc TEXT");

        if (!Schema.Table("LineContents").Column("RecordingStartedOnUtc").Exists())
            Execute.Sql(@"ALTER TABLE LineContents 
                    ADD COLUMN RecordingStartedOnUtc TEXT");
        if (!Schema.Table("HistoricLineContents").Column("RecordingStartedOnUtc").Exists())
            Execute.Sql(@"ALTER TABLE HistoricLineContents
                    ADD COLUMN RecordingStartedOnUtc TEXT");

        if (!Schema.Table("LineContents").Column("RecordingEndedOnUtc").Exists())
            Execute.Sql(@"ALTER TABLE LineContents 
                    ADD COLUMN RecordingEndedOnUtc TEXT");
        if (!Schema.Table("HistoricLineContents").Column("RecordingEndedOnUtc").Exists())
            Execute.Sql(@"ALTER TABLE HistoricLineContents 
                    ADD COLUMN RecordingEndedOnUtc TEXT");
    }
}