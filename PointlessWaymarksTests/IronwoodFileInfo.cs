using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using PointlessWaymarksCmsData;
using PointlessWaymarksCmsData.Content;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Html;
using PointlessWaymarksCmsData.Json;EF

namespace PointlessWaymarksTests
{
    public static class IronwoodFileInfo
    {
        public static FileContent MapContent01 =>
            new FileContent
            {
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                BodyContent = "A map of Ironwood Forest National Monument",
                ContentId = Guid.NewGuid(),
                CreatedBy = "File Test",
                CreatedOn = new DateTime(2020, 7, 24, 5, 55, 55),
                Folder = "Maps",
                PublicDownloadLink = true,
                Title = "Ironwood Forest National Monument Map",
                ShowInMainSiteFeed = false,
                Slug = SlugUtility.Create(true, "Ironwood Forest National Monument Map"),
                Summary = "A map of Ironwood.",
                Tags = "ironwood forest national monument,map",
                UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
            };

        public static string MapFilename => "AZ_IronwoodForest_NM_map.pdf";
        public static string ProclamationFilename => "ironwood_proc.pdf";

        public static void CheckFileCountsAfterHtmlGeneration(FileContent newContent)
        {
            var contentDirectory = UserSettingsSingleton.CurrentSettings()
                .LocalSiteFileContentDirectory(newContent, false);
            Assert.True(contentDirectory.Exists, "Content Directory Not Found?");

            var expectedNumberOfFiles = 3;
            Assert.AreEqual(contentDirectory.GetFiles().Length, expectedNumberOfFiles,
                "Expected Number of Files Does Not Match");
        }

        public static void CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(FileContent newContent)
        {
            var expectedDirectory = UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(newContent);
            Assert.IsTrue(expectedDirectory.Exists, $"Expected directory {expectedDirectory.FullName} does not exist");

            var expectedFile = UserSettingsSingleton.CurrentSettings().LocalSiteFileHtmlFile(newContent);
            Assert.IsTrue(expectedFile.Exists, $"Expected html file {expectedFile.FullName} does not exist");

            var expectedOriginalFileInContent =
                new FileInfo(Path.Combine(expectedDirectory.FullName, newContent.OriginalFileName));
            Assert.IsTrue(expectedOriginalFileInContent.Exists,
                $"Expected to find original file in content directory but {expectedOriginalFileInContent.FullName} does not exist");

            var expectedOriginalFileInMediaArchive = new FileInfo(Path.Combine(
                UserSettingsSingleton.CurrentSettings().LocalMediaArchiveFileDirectory().FullName,
                expectedOriginalFileInContent.Name));
            Assert.IsTrue(expectedOriginalFileInMediaArchive.Exists,
                $"Expected to find original file in media archive file directory but {expectedOriginalFileInMediaArchive.FullName} does not exist");
        }

        public static (bool hasInvalidComparison, string comparisonNotes) Compare(FileContent reference,
            FileContent toCompare)
        {
            Db.DefaultPropertyCleanup(reference);
            reference.Tags = Db.TagListCleanup(reference.Tags);

            Db.DefaultPropertyCleanup(toCompare);
            toCompare.Tags = Db.TagListCleanup(toCompare.Tags);

            var compareLogic = new CompareLogic
            {
                Config = {MembersToIgnore = new List<string> {"ContentId", "ContentVersion", "Id"}}
            };

            var compareResult = compareLogic.Compare(reference, toCompare);

            return (compareResult.AreEqual, compareResult.DifferencesString);
        }

        public static async Task HtmlChecks(FileContent newFileContent)
        {
            var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSiteFileHtmlFile(newFileContent);

            Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

            var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

            await IronwoodHtmlHelpers.CommonContentChecks(document, newFileContent);
        }

        public static void JsonTest(FileContent newContent)
        {
            //Check JSON File
            var jsonFile =
                new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalSiteFileContentDirectory(newContent).FullName,
                    $"{Names.FileContentPrefix}{newContent.ContentId}.json"));
            Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

            var jsonFileImported = Import.ContentFromFiles<FileContent>(
                new List<string> {jsonFile.FullName}, Names.FileContentPrefix).Single();
            var compareLogic = new CompareLogic();
            var comparisonResult = compareLogic.Compare(newContent, jsonFileImported);
            Assert.True(comparisonResult.AreEqual,
                $"Json Import does not match expected File Content {comparisonResult.DifferencesString}");
        }

        public static async Task FileTest(string fileName, FileContent newFileReference)
        {
            var testFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "IronwoodTestContent", fileName));
            Assert.True(testFile.Exists, "Test File Found");

            var validationReturn = await FileGenerator.Validate(newFileReference, testFile);
            Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

            var saveReturn = await FileGenerator.SaveAndGenerateHtml(newFileReference, testFile, true,
                IronwoodTests.DebugProgressTracker());
            Assert.False(saveReturn.generationReturn.HasError,
                $"Unexpected Save Error - {saveReturn.generationReturn.GenerationNote}");

            CheckOriginalFileInContentAndMediaArchiveAfterHtmlGeneration(newFileReference);

            CheckFileCountsAfterHtmlGeneration(newFileReference);

            JsonTest(newFileReference);

            await HtmlChecks(newFileReference);
        }
    }
}