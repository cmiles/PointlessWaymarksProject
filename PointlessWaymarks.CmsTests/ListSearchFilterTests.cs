using NUnit.Framework;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsTests;

public class ListSearchFilterTests
{
    [Test]
    public void FocalLengthBlankSearchAndBlankFocalLength_Include()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterFocalLength(null, string.Empty).Include);
    }

    [Test]
    public void FocalLengthBlankSearchAndFocalLength_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterFocalLength("99", null).Include);
    }

    [TestCase("21")]
    [TestCase("21.0mm")]
    [TestCase("21.00000")]
    [TestCase("21.001")]
    [TestCase("19")]
    [TestCase("18.9mm")]
    [TestCase("19.0000 mm")]
    public void FocalLengthEqualTolerance_DoNotInclude(string photoFocalLength)
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterFocalLength(photoFocalLength, "Focal Length: ==20").Include);
    }

    [TestCase("20")]
    [TestCase("20.0mm")]
    [TestCase("20.00000")]
    [TestCase("20.008")]
    [TestCase("19.9")]
    [TestCase("19.09mm")]
    [TestCase("20.90 mm")]
    public void FocalLengthEqualTolerance_Include(string photoFocalLength)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterFocalLength(photoFocalLength, " == 20").Include);
    }

    [Test]
    public void FocalLengthIs100_DoubleEquals100mmString()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterFocalLength("100 mm", "Focal Length: ==100mm").Include);
    }

    [Test]
    public void FocalLengthIs100_Simple100String()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterFocalLength("100 mm", "Focal Length: 100").Include);
    }

    [Test]
    public void FocalLengthIs100_SingleEquals100String()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterFocalLength("100 mm", " = 100").Include);
    }

    [TestCase("50")]
    [TestCase("100mm")]
    [TestCase("100")]
    [TestCase("100 mm")]
    [TestCase("1000")]
    [TestCase("1000mm")]
    [TestCase("1000 mm")]
    [TestCase("500")]
    [TestCase("500mm")]
    [TestCase("500 mm")]
    public void FocalLengthIsGreaterThan100AndLessThan500_DoNotInclude(string photoFocalLength)
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterFocalLength(photoFocalLength, ">100 < 500mm").Include);
    }

    [TestCase("50")]
    [TestCase("100mm")]
    [TestCase("100")]
    [TestCase("100 mm")]
    [TestCase("75")]
    [TestCase("75mm")]
    [TestCase("75 mm")]
    public void FocalLengthIsGreaterThan100AndLessThan500_Include(string photoFocalLength)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterFocalLength(photoFocalLength, "Focal Length: >= 50mm <=100 mm")
            .Include);
    }

    [Test]
    public void FocalLengthSearchAndBlankFocalLength_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterFocalLength(string.Empty, "== 99").Include);
    }
}