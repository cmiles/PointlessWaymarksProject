using Codeuctivity.SkiaSharpCompare;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.UtilitarianImage;
using SkiaSharp;

namespace PointlessWaymarks.UtilitarianImageTests;

public class CombinerTests
{
    public FileInfo? BlueBottomTestImage { get; set; }
    public FileInfo? BlueTopTestImage { get; set; }
    public FileInfo? CactusForestDriveTestPdf { get; set; }
    public FileInfo? GreenBlueSquareTestImage { get; set; }
    public FileInfo? GreenDownLeftOfCenterTestImage { get; set; }
    public FileInfo? GreenDownRightOfCenterTestImage { get; set; }
    public DirectoryInfo? OriginalImageDirectory { get; set; }
    public FileInfo ReferenceResulEGridFiveRows { get; set; }
    public FileInfo ReferenceResulFGridThreeByThree { get; set; }
    public FileInfo? ReferenceResultARotatedVertical { get; set; }
    public FileInfo? ReferenceResultBRotatedHorizontal { get; set; }
    public FileInfo ReferenceResultCGridAutoRowsAndColumns { get; set; }
    public FileInfo? ReferenceResultDGridFourColumns { get; set; }
    public FileInfo? ResultARotatedVertical { get; set; }
    public FileInfo? ResultBRotatedHorizontal { get; set; }
    public FileInfo ResultCGridAutoRowsAndColumns { get; set; }
    public FileInfo? ResultDGridFourColumns { get; set; }
    public FileInfo ResultEGridFiveRows { get; set; }
    public FileInfo ResultFGridThreeByThreeRows { get; set; }
    public DirectoryInfo? UnitTestDirectory { get; set; }

    [Test]
    public void A_FilesExist()
    {
        Assert.That(UnitTestDirectory.Exists);
        Assert.That(BlueBottomTestImage.Exists);
        Assert.That(BlueTopTestImage.Exists);
        Assert.That(CactusForestDriveTestPdf.Exists);
        Assert.That(GreenBlueSquareTestImage.Exists);
        Assert.That(GreenDownLeftOfCenterTestImage.Exists);
        Assert.That(GreenDownRightOfCenterTestImage.Exists);
        Assert.That(ReferenceResultARotatedVertical.Exists);
        Assert.That(ReferenceResultBRotatedHorizontal.Exists);
    }

    [Test]
    public async Task A_RotatedVertical()
    {
        ResetTestFiles();

        var rotate1 = await Combiner.RotateLeft(BlueBottomTestImage.FullName);
        var rotate2 = await Combiner.Flip(BlueTopTestImage.FullName);
        var rotate3 = await Combiner.RotateRight(GreenBlueSquareTestImage.FullName);

        await Combiner.CombineImagesVertical(
            [rotate1.FullName, rotate2.FullName, rotate3.FullName], 3000, 3000,
            ResultARotatedVertical.FullName, 85, SKColors.Black,
            new ConsoleProgress());

        ResultARotatedVertical.Refresh();

        Assert.That(Compare.ImagesAreEqual(ResultARotatedVertical.FullName, ReferenceResultARotatedVertical.FullName));
    }

    [Test]
    public async Task B_RotatedHorizontal()
    {
        ResetTestFiles();

        var rotate1 = await Combiner.RotateLeft(GreenDownLeftOfCenterTestImage.FullName);
        var rotate2 = await Combiner.Flip(GreenDownRightOfCenterTestImage.FullName);

        await Combiner.CombineImagesHorizontal(
            [
                rotate1.FullName, rotate2.FullName,
                CactusForestDriveTestPdf.FullName
            ], 3000, 3000, ResultBRotatedHorizontal.FullName,
            92, SKColors.White, new ConsoleProgress());

        ResultBRotatedHorizontal.Refresh();

        Assert.That(Compare.ImagesAreEqual(ResultBRotatedHorizontal.FullName,
            ReferenceResultBRotatedHorizontal.FullName));
    }

    [Test]
    public async Task C_GridAutoRowsAndColumns()
    {
        ResetTestFiles();

        await Combiner.CombineImagesInGrid(
            [
                BlueBottomTestImage.FullName, BlueTopTestImage.FullName, GreenBlueSquareTestImage.FullName,
                GreenDownLeftOfCenterTestImage.FullName, GreenDownRightOfCenterTestImage.FullName
            ], 3000, 3000, ResultCGridAutoRowsAndColumns.FullName,
            92, SKColors.Blue, new ConsoleProgress());

        ResultCGridAutoRowsAndColumns.Refresh();

        Assert.That(Compare.ImagesAreEqual(ResultCGridAutoRowsAndColumns.FullName,
            ReferenceResultCGridAutoRowsAndColumns.FullName));
    }

    [Test]
    public async Task D_GridFourColumns()
    {
        ResetTestFiles();

        await Combiner.CombineImagesInGrid(
            [
                BlueBottomTestImage.FullName, BlueTopTestImage.FullName, GreenBlueSquareTestImage.FullName,
                GreenDownLeftOfCenterTestImage.FullName, GreenDownRightOfCenterTestImage.FullName
            ], 3000, 3000, ResultDGridFourColumns.FullName,
            92, SKColors.Bisque, new ConsoleProgress(), null, 4);

        ResultDGridFourColumns.Refresh();

        Assert.That(Compare.ImagesAreEqual(ResultDGridFourColumns.FullName,
            ReferenceResultDGridFourColumns.FullName));
    }

    [Test]
    public async Task E_GridFiveRow()
    {
        ResetTestFiles();

        await Combiner.CombineImagesInGrid(
            [
                BlueBottomTestImage.FullName, BlueTopTestImage.FullName, GreenBlueSquareTestImage.FullName,
                GreenDownLeftOfCenterTestImage.FullName, GreenDownRightOfCenterTestImage.FullName
            ], 3000, 3000, ResultEGridFiveRows.FullName,
            92, SKColors.Bisque, new ConsoleProgress(), 5, null);

        ResultEGridFiveRows.Refresh();

        Assert.That(Compare.ImagesAreEqual(ResultEGridFiveRows.FullName,
            ReferenceResulEGridFiveRows.FullName));
    }

    [Test]
    public async Task F_GridThreeByThree()
    {
        ResetTestFiles();

        await Combiner.CombineImagesInGrid(
            [
                GreenDownLeftOfCenterTestImage.FullName, GreenDownRightOfCenterTestImage.FullName,
                BlueBottomTestImage.FullName, BlueTopTestImage.FullName, GreenBlueSquareTestImage.FullName
            ], 3000, 3000, ResultFGridThreeByThreeRows.FullName,
            92, SKColors.Red, new ConsoleProgress(), 3, 3);

        ResultFGridThreeByThreeRows.Refresh();

        Assert.That(Compare.ImagesAreEqual(ResultFGridThreeByThreeRows.FullName,
            ReferenceResulFGridThreeByThree.FullName));
    }

    public void ResetTestFiles()
    {
        foreach (var file in Directory.GetFiles(OriginalImageDirectory.FullName))
        {
            if (Path.GetFileName(file).StartsWith("Result-", StringComparison.OrdinalIgnoreCase)) continue;
            File.Copy(file, Path.Combine(UnitTestDirectory.FullName, Path.GetFileName(file)), true);
        }
    }

    [OneTimeSetUp]
    public void Setup()
    {
        OriginalImageDirectory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "TestImages"));
        UnitTestDirectory =
            new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, $"{DateTime.Now:yy-MM-dd-HH-mm-ss}-ImageTests"));
        UnitTestDirectory.Create();

        ResetTestFiles();

        BlueBottomTestImage = new FileInfo(Path.Combine(UnitTestDirectory.FullName, "BlueBottom.png"));
        BlueTopTestImage = new FileInfo(Path.Combine(UnitTestDirectory.FullName, "BlueTop.jpg"));
        CactusForestDriveTestPdf = new FileInfo(Path.Combine(UnitTestDirectory.FullName, "CactusForestDrive.pdf"));
        GreenBlueSquareTestImage = new FileInfo(Path.Combine(UnitTestDirectory.FullName, "GreenBlueSquare.jpeg"));
        GreenDownLeftOfCenterTestImage =
            new FileInfo(Path.Combine(UnitTestDirectory.FullName, "GreenDownLeftOfCenter.bmp"));
        GreenDownRightOfCenterTestImage =
            new FileInfo(Path.Combine(UnitTestDirectory.FullName, "GreenDownRightOfCenter.gif"));
        ResultARotatedVertical = new FileInfo(Path.Combine(UnitTestDirectory.FullName, "A-RotatedVertical.jpg"));
        ResultBRotatedHorizontal = new FileInfo(Path.Combine(UnitTestDirectory.FullName, "B-RotatedHorizontal.jpg"));
        ResultCGridAutoRowsAndColumns =
            new FileInfo(Path.Combine(UnitTestDirectory.FullName, "C-GridAutoRowsAndColumns.jpg"));
        ResultDGridFourColumns = new FileInfo(Path.Combine(UnitTestDirectory.FullName, "D-GridFourColumns.jpg"));
        ResultEGridFiveRows = new FileInfo(Path.Combine(UnitTestDirectory.FullName, "E-GridFiveRows.jpg"));
        ResultFGridThreeByThreeRows = new FileInfo(Path.Combine(UnitTestDirectory.FullName, "F-GridThreeByThree.jpg"));

        ReferenceResultARotatedVertical =
            new FileInfo(Path.Combine(OriginalImageDirectory.FullName, "Result-A-RotatedVertical.jpg"));
        ReferenceResultBRotatedHorizontal =
            new FileInfo(Path.Combine(OriginalImageDirectory.FullName, "Result-B-RotatedHorizontal.jpg"));
        ReferenceResultCGridAutoRowsAndColumns =
            new FileInfo(Path.Combine(OriginalImageDirectory.FullName, "Result-C-GridAutoRowsAndColumns.jpg"));
        ReferenceResultDGridFourColumns =
            new FileInfo(Path.Combine(OriginalImageDirectory.FullName, "Result-D-GridFourColumns.jpg"));
        ReferenceResulEGridFiveRows =
            new FileInfo(Path.Combine(OriginalImageDirectory.FullName, "Result-E-GridFiveRows.jpg"));
        ReferenceResulFGridThreeByThree =
            new FileInfo(Path.Combine(OriginalImageDirectory.FullName, "Result-F-GridThreeByThree.jpg"));
    }
}