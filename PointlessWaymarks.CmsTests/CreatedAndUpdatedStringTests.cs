using NUnit.Framework;
using PointlessWaymarks.CmsData.Database.Models;
using static PointlessWaymarks.CmsData.CommonHtml.Tags;

namespace PointlessWaymarks.CmsTests
{
    public class CreatedAndUpdatedStringTests
    {
        [Test]
        public void CreatedAndUpdatedString_CreatedOnly()
        {
            var testData = new CreatedAndUpdatedTestStructure
            {
                CreatedBy = "Creating Creator", CreatedOn = new DateTime(2020, 10, 9, 8, 7, 6, 5)
            };

            var createdAndUpdatedString = CreatedByAndUpdatedOnString(testData);

            Assert.AreEqual(
                $"Created by {testData.CreatedBy} on {CreatedByAndUpdatedOnFormattedDateTimeString(testData.CreatedOn)}.",
                createdAndUpdatedString);
        }

        [Test]
        public void CreatedAndUpdatedString_DifferentCreatedAndUpdatedByAndUpdatedOnWithinSameDay()
        {
            var testData = new CreatedAndUpdatedTestStructure
            {
                CreatedBy = "Creating Creator",
                CreatedOn = new DateTime(2020, 10, 9, 8, 7, 6, 5),
                LastUpdatedBy = "Updating Updater",
                LastUpdatedOn = new DateTime(2020, 10, 9, 12, 7, 6, 5)
            };

            var createdAndUpdatedString = CreatedByAndUpdatedOnString(testData);

            Assert.AreEqual(
                $"Created by {testData.CreatedBy} on {CreatedByAndUpdatedOnFormattedDateTimeString(testData.CreatedOn)}." +
                $" Updated by {testData.LastUpdatedBy} on {CreatedByAndUpdatedOnFormattedDateTimeString(testData.LastUpdatedOn.Value)}.",
                createdAndUpdatedString);
        }

        [Test]
        public void CreatedAndUpdatedString_DifferentCreatedAndUpdatedByDifferentCreatedAndUpdatedOn()
        {
            var testData = new CreatedAndUpdatedTestStructure
            {
                CreatedBy = "Creating Creator",
                CreatedOn = new DateTime(2020, 10, 9, 8, 7, 6, 5),
                LastUpdatedBy = "Updating Updater",
                LastUpdatedOn = new DateTime(2020, 10, 10, 8, 7, 6, 5)
            };

            var createdAndUpdatedString = CreatedByAndUpdatedOnString(testData);

            Assert.AreEqual(
                $"Created by {testData.CreatedBy} on {CreatedByAndUpdatedOnFormattedDateTimeString(testData.CreatedOn)}." +
                $" Updated by {testData.LastUpdatedBy} on {CreatedByAndUpdatedOnFormattedDateTimeString(testData.LastUpdatedOn.Value)}.",
                createdAndUpdatedString);
        }

        [Test]
        public void CreatedAndUpdatedString_SameCreatedAndUpdatedByCreatedAndUpdatedOnWithinSameDay()
        {
            var testData = new CreatedAndUpdatedTestStructure
            {
                CreatedBy = "Creating Creator",
                CreatedOn = new DateTime(2020, 10, 9, 8, 7, 6, 5),
                LastUpdatedBy = "Creating Creator",
                LastUpdatedOn = new DateTime(2020, 10, 9, 8, 7, 6, 5)
            };

            var createdAndUpdatedString = CreatedByAndUpdatedOnString(testData);

            Assert.AreEqual(
                $"Created and Updated by {testData.CreatedBy} on {CreatedByAndUpdatedOnFormattedDateTimeString(testData.CreatedOn)}.",
                createdAndUpdatedString);
        }

        [Test]
        public void CreatedAndUpdatedString_SameCreatedAndUpdatedByDifferentCreatedAndUpdatedOn()
        {
            var testData = new CreatedAndUpdatedTestStructure
            {
                CreatedBy = "Creating Creator",
                CreatedOn = new DateTime(2020, 10, 9, 8, 7, 6, 5),
                LastUpdatedBy = "Creating Creator",
                LastUpdatedOn = new DateTime(2020, 11, 9, 8, 7, 6, 5)
            };

            var createdAndUpdatedString = CreatedByAndUpdatedOnString(testData);

            Assert.AreEqual(
                $"Created by {testData.CreatedBy} on {CreatedByAndUpdatedOnFormattedDateTimeString(testData.CreatedOn)}." +
                $" Updated on {CreatedByAndUpdatedOnFormattedDateTimeString(testData.LastUpdatedOn.Value)}.",
                createdAndUpdatedString);
        }

        private class CreatedAndUpdatedTestStructure : ICreatedAndLastUpdateOnAndBy
        {
            public string CreatedBy { get; set; }
            public DateTime CreatedOn { get; set; }
            public string LastUpdatedBy { get; set; }
            public DateTime? LastUpdatedOn { get; set; }
            public DateTime LatestUpdate => LastUpdatedOn ?? CreatedOn;
        }
    }
}