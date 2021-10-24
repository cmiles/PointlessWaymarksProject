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
    public static class IronwoodPostInfo
    {
        public static PostContent WikiQuotePostContent01 =>
            new()
            {
                Title = "First Post",
                Slug = "first-post",
                BodyContent = @"A Test Post for Ironwood Forest National Monument. From
Wikipedia:
> Ironwood Forest National Monument is located in the Sonoran Desert of Arizona. Created by Bill Clinton by Presidential Proclamation 7320 on June 9, 2000, the monument is managed by the Bureau of Land Management, an agency within the United States Department of the Interior. The monument covers 129,055 acres (52,227 ha),[2] of which 59,573 acres (24,108 ha) are non-federal and include private land holdings and Arizona State School Trust lands.

A significant concentration of ironwood (also known as desert ironwood, Olneya tesota) trees is found in the monument, along with two federally recognized endangered animal and plant species. More than 200 Hohokam and Paleo-Indian archaeological sites have been identified in the monument, dated between 600 and 1450.",
                BodyContentFormat = ContentFormatDefaults.Content.ToString(),
                ContentId = Guid.NewGuid(),
                CreatedBy = "Post Test",
                CreatedOn = new DateTime(2020, 8, 18, 7, 16, 16),
                FeedOn = new DateTime(2020, 8, 18, 7, 16, 16),
                Folder = "IronwoodForest",
                ShowInMainSiteFeed = true,
                Summary = "Basic information for Ironwood Forest National Monument",
                Tags = "ironwood forest national monument, excluded tag",
                UpdateNotesFormat = ContentFormatDefaults.Content.ToString()
            };

        public static async Task CheckForExpectedFilesAfterHtmlGeneration(PostContent newContent)
        {
            var contentDirectory = UserSettingsSingleton.CurrentSettings()
                .LocalSitePostContentDirectory(newContent, false);
            Assert.True(contentDirectory.Exists, "Content Directory Not Found?");

            var filesInDirectory = contentDirectory.GetFiles().ToList();

            Assert.True(filesInDirectory.Any(x => x.Name == $"{newContent.Slug}.html"),
                "Post Html file not found in Content Directory");

            Assert.True(filesInDirectory.Any(x => x.Name == $"{Names.PostContentPrefix}{newContent.ContentId}.json"),
                "Post Json file not found in Content Directory");

            var db = await Db.Context();
            if (db.HistoricPostContents.Any(x => x.ContentId == newContent.ContentId))
            {
                var historicJsonFile = filesInDirectory.SingleOrDefault(x =>
                    x.Name == $"{Names.HistoricPostContentPrefix}{newContent.ContentId}.json");

                Assert.NotNull(historicJsonFile, "Historic Post Json File not Found in Content Directory");
            }
        }

        public static (bool areEqual, string comparisonNotes) CompareContent(PostContent reference,
            PostContent toCompare)
        {
            Db.DefaultPropertyCleanup(reference);
            reference.Tags = Db.TagListCleanup(reference.Tags);
            if (string.IsNullOrWhiteSpace(reference.CreatedBy))
                reference.CreatedBy = UserSettingsSingleton.CurrentSettings().DefaultCreatedBy;

            Db.DefaultPropertyCleanup(toCompare);
            toCompare.Tags = Db.TagListCleanup(toCompare.Tags);

            var compareLogic = new CompareLogic
            {
                Config = { MembersToIgnore = new List<string> { "ContentId", "ContentVersion", "Id" } }
            };

            var compareResult = compareLogic.Compare(reference, toCompare);

            return (compareResult.AreEqual, compareResult.DifferencesString);
        }

        public static async Task HtmlChecks(PostContent newContent)
        {
            var htmlFile = UserSettingsSingleton.CurrentSettings().LocalSitePostHtmlFile(newContent);

            Assert.True(htmlFile.Exists, "Html File not Found for Html Checks?");

            var document = IronwoodHtmlHelpers.DocumentFromFile(htmlFile);

            await IronwoodHtmlHelpers.CommonContentChecks(document, newContent);
        }

        public static void JsonTest(PostContent newContent)
        {
            //Check JSON File
            var jsonFile =
                new FileInfo(Path.Combine(
                    UserSettingsSingleton.CurrentSettings().LocalSitePostContentDirectory(newContent).FullName,
                    $"{Names.PostContentPrefix}{newContent.ContentId}.json"));
            Assert.True(jsonFile.Exists, $"Json file {jsonFile.FullName} does not exist?");

            var jsonFileImported = Import.ContentFromFiles<PostContent>(
                new List<string> { jsonFile.FullName }, Names.PostContentPrefix).Single();
            var compareLogic = new CompareLogic();
            var comparisonResult = compareLogic.Compare(newContent, jsonFileImported);
            Assert.True(comparisonResult.AreEqual,
                $"Json Import does not match expected File Content {comparisonResult.DifferencesString}");
        }


        public static async Task<PostContent> PostTest(PostContent contentReference)
        {
            var contentToSave = new PostContent();
            contentToSave.InjectFrom(contentReference);

            var validationReturn = await PostGenerator.Validate(contentToSave);
            Assert.False(validationReturn.HasError, $"Unexpected Validation Error - {validationReturn.GenerationNote}");

            var (generationReturn, newContent) =
                await PostGenerator.SaveAndGenerateHtml(contentToSave, null, DebugTrackers.DebugProgressTracker());
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