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
    public void IsoSearchAndBlankIso_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterIso(string.Empty, "== 99").Include);
    }
    
       [Test]
    public void IsoBlankSearchAndBlankIso_Include()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterIso(null, string.Empty).Include);
    }

    [Test]
    public void IsoBlankSearchAndIso_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterIso("99", null).Include);
    }

    [Test]
    public void IsoIs100_Simple100String()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterIso("100", "Iso: 100").Include);
    }
    
    [TestCase("50")]
    [TestCase("100")]
    [TestCase("1000")]
    [TestCase("500")]
    [TestCase("500 ")]
    [TestCase(" 500")]
    public void IsoIsGreaterThan100AndLessThan500_DoNotInclude(string photoIso)
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterIso(photoIso, ">100 < 500").Include);
    }

    [TestCase("50")]
    [TestCase("100")]
    [TestCase("100 ")]
    [TestCase(" 100")]
    [TestCase("75")]
    [TestCase(" 75")]
    [TestCase("75 ")]
    public void IsoIsGreaterThan100AndLessThan500_Include(string photoIso)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterIso(photoIso, "Iso: >= 50 <=100")
            .Include);
    }

    [Test]
    public void StringContainsBlankItemAndBlankSearch_Include()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterStringContains(string.Empty, null, "Test String").Include);
    }
    
    [Test]
    public void StringContainsNotBlankItemAndBlankSearch_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterStringContains("A Nice String", null, "Test String").Include);
    }
    
    [Test]
    public void StringContainsBlankItemAndNotBlankSearch_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterStringContains(string.Empty, "Mountains ", "Test String").Include);
    }

    [TestCase("A")]
    [TestCase("    mountain ")]
    [TestCase("sce ")]
    [TestCase(" .")]
    [TestCase("(")]
    [TestCase("g)")]
    [TestCase("interesting")]
    public void StringContains_Include(string searchString)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterStringContains("An (interesting) MOUNTAIN scene. ", searchString, "Summary")
            .Include);
    }
    
    [TestCase("Ocean")]
    [TestCase("    () ")]
    [TestCase("   river")]
    [TestCase("canyon   ")]
    [TestCase("z")]
    public void StringContains_DoNotInclude(string searchString)
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterStringContains("An (interesting) MOUNTAIN scene. ", searchString, "Summary")
            .Include);
    }
    
}