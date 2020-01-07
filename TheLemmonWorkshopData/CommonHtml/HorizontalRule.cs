using HtmlTags;

namespace TheLemmonWorkshopData.CommonHtml
{
    public static class HorizontalRule
    {
        public static HtmlTag StandardRule()
        {
            return new HtmlTag("hr").AddClass("standard-rule");
        }
    }
}