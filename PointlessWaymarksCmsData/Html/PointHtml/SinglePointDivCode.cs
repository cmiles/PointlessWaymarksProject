using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Html.PointHtml
{
    public partial class SinglePointDiv
    {
        public SinglePointDiv(PointContent dbEntry)
        {
            DbEntry = dbEntry;
        }

        public PointContent DbEntry { get; set; }

        public void WriteLocalHtml()
        {
        }
    }
}