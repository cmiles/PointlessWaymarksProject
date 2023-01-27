namespace PointlessWaymarks.CommonTools;

public static class WindowsNotificationBuilders
{
    public static WindowsNotificationTool NewNotifier(string attribution)
    {
        return new WindowsNotificationTool().SetAttribution(attribution);
    }

    public static WindowsNotificationTool SetAttribution(this WindowsNotificationTool toEdit, string attribution)
    {
        toEdit.Attribution = attribution;
        return toEdit;
    }

    public static WindowsNotificationTool SetAutomationLogoNotificationIconUrl(this WindowsNotificationTool toEdit)
    {
        toEdit.NotificationIconUrl =
            $"file://{Path.Combine(AppContext.BaseDirectory, "PointlessWaymarksCmsAutomationSquareLogo.png")}";
        return toEdit;
    }

    public static WindowsNotificationTool SetProjectLogoNotificationIconUrl(this WindowsNotificationTool toEdit)
    {
        toEdit.NotificationIconUrl =
            $"file://{Path.Combine(AppContext.BaseDirectory, "PointlessWaymarksCmsSquareLogo.png")}";
        return toEdit;
    }
}