﻿using NUnit.Framework;
using PointlessWaymarks.CmsWpfControls.ContentList;

namespace PointlessWaymarks.CmsTests;

public class ListSearchFilterTests
{
    [Test]
    public void ApertureBlankSearchAndAperture_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterAperture("f 8.0", null).Include);
    }

    [Test]
    public void ApertureBlankSearchAndBlankAperture_Include()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterAperture(null, string.Empty).Include);
    }

    [TestCase("Aperture: <4 >16")]
    [TestCase("Aperture: < f8.0 > f64")]
    [TestCase("Aperture: > 64.0 <4")]
    [TestCase("Aperture:<=f11.00 >f13")]
    public void ApertureIsBetween_Include(string searchString)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterAperture("f 11.0", searchString).Include);
    }

    [TestCase("Aperture: 11")]
    [TestCase("f11")]
    [TestCase("11")]
    [TestCase("11.00")]
    public void ApertureIsEqual_Include(string searchString)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterAperture("f 11", searchString).Include);
    }

    [TestCase("Aperture: >11")]
    [TestCase("Aperture: > f64")]
    [TestCase("Aperture: > f 64")]
    [TestCase("Aperture:>=f8")]
    [TestCase(">f9 ")]
    [TestCase(" >16")]
    [TestCase(">  11.00  ")]
    public void ApertureIsGreaterThan_Include(string searchString)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterAperture("f 8.0", searchString).Include);
    }

    [TestCase("Aperture: <4")]
    [TestCase("Aperture: < f8.0")]
    [TestCase("Aperture: < f 11")]
    [TestCase("Aperture:<=f16.00")]
    [TestCase("<f 9 ")]
    [TestCase(" < 8")]
    [TestCase("<  11.00  ")]
    public void ApertureIsLessThan_Include(string searchString)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterAperture("f16", searchString).Include);
    }

    [TestCase("Aperture: <4 >16")]
    [TestCase("Aperture: < f8.0 > f64")]
    [TestCase("Aperture: > 64.0 <4")]
    [TestCase("Aperture:<=f11.00 >f13")]
    public void ApertureIsNotBetween_Include(string searchString)
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterAperture("f 4", searchString).Include);
    }

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
        Assert.IsTrue(ContentListSearchFunctions.FilterIso(photoIso, "Iso: >= 50 <=100").Include);
    }

    [Test]
    public void IsoSearchAndBlankIso_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterIso(string.Empty, "== 99").Include);
    }

    [TestCase("1/1    ")]
    [TestCase(" >    3pm ")]
    [TestCase("Jan 1 2022")]
    [TestCase("Jan 1 2022 2pm   ")]
    [TestCase(" <= Jan 1 2022 2pm")]
    [TestCase(" == December, 19, 1999 ")]
    public void OperatorTokenList_OneToken(string noOperatorSearch)
    {
        Assert.AreEqual(ContentListSearchFunctions.FilterListOperatorDividedTokenList(noOperatorSearch).Count, 1);
    }

    [TestCase("1/1    ", "")]
    [TestCase(" >    3pm ", ">")]
    [TestCase("Jan 1 2022", "")]
    [TestCase(" <Jan 1 2022", "<")]
    [TestCase(" =Jan 1 2022", "=")]
    [TestCase(" != Jan 1 2022", "!=")]
    [TestCase(" >=   Jan 1 2022 2pm   ", ">=")]
    [TestCase(" <= Jan 1 2022 2pm", "<=")]
    [TestCase(" == December, 19, 1999 ", "==")]
    public void OperatorTokenList_OneTokenOperatorProperlyDetected(string noOperatorSearch, string operatorString)
    {
        Assert.AreEqual(ContentListSearchFunctions.FilterListOperatorDividedTokenList(noOperatorSearch).Count, 1);
        Assert.AreEqual(
            ContentListSearchFunctions.FilterListOperatorDividedTokenList(noOperatorSearch).First().operatorString,
            operatorString);
    }

    [TestCase("1/1    ", "1/1")]
    [TestCase(" >    3pm ", "3pm")]
    [TestCase("Jan 1 2022 2pm   ", "Jan 1 2022 2pm")]
    [TestCase(" <= Jan 1 2022 2pm", "Jan 1 2022 2pm")]
    [TestCase(" == December, 19, 1999 ", "December, 19, 1999")]
    public void OperatorTokenList_SpacesAreProperlyReduced(string noOperatorSearch, string spaceReducedSearch)
    {
        Assert.AreEqual(ContentListSearchFunctions.FilterListOperatorDividedTokenList(noOperatorSearch).Count, 1);
        Assert.AreEqual(
            ContentListSearchFunctions.FilterListOperatorDividedTokenList(noOperatorSearch).First().searchString,
            spaceReducedSearch);
    }

    [TestCase("> 1/1   < 1/2 ", "<")]
    [TestCase(" >    3pm  <= 4pm", "<=")]
    [TestCase("!= Jan 1 2022 > 2pm", ">")]
    [TestCase(" <Jan 1 2022 >Jan 1 2021", ">")]
    [TestCase(" =Jan 1 2022 >= 3:53:01", ">=")]
    [TestCase(" != Jan 1 2022 < Dec 2", "<")]
    public void OperatorTokenList_TwoTokensOperatorProperlyDetected(string noOperatorSearch,
        string secondOperatorString)
    {
        Assert.AreEqual(ContentListSearchFunctions.FilterListOperatorDividedTokenList(noOperatorSearch).Count, 2);
        Assert.AreEqual(
            ContentListSearchFunctions.FilterListOperatorDividedTokenList(noOperatorSearch)[1].operatorString,
            secondOperatorString);
    }


    [Test]
    public void ShutterSpeedBlankSearchAndBlankShutterSpeed_Include()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterShutterSpeedLength(null, string.Empty).Include);
    }

    [Test]
    public void ShutterSpeedBlankSearchAndShutterSpeed_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterShutterSpeedLength("99", null).Include);
    }

    [TestCase("Shutter Speed: .5")]
    [TestCase("1/50")]
    [TestCase("1/800")]
    [TestCase("shutter speed:1/25")]
    public void ShutterSpeedIsEqual_DoNotInclude(string searchString)
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterShutterSpeedLength("1/250", searchString).Include);
    }

    [TestCase("Shutter Speed: .004")]
    [TestCase("1/250")]
    public void ShutterSpeedIsEqual_Include(string searchString)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterShutterSpeedLength("1/250", searchString).Include);
    }

    [TestCase("shutter speed: > 1/25 < 10.021")]
    [TestCase("shutter speed: > 1/32000 <= 2/1")]
    [TestCase("shutter speed: >= 2 <= 8")]
    public void ShutterSpeedIsInRange_Include(string searchString)
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterShutterSpeedLength("2.0", searchString).Include);
    }

    [TestCase("shutter speed: > 1/250 < 1/1000")]
    [TestCase("shutter speed: > 1/25 <= 1/999")]
    [TestCase("shutter speed: >= 2 <= 8")]
    public void ShutterSpeedIsOutOfRange_DoNotInclude(string searchString)
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterShutterSpeedLength("1/1000", searchString).Include);
    }

    [TestCase("Ocean")]
    [TestCase("    () ")]
    [TestCase("   river")]
    [TestCase("canyon   ")]
    [TestCase("z")]
    public void StringContains_DoNotInclude(string searchString)
    {
        Assert.IsFalse(ContentListSearchFunctions
            .FilterStringContains("An (interesting) MOUNTAIN scene. ", searchString, "Summary").Include);
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
        Assert.IsTrue(ContentListSearchFunctions
            .FilterStringContains("An (interesting) MOUNTAIN scene. ", searchString, "Summary").Include);
    }

    [Test]
    public void StringContainsBlankItemAndBlankSearch_Include()
    {
        Assert.IsTrue(ContentListSearchFunctions.FilterStringContains(string.Empty, null, "Test String").Include);
    }

    [Test]
    public void StringContainsBlankItemAndNotBlankSearch_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterStringContains(string.Empty, "Mountains ", "Test String")
            .Include);
    }

    [Test]
    public void StringContainsNotBlankItemAndBlankSearch_DoNotInclude()
    {
        Assert.IsFalse(ContentListSearchFunctions.FilterStringContains("A Nice String", null, "Test String").Include);
    }
}