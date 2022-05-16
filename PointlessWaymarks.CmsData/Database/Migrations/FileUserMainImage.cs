using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202205160900)]
public class FileUserMainPicture : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddShowPhotoSizes");
    }

    public override void Up()
    {
        if (!Schema.Table("FileContents").Column("UserMainPicture").Exists())
            Execute.Sql(@"ALTER TABLE FileContents 
                    ADD COLUMN UserMainPicture TEXT");
        if (!Schema.Table("HistoricFileContents").Column("UserMainPicture").Exists())
            Execute.Sql(@"ALTER TABLE HistoricFileContents 
                    ADD COLUMN UserMainPicture TEXT");
    }
}