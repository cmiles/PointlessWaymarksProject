using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.PointHtml
{
    public partial class SinglePointPage
    {
        public SinglePointPage(PointContent dbEntry)
        {
            DbEntry = dbEntry;
        }

        public PointContent DbEntry { get; set; }

        public void WriteLocalHtml()
        {
        }
    }
}