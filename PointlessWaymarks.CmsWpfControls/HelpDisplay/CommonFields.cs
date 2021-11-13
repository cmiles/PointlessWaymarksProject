namespace PointlessWaymarks.CmsWpfControls.HelpDisplay;

public static class CommonFields
{
    public static string FolderFieldBlock =>
        @"
**Folder:** This is a way to help you group and organize content - the Folder and Html for the content will be placed inside a folder with the specified name - for example you might choose to group content about the Grand Canyon into a 'GrandCanyon' folder. The Folder will appear in the URL for the content.
";

    public static string SlugFieldBlock =>
        @"
**Slug:** This will be used as the web identifier for the content - it will be the file name of the generated html, used as the folder name to group all the related files and be seen in the url for the content. In most cases using 'Title to Slug' is a great option - it will transform the title into a consistent string eliminating problematic/illegal characters.
";

    public static string SummaryFieldBlock =>
        @"
**Summary:** Used in a number of places as the short summary of the content.
";

    public static string TitleFieldBlock =>
        @"
**Title:**  Title for the content.
";

    public static string TitleSlugFolderSummary =>
        TitleFieldBlock + SlugFieldBlock + FolderFieldBlock + SummaryFieldBlock;
}