using System;
using System.Linq;
using NUnit.Framework;
using PointlessWaymarksCmsData.CommonHtml;

namespace PointlessWaymarksTests
{
    public class BracketCodeTests
    {
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