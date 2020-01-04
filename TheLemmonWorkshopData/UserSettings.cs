namespace TheLemmonWorkshopData
{
    public class UserSettings
    {
        public string AmazonS3AccessKey { get; set; } = string.Empty;
        public string AmazonS3Bucket { get; set; } = string.Empty;
        public string AmazonS3SecretKey { get; set; } = string.Empty;
        public string BingApiKey { get; set; } = string.Empty;
        public string CalTopoApiKey { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "PointlessWaymarksDb";
        public string GoogleMapsApiKey { get; set; } = string.Empty;

        public string LocalSiteRootDirectory { get; set; }

        public string LocalMasterMediaArchive { get; set; }

        public string SiteName { get; set; }

        public string SiteUrl { get; set; }
    }
}