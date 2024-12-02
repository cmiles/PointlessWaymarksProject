using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202512012000)]
public class AddPhotoDirection : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddPhotoDirection");
    }

    public override void Up()
    {
        if (!Schema.Table("PhotoContents").Column("PhotoDirection").Exists())
            Execute.Sql(@"ALTER TABLE PhotoContents 
                    ADD COLUMN PhotoDirection REAL");

        if (!Schema.Table("HistoricPhotoContents").Column("PhotoDirection").Exists())
            Execute.Sql(@"ALTER TABLE HistoricPhotoContents 
                    ADD COLUMN PhotoDirection REAL");
    }
}