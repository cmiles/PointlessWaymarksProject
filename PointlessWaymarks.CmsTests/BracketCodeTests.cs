using System;
using System.Linq;
using NUnit.Framework;
using PointlessWaymarks.CmsData.CommonHtml;

namespace PointlessWaymarks.CmsTests
{
    public class BracketCodeTests
    {
        [Test]
        public void BracketCodeContentIdsFindsAllMatches()
        {
            var testString = @"
A Test Post for Ironwood Forest National Monument. From
Wikipedia:
> Ironwood Forest National Monument is located in the Sonoran Desert of Arizona. Created by Bill Clinton by Presidential Proclamation 7320 on June 9, 2000, the monument is managed by the Bureau of Land Management, an agency within the United States Department of the Interior. The monument covers 129,055 acres (52,227 ha),[2] of which 59,573 acres (24,108 ha) are non-federal and include private land holdings and Arizona State School Trust lands.

A significant concentration of ironwood (also known as desert ironwood, Olneya tesota) trees is found in the monument, along with two federally recognized endangered animal and plant species. More than 200 Hohokam and Paleo-Indian archaeological sites have been identified in the monument, dated between 600 and 1450.

{{photo be010d97-a2b1-4c88-97ac-c36ebbd3fad4; 2020 June Disappearing into the Flower}}
{{photo ce493d74-516c-4e7d-a51f-2db458a834e3; 2020 May Ironwood Pod}}
{{photo 32563d87-d002-4672-a27e-d0b31c2a6875; 2020 May A Quarry in Ironwood Forest National Monument}}
{{photo d45ef8f8-e376-4144-acef-164310ee85bc; 2017 May Ironwood Tree Against The Sky}}
{{photo 611a88cd-908b-45bc-ac3c-a904b0c7a9c7; 2018 August Agua Blanca Ranch Sign at the Manville Road Entrance to the Ironwood Forest National Monument}}Basic information for Ironwood Forest National Monument
";
            var result = BracketCodeCommon.BracketCodeContentIds(testString);

            Assert.AreEqual(5, result.Count);
        }

        [Test]
        public void BracketCodeContentIdsFindsAllMatchesInBackToBackBracketCodes()
        {
            var testString = @"
A Test Post for Ironwood Forest National Monument. From
Wikipedia:
> Ironwood Forest National Monument is located in the Sonoran Desert of Arizona. Created by Bill Clinton by Presidential Proclamation 7320 on June 9, 2000, the monument is managed by the Bureau of Land Management, an agency within the United States Department of the Interior. The monument covers 129,055 acres (52,227 ha),[2] of which 59,573 acres (24,108 ha) are non-federal and include private land holdings and Arizona State School Trust lands.

A significant concentration of ironwood (also known as desert ironwood, Olneya tesota) trees is found in the monument, along with two federally recognized endangered animal and plant species. More than 200 Hohokam and Paleo-Indian archaeological sites have been identified in the monument, dated between 600 and 1450.{{photo efe8caac-9d62-4456-af54-873c0d6a0dce; 2018 August Agua Blanca Ranch Sign at the Manville Road Entrance to the Ironwood Forest National Monument}}{{photo d1bc6caf-2155-45f8-ac35-6b811535de0d; 2020 June Disappearing into the Flower}}{{photo 11d4c9e7-bfa0-489d-8dc0-69e93e3384de; 2020 May A Quarry in Ironwood Forest National Monument}}{{photo 481f4177-329b-46ae-8f53-b9f742a3d227; 2020 May Ironwood Pod}}{{photo 8526996b-71b5-4d31-85a8-4267619884b5; 2017 May Ironwood Tree Against The Sky}}Basic information for Ironwood Forest National Monument
";
            var result = BracketCodeCommon.BracketCodeContentIds(testString);

            Assert.AreEqual(5, result.Count);
        }

        [Test]
        public void ContentBracketCodeStringSingleWithoutTextParses()
        {
            var testGuid = Guid.NewGuid();
            var bracketCodeToken = "SpecialTestToken";
            var bracketCode = $"{{{{{bracketCodeToken} {testGuid}; Text that doesn't matter}}}}";

            var testString = $"This is a test {bracketCode} of the bracket text";

            var matches = BracketCodeCommon.ContentBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 1);
            Assert.AreEqual(testGuid, matches.First().contentGuid);
            Assert.AreEqual(bracketCode, matches.First().bracketCodeText);
            Assert.AreEqual(string.Empty, matches.First().displayText);
        }

        [Test]
        public void ContentBracketCodeStringSingleWithTextCasingCheckParses()
        {
            var testGuid = Guid.NewGuid();
            var bracketCodeToken = "SpecialTestToken";
            var displayText = "text that does matter";
            var bracketCode = $"{{{{{bracketCodeToken} {testGuid}; Text {displayText};}}}}";

            var testString = $"This is a test text {bracketCode} of the bracket string";

            var matches = BracketCodeCommon.ContentBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 1);
            Assert.AreEqual(testGuid, matches.First().contentGuid);
            Assert.AreEqual(bracketCode, matches.First().bracketCodeText);
            Assert.AreEqual(displayText, matches.First().displayText);
        }

        [Test]
        public void ContentBracketCodeStringSingleWithTextParses()
        {
            var testGuid = Guid.NewGuid();
            var bracketCodeToken = "SpecialTestToken";
            var displayText = "text that does matter";
            var bracketCode = $"{{{{{bracketCodeToken} {testGuid}; text {displayText};}}}}";

            var testString = $"This is a test {bracketCode} of text bracket string";

            var matches = BracketCodeCommon.ContentBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 1);
            Assert.AreEqual(testGuid, matches.First().contentGuid);
            Assert.AreEqual(bracketCode, matches.First().bracketCodeText);
            Assert.AreEqual(displayText, matches.First().displayText);
        }

        [Test]
        public void ContentBracketCodeStringTwoIdenticalAndOneDifferentWithoutTextAllParse()
        {
            var testGuid = Guid.NewGuid();
            var bracketCodeToken = "SpecialTestToken";
            var bracketCode = $"{{{{{bracketCodeToken} {testGuid}; Text that doesn't matter}}}}";

            var testGuid02 = Guid.NewGuid();
            var displayText02 = "Display text Test";
            var bracketCode02 = $"{{{{{bracketCodeToken} {testGuid02}; text {displayText02};}}}}";

            var testString =
                $"This is a test{bracketCode}{bracketCode}of the bracket string.{Environment.NewLine} And another {bracketCode02} test.";

            var matches = BracketCodeCommon.ContentBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 3);
            Assert.AreEqual(testGuid02, matches[0].contentGuid);
            Assert.AreEqual(testGuid, matches[1].contentGuid);
            Assert.AreEqual(testGuid, matches[2].contentGuid);

            Assert.AreEqual(bracketCode02, matches[0].bracketCodeText);
            Assert.AreEqual(bracketCode, matches[1].bracketCodeText);
            Assert.AreEqual(bracketCode, matches[2].bracketCodeText);

            Assert.AreEqual(displayText02, matches[0].displayText);
            Assert.AreEqual(string.Empty, matches[1].displayText);
            Assert.AreEqual(string.Empty, matches[2].displayText);
        }

        [Test]
        public void ContentBracketCodeStringTwoIdenticalWithoutTextBothParse()
        {
            var testGuid = Guid.NewGuid();
            var bracketCodeToken = "SpecialTestToken";
            var bracketCode = $"{{{{{bracketCodeToken} {testGuid}; Text that doesn't matter}}}}";

            var testString =
                $"This is a test{bracketCode}of the bracket string.{Environment.NewLine} And another text{bracketCode}.";

            var matches = BracketCodeCommon.ContentBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 2);
            Assert.AreEqual(testGuid, matches[0].contentGuid);
            Assert.AreEqual(testGuid, matches[1].contentGuid);
            Assert.AreEqual(bracketCode, matches[0].bracketCodeText);
            Assert.AreEqual(bracketCode, matches[1].bracketCodeText);
            Assert.AreEqual(string.Empty, matches[0].displayText);
            Assert.AreEqual(string.Empty, matches[1].displayText);
        }

        [Test]
        public void MixedContentAndSpecialPagesBracketCodeParse()
        {
            var testGuid = Guid.NewGuid();
            var bracketCodeToken = "SpecialTestToken";
            var bracketCode = $"{{{{{bracketCodeToken} {testGuid}; Text that doesn't matter}}}}";

            var testGuid02 = Guid.NewGuid();
            var displayText02 = "Display text Test";
            var bracketCode02 = $"{{{{{bracketCodeToken} {testGuid02}; text {displayText02};}}}}";

            var pageBracketCodeToken01 = "SpecialTestToken";
            var pageDisplayText = "text that does matter";
            var pageBracketCode01 = $"{{{{{pageBracketCodeToken01}; text {pageDisplayText};}}}}";

            var pageBracketCode02 = $"{{{{{pageBracketCodeToken01};}}}}";

            var testString =
                $"This is a test{bracketCode}{pageBracketCode02}{bracketCode}of the bracket string.{Environment.NewLine} And another {bracketCode02} test. {pageBracketCode01}";

            var contentMatches = BracketCodeCommon.ContentBracketCodeMatches(testString, bracketCodeToken);

            var pageMatches = BracketCodeCommon.SpecialPageBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(contentMatches.Count == 3);
            Assert.AreEqual(testGuid02, contentMatches[0].contentGuid);
            Assert.AreEqual(testGuid, contentMatches[1].contentGuid);
            Assert.AreEqual(testGuid, contentMatches[2].contentGuid);

            Assert.AreEqual(bracketCode02, contentMatches[0].bracketCodeText);
            Assert.AreEqual(bracketCode, contentMatches[1].bracketCodeText);
            Assert.AreEqual(bracketCode, contentMatches[2].bracketCodeText);

            Assert.AreEqual(displayText02, contentMatches[0].displayText);
            Assert.AreEqual(string.Empty, contentMatches[1].displayText);
            Assert.AreEqual(string.Empty, contentMatches[2].displayText);

            Assert.IsTrue(pageMatches.Count == 2);
            Assert.AreEqual(pageBracketCode01, pageMatches[0].bracketCodeText);
            Assert.AreEqual(pageBracketCode02, pageMatches[1].bracketCodeText);
            Assert.AreEqual(pageDisplayText, pageMatches[0].displayText);
        }


        [Test]
        public void PhotoOrImageCodeFirstIdInContentFindsFirstPhoto()
        {
            var testString = @"
A Test Post for Ironwood Forest National Monument. From
Wikipedia:
> Ironwood Forest National Monument is located in the Sonoran Desert of Arizona. Created by Bill Clinton by Presidential Proclamation 7320 on June 9, 2000, the monument is managed by the Bureau of Land Management, an agency within the United States Department of the Interior. The monument covers 129,055 acres (52,227 ha),[2] of which 59,573 acres (24,108 ha) are non-federal and include private land holdings and Arizona State School Trust lands.

A significant concentration of ironwood (also known as desert ironwood, Olneya tesota) trees is found in the monument, along with two federally recognized endangered animal and plant species. More than 200 Hohokam and Paleo-Indian archaeological sites have been identified in the monument, dated between 600 and 1450.

{{photo be010d97-a2b1-4c88-97ac-c36ebbd3fad4; 2020 June Disappearing into the Flower}}
{{photo ce493d74-516c-4e7d-a51f-2db458a834e3; 2020 May Ironwood Pod}}
{{photo 32563d87-d002-4672-a27e-d0b31c2a6875; 2020 May A Quarry in Ironwood Forest National Monument}}
{{photo d45ef8f8-e376-4144-acef-164310ee85bc; 2017 May Ironwood Tree Against The Sky}}
{{photo 611a88cd-908b-45bc-ac3c-a904b0c7a9c7; 2018 August Agua Blanca Ranch Sign at the Manville Road Entrance to the Ironwood Forest National Monument}}Basic information for Ironwood Forest National Monument
";
            var result = BracketCodeCommon.PhotoOrImageCodeFirstIdInContent(testString);

            Assert.AreEqual(Guid.Parse("be010d97-a2b1-4c88-97ac-c36ebbd3fad4"), result);
        }

        [Test]
        public void SpecialPageBracketCodeStringSingleWithoutTextParses()
        {
            var bracketCodeToken = "SpecialTestToken";
            var bracketCode = $"{{{{{bracketCodeToken};}}}}";

            var testString = $"This is a test {bracketCode} of the bracket text";

            var matches = BracketCodeCommon.SpecialPageBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 1);
            Assert.AreEqual(bracketCode, matches.First().bracketCodeText);
            Assert.AreEqual(string.Empty, matches.First().displayText);
        }

        [Test]
        public void SpecialPageBracketCodeStringSingleWithTextCasingCheckParses()
        {
            var bracketCodeToken = "SpecialTestToken";
            var displayText = "text that does matter";
            var bracketCode = $"{{{{{bracketCodeToken}; Text {displayText};}}}}";

            var testString = $"This is a test text {bracketCode} of the bracket string";

            var matches = BracketCodeCommon.SpecialPageBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 1);
            Assert.AreEqual(bracketCode, matches.First().bracketCodeText);
            Assert.AreEqual(displayText, matches.First().displayText);
        }

        [Test]
        public void SpecialPageBracketCodeStringSingleWithTextParses()
        {
            var bracketCodeToken = "SpecialTestToken";
            var displayText = "text that does matter";
            var bracketCode = $"{{{{{bracketCodeToken}; text {displayText};}}}}";

            var testString = $"This is a test {bracketCode} of the bracket text";

            var matches = BracketCodeCommon.SpecialPageBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 1);
            Assert.AreEqual(bracketCode, matches.First().bracketCodeText);
            Assert.AreEqual(displayText, matches.First().displayText);
        }

        [Test]
        public void SpecialPageBracketCodeStringTwoIdenticalAndOneDifferentWithoutTextAllParse()
        {
            var bracketCodeToken = "SpecialTestToken";
            var bracketCode = $"{{{{{bracketCodeToken};}}}}";

            var displayText02 = "Display text Test";
            var bracketCode02 = $"{{{{{bracketCodeToken}; text {displayText02};}}}}";

            var testString =
                $"This is a test{bracketCode}{bracketCode}of the bracket string.{Environment.NewLine} And another {bracketCode02} test.";

            var matches = BracketCodeCommon.SpecialPageBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 3);

            Assert.AreEqual(bracketCode02, matches[0].bracketCodeText);
            Assert.AreEqual(bracketCode, matches[1].bracketCodeText);
            Assert.AreEqual(bracketCode, matches[2].bracketCodeText);

            Assert.AreEqual(displayText02, matches[0].displayText);
            Assert.AreEqual(string.Empty, matches[1].displayText);
            Assert.AreEqual(string.Empty, matches[2].displayText);
        }

        [Test]
        public void SpecialPageBracketCodeStringTwoIdenticalWithoutTextBothParse()
        {
            var bracketCodeToken = "SpecialTestToken";
            var bracketCode = $"{{{{{bracketCodeToken};}}}}";

            var testString =
                $"This is a test{bracketCode}of the bracket string.{Environment.NewLine} And another text{bracketCode}.";

            var matches = BracketCodeCommon.SpecialPageBracketCodeMatches(testString, bracketCodeToken);

            Assert.IsTrue(matches.Count == 2);
            Assert.AreEqual(bracketCode, matches[0].bracketCodeText);
            Assert.AreEqual(bracketCode, matches[1].bracketCodeText);
            Assert.AreEqual(string.Empty, matches[0].displayText);
            Assert.AreEqual(string.Empty, matches[1].displayText);
        }
    }
}