using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using Omu.ValueInjecter;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Json;

namespace PointlessWaymarks.CmsTests
{
    public static class IronwoodNoteInfo
    {
        public static NoteContent LinkNoteContent01 =>
            new()
            {
                BodyContent = @"Some useful links for Ironwood Forest National Monument:
* [Ironwood Forest National Monument | Bureau of Land Management](https://www.blm.gov/visit/ironwood)
* [Home - Friends of Ironwood Forest](https://ironwoodforest.org/)
* [Ironwood Forest National Monument - Wikipedia](https://en.wikipedia.org/wiki/Ironwood_Forest_National_Monument)
* [Ironwood Forest](https://www.desertmuseum.org/programs/ifnm_index.php)",
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                ContentId = Guid.NewGuid(),
                CreatedBy = "Note Test",
                CreatedOn = new DateTime(2020, 8, 17, 7, 17, 17),
                Folder = "IronwoodForest",
                ShowInMainSiteFeed = true,
                Summary = "Basic links for Ironwood Forest National Monument",
                Tags = "ironwood forest national monument, excluded tag",
                Slug = NoteGenerator.UniqueNoteSlug().Result
            };

        public static async Task CheckForExpectedFilesAfterHtmlGeneration(NoteContent newContent)
        {
            var contentDirectory = UserSettingsSingleton.CurrentSettings()
                .LocalSiteNoteContentDirectory(newContent, false);
            Assert.True(contentDirectory.Exists, "Content Directory Not Found?");

            var filesInDirectory = contentDirectory.GetFiles().ToList();

            Assert.True(filesInDirectory.Any(x => x.Name == $"{newContent.Slug}.html"),
                "Note Html file not found in Content Directory");

            Assert.True(filesInDirectory.Any(x => x.Name == $"{Names.NoteContentPrefix}{newContent.ContentId}.json"),
                "Note Json file not found in Content Directory");

            var db = await Db.Context();
            if (db.HistoricNoteContents.Any(x => x.ContentId == newContent.ContentId))
            {
                var historicJsonFile = filesInDirectory.SingleOrDefault(x =>
                    x.Name == $"{Names.HistoricNoteContentPrefix}{newContent.ContentId}.json");

                Assert.NotNull(historicJsonFile, "Historic Note Json File not Found in Content Directory");
            }
        }

        public static (bool areEqual, string comparisonNotes) CompareContent(NoteContent reference,
            NoteContent toCompare)
        {
            Db.DefaultPropertyCleanup(reference);
            reference.Tags = Db.TagListCleanup(reference.Tags);
            if (string.IsNullOrWhiteSpace(reference.CreatedBy))
                reference.CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;

            Db.DefaultPropertyCleanup(toCompare);
            toCompare.Tags = Db.TagListCleanup(toCompare.Tags);

            var compareLogic = new CompareLogic
            {
                Config = {MembersToIgnore = new List<string> {"ContentId", "ContentVersion", "Id"}}
            };

            var compareResult = compareLogic.Compare(reference, toCompare);

            return (compareResult.AreEqual, compareResult.DifferencesString);
        }

        public static async Task HtmlChecks(NoteContent newContent)
        {
            var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSiteNoteHtmlFile(newContent);

            Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

            var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

            await IronwoodHtmlHelpers.CommonContentChecks(document, newContent);
        }

        public static void JsonTest(NoteContent newContent)
        {
            //Check JSON File
            var jsonFile =
                new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalSiteNoteContentDirectory(newContent).FullName,
                    $"{Names.NoteContentPrefix}{newContent.ContentId}.json"));
            Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

            var jsonFileImported = Import.ContentFromFiles<NoteContent>(
                new List<string> {jsonFile.FullName}, Names.NoteContentPrefix).Single();
            var compareLogic = new CompareLogic();
            var comparisonResult = compareLogic.Compare(newContent, jsonFileImported);
            Assert.True(comparisonResult.AreEqual,
                $"Json Import does not match expected File Content {comparisonResult.DifferencesString}");
        }

        public static async Task<NoteContent> NoteTest(NoteContent contentReference)
        {
            var contentToSave = new NoteContent();
            contentToSave.InjectFrom(contentReference);

            var validationReturn = await NoteGenerator.Validate(contentToSave);
            Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

            var (generationReturn, newContent) =
                await NoteGenerator.SaveAndGenerateHtml(contentToSave, null, DebugTrackers.DebugProgressTracker());
            Assert.False(generationReturn.HasError, $"Unexpected Save Error - {generationReturn.GenerationNote}");

            var contentComparison = CompareContent(contentReference, newContent);
            Assert.True(contentComparison.areEqual, contentComparison.comparisonNotes);

            await CheckForExpectedFilesAfterHtmlGeneration(newContent);

            JsonTest(newContent);

            await HtmlChecks(newContent);

            return newContent;
        }
    }
}