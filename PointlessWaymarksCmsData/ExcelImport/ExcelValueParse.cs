namespace PointlessWaymarksCmsData.ExcelImport
{
    public class ExcelValueParse<T>
    {
        public T ParsedValue { get; set; }
        public string StringValue { get; set; }
        public bool? ValueParsed { get; set; }
    }
}