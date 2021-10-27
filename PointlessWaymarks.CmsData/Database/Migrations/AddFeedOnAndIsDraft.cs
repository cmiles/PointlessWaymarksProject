using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations
{
    [Migration(202110260000)]
    public class AddFeedOnAndIsDraft : Migration
    {
        public override void Down()
        {
            throw new DataException("No Down Available for Migration AddAndReviseGenerationSupportTables");
        }

        public override void Up()
        {
            if (Schema.Table("FileContents").Column("FeedOn").Exists())
                return;

            var tableList = new List<string>
            {
                "FileContents",
                "GeoJsonContents",
                "HistoricFileContents",
                "HistoricGeoJsonContents",
                "HistoricImageContents",
                "HistoricLineContents",
                "HistoricNoteContents",
                "HistoricPhotoContents",
                "HistoricPointContents",
                "HistoricPostContents",
                "ImageContents",
                "LineContents",
                "NoteContents",
                "PhotoContents",
                "PointContents",
                "PostContents"
            };

            foreach (var loopTable in tableList)
            {
                Execute.Sql(@$"ALTER TABLE {loopTable} 
                    ADD COLUMN IsDraft INTEGER 
                    NOT NULL DEFAULT 0");

                Execute.Sql(@$"ALTER TABLE {loopTable} 
                    ADD COLUMN FeedOn Text 
                    NOT NULL DEFAULT '{DateTime.Now}'");

                Execute.Sql(@$"Update {loopTable} set FeedOn = CreatedOn");
            }
        }
    }
}