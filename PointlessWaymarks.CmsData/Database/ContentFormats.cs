namespace PointlessWaymarks.CmsData.Database;

public static class ContentFormatDefaults
{
    public static ContentFormatEnum Content => ContentFormatEnum.MarkdigMarkdown01;
}

public enum ContentFormatEnum
{
    MarkdigMarkdown01,
    Html01
}