using System.Data;
using FluentMigrator;

namespace PointlessWaymarks.CmsData.Database.Migrations;

[Migration(202411291900)]
public class ModifyTrailFeeDogBikeColumnTypes : Migration
{
    public override void Down()
    {
        throw new DataException("No Down Available for Migration ModifyTrailFeeDogBikeColumnTypes");
    }

    public override void Up()
    {
        // Check if the current Bikes column is of type integer
        var bikesColumnType = string.Empty;
        Execute.WithConnection((connection, transaction) =>
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "PRAGMA table_info(TrailContents)";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader["name"].ToString() == "Bikes")
                {
                    bikesColumnType = reader["type"].ToString();
                    return;
                }
            }
        });

        if (bikesColumnType == "INTEGER")
        {
            return;
        }

        Execute.Sql("""
                    -- Rename the existing columns
                    ALTER TABLE TrailContents RENAME COLUMN Bikes TO BikesString;
                    ALTER TABLE TrailContents RENAME COLUMN Dogs TO DogsString;
                    ALTER TABLE TrailContents RENAME COLUMN Fee TO FeeString;

                    -- Add new integer columns to represent boolean values
                    ALTER TABLE TrailContents ADD COLUMN Bikes INTEGER DEFAULT 0;
                    ALTER TABLE TrailContents ADD COLUMN Dogs INTEGER DEFAULT 0;
                    ALTER TABLE TrailContents ADD COLUMN Fees INTEGER DEFAULT 0;
                    ALTER TABLE TrailContents RENAME COLUMN FeeNote TO FeesNote;
                    
                    -- Update the new integer columns based on the values of the renamed columns
                    UPDATE TrailContents SET Bikes = CASE WHEN BikesString = 'Yes' THEN 1 ELSE 0 END;
                    UPDATE TrailContents SET Dogs = CASE WHEN DogsString = 'Yes' THEN 1 ELSE 0 END;
                    UPDATE TrailContents SET Fees = CASE WHEN FeeString = 'Yes' THEN 1 ELSE 0 END;

                    -- Drop the renamed columns
                    ALTER TABLE TrailContents DROP COLUMN BikesString;
                    ALTER TABLE TrailContents DROP COLUMN DogsString;
                    ALTER TABLE TrailContents DROP COLUMN FeeString;

                    -- Repeat the same steps for HistoricTrailContents table
                    -- Rename the existing columns
                    ALTER TABLE HistoricTrailContents RENAME COLUMN Bikes TO BikesString;
                    ALTER TABLE HistoricTrailContents RENAME COLUMN Dogs TO DogsString;
                    ALTER TABLE HistoricTrailContents RENAME COLUMN Fee TO FeeString;

                    -- Add new integer columns to represent boolean values
                    ALTER TABLE HistoricTrailContents ADD COLUMN Bikes INTEGER DEFAULT 0;
                    ALTER TABLE HistoricTrailContents ADD COLUMN Dogs INTEGER DEFAULT 0;
                    ALTER TABLE HistoricTrailContents ADD COLUMN Fees INTEGER DEFAULT 0;
                    ALTER TABLE HistoricTrailContents RENAME COLUMN FeeNote TO FeesNote;
                    
                    -- Update the new integer columns based on the values of the renamed columns
                    UPDATE HistoricTrailContents SET Bikes = CASE WHEN BikesString = 'Yes' THEN 1 ELSE 0 END;
                    UPDATE HistoricTrailContents SET Dogs = CASE WHEN DogsString = 'Yes' THEN 1 ELSE 0 END;
                    UPDATE HistoricTrailContents SET Fees = CASE WHEN FeeString = 'Yes' THEN 1 ELSE 0 END;

                    -- Drop the renamed columns
                    ALTER TABLE HistoricTrailContents DROP COLUMN BikesString;
                    ALTER TABLE HistoricTrailContents DROP COLUMN DogsString;
                    ALTER TABLE HistoricTrailContents DROP COLUMN FeeString;
                    """);
    }
}