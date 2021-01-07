using NUnit.Framework;
using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsTests
{
    public class FolderFileUtilityTests
    {
        [TestCase("this-has-a-capital-0E0")]
        [TestCase("this hasaspaces")]
        [TestCase("this_has_an_asterisk*")]
        [TestCase("this_has_>_greater_than")]
        [TestCase("this_has_<_less_than")]
        [TestCase("this.has.periods")]
        [TestCase("this/has_slash")]
        [TestCase("this\\has_backslash")]
        public void NoEncodingLowerShouldFailTest(string toFail)
        {
            Assert.False(FolderFileUtility.IsNoUrlEncodingNeededLowerCase(toFail));
        }

        [TestCase("this-is-simple-lower-case-hyphen")]
        [TestCase("this_is_simple_lower_case_underscore")]
        [TestCase("nospaces")]
        [TestCase("this-has-009-0-numbers")]
        [TestCase("000-starts-with-numbers")]
        [TestCase("ends-with-numbers")]
        public void NoEncodingShouldPassTest(string toFail)
        {
            Assert.True(FolderFileUtility.IsNoUrlEncodingNeededLowerCase(toFail));
        }
    }
}