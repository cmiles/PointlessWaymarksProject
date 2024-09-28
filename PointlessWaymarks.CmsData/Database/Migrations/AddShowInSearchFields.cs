using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202409270000)]
public class AddShowInSearchFields : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration AddShowInSearchFields");
    }

    public override void Up()
    {
        var tableList = new[]
        {
            "FileContents", "GeoJsonContents", "HistoricFileContents", "HistoricGeoJsonContents", "HistoricLineContents",
            "HistoricNoteContents", "HistoricPhotoContents", "HistoricPointContents", "HistoricPostContents",
            "HistoricVideoContents", "LineContents", "NoteContents", "PhotoContents", "PointContents", "PostContents",
            "VideoContents"
        };

        foreach (var loopTable in tableList)
            if (!Schema.Table(loopTable).Column("ShowInSearch").Exists())
                Execute.Sql(@$"ALTER TABLE {loopTable} 
                    ADD COLUMN ShowInSearch INTEGER 
                    NOT NULL DEFAULT 1");
    }
}