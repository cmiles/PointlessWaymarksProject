namespace PointlessWaymarks.CmsData.ExcelImport;

public class ContentImportValueParse<T>
{
    public T? ParsedValue { get; set; }
    public string? StringValue { get; init; }
    public bool? ValueParsed { get; set; }
}